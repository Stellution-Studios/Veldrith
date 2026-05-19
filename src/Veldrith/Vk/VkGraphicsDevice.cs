using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkGraphicsDevice.
/// </summary>
internal unsafe class VkGraphicsDevice : GraphicsDevice {

    /// <summary>
    /// Stores the vk instance create enumerate portability bit khr state used by this instance.
    /// </summary>
    private const uint _vk_instance_create_enumerate_portability_bit_khr = 0x00000001;

    /// <summary>
    /// Stores the shared command pool count value used during command execution.
    /// </summary>
    private const int _shared_command_pool_count = 4;

    // Staging Resources

    /// <summary>
    /// Stores the min staging buffer size value used during command execution.
    /// </summary>
    private const uint _min_staging_buffer_size = 64;

    /// <summary>
    /// Stores the max staging buffer size value used during command execution.
    /// </summary>
    private const uint _max_staging_buffer_size = 512;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private static readonly FixedUtf8String _s_name = "Veldrith-VkGraphicsDevice";

    /// <summary>
    /// Stores the s is supported state used by this instance.
    /// </summary>


    private static readonly Lazy<bool> _s_is_supported = new(checkIsSupported, true);

    /// <summary>
    /// Stores the available staging buffers collection used by this instance.
    /// </summary>
    private readonly List<VkBuffer> _availableStagingBuffers = new();

    /// <summary>
    /// Stores the available staging textures collection used by this instance.
    /// </summary>
    private readonly List<VkTexture> _availableStagingTextures = new();

    /// <summary>
    /// Stores the available submission fences collection used by this instance.
    /// </summary>
    private readonly ConcurrentQueue<Vulkan.VkFence> _availableSubmissionFences = new();

    /// <summary>
    /// Stores the filters state used by this instance.
    /// </summary>
    private readonly ConcurrentDictionary<VkFormat, VkFilter> _filters = new();

    /// <summary>
    /// Synchronizes access to the graphics command pool lock state.
    /// </summary>
    private readonly object _graphicsCommandPoolLock = new();

    /// <summary>
    /// Synchronizes access to the graphics queue lock state.
    /// </summary>
    private readonly object _graphicsQueueLock = new();

    /// <summary>
    /// Stores the main swapchain state used by this instance.
    /// </summary>
    private readonly VkSwapchain _mainSwapchain;

    /// <summary>
    /// Stores the shared graphics command pools collection used by this instance.
    /// </summary>
    private readonly Stack<SharedCommandPool> _sharedGraphicsCommandPools = new();

    /// <summary>
    /// Stores the staging resources lock collection used by this instance.
    /// </summary>
    private readonly object _stagingResourcesLock = new();

    /// <summary>
    /// Stores the submitted fences collection used by this instance.
    /// </summary>
    private readonly List<FenceSubmissionInfo> _submittedFences = new();

    /// <summary>
    /// Synchronizes access to the submitted fences lock state.
    /// </summary>
    private readonly object _submittedFencesLock = new();

    /// <summary>
    /// Stores the submitted shared command pools collection used by this instance.
    /// </summary>
    private readonly Dictionary<VkCommandBuffer, SharedCommandPool> _submittedSharedCommandPools = new();

    /// <summary>
    /// Stores the submitted staging buffers collection used by this instance.
    /// </summary>
    private readonly Dictionary<VkCommandBuffer, VkBuffer> _submittedStagingBuffers = new();

    /// <summary>
    /// Stores the submitted staging textures collection used by this instance.
    /// </summary>
    private readonly Dictionary<VkCommandBuffer, VkTexture> _submittedStagingTextures = new();

    /// <summary>
    /// Stores the surface extensions state used by this instance.
    /// </summary>
    private readonly List<FixedUtf8String> _surfaceExtensions = new();

    /// <summary>
    /// Stores the vulkan info state used by this instance.
    /// </summary>
    private readonly BackendInfoVulkan _vulkanInfo;

    /// <summary>
    /// Stores the api version state used by this instance.
    /// </summary>
    private GraphicsApiVersion _apiVersion;

    /// <summary>
    /// Stores the debug callback func state used by this instance.
    /// </summary>
    private PFN_vkDebugReportCallbackEXT _debugCallbackFunc;

    /// <summary>
    /// Stores the debug callback handle state used by this instance.
    /// </summary>
    private VkDebugReportCallbackEXT _debugCallbackHandle;

    /// <summary>
    /// Stores the debug marker enabled state used by this instance.
    /// </summary>
    private bool _debugMarkerEnabled;

    /// <summary>
    /// Stores the device name state used by this instance.
    /// </summary>
    private string _deviceName;

    /// <summary>
    /// Stores the get physical device properties2 state used by this instance.
    /// </summary>
    private VkGetPhysicalDeviceProperties2T _getPhysicalDeviceProperties2;

    /// <summary>
    /// Stores the graphics command pool state used by this instance.
    /// </summary>
    private VkCommandPool _graphicsCommandPool;

    /// <summary>
    /// Stores the graphics queue state used by this instance.
    /// </summary>
    private VkQueue _graphicsQueue;

    /// <summary>
    /// Stores the khronos validation supported state used by this instance.
    /// </summary>
    private bool _khronosValidationSupported;

    /// <summary>
    /// Stores the physical device features state used by this instance.
    /// </summary>
    private VkPhysicalDeviceFeatures _physicalDeviceFeatures;

    /// <summary>
    /// Stores the physical device mem properties state used by this instance.
    /// </summary>
    private VkPhysicalDeviceMemoryProperties _physicalDeviceMemProperties;

    /// <summary>
    /// Stores the physical device properties state used by this instance.
    /// </summary>
    private VkPhysicalDeviceProperties _physicalDeviceProperties;

    /// <summary>
    /// Stores the set object name delegate state used by this instance.
    /// </summary>
    private VkDebugMarkerSetObjectNameExtT _setObjectNameDelegate;

    /// <summary>
    /// Stores the standard clip ydirection state used by this instance.
    /// </summary>
    private bool _standardClipYDirection;

    /// <summary>
    /// Stores the standard validation supported state used by this instance.
    /// </summary>
    private bool _standardValidationSupported;

    /// <summary>
    /// Stores the vendor name state used by this instance.
    /// </summary>
    private string _vendorName;

    /// <summary>
    /// Stores the device state used by this instance.
    /// </summary>
    private VkDevice device;

