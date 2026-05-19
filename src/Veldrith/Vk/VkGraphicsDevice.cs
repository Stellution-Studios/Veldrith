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
/// Represents the VkGraphicsDevice class.
/// </summary>
internal unsafe class VkGraphicsDevice : GraphicsDevice {

    /// <summary>
    /// Represents the _vk_instance_create_enumerate_portability_bit_khr field.
    /// </summary>
    private const uint _vk_instance_create_enumerate_portability_bit_khr = 0x00000001;

    /// <summary>
    /// Represents the _shared_command_pool_count field.
    /// </summary>
    private const int _shared_command_pool_count = 4;

    // Staging Resources

    /// <summary>
    /// Represents the _min_staging_buffer_size field.
    /// </summary>
    private const uint _min_staging_buffer_size = 64;

    /// <summary>
    /// Represents the _max_staging_buffer_size field.
    /// </summary>
    private const uint _max_staging_buffer_size = 512;

    /// <summary>
    /// Represents the _s_name field.
    /// </summary>
    private static readonly FixedUtf8String _s_name = "Veldrith-VkGraphicsDevice";

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="checkIsSupported">The value of checkIsSupported.</param>
    /// <param name="true">The value of true.</param>
    /// <returns>The result of the new operation.</returns>
    private static readonly Lazy<bool> _s_is_supported = new(checkIsSupported, true);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly List<VkBuffer> _availableStagingBuffers = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly List<VkTexture> _availableStagingTextures = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly ConcurrentQueue<Vulkan.VkFence> _availableSubmissionFences = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly ConcurrentDictionary<VkFormat, VkFilter> _filters = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly object _graphicsCommandPoolLock = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly object _graphicsQueueLock = new();

    /// <summary>
    /// Represents the _mainSwapchain field.
    /// </summary>
    private readonly VkSwapchain _mainSwapchain;

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly Stack<SharedCommandPool> _sharedGraphicsCommandPools = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly object _stagingResourcesLock = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly List<FenceSubmissionInfo> _submittedFences = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly object _submittedFencesLock = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly Dictionary<VkCommandBuffer, SharedCommandPool> _submittedSharedCommandPools = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly Dictionary<VkCommandBuffer, VkBuffer> _submittedStagingBuffers = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly Dictionary<VkCommandBuffer, VkTexture> _submittedStagingTextures = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly List<FixedUtf8String> _surfaceExtensions = new();

    /// <summary>
    /// Represents the _vulkanInfo field.
    /// </summary>
    private readonly BackendInfoVulkan _vulkanInfo;

    /// <summary>
    /// Represents the _apiVersion field.
    /// </summary>
    private GraphicsApiVersion _apiVersion;

    /// <summary>
    /// Represents the _debugCallbackFunc field.
    /// </summary>
    private PFN_vkDebugReportCallbackEXT _debugCallbackFunc;

    /// <summary>
    /// Represents the _debugCallbackHandle field.
    /// </summary>
    private VkDebugReportCallbackEXT _debugCallbackHandle;

    /// <summary>
    /// Represents the _debugMarkerEnabled field.
    /// </summary>
    private bool _debugMarkerEnabled;

    /// <summary>
    /// Represents the _deviceName field.
    /// </summary>
    private string _deviceName;

    /// <summary>
    /// Represents the _getPhysicalDeviceProperties2 field.
    /// </summary>
    private VkGetPhysicalDeviceProperties2T _getPhysicalDeviceProperties2;

    /// <summary>
    /// Represents the _graphicsCommandPool field.
    /// </summary>
    private VkCommandPool _graphicsCommandPool;

    /// <summary>
    /// Represents the _graphicsQueue field.
    /// </summary>
    private VkQueue _graphicsQueue;

    /// <summary>
    /// Represents the _khronosValidationSupported field.
    /// </summary>
    private bool _khronosValidationSupported;

    /// <summary>
    /// Represents the _physicalDeviceFeatures field.
    /// </summary>
    private VkPhysicalDeviceFeatures _physicalDeviceFeatures;

