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
    /// Initializes a new instance of the <see cref="GraphicsDeviceOptions" /> type.
    /// </summary>
    /// <param name="debug">The value of debug.</param>
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
    /// Initializes a new instance of the <see cref="GraphicsDeviceOptions" /> type.
    /// </summary>
    /// <param name="debug">The value of debug.</param>
    /// <param name="swapchainDepthFormat">The value of swapchainDepthFormat.</param>
    /// <param name="syncToVerticalBlank">The value of syncToVerticalBlank.</param>
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
    /// Initializes a new instance of the <see cref="GraphicsDeviceOptions" /> type.
    /// </summary>
    /// <param name="debug">The value of debug.</param>
    /// <param name="swapchainDepthFormat">The value of swapchainDepthFormat.</param>
    /// <param name="syncToVerticalBlank">The value of syncToVerticalBlank.</param>
    /// <param name="resourceBindingModel">The value of resourceBindingModel.</param>
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
    /// Initializes a new instance of the <see cref="GraphicsDeviceOptions" /> type.
    /// </summary>
    /// <param name="debug">The value of debug.</param>
    /// <param name="swapchainDepthFormat">The value of swapchainDepthFormat.</param>
    /// <param name="syncToVerticalBlank">The value of syncToVerticalBlank.</param>
    /// <param name="resourceBindingModel">The value of resourceBindingModel.</param>
    /// <param name="preferDepthRangeZeroToOne">The value of preferDepthRangeZeroToOne.</param>
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
    /// Initializes a new instance of the <see cref="GraphicsDeviceOptions" /> type.
    /// </summary>
    /// <param name="debug">The value of debug.</param>
    /// <param name="swapchainDepthFormat">The value of swapchainDepthFormat.</param>
    /// <param name="syncToVerticalBlank">The value of syncToVerticalBlank.</param>
    /// <param name="resourceBindingModel">The value of resourceBindingModel.</param>
    /// <param name="preferDepthRangeZeroToOne">The value of preferDepthRangeZeroToOne.</param>
    /// <param name="preferStandardClipSpaceYDirection">The value of preferStandardClipSpaceYDirection.</param>
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
    /// Initializes a new instance of the <see cref="GraphicsDeviceOptions" /> type.
    /// </summary>
    /// <param name="debug">The value of debug.</param>
    /// <param name="swapchainDepthFormat">The value of swapchainDepthFormat.</param>
    /// <param name="syncToVerticalBlank">The value of syncToVerticalBlank.</param>
    /// <param name="resourceBindingModel">The value of resourceBindingModel.</param>
    /// <param name="preferDepthRangeZeroToOne">The value of preferDepthRangeZeroToOne.</param>
    /// <param name="preferStandardClipSpaceYDirection">The value of preferStandardClipSpaceYDirection.</param>
    /// <param name="swapchainSrgbFormat">The value of swapchainSrgbFormat.</param>
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