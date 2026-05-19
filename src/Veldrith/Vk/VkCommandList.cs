using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.RawConstants;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkCommandList.
/// </summary>
internal unsafe class VkCommandList : CommandList {

    /// <summary>
    /// Stores the available command buffers collection used by this instance.
    /// </summary>
    private readonly Queue<VkCommandBuffer> _availableCommandBuffers = new();

    /// <summary>
    /// Stores the available staging buffers collection used by this instance.
    /// </summary>
    private readonly List<VkBuffer> _availableStagingBuffers = new();

    /// <summary>
    /// Stores the available staging infos state used by this instance.
    /// </summary>
    private readonly List<StagingResourceInfo> _availableStagingInfos = new();

    /// <summary>
    /// Stores the command buffer list lock collection used by this instance.
    /// </summary>
    private readonly object _commandBufferListLock = new();

    /// <summary>
    /// Stores the pool state used by this instance.
    /// </summary>
    private readonly VkCommandPool _pool;

    /// <summary>
    /// Stores the pre draw sampled images state used by this instance.
    /// </summary>
    private readonly List<VkTexture> _preDrawSampledImages = new();

    /// <summary>
    /// Synchronizes access to the staging lock state.
    /// </summary>
    private readonly object _stagingLock = new();

    /// <summary>
    /// Stores the submitted command buffers collection used by this instance.
    /// </summary>
    private readonly List<VkCommandBuffer> _submittedCommandBuffers = new();

    /// <summary>
    /// Stores the submitted staging infos state used by this instance.
    /// </summary>
    private readonly Dictionary<VkCommandBuffer, StagingResourceInfo> _submittedStagingInfos = new();

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the active render pass state used by this instance.
    /// </summary>
    private VkRenderPass _activeRenderPass;

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private VkClearValue[] _clearValues = Array.Empty<VkClearValue>();

    /// <summary>
    /// Stores the command buffer begun state used by this instance.
    /// </summary>
    private bool _commandBufferBegun;

    /// <summary>
    /// Stores the command buffer ended state used by this instance.
    /// </summary>
    private bool _commandBufferEnded;

    /// <summary>
    /// Stores the compute resource sets changed collection used by this instance.
    /// </summary>
    private bool[] _computeResourceSetsChanged;

    // Compute State

    /// <summary>
    /// Stores the current compute pipeline state used by this instance.
    /// </summary>
    private VkPipeline _currentComputePipeline;

    /// <summary>
    /// Stores the current compute resource sets collection used by this instance.
    /// </summary>
    private BoundResourceSetInfo[] _currentComputeResourceSets = Array.Empty<BoundResourceSetInfo>();

    // Graphics State

    /// <summary>
    /// Stores the current framebuffer state used by this instance.
    /// </summary>
    private VkFramebufferBase _currentFramebuffer;

    /// <summary>
    /// Stores the current framebuffer ever active state used by this instance.
    /// </summary>
    private bool _currentFramebufferEverActive;

    /// <summary>
    /// Stores the current graphics pipeline state used by this instance.
    /// </summary>
    private VkPipeline _currentGraphicsPipeline;

    /// <summary>
    /// Stores the current graphics resource sets collection used by this instance.
    /// </summary>
    private BoundResourceSetInfo[] _currentGraphicsResourceSets = Array.Empty<BoundResourceSetInfo>();

    /// <summary>
    /// Stores the current staging info state used by this instance.
    /// </summary>
    private StagingResourceInfo _currentStagingInfo;

    /// <summary>
    /// Stores the depth clear value value used during command execution.
    /// </summary>
    private VkClearValue? _depthClearValue;

    /// <summary>
    /// Stores the destroyed state used by this instance.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the graphics resource sets changed collection used by this instance.
    /// </summary>
    private bool[] _graphicsResourceSetsChanged;

    /// <summary>
    /// Stores the new framebuffer state used by this instance.
    /// </summary>
    private bool _newFramebuffer; // Render pass cycle state

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private VkRect2D[] _scissorRects = Array.Empty<VkRect2D>();

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private bool[] _validColorClearValues = Array.Empty<bool>();

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkCommandList" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public VkCommandList(VkGraphicsDevice gd, ref CommandListDescription description) : base(ref description, gd.Features, gd.UniformBufferMinOffsetAlignment, gd.StructuredBufferMinOffsetAlignment) {
        this.gd = gd;
        VkCommandPoolCreateInfo poolCi = VkCommandPoolCreateInfo.New();
        poolCi.flags = VkCommandPoolCreateFlags.ResetCommandBuffer;
        poolCi.queueFamilyIndex = gd.GraphicsQueueIndex;
        VkResult result = vkCreateCommandPool(this.gd.Device, ref poolCi, null, out this._pool);
        CheckResult(result);

        this.CommandBuffer = this.GetNextCommandBuffer();
        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    /// <summary>
    /// Stores the command pool state used by this instance.
    /// </summary>
    public VkCommandPool CommandPool => this._pool;

    /// <summary>
    /// Gets or sets CommandBuffer.
    /// </summary>
    public VkCommandBuffer CommandBuffer { get; private set; }

