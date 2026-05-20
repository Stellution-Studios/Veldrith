using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlCommandList.
/// </summary>
internal unsafe class MtlCommandList : CommandList {

    /// <summary>
    /// Stores the available staging buffers collection used by this instance.
    /// </summary>
    private readonly List<MtlBuffer> _availableStagingBuffers = new();

    /// <summary>
    /// Stores the bound compute buffers collection used by this instance.
    /// </summary>
    private readonly Dictionary<UIntPtr, DeviceBufferRange> _boundComputeBuffers = new();

    /// <summary>
    /// Stores the bound compute samplers collection used by this instance.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLSamplerState> _boundComputeSamplers = new();

    /// <summary>
    /// Stores the bound compute textures collection used by this instance.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLTexture> _boundComputeTextures = new();

    /// <summary>
    /// Stores the bound fragment buffers collection used by this instance.
    /// </summary>
    private readonly Dictionary<UIntPtr, DeviceBufferRange> _boundFragmentBuffers = new();

    /// <summary>
    /// Stores the bound fragment samplers collection used by this instance.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLSamplerState> _boundFragmentSamplers = new();

    /// <summary>
    /// Stores the bound fragment textures collection used by this instance.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLTexture> _boundFragmentTextures = new();

    /// <summary>
    /// Stores the bound vertex buffers collection used by this instance.
    /// </summary>
    private readonly Dictionary<UIntPtr, DeviceBufferRange> _boundVertexBuffers = new();

    /// <summary>
    /// Stores the bound vertex samplers collection used by this instance.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLSamplerState> _boundVertexSamplers = new();

    /// <summary>
    /// Stores the bound vertex textures collection used by this instance.
    /// </summary>
    private readonly Dictionary<UIntPtr, MTLTexture> _boundVertexTextures = new();

    /// <summary>
    /// Stores the completion fences collection used by this instance.
    /// </summary>
    private readonly CommandBufferUsageList<MtlFence> _completionFences = new();

    /// <summary>
    /// Synchronizes access to the submitted commands lock state.
    /// </summary>
    private readonly object _submittedCommandsLock = new();

    /// <summary>
    /// Stores the submitted staging buffers collection used by this instance.
    /// </summary>
    private readonly CommandBufferUsageList<MtlBuffer> _submittedStagingBuffers = new();

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private MTLScissorRect[] _activeScissorRects = Array.Empty<MTLScissorRect>();

    /// <summary>
    /// Stores the bce state used by this instance.
    /// </summary>
    private MTLBlitCommandEncoder _bce;

    /// <summary>
    /// Stores the cce state used by this instance.
    /// </summary>
    private MTLComputeCommandEncoder _cce;

    /// <summary>
    /// Executes the value logic for this backend.
    /// </summary>
    private RgbaFloat?[] _clearColors = Array.Empty<RgbaFloat?>();

    /// <summary>
    /// Stores the clear depth value used during command execution.
    /// </summary>
    /// <param name="depth">The depth value.</param>
    /// <param name="stencil">The stencil value used by this operation.</param>
    private (float depth, byte stencil)? _clearDepth;

    /// <summary>
    /// Stores the compute pipeline state used by this instance.
    /// </summary>
    private MtlPipeline _computePipeline;

    /// <summary>
    /// Stores the compute resource set count collection used by this instance.
    /// </summary>
    private uint _computeResourceSetCount;

    /// <summary>
    /// Stores the compute resource sets collection used by this instance.
    /// </summary>
    private BoundResourceSetInfo[] _computeResourceSets;

    /// <summary>
    /// Stores the compute resource sets active collection used by this instance.
    /// </summary>
    private bool[] _computeResourceSetsActive;

    /// <summary>
    /// Stores the current framebuffer ever active state used by this instance.
    /// </summary>
    private bool _currentFramebufferEverActive;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the graphics pipeline state used by this instance.
    /// </summary>
    private MtlPipeline _graphicsPipeline;

    /// <summary>
    /// Stores whether push constants target the compute pipeline for the current state.
    /// </summary>
    private bool _pushConstantsUseComputePipeline;

    /// <summary>
    /// Stores the graphics resource set count collection used by this instance.
    /// </summary>
    private uint _graphicsResourceSetCount;

    /// <summary>
    /// Stores the graphics resource sets collection used by this instance.
    /// </summary>
    private BoundResourceSetInfo[] _graphicsResourceSets;

    /// <summary>
    /// Stores the graphics resource sets active collection used by this instance.
    /// </summary>
    private bool[] _graphicsResourceSetsActive;

    /// <summary>
    /// Stores the ib offset value used during command execution.
    /// </summary>
    private uint _ibOffset;

    /// <summary>
    /// Stores the index buffer value used during command execution.
    /// </summary>
    private MtlBuffer _indexBuffer;

    /// <summary>
    /// Stores the index type value used during command execution.
    /// </summary>
    private MTLIndexType _indexType;

