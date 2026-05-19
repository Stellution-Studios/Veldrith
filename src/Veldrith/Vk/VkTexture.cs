using System.Diagnostics;
using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Defines the behavior and responsibilities of the VkTexture class.
/// </summary>
internal unsafe class VkTexture : Texture {

    /// <summary>
    /// Stores the value associated with <c>_imageLayouts</c>.
    /// </summary>
    private readonly VkImageLayout[] _imageLayouts;

    /// <summary>
    /// Stores the value associated with <c>_memoryBlock</c>.
    /// </summary>
    private readonly VkMemoryBlock _memoryBlock;

    /// <summary>
    /// Stores the value associated with <c>_optimalImage</c>.
    /// </summary>
    private readonly VkImage _optimalImage;

    /// <summary>
    /// Stores the value associated with <c>_stagingBuffer</c>.
    /// </summary>
    private readonly Vulkan.VkBuffer _stagingBuffer;

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_destroyed</c>.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the value associated with <c>_name</c>.
    /// </summary>
    private string _name;

    /// <summary>
    /// Stores the value associated with <c>depth</c>.
    /// </summary>
    private uint depth;

    /// <summary>
    /// Stores the value associated with <c>format</c>.
    /// </summary>
    private PixelFormat format; // Static for regular images -- may change for shared staging images

    /// <summary>
    /// Stores the value associated with <c>height</c>.
    /// </summary>
    private uint height;

    // Immutable except for shared staging Textures.