    /// <summary>
    /// Gets or sets RefCount.
    /// </summary>
    public ResourceRefCount RefCount { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._destroyed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name {
        get => this.name;
        set {
            this.name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes the command buffer submitted logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    public void CommandBufferSubmitted(VkCommandBuffer cb) {
        this.RefCount.Increment();
        foreach (ResourceRefCount rrc in this._currentStagingInfo.Resources) {
            rrc.Increment();
        }

        this._submittedStagingInfos.Add(cb, this._currentStagingInfo);
        this._currentStagingInfo = null;
    }

    /// <summary>
    /// Executes the command buffer completed logic for this backend.
    /// </summary>
    /// <param name="completedCb">The completed cb value used by this operation.</param>
    public void CommandBufferCompleted(VkCommandBuffer completedCb) {
        lock (this._commandBufferListLock) {
            for (int i = 0; i < this._submittedCommandBuffers.Count; i++) {
                VkCommandBuffer submittedCb = this._submittedCommandBuffers[i];

                if (submittedCb == completedCb) {
                    this._availableCommandBuffers.Enqueue(completedCb);
                    this._submittedCommandBuffers.RemoveAt(i);
                    i -= 1;
                }
            }
        }

        lock (this._stagingLock) {
            if (this._submittedStagingInfos.TryGetValue(completedCb, out StagingResourceInfo info)) {
                this.RecycleStagingInfo(info);
                this._submittedStagingInfos.Remove(completedCb);
            }
        }

        this.RefCount.Decrement();
    }

    /// <summary>
    /// Begins the value operation.
    /// </summary>
    public override void Begin() {
        if (this._commandBufferBegun) {
            throw new VeldridException("CommandList must be in its initial state, or End() must have been called, for Begin() to be valid to call.");
        }

        if (this._commandBufferEnded) {
            this._commandBufferEnded = false;
            this.CommandBuffer = this.GetNextCommandBuffer();
            if (this._currentStagingInfo != null) {
                this.RecycleStagingInfo(this._currentStagingInfo);
            }
        }

        this._currentStagingInfo = this.GetStagingResourceInfo();

        VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
        beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;
        vkBeginCommandBuffer(this.CommandBuffer, ref beginInfo);
        this._commandBufferBegun = true;

        this.ClearCachedState();
        this._currentFramebuffer = null;
        this._currentGraphicsPipeline = null;
        this.ClearSets(this._currentGraphicsResourceSets);
        Util.ClearArray(this._scissorRects);

        this._currentComputePipeline = null;
        this.ClearSets(this._currentComputeResourceSets);
    }

    /// <summary>
    /// Executes the dispatch logic for this backend.
    /// </summary>
    /// <param name="groupCountX">The group count x value used by this operation.</param>
    /// <param name="groupCountY">The group count y value used by this operation.</param>
    /// <param name="groupCountZ">The group count z value used by this operation.</param>
    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ) {
        this.PreDispatchCommand();

        vkCmdDispatch(this.CommandBuffer, groupCountX, groupCountY, groupCountZ);
    }

    /// <summary>
    /// Ends the value operation.
    /// </summary>
    public override void End() {
        if (!this._commandBufferBegun) {
            throw new VeldridException("CommandBuffer must have been started before End() may be called.");
        }

        this._commandBufferBegun = false;
        this._commandBufferEnded = true;

        if (!this._currentFramebufferEverActive && this._currentFramebuffer != null) {
            this.BeginCurrentRenderPass();
        }

        if (this._activeRenderPass != VkRenderPass.Null) {
            this.EndCurrentRenderPass();
            this._currentFramebuffer!.TransitionToFinalLayout(this.CommandBuffer);
        }

        vkEndCommandBuffer(this.CommandBuffer);
        this._submittedCommandBuffers.Add(this.CommandBuffer);
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
        if (index == 0 || this.gd.Features.MultipleViewports) {
            VkRect2D scissor = new((int)x, (int)y, (int)width, (int)height);

            if (this._scissorRects[index] != scissor) {
                this._scissorRects[index] = scissor;
                vkCmdSetScissor(this.CommandBuffer, index, 1, ref scissor);
            }
        }
    }

    /// <summary>
    /// Sets the viewport value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="viewport">The viewport value used by this operation.</param>
    public override void SetViewport(uint index, ref Viewport viewport) {
        if (index == 0 || this.gd.Features.MultipleViewports) {
            float vpY = this.gd.IsClipSpaceYInverted
                ? viewport.Y
                : viewport.Height + viewport.Y;
            float vpHeight = this.gd.IsClipSpaceYInverted
                ? viewport.Height
                : -viewport.Height;

            VkViewport vkViewport = new() {
                x = viewport.X,
                y = vpY,
                width = viewport.Width,
                height = vpHeight,
                minDepth = viewport.MinDepth,
                maxDepth = viewport.MaxDepth
            };

            vkCmdSetViewport(this.CommandBuffer, index, 1, ref vkViewport);
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
        VkBuffer stagingBuffer = this.GetStagingBuffer(sizeInBytes);
        this.gd.UpdateBuffer(stagingBuffer, 0, source, sizeInBytes);
        this.CopyBuffer(stagingBuffer, 0, buffer, bufferOffsetInBytes, sizeInBytes);
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
        this.EnsureNoRenderPass();

        VkBuffer srcVkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(source);
        this._currentStagingInfo.Resources.Add(srcVkBuffer.RefCount);
        VkBuffer dstVkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(destination);
        this._currentStagingInfo.Resources.Add(dstVkBuffer.RefCount);

        VkBufferCopy region = new() {
            srcOffset = sourceOffset,
            dstOffset = destinationOffset,
            size = sizeInBytes
        };

        vkCmdCopyBuffer(this.CommandBuffer, srcVkBuffer.DeviceBuffer, dstVkBuffer.DeviceBuffer, 1, ref region);

        bool needToProtectUniform = destination.Usage.HasFlag(BufferUsage.UniformBuffer);

        VkMemoryBarrier barrier;
        barrier.sType = VkStructureType.MemoryBarrier;
        barrier.srcAccessMask = VkAccessFlags.TransferWrite;
        barrier.dstAccessMask = needToProtectUniform ? VkAccessFlags.UniformRead : VkAccessFlags.VertexAttributeRead;
        barrier.pNext = null;
        vkCmdPipelineBarrier(this.CommandBuffer, VkPipelineStageFlags.Transfer, needToProtectUniform
                ? VkPipelineStageFlags.VertexShader | VkPipelineStageFlags.ComputeShader |
                  VkPipelineStageFlags.FragmentShader | VkPipelineStageFlags.GeometryShader |
                  VkPipelineStageFlags.TessellationControlShader | VkPipelineStageFlags.TessellationEvaluationShader
                : VkPipelineStageFlags.VertexInput, VkDependencyFlags.None, 1, ref barrier, 0, null, 0, null);
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
        this.EnsureNoRenderPass();
        CopyTextureCore_VkCommandBuffer(this.CommandBuffer, source, srcX, srcY, srcZ, srcMipLevel, srcBaseArrayLayer, destination, dstX, dstY, dstZ, dstMipLevel, dstBaseArrayLayer, width, height, depth, layerCount);

        VkTexture srcVkTexture = Util.AssertSubtype<Texture, VkTexture>(source);
        this._currentStagingInfo.Resources.Add(srcVkTexture.RefCount);
        VkTexture dstVkTexture = Util.AssertSubtype<Texture, VkTexture>(destination);
        this._currentStagingInfo.Resources.Add(dstVkTexture.RefCount);
    }

    /// <summary>
    /// Copies texture core vk command buffer data between resources.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
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
    internal static void CopyTextureCore_VkCommandBuffer(VkCommandBuffer cb, Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount) {
        VkTexture srcVkTexture = Util.AssertSubtype<Texture, VkTexture>(source);
        VkTexture dstVkTexture = Util.AssertSubtype<Texture, VkTexture>(destination);

        bool sourceIsStaging = (source.Usage & TextureUsage.Staging) == TextureUsage.Staging;
        bool destIsStaging = (destination.Usage & TextureUsage.Staging) == TextureUsage.Staging;

        if (!sourceIsStaging && !destIsStaging) {
            VkImageSubresourceLayers srcSubresource = new() {
                aspectMask = VkImageAspectFlags.Color,
                layerCount = layerCount,
                mipLevel = srcMipLevel,
                baseArrayLayer = srcBaseArrayLayer
            };

            VkImageSubresourceLayers dstSubresource = new() {
                aspectMask = VkImageAspectFlags.Color,
                layerCount = layerCount,
                mipLevel = dstMipLevel,
                baseArrayLayer = dstBaseArrayLayer
            };

            VkImageCopy region = new() {
                srcOffset = new VkOffset3D { x = (int)srcX, y = (int)srcY, z = (int)srcZ },
                dstOffset = new VkOffset3D { x = (int)dstX, y = (int)dstY, z = (int)dstZ },
                srcSubresource = srcSubresource,
                dstSubresource = dstSubresource,
                extent = new VkExtent3D { width = width, height = height, depth = depth }
            };

            srcVkTexture.TransitionImageLayout(cb, srcMipLevel, 1, srcBaseArrayLayer, layerCount, VkImageLayout.TransferSrcOptimal);

            dstVkTexture.TransitionImageLayout(cb, dstMipLevel, 1, dstBaseArrayLayer, layerCount, VkImageLayout.TransferDstOptimal);

            vkCmdCopyImage(cb, srcVkTexture.OptimalDeviceImage, VkImageLayout.TransferSrcOptimal, dstVkTexture.OptimalDeviceImage, VkImageLayout.TransferDstOptimal, 1, ref region);

            if ((srcVkTexture.Usage & TextureUsage.Sampled) != 0) {
                srcVkTexture.TransitionImageLayout(cb, srcMipLevel, 1, srcBaseArrayLayer, layerCount, VkImageLayout.ShaderReadOnlyOptimal);
            }

            if ((dstVkTexture.Usage & TextureUsage.Sampled) != 0) {
                dstVkTexture.TransitionImageLayout(cb, dstMipLevel, 1, dstBaseArrayLayer, layerCount, VkImageLayout.ShaderReadOnlyOptimal);
            }
        }
        else if (sourceIsStaging && !destIsStaging) {
            Vulkan.VkBuffer srcBuffer = srcVkTexture.StagingBuffer;
            VkSubresourceLayout srcLayout = srcVkTexture.GetSubresourceLayout(srcVkTexture.CalculateSubresource(srcMipLevel, srcBaseArrayLayer));
            VkImage dstImage = dstVkTexture.OptimalDeviceImage;
            dstVkTexture.TransitionImageLayout(cb, dstMipLevel, 1, dstBaseArrayLayer, layerCount, VkImageLayout.TransferDstOptimal);

            VkImageSubresourceLayers dstSubresource = new() {
                aspectMask = VkImageAspectFlags.Color,
                layerCount = layerCount,
                mipLevel = dstMipLevel,
                baseArrayLayer = dstBaseArrayLayer
            };

            Util.GetMipDimensions(srcVkTexture, srcMipLevel, out uint mipWidth, out uint mipHeight, out uint _);
            uint blockSize = FormatHelpers.IsCompressedFormat(srcVkTexture.Format) ? 4u : 1u;
            uint bufferRowLength = Math.Max(mipWidth, blockSize);
            uint bufferImageHeight = Math.Max(mipHeight, blockSize);
            uint compressedX = srcX / blockSize;
            uint compressedY = srcY / blockSize;
            uint blockSizeInBytes = blockSize == 1
                ? FormatSizeHelpers.GetSizeInBytes(srcVkTexture.Format)
                : FormatHelpers.GetBlockSizeInBytes(srcVkTexture.Format);
            uint rowPitch = FormatHelpers.GetRowPitch(bufferRowLength, srcVkTexture.Format);
            uint depthPitch = FormatHelpers.GetDepthPitch(rowPitch, bufferImageHeight, srcVkTexture.Format);

            uint copyWidth = Math.Min(width, mipWidth);
            uint copyheight = Math.Min(height, mipHeight);

            VkBufferImageCopy regions = new() {
                bufferOffset = srcLayout.offset
                               + srcZ * depthPitch
                               + compressedY * rowPitch
                               + compressedX * blockSizeInBytes,
                bufferRowLength = bufferRowLength,
                bufferImageHeight = bufferImageHeight,
                imageExtent = new VkExtent3D { width = copyWidth, height = copyheight, depth = depth },
                imageOffset = new VkOffset3D { x = (int)dstX, y = (int)dstY, z = (int)dstZ },
                imageSubresource = dstSubresource
            };

            vkCmdCopyBufferToImage(cb, srcBuffer, dstImage, VkImageLayout.TransferDstOptimal, 1, ref regions);

            if ((dstVkTexture.Usage & TextureUsage.Sampled) != 0) {
                dstVkTexture.TransitionImageLayout(cb, dstMipLevel, 1, dstBaseArrayLayer, layerCount, VkImageLayout.ShaderReadOnlyOptimal);
            }
        }
        else if (!sourceIsStaging) {
            VkImage srcImage = srcVkTexture.OptimalDeviceImage;
            srcVkTexture.TransitionImageLayout(cb, srcMipLevel, 1, srcBaseArrayLayer, layerCount, VkImageLayout.TransferSrcOptimal);

            Vulkan.VkBuffer dstBuffer = dstVkTexture.StagingBuffer;

            VkImageAspectFlags aspect = (srcVkTexture.Usage & TextureUsage.DepthStencil) != 0
                ? VkImageAspectFlags.Depth
                : VkImageAspectFlags.Color;

            Util.GetMipDimensions(dstVkTexture, dstMipLevel, out uint mipWidth, out uint mipHeight, out uint _);
            uint blockSize = FormatHelpers.IsCompressedFormat(srcVkTexture.Format) ? 4u : 1u;
            uint bufferRowLength = Math.Max(mipWidth, blockSize);
            uint bufferImageHeight = Math.Max(mipHeight, blockSize);
            uint compressedDstX = dstX / blockSize;
            uint compressedDstY = dstY / blockSize;
            uint blockSizeInBytes = blockSize == 1
                ? FormatSizeHelpers.GetSizeInBytes(dstVkTexture.Format)
                : FormatHelpers.GetBlockSizeInBytes(dstVkTexture.Format);
            uint rowPitch = FormatHelpers.GetRowPitch(bufferRowLength, dstVkTexture.Format);
            uint depthPitch = FormatHelpers.GetDepthPitch(rowPitch, bufferImageHeight, dstVkTexture.Format);

            VkBufferImageCopy* layers = stackalloc VkBufferImageCopy[(int)layerCount];

            for (uint layer = 0; layer < layerCount; layer++) {
                VkSubresourceLayout dstLayout = dstVkTexture.GetSubresourceLayout(dstVkTexture.CalculateSubresource(dstMipLevel, dstBaseArrayLayer + layer));

                VkImageSubresourceLayers srcSubresource = new() {
                    aspectMask = aspect,
                    layerCount = 1,
                    mipLevel = srcMipLevel,
                    baseArrayLayer = srcBaseArrayLayer + layer
                };

                VkBufferImageCopy region = new() {
                    bufferRowLength = bufferRowLength,
                    bufferImageHeight = bufferImageHeight,
                    bufferOffset = dstLayout.offset
                                   + dstZ * depthPitch
                                   + compressedDstY * rowPitch
                                   + compressedDstX * blockSizeInBytes,
                    imageExtent = new VkExtent3D { width = width, height = height, depth = depth },
                    imageOffset = new VkOffset3D { x = (int)srcX, y = (int)srcY, z = (int)srcZ },
                    imageSubresource = srcSubresource
                };

                layers[layer] = region;
            }

            vkCmdCopyImageToBuffer(cb, srcImage, VkImageLayout.TransferSrcOptimal, dstBuffer, layerCount, layers);

            if ((srcVkTexture.Usage & TextureUsage.Sampled) != 0) {
                srcVkTexture.TransitionImageLayout(cb, srcMipLevel, 1, srcBaseArrayLayer, layerCount, VkImageLayout.ShaderReadOnlyOptimal);
            }
        }
        else {
            Debug.Assert(sourceIsStaging && destIsStaging);
            Vulkan.VkBuffer srcBuffer = srcVkTexture.StagingBuffer;
            VkSubresourceLayout srcLayout = srcVkTexture.GetSubresourceLayout(srcVkTexture.CalculateSubresource(srcMipLevel, srcBaseArrayLayer));
            Vulkan.VkBuffer dstBuffer = dstVkTexture.StagingBuffer;
            VkSubresourceLayout dstLayout = dstVkTexture.GetSubresourceLayout(dstVkTexture.CalculateSubresource(dstMipLevel, dstBaseArrayLayer));

            uint zLimit = Math.Max(depth, layerCount);

            if (!FormatHelpers.IsCompressedFormat(source.Format)) {
                uint pixelSize = FormatSizeHelpers.GetSizeInBytes(srcVkTexture.Format);

                for (uint zz = 0; zz < zLimit; zz++) {
                    for (uint yy = 0; yy < height; yy++) {
                        VkBufferCopy region = new() {
                            srcOffset = srcLayout.offset
                                        + srcLayout.depthPitch * (zz + srcZ)
                                        + srcLayout.rowPitch * (yy + srcY)
                                        + pixelSize * srcX,
                            dstOffset = dstLayout.offset
                                        + dstLayout.depthPitch * (zz + dstZ)
                                        + dstLayout.rowPitch * (yy + dstY)
                                        + pixelSize * dstX,
                            size = width * pixelSize
                        };

                        vkCmdCopyBuffer(cb, srcBuffer, dstBuffer, 1, ref region);
                    }
                }
            }
            else // IsCompressedFormat
            {
                uint denseRowSize = FormatHelpers.GetRowPitch(width, source.Format);
                uint numRows = FormatHelpers.GetNumRows(height, source.Format);
                uint compressedSrcX = srcX / 4;
                uint compressedSrcY = srcY / 4;
                uint compressedDstX = dstX / 4;
                uint compressedDstY = dstY / 4;
                uint blockSizeInBytes = FormatHelpers.GetBlockSizeInBytes(source.Format);

                for (uint zz = 0; zz < zLimit; zz++) {
                    for (uint row = 0; row < numRows; row++) {
                        VkBufferCopy region = new() {
                            srcOffset = srcLayout.offset
                                        + srcLayout.depthPitch * (zz + srcZ)
                                        + srcLayout.rowPitch * (row + compressedSrcY)
                                        + blockSizeInBytes * compressedSrcX,
                            dstOffset = dstLayout.offset
                                        + dstLayout.depthPitch * (zz + dstZ)
                                        + dstLayout.rowPitch * (row + compressedDstY)
                                        + blockSizeInBytes * compressedDstX,
                            size = denseRowSize
                        };

                        vkCmdCopyBuffer(cb, srcBuffer, dstBuffer, 1, ref region);
                    }
                }
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
        this.PreDrawCommand();
        VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(indirectBuffer);
        this._currentStagingInfo.Resources.Add(vkBuffer.RefCount);
        vkCmdDrawIndirect(this.CommandBuffer, vkBuffer.DeviceBuffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Executes the draw indexed indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="drawCount">The draw count value used by this operation.</param>
    /// <param name="stride">The stride value used by this operation.</param>
    protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        this.PreDrawCommand();
        VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(indirectBuffer);
        this._currentStagingInfo.Resources.Add(vkBuffer.RefCount);
        vkCmdDrawIndexedIndirect(this.CommandBuffer, vkBuffer.DeviceBuffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Executes the dispatch indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset) {
        this.PreDispatchCommand();

        VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(indirectBuffer);
        this._currentStagingInfo.Resources.Add(vkBuffer.RefCount);
        vkCmdDispatchIndirect(this.CommandBuffer, vkBuffer.DeviceBuffer, offset);
    }

    /// <summary>
    /// Executes the resolve texture core logic for this backend.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="destination">The destination value or resource.</param>
    protected override void ResolveTextureCore(Texture source, Texture destination) {
        if (this._activeRenderPass != VkRenderPass.Null) {
            this.EndCurrentRenderPass();
        }

        VkTexture vkSource = Util.AssertSubtype<Texture, VkTexture>(source);
        this._currentStagingInfo.Resources.Add(vkSource.RefCount);
        VkTexture vkDestination = Util.AssertSubtype<Texture, VkTexture>(destination);
        this._currentStagingInfo.Resources.Add(vkDestination.RefCount);
        VkImageAspectFlags aspectFlags = (source.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil
            ? VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil
            : VkImageAspectFlags.Color;
        VkImageResolve region = new() {
            extent = new VkExtent3D { width = source.Width, height = source.Height, depth = source.Depth },
            srcSubresource = new VkImageSubresourceLayers { layerCount = 1, aspectMask = aspectFlags },
            dstSubresource = new VkImageSubresourceLayers { layerCount = 1, aspectMask = aspectFlags }
        };

        vkSource.TransitionImageLayout(this.CommandBuffer, 0, 1, 0, 1, VkImageLayout.TransferSrcOptimal);
        vkDestination.TransitionImageLayout(this.CommandBuffer, 0, 1, 0, 1, VkImageLayout.TransferDstOptimal);

        vkCmdResolveImage(this.CommandBuffer, vkSource.OptimalDeviceImage, VkImageLayout.TransferSrcOptimal, vkDestination.OptimalDeviceImage, VkImageLayout.TransferDstOptimal, 1, ref region);

        if ((vkDestination.Usage & TextureUsage.Sampled) != 0) {
            vkDestination.TransitionImageLayout(this.CommandBuffer, 0, 1, 0, 1, VkImageLayout.ShaderReadOnlyOptimal);
        }
    }

    /// <summary>
    /// Sets the framebuffer core value.
    /// </summary>
    /// <param name="fb">The fb value used by this operation.</param>
    protected override void SetFramebufferCore(Framebuffer fb) {
        if (this._activeRenderPass.Handle != VkRenderPass.Null) {
            this.EndCurrentRenderPass();
        }
        else if (!this._currentFramebufferEverActive && this._currentFramebuffer != null) {
            // This forces any queued up texture clears to be emitted.
            this.BeginCurrentRenderPass();
            this.EndCurrentRenderPass();
        }

        this._currentFramebuffer?.TransitionToFinalLayout(this.CommandBuffer);

        VkFramebufferBase vkFb = Util.AssertSubtype<Framebuffer, VkFramebufferBase>(fb);
        this._currentFramebuffer = vkFb;
        this._currentFramebufferEverActive = false;
        this._newFramebuffer = true;
        Util.EnsureArrayMinimumSize(ref this._scissorRects, Math.Max(1, (uint)vkFb.ColorTargets.Count));
        uint clearValueCount = (uint)vkFb.ColorTargets.Count;
        Util.EnsureArrayMinimumSize(ref this._clearValues, clearValueCount + 1); // Leave an extra space for the depth value (tracked separately). Util.ClearArray(this._validColorClearValues);
        Util.EnsureArrayMinimumSize(ref this._validColorClearValues, clearValueCount);
        this._currentStagingInfo.Resources.Add(vkFb.RefCount);

        if (fb is VkSwapchainFramebuffer scFb) {
            this._currentStagingInfo.Resources.Add(scFb.Swapchain.RefCount);
        }
    }

    /// <summary>
    /// Sets the graphics resource set core value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="rs">The rs value used by this operation.</param>
    /// <param name="dynamicOffsetsCount">The dynamic offsets count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
        if (!this._currentGraphicsResourceSets[slot].Equals(rs, dynamicOffsetsCount, ref dynamicOffsets)) {
            this._currentGraphicsResourceSets[slot].Offsets.Dispose();
            this._currentGraphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
            this._graphicsResourceSetsChanged[slot] = true;
            Util.AssertSubtype<ResourceSet, VkResourceSet>(rs);
        }
    }

    /// <summary>
    /// Sets the compute resource set core value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="rs">The rs value used by this operation.</param>
    /// <param name="dynamicOffsetsCount">The dynamic offsets count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    protected override void SetComputeResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
        if (!this._currentComputeResourceSets[slot].Equals(rs, dynamicOffsetsCount, ref dynamicOffsets)) {
            this._currentComputeResourceSets[slot].Offsets.Dispose();
            this._currentComputeResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
            this._computeResourceSetsChanged[slot] = true;
            Util.AssertSubtype<ResourceSet, VkResourceSet>(rs);
        }
    }

    /// <summary>
    /// Gets the next command buffer value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private VkCommandBuffer GetNextCommandBuffer() {
        lock (this._commandBufferListLock) {
            if (this._availableCommandBuffers.Count > 0) {
                VkCommandBuffer cachedCb = this._availableCommandBuffers.Dequeue();
                VkResult resetResult = vkResetCommandBuffer(cachedCb, VkCommandBufferResetFlags.None);
                CheckResult(resetResult);
                return cachedCb;
            }
        }

        VkCommandBufferAllocateInfo cbAi = VkCommandBufferAllocateInfo.New();
        cbAi.commandPool = this._pool;
        cbAi.commandBufferCount = 1;
        cbAi.level = VkCommandBufferLevel.Primary;
        VkResult result = vkAllocateCommandBuffers(this.gd.Device, ref cbAi, out VkCommandBuffer cb);
        CheckResult(result);
        return cb;
    }

    /// <summary>
    /// Executes the pre draw command logic for this backend.
    /// </summary>
    private void PreDrawCommand() {
        this.TransitionImages(this._preDrawSampledImages, VkImageLayout.ShaderReadOnlyOptimal);
        this._preDrawSampledImages.Clear();

        this.EnsureRenderPassActive();

        this.FlushNewResourceSets(this._currentGraphicsResourceSets, this._graphicsResourceSetsChanged, this._currentGraphicsPipeline.ResourceSetCount, VkPipelineBindPoint.Graphics, this._currentGraphicsPipeline.PipelineLayout);
    }

    /// <summary>
    /// Executes the flush new resource sets logic for this backend.
    /// </summary>
    /// <param name="resourceSets">The resource sets value used by this operation.</param>
    /// <param name="resourceSetsChanged">The resource sets changed value used by this operation.</param>
    /// <param name="resourceSetCount">The resource set count value used by this operation.</param>
    /// <param name="bindPoint">The bind point value used by this operation.</param>
    /// <param name="pipelineLayout">The pipeline layout value used by this operation.</param>
    private void FlushNewResourceSets(BoundResourceSetInfo[] resourceSets, bool[] resourceSetsChanged, uint resourceSetCount, VkPipelineBindPoint bindPoint, VkPipelineLayout pipelineLayout) {
        VkPipeline pipeline = bindPoint == VkPipelineBindPoint.Graphics
            ? this._currentGraphicsPipeline
            : this._currentComputePipeline;

        VkDescriptorSet* descriptorSets = stackalloc VkDescriptorSet[(int)resourceSetCount];
        uint* dynamicOffsets = stackalloc uint[(int)pipeline.DynamicOffsetsCount];
        uint currentBatchCount = 0;
        uint currentBatchFirstSet = 0;
        uint currentBatchDynamicOffsetCount = 0;

        for (uint currentSlot = 0; currentSlot < resourceSetCount; currentSlot++) {
            bool batchEnded = !resourceSetsChanged[currentSlot] || currentSlot == resourceSetCount - 1;

            if (resourceSetsChanged[currentSlot]) {
                resourceSetsChanged[currentSlot] = false;
                VkResourceSet vkSet = Util.AssertSubtype<ResourceSet, VkResourceSet>(resourceSets[currentSlot].Set);
                descriptorSets[currentBatchCount] = vkSet.DescriptorSet;
                currentBatchCount += 1;

                ref SmallFixedOrDynamicArray curSetOffsets = ref resourceSets[currentSlot].Offsets;

                for (uint i = 0; i < curSetOffsets.Count; i++) {
                    dynamicOffsets[currentBatchDynamicOffsetCount] = curSetOffsets.Get(i);
                    currentBatchDynamicOffsetCount += 1;
                }

                // Increment ref count on first use of a set.
                this._currentStagingInfo.Resources.Add(vkSet.RefCount);
                for (int i = 0; i < vkSet.RefCounts.Count; i++) {
                    this._currentStagingInfo.Resources.Add(vkSet.RefCounts[i]);
                }
            }

            if (batchEnded) {
                if (currentBatchCount != 0) {
                    // Flush current batch.
                    vkCmdBindDescriptorSets(this.CommandBuffer, bindPoint, pipelineLayout, currentBatchFirstSet, currentBatchCount, descriptorSets, currentBatchDynamicOffsetCount, dynamicOffsets);
                }

                currentBatchCount = 0;
                currentBatchFirstSet = currentSlot + 1;
            }
        }
    }

    /// <summary>
    /// Executes the transition images logic for this backend.
    /// </summary>
    /// <param name="sampledTextures">The texture resource involved in this operation.</param>
    /// <param name="layout">The resource layout used by this operation.</param>
    private void TransitionImages(List<VkTexture> sampledTextures, VkImageLayout layout) {
        for (int i = 0; i < sampledTextures.Count; i++) {
            VkTexture tex = sampledTextures[i];
            tex.TransitionImageLayout(this.CommandBuffer, 0, tex.MipLevels, 0, tex.ActualArrayLayers, layout);
        }
    }

    /// <summary>
    /// Executes the pre dispatch command logic for this backend.
    /// </summary>
    private void PreDispatchCommand() {
        this.EnsureNoRenderPass();

        for (uint currentSlot = 0; currentSlot < this._currentComputePipeline.ResourceSetCount; currentSlot++) {
            VkResourceSet vkSet = Util.AssertSubtype<ResourceSet, VkResourceSet>(this._currentComputeResourceSets[currentSlot].Set);

            this.TransitionImages(vkSet.SampledTextures, VkImageLayout.ShaderReadOnlyOptimal);
            this.TransitionImages(vkSet.StorageTextures, VkImageLayout.General);

            for (int texIdx = 0; texIdx < vkSet.StorageTextures.Count; texIdx++) {
                VkTexture storageTex = vkSet.StorageTextures[texIdx];
                if ((storageTex.Usage & TextureUsage.Sampled) != 0) {
                    this._preDrawSampledImages.Add(storageTex);
                }
            }
        }

        this.FlushNewResourceSets(this._currentComputeResourceSets, this._computeResourceSetsChanged, this._currentComputePipeline.ResourceSetCount, VkPipelineBindPoint.Compute, this._currentComputePipeline.PipelineLayout);
    }

    /// <summary>
    /// Executes the ensure render pass active logic for this backend.
    /// </summary>
    private void EnsureRenderPassActive() {
        if (this._activeRenderPass == VkRenderPass.Null) {
            this.BeginCurrentRenderPass();
        }
    }

    /// <summary>
    /// Executes the ensure no render pass logic for this backend.
    /// </summary>
    private void EnsureNoRenderPass() {
        if (this._activeRenderPass != VkRenderPass.Null) {
            this.EndCurrentRenderPass();
        }
    }

    /// <summary>
    /// Begins the current render pass operation.
    /// </summary>
    private void BeginCurrentRenderPass() {
        Debug.Assert(this._activeRenderPass == VkRenderPass.Null);
        Debug.Assert(this._currentFramebuffer != null);
        this._currentFramebufferEverActive = true;

        uint attachmentCount = this._currentFramebuffer.AttachmentCount;
        bool haveAnyAttachments = this._currentFramebuffer.ColorTargets.Count > 0 || this._currentFramebuffer.DepthTarget != null;
        bool haveAllClearValues = this._depthClearValue.HasValue || this._currentFramebuffer.DepthTarget == null;
        bool haveAnyClearValues = this._depthClearValue.HasValue;

        for (int i = 0; i < this._currentFramebuffer.ColorTargets.Count; i++) {
            if (!this._validColorClearValues[i]) {
                haveAllClearValues = false;
            }
            else {
                haveAnyClearValues = true;
            }
        }

        VkRenderPassBeginInfo renderPassBi = VkRenderPassBeginInfo.New();
        renderPassBi.renderArea = new VkRect2D(this._currentFramebuffer.RenderableWidth, this._currentFramebuffer.RenderableHeight);
        renderPassBi.framebuffer = this._currentFramebuffer.CurrentFramebuffer;

        if (!haveAnyAttachments || !haveAllClearValues) {
            renderPassBi.renderPass = this._newFramebuffer
                ? this._currentFramebuffer.RenderPassNoClearInit
                : this._currentFramebuffer.RenderPassNoClearLoad;
            vkCmdBeginRenderPass(this.CommandBuffer, ref renderPassBi, VkSubpassContents.Inline);
            this._activeRenderPass = renderPassBi.renderPass;

            if (haveAnyClearValues) {
                if (this._depthClearValue.HasValue) {
                    this.ClearDepthStencilCore(this._depthClearValue.Value.depthStencil.depth, (byte)this._depthClearValue.Value.depthStencil.stencil);
                    this._depthClearValue = null;
                }

                for (uint i = 0; i < this._currentFramebuffer.ColorTargets.Count; i++) {
                    if (this._validColorClearValues[i]) {
                        this._validColorClearValues[i] = false;
                        VkClearValue vkClearValue = this._clearValues[i];
                        RgbaFloat clearColor = new(vkClearValue.color.float32_0, vkClearValue.color.float32_1, vkClearValue.color.float32_2, vkClearValue.color.float32_3);
                        this.ClearColorTarget(i, clearColor);
                    }
                }
            }
        }
        else {
            // We have clear values for every attachment.
            renderPassBi.renderPass = this._currentFramebuffer.RenderPassClear;

            fixed (VkClearValue* clearValuesPtr = &this._clearValues[0]) {
                renderPassBi.clearValueCount = attachmentCount;
                renderPassBi.pClearValues = clearValuesPtr;

                if (this._depthClearValue.HasValue) {
                    this._clearValues[this._currentFramebuffer.ColorTargets.Count] = this._depthClearValue.Value;
                    this._depthClearValue = null;
                }

                vkCmdBeginRenderPass(this.CommandBuffer, ref renderPassBi, VkSubpassContents.Inline);
                this._activeRenderPass = this._currentFramebuffer.RenderPassClear;
                Util.ClearArray(this._validColorClearValues);
            }
        }

        this._newFramebuffer = false;
    }

    /// <summary>
    /// Ends the current render pass operation.
    /// </summary>
    private void EndCurrentRenderPass() {
        Debug.Assert(this._activeRenderPass != VkRenderPass.Null);
        vkCmdEndRenderPass(this.CommandBuffer);
        this._currentFramebuffer.TransitionToIntermediateLayout(this.CommandBuffer);
        this._activeRenderPass = VkRenderPass.Null;

        // Place a barrier between RenderPasses, so that color / depth outputs
        // can be read in subsequent passes.
        vkCmdPipelineBarrier(this.CommandBuffer, VkPipelineStageFlags.BottomOfPipe, VkPipelineStageFlags.TopOfPipe, VkDependencyFlags.None, 0, null, 0, null, 0, null);
    }

    /// <summary>
    /// Executes the clear sets logic for this backend.
    /// </summary>
    /// <param name="boundSets">The bound sets value used by this operation.</param>
    private void ClearSets(BoundResourceSetInfo[] boundSets) {
        foreach (BoundResourceSetInfo boundSetInfo in boundSets) {
            boundSetInfo.Offsets.Dispose();
        }

        Util.ClearArray(boundSets);
    }

    [Conditional("DEBUG")]

    /// <summary>
    /// Executes the debug full pipeline barrier logic for this backend.
    /// </summary>
    private void DebugFullPipelineBarrier() {
        VkMemoryBarrier memoryBarrier = VkMemoryBarrier.New();
        memoryBarrier.srcAccessMask = VK_ACCESS_INDIRECT_COMMAND_READ_BIT |
                                      VK_ACCESS_INDEX_READ_BIT |
                                      VK_ACCESS_VERTEX_ATTRIBUTE_READ_BIT |
                                      VK_ACCESS_UNIFORM_READ_BIT |
                                      VK_ACCESS_INPUT_ATTACHMENT_READ_BIT |
                                      VK_ACCESS_SHADER_READ_BIT |
                                      VK_ACCESS_SHADER_WRITE_BIT |
                                      VK_ACCESS_COLOR_ATTACHMENT_READ_BIT |
                                      VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT |
                                      VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT |
                                      VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT |
                                      VK_ACCESS_TRANSFER_READ_BIT |
                                      VK_ACCESS_TRANSFER_WRITE_BIT |
                                      VK_ACCESS_HOST_READ_BIT |
                                      VK_ACCESS_HOST_WRITE_BIT;
        memoryBarrier.dstAccessMask = VK_ACCESS_INDIRECT_COMMAND_READ_BIT |
                                      VK_ACCESS_INDEX_READ_BIT |
                                      VK_ACCESS_VERTEX_ATTRIBUTE_READ_BIT |
                                      VK_ACCESS_UNIFORM_READ_BIT |
                                      VK_ACCESS_INPUT_ATTACHMENT_READ_BIT |
                                      VK_ACCESS_SHADER_READ_BIT |
                                      VK_ACCESS_SHADER_WRITE_BIT |
                                      VK_ACCESS_COLOR_ATTACHMENT_READ_BIT |
                                      VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT |
                                      VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT |
                                      VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT |
                                      VK_ACCESS_TRANSFER_READ_BIT |
                                      VK_ACCESS_TRANSFER_WRITE_BIT |
                                      VK_ACCESS_HOST_READ_BIT |
                                      VK_ACCESS_HOST_WRITE_BIT;

        vkCmdPipelineBarrier(this.CommandBuffer, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, // srcStageMask
            VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, // dstStageMask
            VkDependencyFlags.None, 1, // memoryBarrierCount
            &memoryBarrier, // pMemoryBarriers
            0, null, 0, null);
    }

    /// <summary>
    /// Gets the staging buffer value.
    /// </summary>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private VkBuffer GetStagingBuffer(uint size) {
        lock (this._stagingLock) {
            VkBuffer ret = null;

            foreach (VkBuffer buffer in this._availableStagingBuffers) {
                if (buffer.SizeInBytes >= size) {
                    ret = buffer;
                    this._availableStagingBuffers.Remove(buffer);
                    break;
                }
            }

            if (ret == null) {
                ret = (VkBuffer)this.gd.ResourceFactory.CreateBuffer(new BufferDescription(size, BufferUsage.Staging));
                ret.Name = $"Staging Buffer (CommandList {this.name})";
            }

            this._currentStagingInfo.BuffersUsed.Add(ret);
            return ret;
        }
    }

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private void DisposeCore() {
        if (!this._destroyed) {
            this._destroyed = true;
            vkDestroyCommandPool(this.gd.Device, this._pool, null);

            Debug.Assert(this._submittedStagingInfos.Count == 0);

            foreach (VkBuffer buffer in this._availableStagingBuffers) {
                buffer.Dispose();
            }
        }
    }

    /// <summary>
    /// Gets the staging resource info value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private StagingResourceInfo GetStagingResourceInfo() {
        lock (this._stagingLock) {
            StagingResourceInfo ret;
            int availableCount = this._availableStagingInfos.Count;

            if (availableCount > 0) {
                ret = this._availableStagingInfos[availableCount - 1];
                this._availableStagingInfos.RemoveAt(availableCount - 1);
            }
            else {
                ret = new StagingResourceInfo();
            }

            return ret;
        }
    }

    /// <summary>
    /// Executes the recycle staging info logic for this backend.
    /// </summary>
    /// <param name="info">The info value used by this operation.</param>
    private void RecycleStagingInfo(StagingResourceInfo info) {
        lock (this._stagingLock) {
            foreach (VkBuffer buffer in info.BuffersUsed) {
                this._availableStagingBuffers.Add(buffer);
            }

            foreach (ResourceRefCount rrc in info.Resources) {
                rrc.Decrement();
            }

            info.Clear();

            this._availableStagingInfos.Add(info);
        }
    }

    /// <summary>
    /// Executes the clear color target core logic for this backend.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="clearColor">The clear color value used by this operation.</param>
    private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor) {
        VkClearValue clearValue = new() {
            color = new VkClearColorValue(clearColor.R, clearColor.G, clearColor.B, clearColor.A)
        };

        if (this._activeRenderPass != VkRenderPass.Null) {
            VkClearAttachment clearAttachment = new() {
                colorAttachment = index,
                aspectMask = VkImageAspectFlags.Color,
                clearValue = clearValue
            };

            Texture colorTex = this._currentFramebuffer.ColorTargets[(int)index].Target;
            VkClearRect clearRect = new() {
                baseArrayLayer = 0,
                layerCount = 1,
                rect = new VkRect2D(0, 0, colorTex.Width, colorTex.Height)
            };

            vkCmdClearAttachments(this.CommandBuffer, 1, ref clearAttachment, 1, ref clearRect);
        }
        else {
            // Queue up the clear value for the next RenderPass.
            this._clearValues[index] = clearValue;
            this._validColorClearValues[index] = true;
        }
    }

    /// <summary>
    /// Executes the clear depth stencil core logic for this backend.
    /// </summary>
    /// <param name="depth">The depth value.</param>
    /// <param name="stencil">The stencil value used by this operation.</param>
    private protected override void ClearDepthStencilCore(float depth, byte stencil) {
        VkClearValue clearValue = new() { depthStencil = new VkClearDepthStencilValue(depth, stencil) };

        if (this._activeRenderPass != VkRenderPass.Null) {
            VkImageAspectFlags aspect = this._currentFramebuffer.DepthTarget is FramebufferAttachment depthAttachment && FormatHelpers.IsStencilFormat(depthAttachment.Target.Format)
                    ? VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil
                    : VkImageAspectFlags.Depth;

            VkClearAttachment clearAttachment = new() {
                aspectMask = aspect,
                clearValue = clearValue
            };

            uint renderableWidth = this._currentFramebuffer.RenderableWidth;
            uint renderableHeight = this._currentFramebuffer.RenderableHeight;

            if (renderableWidth > 0 && renderableHeight > 0) {
                VkClearRect clearRect = new() {
                    baseArrayLayer = 0,
                    layerCount = 1,
                    rect = new VkRect2D(0, 0, renderableWidth, renderableHeight)
                };

                vkCmdClearAttachments(this.CommandBuffer, 1, ref clearAttachment, 1, ref clearRect);
            }
        }
        else {
            // Queue up the clear value for the next RenderPass.
            this._depthClearValue = clearValue;
        }
    }

    /// <summary>
    /// Executes the draw core logic for this backend.
    /// </summary>
    /// <param name="vertexCount">The vertex count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="vertexStart">The vertex start value used by this operation.</param>
    /// <param name="instanceStart">The instance start value used by this operation.</param>
    private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart) {
        this.PreDrawCommand();
        vkCmdDraw(this.CommandBuffer, vertexCount, instanceCount, vertexStart, instanceStart);
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
        this.PreDrawCommand();
        vkCmdDrawIndexed(this.CommandBuffer, indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
    }

    /// <summary>
    /// Sets the vertex buffer core value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset) {
        VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(buffer);
        Vulkan.VkBuffer deviceBuffer = vkBuffer.DeviceBuffer;
        ulong offset64 = offset;
        vkCmdBindVertexBuffers(this.CommandBuffer, index, 1, ref deviceBuffer, ref offset64);
        this._currentStagingInfo.Resources.Add(vkBuffer.RefCount);
    }

    /// <summary>
    /// Sets the index buffer core value.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset) {
        VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(buffer);
        vkCmdBindIndexBuffer(this.CommandBuffer, vkBuffer.DeviceBuffer, offset, VkFormats.VdToVkIndexFormat(format));
        this._currentStagingInfo.Resources.Add(vkBuffer.RefCount);
    }