    /// <summary>
    /// Stores the last compute pipeline state used by this instance.
    /// </summary>
    private MtlPipeline _lastComputePipeline;

    /// <summary>
    /// Stores the last graphics pipeline state used by this instance.
    /// </summary>
    private MtlPipeline _lastGraphicsPipeline;

    /// <summary>
    /// Stores the mtl framebuffer state used by this instance.
    /// </summary>
    private MtlFramebuffer _mtlFramebuffer;

    /// <summary>
    /// Stores the non vertex buffer count value used during command execution.
    /// </summary>
    private uint _nonVertexBufferCount;

    /// <summary>
    /// Stores the rce state used by this instance.
    /// </summary>
    private MTLRenderCommandEncoder _rce;

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private MTLScissorRect[] _scissorRects = Array.Empty<MTLScissorRect>();

    /// <summary>
    /// Stores the vb offsets value used during command execution.
    /// </summary>
    private uint[] _vbOffsets;

    /// <summary>
    /// Stores the vb offsets active value used during command execution.
    /// </summary>
    private bool[] _vbOffsetsActive;

    /// <summary>
    /// Stores the vertex buffer count value used during command execution.
    /// </summary>
    private uint _vertexBufferCount;

    /// <summary>
    /// Stores the vertex buffers collection used by this instance.
    /// </summary>
    private MtlBuffer[] _vertexBuffers;

    /// <summary>
    /// Stores the vertex buffers active collection used by this instance.
    /// </summary>
    private bool[] _vertexBuffersActive;

    /// <summary>
    /// Stores the viewport count value used during command execution.
    /// </summary>
    private uint _viewportCount;

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private MTLViewport[] _viewports = Array.Empty<MTLViewport>();

    /// <summary>
    /// Stores the viewports changed state used by this instance.
    /// </summary>
    private bool _viewportsChanged;

    /// <summary>
    /// Stores the cb state used by this instance.
    /// </summary>
    private MTLCommandBuffer cb;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlCommandList" /> class.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="gd">The graphics device that owns this operation.</param>
    public MtlCommandList(ref CommandListDescription description, MtlGraphicsDevice gd) : base(ref description, gd.Features, gd.UniformBufferMinOffsetAlignment, gd.StructuredBufferMinOffsetAlignment) {
        this.gd = gd;
    }

    /// <summary>
    /// Stores the command buffer state used by this instance.
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
    /// Stores the render encoder active state used by this instance.
    /// </summary>
    private bool renderEncoderActive => !this._rce.IsNull;

    /// <summary>
    /// Stores the blit encoder active state used by this instance.
    /// </summary>
    private bool blitEncoderActive => !this._bce.IsNull;