    /// <summary>
    /// Stores the instance state used by this instance.
    /// </summary>
    private VkInstance instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkGraphicsDevice" /> type.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="scDesc">The sc desc value used by this operation.</param>
    public VkGraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? scDesc)
        : this(options, scDesc, new VulkanDeviceOptions()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VkGraphicsDevice" /> type.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="scDesc">The sc desc value used by this operation.</param>
    /// <param name="vkOptions">The vk options value used by this operation.</param>
    public VkGraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? scDesc, VulkanDeviceOptions vkOptions) {
        this.createInstance(options.Debug, vkOptions);

        VkSurfaceKHR surface = VkSurfaceKHR.Null;
        if (scDesc != null) {
            surface = VkSurfaceUtil.CreateSurface(this, this.instance, scDesc.Value.Source);
        }

        this.createPhysicalDevice();
        this.createLogicalDevice(surface, options.PreferStandardClipSpaceYDirection, vkOptions);

        this.MemoryManager = new VkDeviceMemoryManager(this.device, this._physicalDeviceProperties.limits.bufferImageGranularity, this.GetBufferMemoryRequirements2, this.GetImageMemoryRequirements2);

        this.Features = new GraphicsDeviceFeatures(true, this._physicalDeviceFeatures.geometryShader, this._physicalDeviceFeatures.tessellationShader, this._physicalDeviceFeatures.multiViewport, true, true, true, true, this._physicalDeviceFeatures.drawIndirectFirstInstance, this._physicalDeviceFeatures.fillModeNonSolid, this._physicalDeviceFeatures.samplerAnisotropy, this._physicalDeviceFeatures.depthClamp, true, this._physicalDeviceFeatures.independentBlend, true, true, this._debugMarkerEnabled, true, this._physicalDeviceFeatures.shaderFloat64);

        this.ResourceFactory = new VkResourceFactory(this);

        if (scDesc != null) {
            SwapchainDescription desc = scDesc.Value;
            this._mainSwapchain = new VkSwapchain(this, ref desc, surface);
        }

        this.createDescriptorPool();
        this.createGraphicsCommandPool();
        for (int i = 0; i < _shared_command_pool_count; i++) {
            this._sharedGraphicsCommandPools.Push(new SharedCommandPool(this, true));
        }

        this._vulkanInfo = new BackendInfoVulkan(this);

        this.PostDeviceCreated();
    }

    /// <summary>
    /// Gets or sets DeviceName.
    /// </summary>
    public override string DeviceName => this._deviceName;

    /// <summary>
    /// Gets or sets VendorName.
    /// </summary>
    public override string VendorName => this._vendorName;

    /// <summary>
    /// Gets or sets ApiVersion.
    /// </summary>
    public override GraphicsApiVersion ApiVersion => this._apiVersion;

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Vulkan;

    /// <summary>
    /// Gets or sets IsUvOriginTopLeft.
    /// </summary>
    public override bool IsUvOriginTopLeft => true;

    /// <summary>
    /// Gets or sets IsDepthRangeZeroToOne.
    /// </summary>
    public override bool IsDepthRangeZeroToOne => true;

    /// <summary>
    /// Gets or sets IsClipSpaceYInverted.
    /// </summary>
    public override bool IsClipSpaceYInverted => !this._standardClipYDirection;

    /// <summary>
    /// Gets or sets AllowTearing.
    /// </summary>
    public override bool AllowTearing {
        get => this._mainSwapchain.AllowTearing;
        set => this._mainSwapchain.AllowTearing = value;
    }

    /// <summary>
    /// Gets or sets MainSwapchain.
    /// </summary>
    public override Swapchain MainSwapchain => this._mainSwapchain;

    /// <summary>
    /// Gets or sets Features.
    /// </summary>
    public override GraphicsDeviceFeatures Features { get; }

    /// <summary>
    /// Stores the instance state used by this instance.
    /// </summary>
    public VkInstance Instance => this.instance;

    /// <summary>
    /// Stores the device state used by this instance.
    /// </summary>
    public VkDevice Device => this.device;

    /// <summary>
    /// Gets or sets PhysicalDevice.
    /// </summary>
    public VkPhysicalDevice PhysicalDevice { get; private set; }

    /// <summary>
    /// Stores the physical device mem properties state used by this instance.
    /// </summary>
    public VkPhysicalDeviceMemoryProperties PhysicalDeviceMemProperties => this._physicalDeviceMemProperties;

    /// <summary>
    /// Stores the graphics queue state used by this instance.
    /// </summary>
    public VkQueue GraphicsQueue => this._graphicsQueue;

    /// <summary>
    /// Gets or sets GraphicsQueueIndex.
    /// </summary>
    public uint GraphicsQueueIndex { get; private set; }

    /// <summary>
    /// Gets or sets PresentQueueIndex.
    /// </summary>
    public uint PresentQueueIndex { get; private set; }

    /// <summary>
    /// Gets or sets DriverName.
    /// </summary>
    public string DriverName { get; private set; }

    /// <summary>
    /// Gets or sets DriverInfo.
    /// </summary>
    public string DriverInfo { get; private set; }

    /// <summary>
    /// Gets or sets MemoryManager.
    /// </summary>
    public VkDeviceMemoryManager MemoryManager { get; }

    /// <summary>
    /// Gets or sets DescriptorPoolManager.
    /// </summary>
    public VkDescriptorPoolManager DescriptorPoolManager { get; private set; }

    /// <summary>
    /// Gets or sets MarkerBegin.
    /// </summary>
    public VkCmdDebugMarkerBeginExtT MarkerBegin { get; private set; }

    /// <summary>
    /// Gets or sets MarkerEnd.
    /// </summary>
    public VkCmdDebugMarkerEndExtT MarkerEnd { get; private set; }

    /// <summary>
    /// Gets or sets MarkerInsert.
    /// </summary>
    public VkCmdDebugMarkerInsertExtT MarkerInsert { get; private set; }

    /// <summary>
    /// Gets or sets GetBufferMemoryRequirements2.
    /// </summary>
    public VkGetBufferMemoryRequirements2T GetBufferMemoryRequirements2 { get; private set; }

    /// <summary>
    /// Gets or sets GetImageMemoryRequirements2.
    /// </summary>
    public VkGetImageMemoryRequirements2T GetImageMemoryRequirements2 { get; private set; }

    /// <summary>
    /// Gets or sets CreateMetalSurfaceExt.
    /// </summary>
    public VkCreateMetalSurfaceExtT CreateMetalSurfaceExt { get; private set; }

    /// <summary>
    /// Gets or sets ResourceFactory.
    /// </summary>
    public override ResourceFactory ResourceFactory { get; }

    /// <summary>
    /// Gets the vulkan info value.
    /// </summary>
    /// <param name="info">The info value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool GetVulkanInfo(out BackendInfoVulkan info) {
        info = this._vulkanInfo;
        return true;
    }

    /// <summary>
    /// Executes the has surface extension logic for this backend.
    /// </summary>
    /// <param name="extension">The extension value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool HasSurfaceExtension(FixedUtf8String extension) {
        return this._surfaceExtensions.Contains(extension);
    }

    /// <summary>
    /// Executes the enable debug callback logic for this backend.
    /// </summary>
    /// <param name="flags">The flags value used by this operation.</param>
    public void EnableDebugCallback(VkDebugReportFlagsEXT flags = VkDebugReportFlagsEXT.WarningEXT | VkDebugReportFlagsEXT.ErrorEXT) {
        Debug.WriteLine("Enabling Vulkan Debug callbacks.");
        this._debugCallbackFunc = this.debugCallback;
        IntPtr debugFunctionPtr = Marshal.GetFunctionPointerForDelegate(this._debugCallbackFunc);
        VkDebugReportCallbackCreateInfoEXT debugCallbackCi = VkDebugReportCallbackCreateInfoEXT.New();
        debugCallbackCi.flags = flags;
        debugCallbackCi.pfnCallback = debugFunctionPtr;
        IntPtr createFnPtr;
        using (FixedUtf8String debugExtFnName = "vkCreateDebugReportCallbackEXT") {
            createFnPtr = vkGetInstanceProcAddr(this.instance, debugExtFnName);
        }

        if (createFnPtr == IntPtr.Zero) {
            return;
        }

        VkCreateDebugReportCallbackExtD createDelegate = Marshal.GetDelegateForFunctionPointer<VkCreateDebugReportCallbackExtD>(createFnPtr);
        VkResult result = createDelegate(this.instance, &debugCallbackCi, IntPtr.Zero, out this._debugCallbackHandle);
        CheckResult(result);
    }

    /// <summary>
    /// Gets the device extension properties value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public VkExtensionProperties[] GetDeviceExtensionProperties() {
        uint propertyCount = 0;
        VkResult result = vkEnumerateDeviceExtensionProperties(this.PhysicalDevice, (byte*)null, &propertyCount, null);
        CheckResult(result);
        VkExtensionProperties[] props = new VkExtensionProperties[(int)propertyCount];

        fixed (VkExtensionProperties* properties = props) {
            result = vkEnumerateDeviceExtensionProperties(this.PhysicalDevice, (byte*)null, &propertyCount, properties);
            CheckResult(result);
        }

        return props;
    }

    /// <summary>
    /// Gets the sample count limit value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="depthFormat">The depth format value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat) {
        VkImageUsageFlags usageFlags = VkImageUsageFlags.Sampled;
        usageFlags |= depthFormat ? VkImageUsageFlags.DepthStencilAttachment : VkImageUsageFlags.ColorAttachment;

        vkGetPhysicalDeviceImageFormatProperties(this.PhysicalDevice, VkFormats.VdToVkPixelFormat(format), VkImageType.Image2D, VkImageTiling.Optimal, usageFlags, VkImageCreateFlags.None, out VkImageFormatProperties formatProperties);

        VkSampleCountFlags vkSampleCounts = formatProperties.sampleCounts;
        if ((vkSampleCounts & VkSampleCountFlags.Count32) == VkSampleCountFlags.Count32) {
            return TextureSampleCount.Count32;
        }

        if ((vkSampleCounts & VkSampleCountFlags.Count16) == VkSampleCountFlags.Count16) {
            return TextureSampleCount.Count16;
        }

        if ((vkSampleCounts & VkSampleCountFlags.Count8) == VkSampleCountFlags.Count8) {
            return TextureSampleCount.Count8;
        }

        if ((vkSampleCounts & VkSampleCountFlags.Count4) == VkSampleCountFlags.Count4) {
            return TextureSampleCount.Count4;
        }

        if ((vkSampleCounts & VkSampleCountFlags.Count2) == VkSampleCountFlags.Count2) {
            return TextureSampleCount.Count2;
        }

        return TextureSampleCount.Count1;
    }

    /// <summary>
    /// Executes the reset fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    public override void ResetFence(Fence fence) {
        Vulkan.VkFence vkFence = Util.AssertSubtype<Fence, VkFence>(fence).DeviceFence;
        vkResetFences(this.device, 1, ref vkFence);
    }

    /// <summary>
    /// Executes the wait for fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool WaitForFence(Fence fence, ulong nanosecondTimeout) {
        Vulkan.VkFence vkFence = Util.AssertSubtype<Fence, VkFence>(fence).DeviceFence;
        VkResult result = vkWaitForFences(this.device, 1, ref vkFence, true, nanosecondTimeout);
        return result == VkResult.Success;
    }

    /// <summary>
    /// Executes the wait for fences logic for this backend.
    /// </summary>
    /// <param name="fences">The synchronization fence used by this operation.</param>
    /// <param name="waitAll">The wait all value used by this operation.</param>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout) {
        int fenceCount = fences.Length;
        Vulkan.VkFence* fencesPtr = stackalloc Vulkan.VkFence[fenceCount];
        for (int i = 0; i < fenceCount; i++) {
            fencesPtr[i] = Util.AssertSubtype<Fence, VkFence>(fences[i]).DeviceFence;
        }

        VkResult result = vkWaitForFences(this.device, (uint)fenceCount, fencesPtr, waitAll, nanosecondTimeout);
        return result == VkResult.Success;
    }

    /// <summary>
    /// Executes the is supported logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal static bool IsSupported() {
        return _s_is_supported.Value;
    }

    /// <summary>
    /// Sets the resource name value.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="name">The name used by this operation.</param>
    internal void SetResourceName(IDeviceResource resource, string name) {
        if (this._debugMarkerEnabled) {
            switch (resource) {
                case VkBuffer buffer:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.BufferEXT, buffer.DeviceBuffer.Handle, name);
                    break;

                case VkCommandList commandList:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.CommandBufferEXT, (ulong)commandList.CommandBuffer.Handle, $"{name}_CommandBuffer");
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.CommandPoolEXT, commandList.CommandPool.Handle, $"{name}_CommandPool");
                    break;

                case VkFramebuffer framebuffer:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.FramebufferEXT, framebuffer.CurrentFramebuffer.Handle, name);
                    break;

                case VkPipeline pipeline:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.PipelineEXT, pipeline.DevicePipeline.Handle, name);
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.PipelineLayoutEXT, pipeline.PipelineLayout.Handle, name);
                    break;

                case VkResourceLayout resourceLayout:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.DescriptorSetLayoutEXT, resourceLayout.DescriptorSetLayout.Handle, name);
                    break;

                case VkResourceSet resourceSet:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.DescriptorSetEXT, resourceSet.DescriptorSet.Handle, name);
                    break;

                case VkSampler sampler:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.SamplerEXT, sampler.DeviceSampler.Handle, name);
                    break;

                case VkShader shader:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.ShaderModuleEXT, shader.ShaderModule.Handle, name);
                    break;

                case VkTexture tex:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.ImageEXT, tex.OptimalDeviceImage.Handle, name);
                    break;

                case VkTextureView texView:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.ImageViewEXT, texView.ImageView.Handle, name);
                    break;

                case VkFence fence:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.FenceEXT, fence.DeviceFence.Handle, name);
                    break;

                case VkSwapchain sc:
                    this.setDebugMarkerName(VkDebugReportObjectTypeEXT.SwapchainKHREXT, sc.DeviceSwapchain.Handle, name);
                    break;
            }
        }
    }

    /// <summary>
    /// Gets the format filter value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal VkFilter GetFormatFilter(VkFormat format) {
        if (!this._filters.TryGetValue(format, out VkFilter filter)) {
            vkGetPhysicalDeviceFormatProperties(this.PhysicalDevice, format, out VkFormatProperties vkFormatProps);
            filter = (vkFormatProps.optimalTilingFeatures & VkFormatFeatureFlags.SampledImageFilterLinear) != 0
                ? VkFilter.Linear
                : VkFilter.Nearest;
            this._filters.TryAdd(format, filter);
        }

        return filter;
    }

    /// <summary>
    /// Executes the clear color texture logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="color">The color value used by this operation.</param>
    internal void ClearColorTexture(VkTexture texture, VkClearColorValue color) {
        uint effectiveLayers = texture.ArrayLayers;
        if ((texture.Usage & TextureUsage.Cubemap) != 0) {
            effectiveLayers *= 6;
        }

        VkImageSubresourceRange range = new(VkImageAspectFlags.Color, 0, texture.MipLevels, 0, effectiveLayers);
        SharedCommandPool pool = this.getFreeCommandPool();
        VkCommandBuffer cb = pool.BeginNewCommandBuffer();
        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, VkImageLayout.TransferDstOptimal);
        vkCmdClearColorImage(cb, texture.OptimalDeviceImage, VkImageLayout.TransferDstOptimal, &color, 1, &range);
        VkImageLayout colorLayout = texture.IsSwapchainTexture
            ? VkImageLayout.PresentSrcKHR
            : VkImageLayout.ColorAttachmentOptimal;
        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, colorLayout);
        pool.EndAndSubmit(cb);
    }

    /// <summary>
    /// Executes the clear depth texture logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="clearValue">The clear value value used by this operation.</param>
    internal void ClearDepthTexture(VkTexture texture, VkClearDepthStencilValue clearValue) {
        uint effectiveLayers = texture.ArrayLayers;
        if ((texture.Usage & TextureUsage.Cubemap) != 0) {
            effectiveLayers *= 6;
        }

        VkImageAspectFlags aspect = FormatHelpers.IsStencilFormat(texture.Format)
            ? VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil
            : VkImageAspectFlags.Depth;
        VkImageSubresourceRange range = new(aspect, 0, texture.MipLevels, 0, effectiveLayers);
        SharedCommandPool pool = this.getFreeCommandPool();
        VkCommandBuffer cb = pool.BeginNewCommandBuffer();
        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, VkImageLayout.TransferDstOptimal);
        vkCmdClearDepthStencilImage(cb, texture.OptimalDeviceImage, VkImageLayout.TransferDstOptimal, &clearValue, 1, &range);
        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, VkImageLayout.DepthStencilAttachmentOptimal);
        pool.EndAndSubmit(cb);
    }

    /// <summary>
    /// Gets the uniform buffer min offset alignment core value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal override uint GetUniformBufferMinOffsetAlignmentCore() {
        return (uint)this._physicalDeviceProperties.limits.minUniformBufferOffsetAlignment;
    }

    /// <summary>
    /// Gets the structured buffer min offset alignment core value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal override uint GetStructuredBufferMinOffsetAlignmentCore() {
        return (uint)this._physicalDeviceProperties.limits.minStorageBufferOffsetAlignment;
    }

    /// <summary>
    /// Executes the transition image layout logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="layout">The resource layout used by this operation.</param>
    internal void TransitionImageLayout(VkTexture texture, VkImageLayout layout) {
        SharedCommandPool pool = this.getFreeCommandPool();
        VkCommandBuffer cb = pool.BeginNewCommandBuffer();
        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, texture.ActualArrayLayers, layout);
        pool.EndAndSubmit(cb);
    }

    /// <summary>
    /// Maps the core resource for CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource) {
        VkMemoryBlock memoryBlock;
        IntPtr mappedPtr = IntPtr.Zero;
        uint sizeInBytes;
        uint offset = 0;
        uint rowPitch = 0;
        uint depthPitch = 0;

        if (resource is VkBuffer buffer) {
            memoryBlock = buffer.Memory;
            sizeInBytes = buffer.SizeInBytes;
        }
        else {
            VkTexture texture = Util.AssertSubtype<IMappableResource, VkTexture>(resource);
            VkSubresourceLayout layout = texture.GetSubresourceLayout(subresource);
            memoryBlock = texture.Memory;
            sizeInBytes = (uint)layout.size;
            offset = (uint)layout.offset;
            rowPitch = (uint)layout.rowPitch;
            depthPitch = (uint)layout.depthPitch;
        }

        if (memoryBlock.DeviceMemory.Handle != 0) {
            if (memoryBlock.IsPersistentMapped) {
                mappedPtr = (IntPtr)memoryBlock.BlockMappedPointer;
            }
            else {
                mappedPtr = this.MemoryManager.Map(memoryBlock);
            }
        }

        byte* dataPtr = (byte*)mappedPtr.ToPointer() + offset;
        return new MappedResource(resource, mode, (IntPtr)dataPtr, sizeInBytes, subresource, rowPitch, depthPitch);
    }

    /// <summary>
    /// Unmaps the core resource from CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    protected override void UnmapCore(IMappableResource resource, uint subresource) {
        VkMemoryBlock memoryBlock;

        if (resource is VkBuffer buffer) {
            memoryBlock = buffer.Memory;
        }
        else {
            VkTexture tex = Util.AssertSubtype<IMappableResource, VkTexture>(resource);
            memoryBlock = tex.Memory;
        }

        if (memoryBlock.DeviceMemory.Handle != 0 && !memoryBlock.IsPersistentMapped) {
            vkUnmapMemory(this.device, memoryBlock.DeviceMemory);
        }
    }

    /// <summary>
    /// Executes the platform dispose logic for this backend.
    /// </summary>
    protected override void PlatformDispose() {
        Debug.Assert(this._submittedFences.Count == 0);
        foreach (Vulkan.VkFence fence in this._availableSubmissionFences) {
            vkDestroyFence(this.device, fence, null);
        }

        this._mainSwapchain?.Dispose();

        if (this._debugCallbackFunc != null) {
            this._debugCallbackFunc = null;
            FixedUtf8String debugExtFnName = "vkDestroyDebugReportCallbackEXT";
            IntPtr destroyFuncPtr = vkGetInstanceProcAddr(this.instance, debugExtFnName);
            VkDestroyDebugReportCallbackExtD destroyDel
                = Marshal.GetDelegateForFunctionPointer<VkDestroyDebugReportCallbackExtD>(destroyFuncPtr);
            destroyDel(this.instance, this._debugCallbackHandle, null);
        }

        this.DescriptorPoolManager.DestroyAll();
        vkDestroyCommandPool(this.device, this._graphicsCommandPool, null);

        Debug.Assert(this._submittedStagingTextures.Count == 0);
        foreach (VkTexture tex in this._availableStagingTextures) {
            tex.Dispose();
        }

        Debug.Assert(this._submittedStagingBuffers.Count == 0);
        foreach (VkBuffer buffer in this._availableStagingBuffers) {
            buffer.Dispose();
        }

        lock (this._graphicsCommandPoolLock) {
            while (this._sharedGraphicsCommandPools.Count > 0) {
                SharedCommandPool sharedPool = this._sharedGraphicsCommandPools.Pop();
                sharedPool.Destroy();
            }
        }

        this.MemoryManager.Dispose();

        VkResult result = vkDeviceWaitIdle(this.device);
        CheckResult(result);
        vkDestroyDevice(this.device, null);
        vkDestroyInstance(this.instance, null);
    }

    /// <summary>
    /// Executes the check is supported logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool checkIsSupported() {
        if (!IsVulkanLoaded()) {
            return false;
        }

        VkInstanceCreateInfo instanceCi = VkInstanceCreateInfo.New();
        VkApplicationInfo applicationInfo = new() {
            apiVersion = new VkVersion(1, 0, 0),
            applicationVersion = new VkVersion(1, 0, 0),
            engineVersion = new VkVersion(1, 0, 0),
            pApplicationName = _s_name,
            pEngineName = _s_name
        };

        instanceCi.pApplicationInfo = &applicationInfo;

        VkResult result = vkCreateInstance(ref instanceCi, null, out VkInstance testInstance);
        if (result != VkResult.Success) {
            return false;
        }

        uint physicalDeviceCount = 0;
        result = vkEnumeratePhysicalDevices(testInstance, ref physicalDeviceCount, null);

        if (result != VkResult.Success || physicalDeviceCount == 0) {
            vkDestroyInstance(testInstance, null);
            return false;
        }

        vkDestroyInstance(testInstance, null);

        HashSet<string> instanceExtensions = new(GetInstanceExtensions());
        if (!instanceExtensions.Contains(CommonStrings.VkKhrSurfaceExtensionName)) {
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            return instanceExtensions.Contains(CommonStrings.VkKhrWin32SurfaceExtensionName);
        }