    /// <summary>
    /// Sets the pipeline core value.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    private protected override void SetPipelineCore(Pipeline pipeline) {
        VkPipeline vkPipeline = Util.AssertSubtype<Pipeline, VkPipeline>(pipeline);

        if (!pipeline.IsComputePipeline && this._currentGraphicsPipeline != pipeline) {
            Util.EnsureArrayMinimumSize(ref this._currentGraphicsResourceSets, vkPipeline.ResourceSetCount);
            this.ClearSets(this._currentGraphicsResourceSets);
            Util.EnsureArrayMinimumSize(ref this._graphicsResourceSetsChanged, vkPipeline.ResourceSetCount);
            vkCmdBindPipeline(this.CommandBuffer, VkPipelineBindPoint.Graphics, vkPipeline.DevicePipeline);
            this._currentGraphicsPipeline = vkPipeline;
        }
        else if (pipeline.IsComputePipeline && this._currentComputePipeline != pipeline) {
            Util.EnsureArrayMinimumSize(ref this._currentComputeResourceSets, vkPipeline.ResourceSetCount);
            this.ClearSets(this._currentComputeResourceSets);
            Util.EnsureArrayMinimumSize(ref this._computeResourceSetsChanged, vkPipeline.ResourceSetCount);
            vkCmdBindPipeline(this.CommandBuffer, VkPipelineBindPoint.Compute, vkPipeline.DevicePipeline);
            this._currentComputePipeline = vkPipeline;
        }

