using System;
using System.Collections.Generic;
using System.Diagnostics;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Defines the behavior and responsibilities of the MtlCommandList class.
/// </summary>
internal unsafe class MtlCommandList : CommandList {

    /// <summary>
    /// Stores the value associated with <c>_availableStagingBuffers</c>.
    /// </summary>
    private readonly List<MtlBuffer> _availableStagingBuffers = new();

    /// <summary>
    /// Stores the value associated with <c>_boundComputeBuffers</c>.
    /// </summary>
    private readonly Dictionary<UIntPtr, DeviceBufferRange> _boundComputeBuffers = new();

    /// <summary>
    /// Stores the value associated with <c>_boundComputeSamplers</c>.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLSamplerState> _boundComputeSamplers = new();

    /// <summary>
    /// Stores the value associated with <c>_boundComputeTextures</c>.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLTexture> _boundComputeTextures = new();

    /// <summary>
    /// Stores the value associated with <c>_boundFragmentBuffers</c>.
    /// </summary>
    private readonly Dictionary<UIntPtr, DeviceBufferRange> _boundFragmentBuffers = new();

    /// <summary>
    /// Stores the value associated with <c>_boundFragmentSamplers</c>.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLSamplerState> _boundFragmentSamplers = new();

    /// <summary>
    /// Stores the value associated with <c>_boundFragmentTextures</c>.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLTexture> _boundFragmentTextures = new();

    /// <summary>
    /// Stores the value associated with <c>_boundVertexBuffers</c>.
    /// </summary>
    private readonly Dictionary<UIntPtr, DeviceBufferRange> _boundVertexBuffers = new();

    /// <summary>
    /// Stores the value associated with <c>_boundVertexSamplers</c>.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLSamplerState> _boundVertexSamplers = new();

    /// <summary>
    /// Stores the value associated with <c>_boundVertexTextures</c>.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLTexture> _boundVertexTextures = new();

    /// <summary>
    /// Stores the value associated with <c>_completionFences</c>.
    /// </summary>
    private readonly CommandBufferUsageList<MtlFence> _completionFences = new();

    /// <summary>
    /// Stores the value associated with <c>_submittedCommandsLock</c>.
    /// </summary>
    private readonly object _submittedCommandsLock = new();

    /// <summary>
    /// Stores the value associated with <c>_submittedStagingBuffers</c>.
    /// </summary>
    private readonly CommandBufferUsageList<MtlBuffer> _submittedStagingBuffers = new();

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_activeScissorRects</c>.
    /// </summary>
    private MTLScissorRect[] _activeScissorRects = Array.Empty<MTLScissorRect>();

    /// <summary>
    /// Stores the value associated with <c>_bce</c>.
    /// </summary>
    private MTLBlitCommandEncoder _bce;

    /// <summary>
    /// Stores the value associated with <c>_cce</c>.
    /// </summary>
    private MTLComputeCommandEncoder _cce;

    /// <summary>
    /// Stores the value associated with <c>_clearColors</c>.
    /// </summary>
    private RgbaFloat?[] _clearColors = Array.Empty<RgbaFloat?>();

    /// <summary>
    /// Stores the value associated with <c>_clearDepth</c>.
    /// </summary>
    private (float depth, byte stencil)? _clearDepth;

    /// <summary>
    /// Stores the value associated with <c>_computePipeline</c>.
    /// </summary>
    private MtlPipeline _computePipeline;

    /// <summary>
    /// Stores the value associated with <c>_computeResourceSetCount</c>.
    /// </summary>
    private uint _computeResourceSetCount;

    /// <summary>
    /// Stores the value associated with <c>_computeResourceSets</c>.
    /// </summary>
    private BoundResourceSetInfo[] _computeResourceSets;

    /// <summary>
    /// Stores the value associated with <c>_computeResourceSetsActive</c>.
    /// </summary>
    private bool[] _computeResourceSetsActive;

    /// <summary>
    /// Stores the value associated with <c>_currentFramebufferEverActive</c>.
    /// </summary>
    private bool _currentFramebufferEverActive;

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the value associated with <c>_graphicsPipeline</c>.
    /// </summary>
    private MtlPipeline _graphicsPipeline;

    /// <summary>
    /// Stores the value associated with <c>_graphicsResourceSetCount</c>.
    /// </summary>
    private uint _graphicsResourceSetCount;

    /// <summary>
    /// Stores the value associated with <c>_graphicsResourceSets</c>.
    /// </summary>
    private BoundResourceSetInfo[] _graphicsResourceSets;

    /// <summary>
    /// Stores the value associated with <c>_graphicsResourceSetsActive</c>.
    /// </summary>
    private bool[] _graphicsResourceSetsActive;

    /// <summary>
    /// Stores the value associated with <c>_ibOffset</c>.
    /// </summary>
    private uint _ibOffset;

    /// <summary>
    /// Stores the value associated with <c>_indexBuffer</c>.
    /// </summary>
    private MtlBuffer _indexBuffer;

    /// <summary>
    /// Stores the value associated with <c>_indexType</c>.
    /// </summary>
    private MTLIndexType _indexType;

    /// <summary>
    /// Stores the value associated with <c>_lastComputePipeline</c>.
    /// </summary>
    private MtlPipeline _lastComputePipeline;

    /// <summary>
    /// Stores the value associated with <c>_lastGraphicsPipeline</c>.
    /// </summary>
    private MtlPipeline _lastGraphicsPipeline;

    /// <summary>
    /// Stores the value associated with <c>_mtlFramebuffer</c>.
    /// </summary>
    private MtlFramebuffer _mtlFramebuffer;

    /// <summary>
    /// Stores the value associated with <c>_nonVertexBufferCount</c>.
    /// </summary>
    private uint _nonVertexBufferCount;

    /// <summary>
    /// Stores the value associated with <c>_rce</c>.
    /// </summary>
    private MTLRenderCommandEncoder _rce;

    /// <summary>
    /// Stores the value associated with <c>_scissorRects</c>.
    /// </summary>
    private MTLScissorRect[] _scissorRects = Array.Empty<MTLScissorRect>();

    /// <summary>
    /// Stores the value associated with <c>_vbOffsets</c>.
    /// </summary>
    private uint[] _vbOffsets;

    /// <summary>
    /// Stores the value associated with <c>_vbOffsetsActive</c>.
    /// </summary>
    private bool[] _vbOffsetsActive;

    /// <summary>
    /// Stores the value associated with <c>_vertexBufferCount</c>.
    /// </summary>
    private uint _vertexBufferCount;

    /// <summary>
    /// Stores the value associated with <c>_vertexBuffers</c>.
    /// </summary>
    private MtlBuffer[] _vertexBuffers;

    /// <summary>
    /// Stores the value associated with <c>_vertexBuffersActive</c>.
    /// </summary>
    private bool[] _vertexBuffersActive;

    /// <summary>
    /// Stores the value associated with <c>_viewportCount</c>.
    /// </summary>
    private uint _viewportCount;