#if NET5_0_OR_GREATER

        if (OperatingSystem.IsAndroid()) {
            return instanceExtensions.Contains(CommonStrings.VkKhrAndroidSurfaceExtensionName);
        }
#endif

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            if (RuntimeInformation.OSDescription.Contains("Unix")) // Android
            {
                return instanceExtensions.Contains(CommonStrings.VkKhrAndroidSurfaceExtensionName);
            }

            return instanceExtensions.Contains(CommonStrings.VkKhrXlibSurfaceExtensionName);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            if (RuntimeInformation.OSDescription.Contains("Darwin")) // macOS
            {
                return instanceExtensions.Contains(CommonStrings.VkMvkMacosSurfaceExtensionName);
            }

            // iOS
            return instanceExtensions.Contains(CommonStrings.VkMvkIOSSurfaceExtensionName);
        }

        return false;
    }

    /// <summary>
    /// Executes the submit command list logic for this backend.
    /// </summary>
    /// <param name="cl">The cl value used by this operation.</param>
    /// <param name="waitSemaphoreCount">The wait semaphore count value used by this operation.</param>
    /// <param name="waitSemaphoresPtr">The wait semaphores ptr value used by this operation.</param>
    /// <param name="signalSemaphoreCount">The signal semaphore count value used by this operation.</param>
    /// <param name="signalSemaphoresPtr">The signal semaphores ptr value used by this operation.</param>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    private void submitCommandList(CommandList cl, uint waitSemaphoreCount, VkSemaphore* waitSemaphoresPtr, uint signalSemaphoreCount, VkSemaphore* signalSemaphoresPtr, Fence fence) {
        VkCommandList vkCl = Util.AssertSubtype<CommandList, VkCommandList>(cl);
        VkCommandBuffer vkCb = vkCl.CommandBuffer;

        vkCl.CommandBufferSubmitted(vkCb);
        this.submitCommandBuffer(vkCl, vkCb, waitSemaphoreCount, waitSemaphoresPtr, signalSemaphoreCount, signalSemaphoresPtr, fence);
    }

    /// <summary>
    /// Executes the submit command buffer logic for this backend.
    /// </summary>
    /// <param name="vkCl">The vk cl value used by this operation.</param>
    /// <param name="vkCb">The vk cb value used by this operation.</param>
    /// <param name="waitSemaphoreCount">The wait semaphore count value used by this operation.</param>
    /// <param name="waitSemaphoresPtr">The wait semaphores ptr value used by this operation.</param>
    /// <param name="signalSemaphoreCount">The signal semaphore count value used by this operation.</param>
    /// <param name="signalSemaphoresPtr">The signal semaphores ptr value used by this operation.</param>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    private void submitCommandBuffer(VkCommandList vkCl, VkCommandBuffer vkCb, uint waitSemaphoreCount, VkSemaphore* waitSemaphoresPtr, uint signalSemaphoreCount, VkSemaphore* signalSemaphoresPtr, Fence fence) {
        this.checkSubmittedFences();

        bool useExtraFence = fence != null;
        VkSubmitInfo si = VkSubmitInfo.New();
        si.commandBufferCount = 1;
        si.pCommandBuffers = &vkCb;
        VkPipelineStageFlags waitDstStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
        si.pWaitDstStageMask = &waitDstStageMask;

        si.pWaitSemaphores = waitSemaphoresPtr;
        si.waitSemaphoreCount = waitSemaphoreCount;
        si.pSignalSemaphores = signalSemaphoresPtr;
        si.signalSemaphoreCount = signalSemaphoreCount;

        Vulkan.VkFence vkFence;
        Vulkan.VkFence submissionFence;

        if (useExtraFence) {
            vkFence = Util.AssertSubtype<Fence, VkFence>(fence).DeviceFence;
            submissionFence = this.getFreeSubmissionFence();
        }
        else {
            vkFence = this.getFreeSubmissionFence();
            submissionFence = vkFence;
        }

        lock (this._graphicsQueueLock) {
            VkResult result = vkQueueSubmit(this._graphicsQueue, 1, ref si, vkFence);
            CheckResult(result);

            if (useExtraFence) {
                result = vkQueueSubmit(this._graphicsQueue, 0, null, submissionFence);
                CheckResult(result);
            }
        }

        lock (this._submittedFencesLock) {
            this._submittedFences.Add(new FenceSubmissionInfo(submissionFence, vkCl, vkCb));
        }
    }

    /// <summary>
    /// Executes the check submitted fences logic for this backend.
    /// </summary>
    private void checkSubmittedFences() {
        lock (this._submittedFencesLock) {
            for (int i = 0; i < this._submittedFences.Count; i++) {
                FenceSubmissionInfo fsi = this._submittedFences[i];

                if (vkGetFenceStatus(this.device, fsi.Fence) == VkResult.Success) {
                    this.completeFenceSubmission(fsi);
                    this._submittedFences.RemoveAt(i);
                    i -= 1;
                }
                else {
                    break; // Submissions are in order; later submissions cannot complete if this one hasn't.
                }
            }
        }
    }

    /// <summary>
    /// Executes the complete fence submission logic for this backend.
    /// </summary>
    /// <param name="fsi">The fsi value used by this operation.</param>
    private void completeFenceSubmission(FenceSubmissionInfo fsi) {
        Vulkan.VkFence fence = fsi.Fence;
        VkCommandBuffer completedCb = fsi.CommandBuffer;
        fsi.CommandList?.CommandBufferCompleted(completedCb);
        VkResult resetResult = vkResetFences(this.device, 1, ref fence);
        CheckResult(resetResult);
        this.returnSubmissionFence(fence);

        lock (this._stagingResourcesLock) {
            if (this._submittedStagingTextures.Remove(completedCb, out VkTexture stagingTex)) {
                this._availableStagingTextures.Add(stagingTex);
            }

            if (this._submittedStagingBuffers.Remove(completedCb, out VkBuffer stagingBuffer)) {
                if (stagingBuffer.SizeInBytes <= _max_staging_buffer_size) {
                    this._availableStagingBuffers.Add(stagingBuffer);
                }
                else {
                    stagingBuffer.Dispose();
                }
            }

            if (this._submittedSharedCommandPools.Remove(completedCb, out SharedCommandPool sharedPool)) {
                lock (this._graphicsCommandPoolLock) {
                    if (sharedPool.IsCached) {
                        this._sharedGraphicsCommandPools.Push(sharedPool);
                    }
                    else {
                        sharedPool.Destroy();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Executes the return submission fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    private void returnSubmissionFence(Vulkan.VkFence fence) {
        this._availableSubmissionFences.Enqueue(fence);
    }

    /// <summary>
    /// Gets the free submission fence value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private Vulkan.VkFence getFreeSubmissionFence() {
        if (this._availableSubmissionFences.TryDequeue(out Vulkan.VkFence availableFence)) {
            return availableFence;
        }

        VkFenceCreateInfo fenceCi = VkFenceCreateInfo.New();
        VkResult result = vkCreateFence(this.device, ref fenceCi, null, out Vulkan.VkFence newFence);
        CheckResult(result);
        return newFence;
    }

    /// <summary>
    /// Sets the debug marker name value.
    /// </summary>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="name">The name used by this operation.</param>
    private void setDebugMarkerName(VkDebugReportObjectTypeEXT type, ulong target, string name) {
        Debug.Assert(this._setObjectNameDelegate != null);

        VkDebugMarkerObjectNameInfoEXT nameInfo = VkDebugMarkerObjectNameInfoEXT.New();
        nameInfo.objectType = type;
        nameInfo.@object = target;

        int byteCount = Encoding.UTF8.GetByteCount(name);
        byte* utf8Ptr = stackalloc byte[byteCount + 1];
        fixed (char* namePtr = name) {
            Encoding.UTF8.GetBytes(namePtr, name.Length, utf8Ptr, byteCount);
        }

        utf8Ptr[byteCount] = 0;

        nameInfo.pObjectName = utf8Ptr;
        VkResult result = this._setObjectNameDelegate(this.device, &nameInfo);
        CheckResult(result);
    }

    /// <summary>
    /// Creates the instance instance used by this backend.
    /// </summary>
    /// <param name="debug">The debug value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    private void createInstance(bool debug, VulkanDeviceOptions options) {
        HashSet<string> availableInstanceLayers = new(EnumerateInstanceLayers());
        HashSet<string> availableInstanceExtensions = new(GetInstanceExtensions());

        VkInstanceCreateInfo instanceCi = VkInstanceCreateInfo.New();
        VkApplicationInfo applicationInfo = new() {
            apiVersion = new VkVersion(1, 0, 0),
            applicationVersion = new VkVersion(1, 0, 0),
            engineVersion = new VkVersion(1, 0, 0),
            pApplicationName = _s_name,
            pEngineName = _s_name
        };

        instanceCi.pApplicationInfo = &applicationInfo;

        StackList<IntPtr, Size64Bytes> instanceExtensions = new();
        StackList<IntPtr, Size64Bytes> instanceLayers = new();

        if (availableInstanceExtensions.Contains(CommonStrings.VkKhrPortabilitySubset)) {
            this._surfaceExtensions.Add(CommonStrings.VkKhrPortabilitySubset);
        }

        if (availableInstanceExtensions.Contains(CommonStrings.VkKhrSurfaceExtensionName)) {
            this._surfaceExtensions.Add(CommonStrings.VkKhrSurfaceExtensionName);
        }

        if (availableInstanceExtensions.Contains(CommonStrings.VkKhrPortabilityEnumeration)) {
            instanceExtensions.Add(CommonStrings.VkKhrPortabilityEnumeration);
            instanceCi.flags |= _vk_instance_create_enumerate_portability_bit_khr;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            if (availableInstanceExtensions.Contains(CommonStrings.VkKhrWin32SurfaceExtensionName)) {
                this._surfaceExtensions.Add(CommonStrings.VkKhrWin32SurfaceExtensionName);
            }
        }
        else if (
#if NET5_0_OR_GREATER
            OperatingSystem.IsAndroid() ||
#endif
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            if (availableInstanceExtensions.Contains(CommonStrings.VkKhrAndroidSurfaceExtensionName)) {
                this._surfaceExtensions.Add(CommonStrings.VkKhrAndroidSurfaceExtensionName);
            }

            if (availableInstanceExtensions.Contains(CommonStrings.VkKhrXlibSurfaceExtensionName)) {
                this._surfaceExtensions.Add(CommonStrings.VkKhrXlibSurfaceExtensionName);
            }

            if (availableInstanceExtensions.Contains(CommonStrings.VkKhrWaylandSurfaceExtensionName)) {
                this._surfaceExtensions.Add(CommonStrings.VkKhrWaylandSurfaceExtensionName);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            if (availableInstanceExtensions.Contains(CommonStrings.VkExtMetalSurfaceExtensionName)) {
                this._surfaceExtensions.Add(CommonStrings.VkExtMetalSurfaceExtensionName);
            }
            else // Legacy MoltenVK extensions
            {
                if (availableInstanceExtensions.Contains(CommonStrings.VkMvkMacosSurfaceExtensionName)) {
                    this._surfaceExtensions.Add(CommonStrings.VkMvkMacosSurfaceExtensionName);
                }

                if (availableInstanceExtensions.Contains(CommonStrings.VkMvkIOSSurfaceExtensionName)) {
                    this._surfaceExtensions.Add(CommonStrings.VkMvkIOSSurfaceExtensionName);
                }
            }
        }

        foreach (FixedUtf8String ext in this._surfaceExtensions) {
            instanceExtensions.Add(ext);
        }

        bool hasDeviceProperties2 = availableInstanceExtensions.Contains(CommonStrings.VkKhrGetPhysicalDeviceProperties2);
        if (hasDeviceProperties2) {
            instanceExtensions.Add(CommonStrings.VkKhrGetPhysicalDeviceProperties2);
        }

        string[] requestedInstanceExtensions = options.InstanceExtensions ?? Array.Empty<string>();
        List<FixedUtf8String> tempStrings = new();

        foreach (string requiredExt in requestedInstanceExtensions) {
            if (!availableInstanceExtensions.Contains(requiredExt)) {
                throw new VeldridException($"The required instance extension was not available: {requiredExt}");
            }

            FixedUtf8String utf8Str = new(requiredExt);
            instanceExtensions.Add(utf8Str);
            tempStrings.Add(utf8Str);
        }

        bool debugReportExtensionAvailable = false;

        if (debug) {
            if (availableInstanceExtensions.Contains(CommonStrings.VkExtDebugReportExtensionName)) {
                debugReportExtensionAvailable = true;
                instanceExtensions.Add(CommonStrings.VkExtDebugReportExtensionName);
            }

            if (availableInstanceLayers.Contains(CommonStrings.StandardValidationLayerName)) {
                this._standardValidationSupported = true;
                instanceLayers.Add(CommonStrings.StandardValidationLayerName);
            }

            if (availableInstanceLayers.Contains(CommonStrings.KhronosValidationLayerName)) {
                this._khronosValidationSupported = true;
                instanceLayers.Add(CommonStrings.KhronosValidationLayerName);
            }
        }

        instanceCi.enabledExtensionCount = instanceExtensions.Count;
        instanceCi.ppEnabledExtensionNames = (byte**)instanceExtensions.Data;

        instanceCi.enabledLayerCount = instanceLayers.Count;
        if (instanceLayers.Count > 0) {
            instanceCi.ppEnabledLayerNames = (byte**)instanceLayers.Data;
        }

        VkResult result = vkCreateInstance(ref instanceCi, null, out this.instance);
        CheckResult(result);

        if (this.HasSurfaceExtension(CommonStrings.VkExtMetalSurfaceExtensionName)) {
            this.CreateMetalSurfaceExt = this.getInstanceProcAddr<VkCreateMetalSurfaceExtT>("vkCreateMetalSurfaceEXT");
        }

        if (debug && debugReportExtensionAvailable) {
            this.EnableDebugCallback();
        }

        if (hasDeviceProperties2) {
            this._getPhysicalDeviceProperties2 = this.getInstanceProcAddr<VkGetPhysicalDeviceProperties2T>("vkGetPhysicalDeviceProperties2")
                ?? this.getInstanceProcAddr<VkGetPhysicalDeviceProperties2T>("vkGetPhysicalDeviceProperties2KHR");
        }

        foreach (FixedUtf8String tempStr in tempStrings) {
            tempStr.Dispose();
        }
    }

    /// <summary>
    /// Executes the debug callback logic for this backend.
    /// </summary>
    /// <param name="flags">The flags value used by this operation.</param>
    /// <param name="objectType">The object type value used by this operation.</param>
    /// <param name="object">The object value used by this operation.</param>
    /// <param name="location">The location value used by this operation.</param>
    /// <param name="messageCode">The message code value used by this operation.</param>
    /// <param name="pLayerPrefix">The p layer prefix value used by this operation.</param>
    /// <param name="pMessage">The p message value used by this operation.</param>
    /// <param name="pUserData">The p user data value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private uint debugCallback(uint flags, VkDebugReportObjectTypeEXT objectType, ulong @object, UIntPtr location, int messageCode, byte* pLayerPrefix, byte* pMessage, void* pUserData) {
        string message = Util.GetString(pMessage);
        VkDebugReportFlagsEXT debugReportFlags = (VkDebugReportFlagsEXT)flags;

#if DEBUG
        if (Debugger.IsAttached) Debugger.Break();
#endif

        string fullMessage = $"[{debugReportFlags}] ({objectType}) {message}";

        if (debugReportFlags == VkDebugReportFlagsEXT.ErrorEXT) {
            throw new VeldridException("A Vulkan validation error was encountered: " + fullMessage);
        }

        Console.WriteLine(fullMessage);
        return 0;
    }

    /// <summary>
    /// Creates the physical device instance used by this backend.
    /// </summary>
    private void createPhysicalDevice() {
        uint deviceCount = 0;
        vkEnumeratePhysicalDevices(this.instance, ref deviceCount, null);
        if (deviceCount == 0) {
            throw new InvalidOperationException("No physical devices exist.");
        }

        VkPhysicalDevice[] physicalDevices = new VkPhysicalDevice[deviceCount];
        vkEnumeratePhysicalDevices(this.instance, ref deviceCount, ref physicalDevices[0]);
        // Just use the first one.
        this.PhysicalDevice = physicalDevices[0];

        vkGetPhysicalDeviceProperties(this.PhysicalDevice, out this._physicalDeviceProperties);
        fixed (byte* utf8NamePtr = this._physicalDeviceProperties.deviceName) {
            this._deviceName = Encoding.UTF8.GetString(utf8NamePtr, (int)MaxPhysicalDeviceNameSize).TrimEnd('\0');
        }

        this._vendorName = "id:" + this._physicalDeviceProperties.vendorID.ToString("x8");
        this._apiVersion = GraphicsApiVersion.Unknown;
        this.DriverInfo = "version:" + this._physicalDeviceProperties.driverVersion.ToString("x8");

        vkGetPhysicalDeviceFeatures(this.PhysicalDevice, out this._physicalDeviceFeatures);

        vkGetPhysicalDeviceMemoryProperties(this.PhysicalDevice, out this._physicalDeviceMemProperties);
    }

    /// <summary>
    /// Creates the logical device instance used by this backend.
    /// </summary>
    /// <param name="surface">The surface value used by this operation.</param>
    /// <param name="preferStandardClipY">The prefer standard clip y value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    private void createLogicalDevice(VkSurfaceKHR surface, bool preferStandardClipY, VulkanDeviceOptions options) {
        this.getQueueFamilyIndices(surface);

        HashSet<uint> familyIndices = new() { this.GraphicsQueueIndex, this.PresentQueueIndex };
        VkDeviceQueueCreateInfo* queueCreateInfos = stackalloc VkDeviceQueueCreateInfo[familyIndices.Count];
        uint queueCreateInfosCount = (uint)familyIndices.Count;

        int i = 0;

        foreach (uint _ in familyIndices) {
            VkDeviceQueueCreateInfo queueCreateInfo = VkDeviceQueueCreateInfo.New();
            queueCreateInfo.queueFamilyIndex = this.GraphicsQueueIndex;
            queueCreateInfo.queueCount = 1;
            float priority = 1f;
            queueCreateInfo.pQueuePriorities = &priority;
            queueCreateInfos[i] = queueCreateInfo;
            i += 1;
        }

        VkPhysicalDeviceFeatures deviceFeatures = this._physicalDeviceFeatures;

        VkExtensionProperties[] props = this.GetDeviceExtensionProperties();

        HashSet<string> requiredInstanceExtensions = new(options.DeviceExtensions ?? Array.Empty<string>());

        bool hasMemReqs2 = false;
        bool hasDedicatedAllocation = false;
        bool hasDriverProperties = false;
        IntPtr[] activeExtensions = new IntPtr[props.Length];
        uint activeExtensionCount = 0;

        fixed (VkExtensionProperties* properties = props) {
            for (int property = 0; property < props.Length; property++) {
                string extensionName = Util.GetString(properties[property].extensionName);

                if (extensionName == "VK_EXT_debug_marker") {
                    activeExtensions[activeExtensionCount++] = CommonStrings.VkExtDebugMarkerExtensionName;
                    requiredInstanceExtensions.Remove(extensionName);
                    this._debugMarkerEnabled = true;
                }
                else if (extensionName == "VK_KHR_swapchain") {
                    activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].extensionName;
                    requiredInstanceExtensions.Remove(extensionName);
                }
                else if (preferStandardClipY && extensionName == "VK_KHR_maintenance1") {
                    activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].extensionName;
                    requiredInstanceExtensions.Remove(extensionName);
                    this._standardClipYDirection = true;
                }
                else if (extensionName == "VK_KHR_get_memory_requirements2") {
                    activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].extensionName;
                    requiredInstanceExtensions.Remove(extensionName);
                    hasMemReqs2 = true;
                }
                else if (extensionName == "VK_KHR_dedicated_allocation") {
                    activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].extensionName;
                    requiredInstanceExtensions.Remove(extensionName);
                    hasDedicatedAllocation = true;
                }
                else if (extensionName == "VK_KHR_driver_properties") {
                    activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].extensionName;
                    requiredInstanceExtensions.Remove(extensionName);
                    hasDriverProperties = true;
                }
                else if (extensionName == CommonStrings.VkKhrPortabilitySubset) {
                    activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].extensionName;
                    requiredInstanceExtensions.Remove(extensionName);
                }
                else if (requiredInstanceExtensions.Remove(extensionName)) {
                    activeExtensions[activeExtensionCount++] = (IntPtr)properties[property].extensionName;
                }
            }
        }

        if (requiredInstanceExtensions.Count != 0) {
            string missingList = string.Join(", ", requiredInstanceExtensions);
            throw new VeldridException($"The following Vulkan device extensions were not available: {missingList}");
        }

        VkDeviceCreateInfo deviceCreateInfo = VkDeviceCreateInfo.New();
        deviceCreateInfo.queueCreateInfoCount = queueCreateInfosCount;
        deviceCreateInfo.pQueueCreateInfos = queueCreateInfos;

        deviceCreateInfo.pEnabledFeatures = &deviceFeatures;

        StackList<IntPtr> layerNames = new();
        if (this._standardValidationSupported) {
            layerNames.Add(CommonStrings.StandardValidationLayerName);
        }

        if (this._khronosValidationSupported) {
            layerNames.Add(CommonStrings.KhronosValidationLayerName);
        }

        deviceCreateInfo.enabledLayerCount = layerNames.Count;
        deviceCreateInfo.ppEnabledLayerNames = (byte**)layerNames.Data;

        fixed (IntPtr* activeExtensionsPtr = activeExtensions) {
            deviceCreateInfo.enabledExtensionCount = activeExtensionCount;
            deviceCreateInfo.ppEnabledExtensionNames = (byte**)activeExtensionsPtr;

            VkResult result = vkCreateDevice(this.PhysicalDevice, ref deviceCreateInfo, null, out this.device);
            CheckResult(result);
        }

        vkGetDeviceQueue(this.device, this.GraphicsQueueIndex, 0, out this._graphicsQueue);

        if (this._debugMarkerEnabled) {
            this._setObjectNameDelegate = Marshal.GetDelegateForFunctionPointer<VkDebugMarkerSetObjectNameExtT>(this.getInstanceProcAddr("vkDebugMarkerSetObjectNameEXT"));
            this.MarkerBegin = Marshal.GetDelegateForFunctionPointer<VkCmdDebugMarkerBeginExtT>(this.getInstanceProcAddr("vkCmdDebugMarkerBeginEXT"));
            this.MarkerEnd = Marshal.GetDelegateForFunctionPointer<VkCmdDebugMarkerEndExtT>(this.getInstanceProcAddr("vkCmdDebugMarkerEndEXT"));
            this.MarkerInsert = Marshal.GetDelegateForFunctionPointer<VkCmdDebugMarkerInsertExtT>(this.getInstanceProcAddr("vkCmdDebugMarkerInsertEXT"));
        }

        if (hasDedicatedAllocation && hasMemReqs2) {
            this.GetBufferMemoryRequirements2 = this.getDeviceProcAddr<VkGetBufferMemoryRequirements2T>("vkGetBufferMemoryRequirements2")
                ?? this.getDeviceProcAddr<VkGetBufferMemoryRequirements2T>("vkGetBufferMemoryRequirements2KHR");
            this.GetImageMemoryRequirements2 = this.getDeviceProcAddr<VkGetImageMemoryRequirements2T>("vkGetImageMemoryRequirements2")
                ?? this.getDeviceProcAddr<VkGetImageMemoryRequirements2T>("vkGetImageMemoryRequirements2KHR");
        }

        if (this._getPhysicalDeviceProperties2 != null && hasDriverProperties) {
            VkPhysicalDeviceProperties2KHR deviceProps = VkPhysicalDeviceProperties2KHR.New();
            VkPhysicalDeviceDriverProperties driverProps = VkPhysicalDeviceDriverProperties.New();

            deviceProps.pNext = &driverProps;
            this._getPhysicalDeviceProperties2(this.PhysicalDevice, &deviceProps);

            string driverName = Encoding.UTF8.GetString(driverProps.DriverName, VkPhysicalDeviceDriverProperties.DRIVER_NAME_LENGTH).TrimEnd('\0');

            string driverInfo = Encoding.UTF8.GetString(driverProps.DriverInfo, VkPhysicalDeviceDriverProperties.DRIVER_INFO_LENGTH).TrimEnd('\0');

            VkConformanceVersion conforming = driverProps.ConformanceVersion;
            this._apiVersion = new GraphicsApiVersion(conforming.Major, conforming.Minor, conforming.Subminor, conforming.Patch);
            this.DriverName = driverName;
            this.DriverInfo = driverInfo;
        }
    }

    /// <summary>
    /// Gets the instance proc addr value.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private IntPtr getInstanceProcAddr(string name) {
        int byteCount = Encoding.UTF8.GetByteCount(name);
        byte* utf8Ptr = stackalloc byte[byteCount + 1];

        fixed (char* namePtr = name) {
            Encoding.UTF8.GetBytes(namePtr, name.Length, utf8Ptr, byteCount);
        }

        utf8Ptr[byteCount] = 0;

        return vkGetInstanceProcAddr(this.instance, utf8Ptr);
    }

    /// <summary>
    /// Resolves a Vulkan instance-level function pointer and returns it as a typed delegate.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private T getInstanceProcAddr<T>(string name) {
        IntPtr funcPtr = this.getInstanceProcAddr(name);
        if (funcPtr != IntPtr.Zero) {
            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }

        return default;
    }

    /// <summary>
    /// Gets the device proc addr value.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private IntPtr getDeviceProcAddr(string name) {
        int byteCount = Encoding.UTF8.GetByteCount(name);
        byte* utf8Ptr = stackalloc byte[byteCount + 1];

        fixed (char* namePtr = name) {
            Encoding.UTF8.GetBytes(namePtr, name.Length, utf8Ptr, byteCount);
        }

        utf8Ptr[byteCount] = 0;

        return vkGetDeviceProcAddr(this.device, utf8Ptr);
    }

    /// <summary>
    /// Resolves a Vulkan device-level function pointer and returns it as a typed delegate.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private T getDeviceProcAddr<T>(string name) {
        IntPtr funcPtr = this.getDeviceProcAddr(name);
        if (funcPtr != IntPtr.Zero) {
            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }

        return default;
    }

    /// <summary>
    /// Gets the queue family indices value.
    /// </summary>
    /// <param name="surface">The surface value used by this operation.</param>
    private void getQueueFamilyIndices(VkSurfaceKHR surface) {
        uint queueFamilyCount = 0;
        vkGetPhysicalDeviceQueueFamilyProperties(this.PhysicalDevice, ref queueFamilyCount, null);
        VkQueueFamilyProperties[] qfp = new VkQueueFamilyProperties[queueFamilyCount];
        vkGetPhysicalDeviceQueueFamilyProperties(this.PhysicalDevice, ref queueFamilyCount, out qfp[0]);

        bool foundGraphics = false;
        bool foundPresent = surface == VkSurfaceKHR.Null;

        for (uint i = 0; i < qfp.Length; i++) {
            if ((qfp[i].queueFlags & VkQueueFlags.Graphics) != 0) {
                this.GraphicsQueueIndex = i;
                foundGraphics = true;
            }

            if (!foundPresent) {
                vkGetPhysicalDeviceSurfaceSupportKHR(this.PhysicalDevice, i, surface, out VkBool32 presentSupported);

                if (presentSupported) {
                    this.PresentQueueIndex = i;
                    foundPresent = true;
                }
            }

            if (foundGraphics && foundPresent) {
                return;
            }
        }
    }

    /// <summary>
    /// Creates the descriptor pool instance used by this backend.
    /// </summary>
    private void createDescriptorPool() {
        this.DescriptorPoolManager = new VkDescriptorPoolManager(this);
    }

    /// <summary>
    /// Creates the graphics command pool instance used by this backend.
    /// </summary>
    private void createGraphicsCommandPool() {
        VkCommandPoolCreateInfo commandPoolCi = VkCommandPoolCreateInfo.New();
        commandPoolCi.flags = VkCommandPoolCreateFlags.ResetCommandBuffer;
        commandPoolCi.queueFamilyIndex = this.GraphicsQueueIndex;
        VkResult result = vkCreateCommandPool(this.device, ref commandPoolCi, null, out this._graphicsCommandPool);
        CheckResult(result);
    }

    /// <summary>
    /// Gets the free command pool value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private SharedCommandPool getFreeCommandPool() {
        SharedCommandPool sharedPool = null;

        lock (this._graphicsCommandPoolLock) {
            if (this._sharedGraphicsCommandPools.Count > 0) {
                sharedPool = this._sharedGraphicsCommandPools.Pop();
            }
        }

        return sharedPool ?? new SharedCommandPool(this, false);
    }

    /// <summary>
    /// Maps the buffer resource for CPU access.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="numBytes">The num bytes value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private IntPtr mapBuffer(VkBuffer buffer, uint numBytes) {
        if (buffer.Memory.IsPersistentMapped) {
            return (IntPtr)buffer.Memory.BlockMappedPointer;
        }

        void* mappedPtr;
        VkResult result = vkMapMemory(this.Device, buffer.Memory.DeviceMemory, buffer.Memory.Offset, numBytes, 0, &mappedPtr);
        CheckResult(result);
        return (IntPtr)mappedPtr;
    }

    /// <summary>
    /// Unmaps the buffer resource from CPU access.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    private void unmapBuffer(VkBuffer buffer) {
        if (!buffer.Memory.IsPersistentMapped) {
            vkUnmapMemory(this.Device, buffer.Memory.DeviceMemory);
        }
    }

    /// <summary>
    /// Gets the free staging texture value.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private VkTexture getFreeStagingTexture(uint width, uint height, uint depth, PixelFormat format) {
        uint totalSize = FormatHelpers.GetRegionSize(width, height, depth, format);

        lock (this._stagingResourcesLock) {
            for (int i = 0; i < this._availableStagingTextures.Count; i++) {
                VkTexture tex = this._availableStagingTextures[i];

                if (tex.Memory.Size >= totalSize) {
                    this._availableStagingTextures.RemoveAt(i);
                    tex.SetStagingDimensions(width, height, depth, format);
                    return tex;
                }
            }
        }

        uint texWidth = Math.Max(256, width);
        uint texHeight = Math.Max(256, height);
        VkTexture newTex = (VkTexture)this.ResourceFactory.CreateTexture(TextureDescription.Texture3D(texWidth, texHeight, depth, 1, format, TextureUsage.Staging));
        newTex.SetStagingDimensions(width, height, depth, format);

        return newTex;
    }

    /// <summary>
    /// Gets the free staging buffer value.
    /// </summary>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private VkBuffer getFreeStagingBuffer(uint size) {
        lock (this._stagingResourcesLock) {
            for (int i = 0; i < this._availableStagingBuffers.Count; i++) {
                VkBuffer buffer = this._availableStagingBuffers[i];

                if (buffer.SizeInBytes >= size) {
                    this._availableStagingBuffers.RemoveAt(i);
                    return buffer;
                }
            }
        }

        uint newBufferSize = Math.Max(_min_staging_buffer_size, size);
        VkBuffer newBuffer = (VkBuffer)this.ResourceFactory.CreateBuffer(new BufferDescription(newBufferSize, BufferUsage.Staging));
        return newBuffer;
    }

    /// <summary>
    /// Executes the submit commands core logic for this backend.
    /// </summary>
    /// <param name="cl">The cl value used by this operation.</param>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    private protected override void SubmitCommandsCore(CommandList cl, Fence fence) {
        this.submitCommandList(cl, 0, null, 0, null, fence);
    }

    /// <summary>
    /// Executes the swap buffers core logic for this backend.
    /// </summary>
    /// <param name="swapchain">The swapchain used by this operation.</param>
    private protected override void SwapBuffersCore(Swapchain swapchain) {
        VkSwapchain vkSc = Util.AssertSubtype<Swapchain, VkSwapchain>(swapchain);
        VkSwapchainKHR deviceSwapchain = vkSc.DeviceSwapchain;
        VkPresentInfoKHR presentInfo = VkPresentInfoKHR.New();
        presentInfo.swapchainCount = 1;
        presentInfo.pSwapchains = &deviceSwapchain;
        uint imageIndex = vkSc.ImageIndex;
        presentInfo.pImageIndices = &imageIndex;

        object presentLock = vkSc.PresentQueueIndex == this.GraphicsQueueIndex ? this._graphicsQueueLock : vkSc;

        lock (presentLock) {
            vkQueuePresentKHR(vkSc.PresentQueue, ref presentInfo);

            if (vkSc.AcquireNextImage(this.device, VkSemaphore.Null, vkSc.ImageAvailableFence)) {
                Vulkan.VkFence fence = vkSc.ImageAvailableFence;
                vkWaitForFences(this.device, 1, ref fence, true, ulong.MaxValue);
                vkResetFences(this.device, 1, ref fence);
            }
        }
    }

    /// <summary>
    /// Executes the wait for idle core logic for this backend.
    /// </summary>
    private protected override void WaitForIdleCore() {
        lock (this._graphicsQueueLock) {
            vkQueueWaitIdle(this._graphicsQueue);
        }

        this.checkSubmittedFences();
    }

    /// <summary>
    /// Executes the wait for next frame ready core logic for this backend.
    /// </summary>
    private protected override void WaitForNextFrameReadyCore() { }

    /// <summary>
    /// Gets the pixel format support core value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="properties">The properties value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private protected override bool GetPixelFormatSupportCore(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties) {
        VkFormat vkFormat = VkFormats.VdToVkPixelFormat(format, (usage & TextureUsage.DepthStencil) != 0);
        VkImageType vkType = VkFormats.VdToVkTextureType(type);
        VkImageTiling tiling = usage == TextureUsage.Staging ? VkImageTiling.Linear : VkImageTiling.Optimal;
        VkImageUsageFlags vkUsage = VkFormats.VdToVkTextureUsage(usage);

        VkResult result = vkGetPhysicalDeviceImageFormatProperties(this.PhysicalDevice, vkFormat, vkType, tiling, vkUsage, VkImageCreateFlags.None, out VkImageFormatProperties vkProps);

        if (result == VkResult.ErrorFormatNotSupported) {
            properties = default;
            return false;
        }

        CheckResult(result);

        properties = new PixelFormatProperties(vkProps.maxExtent.width, vkProps.maxExtent.height, vkProps.maxExtent.depth, vkProps.maxMipLevels, vkProps.maxArrayLayers, (uint)vkProps.sampleCounts);
        return true;
    }

    /// <summary>
    /// Updates the buffer core state for this command sequence.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        VkBuffer vkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(buffer);
        VkBuffer copySrcVkBuffer = null;
        IntPtr mappedPtr;
        byte* destPtr;
        bool isPersistentMapped = vkBuffer.Memory.IsPersistentMapped;

        if (isPersistentMapped) {
            mappedPtr = (IntPtr)vkBuffer.Memory.BlockMappedPointer;
            destPtr = (byte*)mappedPtr + bufferOffsetInBytes;
        }
        else {
            copySrcVkBuffer = this.getFreeStagingBuffer(sizeInBytes);
            mappedPtr = (IntPtr)copySrcVkBuffer.Memory.BlockMappedPointer;
            destPtr = (byte*)mappedPtr;
        }

        Unsafe.CopyBlock(destPtr, source.ToPointer(), sizeInBytes);

        if (!isPersistentMapped) {
            SharedCommandPool pool = this.getFreeCommandPool();
            VkCommandBuffer cb = pool.BeginNewCommandBuffer();

            VkBufferCopy copyRegion = new() {
                dstOffset = bufferOffsetInBytes,
                size = sizeInBytes
            };
            vkCmdCopyBuffer(cb, copySrcVkBuffer.DeviceBuffer, vkBuffer.DeviceBuffer, 1, ref copyRegion);

            pool.EndAndSubmit(cb);
            lock (this._stagingResourcesLock) {
                this._submittedStagingBuffers.Add(cb, copySrcVkBuffer);
            }
        }
    }

    /// <summary>
    /// Updates the texture core state for this command sequence.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    private protected override void UpdateTextureCore(Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
        VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(texture);
        bool isStaging = (vkTex.Usage & TextureUsage.Staging) != 0;

        if (isStaging) {
            VkMemoryBlock memBlock = vkTex.Memory;
            uint subresource = texture.CalculateSubresource(mipLevel, arrayLayer);
            VkSubresourceLayout layout = vkTex.GetSubresourceLayout(subresource);
            byte* imageBasePtr = (byte*)memBlock.BlockMappedPointer + layout.offset;

            uint srcRowPitch = FormatHelpers.GetRowPitch(width, texture.Format);
            uint srcDepthPitch = FormatHelpers.GetDepthPitch(srcRowPitch, height, texture.Format);
            Util.CopyTextureRegion(source.ToPointer(), 0, 0, 0, srcRowPitch, srcDepthPitch, imageBasePtr, x, y, z, (uint)layout.rowPitch, (uint)layout.depthPitch, width, height, depth, texture.Format);
        }
        else {
            VkTexture stagingTex = this.getFreeStagingTexture(width, height, depth, texture.Format);
            this.UpdateTexture(stagingTex, source, sizeInBytes, 0, 0, 0, width, height, depth, 0, 0);
            SharedCommandPool pool = this.getFreeCommandPool();
            VkCommandBuffer cb = pool.BeginNewCommandBuffer();
            VkCommandList.CopyTextureCore_VkCommandBuffer(cb, stagingTex, 0, 0, 0, 0, 0, texture, x, y, z, mipLevel, arrayLayer, width, height, depth, 1);
            lock (this._stagingResourcesLock) {
                this._submittedStagingTextures.Add(cb, stagingTex);
            }

            pool.EndAndSubmit(cb);
        }
    }

    /// <summary>
    /// Represents the SharedCommandPool type used by the graphics runtime.
    /// </summary>
    private class SharedCommandPool {

        /// <summary>
        /// Stores the cb state used by this instance.
        /// </summary>
        private readonly VkCommandBuffer cb;

        /// <summary>
        /// Stores the gd state used by this instance.
        /// </summary>
        private readonly VkGraphicsDevice gd;

        /// <summary>
        /// Stores the pool state used by this instance.
        /// </summary>
        private readonly VkCommandPool pool;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedCommandPool" /> type.
        /// </summary>
        /// <param name="gd">The graphics device that owns this operation.</param>
        /// <param name="isCached">The is cached value used by this operation.</param>
        public SharedCommandPool(VkGraphicsDevice gd, bool isCached) {
            this.gd = gd;
            this.IsCached = isCached;

            VkCommandPoolCreateInfo commandPoolCi = VkCommandPoolCreateInfo.New();
            commandPoolCi.flags = VkCommandPoolCreateFlags.Transient | VkCommandPoolCreateFlags.ResetCommandBuffer;
            commandPoolCi.queueFamilyIndex = this.gd.GraphicsQueueIndex;
            VkResult result = vkCreateCommandPool(this.gd.Device, ref commandPoolCi, null, out this.pool);
            CheckResult(result);

            VkCommandBufferAllocateInfo allocateInfo = VkCommandBufferAllocateInfo.New();
            allocateInfo.commandBufferCount = 1;
            allocateInfo.level = VkCommandBufferLevel.Primary;
            allocateInfo.commandPool = this.pool;
            result = vkAllocateCommandBuffers(this.gd.Device, ref allocateInfo, out this.cb);
            CheckResult(result);
        }

        /// <summary>
        /// Gets or sets IsCached.
        /// </summary>
        public bool IsCached { get; }

        /// <summary>
        /// Begins the new command buffer operation.
        /// </summary>
        /// <returns>The value produced by this operation.</returns>
        public VkCommandBuffer BeginNewCommandBuffer() {
            VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
            beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;
            VkResult result = vkBeginCommandBuffer(this.cb, ref beginInfo);
            CheckResult(result);

            return this.cb;
        }

        /// <summary>
        /// Ends the and submit operation.
        /// </summary>
        /// <param name="cb">The cb value used by this operation.</param>
        public void EndAndSubmit(VkCommandBuffer cb) {
            VkResult result = vkEndCommandBuffer(cb);
            CheckResult(result);
            this.gd.submitCommandBuffer(null, cb, 0, null, 0, null, null);
            lock (this.gd._stagingResourcesLock) {
                this.gd._submittedSharedCommandPools.Add(cb, this);
            }
        }

        /// <summary>
        /// Executes the destroy logic for this backend.
        /// </summary>
        internal void Destroy() {
            vkDestroyCommandPool(this.gd.Device, this.pool, null);
        }
    }

    /// <summary>
    /// Represents the FenceSubmissionInfo data structure used by the graphics runtime.
    /// </summary>
    private struct FenceSubmissionInfo {

        /// <summary>
        /// Stores the fence state used by this instance.
        /// </summary>
        public readonly Vulkan.VkFence Fence;

        /// <summary>
        /// Stores the command list collection used by this instance.
        /// </summary>
        public readonly VkCommandList CommandList;

        /// <summary>
        /// Stores the command buffer state used by this instance.
        /// </summary>
        public readonly VkCommandBuffer CommandBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="FenceSubmissionInfo" /> type.
        /// </summary>
        /// <param name="fence">The synchronization fence used by this operation.</param>
        /// <param name="commandList">The command list used by this operation.</param>
        /// <param name="commandBuffer">The command buffer value used by this operation.</param>
        public FenceSubmissionInfo(Vulkan.VkFence fence, VkCommandList commandList, VkCommandBuffer commandBuffer) {
            this.Fence = fence;
            this.CommandList = commandList;
            this.CommandBuffer = commandBuffer;
        }
    }
}

