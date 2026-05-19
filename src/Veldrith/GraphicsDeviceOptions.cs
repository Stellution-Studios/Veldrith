namespace Veldrith;

/// <summary>
/// A structure describing several common properties of a GraphicsDevice.
/// </summary>
public struct GraphicsDeviceOptions {

    /// <summary>
    /// Indicates whether the GraphicsDevice will support debug features, provided they are supported by the host system.
    /// </summary>
    public bool Debug;

    /// <summary>
    /// Indicates whether the Graphicsdevice will include a "main" Swapchain. If this value is true, then the
    /// GraphicsDevice
    /// must be created with one of the overloads that provides Swapchain source information.
    /// </summary>
    public bool HasMainSwapchain;

    /// <summary>
    /// An optional <see cref="PixelFormat" /> to be used for the depth buffer of the swapchain. If this value is null,
    /// then
    /// no depth buffer will be present on the swapchain.
    /// </summary>
    public PixelFormat? SwapchainDepthFormat;

    /// <summary>
    /// Indicates whether the main Swapchain will be synchronized to the window system's vertical refresh rate.
    /// </summary>
    public bool SyncToVerticalBlank;

    /// <summary>
    /// Specifies which model the rendering backend should use for binding resources. This can be overridden per-pipeline
    /// by specifying a value in <see cref="GraphicsPipelineDescription.ResourceBindingModel" />.
    /// </summary>
    public ResourceBindingModel ResourceBindingModel;

    /// <summary>
    /// Indicates whether a 0-to-1 depth range mapping is preferred.
    /// </summary>
    public bool PreferDepthRangeZeroToOne;

    /// <summary>
    /// Indicates whether a bottom-to-top-increasing clip space Y direction is preferred. For Vulkan, this is not the
    /// default, and may not be available on all systems.
    /// </summary>
    public bool PreferStandardClipSpaceYDirection;

    /// <summary>
    /// Indicates whether the main Swapchain should use an sRGB format. This value is only used in cases where the
    /// properties
    /// of the main SwapChain are not explicitly specified with a <see cref="SwapchainDescription" />. If they are, then
    /// the
    /// value of <see cref="SwapchainDescription.ColorSrgb" /> will supercede the value specified here.
    /// </summary>
    public bool SwapchainSrgbFormat;

    /// <summary>
    /// Constructs a new GraphicsDeviceOptions for a device with no main Swapchain.
    /// </summary>
    /// <param name="debug">
    /// Indicates whether the GraphicsDevice will support debug features, provided they are supported by
    /// the host system.
    /// </param>
    public GraphicsDeviceOptions(bool debug) {
        this.Debug = debug;
        this.HasMainSwapchain = false;
        this.SwapchainDepthFormat = null;
        this.SyncToVerticalBlank = false;
        this.ResourceBindingModel = ResourceBindingModel.Default;
        this.PreferDepthRangeZeroToOne = false;
        this.PreferStandardClipSpaceYDirection = false;
        this.SwapchainSrgbFormat = false;
    }

    /// <summary>
    /// Constructs a new GraphicsDeviceOptions for a device with a main Swapchain.
    /// </summary>
    /// <param name="debug">
    /// Indicates whether the GraphicsDevice will enable debug features, provided they are supported by
    /// the host system.
    /// </param>
    /// <param name="swapchainDepthFormat">
    /// An optional <see cref="PixelFormat" /> to be used for the depth buffer of the
    /// swapchain. If this value is null, then no depth buffer will be present on the swapchain.
    /// </param>
    /// <param name="syncToVerticalBlank">
    /// Indicates whether the main Swapchain will be synchronized to the window system's
    /// vertical refresh rate.
    /// </param>
    public GraphicsDeviceOptions(bool debug, PixelFormat? swapchainDepthFormat, bool syncToVerticalBlank) {
        this.Debug = debug;
        this.HasMainSwapchain = true;
        this.SwapchainDepthFormat = swapchainDepthFormat;
        this.SyncToVerticalBlank = syncToVerticalBlank;
        this.ResourceBindingModel = ResourceBindingModel.Default;
        this.PreferDepthRangeZeroToOne = false;
        this.PreferStandardClipSpaceYDirection = false;
        this.SwapchainSrgbFormat = false;
    }

    /// <summary>
    /// Constructs a new GraphicsDeviceOptions for a device with a main Swapchain.
    /// </summary>
    /// <param name="debug">
    /// Indicates whether the GraphicsDevice will enable debug features, provided they are supported by
    /// the host system.
    /// </param>
    /// <param name="swapchainDepthFormat">
    /// An optional <see cref="PixelFormat" /> to be used for the depth buffer of the
    /// swapchain. If this value is null, then no depth buffer will be present on the swapchain.
    /// </param>
    /// <param name="syncToVerticalBlank">
    /// Indicates whether the main Swapchain will be synchronized to the window system's
    /// vertical refresh rate.
    /// </param>
    /// <param name="resourceBindingModel">Specifies which model the rendering backend should use for binding resources.</param>
    public GraphicsDeviceOptions(bool debug, PixelFormat? swapchainDepthFormat, bool syncToVerticalBlank, ResourceBindingModel resourceBindingModel) {
        this.Debug = debug;
        this.HasMainSwapchain = true;
        this.SwapchainDepthFormat = swapchainDepthFormat;
        this.SyncToVerticalBlank = syncToVerticalBlank;
        this.ResourceBindingModel = resourceBindingModel;
        this.PreferDepthRangeZeroToOne = false;
        this.PreferStandardClipSpaceYDirection = false;
        this.SwapchainSrgbFormat = false;
    }

