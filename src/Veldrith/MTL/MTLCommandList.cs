using System;
using System.Collections.Generic;
using System.Diagnostics;
using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal unsafe class MtlCommandList : CommandList
    {
        public MTLCommandBuffer CommandBuffer => cb;

        public override bool IsDisposed => _disposed;

        public override string Name { get; set; }
        private readonly MtlGraphicsDevice gd;

        private readonly List<MtlBuffer> _availableStagingBuffers = new List<MtlBuffer>();
        private readonly CommandBufferUsageList<MtlBuffer> _submittedStagingBuffers = new CommandBufferUsageList<MtlBuffer>();
        private readonly object _submittedCommandsLock = new object();
        private readonly CommandBufferUsageList<MtlFence> _completionFences = new CommandBufferUsageList<MtlFence>();

        private readonly Dictionary<UIntPtr, DeviceBufferRange> _boundVertexBuffers = new Dictionary<UIntPtr, DeviceBufferRange>();
        private readonly Dictionary<UIntPtr, DeviceBufferRange> _boundFragmentBuffers = new Dictionary<UIntPtr, DeviceBufferRange>();
        private readonly Dictionary<UIntPtr, DeviceBufferRange> _boundComputeBuffers = new Dictionary<UIntPtr, DeviceBufferRange>();

        private readonly Dictionary<UIntPtr, MTLTexture> _boundVertexTextures = new Dictionary<UIntPtr, MTLTexture>();
        private readonly Dictionary<UIntPtr, MTLTexture> _boundFragmentTextures = new Dictionary<UIntPtr, MTLTexture>();
        private readonly Dictionary<UIntPtr, MTLTexture> _boundComputeTextures = new Dictionary<UIntPtr, MTLTexture>();

        private readonly Dictionary<UIntPtr, MTLSamplerState> _boundVertexSamplers = new Dictionary<UIntPtr, MTLSamplerState>();
        private readonly Dictionary<UIntPtr, MTLSamplerState> _boundFragmentSamplers = new Dictionary<UIntPtr, MTLSamplerState>();
        private readonly Dictionary<UIntPtr, MTLSamplerState> _boundComputeSamplers = new Dictionary<UIntPtr, MTLSamplerState>();

        private bool renderEncoderActive => !_rce.IsNull;
        private bool blitEncoderActive => !_bce.IsNull;
        private bool computeEncoderActive => !_cce.IsNull;
        private MTLCommandBuffer cb;
        private MtlFramebuffer _mtlFramebuffer;
        private uint _viewportCount;
        private bool _currentFramebufferEverActive;
        private MTLRenderCommandEncoder _rce;
        private MTLBlitCommandEncoder _bce;
        private MTLComputeCommandEncoder _cce;
        private RgbaFloat?[] _clearColors = Array.Empty<RgbaFloat?>();
        private (float depth, byte stencil)? _clearDepth;
        private MtlBuffer _indexBuffer;
        private uint _ibOffset;
        private MTLIndexType _indexType;
        private MtlPipeline _lastGraphicsPipeline;
        private MtlPipeline _graphicsPipeline;
        private MtlPipeline _lastComputePipeline;
        private MtlPipeline _computePipeline;
        private MTLViewport[] _viewports = Array.Empty<MTLViewport>();
        private bool _viewportsChanged;
        private MTLScissorRect[] _activeScissorRects = Array.Empty<MTLScissorRect>();
        private MTLScissorRect[] _scissorRects = Array.Empty<MTLScissorRect>();
        private uint _graphicsResourceSetCount;
        private BoundResourceSetInfo[] _graphicsResourceSets;
        private bool[] _graphicsResourceSetsActive;
        private uint _computeResourceSetCount;
        private BoundResourceSetInfo[] _computeResourceSets;
        private bool[] _computeResourceSetsActive;
        private uint _vertexBufferCount;
        private uint _nonVertexBufferCount;
        private MtlBuffer[] _vertexBuffers;
        private bool[] _vertexBuffersActive;
        private uint[] _vbOffsets;
        private bool[] _vbOffsetsActive;
        private bool _disposed;

        public MtlCommandList(ref CommandListDescription description, MtlGraphicsDevice gd)
            : base(ref description, gd.Features, gd.UniformBufferMinOffsetAlignment, gd.StructuredBufferMinOffsetAlignment)
        {
            this.gd = gd;
        }

        #region Disposal

        public override void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                ensureNoRenderPass();

                lock (_submittedStagingBuffers)
                {
                    foreach (var buffer in _availableStagingBuffers)
                        buffer.Dispose();

                    foreach (var buffer in _submittedStagingBuffers.EnumerateItems())
                        buffer.Dispose();

                    _submittedStagingBuffers.Clear();
                }

                if (cb.NativePtr != IntPtr.Zero) ObjectiveCRuntime.release(cb.NativePtr);
            }
        }

        #endregion

        public MTLCommandBuffer Commit()
        {
            cb.commit();
            var ret = cb;
            cb = default;
            return ret;
        }

        public override void Begin()
        {
            if (cb.NativePtr != IntPtr.Zero) ObjectiveCRuntime.release(cb.NativePtr);

            using (NSAutoreleasePool.Begin())
            {
                cb = gd.CommandQueue.commandBuffer();
                ObjectiveCRuntime.retain(cb.NativePtr);
            }

            ClearCachedState();
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            preComputeCommand();
            _cce.dispatchThreadGroups(
                new MTLSize(groupCountX, groupCountY, groupCountZ),
                _computePipeline.ThreadsPerThreadgroup);
        }

        public override void End()
        {
            ensureNoBlitEncoder();
            ensureNoComputeEncoder();

            if (!_currentFramebufferEverActive && _mtlFramebuffer != null) beginCurrentRenderPass();
            ensureNoRenderPass();
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            _scissorRects[index] = new MTLScissorRect(x, y, width, height);
        }

        public override void SetViewport(uint index, ref Viewport viewport)
        {
            _viewportsChanged = true;
            _viewports[index] = new MTLViewport(
                viewport.X,
                viewport.Y,
                viewport.Width,
                viewport.Height,
                viewport.MinDepth,
                viewport.MaxDepth);
        }

        public void SetCompletionFence(MTLCommandBuffer cb, MtlFence fence)
        {
            lock (_submittedCommandsLock)
            {
                Debug.Assert(!_completionFences.Contains(cb));
                _completionFences.Add(cb, fence);
            }
        }

        public void OnCompleted(MTLCommandBuffer cb)
        {
            lock (_submittedCommandsLock)
            {
                foreach (var fence in _completionFences.EnumerateAndRemove(cb))
                    fence.Set();

                foreach (var buffer in _submittedStagingBuffers.EnumerateAndRemove(cb))
                    _availableStagingBuffers.Add(buffer);
            }
        }

        protected override void CopyBufferCore(
            DeviceBuffer source,
            uint sourceOffset,
            DeviceBuffer destination,
            uint destinationOffset,
            uint sizeInBytes)
        {
            var mtlSrc = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(source);
            var mtlDst = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(destination);

            if (sourceOffset % 4 != 0 || destinationOffset % 4 != 0 || sizeInBytes % 4 != 0)
            {
                // Unaligned copy -- use special compute shader.
                ensureComputeEncoder();
                _cce.setComputePipelineState(gd.GetUnalignedBufferCopyPipeline());
                _cce.setBuffer(mtlSrc.DeviceBuffer, UIntPtr.Zero, 0);
                _cce.setBuffer(mtlDst.DeviceBuffer, UIntPtr.Zero, 1);

                MtlUnalignedBufferCopyInfo copyInfo;
                copyInfo.SourceOffset = sourceOffset;
                copyInfo.DestinationOffset = destinationOffset;
                copyInfo.CopySize = sizeInBytes;

                _cce.setBytes(&copyInfo, (UIntPtr)sizeof(MtlUnalignedBufferCopyInfo), 2);
                _cce.dispatchThreadGroups(new MTLSize(1, 1, 1), new MTLSize(1, 1, 1));
            }
            else
            {
                ensureBlitEncoder();
                _bce.copy(
                    mtlSrc.DeviceBuffer, sourceOffset,
                    mtlDst.DeviceBuffer, destinationOffset,
                    sizeInBytes);
            }
        }

        protected override void CopyTextureCore(
            Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer,
            Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer,
            uint width, uint height, uint depth, uint layerCount)
        {
            ensureBlitEncoder();
            var srcMtlTexture = Util.AssertSubtype<Texture, MtlTexture>(source);
            var dstMtlTexture = Util.AssertSubtype<Texture, MtlTexture>(destination);

            bool srcIsStaging = (source.Usage & TextureUsage.Staging) != 0;
            bool dstIsStaging = (destination.Usage & TextureUsage.Staging) != 0;

            if (srcIsStaging && !dstIsStaging)
            {
                // Staging -> Normal
                var srcBuffer = srcMtlTexture.StagingBuffer;
                var dstTexture = dstMtlTexture.DeviceTexture;

                Util.GetMipDimensions(srcMtlTexture, srcMipLevel, out uint mipWidth, out uint mipHeight, out uint _);

                for (uint layer = 0; layer < layerCount; layer++)
                {
                    uint blockSize = FormatHelpers.IsCompressedFormat(srcMtlTexture.Format) ? 4u : 1u;
                    uint compressedSrcX = srcX / blockSize;
                    uint compressedSrcY = srcY / blockSize;
                    uint blockSizeInBytes = blockSize == 1
                        ? FormatSizeHelpers.GetSizeInBytes(srcMtlTexture.Format)
                        : FormatHelpers.GetBlockSizeInBytes(srcMtlTexture.Format);

                    ulong srcSubresourceBase = Util.ComputeSubresourceOffset(
                        srcMtlTexture,
                        srcMipLevel,
                        layer + srcBaseArrayLayer);
                    srcMtlTexture.GetSubresourceLayout(
                        srcMipLevel,
                        srcBaseArrayLayer + layer,
                        out uint srcRowPitch,
                        out uint srcDepthPitch);
                    ulong sourceOffset = srcSubresourceBase
                                         + srcDepthPitch * srcZ
                                         + srcRowPitch * compressedSrcY
                                         + blockSizeInBytes * compressedSrcX;

                    uint copyWidth = width > mipWidth && width <= blockSize
                        ? mipWidth
                        : width;

                    uint copyHeight = height > mipHeight && height <= blockSize
                        ? mipHeight
                        : height;

                    var sourceSize = new MTLSize(copyWidth, copyHeight, depth);
                    if (dstMtlTexture.Type != TextureType.Texture3D) srcDepthPitch = 0;
                    _bce.copyFromBuffer(
                        srcBuffer,
                        (UIntPtr)sourceOffset,
                        srcRowPitch,
                        srcDepthPitch,
                        sourceSize,
                        dstTexture,
                        dstBaseArrayLayer + layer,
                        dstMipLevel,
                        new MTLOrigin(dstX, dstY, dstZ),
                        gd.MetalFeatures.IsMacOS);
                }
            }
            else if (srcIsStaging)
            {
                for (uint layer = 0; layer < layerCount; layer++)
                {
                    // Staging -> Staging
                    ulong srcSubresourceBase = Util.ComputeSubresourceOffset(
                        srcMtlTexture,
                        srcMipLevel,
                        layer + srcBaseArrayLayer);
                    srcMtlTexture.GetSubresourceLayout(
                        srcMipLevel,
                        srcBaseArrayLayer + layer,
                        out uint srcRowPitch,
                        out uint srcDepthPitch);

                    ulong dstSubresourceBase = Util.ComputeSubresourceOffset(
                        dstMtlTexture,
                        dstMipLevel,
                        layer + dstBaseArrayLayer);
                    dstMtlTexture.GetSubresourceLayout(
                        dstMipLevel,
                        dstBaseArrayLayer + layer,
                        out uint dstRowPitch,
                        out uint dstDepthPitch);

                    uint blockSize = FormatHelpers.IsCompressedFormat(dstMtlTexture.Format) ? 4u : 1u;

                    if (blockSize == 1)
                    {
                        uint pixelSize = FormatSizeHelpers.GetSizeInBytes(dstMtlTexture.Format);
                        uint copySize = width * pixelSize;

                        for (uint zz = 0; zz < depth; zz++)
                        {
                            for (uint yy = 0; yy < height; yy++)
                            {
                                ulong srcRowOffset = srcSubresourceBase
                                                     + srcDepthPitch * (zz + srcZ)
                                                     + srcRowPitch * (yy + srcY)
                                                     + pixelSize * srcX;
                                ulong dstRowOffset = dstSubresourceBase
                                                     + dstDepthPitch * (zz + dstZ)
                                                     + dstRowPitch * (yy + dstY)
                                                     + pixelSize * dstX;
                                _bce.copy(
                                    srcMtlTexture.StagingBuffer,
                                    (UIntPtr)srcRowOffset,
                                    dstMtlTexture.StagingBuffer,
                                    (UIntPtr)dstRowOffset,
                                    copySize);
                            }
                        }
                    }
                    else // blockSize != 1
                    {
                        uint paddedWidth = Math.Max(blockSize, width);
                        uint paddedHeight = Math.Max(blockSize, height);
                        uint numRows = FormatHelpers.GetNumRows(paddedHeight, srcMtlTexture.Format);
                        uint rowPitch = FormatHelpers.GetRowPitch(paddedWidth, srcMtlTexture.Format);

                        uint compressedSrcX = srcX / 4;
                        uint compressedSrcY = srcY / 4;
                        uint compressedDstX = dstX / 4;
                        uint compressedDstY = dstY / 4;
                        uint blockSizeInBytes = FormatHelpers.GetBlockSizeInBytes(srcMtlTexture.Format);

                        for (uint zz = 0; zz < depth; zz++)
                        {
                            for (uint row = 0; row < numRows; row++)
                            {
                                ulong srcRowOffset = srcSubresourceBase
                                                     + srcDepthPitch * (zz + srcZ)
                                                     + srcRowPitch * (row + compressedSrcY)
                                                     + blockSizeInBytes * compressedSrcX;
                                ulong dstRowOffset = dstSubresourceBase
                                                     + dstDepthPitch * (zz + dstZ)
                                                     + dstRowPitch * (row + compressedDstY)
                                                     + blockSizeInBytes * compressedDstX;
                                _bce.copy(
                                    srcMtlTexture.StagingBuffer,
                                    (UIntPtr)srcRowOffset,
                                    dstMtlTexture.StagingBuffer,
                                    (UIntPtr)dstRowOffset,
                                    rowPitch);
                            }
                        }
                    }
                }
            }
            else if (dstIsStaging)
            {
                // Normal -> Staging
                var srcOrigin = new MTLOrigin(srcX, srcY, srcZ);
                var srcSize = new MTLSize(width, height, depth);

                for (uint layer = 0; layer < layerCount; layer++)
                {
                    dstMtlTexture.GetSubresourceLayout(
                        dstMipLevel,
                        dstBaseArrayLayer + layer,
                        out uint dstBytesPerRow,
                        out uint dstBytesPerImage);

                    Util.GetMipDimensions(srcMtlTexture, dstMipLevel, out uint mipWidth, out uint mipHeight, out uint _);
                    uint blockSize = FormatHelpers.IsCompressedFormat(srcMtlTexture.Format) ? 4u : 1u;
                    uint bufferRowLength = Math.Max(mipWidth, blockSize);
                    uint bufferImageHeight = Math.Max(mipHeight, blockSize);
                    uint compressedDstX = dstX / blockSize;
                    uint compressedDstY = dstY / blockSize;
                    uint blockSizeInBytes = blockSize == 1
                        ? FormatSizeHelpers.GetSizeInBytes(srcMtlTexture.Format)
                        : FormatHelpers.GetBlockSizeInBytes(srcMtlTexture.Format);
                    uint rowPitch = FormatHelpers.GetRowPitch(bufferRowLength, srcMtlTexture.Format);
                    uint depthPitch = FormatHelpers.GetDepthPitch(rowPitch, bufferImageHeight, srcMtlTexture.Format);

                    ulong dstOffset = Util.ComputeSubresourceOffset(dstMtlTexture, dstMipLevel, dstBaseArrayLayer + layer)
                                      + dstZ * depthPitch
                                      + compressedDstY * rowPitch
                                      + compressedDstX * blockSizeInBytes;

                    _bce.copyTextureToBuffer(
                        srcMtlTexture.DeviceTexture,
                        srcBaseArrayLayer + layer,
                        srcMipLevel,
                        srcOrigin,
                        srcSize,
                        dstMtlTexture.StagingBuffer,
                        (UIntPtr)dstOffset,
                        dstBytesPerRow,
                        dstBytesPerImage);
                }
            }
            else
            {
                // Normal -> Normal
                for (uint layer = 0; layer < layerCount; layer++)
                {
                    _bce.copyFromTexture(
                        srcMtlTexture.DeviceTexture,
                        srcBaseArrayLayer + layer,
                        srcMipLevel,
                        new MTLOrigin(srcX, srcY, srcZ),
                        new MTLSize(width, height, depth),
                        dstMtlTexture.DeviceTexture,
                        dstBaseArrayLayer + layer,
                        dstMipLevel,
                        new MTLOrigin(dstX, dstY, dstZ),
                        gd.MetalFeatures.IsMacOS);
                }
            }
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
            var mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(indirectBuffer);
            preComputeCommand();
            _cce.dispatchThreadgroupsWithIndirectBuffer(
                mtlBuffer.DeviceBuffer,
                offset,
                _computePipeline.ThreadsPerThreadgroup);
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            if (preDrawCommand())
            {
                var mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(indirectBuffer);

                for (uint i = 0; i < drawCount; i++)
                {
                    uint currentOffset = i * stride + offset;
                    _rce.drawIndexedPrimitives(
                        _graphicsPipeline.PrimitiveType,
                        _indexType,
                        _indexBuffer.DeviceBuffer,
                        _ibOffset,
                        mtlBuffer.DeviceBuffer,
                        currentOffset);
                }
            }
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            if (preDrawCommand())
            {
                var mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(indirectBuffer);

                for (uint i = 0; i < drawCount; i++)
                {
                    uint currentOffset = i * stride + offset;
                    _rce.drawPrimitives(_graphicsPipeline.PrimitiveType, mtlBuffer.DeviceBuffer, currentOffset);
                }
            }
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            // TODO: This approach destroys the contents of the source Texture (according to the docs).
            ensureNoBlitEncoder();
            ensureNoRenderPass();

            var mtlSrc = Util.AssertSubtype<Texture, MtlTexture>(source);
            var mtlDst = Util.AssertSubtype<Texture, MtlTexture>(destination);

            var rpDesc = MTLRenderPassDescriptor.New();
            var colorAttachment = rpDesc.colorAttachments[0];
            colorAttachment.texture = mtlSrc.DeviceTexture;
            colorAttachment.loadAction = MTLLoadAction.Load;
            colorAttachment.storeAction = MTLStoreAction.MultisampleResolve;
            colorAttachment.resolveTexture = mtlDst.DeviceTexture;

            using (NSAutoreleasePool.Begin())
            {
                var encoder = cb.renderCommandEncoderWithDescriptor(rpDesc);
                encoder.endEncoding();
            }

            ObjectiveCRuntime.release(rpDesc.NativePtr);
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetCount, ref uint dynamicOffsets)
        {
            if (!_computeResourceSets[slot].Equals(set, dynamicOffsetCount, ref dynamicOffsets))
            {
                _computeResourceSets[slot].Offsets.Dispose();
                _computeResourceSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetCount, ref dynamicOffsets);
                _computeResourceSetsActive[slot] = false;
            }
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            if (!_currentFramebufferEverActive && _mtlFramebuffer != null)
            {
                // This ensures that any submitted clear values will be used even if nothing has been drawn.
                if (ensureRenderPass()) endCurrentRenderPass();
            }

            ensureNoRenderPass();
            _mtlFramebuffer = Util.AssertSubtype<Framebuffer, MtlFramebuffer>(fb);
            _viewportCount = Math.Max(1u, (uint)fb.ColorTargets.Count);
            Util.EnsureArrayMinimumSize(ref _viewports, _viewportCount);
            Util.ClearArray(_viewports);
            Util.EnsureArrayMinimumSize(ref _scissorRects, _viewportCount);
            Util.ClearArray(_scissorRects);
            Util.EnsureArrayMinimumSize(ref _activeScissorRects, _viewportCount);
            Util.ClearArray(_activeScissorRects);
            Util.EnsureArrayMinimumSize(ref _clearColors, (uint)fb.ColorTargets.Count);
            Util.ClearArray(_clearColors);
            _currentFramebufferEverActive = false;
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetCount, ref uint dynamicOffsets)
        {
            if (!_graphicsResourceSets[slot].Equals(rs, dynamicOffsetCount, ref dynamicOffsets))
            {
                _graphicsResourceSets[slot].Offsets.Dispose();
                _graphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetCount, ref dynamicOffsets);
                _graphicsResourceSetsActive[slot] = false;
            }
        }

        private bool preDrawCommand()
        {
            if (ensureRenderPass())
            {
                if (_viewportsChanged)
                {
                    flushViewports();
                    _viewportsChanged = false;
                }

                if (_graphicsPipeline.ScissorTestEnabled)
                    flushScissorRects();

                Debug.Assert(_graphicsPipeline != null);

                if (_graphicsPipeline.RenderPipelineState.NativePtr != _lastGraphicsPipeline?.RenderPipelineState.NativePtr)
                    _rce.setRenderPipelineState(_graphicsPipeline.RenderPipelineState);

                if (_graphicsPipeline.CullMode != _lastGraphicsPipeline?.CullMode)
                    _rce.setCullMode(_graphicsPipeline.CullMode);

                if (_graphicsPipeline.FrontFace != _lastGraphicsPipeline?.FrontFace)
                    _rce.setFrontFacing(_graphicsPipeline.FrontFace);

                if (_graphicsPipeline.FillMode != _lastGraphicsPipeline?.FillMode)
                    _rce.setTriangleFillMode(_graphicsPipeline.FillMode);

                var blendColor = _graphicsPipeline.BlendColor;
                if (blendColor != _lastGraphicsPipeline?.BlendColor)
                    _rce.setBlendColor(blendColor.R, blendColor.G, blendColor.B, blendColor.A);

                if (Framebuffer.DepthTarget != null)
                {
                    if (_graphicsPipeline.DepthStencilState.NativePtr != _lastGraphicsPipeline?.DepthStencilState.NativePtr)
                        _rce.setDepthStencilState(_graphicsPipeline.DepthStencilState);

                    if (_graphicsPipeline.DepthClipMode != _lastGraphicsPipeline?.DepthClipMode)
                        _rce.setDepthClipMode(_graphicsPipeline.DepthClipMode);

                    if (_graphicsPipeline.StencilReference != _lastGraphicsPipeline?.StencilReference)
                        _rce.setStencilReferenceValue(_graphicsPipeline.StencilReference);
                }

                _lastGraphicsPipeline = _graphicsPipeline;

                for (uint i = 0; i < _graphicsResourceSetCount; i++)
                {
                    if (!_graphicsResourceSetsActive[i])
                    {
                        activateGraphicsResourceSet(i, _graphicsResourceSets[i]);
                        _graphicsResourceSetsActive[i] = true;
                    }
                }

                for (uint i = 0; i < _vertexBufferCount; i++)
                {
                    if (!_vertexBuffersActive[i])
                    {
                        UIntPtr index = _graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                            ? _nonVertexBufferCount + i
                            : i;
                        _rce.setVertexBuffer(
                            _vertexBuffers[i].DeviceBuffer,
                            _vbOffsets[i],
                            index);

                        _vertexBuffersActive[i] = true;
                        _vbOffsetsActive[i] = true;
                    }

                    if (!_vbOffsetsActive[i])
                    {
                        UIntPtr index = _graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                            ? _nonVertexBufferCount + i
                            : i;

                        _rce.setVertexBufferOffset(
                            _vbOffsets[i],
                            index);

                        _vbOffsetsActive[i] = true;
                    }
                }

                return true;
            }

            return false;
        }

        private void flushViewports()
        {
            if (gd.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3))
            {
                fixed (MTLViewport* viewportsPtr = &_viewports[0])
                    _rce.setViewports(viewportsPtr, _viewportCount);
            }
            else
                _rce.setViewport(_viewports[0]);
        }

        private void flushScissorRects()
        {
            if (gd.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3))
            {
                bool scissorRectsChanged = false;

                for (int i = 0; i < _scissorRects.Length; i++)
                {
                    scissorRectsChanged |= !_scissorRects[i].Equals(_activeScissorRects[i]);
                    _activeScissorRects[i] = _scissorRects[i];
                }

                if (scissorRectsChanged)
                {
                    fixed (MTLScissorRect* scissorRectsPtr = _scissorRects)
                        _rce.setScissorRects(scissorRectsPtr, _viewportCount);
                }
            }
            else
            {
                if (!_scissorRects[0].Equals(_activeScissorRects[0]))
                    _rce.setScissorRect(_scissorRects[0]);

                _activeScissorRects[0] = _scissorRects[0];
            }
        }

        private void preComputeCommand()
        {
            ensureComputeEncoder();

            if (_computePipeline.ComputePipelineState.NativePtr != _lastComputePipeline?.ComputePipelineState.NativePtr)
                _cce.setComputePipelineState(_computePipeline.ComputePipelineState);

            _lastComputePipeline = _computePipeline;

            for (uint i = 0; i < _computeResourceSetCount; i++)
            {
                if (!_computeResourceSetsActive[i])
                {
                    activateComputeResourceSet(i, _computeResourceSets[i]);
                    _computeResourceSetsActive[i] = true;
                }
            }
        }

        private MtlBuffer getFreeStagingBuffer(uint sizeInBytes)
        {
            lock (_submittedCommandsLock)
            {
                foreach (var buffer in _availableStagingBuffers)
                {
                    if (buffer.SizeInBytes >= sizeInBytes)
                    {
                        _availableStagingBuffers.Remove(buffer);
                        return buffer;
                    }
                }
            }

            var staging = gd.ResourceFactory.CreateBuffer(
                new BufferDescription(sizeInBytes, BufferUsage.Staging));

            return Util.AssertSubtype<DeviceBuffer, MtlBuffer>(staging);
        }

        private void activateGraphicsResourceSet(uint slot, BoundResourceSetInfo brsi)
        {
            Debug.Assert(renderEncoderActive);
            var mtlRs = Util.AssertSubtype<ResourceSet, MtlResourceSet>(brsi.Set);
            var layout = mtlRs.Layout;
            uint dynamicOffsetIndex = 0;

            for (int i = 0; i < mtlRs.Resources.Length; i++)
            {
                var bindingInfo = layout.GetBindingInfo(i);
                var resource = mtlRs.Resources[i];
                uint bufferOffset = 0;

                if (bindingInfo.DynamicBuffer)
                {
                    bufferOffset = brsi.Offsets.Get(dynamicOffsetIndex);
                    dynamicOffsetIndex += 1;
                }

                switch (bindingInfo.Kind)
                {
                    case ResourceKind.UniformBuffer:
                    {
                        var range = Util.GetBufferRange(resource, bufferOffset);
                        bindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    case ResourceKind.TextureReadOnly:
                        var texView = Util.GetTextureView(gd, resource);
                        var mtlTexView = Util.AssertSubtype<TextureView, MtlTextureView>(texView);
                        bindTexture(mtlTexView, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.TextureReadWrite:
                        var texViewRw = Util.GetTextureView(gd, resource);
                        var mtlTexViewRw = Util.AssertSubtype<TextureView, MtlTextureView>(texViewRw);
                        bindTexture(mtlTexViewRw, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.Sampler:
                        var mtlSampler = Util.AssertSubtype<IBindableResource, MtlSampler>(resource);
                        bindSampler(mtlSampler, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.StructuredBufferReadOnly:
                    {
                        var range = Util.GetBufferRange(resource, bufferOffset);
                        bindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    case ResourceKind.StructuredBufferReadWrite:
                    {
                        var range = Util.GetBufferRange(resource, bufferOffset);
                        bindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    default:
                        throw Illegal.Value<ResourceKind>();
                }
            }
        }

        private void activateComputeResourceSet(uint slot, BoundResourceSetInfo brsi)
        {
            Debug.Assert(computeEncoderActive);
            var mtlRs = Util.AssertSubtype<ResourceSet, MtlResourceSet>(brsi.Set);
            var layout = mtlRs.Layout;
            uint dynamicOffsetIndex = 0;

            for (int i = 0; i < mtlRs.Resources.Length; i++)
            {
                var bindingInfo = layout.GetBindingInfo(i);
                var resource = mtlRs.Resources[i];
                uint bufferOffset = 0;

                if (bindingInfo.DynamicBuffer)
                {
                    bufferOffset = brsi.Offsets.Get(dynamicOffsetIndex);
                    dynamicOffsetIndex += 1;
                }

                switch (bindingInfo.Kind)
                {
                    case ResourceKind.UniformBuffer:
                    {
                        var range = Util.GetBufferRange(resource, bufferOffset);
                        bindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    case ResourceKind.TextureReadOnly:
                        var texView = Util.GetTextureView(gd, resource);
                        var mtlTexView = Util.AssertSubtype<TextureView, MtlTextureView>(texView);
                        bindTexture(mtlTexView, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.TextureReadWrite:
                        var texViewRw = Util.GetTextureView(gd, resource);
                        var mtlTexViewRw = Util.AssertSubtype<TextureView, MtlTextureView>(texViewRw);
                        bindTexture(mtlTexViewRw, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.Sampler:
                        var mtlSampler = Util.AssertSubtype<IBindableResource, MtlSampler>(resource);
                        bindSampler(mtlSampler, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.StructuredBufferReadOnly:
                    {
                        var range = Util.GetBufferRange(resource, bufferOffset);
                        bindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    case ResourceKind.StructuredBufferReadWrite:
                    {
                        var range = Util.GetBufferRange(resource, bufferOffset);
                        bindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    default:
                        throw Illegal.Value<ResourceKind>();
                }
            }
        }

        private void bindBuffer(DeviceBufferRange range, uint set, uint slot, ShaderStages stages)
        {
            var mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(range.Buffer);
            uint baseBuffer = getBufferBase(set, stages != ShaderStages.Compute);

            if (stages == ShaderStages.Compute)
            {
                UIntPtr index = slot + baseBuffer;

                if (!_boundComputeBuffers.TryGetValue(index, out var boundBuffer) || !range.Equals(boundBuffer))
                {
                    _cce.setBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);
                    _boundComputeBuffers[index] = range;
                }
            }
            else
            {
                if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
                {
                    UIntPtr index = _graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                        ? slot + baseBuffer
                        : slot + _vertexBufferCount + baseBuffer;

                    if (!_boundVertexBuffers.TryGetValue(index, out var boundBuffer) || boundBuffer.Buffer != range.Buffer)
                    {
                        _rce.setVertexBuffer(mtlBuffer.DeviceBuffer, range.Offset, index);
                        _boundVertexBuffers[index] = range;
                    }
                    else if (!range.Equals(boundBuffer))
                    {
                        _rce.setVertexBufferOffset(range.Offset, index);
                        _boundVertexBuffers[index] = range;
                    }
                }

                if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
                {
                    UIntPtr index = slot + baseBuffer;

                    if (!_boundFragmentBuffers.TryGetValue(index, out var boundBuffer) || boundBuffer.Buffer != range.Buffer)
                    {
                        _rce.setFragmentBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);
                        _boundFragmentBuffers[index] = range;
                    }
                    else if (!range.Equals(boundBuffer))
                    {
                        _rce.setFragmentBufferOffset(range.Offset, slot + baseBuffer);
                        _boundFragmentBuffers[index] = range;
                    }
                }
            }
        }

        private void bindTexture(MtlTextureView mtlTexView, uint set, uint slot, ShaderStages stages)
        {
            uint baseTexture = getTextureBase(set, stages != ShaderStages.Compute);
            UIntPtr index = slot + baseTexture;

            if (stages == ShaderStages.Compute && (!_boundComputeTextures.TryGetValue(index, out var computeTexture) || computeTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr))
            {
                _cce.setTexture(mtlTexView.TargetDeviceTexture, index);
                _boundComputeTextures[index] = mtlTexView.TargetDeviceTexture;
            }

            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex
                && (!_boundVertexTextures.TryGetValue(index, out var vertexTexture) || vertexTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr))
            {
                _rce.setVertexTexture(mtlTexView.TargetDeviceTexture, index);
                _boundVertexTextures[index] = mtlTexView.TargetDeviceTexture;
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment
                && (!_boundFragmentTextures.TryGetValue(index, out var fragmentTexture) || fragmentTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr))
            {
                _rce.setFragmentTexture(mtlTexView.TargetDeviceTexture, index);
                _boundFragmentTextures[index] = mtlTexView.TargetDeviceTexture;
            }
        }

        private void bindSampler(MtlSampler mtlSampler, uint set, uint slot, ShaderStages stages)
        {
            uint baseSampler = getSamplerBase(set, stages != ShaderStages.Compute);
            UIntPtr index = slot + baseSampler;

            if (stages == ShaderStages.Compute && (!_boundComputeSamplers.TryGetValue(index, out var computeSampler) || computeSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr))
            {
                _cce.setSamplerState(mtlSampler.DeviceSampler, index);
                _boundComputeSamplers[index] = mtlSampler.DeviceSampler;
            }

            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex
                && (!_boundVertexSamplers.TryGetValue(index, out var vertexSampler) || vertexSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr))
            {
                _rce.setVertexSamplerState(mtlSampler.DeviceSampler, index);
                _boundVertexSamplers[index] = mtlSampler.DeviceSampler;
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment
                && (!_boundFragmentSamplers.TryGetValue(index, out var fragmentSampler) || fragmentSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr))
            {
                _rce.setFragmentSamplerState(mtlSampler.DeviceSampler, index);
                _boundFragmentSamplers[index] = mtlSampler.DeviceSampler;
            }
        }

        private uint getBufferBase(uint set, bool graphics)
        {
            var layouts = graphics ? _graphicsPipeline.ResourceLayouts : _computePipeline.ResourceLayouts;
            uint ret = 0;

            for (int i = 0; i < set; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].BufferCount;
            }

            return ret;
        }

        private uint getTextureBase(uint set, bool graphics)
        {
            var layouts = graphics ? _graphicsPipeline.ResourceLayouts : _computePipeline.ResourceLayouts;
            uint ret = 0;

            for (int i = 0; i < set; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].TextureCount;
            }

            return ret;
        }

        private uint getSamplerBase(uint set, bool graphics)
        {
            var layouts = graphics ? _graphicsPipeline.ResourceLayouts : _computePipeline.ResourceLayouts;
            uint ret = 0;

            for (int i = 0; i < set; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].SamplerCount;
            }

            return ret;
        }

        private bool ensureRenderPass()
        {
            Debug.Assert(_mtlFramebuffer != null);
            ensureNoBlitEncoder();
            ensureNoComputeEncoder();
            return renderEncoderActive || beginCurrentRenderPass();
        }

        private bool beginCurrentRenderPass()
        {
            if (_mtlFramebuffer is MtlSwapchainFramebuffer swapchainFramebuffer && !swapchainFramebuffer.EnsureDrawableAvailable())
                return false;

            var rpDesc = _mtlFramebuffer.CreateRenderPassDescriptor();

            for (uint i = 0; i < _clearColors.Length; i++)
            {
                if (_clearColors[i] != null)
                {
                    var attachment = rpDesc.colorAttachments[0];
                    attachment.loadAction = MTLLoadAction.Clear;
                    var c = _clearColors[i].Value;
                    attachment.clearColor = new MTLClearColor(c.R, c.G, c.B, c.A);
                    _clearColors[i] = null;
                }
            }

            if (_clearDepth != null)
            {
                var depthAttachment = rpDesc.depthAttachment;
                depthAttachment.loadAction = MTLLoadAction.Clear;
                depthAttachment.clearDepth = _clearDepth.Value.depth;

                if (_mtlFramebuffer.DepthTarget != null && FormatHelpers.IsStencilFormat(_mtlFramebuffer.DepthTarget.Value.Target.Format))
                {
                    var stencilAttachment = rpDesc.stencilAttachment;
                    stencilAttachment.loadAction = MTLLoadAction.Clear;
                    stencilAttachment.clearStencil = _clearDepth.Value.stencil;
                }

                _clearDepth = null;
            }

            using (NSAutoreleasePool.Begin())
            {
                _rce = cb.renderCommandEncoderWithDescriptor(rpDesc);
                ObjectiveCRuntime.retain(_rce.NativePtr);
            }

            ObjectiveCRuntime.release(rpDesc.NativePtr);
            _currentFramebufferEverActive = true;

            return true;
        }

        private void ensureNoRenderPass()
        {
            if (renderEncoderActive) endCurrentRenderPass();

            Debug.Assert(!renderEncoderActive);
        }

        private void endCurrentRenderPass()
        {
            _rce.endEncoding();
            ObjectiveCRuntime.release(_rce.NativePtr);
            _rce = default;

            _lastGraphicsPipeline = null;
            _boundVertexBuffers.Clear();
            _boundVertexTextures.Clear();
            _boundVertexSamplers.Clear();
            _boundFragmentBuffers.Clear();
            _boundFragmentTextures.Clear();
            _boundFragmentSamplers.Clear();
            Util.ClearArray(_graphicsResourceSetsActive);
            Util.ClearArray(_vertexBuffersActive);
            Util.ClearArray(_vbOffsetsActive);

            Util.ClearArray(_activeScissorRects);

            _viewportsChanged = true;
        }

        private void ensureBlitEncoder()
        {
            if (!blitEncoderActive)
            {
                ensureNoRenderPass();
                ensureNoComputeEncoder();

                using (NSAutoreleasePool.Begin())
                {
                    _bce = cb.blitCommandEncoder();
                    ObjectiveCRuntime.retain(_bce.NativePtr);
                }
            }

            Debug.Assert(blitEncoderActive);
            Debug.Assert(!renderEncoderActive);
            Debug.Assert(!computeEncoderActive);
        }

        private void ensureNoBlitEncoder()
        {
            if (blitEncoderActive)
            {
                _bce.endEncoding();
                ObjectiveCRuntime.release(_bce.NativePtr);
                _bce = default;
            }

            Debug.Assert(!blitEncoderActive);
        }

        private void ensureComputeEncoder()
        {
            if (!computeEncoderActive)
            {
                ensureNoBlitEncoder();
                ensureNoRenderPass();

                using (NSAutoreleasePool.Begin())
                {
                    _cce = cb.computeCommandEncoder();
                    ObjectiveCRuntime.retain(_cce.NativePtr);
                }
            }

            Debug.Assert(computeEncoderActive);
            Debug.Assert(!renderEncoderActive);
            Debug.Assert(!blitEncoderActive);
        }

        private void ensureNoComputeEncoder()
        {
            if (computeEncoderActive)
            {
                _cce.endEncoding();
                ObjectiveCRuntime.release(_cce.NativePtr);
                _cce = default;

                _boundComputeBuffers.Clear();
                _boundComputeTextures.Clear();
                _boundComputeSamplers.Clear();
                _lastComputePipeline = null;

                Util.ClearArray(_computeResourceSetsActive);
            }

            Debug.Assert(!computeEncoderActive);
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            ensureNoRenderPass();
            _clearColors[index] = clearColor;
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            ensureNoRenderPass();
            _clearDepth = (depth, stencil);
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            if (preDrawCommand())
            {
                if (instanceStart == 0)
                {
                    _rce.drawPrimitives(
                        _graphicsPipeline.PrimitiveType,
                        vertexStart,
                        vertexCount,
                        instanceCount);
                }
                else
                {
                    _rce.drawPrimitives(
                        _graphicsPipeline.PrimitiveType,
                        vertexStart,
                        vertexCount,
                        instanceCount,
                        instanceStart);
                }
            }
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            if (preDrawCommand())
            {
                uint indexSize = _indexType == MTLIndexType.UInt16 ? 2u : 4u;
                uint indexBufferOffset = indexSize * indexStart + _ibOffset;

                if (vertexOffset == 0 && instanceStart == 0)
                {
                    _rce.drawIndexedPrimitives(
                        _graphicsPipeline.PrimitiveType,
                        indexCount,
                        _indexType,
                        _indexBuffer.DeviceBuffer,
                        indexBufferOffset,
                        instanceCount);
                }
                else
                {
                    _rce.drawIndexedPrimitives(
                        _graphicsPipeline.PrimitiveType,
                        indexCount,
                        _indexType,
                        _indexBuffer.DeviceBuffer,
                        indexBufferOffset,
                        instanceCount,
                        vertexOffset,
                        instanceStart);
                }
            }
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            if (pipeline.IsComputePipeline && _computePipeline != pipeline)
            {
                _computePipeline = Util.AssertSubtype<Pipeline, MtlPipeline>(pipeline);
                _computeResourceSetCount = (uint)_computePipeline.ResourceLayouts.Length;
                Util.EnsureArrayMinimumSize(ref _computeResourceSets, _computeResourceSetCount);
                Util.EnsureArrayMinimumSize(ref _computeResourceSetsActive, _computeResourceSetCount);
                Util.ClearArray(_computeResourceSetsActive);
            }
            else if (!pipeline.IsComputePipeline && _graphicsPipeline != pipeline)
            {
                _graphicsPipeline = Util.AssertSubtype<Pipeline, MtlPipeline>(pipeline);
                _graphicsResourceSetCount = (uint)_graphicsPipeline.ResourceLayouts.Length;
                Util.EnsureArrayMinimumSize(ref _graphicsResourceSets, _graphicsResourceSetCount);
                Util.EnsureArrayMinimumSize(ref _graphicsResourceSetsActive, _graphicsResourceSetCount);
                Util.ClearArray(_graphicsResourceSetsActive);

                _nonVertexBufferCount = _graphicsPipeline.NonVertexBufferCount;

                _vertexBufferCount = _graphicsPipeline.VertexBufferCount;
                Util.EnsureArrayMinimumSize(ref _vertexBuffers, _vertexBufferCount);
                Util.EnsureArrayMinimumSize(ref _vbOffsets, _vertexBufferCount);
                Util.EnsureArrayMinimumSize(ref _vertexBuffersActive, _vertexBufferCount);
                Util.EnsureArrayMinimumSize(ref _vbOffsetsActive, _vertexBufferCount);
                Util.ClearArray(_vertexBuffersActive);
                Util.ClearArray(_vbOffsetsActive);
            }
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            bool useComputeCopy = bufferOffsetInBytes % 4 != 0
                                  || (sizeInBytes % 4 != 0 && bufferOffsetInBytes != 0 && sizeInBytes != buffer.SizeInBytes);

            var dstMtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
            var staging = getFreeStagingBuffer(sizeInBytes);

            gd.UpdateBuffer(staging, 0, source, sizeInBytes);

            if (useComputeCopy)
                CopyBufferCore(staging, 0, buffer, bufferOffsetInBytes, sizeInBytes);
            else
            {
                Debug.Assert(bufferOffsetInBytes % 4 == 0);
                uint sizeRoundFactor = (4 - sizeInBytes % 4) % 4;
                ensureBlitEncoder();
                _bce.copy(
                    staging.DeviceBuffer, UIntPtr.Zero,
                    dstMtlBuffer.DeviceBuffer, bufferOffsetInBytes,
                    sizeInBytes + sizeRoundFactor);
            }

            lock (_submittedCommandsLock)
                _submittedStagingBuffers.Add(cb, staging);
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
            Debug.Assert(texture.MipLevels > 1);
            ensureBlitEncoder();
            var mtlTex = Util.AssertSubtype<Texture, MtlTexture>(texture);
            _bce.generateMipmapsForTexture(mtlTex.DeviceTexture);
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            _indexBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
            _ibOffset = offset;
            _indexType = MtlFormats.VdToMtlIndexFormat(format);
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            Util.EnsureArrayMinimumSize(ref _vertexBuffers, index + 1);
            Util.EnsureArrayMinimumSize(ref _vbOffsets, index + 1);
            Util.EnsureArrayMinimumSize(ref _vertexBuffersActive, index + 1);
            Util.EnsureArrayMinimumSize(ref _vbOffsetsActive, index + 1);

            if (_vertexBuffers[index] != buffer)
            {
                var mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
                _vertexBuffers[index] = mtlBuffer;
                _vertexBuffersActive[index] = false;
            }

            if (_vbOffsets[index] != offset)
            {
                _vbOffsets[index] = offset;
                _vbOffsetsActive[index] = false;
            }
        }

        private protected override void PushDebugGroupCore(string name)
        {
            var nsName = NSString.New(name);
            if (!_bce.IsNull)
                _bce.pushDebugGroup(nsName);
            else if (!_cce.IsNull)
                _cce.pushDebugGroup(nsName);
            else if (!_rce.IsNull) _rce.pushDebugGroup(nsName);

            ObjectiveCRuntime.release(nsName);
        }

        private protected override void PopDebugGroupCore()
        {
            if (!_bce.IsNull)
                _bce.popDebugGroup();
            else if (!_cce.IsNull)
                _cce.popDebugGroup();
            else if (!_rce.IsNull) _rce.popDebugGroup();
        }

        private protected override void InsertDebugMarkerCore(string name)
        {
            var nsName = NSString.New(name);
            if (!_bce.IsNull)
                _bce.insertDebugSignpost(nsName);
            else if (!_cce.IsNull)
                _cce.insertDebugSignpost(nsName);
            else if (!_rce.IsNull) _rce.insertDebugSignpost(nsName);

            ObjectiveCRuntime.release(nsName);
        }
    }
}
