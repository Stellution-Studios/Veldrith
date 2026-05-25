using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Vulkan;
using static Veldrith.Vk.VulkanDispatch;
using static Veldrith.Vk.VulkanUtil;
using static Vortice.Vulkan.Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkGraphicsDevice.
/// </summary>
internal unsafe class VkGraphicsDevice : GraphicsDevice {

    /// <summary>
    /// Stores the vk instance create enumerate portability bit khr state used by this instance.
    /// </summary>
    private const VkInstanceCreateFlags _vkInstanceCreateEnumeratePortabilityBitKhr = VkInstanceCreateFlags.EnumeratePortabilityKHR;

    /// <summary>
    /// Stores the shared command pool count value used during command execution.
    /// </summary>
    private const int _sharedCommandPoolCount = 4;

    // Staging Resources

    /// <summary>
    /// Stores the min staging buffer size value used during command execution.
    /// </summary>
    private const uint _minStagingBufferSize = 64;

    /// <summary>
    /// Stores the max staging buffer size value used during command execution.
    /// </summary>
    private const uint _maxStagingBufferSize = 512;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private static readonly FixedUtf8String _sName = "Veldrith-VkGraphicsDevice";

    /// <summary>
    /// Stores the s is supported state used by this instance.
    /// </summary>
    private static readonly Lazy<bool> _sIsSupported = new(CheckIsSupported, true);

    /// <summary>
    /// Enables the persistent Vulkan pipeline cache unless explicitly disabled.
    /// </summary>
    private static readonly bool _persistentPipelineCacheEnabled = !string.Equals(Environment.GetEnvironmentVariable("VELDRID_VK_PIPELINE_CACHE"), "0", StringComparison.Ordinal);

    /// <summary>
    /// Enables lightweight Vulkan frame timing diagnostics.
    /// </summary>
    internal static readonly bool PerfLogEnabled = IsEnvironmentEnabled("VELDRID_VK_PERF");

