#if !EXCLUDE_VULKAN_BACKEND
using System;
using System.Collections.ObjectModel;
using Veldrith.Vk;
using Vulkan;

namespace Veldrith;

/// <summary>
/// Represents the BackendInfoVulkan class.
/// </summary>
public class BackendInfoVulkan {

    /// <summary>
    /// Represents the _deviceExtensions field.
    /// </summary>
    private readonly Lazy<ReadOnlyCollection<ExtensionProperties>> _deviceExtensions;

    /// <summary>
    /// Represents the _instanceLayers field.
    /// </summary>
    private readonly Lazy<ReadOnlyCollection<string>> _instanceLayers;

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendInfoVulkan" /> type.
    /// </summary>
    /// <param name="gd">The value of gd.</param>
    internal BackendInfoVulkan(VkGraphicsDevice gd) {
        this.gd = gd;
        this._instanceLayers = new Lazy<ReadOnlyCollection<string>>(() =>
                new ReadOnlyCollection<string>(VulkanUtil.EnumerateInstanceLayers()));
        this.AvailableInstanceExtensions = new ReadOnlyCollection<string>(VulkanUtil.GetInstanceExtensions());
        this._deviceExtensions = new Lazy<ReadOnlyCollection<ExtensionProperties>>(this.EnumerateDeviceExtensions);
    }

    /// <summary>
    /// Gets the underlying VkInstance used by the GraphicsDevice.
    /// </summary>
    public IntPtr Instance => this.gd.Instance.Handle;

    /// <summary>
    /// Gets the underlying VkDevice used by the GraphicsDevice.
    /// </summary>
    public IntPtr Device => this.gd.Device.Handle;

    /// <summary>
    /// Gets the underlying VkPhysicalDevice used by the GraphicsDevice.
    /// </summary>
    public IntPtr PhysicalDevice => this.gd.PhysicalDevice.Handle;

    /// <summary>
    /// Gets the VkQueue which is used by the GraphicsDevice to submit graphics work.
    /// </summary>
    public IntPtr GraphicsQueue => this.gd.GraphicsQueue.Handle;

    /// <summary>
    /// Gets the queue family index of the graphics VkQueue.
    /// </summary>
    public uint GraphicsQueueFamilyIndex => this.gd.GraphicsQueueIndex;

    /// <summary>
    /// Gets the driver name of the device. May be null.
    /// </summary>
    public string DriverName => this.gd.DriverName;

    /// <summary>
    /// Gets the driver information of the device. May be null.
    /// </summary>
    public string DriverInfo => this.gd.DriverInfo;

    /// <summary>
    /// Represents the AvailableInstanceLayers field.
    /// </summary>
    public ReadOnlyCollection<string> AvailableInstanceLayers => this._instanceLayers.Value;

    /// <summary>
    /// Gets or sets AvailableInstanceExtensions.
    /// </summary>
    public ReadOnlyCollection<string> AvailableInstanceExtensions { get; }

    /// <summary>
    /// Represents the AvailableDeviceExtensions field.
    /// </summary>
    public ReadOnlyCollection<ExtensionProperties> AvailableDeviceExtensions => this._deviceExtensions.Value;

    /// <summary>
    /// Performs the OverrideImageLayout operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="layout">The value of layout.</param>
    public void OverrideImageLayout(Texture texture, uint layout) {
        VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(texture);

        for (uint layer = 0; layer < vkTex.ArrayLayers; layer++) {
            for (uint level = 0; level < vkTex.MipLevels; level++) {
                vkTex.SetImageLayout(level, layer, (VkImageLayout)layout);
            }
        }
    }

    /// <summary>
    /// Performs the GetVkImage operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <returns>The result of the GetVkImage operation.</returns>
    public ulong GetVkImage(Texture texture) {
        VkTexture vkTexture = Util.AssertSubtype<Texture, VkTexture>(texture);

        if ((vkTexture.Usage & TextureUsage.Staging) != 0) {
            throw new VeldridException($"{nameof(this.GetVkImage)} cannot be used if the {nameof(Texture)} " + $"has {nameof(TextureUsage)}.{nameof(TextureUsage.Staging)}.");
        }

        return vkTexture.OptimalDeviceImage.Handle;
    }

    /// <summary>
    /// Performs the TransitionImageLayout operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="layout">The value of layout.</param>
    public void TransitionImageLayout(Texture texture, uint layout) {
        this.gd.TransitionImageLayout(Util.AssertSubtype<Texture, VkTexture>(texture), (VkImageLayout)layout);
    }

    /// <summary>
    /// Performs the EnumerateDeviceExtensions operation.
    /// </summary>
    /// <returns>The result of the EnumerateDeviceExtensions operation.</returns>
    private unsafe ReadOnlyCollection<ExtensionProperties> EnumerateDeviceExtensions() {
        VkExtensionProperties[] vkProps = this.gd.GetDeviceExtensionProperties();
        ExtensionProperties[] veldridProps = new ExtensionProperties[vkProps.Length];

        for (int i = 0; i < vkProps.Length; i++) {
            VkExtensionProperties prop = vkProps[i];
            veldridProps[i] = new ExtensionProperties(Util.GetString(prop.extensionName), prop.specVersion);
        }

        return new ReadOnlyCollection<ExtensionProperties>(veldridProps);
    }

    /// <summary>
    /// Represents the ExtensionProperties struct.
    /// </summary>
    public readonly struct ExtensionProperties {

        /// <summary>
        /// Represents the Name field.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Represents the SpecVersion field.
        /// </summary>
        public readonly uint SpecVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionProperties" /> type.
        /// </summary>
        /// <param name="name">The value of name.</param>
        /// <param name="specVersion">The value of specVersion.</param>
        public ExtensionProperties(string name, uint specVersion) {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.SpecVersion = specVersion;
        }

        /// <summary>
        /// Performs the ToString operation.
        /// </summary>
        /// <returns>The result of the ToString operation.</returns>
        public override string ToString() {
            return this.Name;
        }
    }
}
#endif