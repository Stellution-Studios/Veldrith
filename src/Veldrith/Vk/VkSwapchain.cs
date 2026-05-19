using System;
using System.Linq;
using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

internal unsafe class VkSwapchain : Swapchain {

    /// <summary>
    /// Represents the _colorSrgb field.
    /// </summary>
    private readonly bool _colorSrgb;

    /// <summary>
    /// Represents the _framebuffer field.
    /// </summary>
    private readonly VkSwapchainFramebuffer _framebuffer;

    /// <summary>
    /// Represents the _presentQueue field.
    /// </summary>
    private readonly VkQueue _presentQueue;

    /// <summary>
    /// Represents the _presentQueueIndex field.
    /// </summary>
    private readonly uint _presentQueueIndex;

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Represents the _allowTearing field.
    /// </summary>
    private bool _allowTearing;

    /// <summary>
    /// Represents the _currentImageIndex field.
    /// </summary>
    private uint _currentImageIndex;

    /// <summary>
    /// Represents the _deviceSwapchain field.
    /// </summary>
    private VkSwapchainKHR _deviceSwapchain;

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Represents the _imageAvailableFence field.
    /// </summary>
    private Vulkan.VkFence _imageAvailableFence;

    /// <summary>
    /// Represents the _name field.
    /// </summary>
    private string _name;

    /// <summary>
    /// Represents the _newSyncToVBlank field.
    /// </summary>
    private bool? _newSyncToVBlank;

