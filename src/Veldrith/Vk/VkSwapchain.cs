using System;
using System.Linq;
using Vortice.Vulkan;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkSwapchain.
/// </summary>
internal unsafe class VkSwapchain : Swapchain {

    /// <summary>
    /// Stores the color srgb state used by this instance.
    /// </summary>
    private readonly bool _colorSrgb;

    /// <summary>
    /// Stores the framebuffer state used by this instance.
    /// </summary>
    private readonly VkSwapchainFramebuffer _framebuffer;

    /// <summary>
    /// Stores the present queue state used by this instance.
    /// </summary>
    private readonly VkQueue _presentQueue;

    /// <summary>
    /// Stores the present queue index value used during command execution.
    /// </summary>
    private readonly uint _presentQueueIndex;

    /// <summary>
    /// Stores the graphics device used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

    /// <summary>
    /// Stores the allow tearing state used by this instance.
    /// </summary>
    private bool _allowTearing;

    /// <summary>
    /// Stores the current image index value used during command execution.
    /// </summary>
    private uint _currentImageIndex;

    /// <summary>
    /// Stores the device swapchain state used by this instance.
    /// </summary>
    private VkSwapchainKHR _deviceSwapchain;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the image available fence state used by this instance.
    /// </summary>
    private global::Vortice.Vulkan.VkFence _imageAvailableFence;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Stores the new sync to vblank state used by this instance.
    /// </summary>
    private bool? _newSyncToVBlank;

