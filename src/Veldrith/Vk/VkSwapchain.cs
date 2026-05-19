using System;
using System.Linq;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk
{
    internal unsafe class VkSwapchain : Swapchain
    {
        public override Framebuffer Framebuffer => this._framebuffer;

        public override bool IsDisposed => this._disposed;

        public VkSwapchainKHR DeviceSwapchain => this._deviceSwapchain;
        public uint ImageIndex => this._currentImageIndex;
        public Vulkan.VkFence ImageAvailableFence => this._imageAvailableFence;
        public VkSurfaceKHR Surface { get; }

        public VkQueue PresentQueue => this._presentQueue;
        public uint PresentQueueIndex => this._presentQueueIndex;
        public ResourceRefCount RefCount { get; }

        public override string Name
        {
            get => this._name;
            set
            {
                this._name = value;
                gd.SetResourceName(this, value);
            }
        }

        public override bool SyncToVerticalBlank
        {
            get => this._newSyncToVBlank ?? this._syncToVBlank;
            set
            {
                if (this._syncToVBlank != value) this._newSyncToVBlank = value;
            }
        }

        private bool _allowTearing;

        public bool AllowTearing
        {
            get => this._allowTearing;
            set
            {
                if (this._allowTearing == value)
                    return;

                this._allowTearing = value;

                RecreateAndReacquire(this._framebuffer.Width, this._framebuffer.Height);
            }
        }

        private readonly VkGraphicsDevice gd;
        private readonly VkSwapchainFramebuffer _framebuffer;
        private readonly uint _presentQueueIndex;
        private readonly VkQueue _presentQueue;
        private readonly bool _colorSrgb;
        private VkSwapchainKHR _deviceSwapchain;
        private Vulkan.VkFence _imageAvailableFence;
        private bool _syncToVBlank;
        private bool? _newSyncToVBlank;
        private uint _currentImageIndex;
        private string _name;
        private bool _disposed;

        public VkSwapchain(VkGraphicsDevice gd, ref SwapchainDescription description)
            : this(gd, ref description, VkSurfaceKHR.Null)
        {
        }

        public VkSwapchain(VkGraphicsDevice gd, ref SwapchainDescription description, VkSurfaceKHR existingSurface)
        {
            this.gd = gd;
            this._syncToVBlank = description.SyncToVerticalBlank;
            this._colorSrgb = description.ColorSrgb;

            SwapchainSource swapchainSource = description.Source;

            Surface = existingSurface == VkSurfaceKHR.Null
                ? VkSurfaceUtil.CreateSurface(gd, gd.Instance, swapchainSource)
                : existingSurface;

            if (!GetPresentQueueIndex(out this._presentQueueIndex)) throw new VeldridException("The system does not support presenting the given Vulkan surface.");

            vkGetDeviceQueue(this.gd.Device, this._presentQueueIndex, 0, out this._presentQueue);

            this._framebuffer = new VkSwapchainFramebuffer(gd, this, Surface, description.Width, description.Height, description.DepthFormat);

            CreateSwapchain(description.Width, description.Height);

            var fenceCi = VkFenceCreateInfo.New();
            fenceCi.flags = VkFenceCreateFlags.None;
            vkCreateFence(this.gd.Device, ref fenceCi, null, out this._imageAvailableFence);

            AcquireNextImage(this.gd.Device, VkSemaphore.Null, this._imageAvailableFence);
            vkWaitForFences(this.gd.Device, 1, ref this._imageAvailableFence, true, ulong.MaxValue);
            vkResetFences(this.gd.Device, 1, ref this._imageAvailableFence);

            RefCount = new ResourceRefCount(DisposeCore);
        }

        #region Disposal

        public override void Dispose()
        {
            RefCount.Decrement();
        }

        #endregion

        public override void Resize(uint width, uint height)
        {
            RecreateAndReacquire(width, height);
        }

        public bool AcquireNextImage(VkDevice device, VkSemaphore semaphore, Vulkan.VkFence fence)
        {
            if (this._newSyncToVBlank != null)
            {
                this._syncToVBlank = this._newSyncToVBlank.Value;
                this._newSyncToVBlank = null;
                RecreateAndReacquire(this._framebuffer.Width, this._framebuffer.Height);
                return false;
            }

            var result = vkAcquireNextImageKHR(
                device,
                this._deviceSwapchain,
                ulong.MaxValue,
                semaphore,
                fence,
                ref this._currentImageIndex);
            this._framebuffer.SetImageIndex(this._currentImageIndex);

            if (result == VkResult.ErrorOutOfDateKHR || result == VkResult.SuboptimalKHR)
            {
                CreateSwapchain(this._framebuffer.Width, this._framebuffer.Height);
                return false;
            }

            if (result != VkResult.Success) throw new VeldridException("Could not acquire next image from the Vulkan swapchain.");

            return true;
        }

        private void RecreateAndReacquire(uint width, uint height)
        {
            if (CreateSwapchain(width, height))
            {
                if (AcquireNextImage(gd.Device, VkSemaphore.Null, this._imageAvailableFence))
                {
                    vkWaitForFences(gd.Device, 1, ref this._imageAvailableFence, true, ulong.MaxValue);
                    vkResetFences(gd.Device, 1, ref this._imageAvailableFence);
                }
            }
        }

        private bool CreateSwapchain(uint width, uint height)
        {
            // Obtain the surface capabilities first -- this will indicate whether the surface has been lost.
            var result = vkGetPhysicalDeviceSurfaceCapabilitiesKHR(gd.PhysicalDevice, Surface, out var surfaceCapabilities);
            if (result == VkResult.ErrorSurfaceLostKHR) throw new VeldridException("The Swapchain's underlying surface has been lost.");

            if (surfaceCapabilities.minImageExtent.width == 0 && surfaceCapabilities.minImageExtent.height == 0
                                                              && surfaceCapabilities.maxImageExtent.width == 0 && surfaceCapabilities.maxImageExtent.height == 0)
                return false;

            if (this._deviceSwapchain != VkSwapchainKHR.Null) gd.WaitForIdle();

            this._currentImageIndex = 0;
            uint surfaceFormatCount = 0;
            result = vkGetPhysicalDeviceSurfaceFormatsKHR(gd.PhysicalDevice, Surface, ref surfaceFormatCount, null);
            CheckResult(result);
            var formats = new VkSurfaceFormatKHR[surfaceFormatCount];
            result = vkGetPhysicalDeviceSurfaceFormatsKHR(gd.PhysicalDevice, Surface, ref surfaceFormatCount, out formats[0]);
            CheckResult(result);

            var desiredFormat = this._colorSrgb
                ? VkFormat.B8g8r8a8Srgb
                : VkFormat.B8g8r8a8Unorm;

            var surfaceFormat = new VkSurfaceFormatKHR();

            if (formats.Length == 1 && formats[0].format == VkFormat.Undefined)
                surfaceFormat = new VkSurfaceFormatKHR { colorSpace = VkColorSpaceKHR.SrgbNonlinearKHR, format = desiredFormat };
            else
            {
                foreach (var format in formats)
                {
                    if (format.colorSpace == VkColorSpaceKHR.SrgbNonlinearKHR && format.format == desiredFormat)
                    {
                        surfaceFormat = format;
                        break;
                    }
                }

                if (surfaceFormat.format == VkFormat.Undefined)
                {
                    if (this._colorSrgb && surfaceFormat.format != VkFormat.R8g8b8a8Srgb) throw new VeldridException("Unable to create an sRGB Swapchain for this surface.");

                    surfaceFormat = formats[0];
                }
            }

            uint presentModeCount = 0;
            result = vkGetPhysicalDeviceSurfacePresentModesKHR(gd.PhysicalDevice, Surface, ref presentModeCount, null);
            CheckResult(result);
            var presentModes = new VkPresentModeKHR[presentModeCount];
            result = vkGetPhysicalDeviceSurfacePresentModesKHR(gd.PhysicalDevice, Surface, ref presentModeCount, out presentModes[0]);
            CheckResult(result);

            var presentMode = VkPresentModeKHR.FifoKHR;

            if (this._syncToVBlank)
            {
                if (presentModes.Contains(VkPresentModeKHR.FifoRelaxedKHR))
                    presentMode = VkPresentModeKHR.FifoRelaxedKHR;
            }
            else if (this._allowTearing && presentModes.Contains(VkPresentModeKHR.ImmediateKHR))
                presentMode = VkPresentModeKHR.ImmediateKHR;
            else if (presentModes.Contains(VkPresentModeKHR.MailboxKHR))
                presentMode = VkPresentModeKHR.MailboxKHR;
            else if (presentModes.Contains(VkPresentModeKHR.ImmediateKHR))
                presentMode = VkPresentModeKHR.ImmediateKHR;

            uint maxImageCount = surfaceCapabilities.maxImageCount == 0 ? uint.MaxValue : surfaceCapabilities.maxImageCount;
            uint imageCount = Math.Min(maxImageCount, surfaceCapabilities.minImageCount + 1);

            var swapchainCi = VkSwapchainCreateInfoKHR.New();
            swapchainCi.surface = Surface;
            swapchainCi.presentMode = presentMode;
            swapchainCi.imageFormat = surfaceFormat.format;
            swapchainCi.imageColorSpace = surfaceFormat.colorSpace;
            uint clampedWidth = Util.Clamp(width, surfaceCapabilities.minImageExtent.width, surfaceCapabilities.maxImageExtent.width);
            uint clampedHeight = Util.Clamp(height, surfaceCapabilities.minImageExtent.height, surfaceCapabilities.maxImageExtent.height);
            swapchainCi.imageExtent = new VkExtent2D { width = clampedWidth, height = clampedHeight };
            swapchainCi.minImageCount = imageCount;
            swapchainCi.imageArrayLayers = 1;
            swapchainCi.imageUsage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst;

            var queueFamilyIndices = new FixedArray2<uint>(gd.GraphicsQueueIndex, gd.PresentQueueIndex);

            if (gd.GraphicsQueueIndex != gd.PresentQueueIndex)
            {
                swapchainCi.imageSharingMode = VkSharingMode.Concurrent;
                swapchainCi.queueFamilyIndexCount = 2;
                swapchainCi.pQueueFamilyIndices = &queueFamilyIndices.First;
            }
            else
            {
                swapchainCi.imageSharingMode = VkSharingMode.Exclusive;
                swapchainCi.queueFamilyIndexCount = 0;
            }

            swapchainCi.preTransform = VkSurfaceTransformFlagsKHR.IdentityKHR;
            swapchainCi.compositeAlpha = VkCompositeAlphaFlagsKHR.OpaqueKHR;
            swapchainCi.clipped = true;

            var oldSwapchain = this._deviceSwapchain;
            swapchainCi.oldSwapchain = oldSwapchain;

            result = vkCreateSwapchainKHR(gd.Device, ref swapchainCi, null, out this._deviceSwapchain);
            CheckResult(result);
            if (oldSwapchain != VkSwapchainKHR.Null) vkDestroySwapchainKHR(gd.Device, oldSwapchain, null);

            this._framebuffer.SetNewSwapchain(this._deviceSwapchain, width, height, surfaceFormat, swapchainCi.imageExtent);
            return true;
        }

        private bool GetPresentQueueIndex(out uint queueFamilyIndex)
        {
            uint deviceGraphicsQueueIndex = gd.GraphicsQueueIndex;
            uint devicePresentQueueIndex = gd.PresentQueueIndex;

            if (QueueSupportsPresent(deviceGraphicsQueueIndex, Surface))
            {
                queueFamilyIndex = deviceGraphicsQueueIndex;
                return true;
            }

            if (deviceGraphicsQueueIndex != devicePresentQueueIndex && QueueSupportsPresent(devicePresentQueueIndex, Surface))
            {
                queueFamilyIndex = devicePresentQueueIndex;
                return true;
            }

            queueFamilyIndex = 0;
            return false;
        }

        private bool QueueSupportsPresent(uint queueFamilyIndex, VkSurfaceKHR surface)
        {
            var result = vkGetPhysicalDeviceSurfaceSupportKHR(
                gd.PhysicalDevice,
                queueFamilyIndex,
                surface,
                out var supported);
            CheckResult(result);
            return supported;
        }

        private void DisposeCore()
        {
            vkDestroyFence(gd.Device, this._imageAvailableFence, null);
            this._framebuffer.Dispose();
            vkDestroySwapchainKHR(gd.Device, this._deviceSwapchain, null);
            vkDestroySurfaceKHR(gd.Instance, Surface, null);

            this._disposed = true;
        }
    }
}
