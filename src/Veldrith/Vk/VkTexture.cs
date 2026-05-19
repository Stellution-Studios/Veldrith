using System.Diagnostics;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

internal unsafe class VkTexture : Texture {
    private readonly VkImageLayout[] _imageLayouts;
    private readonly VkMemoryBlock _memoryBlock;
    private readonly VkImage _optimalImage;
    private readonly Vulkan.VkBuffer _stagingBuffer;

    private readonly VkGraphicsDevice gd;
    private bool _destroyed;
    private string _name;
    private uint depth;
    private PixelFormat format; // Static for regular images -- may change for shared staging images
    private uint height;

    // Immutable except for shared staging Textures.
    private uint width;

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
        this.VkFormat = VkFormats.VdToVkPixelFormat(this.Format,
            (description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil);

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
                prefersDedicatedAllocation = dedicatedReqs.prefersDedicatedAllocation ||
                                             dedicatedReqs.requiresDedicatedAllocation;
            }
            else {
                vkGetImageMemoryRequirements(gd.Device, this._optimalImage, out memoryRequirements);
                prefersDedicatedAllocation = false;
            }

            VkMemoryBlock memoryToken = gd.MemoryManager.Allocate(
                gd.PhysicalDeviceMemProperties,
                memoryRequirements.memoryTypeBits,
                VkMemoryPropertyFlags.DeviceLocal,
                false,
                memoryRequirements.size,
                memoryRequirements.alignment,
                prefersDedicatedAllocation,
                this._optimalImage,
                Vulkan.VkBuffer.Null);
            this._memoryBlock = memoryToken;
            result = vkBindImageMemory(gd.Device, this._optimalImage, this._memoryBlock.DeviceMemory,
                this._memoryBlock.Offset);
            CheckResult(result);