    /// <summary>
    /// Stores the sync to vblank state used by this instance.
    /// </summary>
    private bool _syncToVBlank;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkSwapchain" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public VkSwapchain(VkGraphicsDevice gd, ref SwapchainDescription description)
        : this(gd, ref description, default) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VkSwapchain" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="existingSurface">The existing surface value used by this operation.</param>
    public VkSwapchain(VkGraphicsDevice gd, ref SwapchainDescription description, VkSurfaceKHR existingSurface) {
        this._gd = gd;
        this._syncToVBlank = description.SyncToVerticalBlank;
        this._colorSrgb = description.ColorSrgb;

        SwapchainSource swapchainSource = description.Source;

        this.Surface = existingSurface.IsNull
            ? VkSurfaceUtil.CreateSurface(gd, gd.Instance, swapchainSource)
            : existingSurface;

        if (!this.GetPresentQueueIndex(out this._presentQueueIndex)) {
            throw new VeldridException("The system does not support presenting the given Vulkan surface.");
        }

        this._gd.DeviceApi.vkGetDeviceQueue(this._presentQueueIndex, 0, out this._presentQueue);

        this._framebuffer = new VkSwapchainFramebuffer(gd, this, this.Surface, description.Width, description.Height, description.DepthFormat);

        this.CreateSwapchain(description.Width, description.Height);

        VkFenceCreateInfo fenceCi = new VkFenceCreateInfo();
        fenceCi.flags = VkFenceCreateFlags.None;
        this._gd.DeviceApi.vkCreateFence(ref fenceCi, null, out this._imageAvailableFence);

        this.AcquireNextImage(this._gd.Device, default, this._imageAvailableFence);
        this._gd.DeviceApi.vkWaitForFences(this._imageAvailableFence, true, ulong.MaxValue);
        this._gd.DeviceApi.vkResetFences(this._imageAvailableFence);

        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    /// <summary>
    /// Gets or sets Framebuffer.
    /// </summary>
    public override Framebuffer Framebuffer => this._framebuffer;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Stores the device swapchain state used by this instance.
    /// </summary>
    public VkSwapchainKHR DeviceSwapchain => this._deviceSwapchain;

    /// <summary>
    /// Stores the image index value used during command execution.
    /// </summary>
    public uint ImageIndex => this._currentImageIndex;

    /// <summary>
    /// Stores the image available fence state used by this instance.
    /// </summary>
    public global::Vortice.Vulkan.VkFence ImageAvailableFence => this._imageAvailableFence;

    /// <summary>
    /// Gets or sets Surface.
    /// </summary>
    public VkSurfaceKHR Surface { get; }

    /// <summary>
    /// Stores the present queue state used by this instance.
    /// </summary>
    public VkQueue PresentQueue => this._presentQueue;

    /// <summary>
    /// Stores the present queue index value used during command execution.
    /// </summary>
    public uint PresentQueueIndex => this._presentQueueIndex;

    /// <summary>
    /// Gets or sets RefCount.
    /// </summary>
    public ResourceRefCount RefCount { get; }

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
    /// Gets or sets SyncToVerticalBlank.
    /// </summary>
    public override bool SyncToVerticalBlank {
        get => this._newSyncToVBlank ?? this._syncToVBlank;
        set {
            if (this._syncToVBlank != value) {
                this._newSyncToVBlank = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets AllowTearing.
    /// </summary>
    public bool AllowTearing {
        get => this._allowTearing;
        set {
            if (this._allowTearing == value) {
                return;
            }

            this._allowTearing = value;

            this.RecreateAndReacquire(this._framebuffer.Width, this._framebuffer.Height);
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
    /// Executes the resize logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    public override void Resize(uint width, uint height) {
        this.RecreateAndReacquire(width, height);
    }

    /// <summary>
    /// Executes the acquire next image logic for this backend.
    /// </summary>
    /// <param name="device">The device value used by this operation.</param>
    /// <param name="semaphore">The semaphore value used by this operation.</param>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool AcquireNextImage(VkDevice device, VkSemaphore semaphore, global::Vortice.Vulkan.VkFence fence) {
        if (this._newSyncToVBlank != null) {
            this._syncToVBlank = this._newSyncToVBlank.Value;
            this._newSyncToVBlank = null;
            this.RecreateAndReacquire(this._framebuffer.Width, this._framebuffer.Height);
            return false;
        }

        VkResult result = VulkanDispatch.GetApi(device).vkAcquireNextImageKHR(this._deviceSwapchain, ulong.MaxValue, semaphore, fence, out this._currentImageIndex);
        this._framebuffer.SetImageIndex(this._currentImageIndex);

        if (result == VkResult.ErrorOutOfDateKHR || result == VkResult.SuboptimalKHR) {
            this.CreateSwapchain(this._framebuffer.Width, this._framebuffer.Height);
            return false;
        }

        if (result != VkResult.Success) {
            throw new VeldridException("Could not acquire next image from the Vulkan swapchain.");
        }

        return true;
    }

    /// <summary>
    /// Executes the recreate and reacquire logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    private void RecreateAndReacquire(uint width, uint height) {
        if (this.CreateSwapchain(width, height)) {
            if (this.AcquireNextImage(this._gd.Device, default, this._imageAvailableFence)) {
                this._gd.DeviceApi.vkWaitForFences(this._imageAvailableFence, true, ulong.MaxValue);
                this._gd.DeviceApi.vkResetFences(this._imageAvailableFence);
            }
        }
    }

    /// <summary>
    /// Creates the swapchain instance used by this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool CreateSwapchain(uint width, uint height) {
        // Obtain the surface capabilities first -- this will indicate whether the surface has been lost.
        VkResult result = this._gd.InstanceApi.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(this._gd.PhysicalDevice, this.Surface, out VkSurfaceCapabilitiesKHR surfaceCapabilities);
        if (result == VkResult.ErrorSurfaceLostKHR) {
            throw new VeldridException("The Swapchain's underlying surface has been lost.");
        }

        if (surfaceCapabilities.minImageExtent.width == 0 && surfaceCapabilities.minImageExtent.height == 0
                                                          && surfaceCapabilities.maxImageExtent.width == 0 && surfaceCapabilities.maxImageExtent.height == 0) {
            return false;
        }

        if (this._deviceSwapchain.IsNotNull) {
            this._gd.WaitForIdle();
        }

        this._currentImageIndex = 0;
        uint surfaceFormatCount = 0;
        result = this._gd.InstanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(this._gd.PhysicalDevice, this.Surface, &surfaceFormatCount, null);
        CheckResult(result);
        VkSurfaceFormatKHR[] formats = new VkSurfaceFormatKHR[surfaceFormatCount];
        fixed (VkSurfaceFormatKHR* formatsPtr = formats) {
            result = this._gd.InstanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(this._gd.PhysicalDevice, this.Surface, &surfaceFormatCount, formatsPtr);
            CheckResult(result);
        }

        VkFormat desiredFormat = this._colorSrgb
            ? VkFormat.B8G8R8A8Srgb
            : VkFormat.B8G8R8A8Unorm;

        VkSurfaceFormatKHR surfaceFormat = new();

        if (formats.Length == 1 && formats[0].format == VkFormat.Undefined) {
            surfaceFormat = new VkSurfaceFormatKHR { colorSpace = VkColorSpaceKHR.SrgbNonLinear, format = desiredFormat };
        }
        else {
            foreach (VkSurfaceFormatKHR format in formats) {
                if (format.colorSpace == VkColorSpaceKHR.SrgbNonLinear && format.format == desiredFormat) {
                    surfaceFormat = format;
                    break;
                }
            }

            if (surfaceFormat.format == VkFormat.Undefined) {
                if (this._colorSrgb && surfaceFormat.format != VkFormat.R8G8B8A8Srgb) {
                    throw new VeldridException("Unable to create an sRGB Swapchain for this surface.");
                }

                surfaceFormat = formats[0];
            }
        }

        uint presentModeCount = 0;
        result = this._gd.InstanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(this._gd.PhysicalDevice, this.Surface, &presentModeCount, null);
        CheckResult(result);
        VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
        fixed (VkPresentModeKHR* presentModesPtr = presentModes) {
            result = this._gd.InstanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(this._gd.PhysicalDevice, this.Surface, &presentModeCount, presentModesPtr);
            CheckResult(result);
        }

        VkPresentModeKHR presentMode = VkPresentModeKHR.Fifo;

        if (this._syncToVBlank) {
            if (presentModes.Contains(VkPresentModeKHR.FifoRelaxed)) {
                presentMode = VkPresentModeKHR.FifoRelaxed;
            }
        }
        else if (this._allowTearing && presentModes.Contains(VkPresentModeKHR.Immediate)) {
            presentMode = VkPresentModeKHR.Immediate;
        }
        else if (presentModes.Contains(VkPresentModeKHR.Mailbox)) {
            presentMode = VkPresentModeKHR.Mailbox;
        }
        else if (presentModes.Contains(VkPresentModeKHR.Immediate)) {
            presentMode = VkPresentModeKHR.Immediate;
        }

        uint maxImageCount = surfaceCapabilities.maxImageCount == 0 ? uint.MaxValue : surfaceCapabilities.maxImageCount;
        uint imageCount = Math.Min(maxImageCount, surfaceCapabilities.minImageCount + 1);

        VkSwapchainCreateInfoKHR swapchainCi = new VkSwapchainCreateInfoKHR();
        swapchainCi.surface = this.Surface;
        swapchainCi.presentMode = presentMode;
        swapchainCi.imageFormat = surfaceFormat.format;
        swapchainCi.imageColorSpace = surfaceFormat.colorSpace;
        uint clampedWidth = Util.Clamp(width, surfaceCapabilities.minImageExtent.width, surfaceCapabilities.maxImageExtent.width);
        uint clampedHeight = Util.Clamp(height, surfaceCapabilities.minImageExtent.height, surfaceCapabilities.maxImageExtent.height);
        swapchainCi.imageExtent = new VkExtent2D { width = clampedWidth, height = clampedHeight };
        swapchainCi.minImageCount = imageCount;
        swapchainCi.imageArrayLayers = 1;
        swapchainCi.imageUsage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst;

        FixedArray2<uint> queueFamilyIndices = new(this._gd.GraphicsQueueIndex, this._gd.PresentQueueIndex);

        if (this._gd.GraphicsQueueIndex != this._gd.PresentQueueIndex) {
            swapchainCi.imageSharingMode = VkSharingMode.Concurrent;
            swapchainCi.queueFamilyIndexCount = 2;
            swapchainCi.pQueueFamilyIndices = &queueFamilyIndices.First;
        }
        else {
            swapchainCi.imageSharingMode = VkSharingMode.Exclusive;
            swapchainCi.queueFamilyIndexCount = 0;
        }

        swapchainCi.preTransform = VkSurfaceTransformFlagsKHR.Identity;
        swapchainCi.compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque;
        swapchainCi.clipped = true;

        VkSwapchainKHR oldSwapchain = this._deviceSwapchain;
        swapchainCi.oldSwapchain = oldSwapchain;

        fixed (VkSwapchainKHR* swapchainPtr = &this._deviceSwapchain) {
            result = this._gd.DeviceApi.vkCreateSwapchainKHR(&swapchainCi, null, swapchainPtr);
        }
        CheckResult(result);
        if (oldSwapchain.IsNotNull) {
            this._gd.DeviceApi.vkDestroySwapchainKHR(oldSwapchain, null);
        }

        this._framebuffer.SetNewSwapchain(this._deviceSwapchain, width, height, surfaceFormat, swapchainCi.imageExtent);
        return true;
    }

    /// <summary>
    /// Gets the present queue index value.
    /// </summary>
    /// <param name="queueFamilyIndex">The queue family index value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool GetPresentQueueIndex(out uint queueFamilyIndex) {
        uint deviceGraphicsQueueIndex = this._gd.GraphicsQueueIndex;
        uint devicePresentQueueIndex = this._gd.PresentQueueIndex;

        if (this.QueueSupportsPresent(deviceGraphicsQueueIndex, this.Surface)) {
            queueFamilyIndex = deviceGraphicsQueueIndex;
            return true;
        }

        if (deviceGraphicsQueueIndex != devicePresentQueueIndex && this.QueueSupportsPresent(devicePresentQueueIndex, this.Surface)) {
            queueFamilyIndex = devicePresentQueueIndex;
            return true;
        }

        queueFamilyIndex = 0;
        return false;
    }

    /// <summary>
    /// Executes the queue supports present logic for this backend.
    /// </summary>
    /// <param name="queueFamilyIndex">The queue family index value used by this operation.</param>
    /// <param name="surface">The surface value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool QueueSupportsPresent(uint queueFamilyIndex, VkSurfaceKHR surface) {
        VkResult result = this._gd.InstanceApi.vkGetPhysicalDeviceSurfaceSupportKHR(this._gd.PhysicalDevice, queueFamilyIndex, surface, out VkBool32 supported);
        CheckResult(result);
        return supported;
    }

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private void DisposeCore() {
        this._gd.DeviceApi.vkDestroyFence(this._imageAvailableFence, null);
        this._framebuffer.Dispose();
        this._gd.DeviceApi.vkDestroySwapchainKHR(this._deviceSwapchain, null);
        this._gd.InstanceApi.vkDestroySurfaceKHR(this.Surface, null);

        this._disposed = true;
    }
}