internal unsafe delegate VkResult VkCreateDebugReportCallbackExtD(VkInstance instance, VkDebugReportCallbackCreateInfoEXT* createInfo, IntPtr allocatorPtr, out VkDebugReportCallbackEXT ret);

internal unsafe delegate void VkDestroyDebugReportCallbackExtD(VkInstance instance, VkDebugReportCallbackEXT callback, VkAllocationCallbacks* pAllocator);

internal unsafe delegate VkResult VkDebugMarkerSetObjectNameExtT(VkDevice device, VkDebugMarkerObjectNameInfoEXT* pNameInfo);

internal unsafe delegate void VkCmdDebugMarkerBeginExtT(VkCommandBuffer commandBuffer, VkDebugMarkerMarkerInfoEXT* pMarkerInfo);

internal delegate void VkCmdDebugMarkerEndExtT(VkCommandBuffer commandBuffer);

internal unsafe delegate void VkCmdDebugMarkerInsertExtT(VkCommandBuffer commandBuffer, VkDebugMarkerMarkerInfoEXT* pMarkerInfo);

internal unsafe delegate void VkGetBufferMemoryRequirements2T(VkDevice device, VkBufferMemoryRequirementsInfo2KHR* pInfo, VkMemoryRequirements2KHR* pMemoryRequirements);

