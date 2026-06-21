using System.Diagnostics;
using Vortice.Vulkan;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkTexture.
/// </summary>
internal unsafe class VkTexture : Texture {

    /// <summary>
    /// Stores the image layouts state used by this instance.
    /// </summary>
    private readonly VkImageLayout[] _imageLayouts;

    /// <summary>
    /// Stores the memory block state used by this instance.
    /// </summary>
    private readonly VkMemoryBlock _memoryBlock;

    /// <summary>
    /// Stores the optimal image state used by this instance.
    /// </summary>
    private readonly VkImage _optimalImage;

    /// <summary>
    /// Stores the staging buffer state used by this instance.
    /// </summary>
    private readonly global::Vortice.Vulkan.VkBuffer _stagingBuffer;

    /// <summary>
    /// Stores the graphics device used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

    /// <summary>
    /// Stores the destroyed state used by this instance.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Stores the depth value used during command execution.
    /// </summary>
    private uint _depth;

    /// <summary>
    /// Stores the texture format used by this instance.
    /// </summary>
    private PixelFormat _format; // Static for regular images -- may change for shared staging images

    /// <summary>
    /// Stores the height value used during command execution.
    /// </summary>
    private uint _height;

    // Immutable except for shared staging Textures.

    /// <summary>
    /// Stores the width value used during command execution.
    /// </summary>
    private uint _width;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkTexture" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    internal VkTexture(VkGraphicsDevice gd, ref TextureDescription description) {
        this._gd = gd;
        this._width = description.Width;
        this._height = description.Height;
        this._depth = description.Depth;
        this.MipLevels = description.MipLevels;
        this.ArrayLayers = description.ArrayLayers;
        bool isCubemap = (description.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap;
        this.ActualArrayLayers = isCubemap
            ? 6 * this.ArrayLayers
            : this.ArrayLayers;
        this._format = description.Format;
        this.Usage = description.Usage;
        this.Type = description.Type;
        this.SampleCount = description.SampleCount;
        this.VkSampleCount = VkFormats.VdToVkSampleCount(this.SampleCount);
        this.VkFormat = VkFormats.VdToVkPixelFormat(this.Format, (description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil);

        bool isStaging = (this.Usage & TextureUsage.Staging) == TextureUsage.Staging;

        if (!isStaging) {
            VkImageCreateInfo imageCi = new VkImageCreateInfo();
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
            VkResult result = gd.DeviceApi.vkCreateImage(ref imageCi, null, out this._optimalImage);
            CheckResult(result);

            VkMemoryRequirements memoryRequirements;
            bool prefersDedicatedAllocation;

            if (this._gd.GetImageMemoryRequirements2 != null) {
                VkImageMemoryRequirementsInfo2 memReqsInfo2 = new VkImageMemoryRequirementsInfo2();
                memReqsInfo2.image = this._optimalImage;
                VkMemoryRequirements2 memReqs2 = new VkMemoryRequirements2();
                VkMemoryDedicatedRequirements dedicatedReqs = new VkMemoryDedicatedRequirements();
                memReqs2.pNext = &dedicatedReqs;
                this._gd.GetImageMemoryRequirements2(this._gd.Device, &memReqsInfo2, &memReqs2);
                memoryRequirements = memReqs2.memoryRequirements;
                prefersDedicatedAllocation = dedicatedReqs.prefersDedicatedAllocation || dedicatedReqs.requiresDedicatedAllocation;
            }
            else {
                gd.DeviceApi.vkGetImageMemoryRequirements(this._optimalImage, out memoryRequirements);
                prefersDedicatedAllocation = false;
            }

            VkMemoryBlock memoryToken = gd.MemoryManager.Allocate(gd.PhysicalDeviceMemProperties, memoryRequirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal, false, memoryRequirements.size, memoryRequirements.alignment, prefersDedicatedAllocation, this._optimalImage, default);
            this._memoryBlock = memoryToken;
            result = gd.DeviceApi.vkBindImageMemory(this._optimalImage, this._memoryBlock.DeviceMemory, this._memoryBlock.Offset);
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

            VkBufferCreateInfo bufferCi = new VkBufferCreateInfo();
            bufferCi.usage = VkBufferUsageFlags.TransferSrc | VkBufferUsageFlags.TransferDst;
            bufferCi.size = stagingSize;
            VkResult result = this._gd.DeviceApi.vkCreateBuffer(ref bufferCi, null, out this._stagingBuffer);
            CheckResult(result);

            VkMemoryRequirements bufferMemReqs;
            bool prefersDedicatedAllocation;

            if (this._gd.GetBufferMemoryRequirements2 != null) {
                VkBufferMemoryRequirementsInfo2 memReqInfo2 = new VkBufferMemoryRequirementsInfo2();
                memReqInfo2.buffer = this._stagingBuffer;
                VkMemoryRequirements2 memReqs2 = new VkMemoryRequirements2();
                VkMemoryDedicatedRequirements dedicatedReqs = new VkMemoryDedicatedRequirements();
                memReqs2.pNext = &dedicatedReqs;
                this._gd.GetBufferMemoryRequirements2(this._gd.Device, &memReqInfo2, &memReqs2);
                bufferMemReqs = memReqs2.memoryRequirements;
                prefersDedicatedAllocation = dedicatedReqs.prefersDedicatedAllocation || dedicatedReqs.requiresDedicatedAllocation;
            }
            else {
                gd.DeviceApi.vkGetBufferMemoryRequirements(this._stagingBuffer, out bufferMemReqs);
                prefersDedicatedAllocation = false;
            }

            // Use "host cached" memory when available, for better performance of GPU -> CPU transfers
            VkMemoryPropertyFlags propertyFlags = VkMemoryPropertyFlags.HostVisible |
                                                  VkMemoryPropertyFlags.HostCoherent | VkMemoryPropertyFlags.HostCached;
            if (!TryFindMemoryType(this._gd.PhysicalDeviceMemProperties, bufferMemReqs.memoryTypeBits, propertyFlags, out _)) {
                propertyFlags ^= VkMemoryPropertyFlags.HostCached;
            }

            this._memoryBlock = this._gd.MemoryManager.Allocate(this._gd.PhysicalDeviceMemProperties, bufferMemReqs.memoryTypeBits, propertyFlags, true, bufferMemReqs.size, bufferMemReqs.alignment, prefersDedicatedAllocation, default, this._stagingBuffer);

            result = this._gd.DeviceApi.vkBindBufferMemory(this._stagingBuffer, this._memoryBlock.DeviceMemory, this._memoryBlock.Offset);
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
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="mipLevels">The mip levels value used by this operation.</param>
    /// <param name="arrayLayers">The array layers value used by this operation.</param>
    /// <param name="vkFormat">The vk format value used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="sampleCount">The sample count value used by this operation.</param>
    /// <param name="existingImage">The existing image value used by this operation.</param>
    internal VkTexture(VkGraphicsDevice gd, uint width, uint height, uint mipLevels, uint arrayLayers, VkFormat vkFormat, TextureUsage usage, TextureSampleCount sampleCount, VkImage existingImage) {
        Debug.Assert(width > 0 && height > 0);
        this._gd = gd;
        this.MipLevels = mipLevels;
        this._width = width;
        this._height = height;
        this._depth = 1;
        this.VkFormat = vkFormat;
        this._format = VkFormats.VkToVdPixelFormat(this.VkFormat);
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
    public override uint Width => this._width;

    /// <summary>
    /// Gets or sets Height.
    /// </summary>
    public override uint Height => this._height;

    /// <summary>
    /// Gets or sets Depth.
    /// </summary>
    public override uint Depth => this._depth;

    /// <summary>
    /// Gets or sets Format.
    /// </summary>
    public override PixelFormat Format => this._format;

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
    /// Stores the optimal device image state used by this instance.
    /// </summary>
    public VkImage OptimalDeviceImage => this._optimalImage;

    /// <summary>
    /// Stores the staging buffer state used by this instance.
    /// </summary>
    public global::Vortice.Vulkan.VkBuffer StagingBuffer => this._stagingBuffer;

    /// <summary>
    /// Stores the memory state used by this instance.
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
            this._gd.SetResourceName(this, value);
        }
    }

    /// <summary>
    /// Gets the subresource layout value.
    /// </summary>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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

            VkSubresourceLayout layout;
            this._gd.DeviceApi.vkGetImageSubresourceLayout(this._optimalImage, &imageSubresource, &layout);
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
    /// Executes the transition image layout logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    /// <param name="baseMipLevel">The base mip level value used by this operation.</param>
    /// <param name="levelCount">The level count value used by this operation.</param>
    /// <param name="baseArrayLayer">The base array layer value used by this operation.</param>
    /// <param name="layerCount">The layer count value used by this operation.</param>
    /// <param name="newLayout">The new layout value used by this operation.</param>
    internal void TransitionImageLayout(VkCommandBuffer cb, uint baseMipLevel, uint levelCount, uint baseArrayLayer, uint layerCount, VkImageLayout newLayout) {
        if (this._stagingBuffer.IsNotNull) {
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

            VulkanUtil.TransitionImageLayout(this._gd.DeviceApi, cb, this.OptimalDeviceImage, baseMipLevel, levelCount, baseArrayLayer, layerCount, aspectMask, this._imageLayouts[this.CalculateSubresource(baseMipLevel, baseArrayLayer)], newLayout);

            for (uint level = 0; level < levelCount; level++) {
                for (uint layer = 0; layer < layerCount; layer++) {
                    this._imageLayouts[this.CalculateSubresource(baseMipLevel + level, baseArrayLayer + layer)] = newLayout;
                }
            }
        }
    }

    /// <summary>
    /// Executes the transition image layout nonmatching logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    /// <param name="baseMipLevel">The base mip level value used by this operation.</param>
    /// <param name="levelCount">The level count value used by this operation.</param>
    /// <param name="baseArrayLayer">The base array layer value used by this operation.</param>
    /// <param name="layerCount">The layer count value used by this operation.</param>
    /// <param name="newLayout">The new layout value used by this operation.</param>
    internal void TransitionImageLayoutNonmatching(VkCommandBuffer cb, uint baseMipLevel, uint levelCount, uint baseArrayLayer, uint layerCount, VkImageLayout newLayout) {
        if (this._stagingBuffer.IsNotNull) {
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

                    VulkanUtil.TransitionImageLayout(this._gd.DeviceApi, cb, this.OptimalDeviceImage, level, 1, layer, 1, aspectMask, oldLayout, newLayout);

                    this._imageLayouts[subresource] = newLayout;
                }
            }
        }
    }