    /// <summary>
    /// Stores the persistent Vulkan pipeline cache directory.
    /// </summary>
    private static readonly string _persistentPipelineCacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Veldrith",
        "VkPipelineCache");

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
    private readonly ConcurrentQueue<global::Vortice.Vulkan.VkFence> _availableSubmissionFences = new();

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
    /// Accumulates Vulkan submit time for diagnostics.
    /// </summary>
    private double _perfSubmitMs;

    /// <summary>
    /// Accumulates Vulkan present time for diagnostics.
    /// </summary>
    private double _perfPresentMs;

    /// <summary>
    /// Accumulates Vulkan acquire time for diagnostics.
    /// </summary>
    private double _perfAcquireMs;

    /// <summary>
    /// Counts frames in the current Vulkan diagnostics window.
    /// </summary>
    private int _perfFrameCount;

    /// <summary>
    /// Tracks the previous diagnostics report timestamp.
    /// </summary>
    private long _perfLastReportTicks;

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
    private readonly Dictionary<VkCommandBuffer, List<VkBuffer>> _submittedStagingBuffers = new();

    /// <summary>
    /// Stores the submitted staging textures collection used by this instance.
    /// </summary>
    private readonly Dictionary<VkCommandBuffer, VkTexture> _submittedStagingTextures = new();

    /// <summary>
    /// Synchronizes immediate Vulkan upload recording.
    /// </summary>
    private readonly object _immediateUploadLock = new();

    /// <summary>
    /// Stores an open shared command pool used for batched immediate uploads.
    /// </summary>
    private SharedCommandPool _immediateUploadPool;

    /// <summary>
    /// Stores an open command buffer used for batched immediate uploads.
    /// </summary>
    private VkCommandBuffer _immediateUploadCb;

    /// <summary>
    /// Stores staging buffers referenced by the open immediate upload command buffer.
    /// </summary>
    private readonly List<VkBuffer> _immediateUploadStagingBuffers = new();

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
    private VkDebugReportCallbackExtD _debugCallbackFunc;

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
    /// Stores the Vulkan pipeline cache used by pipeline creation.
    /// </summary>
    private VkPipelineCache _pipelineCache;

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
    private VkDevice _device;

    /// <summary>
    /// Stores the device api state used by this instance.
    /// </summary>
    private VkDeviceApi _deviceApi;

    /// <summary>
    /// Stores the instance state used by this instance.
    /// </summary>
    private VkInstance _instance;

    /// <summary>
    /// Stores the instance api state used by this instance.
    /// </summary>
    private VkInstanceApi _instanceApi;

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
        this.CreateInstance(options.Debug, vkOptions);

        VkSurfaceKHR surface = default;
        if (scDesc != null) {
            surface = VkSurfaceUtil.CreateSurface(this, this._instance, scDesc.Value.Source);
        }

        this.CreatePhysicalDevice();
        this.CreateLogicalDevice(surface, options.PreferStandardClipSpaceYDirection, vkOptions);
        this.CreatePipelineCache();

        this.MemoryManager = new VkDeviceMemoryManager(this._device, this._physicalDeviceProperties.limits.bufferImageGranularity, this.GetBufferMemoryRequirements2, this.GetImageMemoryRequirements2);

        this.Features = new GraphicsDeviceFeatures(true, this._physicalDeviceFeatures.geometryShader, this._physicalDeviceFeatures.tessellationShader, this._physicalDeviceFeatures.multiViewport, true, true, true, true, this._physicalDeviceFeatures.drawIndirectFirstInstance, this._physicalDeviceFeatures.fillModeNonSolid, this._physicalDeviceFeatures.samplerAnisotropy, this._physicalDeviceFeatures.depthClamp, true, this._physicalDeviceFeatures.independentBlend, true, true, this._debugMarkerEnabled, true, true, this._physicalDeviceFeatures.shaderFloat64);

        this.ResourceFactory = new VkResourceFactory(this);

        if (scDesc != null) {
            SwapchainDescription desc = scDesc.Value;
            this._mainSwapchain = new VkSwapchain(this, ref desc, surface);
            this._perfLastReportTicks = Stopwatch.GetTimestamp();
        }

        this.CreateDescriptorPool();
        this.CreateGraphicsCommandPool();
        for (int i = 0; i < _sharedCommandPoolCount; i++) {
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
    public VkInstance Instance => this._instance;

    /// <summary>
    /// Stores the device state used by this instance.
    /// </summary>
    public VkDevice Device => this._device;

    /// <summary>
    /// Stores the device api state used by this instance.
    /// </summary>
    public VkDeviceApi DeviceApi => this._deviceApi;

    /// <summary>
    /// Stores the instance api state used by this instance.
    /// </summary>
    public VkInstanceApi InstanceApi => this._instanceApi;

    /// <summary>
    /// Gets or sets PhysicalDevice.
    /// </summary>
    public VkPhysicalDevice PhysicalDevice { get; private set; }

    /// <summary>
    /// Stores the physical device mem properties state used by this instance.
    /// </summary>
    public VkPhysicalDeviceMemoryProperties PhysicalDeviceMemProperties => this._physicalDeviceMemProperties;

    /// <summary>
    /// Gets the maximum push-constant payload size, in bytes, supported by the physical device.
    /// </summary>
    internal uint MaxPushConstantsSize => this._physicalDeviceProperties.limits.maxPushConstantsSize;

    /// <summary>
    /// Gets the Vulkan pipeline cache used by pipeline creation.
    /// </summary>
    internal VkPipelineCache PipelineCache => this._pipelineCache;

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
    public void EnableDebugCallback(VkDebugReportFlagsEXT flags = VkDebugReportFlagsEXT.Warning | VkDebugReportFlagsEXT.Error) {
        Debug.WriteLine("Enabling Vulkan Debug callbacks.");
        this._debugCallbackFunc = this.DebugCallback;
        IntPtr debugFunctionPtr = Marshal.GetFunctionPointerForDelegate(this._debugCallbackFunc);
        VkDebugReportCallbackCreateInfoEXT debugCallbackCi = new VkDebugReportCallbackCreateInfoEXT();
        debugCallbackCi.flags = flags;
        debugCallbackCi.pfnCallback = (delegate* unmanaged<VkDebugReportFlagsEXT, VkDebugReportObjectTypeEXT, ulong, UIntPtr, int, byte*, byte*, void*, uint>)debugFunctionPtr;
        IntPtr createFnPtr = (IntPtr)vkGetInstanceProcAddr(this._instance, "vkCreateDebugReportCallbackEXT").Value;

        if (createFnPtr == IntPtr.Zero) {
            return;
        }

        VkCreateDebugReportCallbackExtD createDelegate = Marshal.GetDelegateForFunctionPointer<VkCreateDebugReportCallbackExtD>(createFnPtr);
        VkResult result = createDelegate(this._instance, &debugCallbackCi, IntPtr.Zero, out this._debugCallbackHandle);
        CheckResult(result);
    }

    /// <summary>
    /// Gets the device extension properties value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public VkExtensionProperties[] GetDeviceExtensionProperties() {
        uint propertyCount = 0;
        VkResult result = this._instanceApi.vkEnumerateDeviceExtensionProperties(this.PhysicalDevice, (byte*)null, &propertyCount, null);
        CheckResult(result);
        VkExtensionProperties[] props = new VkExtensionProperties[(int)propertyCount];

        fixed (VkExtensionProperties* properties = props) {
            result = this._instanceApi.vkEnumerateDeviceExtensionProperties(this.PhysicalDevice, (byte*)null, &propertyCount, properties);
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

        this._instanceApi.vkGetPhysicalDeviceImageFormatProperties(this.PhysicalDevice, VkFormats.VdToVkPixelFormat(format), VkImageType.Image2D, VkImageTiling.Optimal, usageFlags, VkImageCreateFlags.None, out VkImageFormatProperties formatProperties);

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
        global::Vortice.Vulkan.VkFence vkFence = Util.AssertSubtype<Fence, VkFence>(fence).DeviceFence;
        this.DeviceApi.vkResetFences(vkFence);
    }

    /// <summary>
    /// Executes the wait for fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool WaitForFence(Fence fence, ulong nanosecondTimeout) {
        global::Vortice.Vulkan.VkFence vkFence = Util.AssertSubtype<Fence, VkFence>(fence).DeviceFence;
        VkResult result = this.DeviceApi.vkWaitForFences(vkFence, true, nanosecondTimeout);
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
        global::Vortice.Vulkan.VkFence* fencesPtr = stackalloc global::Vortice.Vulkan.VkFence[fenceCount];
        for (int i = 0; i < fenceCount; i++) {
            fencesPtr[i] = Util.AssertSubtype<Fence, VkFence>(fences[i]).DeviceFence;
        }

        VkResult result = this.DeviceApi.vkWaitForFences((uint)fenceCount, fencesPtr, waitAll, nanosecondTimeout);
        return result == VkResult.Success;
    }

    /// <summary>
    /// Executes the is supported logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal static bool IsSupported() {
        return _sIsSupported.Value;
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
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.Buffer, buffer.DeviceBuffer.Handle, name);
                    break;

                case VkCommandList commandList:
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.CommandBuffer, (ulong)commandList.CommandBuffer.Handle, $"{name}_CommandBuffer");
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.CommandPool, commandList.CommandPool.Handle, $"{name}_CommandPool");
                    break;

                case VkFramebuffer framebuffer:
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.Framebuffer, framebuffer.CurrentFramebuffer.Handle, name);
                    break;

                case VkPipeline pipeline:
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.Pipeline, pipeline.DevicePipeline.Handle, name);
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.PipelineLayout, pipeline.PipelineLayout.Handle, name);
                    break;

                case VkResourceLayout resourceLayout:
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.DescriptorSetLayout, resourceLayout.DescriptorSetLayout.Handle, name);
                    break;

                case VkResourceSet resourceSet:
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.DescriptorSet, resourceSet.DescriptorSet.Handle, name);
                    break;

                case VkSampler sampler:
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.Sampler, sampler.DeviceSampler.Handle, name);
                    break;

                case VkShader shader:
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.ShaderModule, shader.ShaderModule.Handle, name);
                    break;

                case VkTexture tex:
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.Image, tex.OptimalDeviceImage.Handle, name);
                    break;

                case VkTextureView texView:
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.ImageView, texView.ImageView.Handle, name);
                    break;

                case VkFence fence:
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.Fence, fence.DeviceFence.Handle, name);
                    break;

                case VkSwapchain sc:
                    this.SetDebugMarkerName(VkDebugReportObjectTypeEXT.SwapchainKHR, sc.DeviceSwapchain.Handle, name);
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
            this._instanceApi.vkGetPhysicalDeviceFormatProperties(this.PhysicalDevice, format, out VkFormatProperties vkFormatProps);
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
        SharedCommandPool pool = this.GetFreeCommandPool();
        VkCommandBuffer cb = pool.BeginNewCommandBuffer();
        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, VkImageLayout.TransferDstOptimal);
        VulkanDispatch.GetApi(cb).vkCmdClearColorImage(cb, texture.OptimalDeviceImage, VkImageLayout.TransferDstOptimal, &color, 1, &range);
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
        SharedCommandPool pool = this.GetFreeCommandPool();
        VkCommandBuffer cb = pool.BeginNewCommandBuffer();
        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, VkImageLayout.TransferDstOptimal);
        VulkanDispatch.GetApi(cb).vkCmdClearDepthStencilImage(cb, texture.OptimalDeviceImage, VkImageLayout.TransferDstOptimal, &clearValue, 1, &range);
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
        SharedCommandPool pool = this.GetFreeCommandPool();
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
            this.DeviceApi.vkUnmapMemory(memoryBlock.DeviceMemory);
        }
    }

    /// <summary>
    /// Executes the platform dispose logic for this backend.
    /// </summary>
    protected override void PlatformDispose() {
        Debug.Assert(this._submittedFences.Count == 0);
        foreach (global::Vortice.Vulkan.VkFence fence in this._availableSubmissionFences) {
            this.DeviceApi.vkDestroyFence(fence, null);
        }

        this._mainSwapchain?.Dispose();

        if (this._debugCallbackFunc != null) {
            this._debugCallbackFunc = null;
            IntPtr destroyFuncPtr = (IntPtr)vkGetInstanceProcAddr(this._instance, "vkDestroyDebugReportCallbackEXT").Value;
            VkDestroyDebugReportCallbackExtD destroyDel
                = Marshal.GetDelegateForFunctionPointer<VkDestroyDebugReportCallbackExtD>(destroyFuncPtr);
            destroyDel(this._instance, this._debugCallbackHandle, null);
        }

        this.DescriptorPoolManager.DestroyAll();
        this.DeviceApi.vkDestroyCommandPool(this._graphicsCommandPool, null);

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

        VkResult result = this.DeviceApi.vkDeviceWaitIdle();
        CheckResult(result);
        this.StoreAndDestroyPipelineCache();
        this.DeviceApi.vkDestroyDevice(null);
        this.InstanceApi.vkDestroyInstance(null);
    }

    /// <summary>
    /// Creates the Vulkan pipeline cache used by pipeline creation.
    /// </summary>
    private void CreatePipelineCache() {
        if (!_persistentPipelineCacheEnabled) {
            return;
        }

        byte[] initialData = null;
        try {
            string cachePath = this.GetPersistentPipelineCachePath();
            if (File.Exists(cachePath)) {
                initialData = File.ReadAllBytes(cachePath);
            }
        }
        catch {
            initialData = null;
        }

        VkPipelineCacheCreateInfo cacheCi = new VkPipelineCacheCreateInfo();
        fixed (byte* initialDataPtr = initialData) {
            if (initialDataPtr != null && initialData.Length > 0) {
                cacheCi.initialDataSize = (UIntPtr)initialData.Length;
                cacheCi.pInitialData = initialDataPtr;
            }

            VkResult result = this.DeviceApi.vkCreatePipelineCache(ref cacheCi, null, out this._pipelineCache);
            if (result == VkResult.Success) {
                return;
            }
        }

        cacheCi = new VkPipelineCacheCreateInfo();
        VkResult emptyResult = this.DeviceApi.vkCreatePipelineCache(ref cacheCi, null, out this._pipelineCache);
        if (emptyResult != VkResult.Success) {
            this._pipelineCache = default;
        }
    }

    /// <summary>
    /// Stores and destroys the Vulkan pipeline cache.
    /// </summary>
    private void StoreAndDestroyPipelineCache() {
        if (this._pipelineCache.Handle == 0) {
            return;
        }

        if (_persistentPipelineCacheEnabled) {
            try {
                UIntPtr cacheSize = UIntPtr.Zero;
                VkResult sizeResult = this.DeviceApi.vkGetPipelineCacheData(this._pipelineCache, &cacheSize, null);
                if (sizeResult == VkResult.Success && cacheSize != UIntPtr.Zero) {
                    byte[] cacheData = new byte[(int)cacheSize];
                    fixed (byte* cacheDataPtr = cacheData) {
                        VkResult dataResult = this.DeviceApi.vkGetPipelineCacheData(this._pipelineCache, &cacheSize, cacheDataPtr);
                        if (dataResult == VkResult.Success) {
                            Directory.CreateDirectory(_persistentPipelineCacheDirectory);
                            File.WriteAllBytes(this.GetPersistentPipelineCachePath(), cacheData);
                        }
                    }
                }
            }
            catch {
                // Pipeline cache failures must not prevent device disposal.
            }
        }

        this.DeviceApi.vkDestroyPipelineCache(this._pipelineCache, null);
        this._pipelineCache = default;
    }

    /// <summary>
    /// Gets the persistent Vulkan pipeline cache path for the current physical device and driver.
    /// </summary>
    /// <returns>The persistent pipeline cache path.</returns>
    private string GetPersistentPipelineCachePath() {
        return Path.Combine(
            _persistentPipelineCacheDirectory,
            $"{this._physicalDeviceProperties.vendorID:X8}_{this._physicalDeviceProperties.deviceID:X8}_{this._physicalDeviceProperties.driverVersion:X8}.bin");
    }

    /// <summary>
    /// Executes the check is supported logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool CheckIsSupported() {
        if (!IsVulkanLoaded()) {
            return false;
        }

        VkInstanceCreateInfo instanceCi = new VkInstanceCreateInfo();
        VkApplicationInfo applicationInfo = new() {
            apiVersion = new global::Vortice.Vulkan.VkVersion(1, 0, 0),
            applicationVersion = new global::Vortice.Vulkan.VkVersion(1, 0, 0),
            engineVersion = new global::Vortice.Vulkan.VkVersion(1, 0, 0),
            pApplicationName = _sName,
            pEngineName = _sName
        };

        instanceCi.pApplicationInfo = &applicationInfo;

        VkResult result = vkCreateInstance(ref instanceCi, null, out VkInstance testInstance);
        if (result != VkResult.Success) {
            return false;
        }

        VkInstanceApi testInstanceApi = new(ref testInstance);
        uint physicalDeviceCount = 0;
        result = testInstanceApi.vkEnumeratePhysicalDevices(out physicalDeviceCount);

        if (result != VkResult.Success || physicalDeviceCount == 0) {
            testInstanceApi.vkDestroyInstance(null);
            return false;
        }

        testInstanceApi.vkDestroyInstance(null);

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
    private void SubmitCommandList(CommandList cl, uint waitSemaphoreCount, VkSemaphore* waitSemaphoresPtr, uint signalSemaphoreCount, VkSemaphore* signalSemaphoresPtr, Fence fence) {
        VkCommandList vkCl = Util.AssertSubtype<CommandList, VkCommandList>(cl);
        VkCommandBuffer vkCb = vkCl.CommandBuffer;

        vkCl.CommandBufferSubmitted(vkCb);
        this.SubmitCommandBuffer(vkCl, vkCb, waitSemaphoreCount, waitSemaphoresPtr, signalSemaphoreCount, signalSemaphoresPtr, fence);
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
    private void SubmitCommandBuffer(VkCommandList vkCl, VkCommandBuffer vkCb, uint waitSemaphoreCount, VkSemaphore* waitSemaphoresPtr, uint signalSemaphoreCount, VkSemaphore* signalSemaphoresPtr, Fence fence) {
        this.CheckSubmittedFences();

        bool useExtraFence = fence != null;
        VkSubmitInfo si = new VkSubmitInfo();
        si.commandBufferCount = 1;
        si.pCommandBuffers = &vkCb;
        VkPipelineStageFlags waitDstStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
        si.pWaitDstStageMask = &waitDstStageMask;

        si.pWaitSemaphores = waitSemaphoresPtr;
        si.waitSemaphoreCount = waitSemaphoreCount;
        si.pSignalSemaphores = signalSemaphoresPtr;
        si.signalSemaphoreCount = signalSemaphoreCount;

        global::Vortice.Vulkan.VkFence vkFence;
        bool ownsSubmissionFence;

        if (useExtraFence) {
            vkFence = Util.AssertSubtype<Fence, VkFence>(fence).DeviceFence;
            ownsSubmissionFence = false;
        }
        else {
            vkFence = this.getFreeSubmissionFence();
            ownsSubmissionFence = true;
        }

        lock (this._graphicsQueueLock) {
            VkResult result = VulkanDispatch.GetApi(this._graphicsQueue).vkQueueSubmit(this._graphicsQueue, 1, &si, vkFence);
            CheckResult(result);
        }

        lock (this._submittedFencesLock) {
            this._submittedFences.Add(new FenceSubmissionInfo(vkFence, ownsSubmissionFence, vkCl, vkCb));
        }
    }

    /// <summary>
    /// Executes the check submitted fences logic for this backend.
    /// </summary>
    private void CheckSubmittedFences() {
        lock (this._submittedFencesLock) {
            for (int i = 0; i < this._submittedFences.Count; i++) {
                FenceSubmissionInfo fsi = this._submittedFences[i];

                if (this.DeviceApi.vkGetFenceStatus(fsi.Fence) == VkResult.Success) {
                    this.CompleteFenceSubmission(fsi);
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
    private void CompleteFenceSubmission(FenceSubmissionInfo fsi) {
        global::Vortice.Vulkan.VkFence fence = fsi.Fence;
        VkCommandBuffer completedCb = fsi.CommandBuffer;
        fsi.CommandList?.CommandBufferCompleted(completedCb);
        if (fsi.OwnsFence) {
            VkResult resetResult = this.DeviceApi.vkResetFences(fence);
            CheckResult(resetResult);
            this.ReturnSubmissionFence(fence);
        }

        lock (this._stagingResourcesLock) {
            if (this._submittedStagingTextures.Remove(completedCb, out VkTexture stagingTex)) {
                this._availableStagingTextures.Add(stagingTex);
            }

            if (this._submittedStagingBuffers.Remove(completedCb, out List<VkBuffer> stagingBuffers)) {
                for (int i = 0; i < stagingBuffers.Count; i++) {
                    VkBuffer stagingBuffer = stagingBuffers[i];
                    if (stagingBuffer.SizeInBytes <= _maxStagingBufferSize) {
                        this._availableStagingBuffers.Add(stagingBuffer);
                    }
                    else {
                        stagingBuffer.Dispose();
                    }
                }

                stagingBuffers.Clear();
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
    /// Reports lightweight Vulkan frame timing diagnostics.
    /// </summary>
    /// <param name="swapchain">The swapchain used for the frame.</param>
    private void ReportPerf(VkSwapchain swapchain) {
        this._perfFrameCount++;
        long nowTicks = Stopwatch.GetTimestamp();
        double windowMs = TicksToMilliseconds(nowTicks - this._perfLastReportTicks);
        if (windowMs < 1000.0) {
            return;
        }

        double invFrames = this._perfFrameCount == 0 ? 0.0 : 1.0 / this._perfFrameCount;
        double fps = this._perfFrameCount * 1000.0 / windowMs;
        Console.WriteLine($"[VK PERF] fps={fps:F0}, presentMode={swapchain.PresentMode}, submit={this._perfSubmitMs * invFrames:F3}ms, present={this._perfPresentMs * invFrames:F3}ms, acquire={this._perfAcquireMs * invFrames:F3}ms");

        this._perfSubmitMs = 0.0;
        this._perfPresentMs = 0.0;
        this._perfAcquireMs = 0.0;
        this._perfFrameCount = 0;
        this._perfLastReportTicks = nowTicks;
    }

    /// <summary>
    /// Converts high-resolution stopwatch ticks to milliseconds.
    /// </summary>
    /// <param name="ticks">The stopwatch ticks to convert.</param>
    /// <returns>The converted duration in milliseconds.</returns>
    private static double TicksToMilliseconds(long ticks) {
        return ticks * 1000.0 / Stopwatch.Frequency;
    }

    /// <summary>
    /// Executes the return submission fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    private void ReturnSubmissionFence(global::Vortice.Vulkan.VkFence fence) {
        this._availableSubmissionFences.Enqueue(fence);
    }

    /// <summary>
    /// Gets the free submission fence value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private global::Vortice.Vulkan.VkFence getFreeSubmissionFence() {
        if (this._availableSubmissionFences.TryDequeue(out global::Vortice.Vulkan.VkFence availableFence)) {
            return availableFence;
        }

        VkFenceCreateInfo fenceCi = new VkFenceCreateInfo();
        VkResult result = this.DeviceApi.vkCreateFence(ref fenceCi, null, out global::Vortice.Vulkan.VkFence newFence);
        CheckResult(result);
        return newFence;
    }

    /// <summary>
    /// Sets the debug marker name value.
    /// </summary>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="name">The name used by this operation.</param>
    private void SetDebugMarkerName(VkDebugReportObjectTypeEXT type, ulong target, string name) {
        Debug.Assert(this._setObjectNameDelegate != null);

        VkDebugMarkerObjectNameInfoEXT nameInfo = new VkDebugMarkerObjectNameInfoEXT();
        nameInfo.objectType = type;
        nameInfo.@object = target;

        int byteCount = Encoding.UTF8.GetByteCount(name);
        byte* utf8Ptr = stackalloc byte[byteCount + 1];
        fixed (char* namePtr = name) {
            Encoding.UTF8.GetBytes(namePtr, name.Length, utf8Ptr, byteCount);
        }

        utf8Ptr[byteCount] = 0;

        nameInfo.pObjectName = utf8Ptr;
        VkResult result = this._setObjectNameDelegate(this._device, &nameInfo);
        CheckResult(result);
    }

    /// <summary>
    /// Creates the instance instance used by this backend.
    /// </summary>
    /// <param name="debug">The debug value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    private void CreateInstance(bool debug, VulkanDeviceOptions options) {
        HashSet<string> availableInstanceLayers = new(EnumerateInstanceLayers());
        HashSet<string> availableInstanceExtensions = new(GetInstanceExtensions());

        VkInstanceCreateInfo instanceCi = new VkInstanceCreateInfo();
        VkApplicationInfo applicationInfo = new() {
            apiVersion = new global::Vortice.Vulkan.VkVersion(1, 0, 0),
            applicationVersion = new global::Vortice.Vulkan.VkVersion(1, 0, 0),
            engineVersion = new global::Vortice.Vulkan.VkVersion(1, 0, 0),
            pApplicationName = _sName,
            pEngineName = _sName
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
            instanceCi.flags |= _vkInstanceCreateEnumeratePortabilityBitKhr;
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

        VkResult result = vkCreateInstance(ref instanceCi, null, out this._instance);
        CheckResult(result);
        this._instanceApi = VulkanDispatch.RegisterInstance(this._instance);

        if (this.HasSurfaceExtension(CommonStrings.VkExtMetalSurfaceExtensionName)) {
            this.CreateMetalSurfaceExt = this.GetInstanceProcAddr<VkCreateMetalSurfaceExtT>("vkCreateMetalSurfaceEXT");
        }

        if (debug && debugReportExtensionAvailable) {
            this.EnableDebugCallback();
        }

        if (hasDeviceProperties2) {
            this._getPhysicalDeviceProperties2 = this.GetInstanceProcAddr<VkGetPhysicalDeviceProperties2T>("vkGetPhysicalDeviceProperties2")
                                             ?? this.GetInstanceProcAddr<VkGetPhysicalDeviceProperties2T>("vkGetPhysicalDeviceProperties2KHR");
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
    private uint DebugCallback(VkDebugReportFlagsEXT flags, VkDebugReportObjectTypeEXT objectType, ulong @object, UIntPtr location, int messageCode, byte* pLayerPrefix, byte* pMessage, void* pUserData) {
        string message = Util.GetString(pMessage);
        VkDebugReportFlagsEXT debugReportFlags = flags;

#if DEBUG
        if (Debugger.IsAttached) Debugger.Break();
#endif

        string fullMessage = $"[{debugReportFlags}] ({objectType}) {message}";

        Console.WriteLine(fullMessage);
        return 0;
    }

    /// <summary>
    /// Creates the physical device instance used by this backend.
    /// </summary>
    private void CreatePhysicalDevice() {
        uint deviceCount = 0;
        this._instanceApi.vkEnumeratePhysicalDevices(out deviceCount);
        if (deviceCount == 0) {
            throw new InvalidOperationException("No physical devices exist.");
        }

        VkPhysicalDevice[] physicalDevices = new VkPhysicalDevice[deviceCount];
        this._instanceApi.vkEnumeratePhysicalDevices(physicalDevices.AsSpan());
        VulkanDispatch.RegisterPhysicalDevices(this._instanceApi, physicalDevices, deviceCount);
        VkPhysicalDevice selectedPhysicalDevice = default;
        VkPhysicalDeviceProperties selectedProperties = default;
        int selectedScore = int.MinValue;

        foreach (VkPhysicalDevice physicalDevice in physicalDevices) {
            if (!this.HasGraphicsQueueFamily(physicalDevice)) {
                continue;
            }

            this._instanceApi.vkGetPhysicalDeviceProperties(physicalDevice, out VkPhysicalDeviceProperties properties);
            int score = GetPhysicalDeviceScore(properties);
            if (score > selectedScore) {
                selectedScore = score;
                selectedPhysicalDevice = physicalDevice;
                selectedProperties = properties;
            }
        }

        if (selectedScore == int.MinValue) {
            throw new InvalidOperationException("No Vulkan physical device with a graphics queue was found.");
        }

        this.PhysicalDevice = selectedPhysicalDevice;
        this._physicalDeviceProperties = selectedProperties;

        fixed (byte* utf8NamePtr = this._physicalDeviceProperties.deviceName) {
            this._deviceName = Encoding.UTF8.GetString(utf8NamePtr, (int)MaxPhysicalDeviceNameSize).TrimEnd('\0');
        }

        this._vendorName = "id:" + this._physicalDeviceProperties.vendorID.ToString("x8");
        this._apiVersion = GetGraphicsApiVersion(this._physicalDeviceProperties.apiVersion);
        this.DriverInfo = "version:" + this._physicalDeviceProperties.driverVersion.ToString("x8");

        this._instanceApi.vkGetPhysicalDeviceFeatures(this.PhysicalDevice, out this._physicalDeviceFeatures);

        this._instanceApi.vkGetPhysicalDeviceMemoryProperties(this.PhysicalDevice, out this._physicalDeviceMemProperties);
    }

    /// <summary>
    /// Checks whether a physical device exposes at least one graphics-capable queue family.
    /// </summary>
    /// <param name="physicalDevice">The physical device to inspect.</param>
    /// <returns><see langword="true" /> if a graphics queue exists; otherwise, <see langword="false" />.</returns>
    private bool HasGraphicsQueueFamily(VkPhysicalDevice physicalDevice) {
        uint queueFamilyCount = 0;
        this._instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, null);
        if (queueFamilyCount == 0) {
            return false;
        }

        VkQueueFamilyProperties[] queueFamilies = new VkQueueFamilyProperties[queueFamilyCount];
        fixed (VkQueueFamilyProperties* queueFamiliesPtr = queueFamilies) {
            this._instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, queueFamiliesPtr);
        }

        for (int i = 0; i < queueFamilies.Length; i++) {
            if ((queueFamilies[i].queueFlags & VkQueueFlags.Graphics) != 0) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Computes a simple preference score used to pick the default Vulkan physical device.
    /// </summary>
    /// <param name="properties">The physical device properties.</param>
    /// <returns>A higher score indicates a stronger default candidate.</returns>
    private static int GetPhysicalDeviceScore(in VkPhysicalDeviceProperties properties) {
        int score = properties.deviceType switch {
            VkPhysicalDeviceType.DiscreteGpu => 5000,
            VkPhysicalDeviceType.IntegratedGpu => 4000,
            VkPhysicalDeviceType.VirtualGpu => 3000,
            VkPhysicalDeviceType.Cpu => 1000,
            _ => 2000
        };

        // Slightly prefer devices reporting larger image limits among same class.
        score += (int)Math.Min(properties.limits.maxImageDimension2D, 8192u);
        return score;
    }

    /// <summary>
    /// Converts a packed Vulkan API version into a <see cref="GraphicsApiVersion" />.
    /// </summary>
    /// <param name="vkVersion">The packed Vulkan API version.</param>
    /// <returns>The converted API version.</returns>
    private static GraphicsApiVersion GetGraphicsApiVersion(uint vkVersion) {
        if (vkVersion == 0) {
            return GraphicsApiVersion.Unknown;
        }

        int major = (int)((vkVersion >> 22) & 0x3FF);
        int minor = (int)((vkVersion >> 12) & 0x3FF);
        int patch = (int)(vkVersion & 0xFFF);
        return new GraphicsApiVersion(major, minor, 0, patch);
    }

    /// <summary>
    /// Creates the logical device instance used by this backend.
    /// </summary>
    /// <param name="surface">The surface value used by this operation.</param>
    /// <param name="preferStandardClipY">The prefer standard clip y value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    private void CreateLogicalDevice(VkSurfaceKHR surface, bool preferStandardClipY, VulkanDeviceOptions options) {
        this.GetQueueFamilyIndices(surface);

        HashSet<uint> familyIndices = new() { this.GraphicsQueueIndex, this.PresentQueueIndex };
        VkDeviceQueueCreateInfo* queueCreateInfos = stackalloc VkDeviceQueueCreateInfo[familyIndices.Count];
        uint queueCreateInfosCount = (uint)familyIndices.Count;

        int i = 0;

        foreach (uint familyIndex in familyIndices) {
            VkDeviceQueueCreateInfo queueCreateInfo = new VkDeviceQueueCreateInfo();
            queueCreateInfo.queueFamilyIndex = familyIndex;
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

        VkDeviceCreateInfo deviceCreateInfo = new VkDeviceCreateInfo();
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

            VkResult result = this._instanceApi.vkCreateDevice(this.PhysicalDevice, ref deviceCreateInfo, null, out this._device);
            CheckResult(result);
            this._deviceApi = VulkanDispatch.RegisterDevice(this._instanceApi, this._device);
        }

        this.DeviceApi.vkGetDeviceQueue(this.GraphicsQueueIndex, 0, out this._graphicsQueue);
        VulkanDispatch.RegisterQueue(this._graphicsQueue, this._deviceApi);

        if (this._debugMarkerEnabled) {
            this._setObjectNameDelegate = Marshal.GetDelegateForFunctionPointer<VkDebugMarkerSetObjectNameExtT>(this.GetInstanceProcAddr("vkDebugMarkerSetObjectNameEXT"));
            this.MarkerBegin = Marshal.GetDelegateForFunctionPointer<VkCmdDebugMarkerBeginExtT>(this.GetInstanceProcAddr("vkCmdDebugMarkerBeginEXT"));
            this.MarkerEnd = Marshal.GetDelegateForFunctionPointer<VkCmdDebugMarkerEndExtT>(this.GetInstanceProcAddr("vkCmdDebugMarkerEndEXT"));
            this.MarkerInsert = Marshal.GetDelegateForFunctionPointer<VkCmdDebugMarkerInsertExtT>(this.GetInstanceProcAddr("vkCmdDebugMarkerInsertEXT"));
        }

        if (hasDedicatedAllocation && hasMemReqs2) {
        this.GetBufferMemoryRequirements2 = this.GetDeviceProcAddr<VkGetBufferMemoryRequirements2T>("vkGetBufferMemoryRequirements2")
                                          ?? this.GetDeviceProcAddr<VkGetBufferMemoryRequirements2T>("vkGetBufferMemoryRequirements2KHR");
        this.GetImageMemoryRequirements2 = this.GetDeviceProcAddr<VkGetImageMemoryRequirements2T>("vkGetImageMemoryRequirements2")
                                         ?? this.GetDeviceProcAddr<VkGetImageMemoryRequirements2T>("vkGetImageMemoryRequirements2KHR");
        }

        if (this._getPhysicalDeviceProperties2 != null && hasDriverProperties) {
            VkPhysicalDeviceProperties2 deviceProps = new VkPhysicalDeviceProperties2();
            VkPhysicalDeviceDriverProperties driverProps = new VkPhysicalDeviceDriverProperties();

            deviceProps.pNext = &driverProps;
            this._getPhysicalDeviceProperties2(this.PhysicalDevice, &deviceProps);

            string driverName = Encoding.UTF8.GetString(driverProps.DriverName, VkPhysicalDeviceDriverProperties.DRIVER_NAME_LENGTH).TrimEnd('\0');

            string driverInfo = Encoding.UTF8.GetString(driverProps.DriverInfo, VkPhysicalDeviceDriverProperties.DRIVER_INFO_LENGTH).TrimEnd('\0');

            this.DriverName = driverName;
            this.DriverInfo = driverInfo;
        }
    }

    /// <summary>
    /// Gets the instance proc addr value.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private IntPtr GetInstanceProcAddr(string name) {
        return (IntPtr)vkGetInstanceProcAddr(this._instance, name).Value;
    }

    /// <summary>
    /// Resolves a Vulkan instance-level function pointer and returns it as a typed delegate.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private T GetInstanceProcAddr<T>(string name) {
        IntPtr funcPtr = this.GetInstanceProcAddr(name);
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
    private IntPtr GetDeviceProcAddr(string name) {
        return (IntPtr)this._instanceApi.vkGetDeviceProcAddr(this._device, name).Value;
    }

    /// <summary>
    /// Resolves a Vulkan device-level function pointer and returns it as a typed delegate.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private T GetDeviceProcAddr<T>(string name) {
        IntPtr funcPtr = this.GetDeviceProcAddr(name);
        if (funcPtr != IntPtr.Zero) {
            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }

        return default;
    }

    /// <summary>
    /// Gets the queue family indices value.
    /// </summary>
    /// <param name="surface">The surface value used by this operation.</param>
    private void GetQueueFamilyIndices(VkSurfaceKHR surface) {
        uint queueFamilyCount = 0;
        this._instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(this.PhysicalDevice, &queueFamilyCount, null);
        VkQueueFamilyProperties[] qfp = new VkQueueFamilyProperties[queueFamilyCount];
        fixed (VkQueueFamilyProperties* qfpPtr = qfp) {
            this._instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(this.PhysicalDevice, &queueFamilyCount, qfpPtr);
        }

        // Prefer a single queue family that supports both graphics and present to avoid
        // cross-queue synchronization and concurrent swapchain sharing.
        if (surface.IsNotNull) {
            for (uint i = 0; i < qfp.Length; i++) {
                if ((qfp[i].queueFlags & VkQueueFlags.Graphics) == 0) {
                    continue;
                }

                this._instanceApi.vkGetPhysicalDeviceSurfaceSupportKHR(this.PhysicalDevice, i, surface, out VkBool32 presentSupported);
                if (presentSupported) {
                    this.GraphicsQueueIndex = i;
                    this.PresentQueueIndex = i;
                    if (PerfLogEnabled) {
                        Console.WriteLine($"[VK PERF] queueFamilies selected graphics={this.GraphicsQueueIndex}, present={this.PresentQueueIndex} (shared)");
                    }
                    return;
                }
            }
        }

        uint? graphicsIndex = null;
        uint? presentIndex = surface.IsNull ? 0u : null;

        for (uint i = 0; i < qfp.Length; i++) {
            if (graphicsIndex == null && (qfp[i].queueFlags & VkQueueFlags.Graphics) != 0) {
                graphicsIndex = i;
            }

            if (presentIndex == null) {
                this._instanceApi.vkGetPhysicalDeviceSurfaceSupportKHR(this.PhysicalDevice, i, surface, out VkBool32 presentSupported);
                if (presentSupported) {
                    presentIndex = i;
                }
            }

            if (graphicsIndex != null && presentIndex != null) {
                this.GraphicsQueueIndex = graphicsIndex.Value;
                this.PresentQueueIndex = presentIndex.Value;
                if (PerfLogEnabled) {
                    Console.WriteLine($"[VK PERF] queueFamilies selected graphics={this.GraphicsQueueIndex}, present={this.PresentQueueIndex} (split)");
                }
                return;
            }
        }

        throw new VeldridException("Failed to find Vulkan queue families for graphics/present.");
    }

    /// <summary>
    /// Creates the descriptor pool instance used by this backend.
    /// </summary>
    private void CreateDescriptorPool() {
        this.DescriptorPoolManager = new VkDescriptorPoolManager(this);
    }

    /// <summary>
    /// Creates the graphics command pool instance used by this backend.
    /// </summary>
    private void CreateGraphicsCommandPool() {
        VkCommandPoolCreateInfo commandPoolCi = new VkCommandPoolCreateInfo();
        commandPoolCi.flags = VkCommandPoolCreateFlags.ResetCommandBuffer;
        commandPoolCi.queueFamilyIndex = this.GraphicsQueueIndex;
        VkResult result = this.DeviceApi.vkCreateCommandPool(ref commandPoolCi, null, out this._graphicsCommandPool);
        CheckResult(result);
    }

    /// <summary>
    /// Gets the free command pool value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private SharedCommandPool GetFreeCommandPool() {
        SharedCommandPool sharedPool = null;

        lock (this._graphicsCommandPoolLock) {
            if (this._sharedGraphicsCommandPools.Count > 0) {
                sharedPool = this._sharedGraphicsCommandPools.Pop();
            }
        }

        return sharedPool ?? new SharedCommandPool(this, false);
    }

    /// <summary>
    /// Ensures a command buffer is open for batched immediate uploads.
    /// </summary>
    private void EnsureImmediateUploadCommandBuffer() {
        if (this._immediateUploadCb.IsNull) {
            this._immediateUploadPool = this.GetFreeCommandPool();
            this._immediateUploadCb = this._immediateUploadPool.BeginNewCommandBuffer();
        }
    }

    /// <summary>
    /// Submits any buffered immediate upload commands.
    /// </summary>
    private void FlushImmediateUploads() {
        VkCommandBuffer uploadCb = default;
        SharedCommandPool uploadPool = null;
        List<VkBuffer> uploadStagingBuffers = null;

        lock (this._immediateUploadLock) {
            if (this._immediateUploadCb.IsNull) {
                return;
            }

            uploadCb = this._immediateUploadCb;
            uploadPool = this._immediateUploadPool;
            this._immediateUploadCb = default;
            this._immediateUploadPool = null;
            uploadStagingBuffers = new List<VkBuffer>(this._immediateUploadStagingBuffers);
            this._immediateUploadStagingBuffers.Clear();
        }

        VkResult endResult = VulkanDispatch.GetApi(uploadCb).vkEndCommandBuffer(uploadCb);
        CheckResult(endResult);
        this.SubmitCommandBuffer(null, uploadCb, 0, null, 0, null, null);

        lock (this._stagingResourcesLock) {
            this._submittedSharedCommandPools.Add(uploadCb, uploadPool);
            this._submittedStagingBuffers.Add(uploadCb, uploadStagingBuffers);
        }
    }

    /// <summary>
    /// Maps the buffer resource for CPU access.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="numBytes">The num bytes value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private IntPtr MapBuffer(VkBuffer buffer, uint numBytes) {
        if (buffer.Memory.IsPersistentMapped) {
            return (IntPtr)buffer.Memory.BlockMappedPointer;
        }

        void* mappedPtr;
        VkResult result = this.DeviceApi.vkMapMemory(buffer.Memory.DeviceMemory, buffer.Memory.Offset, numBytes, 0, &mappedPtr);
        CheckResult(result);
        return (IntPtr)mappedPtr;
    }

    /// <summary>
    /// Unmaps the buffer resource from CPU access.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    private void UnmapBuffer(VkBuffer buffer) {
        if (!buffer.Memory.IsPersistentMapped) {
            this.DeviceApi.vkUnmapMemory(buffer.Memory.DeviceMemory);
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
    private VkTexture GetFreeStagingTexture(uint width, uint height, uint depth, PixelFormat format) {
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
    private VkBuffer GetFreeStagingBuffer(uint size) {
        lock (this._stagingResourcesLock) {
            for (int i = 0; i < this._availableStagingBuffers.Count; i++) {
                VkBuffer buffer = this._availableStagingBuffers[i];

                if (buffer.SizeInBytes >= size) {
                    this._availableStagingBuffers.RemoveAt(i);
                    return buffer;
                }
            }
        }

        uint newBufferSize = Math.Max(_minStagingBufferSize, size);
        VkBuffer newBuffer = (VkBuffer)this.ResourceFactory.CreateBuffer(new BufferDescription(newBufferSize, BufferUsage.Staging));
        return newBuffer;
    }

    /// <summary>
    /// Executes the submit commands core logic for this backend.
    /// </summary>
    /// <param name="cl">The cl value used by this operation.</param>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    private protected override void SubmitCommandsCore(CommandList cl, Fence fence) {
        long startTicks = PerfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushImmediateUploads();

        if (this._mainSwapchain != null
            && cl is VkCommandList vkCommandList
            && vkCommandList.UsesSwapchainFramebuffer) {
            VkSemaphore waitSemaphore = this._mainSwapchain.ImageAvailableSemaphore;
            if (this._mainSwapchain.PresentQueueIndex == this.GraphicsQueueIndex) {
                this.SubmitCommandList(cl, 1, &waitSemaphore, 0, null, fence);
            }
            else {
                VkSemaphore signalSemaphore = this._mainSwapchain.RenderFinishedSemaphore;
                this.SubmitCommandList(cl, 1, &waitSemaphore, 1, &signalSemaphore, fence);
            }

            if (PerfLogEnabled) {
                this._perfSubmitMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            }

            return;
        }

        this.SubmitCommandList(cl, 0, null, 0, null, fence);
        if (PerfLogEnabled) {
            this._perfSubmitMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Executes the swap buffers core logic for this backend.
    /// </summary>
    /// <param name="swapchain">The swapchain used by this operation.</param>
    private protected override void SwapBuffersCore(Swapchain swapchain) {
        this.FlushImmediateUploads();

        VkSwapchain vkSc = Util.AssertSubtype<Swapchain, VkSwapchain>(swapchain);
        VkSwapchainKHR deviceSwapchain = vkSc.DeviceSwapchain;
        VkPresentInfoKHR presentInfo = new VkPresentInfoKHR();
        presentInfo.swapchainCount = 1;
        presentInfo.pSwapchains = &deviceSwapchain;
        uint imageIndex = vkSc.ImageIndex;
        presentInfo.pImageIndices = &imageIndex;
        VkSemaphore waitSemaphore = vkSc.RenderFinishedSemaphore;
        if (vkSc.PresentQueueIndex != this.GraphicsQueueIndex) {
            presentInfo.waitSemaphoreCount = 1;
            presentInfo.pWaitSemaphores = &waitSemaphore;
        }

        object presentLock = vkSc.PresentQueueIndex == this.GraphicsQueueIndex ? this._graphicsQueueLock : vkSc;

        lock (presentLock) {
            long presentStartTicks = PerfLogEnabled ? Stopwatch.GetTimestamp() : 0;
            VulkanDispatch.GetApi(vkSc.PresentQueue).vkQueuePresentKHR(vkSc.PresentQueue, &presentInfo);
            if (PerfLogEnabled) {
                this._perfPresentMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - presentStartTicks);
            }

            long acquireStartTicks = PerfLogEnabled ? Stopwatch.GetTimestamp() : 0;
            vkSc.AcquireNextImage(this._device, vkSc.ImageAvailableSemaphore, default);
            if (PerfLogEnabled) {
                this._perfAcquireMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - acquireStartTicks);
                this.ReportPerf(vkSc);
            }
        }
    }

    /// <summary>
    /// Reads a boolean diagnostic environment variable.
    /// </summary>
    /// <param name="name">The environment variable name.</param>
    /// <returns><see langword="true" /> when the variable is enabled; otherwise, <see langword="false" />.</returns>
    private static bool IsEnvironmentEnabled(string name) {
        string value = (Environment.GetEnvironmentVariable(name) ?? string.Empty).Trim().Trim('\'', '"');
        return string.Equals(value, "1", StringComparison.Ordinal)
               || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Executes the wait for idle core logic for this backend.
    /// </summary>
    private protected override void WaitForIdleCore() {
        this.FlushImmediateUploads();

        lock (this._graphicsQueueLock) {
            VulkanDispatch.GetApi(this._graphicsQueue).vkQueueWaitIdle(this._graphicsQueue);
        }

        this.CheckSubmittedFences();
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

        VkResult result = this._instanceApi.vkGetPhysicalDeviceImageFormatProperties(this.PhysicalDevice, vkFormat, vkType, tiling, vkUsage, VkImageCreateFlags.None, out VkImageFormatProperties vkProps);

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
        IntPtr mappedPtr;
        byte* destPtr;
        bool isPersistentMapped = vkBuffer.Memory.IsPersistentMapped;

        if (isPersistentMapped) {
            mappedPtr = (IntPtr)vkBuffer.Memory.BlockMappedPointer;
            destPtr = (byte*)mappedPtr + bufferOffsetInBytes;
        }
        else {
            VkBuffer copySrcVkBuffer = this.GetFreeStagingBuffer(sizeInBytes);
            mappedPtr = (IntPtr)copySrcVkBuffer.Memory.BlockMappedPointer;
            destPtr = (byte*)mappedPtr;
            Unsafe.CopyBlock(destPtr, source.ToPointer(), sizeInBytes);

            lock (this._immediateUploadLock) {
                this.EnsureImmediateUploadCommandBuffer();
                VkBufferCopy copyRegion = new() {
                    dstOffset = bufferOffsetInBytes,
                    size = sizeInBytes
                };
                VulkanDispatch.GetApi(this._immediateUploadCb).vkCmdCopyBuffer(this._immediateUploadCb, copySrcVkBuffer.DeviceBuffer, vkBuffer.DeviceBuffer, 1, &copyRegion);
                this._immediateUploadStagingBuffers.Add(copySrcVkBuffer);
            }

            return;
        }

        Unsafe.CopyBlock(destPtr, source.ToPointer(), sizeInBytes);
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
            VkTexture stagingTex = this.GetFreeStagingTexture(width, height, depth, texture.Format);
            this.UpdateTexture(stagingTex, source, sizeInBytes, 0, 0, 0, width, height, depth, 0, 0);
            SharedCommandPool pool = this.GetFreeCommandPool();
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
        private readonly VkCommandBuffer _cb;

        /// <summary>
        /// Stores the graphics device used by this instance.
        /// </summary>
        private readonly VkGraphicsDevice _gd;

        /// <summary>
        /// Stores the pool state used by this instance.
        /// </summary>
        private readonly VkCommandPool _pool;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedCommandPool" /> type.
        /// </summary>
        /// <param name="gd">The graphics device that owns this operation.</param>
        /// <param name="isCached">The is cached value used by this operation.</param>
        public SharedCommandPool(VkGraphicsDevice gd, bool isCached) {
            this._gd = gd;
            this.IsCached = isCached;

            VkCommandPoolCreateInfo commandPoolCi = new VkCommandPoolCreateInfo();
            commandPoolCi.flags = VkCommandPoolCreateFlags.Transient | VkCommandPoolCreateFlags.ResetCommandBuffer;
            commandPoolCi.queueFamilyIndex = this._gd.GraphicsQueueIndex;
            VkResult result = this._gd.DeviceApi.vkCreateCommandPool(ref commandPoolCi, null, out this._pool);
            CheckResult(result);

            VkCommandBufferAllocateInfo allocateInfo = new VkCommandBufferAllocateInfo();
            allocateInfo.commandBufferCount = 1;
            allocateInfo.level = VkCommandBufferLevel.Primary;
            allocateInfo.commandPool = this._pool;
            fixed (VkCommandBuffer* cbPtr = &this._cb) {
                result = this._gd.DeviceApi.vkAllocateCommandBuffers(&allocateInfo, cbPtr);
            }
            CheckResult(result);
            VulkanDispatch.RegisterCommandBuffer(this._cb, this._gd.DeviceApi);
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
            VkCommandBufferBeginInfo beginInfo = new VkCommandBufferBeginInfo();
            beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;
            VkResult result = VulkanDispatch.GetApi(this._cb).vkBeginCommandBuffer(this._cb, &beginInfo);
            CheckResult(result);

            return this._cb;
        }

        /// <summary>
        /// Ends the and submit operation.
        /// </summary>
        /// <param name="cb">The cb value used by this operation.</param>
        public void EndAndSubmit(VkCommandBuffer cb) {
            VkResult result = VulkanDispatch.GetApi(cb).vkEndCommandBuffer(cb);
            CheckResult(result);
            this._gd.SubmitCommandBuffer(null, cb, 0, null, 0, null, null);
            lock (this._gd._stagingResourcesLock) {
                this._gd._submittedSharedCommandPools.Add(cb, this);
            }
        }

        /// <summary>
        /// Executes the destroy logic for this backend.
        /// </summary>
        internal void Destroy() {
            this._gd.DeviceApi.vkDestroyCommandPool(this._pool, null);
        }
    }

    /// <summary>
    /// Represents the FenceSubmissionInfo data structure used by the graphics runtime.
    /// </summary>
    private struct FenceSubmissionInfo {

        /// <summary>
        /// Stores the fence state used by this instance.
        /// </summary>
        public readonly global::Vortice.Vulkan.VkFence Fence;

        /// <summary>
        /// Stores the command list collection used by this instance.
        /// </summary>
        public readonly VkCommandList CommandList;

        /// <summary>
        /// Stores whether the fence is owned by the submission-fence pool.
        /// </summary>
        public readonly bool OwnsFence;

        /// <summary>
        /// Stores the command buffer state used by this instance.
        /// </summary>
        public readonly VkCommandBuffer CommandBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="FenceSubmissionInfo" /> type.
        /// </summary>
        /// <param name="fence">The synchronization fence used by this operation.</param>
        /// <param name="ownsFence">Whether the fence should be reset and returned to the internal pool.</param>
        /// <param name="commandList">The command list used by this operation.</param>
        /// <param name="commandBuffer">The command buffer value used by this operation.</param>
        public FenceSubmissionInfo(global::Vortice.Vulkan.VkFence fence, bool ownsFence, VkCommandList commandList, VkCommandBuffer commandBuffer) {
            this.Fence = fence;
            this.OwnsFence = ownsFence;
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

internal unsafe delegate uint VkDebugReportCallbackExtD(VkDebugReportFlagsEXT flags, VkDebugReportObjectTypeEXT objectType, ulong @object, UIntPtr location, int messageCode, byte* pLayerPrefix, byte* pMessage, void* pUserData);

internal unsafe delegate void VkGetBufferMemoryRequirements2T(VkDevice device, VkBufferMemoryRequirementsInfo2* pInfo, VkMemoryRequirements2* pMemoryRequirements);

internal unsafe delegate void VkGetImageMemoryRequirements2T(VkDevice device, VkImageMemoryRequirementsInfo2* pInfo, VkMemoryRequirements2* pMemoryRequirements);

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
