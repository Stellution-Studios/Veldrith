namespace Veldrith;

/// <summary>
/// A structure describing Metal-specific device creation options.
/// </summary>
public struct MetalDeviceOptions {

    /// <summary>
    /// Indicates whether the depth/stencil attachments of a framebuffer are preferred not to be stored in system memory.
    /// This only affects Apple GPUs.
    /// </summary>
    public bool PreferMemorylessDepthTargets;
}