    /// <summary>
    /// Stores the compute encoder active state used by this instance.
    /// </summary>
    private bool computeEncoderActive => !this._cce.IsNull;

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
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
                ObjectiveCRuntime.Release(this.cb.NativePtr);
            }
        }
    }

    #endregion

    /// <summary>
    /// Executes the commit logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public MTLCommandBuffer Commit() {
        this.cb.Commit();
        MTLCommandBuffer ret = this.cb;
        this.cb = default;
        return ret;
    }

    /// <summary>
    /// Begins the value operation.
    /// </summary>
    public override void Begin() {
        if (this.cb.NativePtr != IntPtr.Zero) {
            ObjectiveCRuntime.Release(this.cb.NativePtr);
        }

        using (NSAutoreleasePool.Begin()) {
            this.cb = this.gd.CommandQueue.CommandBuffer();
            ObjectiveCRuntime.Retain(this.cb.NativePtr);
        }

        this.ClearCachedState();
    }

    /// <summary>
    /// Executes the dispatch logic for this backend.
    /// </summary>
    /// <param name="groupCountX">The group count x value used by this operation.</param>
    /// <param name="groupCountY">The group count y value used by this operation.</param>
    /// <param name="groupCountZ">The group count z value used by this operation.</param>
    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ) {
        this.PreComputeCommand();
        this._cce.DispatchThreadGroups(new MTLSize(groupCountX, groupCountY, groupCountZ), this._computePipeline.ThreadsPerThreadgroup);
    }

    /// <summary>
    /// Ends the value operation.
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
    /// Sets the scissor rect value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height) {
        this._scissorRects[index] = new MTLScissorRect(x, y, width, height);
    }

    /// <summary>
    /// Sets the viewport value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="viewport">The viewport value used by this operation.</param>
    public override void SetViewport(uint index, ref Viewport viewport) {
        this._viewportsChanged = true;
        this._viewports[index] = new MTLViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
    }

    /// <summary>
    /// Sets the completion fence value.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    public void SetCompletionFence(MTLCommandBuffer cb, MtlFence fence) {
        lock (this._submittedCommandsLock) {
            Debug.Assert(!this._completionFences.Contains(cb));
            this._completionFences.Add(cb, fence);
        }
    }

    /// <summary>
    /// Executes the on completed logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
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
    /// Copies buffer core data between resources.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sourceOffset">The source offset value used by this operation.</param>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes) {
        MtlBuffer mtlSrc = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(source);
        MtlBuffer mtlDst = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(destination);

        if (sourceOffset % 4 != 0 || destinationOffset % 4 != 0 || sizeInBytes % 4 != 0) {
            // Unaligned copy -- use special compute shader.
            this.EnsureComputeEncoder();
            this._cce.SetComputePipelineState(this.gd.GetUnalignedBufferCopyPipeline());
            this._cce.SetBuffer(mtlSrc.DeviceBuffer, UIntPtr.Zero, 0);
            this._cce.SetBuffer(mtlDst.DeviceBuffer, UIntPtr.Zero, 1);

            MtlUnalignedBufferCopyInfo copyInfo;
            copyInfo.SourceOffset = sourceOffset;
            copyInfo.DestinationOffset = destinationOffset;
            copyInfo.CopySize = sizeInBytes;

            this._cce.SetBytes(&copyInfo, (UIntPtr)sizeof(MtlUnalignedBufferCopyInfo), 2);
            this._cce.DispatchThreadGroups(new MTLSize(1, 1, 1), new MTLSize(1, 1, 1));
        }
        else {
            this.EnsureBlitEncoder();
            this._bce.Copy(mtlSrc.DeviceBuffer, sourceOffset, mtlDst.DeviceBuffer, destinationOffset, sizeInBytes);
        }
    }

    /// <summary>
    /// Copies texture core data between resources.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="srcX">The src x value used by this operation.</param>
    /// <param name="srcY">The src y value used by this operation.</param>
    /// <param name="srcZ">The src z value used by this operation.</param>
    /// <param name="srcMipLevel">The src mip level value used by this operation.</param>
    /// <param name="srcBaseArrayLayer">The src base array layer value used by this operation.</param>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="dstX">The dst x value used by this operation.</param>
    /// <param name="dstY">The dst y value used by this operation.</param>
    /// <param name="dstZ">The dst z value used by this operation.</param>
    /// <param name="dstMipLevel">The dst mip level value used by this operation.</param>
    /// <param name="dstBaseArrayLayer">The dst base array layer value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="layerCount">The layer count value used by this operation.</param>
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

                this._bce.CopyFromBuffer(srcBuffer, (UIntPtr)sourceOffset, srcRowPitch, srcDepthPitch, sourceSize, dstTexture, dstBaseArrayLayer + layer, dstMipLevel, new MTLOrigin(dstX, dstY, dstZ), this.gd.MetalFeatures.IsMacOS);
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
                            this._bce.Copy(srcMtlTexture.StagingBuffer, (UIntPtr)srcRowOffset, dstMtlTexture.StagingBuffer, (UIntPtr)dstRowOffset, copySize);
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
                            this._bce.Copy(srcMtlTexture.StagingBuffer, (UIntPtr)srcRowOffset, dstMtlTexture.StagingBuffer, (UIntPtr)dstRowOffset, rowPitch);
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

                this._bce.CopyTextureToBuffer(srcMtlTexture.DeviceTexture, srcBaseArrayLayer + layer, srcMipLevel, srcOrigin, srcSize, dstMtlTexture.StagingBuffer, (UIntPtr)dstOffset, dstBytesPerRow, dstBytesPerImage);
            }
        }
        else {
            // Normal -> Normal
            for (uint layer = 0; layer < layerCount; layer++) {
                this._bce.CopyFromTexture(srcMtlTexture.DeviceTexture, srcBaseArrayLayer + layer, srcMipLevel, new MTLOrigin(srcX, srcY, srcZ), new MTLSize(width, height, depth), dstMtlTexture.DeviceTexture, dstBaseArrayLayer + layer, dstMipLevel, new MTLOrigin(dstX, dstY, dstZ), this.gd.MetalFeatures.IsMacOS);
            }
        }
    }

    /// <summary>
    /// Executes the dispatch indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset) {
        MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(indirectBuffer);
        this.PreComputeCommand();
        this._cce.DispatchThreadgroupsWithIndirectBuffer(mtlBuffer.DeviceBuffer, offset, this._computePipeline.ThreadsPerThreadgroup);
    }

    /// <summary>
    /// Executes the draw indexed indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="drawCount">The draw count value used by this operation.</param>
    /// <param name="stride">The stride value used by this operation.</param>
    protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        if (this.PreDrawCommand()) {
            MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(indirectBuffer);

            for (uint i = 0; i < drawCount; i++) {
                uint currentOffset = i * stride + offset;
                this._rce.DrawIndexedPrimitives(this._graphicsPipeline.PrimitiveType, this._indexType, this._indexBuffer.DeviceBuffer, this._ibOffset, mtlBuffer.DeviceBuffer, currentOffset);
            }
        }
    }

    /// <summary>
    /// Executes the draw indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="drawCount">The draw count value used by this operation.</param>
    /// <param name="stride">The stride value used by this operation.</param>
    protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        if (this.PreDrawCommand()) {
            MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(indirectBuffer);

            for (uint i = 0; i < drawCount; i++) {
                uint currentOffset = i * stride + offset;
                this._rce.DrawPrimitives(this._graphicsPipeline.PrimitiveType, mtlBuffer.DeviceBuffer, currentOffset);
            }
        }
    }

    /// <summary>
    /// Executes the resolve texture core logic for this backend.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="destination">The destination value or resource.</param>
    protected override void ResolveTextureCore(Texture source, Texture destination) {
        // TODO: This approach destroys the contents of the source Texture (according to the docs).
        this.EnsureNoBlitEncoder();
        this.EnsureNoRenderPass();

        MtlTexture mtlSrc = Util.AssertSubtype<Texture, MtlTexture>(source);
        MtlTexture mtlDst = Util.AssertSubtype<Texture, MtlTexture>(destination);

        MTLRenderPassDescriptor rpDesc = MTLRenderPassDescriptor.New();
        MTLRenderPassColorAttachmentDescriptor colorAttachment = rpDesc.ColorAttachments[0];
        colorAttachment.texture = mtlSrc.DeviceTexture;
        colorAttachment.LoadAction = MTLLoadAction.Load;
        colorAttachment.StoreAction = MTLStoreAction.MultisampleResolve;
        colorAttachment.ResolveTexture = mtlDst.DeviceTexture;

        using (NSAutoreleasePool.Begin()) {
            MTLRenderCommandEncoder encoder = this.cb.RenderCommandEncoderWithDescriptor(rpDesc);
            encoder.EndEncoding();
        }

        ObjectiveCRuntime.Release(rpDesc.NativePtr);
    }

    /// <summary>
    /// Sets the compute resource set core value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="dynamicOffsetCount">The dynamic offset count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetCount, ref uint dynamicOffsets) {
        if (!this._computeResourceSets[slot].Equals(set, dynamicOffsetCount, ref dynamicOffsets)) {
            this._computeResourceSets[slot].Offsets.Dispose();
            this._computeResourceSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetCount, ref dynamicOffsets);
            this._computeResourceSetsActive[slot] = false;
        }
    }

    /// <summary>
    /// Sets the framebuffer core value.
    /// </summary>
    /// <param name="fb">The fb value used by this operation.</param>
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
    /// Sets the graphics resource set core value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="rs">The rs value used by this operation.</param>
    /// <param name="dynamicOffsetCount">The dynamic offset count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetCount, ref uint dynamicOffsets) {
        if (!this._graphicsResourceSets[slot].Equals(rs, dynamicOffsetCount, ref dynamicOffsets)) {
            this._graphicsResourceSets[slot].Offsets.Dispose();
            this._graphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetCount, ref dynamicOffsets);
            this._graphicsResourceSetsActive[slot] = false;
        }
    }

    /// <summary>
    /// Executes the pre draw command logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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
                this._rce.SetRenderPipelineState(this._graphicsPipeline.RenderPipelineState);
            }

            if (this._graphicsPipeline.CullMode != this._lastGraphicsPipeline?.CullMode) {
                this._rce.SetCullMode(this._graphicsPipeline.CullMode);
            }

            if (this._graphicsPipeline.FrontFace != this._lastGraphicsPipeline?.FrontFace) {
                this._rce.SetFrontFacing(this._graphicsPipeline.FrontFace);
            }

            if (this._graphicsPipeline.FillMode != this._lastGraphicsPipeline?.FillMode) {
                this._rce.SetTriangleFillMode(this._graphicsPipeline.FillMode);
            }

            RgbaFloat blendColor = this._graphicsPipeline.BlendColor;
            if (blendColor != this._lastGraphicsPipeline?.BlendColor) {
                this._rce.SetBlendColor(blendColor.R, blendColor.G, blendColor.B, blendColor.A);
            }

            if (this.Framebuffer.DepthTarget != null) {
                if (this._graphicsPipeline.DepthStencilState.NativePtr != this._lastGraphicsPipeline?.DepthStencilState.NativePtr) {
                    this._rce.SetDepthStencilState(this._graphicsPipeline.DepthStencilState);
                }

                if (this._graphicsPipeline.DepthClipMode != this._lastGraphicsPipeline?.DepthClipMode) {
                    this._rce.SetDepthClipMode(this._graphicsPipeline.DepthClipMode);
                }

                if (this._graphicsPipeline.StencilReference != this._lastGraphicsPipeline?.StencilReference) {
                    this._rce.SetStencilReferenceValue(this._graphicsPipeline.StencilReference);
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
                    this._rce.SetVertexBuffer(this._vertexBuffers[i].DeviceBuffer, this._vbOffsets[i], index);

                    this._vertexBuffersActive[i] = true;
                    this._vbOffsetsActive[i] = true;
                }

                if (!this._vbOffsetsActive[i]) {
                    UIntPtr index = this._graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                        ? this._nonVertexBufferCount + i
                        : i;

                    this._rce.SetVertexBufferOffset(this._vbOffsets[i], index);

                    this._vbOffsetsActive[i] = true;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Executes the flush viewports logic for this backend.
    /// </summary>
    private void FlushViewports() {
        if (this.gd.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3)) {
            fixed (MTLViewport* viewportsPtr = &this._viewports[0]) {
                this._rce.SetViewports(viewportsPtr, this._viewportCount);
            }
        }
        else {
            this._rce.SetViewport(this._viewports[0]);
        }
    }

    /// <summary>
    /// Executes the flush scissor rects logic for this backend.
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
                    this._rce.SetScissorRects(scissorRectsPtr, this._viewportCount);
                }
            }
        }
        else {
            if (!this._scissorRects[0].Equals(this._activeScissorRects[0])) {
                this._rce.SetScissorRect(this._scissorRects[0]);
            }

            this._activeScissorRects[0] = this._scissorRects[0];
        }
    }

    /// <summary>
    /// Executes the pre compute command logic for this backend.
    /// </summary>
    private void PreComputeCommand() {
        this.EnsureComputeEncoder();

        if (this._computePipeline.ComputePipelineState.NativePtr != this._lastComputePipeline?.ComputePipelineState.NativePtr) {
            this._cce.SetComputePipelineState(this._computePipeline.ComputePipelineState);
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
    /// Gets the free staging buffer value.
    /// </summary>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the activate graphics resource set logic for this backend.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="brsi">The brsi value used by this operation.</param>
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
    /// Executes the activate compute resource set logic for this backend.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="brsi">The brsi value used by this operation.</param>
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
    /// Binds the buffer resources for subsequent commands.
    /// </summary>
    /// <param name="range">The range value used by this operation.</param>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="stages">The stages value used by this operation.</param>
    private void BindBuffer(DeviceBufferRange range, uint set, uint slot, ShaderStages stages) {
        MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(range.Buffer);
        uint baseBuffer = this.GetBufferBase(set, stages != ShaderStages.Compute);

        if (stages == ShaderStages.Compute) {
            UIntPtr index = slot + baseBuffer;

            if (!this._boundComputeBuffers.TryGetValue(index, out DeviceBufferRange boundBuffer) || !range.Equals(boundBuffer)) {
                this._cce.SetBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);
                this._boundComputeBuffers[index] = range;
            }
        }
        else {
            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex) {
                UIntPtr index = this._graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                    ? slot + baseBuffer
                    : slot + this._vertexBufferCount + baseBuffer;

                if (!this._boundVertexBuffers.TryGetValue(index, out DeviceBufferRange boundBuffer) || boundBuffer.Buffer != range.Buffer) {
                    this._rce.SetVertexBuffer(mtlBuffer.DeviceBuffer, range.Offset, index);
                    this._boundVertexBuffers[index] = range;
                }
                else if (!range.Equals(boundBuffer)) {
                    this._rce.SetVertexBufferOffset(range.Offset, index);
                    this._boundVertexBuffers[index] = range;
                }
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment) {
                UIntPtr index = slot + baseBuffer;

                if (!this._boundFragmentBuffers.TryGetValue(index, out DeviceBufferRange boundBuffer) || boundBuffer.Buffer != range.Buffer) {
                    this._rce.SetFragmentBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);
                    this._boundFragmentBuffers[index] = range;
                }
                else if (!range.Equals(boundBuffer)) {
                    this._rce.SetFragmentBufferOffset(range.Offset, slot + baseBuffer);
                    this._boundFragmentBuffers[index] = range;
                }
            }
        }
    }

    /// <summary>
    /// Binds the texture resources for subsequent commands.
    /// </summary>
    /// <param name="mtlTexView">The mtl tex view value used by this operation.</param>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="stages">The stages value used by this operation.</param>
    private void BindTexture(MtlTextureView mtlTexView, uint set, uint slot, ShaderStages stages) {
        uint baseTexture = this.GetTextureBase(set, stages != ShaderStages.Compute);
        UIntPtr index = slot + baseTexture;

        if (stages == ShaderStages.Compute && (!this._boundComputeTextures.TryGetValue(index, out MTLTexture computeTexture) || computeTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr)) {
            this._cce.SetTexture(mtlTexView.TargetDeviceTexture, index);
            this._boundComputeTextures[index] = mtlTexView.TargetDeviceTexture;
        }

        if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex
            && (!this._boundVertexTextures.TryGetValue(index, out MTLTexture vertexTexture) || vertexTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr)) {
            this._rce.SetVertexTexture(mtlTexView.TargetDeviceTexture, index);
            this._boundVertexTextures[index] = mtlTexView.TargetDeviceTexture;
        }

        if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment
            && (!this._boundFragmentTextures.TryGetValue(index, out MTLTexture fragmentTexture) || fragmentTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr)) {
            this._rce.SetFragmentTexture(mtlTexView.TargetDeviceTexture, index);
            this._boundFragmentTextures[index] = mtlTexView.TargetDeviceTexture;
        }
    }

    /// <summary>
    /// Binds the sampler resources for subsequent commands.
    /// </summary>
    /// <param name="mtlSampler">The mtl sampler value used by this operation.</param>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="stages">The stages value used by this operation.</param>
    private void BindSampler(MtlSampler mtlSampler, uint set, uint slot, ShaderStages stages) {
        uint baseSampler = this.GetSamplerBase(set, stages != ShaderStages.Compute);
        UIntPtr index = slot + baseSampler;

        if (stages == ShaderStages.Compute && (!this._boundComputeSamplers.TryGetValue(index, out MTLSamplerState computeSampler) || computeSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr)) {
            this._cce.SetSamplerState(mtlSampler.DeviceSampler, index);
            this._boundComputeSamplers[index] = mtlSampler.DeviceSampler;
        }

        if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex
            && (!this._boundVertexSamplers.TryGetValue(index, out MTLSamplerState vertexSampler) || vertexSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr)) {
            this._rce.SetVertexSamplerState(mtlSampler.DeviceSampler, index);
            this._boundVertexSamplers[index] = mtlSampler.DeviceSampler;
        }

        if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment
            && (!this._boundFragmentSamplers.TryGetValue(index, out MTLSamplerState fragmentSampler) || fragmentSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr)) {
            this._rce.SetFragmentSamplerState(mtlSampler.DeviceSampler, index);
            this._boundFragmentSamplers[index] = mtlSampler.DeviceSampler;
        }
    }

    /// <summary>
    /// Gets the buffer base value.
    /// </summary>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="graphics">The graphics value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Gets the texture base value.
    /// </summary>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="graphics">The graphics value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Gets the sampler base value.
    /// </summary>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="graphics">The graphics value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the ensure render pass logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool EnsureRenderPass() {
        Debug.Assert(this._mtlFramebuffer != null);
        this.EnsureNoBlitEncoder();
        this.EnsureNoComputeEncoder();
        return this.renderEncoderActive || this.BeginCurrentRenderPass();
    }

    /// <summary>
    /// Begins the current render pass operation.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool BeginCurrentRenderPass() {
        if (this._mtlFramebuffer is MtlSwapchainFramebuffer swapchainFramebuffer && !swapchainFramebuffer.EnsureDrawableAvailable()) {
            return false;
        }

        MTLRenderPassDescriptor rpDesc = this._mtlFramebuffer.CreateRenderPassDescriptor();

        for (uint i = 0; i < this._clearColors.Length; i++) {
            if (this._clearColors[i] != null) {
                MTLRenderPassColorAttachmentDescriptor attachment = rpDesc.ColorAttachments[0];
                attachment.LoadAction = MTLLoadAction.Clear;
                RgbaFloat c = this._clearColors[i].Value;
                attachment.ClearColor = new MTLClearColor(c.R, c.G, c.B, c.A);
                this._clearColors[i] = null;
            }
        }

        if (this._clearDepth != null) {
            MTLRenderPassDepthAttachmentDescriptor depthAttachment = rpDesc.DepthAttachment;
            depthAttachment.LoadAction = MTLLoadAction.Clear;
            depthAttachment.ClearDepth = this._clearDepth.Value.depth;

            if (this._mtlFramebuffer.DepthTarget != null && FormatHelpers.IsStencilFormat(this._mtlFramebuffer.DepthTarget.Value.Target.Format)) {
                MTLRenderPassStencilAttachmentDescriptor stencilAttachment = rpDesc.StencilAttachment;
                stencilAttachment.LoadAction = MTLLoadAction.Clear;
                stencilAttachment.ClearStencil = this._clearDepth.Value.stencil;
            }

            this._clearDepth = null;
        }

        using (NSAutoreleasePool.Begin()) {
            this._rce = this.cb.RenderCommandEncoderWithDescriptor(rpDesc);
            ObjectiveCRuntime.Retain(this._rce.NativePtr);
        }

        ObjectiveCRuntime.Release(rpDesc.NativePtr);
        this._currentFramebufferEverActive = true;

        return true;
    }

    /// <summary>
    /// Executes the ensure no render pass logic for this backend.
    /// </summary>
    private void EnsureNoRenderPass() {
        if (this.renderEncoderActive) {
            this.EndCurrentRenderPass();
        }

        Debug.Assert(!this.renderEncoderActive);
    }

    /// <summary>
    /// Ends the current render pass operation.
    /// </summary>
    private void EndCurrentRenderPass() {
        this._rce.EndEncoding();
        ObjectiveCRuntime.Release(this._rce.NativePtr);
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
    /// Executes the ensure blit encoder logic for this backend.
    /// </summary>
    private void EnsureBlitEncoder() {
        if (!this.blitEncoderActive) {
            this.EnsureNoRenderPass();
            this.EnsureNoComputeEncoder();

            using (NSAutoreleasePool.Begin()) {
                this._bce = this.cb.BlitCommandEncoder();
                ObjectiveCRuntime.Retain(this._bce.NativePtr);
            }
        }

        Debug.Assert(this.blitEncoderActive);
        Debug.Assert(!this.renderEncoderActive);
        Debug.Assert(!this.computeEncoderActive);
    }

    /// <summary>
    /// Executes the ensure no blit encoder logic for this backend.
    /// </summary>
    private void EnsureNoBlitEncoder() {
        if (this.blitEncoderActive) {
            this._bce.EndEncoding();
            ObjectiveCRuntime.Release(this._bce.NativePtr);
            this._bce = default;
        }

        Debug.Assert(!this.blitEncoderActive);
    }

    /// <summary>
    /// Executes the ensure compute encoder logic for this backend.
    /// </summary>
    private void EnsureComputeEncoder() {
        if (!this.computeEncoderActive) {
            this.EnsureNoBlitEncoder();
            this.EnsureNoRenderPass();

            using (NSAutoreleasePool.Begin()) {
                this._cce = this.cb.ComputeCommandEncoder();
                ObjectiveCRuntime.Retain(this._cce.NativePtr);
            }
        }

        Debug.Assert(this.computeEncoderActive);
        Debug.Assert(!this.renderEncoderActive);
        Debug.Assert(!this.blitEncoderActive);
    }

    /// <summary>
    /// Executes the ensure no compute encoder logic for this backend.
    /// </summary>
    private void EnsureNoComputeEncoder() {
        if (this.computeEncoderActive) {
            this._cce.EndEncoding();
            ObjectiveCRuntime.Release(this._cce.NativePtr);
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
    /// Executes the clear color target core logic for this backend.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="clearColor">The clear color value used by this operation.</param>
    private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor) {
        this.EnsureNoRenderPass();
        this._clearColors[index] = clearColor;
    }

    /// <summary>
    /// Executes the clear depth stencil core logic for this backend.
    /// </summary>
    /// <param name="depth">The depth value.</param>
    /// <param name="stencil">The stencil value used by this operation.</param>
    private protected override void ClearDepthStencilCore(float depth, byte stencil) {
        this.EnsureNoRenderPass();
        this._clearDepth = (depth, stencil);
    }

    /// <summary>
    /// Executes the draw core logic for this backend.
    /// </summary>
    /// <param name="vertexCount">The vertex count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="vertexStart">The vertex start value used by this operation.</param>
    /// <param name="instanceStart">The instance start value used by this operation.</param>
    private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart) {
        if (this.PreDrawCommand()) {
            if (instanceStart == 0) {
                this._rce.DrawPrimitives(this._graphicsPipeline.PrimitiveType, vertexStart, vertexCount, instanceCount);
            }
            else {
                this._rce.DrawPrimitives(this._graphicsPipeline.PrimitiveType, vertexStart, vertexCount, instanceCount, instanceStart);
            }
        }
    }

    /// <summary>
    /// Executes the draw indexed core logic for this backend.
    /// </summary>
    /// <param name="indexCount">The index count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="indexStart">The index start value used by this operation.</param>
    /// <param name="vertexOffset">The vertex offset value used by this operation.</param>
    /// <param name="instanceStart">The instance start value used by this operation.</param>
    private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart) {
        if (this.PreDrawCommand()) {
            uint indexSize = this._indexType == MTLIndexType.UInt16 ? 2u : 4u;
            uint indexBufferOffset = indexSize * indexStart + this._ibOffset;

            if (vertexOffset == 0 && instanceStart == 0) {
                this._rce.DrawIndexedPrimitives(this._graphicsPipeline.PrimitiveType, indexCount, this._indexType, this._indexBuffer.DeviceBuffer, indexBufferOffset, instanceCount);
            }
            else {
                this._rce.DrawIndexedPrimitives(this._graphicsPipeline.PrimitiveType, indexCount, this._indexType, this._indexBuffer.DeviceBuffer, indexBufferOffset, instanceCount, vertexOffset, instanceStart);
            }
        }
    }

    /// <summary>
    /// Sets the pipeline core value.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    private protected override void SetPipelineCore(Pipeline pipeline) {
        if (pipeline.IsComputePipeline && this._computePipeline != pipeline) {
            this._computePipeline = Util.AssertSubtype<Pipeline, MtlPipeline>(pipeline);
            this._computeResourceSetCount = (uint)this._computePipeline.ResourceLayouts.Length;
            Util.EnsureArrayMinimumSize(ref this._computeResourceSets, this._computeResourceSetCount);
            Util.EnsureArrayMinimumSize(ref this._computeResourceSetsActive, this._computeResourceSetCount);
            Util.ClearArray(this._computeResourceSetsActive);
            this._pushConstantsUseComputePipeline = true;
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
            this._pushConstantsUseComputePipeline = false;
        }
    }

    /// <summary>
    /// Updates the buffer core state for this command sequence.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
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
            this._bce.Copy(staging.DeviceBuffer, UIntPtr.Zero, dstMtlBuffer.DeviceBuffer, bufferOffsetInBytes, sizeInBytes + sizeRoundFactor);
        }

        lock (this._submittedCommandsLock) {
            this._submittedStagingBuffers.Add(this.cb, staging);
        }
    }

    /// <summary>
    /// Executes the generate mipmaps core logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    private protected override void GenerateMipmapsCore(Texture texture) {
        Debug.Assert(texture.MipLevels > 1);
        this.EnsureBlitEncoder();
        MtlTexture mtlTex = Util.AssertSubtype<Texture, MtlTexture>(texture);
        this._bce.GenerateMipmapsForTexture(mtlTex.DeviceTexture);
    }

    /// <summary>
    /// Sets the index buffer core value.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset) {
        this._indexBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
        this._ibOffset = offset;
        this._indexType = MtlFormats.VdToMtlIndexFormat(format);
    }

    /// <summary>
    /// Sets the vertex buffer core value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
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
    /// Executes the push debug group core logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private protected override void PushDebugGroupCore(string name) {
        NSString nsName = NSString.New(name);
        if (!this._bce.IsNull) {
            this._bce.PushDebugGroup(nsName);
        }
        else if (!this._cce.IsNull) {
            this._cce.PushDebugGroup(nsName);
        }
        else if (!this._rce.IsNull) {
            this._rce.PushDebugGroup(nsName);
        }

        ObjectiveCRuntime.Release(nsName);
    }

    /// <summary>
    /// Executes the pop debug group core logic for this backend.
    /// </summary>
    private protected override void PopDebugGroupCore() {
        if (!this._bce.IsNull) {
            this._bce.PopDebugGroup();
        }
        else if (!this._cce.IsNull) {
            this._cce.PopDebugGroup();
        }
        else if (!this._rce.IsNull) {
            this._rce.PopDebugGroup();
        }
    }

    /// <summary>
    /// Executes the insert debug marker core logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private protected override void InsertDebugMarkerCore(string name) {
        NSString nsName = NSString.New(name);
        if (!this._bce.IsNull) {
            this._bce.InsertDebugSignpost(nsName);
        }
        else if (!this._cce.IsNull) {
            this._cce.InsertDebugSignpost(nsName);
        }
        else if (!this._rce.IsNull) {
            this._rce.InsertDebugSignpost(nsName);
        }

        ObjectiveCRuntime.Release(nsName);
    }

    /// <summary>
    /// Uploads backend-specific push-constant data to the active pipeline.
    /// </summary>
    /// <param name="offset">The byte offset inside the push-constant range.</param>
    /// <param name="data">A pointer to source data.</param>
    /// <param name="sizeInBytes">The number of bytes to upload.</param>
    private protected override unsafe void PushConstantsCore(uint offset, IntPtr data, uint sizeInBytes) {
        MtlPipeline pipeline = this._pushConstantsUseComputePipeline ? this._computePipeline : this._graphicsPipeline;
        if (pipeline == null) {
            throw new VeldridException("A Metal pipeline must be bound before push constants can be set.");
        }

        if (offset + sizeInBytes > pipeline.MaxPushConstantSizeInBytes) {
            throw new VeldridException($"Push constants exceed the backend limit of {pipeline.MaxPushConstantSizeInBytes} bytes.");
        }

        uint totalSize = offset + sizeInBytes;
        byte* pushConstantData = stackalloc byte[(int)totalSize];
        Unsafe.InitBlock(pushConstantData, 0, totalSize);
        Unsafe.CopyBlock(pushConstantData + offset, (void*)data, sizeInBytes);

        if (this._pushConstantsUseComputePipeline) {
            this.EnsureComputeEncoder();
            this._cce.SetBytes(pushConstantData, (UIntPtr)totalSize, pipeline.ComputePushConstantSlot);
        }
        else {
            if (!this.EnsureRenderPass()) {
                throw new VeldridException("A render pass must be active before graphics push constants can be set on Metal.");
            }

            this._rce.SetVertexBytes(pushConstantData, (UIntPtr)totalSize, pipeline.VertexPushConstantSlot);
            this._rce.SetFragmentBytes(pushConstantData, (UIntPtr)totalSize, pipeline.FragmentPushConstantSlot);
        }
    }
}
