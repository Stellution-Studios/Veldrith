using System;
using System.Collections.Generic;
using System.Diagnostics;
using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal unsafe class MtlCommandList : CommandList
    {
        public MTLCommandBuffer CommandBuffer => cb;

        public override bool IsDisposed => this._disposed;

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

        private bool renderEncoderActive => !this._rce.IsNull;
        private bool blitEncoderActive => !this._bce.IsNull;
        private bool computeEncoderActive => !this._cce.IsNull;
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
            if (!this._disposed)
            {
                this._disposed = true;
                EnsureNoRenderPass();

                lock (this._submittedStagingBuffers)
                {
                    foreach (var buffer in this._availableStagingBuffers)
                        buffer.Dispose();

                    foreach (var buffer in this._submittedStagingBuffers.EnumerateItems())
                        buffer.Dispose();

                    this._submittedStagingBuffers.Clear();
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
            PreComputeCommand();
            this._cce.dispatchThreadGroups(
                new MTLSize(groupCountX, groupCountY, groupCountZ),
                this._computePipeline.ThreadsPerThreadgroup);
        }

        public override void End()
        {
            EnsureNoBlitEncoder();
            EnsureNoComputeEncoder();

            if (!this._currentFramebufferEverActive && this._mtlFramebuffer != null) BeginCurrentRenderPass();
            EnsureNoRenderPass();
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            this._scissorRects[index] = new MTLScissorRect(x, y, width, height);
        }

        public override void SetViewport(uint index, ref Viewport viewport)
        {
            this._viewportsChanged = true;
            this._viewports[index] = new MTLViewport(
                viewport.X,
                viewport.Y,
                viewport.Width,
                viewport.Height,
                viewport.MinDepth,
                viewport.MaxDepth);
        }

        public void SetCompletionFence(MTLCommandBuffer cb, MtlFence fence)
        {
            lock (this._submittedCommandsLock)
            {
                Debug.Assert(!this._completionFences.Contains(cb));
                this._completionFences.Add(cb, fence);
            }
        }

        public void OnCompleted(MTLCommandBuffer cb)
        {
            lock (this._submittedCommandsLock)
            {
                foreach (var fence in this._completionFences.EnumerateAndRemove(cb))
                    fence.Set();

                foreach (var buffer in this._submittedStagingBuffers.EnumerateAndRemove(cb))
                    this._availableStagingBuffers.Add(buffer);
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
                EnsureComputeEncoder();
                this._cce.setComputePipelineState(gd.GetUnalignedBufferCopyPipeline());
                this._cce.setBuffer(mtlSrc.DeviceBuffer, UIntPtr.Zero, 0);
                this._cce.setBuffer(mtlDst.DeviceBuffer, UIntPtr.Zero, 1);

                MtlUnalignedBufferCopyInfo copyInfo;
                copyInfo.SourceOffset = sourceOffset;
                copyInfo.DestinationOffset = destinationOffset;
                copyInfo.CopySize = sizeInBytes;

                this._cce.setBytes(&copyInfo, (UIntPtr)sizeof(MtlUnalignedBufferCopyInfo), 2);
                this._cce.dispatchThreadGroups(new MTLSize(1, 1, 1), new MTLSize(1, 1, 1));
            }
            else
            {
                EnsureBlitEncoder();
                this._bce.copy(
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
            EnsureBlitEncoder();
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
                    this._bce.copyFromBuffer(
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
                                this._bce.copy(
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
                                this._bce.copy(
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

                    this._bce.copyTextureToBuffer(
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
                    this._bce.copyFromTexture(
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
            PreComputeCommand();
            this._cce.dispatchThreadgroupsWithIndirectBuffer(
                mtlBuffer.DeviceBuffer,
                offset,
                this._computePipeline.ThreadsPerThreadgroup);
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            if (PreDrawCommand())
            {
                var mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(indirectBuffer);

                for (uint i = 0; i < drawCount; i++)
                {
                    uint currentOffset = i * stride + offset;
                    this._rce.drawIndexedPrimitives(
                        this._graphicsPipeline.PrimitiveType,
                        this._indexType,
                        this._indexBuffer.DeviceBuffer,
                        this._ibOffset,
                        mtlBuffer.DeviceBuffer,
                        currentOffset);
                }
            }
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            if (PreDrawCommand())
            {
                var mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(indirectBuffer);

                for (uint i = 0; i < drawCount; i++)
                {
                    uint currentOffset = i * stride + offset;
                    this._rce.drawPrimitives(this._graphicsPipeline.PrimitiveType, mtlBuffer.DeviceBuffer, currentOffset);
                }
            }
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            // TODO: This approach destroys the contents of the source Texture (according to the docs).
            EnsureNoBlitEncoder();
            EnsureNoRenderPass();

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
            if (!this._computeResourceSets[slot].Equals(set, dynamicOffsetCount, ref dynamicOffsets))
            {
                this._computeResourceSets[slot].Offsets.Dispose();
                this._computeResourceSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetCount, ref dynamicOffsets);
                this._computeResourceSetsActive[slot] = false;
            }
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            if (!this._currentFramebufferEverActive && this._mtlFramebuffer != null)
            {
                // This ensures that any submitted clear values will be used even if nothing has been drawn.
                if (EnsureRenderPass()) EndCurrentRenderPass();
            }

            EnsureNoRenderPass();
            this._mtlFramebuffer = Util.AssertSubtype<Framebuffer, MtlFramebuffer>(fb);
            this._viewportCount = Math.Max(1u, (uint)fb.ColorTargets.Count);
            Util.EnsureArrayMinimumSize(ref this._viewports, this._viewportCount);
            Util.ClearArray(this._viewports);
            Util.EnsureArrayMinimumSize(ref this._scissorRects, this._viewportCount);
            Util.ClearArray(this._scissorRects);
            Util.EnsureArrayMinimumSize(ref this._activeScissorRects, this._viewportCount);
            Util.ClearArray(this._activeScissorRects);
            Util.EnsureArrayMinimumSize(ref this._clearColors, (uint)fb.ColorTargets.Count);
            Util.ClearArray(this._clearColors);
            this._currentFramebufferEverActive = false;
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetCount, ref uint dynamicOffsets)
        {
            if (!this._graphicsResourceSets[slot].Equals(rs, dynamicOffsetCount, ref dynamicOffsets))
            {
                this._graphicsResourceSets[slot].Offsets.Dispose();
                this._graphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetCount, ref dynamicOffsets);
                this._graphicsResourceSetsActive[slot] = false;
            }
        }

        private bool PreDrawCommand()
        {
            if (EnsureRenderPass())
            {
                if (this._viewportsChanged)
                {
                    FlushViewports();
                    this._viewportsChanged = false;
                }

                if (this._graphicsPipeline.ScissorTestEnabled)
                    FlushScissorRects();

                Debug.Assert(this._graphicsPipeline != null);

                if (this._graphicsPipeline.RenderPipelineState.NativePtr != this._lastGraphicsPipeline?.RenderPipelineState.NativePtr)
                    this._rce.setRenderPipelineState(this._graphicsPipeline.RenderPipelineState);

                if (this._graphicsPipeline.CullMode != this._lastGraphicsPipeline?.CullMode)
                    this._rce.setCullMode(this._graphicsPipeline.CullMode);

                if (this._graphicsPipeline.FrontFace != this._lastGraphicsPipeline?.FrontFace)
                    this._rce.setFrontFacing(this._graphicsPipeline.FrontFace);

                if (this._graphicsPipeline.FillMode != this._lastGraphicsPipeline?.FillMode)
                    this._rce.setTriangleFillMode(this._graphicsPipeline.FillMode);

                var blendColor = this._graphicsPipeline.BlendColor;
                if (blendColor != this._lastGraphicsPipeline?.BlendColor)
                    this._rce.setBlendColor(blendColor.R, blendColor.G, blendColor.B, blendColor.A);

                if (Framebuffer.DepthTarget != null)
                {
                    if (this._graphicsPipeline.DepthStencilState.NativePtr != this._lastGraphicsPipeline?.DepthStencilState.NativePtr)
                        this._rce.setDepthStencilState(this._graphicsPipeline.DepthStencilState);

                    if (this._graphicsPipeline.DepthClipMode != this._lastGraphicsPipeline?.DepthClipMode)
                        this._rce.setDepthClipMode(this._graphicsPipeline.DepthClipMode);

                    if (this._graphicsPipeline.StencilReference != this._lastGraphicsPipeline?.StencilReference)
                        this._rce.setStencilReferenceValue(this._graphicsPipeline.StencilReference);
                }

                this._lastGraphicsPipeline = this._graphicsPipeline;

                for (uint i = 0; i < this._graphicsResourceSetCount; i++)
                {
                    if (!this._graphicsResourceSetsActive[i])
                    {
                        ActivateGraphicsResourceSet(i, this._graphicsResourceSets[i]);
                        this._graphicsResourceSetsActive[i] = true;
                    }
                }

                for (uint i = 0; i < this._vertexBufferCount; i++)
                {
                    if (!this._vertexBuffersActive[i])
                    {
                        UIntPtr index = this._graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                            ? this._nonVertexBufferCount + i
                            : i;
                        this._rce.setVertexBuffer(
                            this._vertexBuffers[i].DeviceBuffer,
                            this._vbOffsets[i],
                            index);

                        this._vertexBuffersActive[i] = true;
                        this._vbOffsetsActive[i] = true;
                    }

                    if (!this._vbOffsetsActive[i])
                    {
                        UIntPtr index = this._graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                            ? this._nonVertexBufferCount + i
                            : i;

                        this._rce.setVertexBufferOffset(
                            this._vbOffsets[i],
                            index);

                        this._vbOffsetsActive[i] = true;
                    }
                }

                return true;
            }

            return false;
        }

        private void FlushViewports()
        {
            if (gd.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3))
            {
                fixed (MTLViewport* viewportsPtr = &this._viewports[0])
                    this._rce.setViewports(viewportsPtr, this._viewportCount);
            }
            else
                this._rce.setViewport(this._viewports[0]);
        }

        private void FlushScissorRects()
        {
            if (gd.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3))
            {
                bool scissorRectsChanged = false;

                for (int i = 0; i < this._scissorRects.Length; i++)
                {
                    scissorRectsChanged |= !this._scissorRects[i].Equals(this._activeScissorRects[i]);
                    this._activeScissorRects[i] = this._scissorRects[i];
                }

                if (scissorRectsChanged)
                {
                    fixed (MTLScissorRect* scissorRectsPtr = this._scissorRects)
                        this._rce.setScissorRects(scissorRectsPtr, this._viewportCount);
                }
            }
            else
            {
                if (!this._scissorRects[0].Equals(this._activeScissorRects[0]))
                    this._rce.setScissorRect(this._scissorRects[0]);

                this._activeScissorRects[0] = this._scissorRects[0];
            }
        }

        private void PreComputeCommand()
        {
            EnsureComputeEncoder();

            if (this._computePipeline.ComputePipelineState.NativePtr != this._lastComputePipeline?.ComputePipelineState.NativePtr)
                this._cce.setComputePipelineState(this._computePipeline.ComputePipelineState);

            this._lastComputePipeline = this._computePipeline;

            for (uint i = 0; i < this._computeResourceSetCount; i++)
            {
                if (!this._computeResourceSetsActive[i])
                {
                    ActivateComputeResourceSet(i, this._computeResourceSets[i]);
                    this._computeResourceSetsActive[i] = true;
                }
            }
        }

        private MtlBuffer GetFreeStagingBuffer(uint sizeInBytes)
        {
            lock (this._submittedCommandsLock)
            {
                foreach (var buffer in this._availableStagingBuffers)
                {
                    if (buffer.SizeInBytes >= sizeInBytes)
                    {
                        this._availableStagingBuffers.Remove(buffer);
                        return buffer;
                    }
                }
            }

            var staging = gd.ResourceFactory.CreateBuffer(
                new BufferDescription(sizeInBytes, BufferUsage.Staging));

            return Util.AssertSubtype<DeviceBuffer, MtlBuffer>(staging);
        }

        private void ActivateGraphicsResourceSet(uint slot, BoundResourceSetInfo brsi)
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
                        BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    case ResourceKind.TextureReadOnly:
                        var texView = Util.GetTextureView(gd, resource);
                        var mtlTexView = Util.AssertSubtype<TextureView, MtlTextureView>(texView);
                        BindTexture(mtlTexView, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.TextureReadWrite:
                        var texViewRw = Util.GetTextureView(gd, resource);
                        var mtlTexViewRw = Util.AssertSubtype<TextureView, MtlTextureView>(texViewRw);
                        BindTexture(mtlTexViewRw, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.Sampler:
                        var mtlSampler = Util.AssertSubtype<IBindableResource, MtlSampler>(resource);
                        BindSampler(mtlSampler, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.StructuredBufferReadOnly:
                    {
                        var range = Util.GetBufferRange(resource, bufferOffset);
                        BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    case ResourceKind.StructuredBufferReadWrite:
                    {
                        var range = Util.GetBufferRange(resource, bufferOffset);
                        BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    default:
                        throw Illegal.Value<ResourceKind>();
                }
            }
        }

        private void ActivateComputeResourceSet(uint slot, BoundResourceSetInfo brsi)
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
                        BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    case ResourceKind.TextureReadOnly:
                        var texView = Util.GetTextureView(gd, resource);
                        var mtlTexView = Util.AssertSubtype<TextureView, MtlTextureView>(texView);
                        BindTexture(mtlTexView, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.TextureReadWrite:
                        var texViewRw = Util.GetTextureView(gd, resource);
                        var mtlTexViewRw = Util.AssertSubtype<TextureView, MtlTextureView>(texViewRw);
                        BindTexture(mtlTexViewRw, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.Sampler:
                        var mtlSampler = Util.AssertSubtype<IBindableResource, MtlSampler>(resource);
                        BindSampler(mtlSampler, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.StructuredBufferReadOnly:
                    {
                        var range = Util.GetBufferRange(resource, bufferOffset);
                        BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    case ResourceKind.StructuredBufferReadWrite:
                    {
                        var range = Util.GetBufferRange(resource, bufferOffset);
                        BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                    default:
                        throw Illegal.Value<ResourceKind>();
                }
            }
        }

        private void BindBuffer(DeviceBufferRange range, uint set, uint slot, ShaderStages stages)
        {
            var mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(range.Buffer);
            uint baseBuffer = GetBufferBase(set, stages != ShaderStages.Compute);

            if (stages == ShaderStages.Compute)
            {
                UIntPtr index = slot + baseBuffer;

                if (!this._boundComputeBuffers.TryGetValue(index, out var boundBuffer) || !range.Equals(boundBuffer))
                {
                    this._cce.setBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);
                    this._boundComputeBuffers[index] = range;
                }
            }
            else
            {
                if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
                {
                    UIntPtr index = this._graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                        ? slot + baseBuffer
                        : slot + this._vertexBufferCount + baseBuffer;

                    if (!this._boundVertexBuffers.TryGetValue(index, out var boundBuffer) || boundBuffer.Buffer != range.Buffer)
                    {
                        this._rce.setVertexBuffer(mtlBuffer.DeviceBuffer, range.Offset, index);
                        this._boundVertexBuffers[index] = range;
                    }
                    else if (!range.Equals(boundBuffer))
                    {
                        this._rce.setVertexBufferOffset(range.Offset, index);
                        this._boundVertexBuffers[index] = range;
                    }
                }

                if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
                {
                    UIntPtr index = slot + baseBuffer;

                    if (!this._boundFragmentBuffers.TryGetValue(index, out var boundBuffer) || boundBuffer.Buffer != range.Buffer)
                    {
                        this._rce.setFragmentBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);
                        this._boundFragmentBuffers[index] = range;
                    }
                    else if (!range.Equals(boundBuffer))
                    {
                        this._rce.setFragmentBufferOffset(range.Offset, slot + baseBuffer);
                        this._boundFragmentBuffers[index] = range;
                    }
                }
            }
        }

        private void BindTexture(MtlTextureView mtlTexView, uint set, uint slot, ShaderStages stages)
        {
            uint baseTexture = GetTextureBase(set, stages != ShaderStages.Compute);
            UIntPtr index = slot + baseTexture;

            if (stages == ShaderStages.Compute && (!this._boundComputeTextures.TryGetValue(index, out var computeTexture) || computeTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr))
            {
                this._cce.setTexture(mtlTexView.TargetDeviceTexture, index);
                this._boundComputeTextures[index] = mtlTexView.TargetDeviceTexture;
            }

            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex
                && (!this._boundVertexTextures.TryGetValue(index, out var vertexTexture) || vertexTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr))
            {
                this._rce.setVertexTexture(mtlTexView.TargetDeviceTexture, index);
                this._boundVertexTextures[index] = mtlTexView.TargetDeviceTexture;
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment
                && (!this._boundFragmentTextures.TryGetValue(index, out var fragmentTexture) || fragmentTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr))
            {
                this._rce.setFragmentTexture(mtlTexView.TargetDeviceTexture, index);
                this._boundFragmentTextures[index] = mtlTexView.TargetDeviceTexture;
            }
        }

        private void BindSampler(MtlSampler mtlSampler, uint set, uint slot, ShaderStages stages)
        {
            uint baseSampler = GetSamplerBase(set, stages != ShaderStages.Compute);
            UIntPtr index = slot + baseSampler;

            if (stages == ShaderStages.Compute && (!this._boundComputeSamplers.TryGetValue(index, out var computeSampler) || computeSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr))
            {
                this._cce.setSamplerState(mtlSampler.DeviceSampler, index);
                this._boundComputeSamplers[index] = mtlSampler.DeviceSampler;
            }

            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex
                && (!this._boundVertexSamplers.TryGetValue(index, out var vertexSampler) || vertexSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr))
            {
                this._rce.setVertexSamplerState(mtlSampler.DeviceSampler, index);
                this._boundVertexSamplers[index] = mtlSampler.DeviceSampler;
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment
                && (!this._boundFragmentSamplers.TryGetValue(index, out var fragmentSampler) || fragmentSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr))
            {
                this._rce.setFragmentSamplerState(mtlSampler.DeviceSampler, index);
                this._boundFragmentSamplers[index] = mtlSampler.DeviceSampler;
            }
        }

        private uint GetBufferBase(uint set, bool graphics)
        {
            var layouts = graphics ? this._graphicsPipeline.ResourceLayouts : this._computePipeline.ResourceLayouts;
            uint ret = 0;

            for (int i = 0; i < set; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].BufferCount;
            }

            return ret;
        }

        private uint GetTextureBase(uint set, bool graphics)
        {
            var layouts = graphics ? this._graphicsPipeline.ResourceLayouts : this._computePipeline.ResourceLayouts;
            uint ret = 0;

            for (int i = 0; i < set; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].TextureCount;
            }

            return ret;
        }

        private uint GetSamplerBase(uint set, bool graphics)
        {
            var layouts = graphics ? this._graphicsPipeline.ResourceLayouts : this._computePipeline.ResourceLayouts;
            uint ret = 0;

            for (int i = 0; i < set; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].SamplerCount;
            }

            return ret;
        }

        private bool EnsureRenderPass()
        {
            Debug.Assert(this._mtlFramebuffer != null);
            EnsureNoBlitEncoder();
            EnsureNoComputeEncoder();
            return renderEncoderActive || BeginCurrentRenderPass();
        }

        private bool BeginCurrentRenderPass()
        {
            if (this._mtlFramebuffer is MtlSwapchainFramebuffer swapchainFramebuffer && !swapchainFramebuffer.EnsureDrawableAvailable())
                return false;

            var rpDesc = this._mtlFramebuffer.CreateRenderPassDescriptor();

            for (uint i = 0; i < this._clearColors.Length; i++)
            {
                if (this._clearColors[i] != null)
                {
                    var attachment = rpDesc.colorAttachments[0];
                    attachment.loadAction = MTLLoadAction.Clear;
                    var c = this._clearColors[i].Value;
                    attachment.clearColor = new MTLClearColor(c.R, c.G, c.B, c.A);
                    this._clearColors[i] = null;
                }
            }

            if (this._clearDepth != null)
            {
                var depthAttachment = rpDesc.depthAttachment;
                depthAttachment.loadAction = MTLLoadAction.Clear;
                depthAttachment.clearDepth = this._clearDepth.Value.depth;

                if (this._mtlFramebuffer.DepthTarget != null && FormatHelpers.IsStencilFormat(this._mtlFramebuffer.DepthTarget.Value.Target.Format))
                {
                    var stencilAttachment = rpDesc.stencilAttachment;
                    stencilAttachment.loadAction = MTLLoadAction.Clear;
                    stencilAttachment.clearStencil = this._clearDepth.Value.stencil;
                }

                this._clearDepth = null;
            }

            using (NSAutoreleasePool.Begin())
            {
                this._rce = cb.renderCommandEncoderWithDescriptor(rpDesc);
                ObjectiveCRuntime.retain(this._rce.NativePtr);
            }

            ObjectiveCRuntime.release(rpDesc.NativePtr);
            this._currentFramebufferEverActive = true;

            return true;
        }

        private void EnsureNoRenderPass()
        {
            if (renderEncoderActive) EndCurrentRenderPass();

            Debug.Assert(!renderEncoderActive);
        }

        private void EndCurrentRenderPass()
        {
            this._rce.endEncoding();
            ObjectiveCRuntime.release(this._rce.NativePtr);
            this._rce = default;

            this._lastGraphicsPipeline = null;
            this._boundVertexBuffers.Clear();
            this._boundVertexTextures.Clear();
            this._boundVertexSamplers.Clear();
            this._boundFragmentBuffers.Clear();
            this._boundFragmentTextures.Clear();
            this._boundFragmentSamplers.Clear();
            Util.ClearArray(this._graphicsResourceSetsActive);
            Util.ClearArray(this._vertexBuffersActive);
            Util.ClearArray(this._vbOffsetsActive);

            Util.ClearArray(this._activeScissorRects);

            this._viewportsChanged = true;
        }

        private void EnsureBlitEncoder()
        {
            if (!blitEncoderActive)
            {
                EnsureNoRenderPass();
                EnsureNoComputeEncoder();

                using (NSAutoreleasePool.Begin())
                {
                    this._bce = cb.blitCommandEncoder();
                    ObjectiveCRuntime.retain(this._bce.NativePtr);
                }
            }

            Debug.Assert(blitEncoderActive);
            Debug.Assert(!renderEncoderActive);
            Debug.Assert(!computeEncoderActive);
        }

        private void EnsureNoBlitEncoder()
        {
            if (blitEncoderActive)
            {
                this._bce.endEncoding();
                ObjectiveCRuntime.release(this._bce.NativePtr);
                this._bce = default;
            }

            Debug.Assert(!blitEncoderActive);
        }

        private void EnsureComputeEncoder()
        {
            if (!computeEncoderActive)
            {
                EnsureNoBlitEncoder();
                EnsureNoRenderPass();

                using (NSAutoreleasePool.Begin())
                {
                    this._cce = cb.computeCommandEncoder();
                    ObjectiveCRuntime.retain(this._cce.NativePtr);
                }
            }

            Debug.Assert(computeEncoderActive);
            Debug.Assert(!renderEncoderActive);
            Debug.Assert(!blitEncoderActive);
        }

        private void EnsureNoComputeEncoder()
        {
            if (computeEncoderActive)
            {
                this._cce.endEncoding();
                ObjectiveCRuntime.release(this._cce.NativePtr);
                this._cce = default;

                this._boundComputeBuffers.Clear();
                this._boundComputeTextures.Clear();
                this._boundComputeSamplers.Clear();
                this._lastComputePipeline = null;

                Util.ClearArray(this._computeResourceSetsActive);
            }

            Debug.Assert(!computeEncoderActive);
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            EnsureNoRenderPass();
            this._clearColors[index] = clearColor;
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            EnsureNoRenderPass();
            this._clearDepth = (depth, stencil);
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            if (PreDrawCommand())
            {
                if (instanceStart == 0)
                {
                    this._rce.drawPrimitives(
                        this._graphicsPipeline.PrimitiveType,
                        vertexStart,
                        vertexCount,
                        instanceCount);
                }
                else
                {
                    this._rce.drawPrimitives(
                        this._graphicsPipeline.PrimitiveType,
                        vertexStart,
                        vertexCount,
                        instanceCount,
                        instanceStart);
                }
            }
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            if (PreDrawCommand())
            {
                uint indexSize = this._indexType == MTLIndexType.UInt16 ? 2u : 4u;
                uint indexBufferOffset = indexSize * indexStart + this._ibOffset;

                if (vertexOffset == 0 && instanceStart == 0)
                {
                    this._rce.drawIndexedPrimitives(
                        this._graphicsPipeline.PrimitiveType,
                        indexCount,
                        this._indexType,
                        this._indexBuffer.DeviceBuffer,
                        indexBufferOffset,
                        instanceCount);
                }
                else
                {
                    this._rce.drawIndexedPrimitives(
                        this._graphicsPipeline.PrimitiveType,
                        indexCount,
                        this._indexType,
                        this._indexBuffer.DeviceBuffer,
                        indexBufferOffset,
                        instanceCount,
                        vertexOffset,
                        instanceStart);
                }
            }
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            if (pipeline.IsComputePipeline && this._computePipeline != pipeline)
            {
                this._computePipeline = Util.AssertSubtype<Pipeline, MtlPipeline>(pipeline);
                this._computeResourceSetCount = (uint)this._computePipeline.ResourceLayouts.Length;
                Util.EnsureArrayMinimumSize(ref this._computeResourceSets, this._computeResourceSetCount);
                Util.EnsureArrayMinimumSize(ref this._computeResourceSetsActive, this._computeResourceSetCount);
                Util.ClearArray(this._computeResourceSetsActive);
            }
            else if (!pipeline.IsComputePipeline && this._graphicsPipeline != pipeline)
            {
                this._graphicsPipeline = Util.AssertSubtype<Pipeline, MtlPipeline>(pipeline);
                this._graphicsResourceSetCount = (uint)this._graphicsPipeline.ResourceLayouts.Length;
                Util.EnsureArrayMinimumSize(ref this._graphicsResourceSets, this._graphicsResourceSetCount);
                Util.EnsureArrayMinimumSize(ref this._graphicsResourceSetsActive, this._graphicsResourceSetCount);
                Util.ClearArray(this._graphicsResourceSetsActive);

                this._nonVertexBufferCount = this._graphicsPipeline.NonVertexBufferCount;

                this._vertexBufferCount = this._graphicsPipeline.VertexBufferCount;
                Util.EnsureArrayMinimumSize(ref this._vertexBuffers, this._vertexBufferCount);
                Util.EnsureArrayMinimumSize(ref this._vbOffsets, this._vertexBufferCount);
                Util.EnsureArrayMinimumSize(ref this._vertexBuffersActive, this._vertexBufferCount);
                Util.EnsureArrayMinimumSize(ref this._vbOffsetsActive, this._vertexBufferCount);
                Util.ClearArray(this._vertexBuffersActive);
                Util.ClearArray(this._vbOffsetsActive);
            }
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            bool useComputeCopy = bufferOffsetInBytes % 4 != 0
                                  || (sizeInBytes % 4 != 0 && bufferOffsetInBytes != 0 && sizeInBytes != buffer.SizeInBytes);

            var dstMtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
            var staging = GetFreeStagingBuffer(sizeInBytes);

            gd.UpdateBuffer(staging, 0, source, sizeInBytes);

            if (useComputeCopy)
                CopyBufferCore(staging, 0, buffer, bufferOffsetInBytes, sizeInBytes);
            else
            {
                Debug.Assert(bufferOffsetInBytes % 4 == 0);
                uint sizeRoundFactor = (4 - sizeInBytes % 4) % 4;
                EnsureBlitEncoder();
                this._bce.copy(
                    staging.DeviceBuffer, UIntPtr.Zero,
                    dstMtlBuffer.DeviceBuffer, bufferOffsetInBytes,
                    sizeInBytes + sizeRoundFactor);
            }

            lock (this._submittedCommandsLock)
                this._submittedStagingBuffers.Add(cb, staging);
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
            Debug.Assert(texture.MipLevels > 1);
            EnsureBlitEncoder();
            var mtlTex = Util.AssertSubtype<Texture, MtlTexture>(texture);
            this._bce.generateMipmapsForTexture(mtlTex.DeviceTexture);
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            this._indexBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
            this._ibOffset = offset;
            this._indexType = MtlFormats.VdToMtlIndexFormat(format);
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            Util.EnsureArrayMinimumSize(ref this._vertexBuffers, index + 1);
            Util.EnsureArrayMinimumSize(ref this._vbOffsets, index + 1);
            Util.EnsureArrayMinimumSize(ref this._vertexBuffersActive, index + 1);
            Util.EnsureArrayMinimumSize(ref this._vbOffsetsActive, index + 1);

            if (this._vertexBuffers[index] != buffer)
            {
                var mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
                this._vertexBuffers[index] = mtlBuffer;
                this._vertexBuffersActive[index] = false;
            }

            if (this._vbOffsets[index] != offset)
            {
                this._vbOffsets[index] = offset;
                this._vbOffsetsActive[index] = false;
            }
        }

        private protected override void PushDebugGroupCore(string name)
        {
            var nsName = NSString.New(name);
            if (!this._bce.IsNull)
                this._bce.pushDebugGroup(nsName);
            else if (!this._cce.IsNull)
                this._cce.pushDebugGroup(nsName);
            else if (!this._rce.IsNull) this._rce.pushDebugGroup(nsName);

            ObjectiveCRuntime.release(nsName);
        }

        private protected override void PopDebugGroupCore()
        {
            if (!this._bce.IsNull)
                this._bce.popDebugGroup();
            else if (!this._cce.IsNull)
                this._cce.popDebugGroup();
            else if (!this._rce.IsNull) this._rce.popDebugGroup();
        }

        private protected override void InsertDebugMarkerCore(string name)
        {
            var nsName = NSString.New(name);
            if (!this._bce.IsNull)
                this._bce.insertDebugSignpost(nsName);
            else if (!this._cce.IsNull)
                this._cce.insertDebugSignpost(nsName);
            else if (!this._rce.IsNull) this._rce.insertDebugSignpost(nsName);

            ObjectiveCRuntime.release(nsName);
        }
    }
}
