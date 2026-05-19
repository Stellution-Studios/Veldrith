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
/// Defines the behavior and responsibilities of the VkCommandList class.
/// </summary>
internal unsafe class VkCommandList : CommandList {

    /// <summary>
    /// Stores the value associated with <c>_availableCommandBuffers</c>.
    /// </summary>
    private readonly Queue<VkCommandBuffer> _availableCommandBuffers = new();

    /// <summary>
    /// Stores the value associated with <c>_availableStagingBuffers</c>.
    /// </summary>
    private readonly List<VkBuffer> _availableStagingBuffers = new();

    /// <summary>
    /// Stores the value associated with <c>_availableStagingInfos</c>.
    /// </summary>
    private readonly List<StagingResourceInfo> _availableStagingInfos = new();

    /// <summary>
    /// Stores the value associated with <c>_commandBufferListLock</c>.
    /// </summary>
    private readonly object _commandBufferListLock = new();

    /// <summary>
    /// Stores the value associated with <c>_pool</c>.
    /// </summary>
    private readonly VkCommandPool _pool;

    /// <summary>
    /// Stores the value associated with <c>_preDrawSampledImages</c>.
    /// </summary>
    private readonly List<VkTexture> _preDrawSampledImages = new();

    /// <summary>
    /// Stores the value associated with <c>_stagingLock</c>.
    /// </summary>
    private readonly object _stagingLock = new();

    /// <summary>
    /// Stores the value associated with <c>_submittedCommandBuffers</c>.
    /// </summary>
    private readonly List<VkCommandBuffer> _submittedCommandBuffers = new();

    /// <summary>
    /// Stores the value associated with <c>_submittedStagingInfos</c>.
    /// </summary>
    private readonly Dictionary<VkCommandBuffer, StagingResourceInfo> _submittedStagingInfos = new();

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_activeRenderPass</c>.
    /// </summary>
    private VkRenderPass _activeRenderPass;

    /// <summary>
    /// Stores the value associated with <c>_clearValues</c>.
    /// </summary>
    private VkClearValue[] _clearValues = Array.Empty<VkClearValue>();

    /// <summary>
    /// Stores the value associated with <c>_commandBufferBegun</c>.
    /// </summary>
    private bool _commandBufferBegun;

    /// <summary>
    /// Stores the value associated with <c>_commandBufferEnded</c>.
    /// </summary>
    private bool _commandBufferEnded;

    /// <summary>
    /// Stores the value associated with <c>_computeResourceSetsChanged</c>.
    /// </summary>
    private bool[] _computeResourceSetsChanged;

    // Compute State

    /// <summary>
    /// Stores the value associated with <c>_currentComputePipeline</c>.
    /// </summary>
    private VkPipeline _currentComputePipeline;

    /// <summary>
    /// Stores the value associated with <c>_currentComputeResourceSets</c>.
    /// </summary>
    private BoundResourceSetInfo[] _currentComputeResourceSets = Array.Empty<BoundResourceSetInfo>();

    // Graphics State

    /// <summary>
    /// Stores the value associated with <c>_currentFramebuffer</c>.
    /// </summary>
    private VkFramebufferBase _currentFramebuffer;

    /// <summary>
    /// Stores the value associated with <c>_currentFramebufferEverActive</c>.
    /// </summary>
    private bool _currentFramebufferEverActive;

    /// <summary>
    /// Stores the value associated with <c>_currentGraphicsPipeline</c>.
    /// </summary>
    private VkPipeline _currentGraphicsPipeline;

    /// <summary>
    /// Stores the value associated with <c>_currentGraphicsResourceSets</c>.
    /// </summary>
    private BoundResourceSetInfo[] _currentGraphicsResourceSets = Array.Empty<BoundResourceSetInfo>();

    /// <summary>
    /// Stores the value associated with <c>_currentStagingInfo</c>.
    /// </summary>
    private StagingResourceInfo _currentStagingInfo;

    /// <summary>
    /// Stores the value associated with <c>_depthClearValue</c>.
    /// </summary>
    private VkClearValue? _depthClearValue;

    /// <summary>
    /// Stores the value associated with <c>_destroyed</c>.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the value associated with <c>_graphicsResourceSetsChanged</c>.
    /// </summary>
    private bool[] _graphicsResourceSetsChanged;