    /// <summary>
    /// Stores the value associated with <c>width</c>.
    /// </summary>
    private uint width;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkTexture" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    internal VkTexture(VkGraphicsDevice gd, ref TextureDescription description) {
        this.gd = gd;
        this.width = description.Width;
        this.height = description.Height;
        this.depth = description.Depth;
        this.MipLevels = description.MipLevels;
        this.ArrayLayers = description.ArrayLayers;
        bool isCubemap = (description.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap;
        this.ActualArrayLayers = isCubemap
            ? 6 * this.ArrayLayers
            : this.ArrayLayers;
        this.format = description.Format;
        this.Usage = description.Usage;
        this.Type = description.Type;
        this.SampleCount = description.SampleCount;
        this.VkSampleCount = VkFormats.VdToVkSampleCount(this.SampleCount);
        this.VkFormat = VkFormats.VdToVkPixelFormat(this.Format, (description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil);

        bool isStaging = (this.Usage & TextureUsage.Staging) == TextureUsage.Staging;

        if (!isStaging) {
            VkImageCreateInfo imageCi = VkImageCreateInfo.New();
            imageCi.mipLevels = this.MipLevels;
            imageCi.arrayLayers = this.ActualArrayLayers;
            imageCi.imageType = VkFormats.VdToVkTextureType(this.Type);
            imageCi.extent.width = this.Width;
            imageCi.extent.height = this.Height;
            imageCi.extent.depth = this.Depth;
            imageCi.initialLayout = VkImageLayout.Preinitialized;
            imageCi.usage = VkFormats.VdToVkTextureUsage(this.Usage);
            imageCi.tiling = VkImageTiling.Optimal;
            imageCi.format = this.VkFormat;
            imageCi.flags = VkImageCreateFlags.MutableFormat;

            imageCi.samples = this.VkSampleCount;
            if (isCubemap) {
                imageCi.flags |= VkImageCreateFlags.CubeCompatible;
            }

            uint subresourceCount = this.MipLevels * this.ActualArrayLayers * this.Depth;
            VkResult result = vkCreateImage(gd.Device, ref imageCi, null, out this._optimalImage);
            CheckResult(result);

            VkMemoryRequirements memoryRequirements;
            bool prefersDedicatedAllocation;

            if (this.gd.GetImageMemoryRequirements2 != null) {
                VkImageMemoryRequirementsInfo2KHR memReqsInfo2 = VkImageMemoryRequirementsInfo2KHR.New();
                memReqsInfo2.image = this._optimalImage;
                VkMemoryRequirements2KHR memReqs2 = VkMemoryRequirements2KHR.New();
                VkMemoryDedicatedRequirementsKHR dedicatedReqs = VkMemoryDedicatedRequirementsKHR.New();
                memReqs2.pNext = &dedicatedReqs;
                this.gd.GetImageMemoryRequirements2(this.gd.Device, &memReqsInfo2, &memReqs2);
                memoryRequirements = memReqs2.memoryRequirements;
                prefersDedicatedAllocation = dedicatedReqs.prefersDedicatedAllocation || dedicatedReqs.requiresDedicatedAllocation;
            }
            else {
                vkGetImageMemoryRequirements(gd.Device, this._optimalImage, out memoryRequirements);
                prefersDedicatedAllocation = false;
            }

            VkMemoryBlock memoryToken = gd.MemoryManager.Allocate(gd.PhysicalDeviceMemProperties, memoryRequirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal, false, memoryRequirements.size, memoryRequirements.alignment, prefersDedicatedAllocation, this._optimalImage, Vulkan.VkBuffer.Null);
            this._memoryBlock = memoryToken;
            result = vkBindImageMemory(gd.Device, this._optimalImage, this._memoryBlock.DeviceMemory, this._memoryBlock.Offset);
            CheckResult(result);

            this._imageLayouts = new VkImageLayout[subresourceCount];
            for (int i = 0; i < this._imageLayouts.Length; i++) {
                this._imageLayouts[i] = VkImageLayout.Preinitialized;
            }
        }
        else // isStaging
        {
            uint depthPitch = FormatHelpers.GetDepthPitch(FormatHelpers.GetRowPitch(this.Width, this.Format), this.Height, this.Format);
            uint stagingSize = depthPitch * this.Depth;

            for (uint level = 1; level < this.MipLevels; level++) {
                Util.GetMipDimensions(this, level, out uint mipWidth, out uint mipHeight, out uint mipDepth);

                depthPitch = FormatHelpers.GetDepthPitch(FormatHelpers.GetRowPitch(mipWidth, this.Format), mipHeight, this.Format);

                stagingSize += depthPitch * mipDepth;
            }

            stagingSize *= this.ArrayLayers;

            VkBufferCreateInfo bufferCi = VkBufferCreateInfo.New();
            bufferCi.usage = VkBufferUsageFlags.TransferSrc | VkBufferUsageFlags.TransferDst;
            bufferCi.size = stagingSize;
            VkResult result = vkCreateBuffer(this.gd.Device, ref bufferCi, null, out this._stagingBuffer);
            CheckResult(result);

            VkMemoryRequirements bufferMemReqs;
            bool prefersDedicatedAllocation;

            if (this.gd.GetBufferMemoryRequirements2 != null) {
                VkBufferMemoryRequirementsInfo2KHR memReqInfo2 = VkBufferMemoryRequirementsInfo2KHR.New();
                memReqInfo2.buffer = this._stagingBuffer;
                VkMemoryRequirements2KHR memReqs2 = VkMemoryRequirements2KHR.New();
                VkMemoryDedicatedRequirementsKHR dedicatedReqs = VkMemoryDedicatedRequirementsKHR.New();
                memReqs2.pNext = &dedicatedReqs;
                this.gd.GetBufferMemoryRequirements2(this.gd.Device, &memReqInfo2, &memReqs2);
                bufferMemReqs = memReqs2.memoryRequirements;
                prefersDedicatedAllocation = dedicatedReqs.prefersDedicatedAllocation || dedicatedReqs.requiresDedicatedAllocation;
            }
            else {
                vkGetBufferMemoryRequirements(gd.Device, this._stagingBuffer, out bufferMemReqs);
                prefersDedicatedAllocation = false;
            }

            // Use "host cached" memory when available, for better performance of GPU -> CPU transfers
            VkMemoryPropertyFlags propertyFlags = VkMemoryPropertyFlags.HostVisible |
                                                  VkMemoryPropertyFlags.HostCoherent | VkMemoryPropertyFlags.HostCached;
            if (!TryFindMemoryType(this.gd.PhysicalDeviceMemProperties, bufferMemReqs.memoryTypeBits, propertyFlags, out _)) {
                propertyFlags ^= VkMemoryPropertyFlags.HostCached;
            }

            this._memoryBlock = this.gd.MemoryManager.Allocate(this.gd.PhysicalDeviceMemProperties, bufferMemReqs.memoryTypeBits, propertyFlags, true, bufferMemReqs.size, bufferMemReqs.alignment, prefersDedicatedAllocation, VkImage.Null, this._stagingBuffer);

            result = vkBindBufferMemory(this.gd.Device, this._stagingBuffer, this._memoryBlock.DeviceMemory, this._memoryBlock.Offset);
            CheckResult(result);
        }

        this.ClearIfRenderTarget();
        this.TransitionIfSampled();
        this.RefCount = new ResourceRefCount(this.RefCountedDispose);
    }

    // Used to construct Swapchain textures.

    /// <summary>
    /// Initializes a new instance of the <see cref="VkTexture" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="mipLevels">Specifies the value of <paramref name="mipLevels" />.</param>
    /// <param name="arrayLayers">Specifies the value of <paramref name="arrayLayers" />.</param>
    /// <param name="vkFormat">Specifies the value of <paramref name="vkFormat" />.</param>
    /// <param name="usage">Specifies the value of <paramref name="usage" />.</param>
    /// <param name="sampleCount">Specifies the value of <paramref name="sampleCount" />.</param>
    /// <param name="existingImage">Specifies the value of <paramref name="existingImage" />.</param>
    internal VkTexture(VkGraphicsDevice gd, uint width, uint height, uint mipLevels, uint arrayLayers, VkFormat vkFormat, TextureUsage usage, TextureSampleCount sampleCount, VkImage existingImage) {
        Debug.Assert(width > 0 && height > 0);
        this.gd = gd;
        this.MipLevels = mipLevels;
        this.width = width;
        this.height = height;
        this.depth = 1;
        this.VkFormat = vkFormat;
        this.format = VkFormats.VkToVdPixelFormat(this.VkFormat);
        this.ArrayLayers = arrayLayers;
        this.Usage = usage;
        this.Type = TextureType.Texture2D;
        this.SampleCount = sampleCount;
        this.VkSampleCount = VkFormats.VdToVkSampleCount(sampleCount);
        this._optimalImage = existingImage;
        this._imageLayouts = new[] { VkImageLayout.Undefined };
        this.IsSwapchainTexture = true;

        this.ClearIfRenderTarget();
        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    /// <summary>
    /// Gets or sets Width.
    /// </summary>
    public override uint Width => this.width;

    /// <summary>
    /// Gets or sets Height.
    /// </summary>
    public override uint Height => this.height;

    /// <summary>
    /// Gets or sets Depth.
    /// </summary>
    public override uint Depth => this.depth;

    /// <summary>
    /// Gets or sets Format.
    /// </summary>
    public override PixelFormat Format => this.format;

    /// <summary>
    /// Gets or sets MipLevels.
    /// </summary>
    public override uint MipLevels { get; }

    /// <summary>
    /// Gets or sets ArrayLayers.
    /// </summary>
    public override uint ArrayLayers { get; }

    /// <summary>
    /// Gets or sets ActualArrayLayers.
    /// </summary>
    public uint ActualArrayLayers { get; }

    /// <summary>
    /// Gets or sets Usage.
    /// </summary>
    public override TextureUsage Usage { get; }

    /// <summary>
    /// Gets or sets Type.
    /// </summary>
    public override TextureType Type { get; }

    /// <summary>
    /// Gets or sets SampleCount.
    /// </summary>
    public override TextureSampleCount SampleCount { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._destroyed;

    /// <summary>
    /// Stores the value associated with <c>OptimalDeviceImage</c>.
    /// </summary>
    public VkImage OptimalDeviceImage => this._optimalImage;

    /// <summary>
    /// Stores the value associated with <c>StagingBuffer</c>.
    /// </summary>
    public Vulkan.VkBuffer StagingBuffer => this._stagingBuffer;

    /// <summary>
    /// Stores the value associated with <c>Memory</c>.
    /// </summary>
    public VkMemoryBlock Memory => this._memoryBlock;

    /// <summary>
    /// Gets or sets VkFormat.
    /// </summary>
    public VkFormat VkFormat { get; }

    /// <summary>
    /// Gets or sets VkSampleCount.
    /// </summary>
    public VkSampleCountFlags VkSampleCount { get; }

    /// <summary>
    /// Gets or sets RefCount.
    /// </summary>
    public ResourceRefCount RefCount { get; }

    /// <summary>
    /// Gets or sets IsSwapchainTexture.
    /// </summary>
    public bool IsSwapchainTexture { get; }

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    /// <summary>
    /// Executes the GetSubresourceLayout operation.
    /// </summary>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    /// <returns>Returns the result produced by the GetSubresourceLayout operation.</returns>
    internal VkSubresourceLayout GetSubresourceLayout(uint subresource) {
        bool staging = this._stagingBuffer.Handle != 0;
        Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out uint arrayLayer);

        if (!staging) {
            VkImageAspectFlags aspect = (this.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil
                ? VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil
                : VkImageAspectFlags.Color;
            VkImageSubresource imageSubresource = new() {
                arrayLayer = arrayLayer,
                mipLevel = mipLevel,
                aspectMask = aspect
            };

            vkGetImageSubresourceLayout(this.gd.Device, this._optimalImage, ref imageSubresource, out VkSubresourceLayout layout);
            return layout;
        }
        else {
            Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint _);
            uint rowPitch = FormatHelpers.GetRowPitch(mipWidth, this.Format);
            uint depthPitch = FormatHelpers.GetDepthPitch(rowPitch, mipHeight, this.Format);

            VkSubresourceLayout layout = new() {
                rowPitch = rowPitch,
                depthPitch = depthPitch,
                arrayPitch = depthPitch,
                size = depthPitch,
                offset = Util.ComputeSubresourceOffset(this, mipLevel, arrayLayer)
            };

            return layout;
        }
    }

    /// <summary>
    /// Executes the TransitionImageLayout operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    /// <param name="baseMipLevel">Specifies the value of <paramref name="baseMipLevel" />.</param>
    /// <param name="levelCount">Specifies the value of <paramref name="levelCount" />.</param>
    /// <param name="baseArrayLayer">Specifies the value of <paramref name="baseArrayLayer" />.</param>
    /// <param name="layerCount">Specifies the value of <paramref name="layerCount" />.</param>
    /// <param name="newLayout">Specifies the value of <paramref name="newLayout" />.</param>
    internal void TransitionImageLayout(VkCommandBuffer cb, uint baseMipLevel, uint levelCount, uint baseArrayLayer, uint layerCount, VkImageLayout newLayout) {
        if (this._stagingBuffer != Vulkan.VkBuffer.Null) {
            return;
        }

        VkImageLayout oldLayout = this._imageLayouts[this.CalculateSubresource(baseMipLevel, baseArrayLayer)];
#if DEBUG
        for (uint level = 0; level < levelCount; level++) {
            for (uint layer = 0; layer < layerCount; layer++) {
                if (this._imageLayouts[this.CalculateSubresource(baseMipLevel + level, baseArrayLayer + layer)] != oldLayout)
                    throw new VeldridException("Unexpected image layout.");
            }
        }
#endif
        if (oldLayout != newLayout) {
            VkImageAspectFlags aspectMask;

            if ((this.Usage & TextureUsage.DepthStencil) != 0) {
                aspectMask = FormatHelpers.IsStencilFormat(this.Format)
                    ? VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil
                    : VkImageAspectFlags.Depth;
            }
            else {
                aspectMask = VkImageAspectFlags.Color;
            }

            VulkanUtil.TransitionImageLayout(cb, this.OptimalDeviceImage, baseMipLevel, levelCount, baseArrayLayer, layerCount, aspectMask, this._imageLayouts[this.CalculateSubresource(baseMipLevel, baseArrayLayer)], newLayout);

            for (uint level = 0; level < levelCount; level++) {
                for (uint layer = 0; layer < layerCount; layer++) {
                    this._imageLayouts[this.CalculateSubresource(baseMipLevel + level, baseArrayLayer + layer)] = newLayout;
                }
            }
        }
    }

    /// <summary>
    /// Executes the TransitionImageLayoutNonmatching operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    /// <param name="baseMipLevel">Specifies the value of <paramref name="baseMipLevel" />.</param>
    /// <param name="levelCount">Specifies the value of <paramref name="levelCount" />.</param>
    /// <param name="baseArrayLayer">Specifies the value of <paramref name="baseArrayLayer" />.</param>
    /// <param name="layerCount">Specifies the value of <paramref name="layerCount" />.</param>
    /// <param name="newLayout">Specifies the value of <paramref name="newLayout" />.</param>
    internal void TransitionImageLayoutNonmatching(VkCommandBuffer cb, uint baseMipLevel, uint levelCount, uint baseArrayLayer, uint layerCount, VkImageLayout newLayout) {
        if (this._stagingBuffer != Vulkan.VkBuffer.Null) {
            return;
        }

        for (uint level = baseMipLevel; level < baseMipLevel + levelCount; level++) {
            for (uint layer = baseArrayLayer; layer < baseArrayLayer + layerCount; layer++) {
                uint subresource = this.CalculateSubresource(level, layer);
                VkImageLayout oldLayout = this._imageLayouts[subresource];

                if (oldLayout != newLayout) {
                    VkImageAspectFlags aspectMask;

                    if ((this.Usage & TextureUsage.DepthStencil) != 0) {
                        aspectMask = FormatHelpers.IsStencilFormat(this.Format)
                            ? VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil
                            : VkImageAspectFlags.Depth;
                    }
                    else {
                        aspectMask = VkImageAspectFlags.Color;
                    }

                    VulkanUtil.TransitionImageLayout(cb, this.OptimalDeviceImage, level, 1, layer, 1, aspectMask, oldLayout, newLayout);

                    this._imageLayouts[subresource] = newLayout;
                }
            }
        }
    }

    /// <summary>
    /// Executes the GetImageLayout operation.
    /// </summary>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    /// <returns>Returns the result produced by the GetImageLayout operation.</returns>
    internal VkImageLayout GetImageLayout(uint mipLevel, uint arrayLayer) {
        return this._imageLayouts[this.CalculateSubresource(mipLevel, arrayLayer)];
    }

    /// <summary>
    /// Executes the SetStagingDimensions operation.
    /// </summary>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    internal void SetStagingDimensions(uint width, uint height, uint depth, PixelFormat format) {
        Debug.Assert(this._stagingBuffer != Vulkan.VkBuffer.Null);
        Debug.Assert(this.Usage == TextureUsage.Staging);
        this.width = width;
        this.height = height;
        this.depth = depth;
        this.format = format;
    }

    /// <summary>
    /// Executes the SetImageLayout operation.
    /// </summary>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    /// <param name="layout">Specifies the value of <paramref name="layout" />.</param>
    internal void SetImageLayout(uint mipLevel, uint arrayLayer, VkImageLayout layout) {
        this._imageLayouts[this.CalculateSubresource(mipLevel, arrayLayer)] = layout;
    }

    /// <summary>
    /// Executes the ClearIfRenderTarget operation.
    /// </summary>
    private void ClearIfRenderTarget() {
        // If the image is going to be used as a render target, we need to clear the data before its first use.
        if ((this.Usage & TextureUsage.RenderTarget) != 0) {
            this.gd.ClearColorTexture(this, new VkClearColorValue(0, 0, 0, 0));
        }
        else if ((this.Usage & TextureUsage.DepthStencil) != 0) {
            this.gd.ClearDepthTexture(this, new VkClearDepthStencilValue(0, 0));
        }
    }

    /// <summary>
    /// Executes the TransitionIfSampled operation.
    /// </summary>
    private void TransitionIfSampled() {
        if ((this.Usage & TextureUsage.Sampled) != 0) {
            this.gd.TransitionImageLayout(this, VkImageLayout.ShaderReadOnlyOptimal);
        }
    }

    /// <summary>
    /// Executes the RefCountedDispose operation.
    /// </summary>
    private void RefCountedDispose() {
        if (!this._destroyed) {
            base.Dispose();

            this._destroyed = true;

            bool isStaging = (this.Usage & TextureUsage.Staging) == TextureUsage.Staging;
            if (isStaging) {
                vkDestroyBuffer(this.gd.Device, this._stagingBuffer, null);
            }
            else {
                vkDestroyImage(this.gd.Device, this._optimalImage, null);
            }

            if (this._memoryBlock.DeviceMemory.Handle != 0) {
                this.gd.MemoryManager.Free(this._memoryBlock);
            }
        }
    }

    /// <summary>
    /// Executes the DisposeCore operation.
    /// </summary>
    private protected override void DisposeCore() {
        this.RefCount.Decrement();
    }
}