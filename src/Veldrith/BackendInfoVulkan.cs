#if !EXCLUDE_VULKAN_BACKEND
using System;
using System.Collections.ObjectModel;
using Veldrith.Vk;
using Vulkan;

namespace Veldrith;

/// <summary>
///     Exposes Vulkan-specific functionality,
///     useful for interoperating with native components which interface directly with Vulkan.
///     Can only be used on <see cref="GraphicsBackend.Vulkan" />.
/// </summary>
public class BackendInfoVulkan {
    private readonly Lazy<ReadOnlyCollection<ExtensionProperties>> _deviceExtensions;
    private readonly Lazy<ReadOnlyCollection<string>> _instanceLayers;

    private readonly VkGraphicsDevice gd;

    internal BackendInfoVulkan(VkGraphicsDevice gd) {
        this.gd = gd;
        this._instanceLayers =
            new Lazy<ReadOnlyCollection<string>>(() =>
                new ReadOnlyCollection<string>(VulkanUtil.EnumerateInstanceLayers()));
        this.AvailableInstanceExtensions = new ReadOnlyCollection<string>(VulkanUtil.GetInstanceExtensions());
        this._deviceExtensions = new Lazy<ReadOnlyCollection<ExtensionProperties>>(this.EnumerateDeviceExtensions);
    }

    /// <summary>
    ///     Gets the underlying VkInstance used by the GraphicsDevice.
    /// </summary>
    public IntPtr Instance => this.gd.Instance.Handle;

    /// <summary>
    ///     Gets the underlying VkDevice used by the GraphicsDevice.
    /// </summary>
    public IntPtr Device => this.gd.Device.Handle;

    /// <summary>
    ///     Gets the underlying VkPhysicalDevice used by the GraphicsDevice.
    /// </summary>
    public IntPtr PhysicalDevice => this.gd.PhysicalDevice.Handle;

    /// <summary>
    ///     Gets the VkQueue which is used by the GraphicsDevice to submit graphics work.
    /// </summary>
    public IntPtr GraphicsQueue => this.gd.GraphicsQueue.Handle;

    /// <summary>
    ///     Gets the queue family index of the graphics VkQueue.
    /// </summary>
    public uint GraphicsQueueFamilyIndex => this.gd.GraphicsQueueIndex;

    /// <summary>
    ///     Gets the driver name of the device. May be null.
    /// </summary>
    public string DriverName => this.gd.DriverName;

    /// <summary>
    ///     Gets the driver information of the device. May be null.
    /// </summary>
    public string DriverInfo => this.gd.DriverInfo;

    public ReadOnlyCollection<string> AvailableInstanceLayers => this._instanceLayers.Value;

    public ReadOnlyCollection<string> AvailableInstanceExtensions { get; }

    public ReadOnlyCollection<ExtensionProperties> AvailableDeviceExtensions => this._deviceExtensions.Value;

    /// <summary>
    ///     Overrides the current VkImageLayout tracked by the given Texture. This should be used when a VkImage is created by
    ///     an external library to inform Veldrith about its initial layout.
    /// </summary>
    /// <param name="texture">The Texture whose currently-tracked VkImageLayout will be overridden.</param>
    /// <param name="layout">The new VkImageLayout value.</param>
    public void OverrideImageLayout(Texture texture, uint layout) {
        VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(texture);

        for (uint layer = 0; layer < vkTex.ArrayLayers; layer++) {
            for (uint level = 0; level < vkTex.MipLevels; level++) {
                vkTex.SetImageLayout(level, layer, (VkImageLayout)layout);
            }
        }
    }

    /// <summary>
    ///     Gets the underlying VkImage wrapped by the given Veldrith Texture. This method can not be used on Textures with
    ///     TextureUsage.Staging.
    /// </summary>
    /// <param name="texture">The Texture whose underlying VkImage will be returned.</param>
    /// <returns>The underlying VkImage for the given Texture.</returns>
    public ulong GetVkImage(Texture texture) {
        VkTexture vkTexture = Util.AssertSubtype<Texture, VkTexture>(texture);

        if ((vkTexture.Usage & TextureUsage.Staging) != 0) {
            throw new VeldridException(
                $"{nameof(this.GetVkImage)} cannot be used if the {nameof(Texture)} " +
                $"has {nameof(TextureUsage)}.{nameof(TextureUsage.Staging)}.");
        }

        return vkTexture.OptimalDeviceImage.Handle;
    }

    /// <summary>
    ///     Transitions the given Texture's underlying VkImage into a new layout.
    /// </summary>
    /// <param name="texture">The Texture whose underlying VkImage will be transitioned.</param>
    /// <param name="layout">The new VkImageLayout value.</param>
    public void TransitionImageLayout(Texture texture, uint layout) {
        this.gd.TransitionImageLayout(Util.AssertSubtype<Texture, VkTexture>(texture), (VkImageLayout)layout);
    }

    private unsafe ReadOnlyCollection<ExtensionProperties> EnumerateDeviceExtensions() {
        VkExtensionProperties[] vkProps = this.gd.GetDeviceExtensionProperties();
        ExtensionProperties[] veldridProps = new ExtensionProperties[vkProps.Length];

        for (int i = 0; i < vkProps.Length; i++) {
            VkExtensionProperties prop = vkProps[i];
            veldridProps[i] = new ExtensionProperties(Util.GetString(prop.extensionName), prop.specVersion);
        }

        return new ReadOnlyCollection<ExtensionProperties>(veldridProps);
    }

    public readonly struct ExtensionProperties {
        public readonly string Name;
        public readonly uint SpecVersion;

        public ExtensionProperties(string name, uint specVersion) {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.SpecVersion = specVersion;
        }

        public override string ToString() {
            return this.Name;
        }
    }
}
#endif