internal unsafe delegate void VkGetImageMemoryRequirements2T(VkDevice device, VkImageMemoryRequirementsInfo2KHR* pInfo, VkMemoryRequirements2KHR* pMemoryRequirements);

internal unsafe delegate void VkGetPhysicalDeviceProperties2T(VkPhysicalDevice physicalDevice, void* properties);

// VK_EXT_metal_surface

internal unsafe delegate VkResult VkCreateMetalSurfaceExtT(VkInstance instance, VkMetalSurfaceCreateInfoExt* pCreateInfo, VkAllocationCallbacks* pAllocator, VkSurfaceKHR* pSurface);

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

/// <summary>
/// Provides the Vulkan backend implementation for VkMetalSurfaceCreateInfoExt.
/// </summary>
internal unsafe struct VkMetalSurfaceCreateInfoExt {

    /// <summary>
    /// Defines the predefined value for vk structure type metal surface create info ext.
    /// </summary>

    public const VkStructureType VK_STRUCTURE_TYPE_METAL_SURFACE_CREATE_INFO_EXT = (VkStructureType)1000217000;

    /// <summary>
    /// Stores the stype state used by this instance.
    /// </summary>
    public VkStructureType SType;

    /// <summary>
    /// Stores the pnext state used by this instance.
    /// </summary>
    public void* PNext;

