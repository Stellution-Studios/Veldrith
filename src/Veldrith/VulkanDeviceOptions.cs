namespace Veldrith;

/// <summary>
/// A structure describing Vulkan-specific device creation options.
/// </summary>
public struct VulkanDeviceOptions {

    /// <summary>
    /// An array of required Vulkan instance extensions. Entries in this array will be enabled in the GraphicsDevice's
    /// </summary>
    public string[] InstanceExtensions;

    /// <summary>
    /// An array of required Vulkan device extensions. Entries in this array will be enabled in the GraphicsDevice's
    /// </summary>
    public string[] DeviceExtensions;

    /// <summary>
    /// Initializes a new instance of the <see cref="VulkanDeviceOptions" /> type.
    /// </summary>
    /// <param name="instanceExtensions">The instance extensions value used by this operation.</param>
    /// <param name="deviceExtensions">The device extensions value used by this operation.</param>
    public VulkanDeviceOptions(string[] instanceExtensions, string[] deviceExtensions) {
        this.InstanceExtensions = instanceExtensions;
        this.DeviceExtensions = deviceExtensions;
    }
}