    /// <summary>
    /// Stores the value associated with <c>_viewports</c>.
    /// </summary>
    private MTLViewport[] _viewports = Array.Empty<MTLViewport>();

    /// <summary>
    /// Stores the value associated with <c>_viewportsChanged</c>.
    /// </summary>
    private bool _viewportsChanged;

    /// <summary>
    /// Stores the value associated with <c>cb</c>.
    /// </summary>
    private MTLCommandBuffer cb;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlCommandList" /> class.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
    public MtlCommandList(ref CommandListDescription description, MtlGraphicsDevice gd) : base(ref description, gd.Features, gd.UniformBufferMinOffsetAlignment, gd.StructuredBufferMinOffsetAlignment) {
        this.gd = gd;
    }

    /// <summary>
    /// Stores the value associated with <c>CommandBuffer</c>.
    /// </summary>
    public MTLCommandBuffer CommandBuffer => this.cb;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Stores the value associated with <c>renderEncoderActive</c>.
    /// </summary>
    private bool renderEncoderActive => !this._rce.IsNull;

    /// <summary>
    /// Stores the value associated with <c>blitEncoderActive</c>.
    /// </summary>
    private bool blitEncoderActive => !this._bce.IsNull;

    /// <summary>
    /// Stores the value associated with <c>computeEncoderActive</c>.
    /// </summary>
    private bool computeEncoderActive => !this._cce.IsNull;

    #region Disposal

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            this.EnsureNoRenderPass();

            lock (this._submittedStagingBuffers) {
                foreach (MtlBuffer buffer in this._availableStagingBuffers) {
                    buffer.Dispose();
                }

                foreach (MtlBuffer buffer in this._submittedStagingBuffers.EnumerateItems()) {
                    buffer.Dispose();
                }

                this._submittedStagingBuffers.Clear();
            }