    /// <summary>
    /// Stores the flags state used by this instance.
    /// </summary>
    public uint Flags;

    /// <summary>
    /// Stores the player state used by this instance.
    /// </summary>
    public void* PLayer;
}

/// <summary>
/// Provides the Vulkan backend implementation for VkPhysicalDeviceDriverProperties.
/// </summary>
internal unsafe struct VkPhysicalDeviceDriverProperties {

    /// <summary>
    /// Defines the predefined value for driver name length.
    /// </summary>
    public const int DRIVER_NAME_LENGTH = 256;

    /// <summary>
    /// Defines the predefined value for driver info length.
    /// </summary>
    public const int DRIVER_INFO_LENGTH = 256;

    /// <summary>
    /// Defines the predefined value for vk structure type physical device driver properties.
    /// </summary>

    public const VkStructureType VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_DRIVER_PROPERTIES = (VkStructureType)1000196000;

    /// <summary>
    /// Stores the stype state used by this instance.
    /// </summary>
    public VkStructureType SType;

    /// <summary>
    /// Stores the pnext state used by this instance.
    /// </summary>
    public void* PNext;

    /// <summary>
    /// Stores the driver id state used by this instance.
    /// </summary>
    public VkDriverId DriverID;

    public fixed byte DriverName[DRIVER_NAME_LENGTH];

    public fixed byte DriverInfo[DRIVER_INFO_LENGTH];

    /// <summary>
    /// Stores the conformance version state used by this instance.
    /// </summary>
    public VkConformanceVersion ConformanceVersion;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static VkPhysicalDeviceDriverProperties New() {
        return new VkPhysicalDeviceDriverProperties { SType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_DRIVER_PROPERTIES };
    }
}

/// <summary>
/// Defines the available values of the VkDriverId enumeration.
/// </summary>
internal enum VkDriverId { }

/// <summary>
/// Provides the Vulkan backend implementation for VkConformanceVersion.
/// </summary>
internal struct VkConformanceVersion {

    /// <summary>
    /// Stores the major state used by this instance.
    /// </summary>
    public byte Major;

    /// <summary>
    /// Stores the minor state used by this instance.
    /// </summary>
    public byte Minor;

    /// <summary>
    /// Stores the subminor state used by this instance.
    /// </summary>
    public byte Subminor;

    /// <summary>
    /// Stores the patch state used by this instance.
    /// </summary>
    public byte Patch;
}
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value