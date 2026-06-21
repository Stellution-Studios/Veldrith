using Vortice.Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkSampler.
/// </summary>
internal unsafe class VkSampler : Sampler {

    /// <summary>
    /// Stores the sampler state used by this instance.
    /// </summary>
    private readonly global::Vortice.Vulkan.VkSampler _sampler;

    /// <summary>
    /// Stores the graphics device used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkSampler" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public VkSampler(VkGraphicsDevice gd, ref SamplerDescription description) {
        this._gd = gd;
        VkFormats.GetFilterParams(description.Filter, out VkFilter minFilter, out VkFilter magFilter, out VkSamplerMipmapMode mipmapMode);

        VkSamplerCreateInfo samplerCi = new() {
            sType = VkStructureType.SamplerCreateInfo,
            addressModeU = VkFormats.VdToVkSamplerAddressMode(description.AddressModeU),
            addressModeV = VkFormats.VdToVkSamplerAddressMode(description.AddressModeV),
            addressModeW = VkFormats.VdToVkSamplerAddressMode(description.AddressModeW),
            minFilter = minFilter,
            magFilter = magFilter,
            mipmapMode = mipmapMode,
            compareEnable = description.ComparisonKind != null,
            compareOp = description.ComparisonKind != null
                ? VkFormats.VdToVkCompareOp(description.ComparisonKind.Value)
                : VkCompareOp.Never,
            anisotropyEnable = description.Filter == SamplerFilter.Anisotropic,
            maxAnisotropy = description.MaximumAnisotropy,
            minLod = description.MinimumLod,
            maxLod = description.MaximumLod,
            mipLodBias = description.LodBias,
            borderColor = VkFormats.VdToVkSamplerBorderColor(description.BorderColor)
        };

        this._gd.DeviceApi.vkCreateSampler(ref samplerCi, null, out this._sampler);
        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    /// <summary>
    /// Stores the device sampler state used by this instance.
    /// </summary>
    public global::Vortice.Vulkan.VkSampler DeviceSampler => this._sampler;

    /// <summary>
    /// Gets or sets RefCount.
    /// </summary>
    public ResourceRefCount RefCount { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this._gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private void DisposeCore() {
        if (!this._disposed) {
            this._gd.DeviceApi.vkDestroySampler(this._sampler, null);
            this._disposed = true;
        }
    }
}