using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Defines the behavior and responsibilities of the VkSampler class.
/// </summary>
internal unsafe class VkSampler : Sampler {

    /// <summary>
    /// Stores the value associated with <c>_sampler</c>.
    /// </summary>
    private readonly Vulkan.VkSampler _sampler;

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the value associated with <c>_name</c>.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkSampler" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    public VkSampler(VkGraphicsDevice gd, ref SamplerDescription description) {
        this.gd = gd;
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

        vkCreateSampler(this.gd.Device, ref samplerCi, null, out this._sampler);
        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    /// <summary>
    /// Stores the value associated with <c>DeviceSampler</c>.
    /// </summary>
    public Vulkan.VkSampler DeviceSampler => this._sampler;

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
            this.gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes the DisposeCore operation.
    /// </summary>
    private void DisposeCore() {
        if (!this._disposed) {
            vkDestroySampler(this.gd.Device, this._sampler, null);
            this._disposed = true;
        }
    }
}