            this._imageLayouts = new VkImageLayout[subresourceCount];
            for (int i = 0; i < this._imageLayouts.Length; i++) {
                this._imageLayouts[i] = VkImageLayout.Preinitialized;
            }
        }
        else // isStaging
        {
            uint depthPitch = FormatHelpers.GetDepthPitch(
                FormatHelpers.GetRowPitch(this.Width, this.Format), this.Height, this.Format);
            uint stagingSize = depthPitch * this.Depth;

            for (uint level = 1; level < this.MipLevels; level++) {
                Util.GetMipDimensions(this, level, out uint mipWidth, out uint mipHeight, out uint mipDepth);

                depthPitch = FormatHelpers.GetDepthPitch(
                    FormatHelpers.GetRowPitch(mipWidth, this.Format),
                    mipHeight, this.Format);

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
                prefersDedicatedAllocation = dedicatedReqs.prefersDedicatedAllocation ||
                                             dedicatedReqs.requiresDedicatedAllocation;
            }
            else {
                vkGetBufferMemoryRequirements(gd.Device, this._stagingBuffer, out bufferMemReqs);
                prefersDedicatedAllocation = false;
            }

            // Use "host cached" memory when available, for better performance of GPU -> CPU transfers
            VkMemoryPropertyFlags propertyFlags = VkMemoryPropertyFlags.HostVisible |
                                                  VkMemoryPropertyFlags.HostCoherent | VkMemoryPropertyFlags.HostCached;
            if (!TryFindMemoryType(this.gd.PhysicalDeviceMemProperties, bufferMemReqs.memoryTypeBits, propertyFlags,
                    out _)) {
                propertyFlags ^= VkMemoryPropertyFlags.HostCached;
            }

            this._memoryBlock = this.gd.MemoryManager.Allocate(
                this.gd.PhysicalDeviceMemProperties,
                bufferMemReqs.memoryTypeBits,
                propertyFlags,
                true,
                bufferMemReqs.size,
                bufferMemReqs.alignment,
                prefersDedicatedAllocation,
                VkImage.Null,
                this._stagingBuffer);

            result = vkBindBufferMemory(this.gd.Device, this._stagingBuffer, this._memoryBlock.DeviceMemory,
                this._memoryBlock.Offset);
            CheckResult(result);
        }

        this.ClearIfRenderTarget();
        this.TransitionIfSampled();
        this.RefCount = new ResourceRefCount(this.RefCountedDispose);
    }

    // Used to construct Swapchain textures.
    internal VkTexture(
        VkGraphicsDevice gd,
        uint width,
        uint height,
        uint mipLevels,
        uint arrayLayers,
        VkFormat vkFormat,
        TextureUsage usage,
        TextureSampleCount sampleCount,
        VkImage existingImage) {
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

    public override uint Width => this.width;

    public override uint Height => this.height;

    public override uint Depth => this.depth;

    public override PixelFormat Format => this.format;

    public override uint MipLevels { get; }

    public override uint ArrayLayers { get; }
    public uint ActualArrayLayers { get; }

    public override TextureUsage Usage { get; }

    public override TextureType Type { get; }

    public override TextureSampleCount SampleCount { get; }

    public override bool IsDisposed => this._destroyed;

    public VkImage OptimalDeviceImage => this._optimalImage;
    public Vulkan.VkBuffer StagingBuffer => this._stagingBuffer;
    public VkMemoryBlock Memory => this._memoryBlock;

    public VkFormat VkFormat { get; }
    public VkSampleCountFlags VkSampleCount { get; }

    public ResourceRefCount RefCount { get; }
    public bool IsSwapchainTexture { get; }

    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

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

            vkGetImageSubresourceLayout(this.gd.Device, this._optimalImage, ref imageSubresource,
                out VkSubresourceLayout layout);
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

    internal void TransitionImageLayout(
        VkCommandBuffer cb,
        uint baseMipLevel,
        uint levelCount,
        uint baseArrayLayer,
        uint layerCount,
        VkImageLayout newLayout) {
        if (this._stagingBuffer != Vulkan.VkBuffer.Null) {
            return;
        }

        VkImageLayout oldLayout = this._imageLayouts[this.CalculateSubresource(baseMipLevel, baseArrayLayer)];
#if DEBUG
            for (uint level = 0; level < levelCount; level++)
            {
                for (uint layer = 0; layer < layerCount; layer++)
                {
                    if (imageLayouts[CalculateSubresource(baseMipLevel + level, baseArrayLayer + layer)] != oldLayout)
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

            VulkanUtil.TransitionImageLayout(
                cb, this.OptimalDeviceImage,
                baseMipLevel,
                levelCount,
                baseArrayLayer,
                layerCount,
                aspectMask,
                this._imageLayouts[this.CalculateSubresource(baseMipLevel, baseArrayLayer)],
                newLayout);

            for (uint level = 0; level < levelCount; level++) {
                for (uint layer = 0; layer < layerCount; layer++) {
                    this._imageLayouts[this.CalculateSubresource(baseMipLevel + level, baseArrayLayer + layer)] =
                        newLayout;
                }
            }
        }
    }

    internal void TransitionImageLayoutNonmatching(
        VkCommandBuffer cb,
        uint baseMipLevel,
        uint levelCount,
        uint baseArrayLayer,
        uint layerCount,
        VkImageLayout newLayout) {
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

                    VulkanUtil.TransitionImageLayout(
                        cb, this.OptimalDeviceImage,
                        level,
                        1,
                        layer,
                        1,
                        aspectMask,
                        oldLayout,
                        newLayout);

                    this._imageLayouts[subresource] = newLayout;
                }
            }
        }
    }

    internal VkImageLayout GetImageLayout(uint mipLevel, uint arrayLayer) {
        return this._imageLayouts[this.CalculateSubresource(mipLevel, arrayLayer)];
    }

    internal void SetStagingDimensions(uint width, uint height, uint depth, PixelFormat format) {
        Debug.Assert(this._stagingBuffer != Vulkan.VkBuffer.Null);
        Debug.Assert(this.Usage == TextureUsage.Staging);
        this.width = width;
        this.height = height;
        this.depth = depth;
        this.format = format;
    }

    internal void SetImageLayout(uint mipLevel, uint arrayLayer, VkImageLayout layout) {
        this._imageLayouts[this.CalculateSubresource(mipLevel, arrayLayer)] = layout;
    }

    private void ClearIfRenderTarget() {
        // If the image is going to be used as a render target, we need to clear the data before its first use.
        if ((this.Usage & TextureUsage.RenderTarget) != 0) {
            this.gd.ClearColorTexture(this, new VkClearColorValue(0, 0, 0, 0));
        }
        else if ((this.Usage & TextureUsage.DepthStencil) != 0) {
            this.gd.ClearDepthTexture(this, new VkClearDepthStencilValue(0, 0));
        }
    }

    private void TransitionIfSampled() {
        if ((this.Usage & TextureUsage.Sampled) != 0) {
            this.gd.TransitionImageLayout(this, VkImageLayout.ShaderReadOnlyOptimal);
        }
    }

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

    private protected override void DisposeCore() {
        this.RefCount.Decrement();
    }
}