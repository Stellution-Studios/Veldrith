using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

internal unsafe class VkSampler : Sampler {
    private readonly Vulkan.VkSampler _sampler;

    private readonly VkGraphicsDevice gd;
    private bool _disposed;
    private string _name;

    public VkSampler(VkGraphicsDevice gd, ref SamplerDescription description) {
        this.gd = gd;
        VkFormats.GetFilterParams(description.Filter, out VkFilter minFilter, out VkFilter magFilter,
            out VkSamplerMipmapMode mipmapMode);

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

    public Vulkan.VkSampler DeviceSampler => this._sampler;

    public ResourceRefCount RefCount { get; }

    public override bool IsDisposed => this._disposed;

    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    private void DisposeCore() {
        if (!this._disposed) {
            vkDestroySampler(this.gd.Device, this._sampler, null);
            this._disposed = true;
        }
    }
}