    /// <summary>
    /// Represents the _syncToVBlank field.
    /// </summary>
    private bool _syncToVBlank;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkSwapchain" /> class.
    /// </summary>
    public VkSwapchain(VkGraphicsDevice gd, ref SwapchainDescription description)
        : this(gd, ref description, VkSurfaceKHR.Null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VkSwapchain" /> class.
    /// </summary>
    public VkSwapchain(VkGraphicsDevice gd, ref SwapchainDescription description, VkSurfaceKHR existingSurface) {
        this.gd = gd;
        this._syncToVBlank = description.SyncToVerticalBlank;
        this._colorSrgb = description.ColorSrgb;

        SwapchainSource swapchainSource = description.Source;

        this.Surface = existingSurface == VkSurfaceKHR.Null
            ? VkSurfaceUtil.CreateSurface(gd, gd.Instance, swapchainSource)
            : existingSurface;

        if (!this.GetPresentQueueIndex(out this._presentQueueIndex)) {
            throw new VeldridException("The system does not support presenting the given Vulkan surface.");
        }

        vkGetDeviceQueue(this.gd.Device, this._presentQueueIndex, 0, out this._presentQueue);

        this._framebuffer = new VkSwapchainFramebuffer(gd, this, this.Surface, description.Width, description.Height, description.DepthFormat);

        this.CreateSwapchain(description.Width, description.Height);

        VkFenceCreateInfo fenceCi = VkFenceCreateInfo.New();
        fenceCi.flags = VkFenceCreateFlags.None;
        vkCreateFence(this.gd.Device, ref fenceCi, null, out this._imageAvailableFence);

        this.AcquireNextImage(this.gd.Device, VkSemaphore.Null, this._imageAvailableFence);
        vkWaitForFences(this.gd.Device, 1, ref this._imageAvailableFence, true, ulong.MaxValue);
        vkResetFences(this.gd.Device, 1, ref this._imageAvailableFence);

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
    /// Represents the DeviceSwapchain field.
    /// </summary>
    public VkSwapchainKHR DeviceSwapchain => this._deviceSwapchain;

    /// <summary>
    /// Represents the ImageIndex field.
    /// </summary>
    public uint ImageIndex => this._currentImageIndex;

    /// <summary>
    /// Represents the ImageAvailableFence field.
    /// </summary>
    public Vulkan.VkFence ImageAvailableFence => this._imageAvailableFence;

    /// <summary>
    /// Gets or sets Surface.
    /// </summary>
    public VkSurfaceKHR Surface { get; }

    /// <summary>
    /// Represents the PresentQueue field.
    /// </summary>
    public VkQueue PresentQueue => this._presentQueue;

    /// <summary>
    /// Represents the PresentQueueIndex field.
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
            this.gd.SetResourceName(this, value);
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
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes Resize.
    /// </summary>
    public override void Resize(uint width, uint height) {
        this.RecreateAndReacquire(width, height);
    }

    /// <summary>
    /// Executes AcquireNextImage.
    /// </summary>
    public bool AcquireNextImage(VkDevice device, VkSemaphore semaphore, Vulkan.VkFence fence) {
        if (this._newSyncToVBlank != null) {
            this._syncToVBlank = this._newSyncToVBlank.Value;
            this._newSyncToVBlank = null;
            this.RecreateAndReacquire(this._framebuffer.Width, this._framebuffer.Height);
            return false;
        }

        VkResult result = vkAcquireNextImageKHR(device, this._deviceSwapchain, ulong.MaxValue, semaphore, fence, ref this._currentImageIndex);
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
    /// Executes RecreateAndReacquire.
    /// </summary>
    private void RecreateAndReacquire(uint width, uint height) {
        if (this.CreateSwapchain(width, height)) {
            if (this.AcquireNextImage(this.gd.Device, VkSemaphore.Null, this._imageAvailableFence)) {
                vkWaitForFences(this.gd.Device, 1, ref this._imageAvailableFence, true, ulong.MaxValue);
                vkResetFences(this.gd.Device, 1, ref this._imageAvailableFence);
            }
        }
    }

    /// <summary>
    /// Executes CreateSwapchain.
    /// </summary>
    private bool CreateSwapchain(uint width, uint height) {
        // Obtain the surface capabilities first -- this will indicate whether the surface has been lost.
        VkResult result = vkGetPhysicalDeviceSurfaceCapabilitiesKHR(this.gd.PhysicalDevice, this.Surface, out VkSurfaceCapabilitiesKHR surfaceCapabilities);
        if (result == VkResult.ErrorSurfaceLostKHR) {
            throw new VeldridException("The Swapchain's underlying surface has been lost.");
        }

        if (surfaceCapabilities.minImageExtent.width == 0 && surfaceCapabilities.minImageExtent.height == 0
                                                          && surfaceCapabilities.maxImageExtent.width == 0 && surfaceCapabilities.maxImageExtent.height == 0) {
            return false;
        }

        if (this._deviceSwapchain != VkSwapchainKHR.Null) {
            this.gd.WaitForIdle();
        }

        this._currentImageIndex = 0;
        uint surfaceFormatCount = 0;
        result = vkGetPhysicalDeviceSurfaceFormatsKHR(this.gd.PhysicalDevice, this.Surface, ref surfaceFormatCount, null);
        CheckResult(result);
        VkSurfaceFormatKHR[] formats = new VkSurfaceFormatKHR[surfaceFormatCount];
        result = vkGetPhysicalDeviceSurfaceFormatsKHR(this.gd.PhysicalDevice, this.Surface, ref surfaceFormatCount, out formats[0]);
        CheckResult(result);

        VkFormat desiredFormat = this._colorSrgb
            ? VkFormat.B8g8r8a8Srgb
            : VkFormat.B8g8r8a8Unorm;

        VkSurfaceFormatKHR surfaceFormat = new();

        if (formats.Length == 1 && formats[0].format == VkFormat.Undefined) {
            surfaceFormat = new VkSurfaceFormatKHR { colorSpace = VkColorSpaceKHR.SrgbNonlinearKHR, format = desiredFormat };
        }
        else {
            foreach (VkSurfaceFormatKHR format in formats) {
                if (format.colorSpace == VkColorSpaceKHR.SrgbNonlinearKHR && format.format == desiredFormat) {
                    surfaceFormat = format;
                    break;
                }
            }

            if (surfaceFormat.format == VkFormat.Undefined) {
                if (this._colorSrgb && surfaceFormat.format != VkFormat.R8g8b8a8Srgb) {
                    throw new VeldridException("Unable to create an sRGB Swapchain for this surface.");
                }

                surfaceFormat = formats[0];
            }
        }

        uint presentModeCount = 0;
        result = vkGetPhysicalDeviceSurfacePresentModesKHR(this.gd.PhysicalDevice, this.Surface, ref presentModeCount, null);
        CheckResult(result);
        VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
        result = vkGetPhysicalDeviceSurfacePresentModesKHR(this.gd.PhysicalDevice, this.Surface, ref presentModeCount, out presentModes[0]);
        CheckResult(result);

        VkPresentModeKHR presentMode = VkPresentModeKHR.FifoKHR;

        if (this._syncToVBlank) {
            if (presentModes.Contains(VkPresentModeKHR.FifoRelaxedKHR)) {
                presentMode = VkPresentModeKHR.FifoRelaxedKHR;
            }
        }
        else if (this._allowTearing && presentModes.Contains(VkPresentModeKHR.ImmediateKHR)) {
            presentMode = VkPresentModeKHR.ImmediateKHR;
        }
        else if (presentModes.Contains(VkPresentModeKHR.MailboxKHR)) {
            presentMode = VkPresentModeKHR.MailboxKHR;
        }
        else if (presentModes.Contains(VkPresentModeKHR.ImmediateKHR)) {
            presentMode = VkPresentModeKHR.ImmediateKHR;
        }

        uint maxImageCount = surfaceCapabilities.maxImageCount == 0 ? uint.MaxValue : surfaceCapabilities.maxImageCount;
        uint imageCount = Math.Min(maxImageCount, surfaceCapabilities.minImageCount + 1);

        VkSwapchainCreateInfoKHR swapchainCi = VkSwapchainCreateInfoKHR.New();
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

        FixedArray2<uint> queueFamilyIndices = new(this.gd.GraphicsQueueIndex, this.gd.PresentQueueIndex);

        if (this.gd.GraphicsQueueIndex != this.gd.PresentQueueIndex) {
            swapchainCi.imageSharingMode = VkSharingMode.Concurrent;
            swapchainCi.queueFamilyIndexCount = 2;
            swapchainCi.pQueueFamilyIndices = &queueFamilyIndices.First;
        }
        else {
            swapchainCi.imageSharingMode = VkSharingMode.Exclusive;
            swapchainCi.queueFamilyIndexCount = 0;
        }

        swapchainCi.preTransform = VkSurfaceTransformFlagsKHR.IdentityKHR;
        swapchainCi.compositeAlpha = VkCompositeAlphaFlagsKHR.OpaqueKHR;
        swapchainCi.clipped = true;

        VkSwapchainKHR oldSwapchain = this._deviceSwapchain;
        swapchainCi.oldSwapchain = oldSwapchain;

        result = vkCreateSwapchainKHR(this.gd.Device, ref swapchainCi, null, out this._deviceSwapchain);
        CheckResult(result);
        if (oldSwapchain != VkSwapchainKHR.Null) {
            vkDestroySwapchainKHR(this.gd.Device, oldSwapchain, null);
        }

        this._framebuffer.SetNewSwapchain(this._deviceSwapchain, width, height, surfaceFormat, swapchainCi.imageExtent);
        return true;
    }

    /// <summary>
    /// Executes GetPresentQueueIndex.
    /// </summary>
    private bool GetPresentQueueIndex(out uint queueFamilyIndex) {
        uint deviceGraphicsQueueIndex = this.gd.GraphicsQueueIndex;
        uint devicePresentQueueIndex = this.gd.PresentQueueIndex;

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
    /// Executes QueueSupportsPresent.
    /// </summary>
    private bool QueueSupportsPresent(uint queueFamilyIndex, VkSurfaceKHR surface) {
        VkResult result = vkGetPhysicalDeviceSurfaceSupportKHR(this.gd.PhysicalDevice, queueFamilyIndex, surface, out VkBool32 supported);
        CheckResult(result);
        return supported;
    }

    /// <summary>
    /// Executes DisposeCore.
    /// </summary>
    private void DisposeCore() {
        vkDestroyFence(this.gd.Device, this._imageAvailableFence, null);
        this._framebuffer.Dispose();
        vkDestroySwapchainKHR(this.gd.Device, this._deviceSwapchain, null);
        vkDestroySurfaceKHR(this.gd.Instance, this.Surface, null);

        this._disposed = true;
    }
}