        this._currentStagingInfo.Resources.Add(vkPipeline.RefCount);
    }

    /// <summary>
    /// Executes the generate mipmaps core logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    private protected override void GenerateMipmapsCore(Texture texture) {
        this.EnsureNoRenderPass();
        VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(texture);
        this._currentStagingInfo.Resources.Add(vkTex.RefCount);

        uint layerCount = vkTex.ArrayLayers;
        if ((vkTex.Usage & TextureUsage.Cubemap) != 0) {
            layerCount *= 6;
        }

        uint width = vkTex.Width;
        uint height = vkTex.Height;
        uint depth = vkTex.Depth;

        for (uint level = 1; level < vkTex.MipLevels; level++) {
            vkTex.TransitionImageLayoutNonmatching(this.CommandBuffer, level - 1, 1, 0, layerCount, VkImageLayout.TransferSrcOptimal);
            vkTex.TransitionImageLayoutNonmatching(this.CommandBuffer, level, 1, 0, layerCount, VkImageLayout.TransferDstOptimal);

            VkImage deviceImage = vkTex.OptimalDeviceImage;
            uint mipWidth = Math.Max(width >> 1, 1);
            uint mipHeight = Math.Max(height >> 1, 1);
            uint mipDepth = Math.Max(depth >> 1, 1);

            VkImageBlit region = new() {
                srcSubresource = new VkImageSubresourceLayers {
                    aspectMask = VkImageAspectFlags.Color,
                    baseArrayLayer = 0,
                    layerCount = layerCount,
                    mipLevel = level - 1
                },
                srcOffsets_0 = new VkOffset3D(),
                srcOffsets_1 = new VkOffset3D { x = (int)width, y = (int)height, z = (int)depth },
                dstOffsets_0 = new VkOffset3D(),
                dstSubresource = new VkImageSubresourceLayers {
                    aspectMask = VkImageAspectFlags.Color,
                    baseArrayLayer = 0,
                    layerCount = layerCount,
                    mipLevel = level
                },
                dstOffsets_1 = new VkOffset3D { x = (int)mipWidth, y = (int)mipHeight, z = (int)mipDepth }
            };

            vkCmdBlitImage(this.CommandBuffer, deviceImage, VkImageLayout.TransferSrcOptimal, deviceImage, VkImageLayout.TransferDstOptimal, 1, &region, this.gd.GetFormatFilter(vkTex.VkFormat));

            width = mipWidth;
            height = mipHeight;
            depth = mipDepth;
        }

        if ((vkTex.Usage & TextureUsage.Sampled) != 0) {
            vkTex.TransitionImageLayoutNonmatching(this.CommandBuffer, 0, vkTex.MipLevels, 0, layerCount, VkImageLayout.ShaderReadOnlyOptimal);
        }
    }

    /// <summary>
    /// Executes the push debug group core logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private protected override void PushDebugGroupCore(string name) {
        VkCmdDebugMarkerBeginExtT func = this.gd.MarkerBegin;
        if (func == null) {
            return;
        }

        VkDebugMarkerMarkerInfoEXT markerInfo = VkDebugMarkerMarkerInfoEXT.New();

        int byteCount = Encoding.UTF8.GetByteCount(name);
        byte* utf8Ptr = stackalloc byte[byteCount + 1];
        fixed (char* namePtr = name) {
            Encoding.UTF8.GetBytes(namePtr, name.Length, utf8Ptr, byteCount);
        }

        utf8Ptr[byteCount] = 0;

        markerInfo.pMarkerName = utf8Ptr;

        func(this.CommandBuffer, &markerInfo);
    }

    /// <summary>
    /// Executes the pop debug group core logic for this backend.
    /// </summary>
    private protected override void PopDebugGroupCore() {
        VkCmdDebugMarkerEndExtT func = this.gd.MarkerEnd;

        func?.Invoke(this.CommandBuffer);
    }

    /// <summary>
    /// Executes the insert debug marker core logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private protected override void InsertDebugMarkerCore(string name) {
        VkCmdDebugMarkerInsertExtT func = this.gd.MarkerInsert;
        if (func == null) {
            return;
        }

        VkDebugMarkerMarkerInfoEXT markerInfo = VkDebugMarkerMarkerInfoEXT.New();

        int byteCount = Encoding.UTF8.GetByteCount(name);
        byte* utf8Ptr = stackalloc byte[byteCount + 1];
        fixed (char* namePtr = name) {
            Encoding.UTF8.GetBytes(namePtr, name.Length, utf8Ptr, byteCount);
        }

        utf8Ptr[byteCount] = 0;

        markerInfo.pMarkerName = utf8Ptr;

        func(this.CommandBuffer, &markerInfo);
    }

    /// <summary>
    /// Represents the StagingResourceInfo type used by the graphics runtime.
    /// </summary>
    private class StagingResourceInfo {

        /// <summary>
        /// Stores the buffers used collection used by this instance.
        /// </summary>
        public List<VkBuffer> BuffersUsed { get; } = new();

        /// <summary>
        /// Stores the resources collection used by this instance.
        /// </summary>
        public HashSet<ResourceRefCount> Resources { get; } = new();

        /// <summary>
        /// Executes the clear logic for this backend.
        /// </summary>
        public void Clear() {
            this.BuffersUsed.Clear();
            this.Resources.Clear();
        }
    }
}