            if (this.cb.NativePtr != IntPtr.Zero) {
                ObjectiveCRuntime.release(this.cb.NativePtr);
            }
        }
    }

    #endregion

    /// <summary>
    /// Executes the Commit operation.
    /// </summary>
    /// <returns>Returns the result produced by the Commit operation.</returns>
    public MTLCommandBuffer Commit() {
        this.cb.commit();
        MTLCommandBuffer ret = this.cb;
        this.cb = default;
        return ret;
    }

    /// <summary>
    /// Executes the Begin operation.
    /// </summary>
    public override void Begin() {
        if (this.cb.NativePtr != IntPtr.Zero) {
            ObjectiveCRuntime.release(this.cb.NativePtr);
        }

        using (NSAutoreleasePool.Begin()) {
            this.cb = this.gd.CommandQueue.commandBuffer();
            ObjectiveCRuntime.retain(this.cb.NativePtr);
        }

        this.ClearCachedState();
    }

    /// <summary>
    /// Executes the Dispatch operation.
    /// </summary>
    /// <param name="groupCountX">Specifies the value of <paramref name="groupCountX" />.</param>
    /// <param name="groupCountY">Specifies the value of <paramref name="groupCountY" />.</param>
    /// <param name="groupCountZ">Specifies the value of <paramref name="groupCountZ" />.</param>
    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ) {
        this.PreComputeCommand();
        this._cce.dispatchThreadGroups(new MTLSize(groupCountX, groupCountY, groupCountZ), this._computePipeline.ThreadsPerThreadgroup);
    }

    /// <summary>
    /// Executes the End operation.
    /// </summary>
    public override void End() {
        this.EnsureNoBlitEncoder();
        this.EnsureNoComputeEncoder();

        if (!this._currentFramebufferEverActive && this._mtlFramebuffer != null) {
            this.BeginCurrentRenderPass();
        }

        this.EnsureNoRenderPass();
    }

    /// <summary>
    /// Executes the SetScissorRect operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height) {
        this._scissorRects[index] = new MTLScissorRect(x, y, width, height);
    }

    /// <summary>
    /// Executes the SetViewport operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="viewport">Specifies the value of <paramref name="viewport" />.</param>
    public override void SetViewport(uint index, ref Viewport viewport) {
        this._viewportsChanged = true;
        this._viewports[index] = new MTLViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
    }

    /// <summary>
    /// Executes the SetCompletionFence operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    /// <param name="fence">Specifies the value of <paramref name="fence" />.</param>
    public void SetCompletionFence(MTLCommandBuffer cb, MtlFence fence) {
        lock (this._submittedCommandsLock) {
            Debug.Assert(!this._completionFences.Contains(cb));
            this._completionFences.Add(cb, fence);
        }
    }

    /// <summary>
    /// Executes the OnCompleted operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    public void OnCompleted(MTLCommandBuffer cb) {
        lock (this._submittedCommandsLock) {
            foreach (MtlFence fence in this._completionFences.EnumerateAndRemove(cb)) {
                fence.Set();
            }

            foreach (MtlBuffer buffer in this._submittedStagingBuffers.EnumerateAndRemove(cb)) {
                this._availableStagingBuffers.Add(buffer);
            }
        }
    }

    /// <summary>
    /// Executes the CopyBufferCore operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sourceOffset">Specifies the value of <paramref name="sourceOffset" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    /// <param name="destinationOffset">Specifies the value of <paramref name="destinationOffset" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes) {
        MtlBuffer mtlSrc = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(source);
        MtlBuffer mtlDst = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(destination);

        if (sourceOffset % 4 != 0 || destinationOffset % 4 != 0 || sizeInBytes % 4 != 0) {
            // Unaligned copy -- use special compute shader.
            this.EnsureComputeEncoder();
            this._cce.setComputePipelineState(this.gd.GetUnalignedBufferCopyPipeline());
            this._cce.setBuffer(mtlSrc.DeviceBuffer, UIntPtr.Zero, 0);
            this._cce.setBuffer(mtlDst.DeviceBuffer, UIntPtr.Zero, 1);

            MtlUnalignedBufferCopyInfo copyInfo;
            copyInfo.SourceOffset = sourceOffset;
            copyInfo.DestinationOffset = destinationOffset;
            copyInfo.CopySize = sizeInBytes;

            this._cce.setBytes(&copyInfo, (UIntPtr)sizeof(MtlUnalignedBufferCopyInfo), 2);
            this._cce.dispatchThreadGroups(new MTLSize(1, 1, 1), new MTLSize(1, 1, 1));
        }
        else {
            this.EnsureBlitEncoder();
            this._bce.copy(mtlSrc.DeviceBuffer, sourceOffset, mtlDst.DeviceBuffer, destinationOffset, sizeInBytes);
        }
    }

    /// <summary>
    /// Executes the CopyTextureCore operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="srcX">Specifies the value of <paramref name="srcX" />.</param>
    /// <param name="srcY">Specifies the value of <paramref name="srcY" />.</param>
    /// <param name="srcZ">Specifies the value of <paramref name="srcZ" />.</param>
    /// <param name="srcMipLevel">Specifies the value of <paramref name="srcMipLevel" />.</param>
    /// <param name="srcBaseArrayLayer">Specifies the value of <paramref name="srcBaseArrayLayer" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    /// <param name="dstX">Specifies the value of <paramref name="dstX" />.</param>
    /// <param name="dstY">Specifies the value of <paramref name="dstY" />.</param>
    /// <param name="dstZ">Specifies the value of <paramref name="dstZ" />.</param>
    /// <param name="dstMipLevel">Specifies the value of <paramref name="dstMipLevel" />.</param>
    /// <param name="dstBaseArrayLayer">Specifies the value of <paramref name="dstBaseArrayLayer" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="layerCount">Specifies the value of <paramref name="layerCount" />.</param>
    protected override void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount) {
        this.EnsureBlitEncoder();
        MtlTexture srcMtlTexture = Util.AssertSubtype<Texture, MtlTexture>(source);
        MtlTexture dstMtlTexture = Util.AssertSubtype<Texture, MtlTexture>(destination);

        bool srcIsStaging = (source.Usage & TextureUsage.Staging) != 0;
        bool dstIsStaging = (destination.Usage & TextureUsage.Staging) != 0;

        if (srcIsStaging && !dstIsStaging) {
            // Staging -> Normal
            MTLBuffer srcBuffer = srcMtlTexture.StagingBuffer;
            MTLTexture dstTexture = dstMtlTexture.DeviceTexture;

            Util.GetMipDimensions(srcMtlTexture, srcMipLevel, out uint mipWidth, out uint mipHeight, out uint _);

            for (uint layer = 0; layer < layerCount; layer++) {
                uint blockSize = FormatHelpers.IsCompressedFormat(srcMtlTexture.Format) ? 4u : 1u;
                uint compressedSrcX = srcX / blockSize;
                uint compressedSrcY = srcY / blockSize;
                uint blockSizeInBytes = blockSize == 1
                    ? FormatSizeHelpers.GetSizeInBytes(srcMtlTexture.Format)
                    : FormatHelpers.GetBlockSizeInBytes(srcMtlTexture.Format);

                ulong srcSubresourceBase = Util.ComputeSubresourceOffset(srcMtlTexture, srcMipLevel, layer + srcBaseArrayLayer);
                srcMtlTexture.GetSubresourceLayout(srcMipLevel, srcBaseArrayLayer + layer, out uint srcRowPitch, out uint srcDepthPitch);
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

                MTLSize sourceSize = new(copyWidth, copyHeight, depth);
                if (dstMtlTexture.Type != TextureType.Texture3D) {
                    srcDepthPitch = 0;
                }

                this._bce.copyFromBuffer(srcBuffer, (UIntPtr)sourceOffset, srcRowPitch, srcDepthPitch, sourceSize, dstTexture, dstBaseArrayLayer + layer, dstMipLevel, new MTLOrigin(dstX, dstY, dstZ), this.gd.MetalFeatures.IsMacOS);
            }
        }
        else if (srcIsStaging) {
            for (uint layer = 0; layer < layerCount; layer++) {
                // Staging -> Staging
                ulong srcSubresourceBase = Util.ComputeSubresourceOffset(srcMtlTexture, srcMipLevel, layer + srcBaseArrayLayer);
                srcMtlTexture.GetSubresourceLayout(srcMipLevel, srcBaseArrayLayer + layer, out uint srcRowPitch, out uint srcDepthPitch);

                ulong dstSubresourceBase = Util.ComputeSubresourceOffset(dstMtlTexture, dstMipLevel, layer + dstBaseArrayLayer);
                dstMtlTexture.GetSubresourceLayout(dstMipLevel, dstBaseArrayLayer + layer, out uint dstRowPitch, out uint dstDepthPitch);

                uint blockSize = FormatHelpers.IsCompressedFormat(dstMtlTexture.Format) ? 4u : 1u;

                if (blockSize == 1) {
                    uint pixelSize = FormatSizeHelpers.GetSizeInBytes(dstMtlTexture.Format);
                    uint copySize = width * pixelSize;

                    for (uint zz = 0; zz < depth; zz++) {
                        for (uint yy = 0; yy < height; yy++) {
                            ulong srcRowOffset = srcSubresourceBase
                                                 + srcDepthPitch * (zz + srcZ)
                                                 + srcRowPitch * (yy + srcY)
                                                 + pixelSize * srcX;
                            ulong dstRowOffset = dstSubresourceBase
                                                 + dstDepthPitch * (zz + dstZ)
                                                 + dstRowPitch * (yy + dstY)
                                                 + pixelSize * dstX;
                            this._bce.copy(srcMtlTexture.StagingBuffer, (UIntPtr)srcRowOffset, dstMtlTexture.StagingBuffer, (UIntPtr)dstRowOffset, copySize);
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

                    for (uint zz = 0; zz < depth; zz++) {
                        for (uint row = 0; row < numRows; row++) {
                            ulong srcRowOffset = srcSubresourceBase
                                                 + srcDepthPitch * (zz + srcZ)
                                                 + srcRowPitch * (row + compressedSrcY)
                                                 + blockSizeInBytes * compressedSrcX;
                            ulong dstRowOffset = dstSubresourceBase
                                                 + dstDepthPitch * (zz + dstZ)
                                                 + dstRowPitch * (row + compressedDstY)
                                                 + blockSizeInBytes * compressedDstX;
                            this._bce.copy(srcMtlTexture.StagingBuffer, (UIntPtr)srcRowOffset, dstMtlTexture.StagingBuffer, (UIntPtr)dstRowOffset, rowPitch);
                        }
                    }
                }
            }
        }
        else if (dstIsStaging) {
            // Normal -> Staging
            MTLOrigin srcOrigin = new(srcX, srcY, srcZ);
            MTLSize srcSize = new(width, height, depth);

            for (uint layer = 0; layer < layerCount; layer++) {
                dstMtlTexture.GetSubresourceLayout(dstMipLevel, dstBaseArrayLayer + layer, out uint dstBytesPerRow, out uint dstBytesPerImage);

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

                this._bce.copyTextureToBuffer(srcMtlTexture.DeviceTexture, srcBaseArrayLayer + layer, srcMipLevel, srcOrigin, srcSize, dstMtlTexture.StagingBuffer, (UIntPtr)dstOffset, dstBytesPerRow, dstBytesPerImage);
            }
        }
        else {
            // Normal -> Normal
            for (uint layer = 0; layer < layerCount; layer++) {
                this._bce.copyFromTexture(srcMtlTexture.DeviceTexture, srcBaseArrayLayer + layer, srcMipLevel, new MTLOrigin(srcX, srcY, srcZ), new MTLSize(width, height, depth), dstMtlTexture.DeviceTexture, dstBaseArrayLayer + layer, dstMipLevel, new MTLOrigin(dstX, dstY, dstZ), this.gd.MetalFeatures.IsMacOS);
            }
        }
    }

    /// <summary>
    /// Executes the DispatchIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset) {
        MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(indirectBuffer);
        this.PreComputeCommand();
        this._cce.dispatchThreadgroupsWithIndirectBuffer(mtlBuffer.DeviceBuffer, offset, this._computePipeline.ThreadsPerThreadgroup);
    }

    /// <summary>
    /// Executes the DrawIndexedIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="drawCount">Specifies the value of <paramref name="drawCount" />.</param>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        if (this.PreDrawCommand()) {
            MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(indirectBuffer);

            for (uint i = 0; i < drawCount; i++) {
                uint currentOffset = i * stride + offset;
                this._rce.drawIndexedPrimitives(this._graphicsPipeline.PrimitiveType, this._indexType, this._indexBuffer.DeviceBuffer, this._ibOffset, mtlBuffer.DeviceBuffer, currentOffset);
            }
        }
    }

    /// <summary>
    /// Executes the DrawIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="drawCount">Specifies the value of <paramref name="drawCount" />.</param>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        if (this.PreDrawCommand()) {
            MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(indirectBuffer);

            for (uint i = 0; i < drawCount; i++) {
                uint currentOffset = i * stride + offset;
                this._rce.drawPrimitives(this._graphicsPipeline.PrimitiveType, mtlBuffer.DeviceBuffer, currentOffset);
            }
        }
    }

    /// <summary>
    /// Executes the ResolveTextureCore operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    protected override void ResolveTextureCore(Texture source, Texture destination) {
        // TODO: This approach destroys the contents of the source Texture (according to the docs).
        this.EnsureNoBlitEncoder();
        this.EnsureNoRenderPass();

        MtlTexture mtlSrc = Util.AssertSubtype<Texture, MtlTexture>(source);
        MtlTexture mtlDst = Util.AssertSubtype<Texture, MtlTexture>(destination);

        MTLRenderPassDescriptor rpDesc = MTLRenderPassDescriptor.New();
        MTLRenderPassColorAttachmentDescriptor colorAttachment = rpDesc.colorAttachments[0];
        colorAttachment.texture = mtlSrc.DeviceTexture;
        colorAttachment.loadAction = MTLLoadAction.Load;
        colorAttachment.storeAction = MTLStoreAction.MultisampleResolve;
        colorAttachment.resolveTexture = mtlDst.DeviceTexture;

        using (NSAutoreleasePool.Begin()) {
            MTLRenderCommandEncoder encoder = this.cb.renderCommandEncoderWithDescriptor(rpDesc);
            encoder.endEncoding();
        }

        ObjectiveCRuntime.release(rpDesc.NativePtr);
    }

    /// <summary>
    /// Executes the SetComputeResourceSetCore operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="dynamicOffsetCount">Specifies the value of <paramref name="dynamicOffsetCount" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
    protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetCount, ref uint dynamicOffsets) {
        if (!this._computeResourceSets[slot].Equals(set, dynamicOffsetCount, ref dynamicOffsets)) {
            this._computeResourceSets[slot].Offsets.Dispose();
            this._computeResourceSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetCount, ref dynamicOffsets);
            this._computeResourceSetsActive[slot] = false;
        }
    }

    /// <summary>
    /// Executes the SetFramebufferCore operation.
    /// </summary>
    /// <param name="fb">Specifies the value of <paramref name="fb" />.</param>
    protected override void SetFramebufferCore(Framebuffer fb) {
        if (!this._currentFramebufferEverActive && this._mtlFramebuffer != null) {
            // This ensures that any submitted clear values will be used even if nothing has been drawn.
            if (this.EnsureRenderPass()) {
                this.EndCurrentRenderPass();
            }
        }

        this.EnsureNoRenderPass();
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

    /// <summary>
    /// Executes the SetGraphicsResourceSetCore operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="rs">Specifies the value of <paramref name="rs" />.</param>
    /// <param name="dynamicOffsetCount">Specifies the value of <paramref name="dynamicOffsetCount" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
    protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetCount, ref uint dynamicOffsets) {
        if (!this._graphicsResourceSets[slot].Equals(rs, dynamicOffsetCount, ref dynamicOffsets)) {
            this._graphicsResourceSets[slot].Offsets.Dispose();
            this._graphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetCount, ref dynamicOffsets);
            this._graphicsResourceSetsActive[slot] = false;
        }
    }

    /// <summary>
    /// Executes the PreDrawCommand operation.
    /// </summary>
    /// <returns>Returns the result produced by the PreDrawCommand operation.</returns>
    private bool PreDrawCommand() {
        if (this.EnsureRenderPass()) {
            if (this._viewportsChanged) {
                this.FlushViewports();
                this._viewportsChanged = false;
            }

            if (this._graphicsPipeline.ScissorTestEnabled) {
                this.FlushScissorRects();
            }

            Debug.Assert(this._graphicsPipeline != null);

            if (this._graphicsPipeline.RenderPipelineState.NativePtr != this._lastGraphicsPipeline?.RenderPipelineState.NativePtr) {
                this._rce.setRenderPipelineState(this._graphicsPipeline.RenderPipelineState);
            }

            if (this._graphicsPipeline.CullMode != this._lastGraphicsPipeline?.CullMode) {
                this._rce.setCullMode(this._graphicsPipeline.CullMode);
            }

            if (this._graphicsPipeline.FrontFace != this._lastGraphicsPipeline?.FrontFace) {
                this._rce.setFrontFacing(this._graphicsPipeline.FrontFace);
            }

            if (this._graphicsPipeline.FillMode != this._lastGraphicsPipeline?.FillMode) {
                this._rce.setTriangleFillMode(this._graphicsPipeline.FillMode);
            }

            RgbaFloat blendColor = this._graphicsPipeline.BlendColor;
            if (blendColor != this._lastGraphicsPipeline?.BlendColor) {
                this._rce.setBlendColor(blendColor.R, blendColor.G, blendColor.B, blendColor.A);
            }

            if (this.Framebuffer.DepthTarget != null) {
                if (this._graphicsPipeline.DepthStencilState.NativePtr != this._lastGraphicsPipeline?.DepthStencilState.NativePtr) {
                    this._rce.setDepthStencilState(this._graphicsPipeline.DepthStencilState);
                }

                if (this._graphicsPipeline.DepthClipMode != this._lastGraphicsPipeline?.DepthClipMode) {
                    this._rce.setDepthClipMode(this._graphicsPipeline.DepthClipMode);
                }

                if (this._graphicsPipeline.StencilReference != this._lastGraphicsPipeline?.StencilReference) {
                    this._rce.setStencilReferenceValue(this._graphicsPipeline.StencilReference);
                }
            }

            this._lastGraphicsPipeline = this._graphicsPipeline;

            for (uint i = 0; i < this._graphicsResourceSetCount; i++) {
                if (!this._graphicsResourceSetsActive[i]) {
                    this.ActivateGraphicsResourceSet(i, this._graphicsResourceSets[i]);
                    this._graphicsResourceSetsActive[i] = true;
                }
            }

            for (uint i = 0; i < this._vertexBufferCount; i++) {
                if (!this._vertexBuffersActive[i]) {
                    UIntPtr index = this._graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                        ? this._nonVertexBufferCount + i
                        : i;
                    this._rce.setVertexBuffer(this._vertexBuffers[i].DeviceBuffer, this._vbOffsets[i], index);

                    this._vertexBuffersActive[i] = true;
                    this._vbOffsetsActive[i] = true;
                }

                if (!this._vbOffsetsActive[i]) {
                    UIntPtr index = this._graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                        ? this._nonVertexBufferCount + i
                        : i;

                    this._rce.setVertexBufferOffset(this._vbOffsets[i], index);

                    this._vbOffsetsActive[i] = true;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Executes the FlushViewports operation.
    /// </summary>
    private void FlushViewports() {
        if (this.gd.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3)) {
            fixed (MTLViewport* viewportsPtr = &this._viewports[0]) {
                this._rce.setViewports(viewportsPtr, this._viewportCount);
            }
        }
        else {
            this._rce.setViewport(this._viewports[0]);
        }
    }

    /// <summary>
    /// Executes the FlushScissorRects operation.
    /// </summary>
    private void FlushScissorRects() {
        if (this.gd.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3)) {
            bool scissorRectsChanged = false;

            for (int i = 0; i < this._scissorRects.Length; i++) {
                scissorRectsChanged |= !this._scissorRects[i].Equals(this._activeScissorRects[i]);
                this._activeScissorRects[i] = this._scissorRects[i];
            }

            if (scissorRectsChanged) {
                fixed (MTLScissorRect* scissorRectsPtr = this._scissorRects) {
                    this._rce.setScissorRects(scissorRectsPtr, this._viewportCount);
                }
            }
        }
        else {
            if (!this._scissorRects[0].Equals(this._activeScissorRects[0])) {
                this._rce.setScissorRect(this._scissorRects[0]);
            }

            this._activeScissorRects[0] = this._scissorRects[0];
        }
    }

    /// <summary>
    /// Executes the PreComputeCommand operation.
    /// </summary>
    private void PreComputeCommand() {
        this.EnsureComputeEncoder();

        if (this._computePipeline.ComputePipelineState.NativePtr != this._lastComputePipeline?.ComputePipelineState.NativePtr) {
            this._cce.setComputePipelineState(this._computePipeline.ComputePipelineState);
        }

        this._lastComputePipeline = this._computePipeline;

        for (uint i = 0; i < this._computeResourceSetCount; i++) {
            if (!this._computeResourceSetsActive[i]) {
                this.ActivateComputeResourceSet(i, this._computeResourceSets[i]);
                this._computeResourceSetsActive[i] = true;
            }
        }
    }

    /// <summary>
    /// Executes the GetFreeStagingBuffer operation.
    /// </summary>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    /// <returns>Returns the result produced by the GetFreeStagingBuffer operation.</returns>
    private MtlBuffer GetFreeStagingBuffer(uint sizeInBytes) {
        lock (this._submittedCommandsLock) {
            foreach (MtlBuffer buffer in this._availableStagingBuffers) {
                if (buffer.SizeInBytes >= sizeInBytes) {
                    this._availableStagingBuffers.Remove(buffer);
                    return buffer;
                }
            }
        }

        DeviceBuffer staging = this.gd.ResourceFactory.CreateBuffer(new BufferDescription(sizeInBytes, BufferUsage.Staging));

        return Util.AssertSubtype<DeviceBuffer, MtlBuffer>(staging);
    }

    /// <summary>
    /// Executes the ActivateGraphicsResourceSet operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="brsi">Specifies the value of <paramref name="brsi" />.</param>
    private void ActivateGraphicsResourceSet(uint slot, BoundResourceSetInfo brsi) {
        Debug.Assert(this.renderEncoderActive);
        MtlResourceSet mtlRs = Util.AssertSubtype<ResourceSet, MtlResourceSet>(brsi.Set);
        MtlResourceLayout layout = mtlRs.Layout;
        uint dynamicOffsetIndex = 0;

        for (int i = 0; i < mtlRs.Resources.Length; i++) {
            MtlResourceLayout.ResourceBindingInfo bindingInfo = layout.GetBindingInfo(i);
            IBindableResource resource = mtlRs.Resources[i];
            uint bufferOffset = 0;

            if (bindingInfo.DynamicBuffer) {
                bufferOffset = brsi.Offsets.Get(dynamicOffsetIndex);
                dynamicOffsetIndex += 1;
            }

            switch (bindingInfo.Kind) {
                case ResourceKind.UniformBuffer: {
                        DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                        this.BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                case ResourceKind.TextureReadOnly:
                    TextureView texView = Util.GetTextureView(this.gd, resource);
                    MtlTextureView mtlTexView = Util.AssertSubtype<TextureView, MtlTextureView>(texView);
                    this.BindTexture(mtlTexView, slot, bindingInfo.Slot, bindingInfo.Stages);
                    break;

                case ResourceKind.TextureReadWrite:
                    TextureView texViewRw = Util.GetTextureView(this.gd, resource);
                    MtlTextureView mtlTexViewRw = Util.AssertSubtype<TextureView, MtlTextureView>(texViewRw);
                    this.BindTexture(mtlTexViewRw, slot, bindingInfo.Slot, bindingInfo.Stages);
                    break;

                case ResourceKind.Sampler:
                    MtlSampler mtlSampler = Util.AssertSubtype<IBindableResource, MtlSampler>(resource);
                    this.BindSampler(mtlSampler, slot, bindingInfo.Slot, bindingInfo.Stages);
                    break;

                case ResourceKind.StructuredBufferReadOnly: {
                        DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                        this.BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                case ResourceKind.StructuredBufferReadWrite: {
                        DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                        this.BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                default: throw Illegal.Value<ResourceKind>();
            }
        }
    }

    /// <summary>
    /// Executes the ActivateComputeResourceSet operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="brsi">Specifies the value of <paramref name="brsi" />.</param>
    private void ActivateComputeResourceSet(uint slot, BoundResourceSetInfo brsi) {
        Debug.Assert(this.computeEncoderActive);
        MtlResourceSet mtlRs = Util.AssertSubtype<ResourceSet, MtlResourceSet>(brsi.Set);
        MtlResourceLayout layout = mtlRs.Layout;
        uint dynamicOffsetIndex = 0;

        for (int i = 0; i < mtlRs.Resources.Length; i++) {
            MtlResourceLayout.ResourceBindingInfo bindingInfo = layout.GetBindingInfo(i);
            IBindableResource resource = mtlRs.Resources[i];
            uint bufferOffset = 0;

            if (bindingInfo.DynamicBuffer) {
                bufferOffset = brsi.Offsets.Get(dynamicOffsetIndex);
                dynamicOffsetIndex += 1;
            }

            switch (bindingInfo.Kind) {
                case ResourceKind.UniformBuffer: {
                        DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                        this.BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                case ResourceKind.TextureReadOnly:
                    TextureView texView = Util.GetTextureView(this.gd, resource);
                    MtlTextureView mtlTexView = Util.AssertSubtype<TextureView, MtlTextureView>(texView);
                    this.BindTexture(mtlTexView, slot, bindingInfo.Slot, bindingInfo.Stages);
                    break;

                case ResourceKind.TextureReadWrite:
                    TextureView texViewRw = Util.GetTextureView(this.gd, resource);
                    MtlTextureView mtlTexViewRw = Util.AssertSubtype<TextureView, MtlTextureView>(texViewRw);
                    this.BindTexture(mtlTexViewRw, slot, bindingInfo.Slot, bindingInfo.Stages);
                    break;

                case ResourceKind.Sampler:
                    MtlSampler mtlSampler = Util.AssertSubtype<IBindableResource, MtlSampler>(resource);
                    this.BindSampler(mtlSampler, slot, bindingInfo.Slot, bindingInfo.Stages);
                    break;

                case ResourceKind.StructuredBufferReadOnly: {
                        DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                        this.BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                case ResourceKind.StructuredBufferReadWrite: {
                        DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                        this.BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;
                    }

                default: throw Illegal.Value<ResourceKind>();
            }
        }
    }

    /// <summary>
    /// Executes the BindBuffer operation.
    /// </summary>
    /// <param name="range">Specifies the value of <paramref name="range" />.</param>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="stages">Specifies the value of <paramref name="stages" />.</param>
    private void BindBuffer(DeviceBufferRange range, uint set, uint slot, ShaderStages stages) {
        MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(range.Buffer);
        uint baseBuffer = this.GetBufferBase(set, stages != ShaderStages.Compute);

        if (stages == ShaderStages.Compute) {
            UIntPtr index = slot + baseBuffer;

            if (!this._boundComputeBuffers.TryGetValue(index, out DeviceBufferRange boundBuffer) || !range.Equals(boundBuffer)) {
                this._cce.setBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);
                this._boundComputeBuffers[index] = range;
            }
        }
        else {
            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex) {
                UIntPtr index = this._graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                    ? slot + baseBuffer
                    : slot + this._vertexBufferCount + baseBuffer;

                if (!this._boundVertexBuffers.TryGetValue(index, out DeviceBufferRange boundBuffer) || boundBuffer.Buffer != range.Buffer) {
                    this._rce.setVertexBuffer(mtlBuffer.DeviceBuffer, range.Offset, index);
                    this._boundVertexBuffers[index] = range;
                }
                else if (!range.Equals(boundBuffer)) {
                    this._rce.setVertexBufferOffset(range.Offset, index);
                    this._boundVertexBuffers[index] = range;
                }
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment) {
                UIntPtr index = slot + baseBuffer;

                if (!this._boundFragmentBuffers.TryGetValue(index, out DeviceBufferRange boundBuffer) || boundBuffer.Buffer != range.Buffer) {
                    this._rce.setFragmentBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);
                    this._boundFragmentBuffers[index] = range;
                }
                else if (!range.Equals(boundBuffer)) {
                    this._rce.setFragmentBufferOffset(range.Offset, slot + baseBuffer);
                    this._boundFragmentBuffers[index] = range;
                }
            }
        }
    }

    /// <summary>
    /// Executes the BindTexture operation.
    /// </summary>
    /// <param name="mtlTexView">Specifies the value of <paramref name="mtlTexView" />.</param>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="stages">Specifies the value of <paramref name="stages" />.</param>
    private void BindTexture(MtlTextureView mtlTexView, uint set, uint slot, ShaderStages stages) {
        uint baseTexture = this.GetTextureBase(set, stages != ShaderStages.Compute);
        UIntPtr index = slot + baseTexture;

        if (stages == ShaderStages.Compute && (!this._boundComputeTextures.TryGetValue(index, out MTLTexture computeTexture) || computeTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr)) {
            this._cce.setTexture(mtlTexView.TargetDeviceTexture, index);
            this._boundComputeTextures[index] = mtlTexView.TargetDeviceTexture;
        }

        if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex
            && (!this._boundVertexTextures.TryGetValue(index, out MTLTexture vertexTexture) || vertexTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr)) {
            this._rce.setVertexTexture(mtlTexView.TargetDeviceTexture, index);
            this._boundVertexTextures[index] = mtlTexView.TargetDeviceTexture;
        }

        if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment
            && (!this._boundFragmentTextures.TryGetValue(index, out MTLTexture fragmentTexture) || fragmentTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr)) {
            this._rce.setFragmentTexture(mtlTexView.TargetDeviceTexture, index);
            this._boundFragmentTextures[index] = mtlTexView.TargetDeviceTexture;
        }
    }

    /// <summary>
    /// Executes the BindSampler operation.
    /// </summary>
    /// <param name="mtlSampler">Specifies the value of <paramref name="mtlSampler" />.</param>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="stages">Specifies the value of <paramref name="stages" />.</param>
    private void BindSampler(MtlSampler mtlSampler, uint set, uint slot, ShaderStages stages) {
        uint baseSampler = this.GetSamplerBase(set, stages != ShaderStages.Compute);
        UIntPtr index = slot + baseSampler;

        if (stages == ShaderStages.Compute && (!this._boundComputeSamplers.TryGetValue(index, out MTLSamplerState computeSampler) || computeSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr)) {
            this._cce.setSamplerState(mtlSampler.DeviceSampler, index);
            this._boundComputeSamplers[index] = mtlSampler.DeviceSampler;
        }

        if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex
            && (!this._boundVertexSamplers.TryGetValue(index, out MTLSamplerState vertexSampler) || vertexSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr)) {
            this._rce.setVertexSamplerState(mtlSampler.DeviceSampler, index);
            this._boundVertexSamplers[index] = mtlSampler.DeviceSampler;
        }

        if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment
            && (!this._boundFragmentSamplers.TryGetValue(index, out MTLSamplerState fragmentSampler) || fragmentSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr)) {
            this._rce.setFragmentSamplerState(mtlSampler.DeviceSampler, index);
            this._boundFragmentSamplers[index] = mtlSampler.DeviceSampler;
        }
    }

    /// <summary>
    /// Executes the GetBufferBase operation.
    /// </summary>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="graphics">Specifies the value of <paramref name="graphics" />.</param>
    /// <returns>Returns the result produced by the GetBufferBase operation.</returns>
    private uint GetBufferBase(uint set, bool graphics) {
        MtlResourceLayout[] layouts = graphics ? this._graphicsPipeline.ResourceLayouts : this._computePipeline.ResourceLayouts;
        uint ret = 0;

        for (int i = 0; i < set; i++) {
            Debug.Assert(layouts[i] != null);
            ret += layouts[i].BufferCount;
        }

        return ret;
    }

    /// <summary>
    /// Executes the GetTextureBase operation.
    /// </summary>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="graphics">Specifies the value of <paramref name="graphics" />.</param>
    /// <returns>Returns the result produced by the GetTextureBase operation.</returns>
    private uint GetTextureBase(uint set, bool graphics) {
        MtlResourceLayout[] layouts = graphics ? this._graphicsPipeline.ResourceLayouts : this._computePipeline.ResourceLayouts;
        uint ret = 0;

        for (int i = 0; i < set; i++) {
            Debug.Assert(layouts[i] != null);
            ret += layouts[i].TextureCount;
        }

        return ret;
    }

    /// <summary>
    /// Executes the GetSamplerBase operation.
    /// </summary>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="graphics">Specifies the value of <paramref name="graphics" />.</param>
    /// <returns>Returns the result produced by the GetSamplerBase operation.</returns>
    private uint GetSamplerBase(uint set, bool graphics) {
        MtlResourceLayout[] layouts = graphics ? this._graphicsPipeline.ResourceLayouts : this._computePipeline.ResourceLayouts;
        uint ret = 0;

        for (int i = 0; i < set; i++) {
            Debug.Assert(layouts[i] != null);
            ret += layouts[i].SamplerCount;
        }

        return ret;
    }

    /// <summary>
    /// Executes the EnsureRenderPass operation.
    /// </summary>
    /// <returns>Returns the result produced by the EnsureRenderPass operation.</returns>
    private bool EnsureRenderPass() {
        Debug.Assert(this._mtlFramebuffer != null);
        this.EnsureNoBlitEncoder();
        this.EnsureNoComputeEncoder();
        return this.renderEncoderActive || this.BeginCurrentRenderPass();
    }

    /// <summary>
    /// Executes the BeginCurrentRenderPass operation.
    /// </summary>
    /// <returns>Returns the result produced by the BeginCurrentRenderPass operation.</returns>
    private bool BeginCurrentRenderPass() {
        if (this._mtlFramebuffer is MtlSwapchainFramebuffer swapchainFramebuffer && !swapchainFramebuffer.EnsureDrawableAvailable()) {
            return false;
        }

        MTLRenderPassDescriptor rpDesc = this._mtlFramebuffer.CreateRenderPassDescriptor();

        for (uint i = 0; i < this._clearColors.Length; i++) {
            if (this._clearColors[i] != null) {
                MTLRenderPassColorAttachmentDescriptor attachment = rpDesc.colorAttachments[0];
                attachment.loadAction = MTLLoadAction.Clear;
                RgbaFloat c = this._clearColors[i].Value;
                attachment.clearColor = new MTLClearColor(c.R, c.G, c.B, c.A);
                this._clearColors[i] = null;
            }
        }

        if (this._clearDepth != null) {
            MTLRenderPassDepthAttachmentDescriptor depthAttachment = rpDesc.depthAttachment;
            depthAttachment.loadAction = MTLLoadAction.Clear;
            depthAttachment.clearDepth = this._clearDepth.Value.depth;

            if (this._mtlFramebuffer.DepthTarget != null && FormatHelpers.IsStencilFormat(this._mtlFramebuffer.DepthTarget.Value.Target.Format)) {
                MTLRenderPassStencilAttachmentDescriptor stencilAttachment = rpDesc.stencilAttachment;
                stencilAttachment.loadAction = MTLLoadAction.Clear;
                stencilAttachment.clearStencil = this._clearDepth.Value.stencil;
            }

            this._clearDepth = null;
        }

        using (NSAutoreleasePool.Begin()) {
            this._rce = this.cb.renderCommandEncoderWithDescriptor(rpDesc);
            ObjectiveCRuntime.retain(this._rce.NativePtr);
        }

        ObjectiveCRuntime.release(rpDesc.NativePtr);
        this._currentFramebufferEverActive = true;

        return true;
    }

    /// <summary>
    /// Executes the EnsureNoRenderPass operation.
    /// </summary>
    private void EnsureNoRenderPass() {
        if (this.renderEncoderActive) {
            this.EndCurrentRenderPass();
        }

        Debug.Assert(!this.renderEncoderActive);
    }

    /// <summary>
    /// Executes the EndCurrentRenderPass operation.
    /// </summary>
    private void EndCurrentRenderPass() {
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

    /// <summary>
    /// Executes the EnsureBlitEncoder operation.
    /// </summary>
    private void EnsureBlitEncoder() {
        if (!this.blitEncoderActive) {
            this.EnsureNoRenderPass();
            this.EnsureNoComputeEncoder();

            using (NSAutoreleasePool.Begin()) {
                this._bce = this.cb.blitCommandEncoder();
                ObjectiveCRuntime.retain(this._bce.NativePtr);
            }
        }

        Debug.Assert(this.blitEncoderActive);
        Debug.Assert(!this.renderEncoderActive);
        Debug.Assert(!this.computeEncoderActive);
    }

    /// <summary>
    /// Executes the EnsureNoBlitEncoder operation.
    /// </summary>
    private void EnsureNoBlitEncoder() {
        if (this.blitEncoderActive) {
            this._bce.endEncoding();
            ObjectiveCRuntime.release(this._bce.NativePtr);
            this._bce = default;
        }

        Debug.Assert(!this.blitEncoderActive);
    }

    /// <summary>
    /// Executes the EnsureComputeEncoder operation.
    /// </summary>
    private void EnsureComputeEncoder() {
        if (!this.computeEncoderActive) {
            this.EnsureNoBlitEncoder();
            this.EnsureNoRenderPass();

            using (NSAutoreleasePool.Begin()) {
                this._cce = this.cb.computeCommandEncoder();
                ObjectiveCRuntime.retain(this._cce.NativePtr);
            }
        }

        Debug.Assert(this.computeEncoderActive);
        Debug.Assert(!this.renderEncoderActive);
        Debug.Assert(!this.blitEncoderActive);
    }

    /// <summary>
    /// Executes the EnsureNoComputeEncoder operation.
    /// </summary>
    private void EnsureNoComputeEncoder() {
        if (this.computeEncoderActive) {
            this._cce.endEncoding();
            ObjectiveCRuntime.release(this._cce.NativePtr);
            this._cce = default;

            this._boundComputeBuffers.Clear();
            this._boundComputeTextures.Clear();
            this._boundComputeSamplers.Clear();
            this._lastComputePipeline = null;

            Util.ClearArray(this._computeResourceSetsActive);
        }

        Debug.Assert(!this.computeEncoderActive);
    }

    /// <summary>
    /// Executes the ClearColorTargetCore operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="clearColor">Specifies the value of <paramref name="clearColor" />.</param>
    private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor) {
        this.EnsureNoRenderPass();
        this._clearColors[index] = clearColor;
    }

    /// <summary>
    /// Executes the ClearDepthStencilCore operation.
    /// </summary>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="stencil">Specifies the value of <paramref name="stencil" />.</param>
    private protected override void ClearDepthStencilCore(float depth, byte stencil) {
        this.EnsureNoRenderPass();
        this._clearDepth = (depth, stencil);
    }

    /// <summary>
    /// Executes the DrawCore operation.
    /// </summary>
    /// <param name="vertexCount">Specifies the value of <paramref name="vertexCount" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    /// <param name="vertexStart">Specifies the value of <paramref name="vertexStart" />.</param>
    /// <param name="instanceStart">Specifies the value of <paramref name="instanceStart" />.</param>
    private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart) {
        if (this.PreDrawCommand()) {
            if (instanceStart == 0) {
                this._rce.drawPrimitives(this._graphicsPipeline.PrimitiveType, vertexStart, vertexCount, instanceCount);
            }
            else {
                this._rce.drawPrimitives(this._graphicsPipeline.PrimitiveType, vertexStart, vertexCount, instanceCount, instanceStart);
            }
        }
    }

    /// <summary>
    /// Executes the DrawIndexedCore operation.
    /// </summary>
    /// <param name="indexCount">Specifies the value of <paramref name="indexCount" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    /// <param name="indexStart">Specifies the value of <paramref name="indexStart" />.</param>
    /// <param name="vertexOffset">Specifies the value of <paramref name="vertexOffset" />.</param>
    /// <param name="instanceStart">Specifies the value of <paramref name="instanceStart" />.</param>
    private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart) {
        if (this.PreDrawCommand()) {
            uint indexSize = this._indexType == MTLIndexType.UInt16 ? 2u : 4u;
            uint indexBufferOffset = indexSize * indexStart + this._ibOffset;

            if (vertexOffset == 0 && instanceStart == 0) {
                this._rce.drawIndexedPrimitives(this._graphicsPipeline.PrimitiveType, indexCount, this._indexType, this._indexBuffer.DeviceBuffer, indexBufferOffset, instanceCount);
            }
            else {
                this._rce.drawIndexedPrimitives(this._graphicsPipeline.PrimitiveType, indexCount, this._indexType, this._indexBuffer.DeviceBuffer, indexBufferOffset, instanceCount, vertexOffset, instanceStart);
            }
        }
    }

    /// <summary>
    /// Executes the SetPipelineCore operation.
    /// </summary>
    /// <param name="pipeline">Specifies the value of <paramref name="pipeline" />.</param>
    private protected override void SetPipelineCore(Pipeline pipeline) {
        if (pipeline.IsComputePipeline && this._computePipeline != pipeline) {
            this._computePipeline = Util.AssertSubtype<Pipeline, MtlPipeline>(pipeline);
            this._computeResourceSetCount = (uint)this._computePipeline.ResourceLayouts.Length;
            Util.EnsureArrayMinimumSize(ref this._computeResourceSets, this._computeResourceSetCount);
            Util.EnsureArrayMinimumSize(ref this._computeResourceSetsActive, this._computeResourceSetCount);
            Util.ClearArray(this._computeResourceSetsActive);
        }
        else if (!pipeline.IsComputePipeline && this._graphicsPipeline != pipeline) {
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

    /// <summary>
    /// Executes the UpdateBufferCore operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        bool useComputeCopy = bufferOffsetInBytes % 4 != 0
                              || (sizeInBytes % 4 != 0 && bufferOffsetInBytes != 0 && sizeInBytes != buffer.SizeInBytes);

        MtlBuffer dstMtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
        MtlBuffer staging = this.GetFreeStagingBuffer(sizeInBytes);

        this.gd.UpdateBuffer(staging, 0, source, sizeInBytes);

        if (useComputeCopy) {
            this.CopyBufferCore(staging, 0, buffer, bufferOffsetInBytes, sizeInBytes);
        }
        else {
            Debug.Assert(bufferOffsetInBytes % 4 == 0);
            uint sizeRoundFactor = (4 - sizeInBytes % 4) % 4;
            this.EnsureBlitEncoder();
            this._bce.copy(staging.DeviceBuffer, UIntPtr.Zero, dstMtlBuffer.DeviceBuffer, bufferOffsetInBytes, sizeInBytes + sizeRoundFactor);
        }

        lock (this._submittedCommandsLock) {
            this._submittedStagingBuffers.Add(this.cb, staging);
        }
    }

    /// <summary>
    /// Executes the GenerateMipmapsCore operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    private protected override void GenerateMipmapsCore(Texture texture) {
        Debug.Assert(texture.MipLevels > 1);
        this.EnsureBlitEncoder();
        MtlTexture mtlTex = Util.AssertSubtype<Texture, MtlTexture>(texture);
        this._bce.generateMipmapsForTexture(mtlTex.DeviceTexture);
    }

    /// <summary>
    /// Executes the SetIndexBufferCore operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset) {
        this._indexBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
        this._ibOffset = offset;
        this._indexType = MtlFormats.VdToMtlIndexFormat(format);
    }

    /// <summary>
    /// Executes the SetVertexBufferCore operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset) {
        Util.EnsureArrayMinimumSize(ref this._vertexBuffers, index + 1);
        Util.EnsureArrayMinimumSize(ref this._vbOffsets, index + 1);
        Util.EnsureArrayMinimumSize(ref this._vertexBuffersActive, index + 1);
        Util.EnsureArrayMinimumSize(ref this._vbOffsetsActive, index + 1);

        if (this._vertexBuffers[index] != buffer) {
            MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
            this._vertexBuffers[index] = mtlBuffer;
            this._vertexBuffersActive[index] = false;
        }

        if (this._vbOffsets[index] != offset) {
            this._vbOffsets[index] = offset;
            this._vbOffsetsActive[index] = false;
        }
    }

    /// <summary>
    /// Executes the PushDebugGroupCore operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    private protected override void PushDebugGroupCore(string name) {
        NSString nsName = NSString.New(name);
        if (!this._bce.IsNull) {
            this._bce.pushDebugGroup(nsName);
        }
        else if (!this._cce.IsNull) {
            this._cce.pushDebugGroup(nsName);
        }
        else if (!this._rce.IsNull) {
            this._rce.pushDebugGroup(nsName);
        }

        ObjectiveCRuntime.release(nsName);
    }

    /// <summary>
    /// Executes the PopDebugGroupCore operation.
    /// </summary>
    private protected override void PopDebugGroupCore() {
        if (!this._bce.IsNull) {
            this._bce.popDebugGroup();
        }
        else if (!this._cce.IsNull) {
            this._cce.popDebugGroup();
        }
        else if (!this._rce.IsNull) {
            this._rce.popDebugGroup();
        }
    }

    /// <summary>
    /// Executes the InsertDebugMarkerCore operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    private protected override void InsertDebugMarkerCore(string name) {
        NSString nsName = NSString.New(name);
        if (!this._bce.IsNull) {
            this._bce.insertDebugSignpost(nsName);
        }
        else if (!this._cce.IsNull) {
            this._cce.insertDebugSignpost(nsName);
        }
        else if (!this._rce.IsNull) {
            this._rce.insertDebugSignpost(nsName);
        }

        ObjectiveCRuntime.release(nsName);
    }
}