    /// <summary>
    /// Gets the image layout value.
    /// </summary>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    /// <returns>The value produced by this operation.</returns>
    internal VkImageLayout GetImageLayout(uint mipLevel, uint arrayLayer) {
        return this._imageLayouts[this.CalculateSubresource(mipLevel, arrayLayer)];
    }

    /// <summary>
    /// Sets the staging dimensions value.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="format">The format used by this operation.</param>
    internal void SetStagingDimensions(uint width, uint height, uint depth, PixelFormat format) {
        Debug.Assert(this._stagingBuffer.IsNotNull);
        Debug.Assert(this.Usage == TextureUsage.Staging);
        this._width = width;
        this._height = height;
        this._depth = depth;
        this._format = format;
    }

    /// <summary>
    /// Sets the image layout value.
    /// </summary>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    /// <param name="layout">The resource layout used by this operation.</param>
    internal void SetImageLayout(uint mipLevel, uint arrayLayer, VkImageLayout layout) {
        this._imageLayouts[this.CalculateSubresource(mipLevel, arrayLayer)] = layout;
    }

    /// <summary>
    /// Executes the clear if render target logic for this backend.
    /// </summary>
    private void ClearIfRenderTarget() {
        // If the image is going to be used as a render target, we need to clear the data before its first use.
        if ((this.Usage & TextureUsage.RenderTarget) != 0) {
            this._gd.ClearColorTexture(this, new VkClearColorValue(0, 0, 0, 0));
        }
        else if ((this.Usage & TextureUsage.DepthStencil) != 0) {
            this._gd.ClearDepthTexture(this, new VkClearDepthStencilValue(0, 0));
        }
    }

    /// <summary>
    /// Executes the transition if sampled logic for this backend.
    /// </summary>
    private void TransitionIfSampled() {
        if ((this.Usage & TextureUsage.Sampled) != 0) {
            this._gd.TransitionImageLayout(this, VkImageLayout.ShaderReadOnlyOptimal);
        }
    }

    /// <summary>
    /// Executes the ref counted dispose logic for this backend.
    /// </summary>
    private void RefCountedDispose() {
        if (!this._destroyed) {
            base.Dispose();

            this._destroyed = true;

            bool isStaging = (this.Usage & TextureUsage.Staging) == TextureUsage.Staging;
            if (isStaging) {
                this._gd.DeviceApi.vkDestroyBuffer(this._stagingBuffer, null);
            }
            else {
                this._gd.DeviceApi.vkDestroyImage(this._optimalImage, null);
            }

            if (this._memoryBlock.DeviceMemory.Handle != 0) {
                this._gd.MemoryManager.Free(this._memoryBlock);
            }
        }
    }

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private protected override void DisposeCore() {
        this.RefCount.Decrement();
    }
}
