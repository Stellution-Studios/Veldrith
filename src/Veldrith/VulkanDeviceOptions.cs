namespace Veldrith;

/// <summary>
/// A structure describing Vulkan-specific device creation options.
/// </summary>
public struct VulkanDeviceOptions {

    /// <summary>
    /// An array of required Vulkan instance extensions. Entries in this array will be enabled in the GraphicsDevice's
    /// created VkInstance.
    /// </summary>
    public string[] InstanceExtensions;

    /// <summary>
    /// An array of required Vulkan device extensions. Entries in this array will be enabled in the GraphicsDevice's
    /// created VkDevice.
    /// </summary>
    public string[] DeviceExtensions;

    /// <summary>
    /// Initializes a new instance of the <see cref="VulkanDeviceOptions" /> type.
    /// </summary>
    /// <param name="instanceExtensions">The value of instanceExtensions.</param>
    /// <param name="deviceExtensions">The value of deviceExtensions.</param>
    public VulkanDeviceOptions(string[] instanceExtensions, string[] deviceExtensions) {
        this.InstanceExtensions = instanceExtensions;
        this.DeviceExtensions = deviceExtensions;
    }
}