    /// <summary>
    /// Stores the value associated with <c>_newFramebuffer</c>.
    /// </summary>
    private bool _newFramebuffer; // Render pass cycle state

    /// <summary>
    /// Stores the value associated with <c>_scissorRects</c>.
    /// </summary>
    private VkRect2D[] _scissorRects = Array.Empty<VkRect2D>();

    /// <summary>
    /// Stores the value associated with <c>_validColorClearValues</c>.
    /// </summary>
    private bool[] _validColorClearValues = Array.Empty<bool>();

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    private string name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkCommandList" /> class.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
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
    /// Stores the value associated with <c>CommandPool</c>.
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes the CommandBufferSubmitted operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    public void CommandBufferSubmitted(VkCommandBuffer cb) {
        this.RefCount.Increment();
        foreach (ResourceRefCount rrc in this._currentStagingInfo.Resources) {
            rrc.Increment();
        }

        this._submittedStagingInfos.Add(cb, this._currentStagingInfo);
        this._currentStagingInfo = null;
    }

    /// <summary>
    /// Executes the CommandBufferCompleted operation.
    /// </summary>
    /// <param name="completedCb">Specifies the value of <paramref name="completedCb" />.</param>
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
    /// Executes the Begin operation.
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
    /// Executes the Dispatch operation.
    /// </summary>
    /// <param name="groupCountX">Specifies the value of <paramref name="groupCountX" />.</param>
    /// <param name="groupCountY">Specifies the value of <paramref name="groupCountY" />.</param>
    /// <param name="groupCountZ">Specifies the value of <paramref name="groupCountZ" />.</param>
    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ) {
        this.PreDispatchCommand();

        vkCmdDispatch(this.CommandBuffer, groupCountX, groupCountY, groupCountZ);
    }

    /// <summary>
    /// Executes the End operation.
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
    /// Executes the SetScissorRect operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
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
    /// Executes the SetViewport operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="viewport">Specifies the value of <paramref name="viewport" />.</param>
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
    /// Executes the UpdateBufferCore operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        VkBuffer stagingBuffer = this.GetStagingBuffer(sizeInBytes);
        this.gd.UpdateBuffer(stagingBuffer, 0, source, sizeInBytes);
        this.CopyBuffer(stagingBuffer, 0, buffer, bufferOffsetInBytes, sizeInBytes);
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
        this.EnsureNoRenderPass();
        CopyTextureCore_VkCommandBuffer(this.CommandBuffer, source, srcX, srcY, srcZ, srcMipLevel, srcBaseArrayLayer, destination, dstX, dstY, dstZ, dstMipLevel, dstBaseArrayLayer, width, height, depth, layerCount);

        VkTexture srcVkTexture = Util.AssertSubtype<Texture, VkTexture>(source);
        this._currentStagingInfo.Resources.Add(srcVkTexture.RefCount);
        VkTexture dstVkTexture = Util.AssertSubtype<Texture, VkTexture>(destination);
        this._currentStagingInfo.Resources.Add(dstVkTexture.RefCount);
    }

    /// <summary>
    /// Executes the CopyTextureCore_VkCommandBuffer operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
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
    /// Executes the DrawIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="drawCount">Specifies the value of <paramref name="drawCount" />.</param>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        this.PreDrawCommand();
        VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(indirectBuffer);
        this._currentStagingInfo.Resources.Add(vkBuffer.RefCount);
        vkCmdDrawIndirect(this.CommandBuffer, vkBuffer.DeviceBuffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Executes the DrawIndexedIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="drawCount">Specifies the value of <paramref name="drawCount" />.</param>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        this.PreDrawCommand();
        VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(indirectBuffer);
        this._currentStagingInfo.Resources.Add(vkBuffer.RefCount);
        vkCmdDrawIndexedIndirect(this.CommandBuffer, vkBuffer.DeviceBuffer, offset, drawCount, stride);
    }

    /// <summary>
    /// Executes the DispatchIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset) {
        this.PreDispatchCommand();

        VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(indirectBuffer);
        this._currentStagingInfo.Resources.Add(vkBuffer.RefCount);
        vkCmdDispatchIndirect(this.CommandBuffer, vkBuffer.DeviceBuffer, offset);
    }

    /// <summary>
    /// Executes the ResolveTextureCore operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
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
    /// Executes the SetFramebufferCore operation.
    /// </summary>
    /// <param name="fb">Specifies the value of <paramref name="fb" />.</param>
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
    /// Executes the SetGraphicsResourceSetCore operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="rs">Specifies the value of <paramref name="rs" />.</param>
    /// <param name="dynamicOffsetsCount">Specifies the value of <paramref name="dynamicOffsetsCount" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
    protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
        if (!this._currentGraphicsResourceSets[slot].Equals(rs, dynamicOffsetsCount, ref dynamicOffsets)) {
            this._currentGraphicsResourceSets[slot].Offsets.Dispose();
            this._currentGraphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
            this._graphicsResourceSetsChanged[slot] = true;
            Util.AssertSubtype<ResourceSet, VkResourceSet>(rs);
        }
    }

    /// <summary>
    /// Executes the SetComputeResourceSetCore operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="rs">Specifies the value of <paramref name="rs" />.</param>
    /// <param name="dynamicOffsetsCount">Specifies the value of <paramref name="dynamicOffsetsCount" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
    protected override void SetComputeResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
        if (!this._currentComputeResourceSets[slot].Equals(rs, dynamicOffsetsCount, ref dynamicOffsets)) {
            this._currentComputeResourceSets[slot].Offsets.Dispose();
            this._currentComputeResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
            this._computeResourceSetsChanged[slot] = true;
            Util.AssertSubtype<ResourceSet, VkResourceSet>(rs);
        }
    }

    /// <summary>
    /// Executes the GetNextCommandBuffer operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetNextCommandBuffer operation.</returns>
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
    /// Executes the PreDrawCommand operation.
    /// </summary>
    private void PreDrawCommand() {
        this.TransitionImages(this._preDrawSampledImages, VkImageLayout.ShaderReadOnlyOptimal);
        this._preDrawSampledImages.Clear();

        this.EnsureRenderPassActive();

        this.FlushNewResourceSets(this._currentGraphicsResourceSets, this._graphicsResourceSetsChanged, this._currentGraphicsPipeline.ResourceSetCount, VkPipelineBindPoint.Graphics, this._currentGraphicsPipeline.PipelineLayout);
    }

    /// <summary>
    /// Executes the FlushNewResourceSets operation.
    /// </summary>
    /// <param name="resourceSets">Specifies the value of <paramref name="resourceSets" />.</param>
    /// <param name="resourceSetsChanged">Specifies the value of <paramref name="resourceSetsChanged" />.</param>
    /// <param name="resourceSetCount">Specifies the value of <paramref name="resourceSetCount" />.</param>
    /// <param name="bindPoint">Specifies the value of <paramref name="bindPoint" />.</param>
    /// <param name="pipelineLayout">Specifies the value of <paramref name="pipelineLayout" />.</param>
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
    /// Executes the TransitionImages operation.
    /// </summary>
    /// <param name="sampledTextures">Specifies the value of <paramref name="sampledTextures" />.</param>
    /// <param name="layout">Specifies the value of <paramref name="layout" />.</param>
    private void TransitionImages(List<VkTexture> sampledTextures, VkImageLayout layout) {
        for (int i = 0; i < sampledTextures.Count; i++) {
            VkTexture tex = sampledTextures[i];
            tex.TransitionImageLayout(this.CommandBuffer, 0, tex.MipLevels, 0, tex.ActualArrayLayers, layout);
        }
    }

    /// <summary>
    /// Executes the PreDispatchCommand operation.
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
    /// Executes the EnsureRenderPassActive operation.
    /// </summary>
    private void EnsureRenderPassActive() {
        if (this._activeRenderPass == VkRenderPass.Null) {
            this.BeginCurrentRenderPass();
        }
    }

    /// <summary>
    /// Executes the EnsureNoRenderPass operation.
    /// </summary>
    private void EnsureNoRenderPass() {
        if (this._activeRenderPass != VkRenderPass.Null) {
            this.EndCurrentRenderPass();
        }
    }

    /// <summary>
    /// Executes the BeginCurrentRenderPass operation.
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
    /// Executes the EndCurrentRenderPass operation.
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
    /// Executes the ClearSets operation.
    /// </summary>
    /// <param name="boundSets">Specifies the value of <paramref name="boundSets" />.</param>
    private void ClearSets(BoundResourceSetInfo[] boundSets) {
        foreach (BoundResourceSetInfo boundSetInfo in boundSets) {
            boundSetInfo.Offsets.Dispose();
        }

        Util.ClearArray(boundSets);
    }

    [Conditional("DEBUG")]

    /// <summary>
    /// Executes the DebugFullPipelineBarrier operation.
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
    /// Executes the GetStagingBuffer operation.
    /// </summary>
    /// <param name="size">Specifies the value of <paramref name="size" />.</param>
    /// <returns>Returns the result produced by the GetStagingBuffer operation.</returns>
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
    /// Executes the DisposeCore operation.
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
    /// Executes the GetStagingResourceInfo operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetStagingResourceInfo operation.</returns>
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
    /// Executes the RecycleStagingInfo operation.
    /// </summary>
    /// <param name="info">Specifies the value of <paramref name="info" />.</param>
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
    /// Executes the ClearColorTargetCore operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="clearColor">Specifies the value of <paramref name="clearColor" />.</param>
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
    /// Executes the ClearDepthStencilCore operation.
    /// </summary>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="stencil">Specifies the value of <paramref name="stencil" />.</param>
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
    /// Executes the DrawCore operation.
    /// </summary>
    /// <param name="vertexCount">Specifies the value of <paramref name="vertexCount" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    /// <param name="vertexStart">Specifies the value of <paramref name="vertexStart" />.</param>
    /// <param name="instanceStart">Specifies the value of <paramref name="instanceStart" />.</param>
    private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart) {
        this.PreDrawCommand();
        vkCmdDraw(this.CommandBuffer, vertexCount, instanceCount, vertexStart, instanceStart);
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
        this.PreDrawCommand();
        vkCmdDrawIndexed(this.CommandBuffer, indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
    }

    /// <summary>
    /// Executes the SetVertexBufferCore operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset) {
        VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(buffer);
        Vulkan.VkBuffer deviceBuffer = vkBuffer.DeviceBuffer;
        ulong offset64 = offset;
        vkCmdBindVertexBuffers(this.CommandBuffer, index, 1, ref deviceBuffer, ref offset64);
        this._currentStagingInfo.Resources.Add(vkBuffer.RefCount);
    }

    /// <summary>
    /// Executes the SetIndexBufferCore operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset) {
        VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(buffer);
        vkCmdBindIndexBuffer(this.CommandBuffer, vkBuffer.DeviceBuffer, offset, VkFormats.VdToVkIndexFormat(format));
        this._currentStagingInfo.Resources.Add(vkBuffer.RefCount);
    }

    /// <summary>
    /// Executes the SetPipelineCore operation.
    /// </summary>
    /// <param name="pipeline">Specifies the value of <paramref name="pipeline" />.</param>
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
    /// Executes the GenerateMipmapsCore operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
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
    /// Executes the PushDebugGroupCore operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
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
    /// Executes the PopDebugGroupCore operation.
    /// </summary>
    private protected override void PopDebugGroupCore() {
        VkCmdDebugMarkerEndExtT func = this.gd.MarkerEnd;

        func?.Invoke(this.CommandBuffer);
    }

    /// <summary>
    /// Executes the InsertDebugMarkerCore operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
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
    /// Defines the behavior and responsibilities of the StagingResourceInfo class.
    /// </summary>
    private class StagingResourceInfo {

        /// <summary>
        /// Stores the value associated with <c>get</c>.
        /// </summary>
        public List<VkBuffer> BuffersUsed { get; } = new();

        /// <summary>
        /// Stores the value associated with <c>get</c>.
        /// </summary>
        public HashSet<ResourceRefCount> Resources { get; } = new();

        /// <summary>
        /// Executes the Clear operation.
        /// </summary>
        public void Clear() {
            this.BuffersUsed.Clear();
            this.Resources.Clear();
        }
    }
}