    /// <summary>
    /// Represents the _physicalDeviceMemProperties field.
    /// </summary>
    private VkPhysicalDeviceMemoryProperties _physicalDeviceMemProperties;

    /// <summary>
    /// Represents the _physicalDeviceProperties field.
    /// </summary>
    private VkPhysicalDeviceProperties _physicalDeviceProperties;

    /// <summary>
    /// Represents the _setObjectNameDelegate field.
    /// </summary>
    private VkDebugMarkerSetObjectNameExtT _setObjectNameDelegate;

    /// <summary>
    /// Represents the _standardClipYDirection field.
    /// </summary>
    private bool _standardClipYDirection;

    /// <summary>
    /// Represents the _standardValidationSupported field.
    /// </summary>
    private bool _standardValidationSupported;

    /// <summary>
    /// Represents the _vendorName field.
    /// </summary>
    private string _vendorName;

    /// <summary>
    /// Represents the device field.
    /// </summary>
    private VkDevice device;

    /// <summary>
    /// Represents the instance field.
    /// </summary>
    private VkInstance instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkGraphicsDevice" /> type.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="scDesc">The value of scDesc.</param>
    public VkGraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? scDesc)
        : this(options, scDesc, new VulkanDeviceOptions()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VkGraphicsDevice" /> type.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="scDesc">The value of scDesc.</param>
    /// <param name="vkOptions">The value of vkOptions.</param>
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
    /// Represents the Instance field.
    /// </summary>
    public VkInstance Instance => this.instance;

    /// <summary>
    /// Represents the Device field.
    /// </summary>
    public VkDevice Device => this.device;

    /// <summary>
    /// Gets or sets PhysicalDevice.
    /// </summary>
    public VkPhysicalDevice PhysicalDevice { get; private set; }

    /// <summary>
    /// Represents the PhysicalDeviceMemProperties field.
    /// </summary>
    public VkPhysicalDeviceMemoryProperties PhysicalDeviceMemProperties => this._physicalDeviceMemProperties;

    /// <summary>
    /// Represents the GraphicsQueue field.
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
    /// Performs the GetVulkanInfo operation.
    /// </summary>
    /// <param name="info">The value of info.</param>
    /// <returns>The result of the GetVulkanInfo operation.</returns>
    public override bool GetVulkanInfo(out BackendInfoVulkan info) {
        info = this._vulkanInfo;
        return true;
    }

    /// <summary>
    /// Performs the HasSurfaceExtension operation.
    /// </summary>
    /// <param name="extension">The value of extension.</param>
    /// <returns>The result of the HasSurfaceExtension operation.</returns>
    public bool HasSurfaceExtension(FixedUtf8String extension) {
        return this._surfaceExtensions.Contains(extension);
    }

    /// <summary>
    /// Performs the EnableDebugCallback operation.
    /// </summary>
    /// <param name="flags">The value of flags.</param>
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
    /// Performs the GetDeviceExtensionProperties operation.
    /// </summary>
    /// <returns>The result of the GetDeviceExtensionProperties operation.</returns>
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
    /// Performs the GetSampleCountLimit operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="depthFormat">The value of depthFormat.</param>
    /// <returns>The result of the GetSampleCountLimit operation.</returns>
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
    /// Performs the ResetFence operation.
    /// </summary>
    /// <param name="fence">The value of fence.</param>
    public override void ResetFence(Fence fence) {
        Vulkan.VkFence vkFence = Util.AssertSubtype<Fence, VkFence>(fence).DeviceFence;
        vkResetFences(this.device, 1, ref vkFence);
    }

    /// <summary>
    /// Performs the WaitForFence operation.
    /// </summary>
    /// <param name="fence">The value of fence.</param>
    /// <param name="nanosecondTimeout">The value of nanosecondTimeout.</param>
    /// <returns>The result of the WaitForFence operation.</returns>
    public override bool WaitForFence(Fence fence, ulong nanosecondTimeout) {
        Vulkan.VkFence vkFence = Util.AssertSubtype<Fence, VkFence>(fence).DeviceFence;
        VkResult result = vkWaitForFences(this.device, 1, ref vkFence, true, nanosecondTimeout);
        return result == VkResult.Success;
    }

    /// <summary>
    /// Performs the WaitForFences operation.
    /// </summary>
    /// <param name="fences">The value of fences.</param>
    /// <param name="waitAll">The value of waitAll.</param>
    /// <param name="nanosecondTimeout">The value of nanosecondTimeout.</param>
    /// <returns>The result of the WaitForFences operation.</returns>
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
    /// Performs the IsSupported operation.
    /// </summary>
    /// <returns>The result of the IsSupported operation.</returns>
    internal static bool IsSupported() {
        return _s_is_supported.Value;
    }

    /// <summary>
    /// Performs the SetResourceName operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="name">The value of name.</param>
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
    /// Performs the GetFormatFilter operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <returns>The result of the GetFormatFilter operation.</returns>
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
    /// Performs the ClearColorTexture operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="color">The value of color.</param>
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
    /// Performs the ClearDepthTexture operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="clearValue">The value of clearValue.</param>
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
    /// Performs the GetUniformBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>The result of the GetUniformBufferMinOffsetAlignmentCore operation.</returns>
    internal override uint GetUniformBufferMinOffsetAlignmentCore() {
        return (uint)this._physicalDeviceProperties.limits.minUniformBufferOffsetAlignment;
    }

    /// <summary>
    /// Performs the GetStructuredBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>The result of the GetStructuredBufferMinOffsetAlignmentCore operation.</returns>
    internal override uint GetStructuredBufferMinOffsetAlignmentCore() {
        return (uint)this._physicalDeviceProperties.limits.minStorageBufferOffsetAlignment;
    }

    /// <summary>
    /// Performs the TransitionImageLayout operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="layout">The value of layout.</param>
    internal void TransitionImageLayout(VkTexture texture, VkImageLayout layout) {
        SharedCommandPool pool = this.getFreeCommandPool();
        VkCommandBuffer cb = pool.BeginNewCommandBuffer();
        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, texture.ActualArrayLayers, layout);
        pool.EndAndSubmit(cb);
    }

    /// <summary>
    /// Performs the MapCore operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="mode">The value of mode.</param>
    /// <param name="subresource">The value of subresource.</param>
    /// <returns>The result of the MapCore operation.</returns>
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
    /// Performs the UnmapCore operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="subresource">The value of subresource.</param>
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
    /// Performs the PlatformDispose operation.
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
    /// Performs the checkIsSupported operation.
    /// </summary>
    /// <returns>The result of the checkIsSupported operation.</returns>
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
    /// Performs the submitCommandList operation.
    /// </summary>
    /// <param name="cl">The value of cl.</param>
    /// <param name="waitSemaphoreCount">The value of waitSemaphoreCount.</param>
    /// <param name="waitSemaphoresPtr">The value of waitSemaphoresPtr.</param>
    /// <param name="signalSemaphoreCount">The value of signalSemaphoreCount.</param>
    /// <param name="signalSemaphoresPtr">The value of signalSemaphoresPtr.</param>
    /// <param name="fence">The value of fence.</param>
    private void submitCommandList(CommandList cl, uint waitSemaphoreCount, VkSemaphore* waitSemaphoresPtr, uint signalSemaphoreCount, VkSemaphore* signalSemaphoresPtr, Fence fence) {
        VkCommandList vkCl = Util.AssertSubtype<CommandList, VkCommandList>(cl);
        VkCommandBuffer vkCb = vkCl.CommandBuffer;

        vkCl.CommandBufferSubmitted(vkCb);
        this.submitCommandBuffer(vkCl, vkCb, waitSemaphoreCount, waitSemaphoresPtr, signalSemaphoreCount, signalSemaphoresPtr, fence);
    }

    /// <summary>
    /// Performs the submitCommandBuffer operation.
    /// </summary>
    /// <param name="vkCl">The value of vkCl.</param>
    /// <param name="vkCb">The value of vkCb.</param>
    /// <param name="waitSemaphoreCount">The value of waitSemaphoreCount.</param>
    /// <param name="waitSemaphoresPtr">The value of waitSemaphoresPtr.</param>
    /// <param name="signalSemaphoreCount">The value of signalSemaphoreCount.</param>
    /// <param name="signalSemaphoresPtr">The value of signalSemaphoresPtr.</param>
    /// <param name="fence">The value of fence.</param>
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
    /// Performs the checkSubmittedFences operation.
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
    /// Performs the completeFenceSubmission operation.
    /// </summary>
    /// <param name="fsi">The value of fsi.</param>
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
    /// Performs the returnSubmissionFence operation.
    /// </summary>
    /// <param name="fence">The value of fence.</param>
    private void returnSubmissionFence(Vulkan.VkFence fence) {
        this._availableSubmissionFences.Enqueue(fence);
    }

    /// <summary>
    /// Performs the getFreeSubmissionFence operation.
    /// </summary>
    /// <returns>The result of the getFreeSubmissionFence operation.</returns>
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
    /// Performs the setDebugMarkerName operation.
    /// </summary>
    /// <param name="type">The value of type.</param>
    /// <param name="target">The value of target.</param>
    /// <param name="name">The value of name.</param>
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
    /// Performs the createInstance operation.
    /// </summary>
    /// <param name="debug">The value of debug.</param>
    /// <param name="options">The value of options.</param>
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
    /// Performs the debugCallback operation.
    /// </summary>
    /// <param name="flags">The value of flags.</param>
    /// <param name="objectType">The value of objectType.</param>
    /// <param name="object">The value of object.</param>
    /// <param name="location">The value of location.</param>
    /// <param name="messageCode">The value of messageCode.</param>
    /// <param name="pLayerPrefix">The value of pLayerPrefix.</param>
    /// <param name="pMessage">The value of pMessage.</param>
    /// <param name="pUserData">The value of pUserData.</param>
    /// <returns>The result of the debugCallback operation.</returns>
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
    /// Performs the createPhysicalDevice operation.
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
    /// Performs the createLogicalDevice operation.
    /// </summary>
    /// <param name="surface">The value of surface.</param>
    /// <param name="preferStandardClipY">The value of preferStandardClipY.</param>
    /// <param name="options">The value of options.</param>
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
    /// Performs the getInstanceProcAddr operation.
    /// </summary>
    /// <param name="name">The value of name.</param>
    /// <returns>The result of the getInstanceProcAddr operation.</returns>
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
    /// <param name="name">The Vulkan function name to resolve.</param>
    /// <typeparam name="T">The delegate type representing the function signature.</typeparam>
    /// <returns>The resolved delegate, or <see langword="default" /> if the function is not available.</returns>
    private T getInstanceProcAddr<T>(string name) {
        IntPtr funcPtr = this.getInstanceProcAddr(name);
        if (funcPtr != IntPtr.Zero) {
            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }

        return default;
    }

    /// <summary>
    /// Performs the getDeviceProcAddr operation.
    /// </summary>
    /// <param name="name">The value of name.</param>
    /// <returns>The result of the getDeviceProcAddr operation.</returns>
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
    /// <param name="name">The Vulkan function name to resolve.</param>
    /// <typeparam name="T">The delegate type representing the function signature.</typeparam>
    /// <returns>The resolved delegate, or <see langword="default" /> if the function is not available.</returns>
    private T getDeviceProcAddr<T>(string name) {
        IntPtr funcPtr = this.getDeviceProcAddr(name);
        if (funcPtr != IntPtr.Zero) {
            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }

        return default;
    }

    /// <summary>
    /// Performs the getQueueFamilyIndices operation.
    /// </summary>
    /// <param name="surface">The value of surface.</param>
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
    /// Performs the createDescriptorPool operation.
    /// </summary>
    private void createDescriptorPool() {
        this.DescriptorPoolManager = new VkDescriptorPoolManager(this);
    }

    /// <summary>
    /// Performs the createGraphicsCommandPool operation.
    /// </summary>
    private void createGraphicsCommandPool() {
        VkCommandPoolCreateInfo commandPoolCi = VkCommandPoolCreateInfo.New();
        commandPoolCi.flags = VkCommandPoolCreateFlags.ResetCommandBuffer;
        commandPoolCi.queueFamilyIndex = this.GraphicsQueueIndex;
        VkResult result = vkCreateCommandPool(this.device, ref commandPoolCi, null, out this._graphicsCommandPool);
        CheckResult(result);
    }

    /// <summary>
    /// Performs the getFreeCommandPool operation.
    /// </summary>
    /// <returns>The result of the getFreeCommandPool operation.</returns>
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
    /// Performs the mapBuffer operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="numBytes">The value of numBytes.</param>
    /// <returns>The result of the mapBuffer operation.</returns>
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
    /// Performs the unmapBuffer operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    private void unmapBuffer(VkBuffer buffer) {
        if (!buffer.Memory.IsPersistentMapped) {
            vkUnmapMemory(this.Device, buffer.Memory.DeviceMemory);
        }
    }

    /// <summary>
    /// Performs the getFreeStagingTexture operation.
    /// </summary>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="format">The value of format.</param>
    /// <returns>The result of the getFreeStagingTexture operation.</returns>
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
    /// Performs the getFreeStagingBuffer operation.
    /// </summary>
    /// <param name="size">The value of size.</param>
    /// <returns>The result of the getFreeStagingBuffer operation.</returns>
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
    /// Performs the SubmitCommandsCore operation.
    /// </summary>
    /// <param name="cl">The value of cl.</param>
    /// <param name="fence">The value of fence.</param>
    private protected override void SubmitCommandsCore(CommandList cl, Fence fence) {
        this.submitCommandList(cl, 0, null, 0, null, fence);
    }

    /// <summary>
    /// Performs the SwapBuffersCore operation.
    /// </summary>
    /// <param name="swapchain">The value of swapchain.</param>
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
    /// Performs the WaitForIdleCore operation.
    /// </summary>
    private protected override void WaitForIdleCore() {
        lock (this._graphicsQueueLock) {
            vkQueueWaitIdle(this._graphicsQueue);
        }

        this.checkSubmittedFences();
    }

    /// <summary>
    /// Performs the WaitForNextFrameReadyCore operation.
    /// </summary>
    private protected override void WaitForNextFrameReadyCore() { }

    /// <summary>
    /// Performs the GetPixelFormatSupportCore operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="type">The value of type.</param>
    /// <param name="usage">The value of usage.</param>
    /// <param name="properties">The value of properties.</param>
    /// <returns>The result of the GetPixelFormatSupportCore operation.</returns>
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
    /// Performs the UpdateBufferCore operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="bufferOffsetInBytes">The value of bufferOffsetInBytes.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
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
    /// Performs the UpdateTextureCore operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="z">The value of z.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="mipLevel">The value of mipLevel.</param>
    /// <param name="arrayLayer">The value of arrayLayer.</param>
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
    /// Represents the SharedCommandPool class.
    /// </summary>
    private class SharedCommandPool {

        /// <summary>
        /// Represents the cb field.
        /// </summary>
        private readonly VkCommandBuffer cb;

        /// <summary>
        /// Represents the gd field.
        /// </summary>
        private readonly VkGraphicsDevice gd;

        /// <summary>
        /// Represents the pool field.
        /// </summary>
        private readonly VkCommandPool pool;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedCommandPool" /> type.
        /// </summary>
        /// <param name="gd">The value of gd.</param>
        /// <param name="isCached">The value of isCached.</param>
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
        /// Performs the BeginNewCommandBuffer operation.
        /// </summary>
        /// <returns>The result of the BeginNewCommandBuffer operation.</returns>
        public VkCommandBuffer BeginNewCommandBuffer() {
            VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
            beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;
            VkResult result = vkBeginCommandBuffer(this.cb, ref beginInfo);
            CheckResult(result);

            return this.cb;
        }

        /// <summary>
        /// Performs the EndAndSubmit operation.
        /// </summary>
        /// <param name="cb">The value of cb.</param>
        public void EndAndSubmit(VkCommandBuffer cb) {
            VkResult result = vkEndCommandBuffer(cb);
            CheckResult(result);
            this.gd.submitCommandBuffer(null, cb, 0, null, 0, null, null);
            lock (this.gd._stagingResourcesLock) {
                this.gd._submittedSharedCommandPools.Add(cb, this);
            }
        }

        /// <summary>
        /// Performs the Destroy operation.
        /// </summary>
        internal void Destroy() {
            vkDestroyCommandPool(this.gd.Device, this.pool, null);
        }
    }

    /// <summary>
    /// Represents the FenceSubmissionInfo struct.
    /// </summary>
    private struct FenceSubmissionInfo {

        /// <summary>
        /// Represents the Fence field.
        /// </summary>
        public readonly Vulkan.VkFence Fence;

        /// <summary>
        /// Represents the CommandList field.
        /// </summary>
        public readonly VkCommandList CommandList;

        /// <summary>
        /// Represents the CommandBuffer field.
        /// </summary>
        public readonly VkCommandBuffer CommandBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="FenceSubmissionInfo" /> type.
        /// </summary>
        /// <param name="fence">The value of fence.</param>
        /// <param name="commandList">The value of commandList.</param>
        /// <param name="commandBuffer">The value of commandBuffer.</param>
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
/// Represents the VkMetalSurfaceCreateInfoExt struct.
/// </summary>
internal unsafe struct VkMetalSurfaceCreateInfoExt {

    /// <summary>
    /// Represents the VK_STRUCTURE_TYPE_METAL_SURFACE_CREATE_INFO_EXT field.
    /// </summary>
    public const VkStructureType VK_STRUCTURE_TYPE_METAL_SURFACE_CREATE_INFO_EXT = (VkStructureType)1000217000;

    /// <summary>
    /// Represents the SType field.
    /// </summary>
    public VkStructureType SType;

    /// <summary>
    /// Represents the PNext field.
    /// </summary>
    public void* PNext;

    /// <summary>
    /// Represents the Flags field.
    /// </summary>
    public uint Flags;

    /// <summary>
    /// Represents the PLayer field.
    /// </summary>
    public void* PLayer;
}

/// <summary>
/// Represents the VkPhysicalDeviceDriverProperties struct.
/// </summary>
internal unsafe struct VkPhysicalDeviceDriverProperties {

    /// <summary>
    /// Represents the DRIVER_NAME_LENGTH field.
    /// </summary>
    public const int DRIVER_NAME_LENGTH = 256;

    /// <summary>
    /// Represents the DRIVER_INFO_LENGTH field.
    /// </summary>
    public const int DRIVER_INFO_LENGTH = 256;

    /// <summary>
    /// Represents the VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_DRIVER_PROPERTIES field.
    /// </summary>
    public const VkStructureType VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_DRIVER_PROPERTIES = (VkStructureType)1000196000;

    /// <summary>
    /// Represents the SType field.
    /// </summary>
    public VkStructureType SType;

    /// <summary>
    /// Represents the PNext field.
    /// </summary>
    public void* PNext;

    /// <summary>
    /// Represents the DriverID field.
    /// </summary>
    public VkDriverId DriverID;

    public fixed byte DriverName[DRIVER_NAME_LENGTH];

    public fixed byte DriverInfo[DRIVER_INFO_LENGTH];

    /// <summary>
    /// Represents the ConformanceVersion field.
    /// </summary>
    public VkConformanceVersion ConformanceVersion;

    /// <summary>
    /// Performs the New operation.
    /// </summary>
    /// <returns>The result of the New operation.</returns>
    public static VkPhysicalDeviceDriverProperties New() {
        return new VkPhysicalDeviceDriverProperties { SType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_DRIVER_PROPERTIES };
    }
}

/// <summary>
/// Represents the VkDriverId enum.
/// </summary>
internal enum VkDriverId { }

/// <summary>
/// Represents the VkConformanceVersion struct.
/// </summary>
internal struct VkConformanceVersion {

    /// <summary>
    /// Represents the Major field.
    /// </summary>
    public byte Major;

    /// <summary>
    /// Represents the Minor field.
    /// </summary>
    public byte Minor;

    /// <summary>
    /// Represents the Subminor field.
    /// </summary>
    public byte Subminor;

    /// <summary>
    /// Represents the Patch field.
    /// </summary>
    public byte Patch;
}
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value