    /// <summary>
    /// Constructs a new GraphicsDeviceOptions for a device with a main Swapchain.
    /// </summary>
    /// <param name="debug">
    /// Indicates whether the GraphicsDevice will enable debug features, provided they are supported by
    /// the host system.
    /// </param>
    /// <param name="swapchainDepthFormat">
    /// An optional <see cref="PixelFormat" /> to be used for the depth buffer of the
    /// swapchain. If this value is null, then no depth buffer will be present on the swapchain.
    /// </param>
    /// <param name="syncToVerticalBlank">
    /// Indicates whether the main Swapchain will be synchronized to the window system's
    /// vertical refresh rate.
    /// </param>
    /// <param name="resourceBindingModel">Specifies which model the rendering backend should use for binding resources.</param>
    /// <param name="preferDepthRangeZeroToOne">
    /// Indicates whether a 0-to-1 depth range mapping is preferred.
    /// </param>
    public GraphicsDeviceOptions(bool debug, PixelFormat? swapchainDepthFormat, bool syncToVerticalBlank, ResourceBindingModel resourceBindingModel, bool preferDepthRangeZeroToOne) {
        this.Debug = debug;
        this.HasMainSwapchain = true;
        this.SwapchainDepthFormat = swapchainDepthFormat;
        this.SyncToVerticalBlank = syncToVerticalBlank;
        this.ResourceBindingModel = resourceBindingModel;
        this.PreferDepthRangeZeroToOne = preferDepthRangeZeroToOne;
        this.PreferStandardClipSpaceYDirection = false;
        this.SwapchainSrgbFormat = false;
    }

    /// <summary>
    /// Constructs a new GraphicsDeviceOptions for a device with a main Swapchain.
    /// </summary>
    /// <param name="debug">
    /// Indicates whether the GraphicsDevice will enable debug features, provided they are supported by
    /// the host system.
    /// </param>
    /// <param name="swapchainDepthFormat">
    /// An optional <see cref="PixelFormat" /> to be used for the depth buffer of the
    /// swapchain. If this value is null, then no depth buffer will be present on the swapchain.
    /// </param>
    /// <param name="syncToVerticalBlank">
    /// Indicates whether the main Swapchain will be synchronized to the window system's
    /// vertical refresh rate.
    /// </param>
    /// <param name="resourceBindingModel">Specifies which model the rendering backend should use for binding resources.</param>
    /// <param name="preferDepthRangeZeroToOne">
    /// Indicates whether a 0-to-1 depth range mapping is preferred.
    /// </param>
    /// <param name="preferStandardClipSpaceYDirection">
    /// Indicates whether a bottom-to-top-increasing clip space Y direction
    /// is preferred. For Vulkan, this is not the default, and is not available on all systems.
    /// </param>
    public GraphicsDeviceOptions(bool debug, PixelFormat? swapchainDepthFormat, bool syncToVerticalBlank, ResourceBindingModel resourceBindingModel, bool preferDepthRangeZeroToOne, bool preferStandardClipSpaceYDirection) {
        this.Debug = debug;
        this.HasMainSwapchain = true;
        this.SwapchainDepthFormat = swapchainDepthFormat;
        this.SyncToVerticalBlank = syncToVerticalBlank;
        this.ResourceBindingModel = resourceBindingModel;
        this.PreferDepthRangeZeroToOne = preferDepthRangeZeroToOne;
        this.PreferStandardClipSpaceYDirection = preferStandardClipSpaceYDirection;
        this.SwapchainSrgbFormat = false;
    }

    /// <summary>
    /// Constructs a new GraphicsDeviceOptions for a device with a main Swapchain.
    /// </summary>
    /// <param name="debug">
    /// Indicates whether the GraphicsDevice will enable debug features, provided they are supported by
    /// the host system.
    /// </param>
    /// <param name="swapchainDepthFormat">
    /// An optional <see cref="PixelFormat" /> to be used for the depth buffer of the
    /// swapchain. If this value is null, then no depth buffer will be present on the swapchain.
    /// </param>
    /// <param name="syncToVerticalBlank">
    /// Indicates whether the main Swapchain will be synchronized to the window system's
    /// vertical refresh rate.
    /// </param>
    /// <param name="resourceBindingModel">Specifies which model the rendering backend should use for binding resources.</param>
    /// <param name="preferDepthRangeZeroToOne">
    /// Indicates whether a 0-to-1 depth range mapping is preferred.
    /// </param>
    /// <param name="preferStandardClipSpaceYDirection">
    /// Indicates whether a bottom-to-top-increasing clip space Y direction
    /// is preferred. For Vulkan, this is not the default, and is not available on all systems.
    /// </param>
    /// <param name="swapchainSrgbFormat">
    /// Indicates whether the main Swapchain should use an sRGB format. This value is only
    /// used in cases where the properties of the main SwapChain are not explicitly specified with a
    /// <see cref="SwapchainDescription" />. If they are, then the value of <see cref="SwapchainDescription.ColorSrgb" />
    /// will
    /// supercede the value specified here.
    /// </param>

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsDeviceOptions" /> class.
    /// </summary>
    public GraphicsDeviceOptions(bool debug, PixelFormat? swapchainDepthFormat, bool syncToVerticalBlank, ResourceBindingModel resourceBindingModel, bool preferDepthRangeZeroToOne, bool preferStandardClipSpaceYDirection, bool swapchainSrgbFormat) {
        this.Debug = debug;
        this.HasMainSwapchain = true;
        this.SwapchainDepthFormat = swapchainDepthFormat;
        this.SyncToVerticalBlank = syncToVerticalBlank;
        this.ResourceBindingModel = resourceBindingModel;
        this.PreferDepthRangeZeroToOne = preferDepthRangeZeroToOne;
        this.PreferStandardClipSpaceYDirection = preferStandardClipSpaceYDirection;
        this.SwapchainSrgbFormat = swapchainSrgbFormat;
    }
}

