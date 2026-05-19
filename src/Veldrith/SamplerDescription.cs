using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="Sampler" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct SamplerDescription : IEquatable<SamplerDescription> {

    /// <summary>
    /// The <see cref="SamplerAddressMode" /> mode to use for the U (or S) coordinate.
    /// </summary>
    public SamplerAddressMode AddressModeU;

    /// <summary>
    /// The <see cref="SamplerAddressMode" /> mode to use for the V (or T) coordinate.
    /// </summary>
    public SamplerAddressMode AddressModeV;

    /// <summary>
    /// The <see cref="SamplerAddressMode" /> mode to use for the W (or R) coordinate.
    /// </summary>
    public SamplerAddressMode AddressModeW;

    /// <summary>
    /// The filter used when sampling.
    /// </summary>
    public SamplerFilter Filter;

    /// <summary>
    /// An optional value controlling the kind of comparison to use when sampling. If null, comparison sampling is not
    /// used.
    /// </summary>
    public ComparisonKind? ComparisonKind;

    /// <summary>
    /// The maximum anisotropy of the filter, when <see cref="SamplerFilter.Anisotropic" /> is used, or otherwise ignored.
    /// </summary>
    public uint MaximumAnisotropy;

    /// <summary>
    /// The minimum level of detail.
    /// </summary>
    public uint MinimumLod;

    /// <summary>
    /// The maximum level of detail.
    /// </summary>
    public uint MaximumLod;

    /// <summary>
    /// The level of detail bias.
    /// </summary>
    public int LodBias;

    /// <summary>
    /// The constant color that is sampled when <see cref="SamplerAddressMode.Border" /> is used, or otherwise ignored.
    /// </summary>
    public SamplerBorderColor BorderColor;

    /// <summary>
    /// Constructs a new SamplerDescription.
    /// </summary>
    /// <param name="addressModeU">The <see cref="SamplerAddressMode" /> mode to use for the U (or R) coordinate.</param>
    /// <param name="addressModeV">The <see cref="SamplerAddressMode" /> mode to use for the V (or S) coordinate.</param>
    /// <param name="addressModeW">The <see cref="SamplerAddressMode" /> mode to use for the W (or T) coordinate.</param>
    /// <param name="filter">The filter used when sampling.</param>
    /// <param name="comparisonKind">
    /// An optional value controlling the kind of comparison to use when sampling. If null,
    /// comparison sampling is not used.
    /// </param>
    /// <param name="maximumAnisotropy">
    /// The maximum anisotropy of the filter, when <see cref="SamplerFilter.Anisotropic" /> is
    /// used, or otherwise ignored.
    /// </param>
    /// <param name="minimumLod">The minimum level of detail.</param>
    /// <param name="maximumLod">The maximum level of detail.</param>
    /// <param name="lodBias">The level of detail bias.</param>
    /// <param name="borderColor">
    /// The constant color that is sampled when <see cref="SamplerAddressMode.Border" /> is used, or
    /// otherwise ignored.
    /// </param>
    public SamplerDescription(SamplerAddressMode addressModeU, SamplerAddressMode addressModeV, SamplerAddressMode addressModeW, SamplerFilter filter, ComparisonKind? comparisonKind, uint maximumAnisotropy, uint minimumLod, uint maximumLod, int lodBias, SamplerBorderColor borderColor) {
        this.AddressModeU = addressModeU;
        this.AddressModeV = addressModeV;
        this.AddressModeW = addressModeW;
        this.Filter = filter;
        this.ComparisonKind = comparisonKind;
        this.MaximumAnisotropy = maximumAnisotropy;
        this.MinimumLod = minimumLod;
        this.MaximumLod = maximumLod;
        this.LodBias = lodBias;
        this.BorderColor = borderColor;
    }

    /// <summary>
    /// Describes a common point-filter sampler, with wrapping address mode.
    /// Settings:
    /// AddressModeU = SamplerAddressMode.Wrap
    /// AddressModeV = SamplerAddressMode.Wrap
    /// AddressModeW = SamplerAddressMode.Wrap
    /// Filter = SamplerFilter.MinPoint_MagPoint_MipPoint
    /// LodBias = 0
    /// MinimumLod = 0
    /// MaximumLod = uint.MaxValue
    /// MaximumAnisotropy = 0
    /// </summary>
    public static readonly SamplerDescription POINT = new() {
        AddressModeU = SamplerAddressMode.Wrap,
        AddressModeV = SamplerAddressMode.Wrap,
        AddressModeW = SamplerAddressMode.Wrap,
        Filter = SamplerFilter.MinPointMagPointMipPoint,
        LodBias = 0,
        MinimumLod = 0,
        MaximumLod = uint.MaxValue,
        MaximumAnisotropy = 0
    };

    /// <summary>
    /// Describes a common linear-filter sampler, with wrapping address mode.
    /// Settings:
    /// AddressModeU = SamplerAddressMode.Wrap
    /// AddressModeV = SamplerAddressMode.Wrap
    /// AddressModeW = SamplerAddressMode.Wrap
    /// Filter = SamplerFilter.MinLinear_MagLinear_MipLinear
    /// LodBias = 0
    /// MinimumLod = 0
    /// MaximumLod = uint.MaxValue
    /// MaximumAnisotropy = 0
    /// </summary>
    public static readonly SamplerDescription LINEAR = new() {
        AddressModeU = SamplerAddressMode.Wrap,
        AddressModeV = SamplerAddressMode.Wrap,
        AddressModeW = SamplerAddressMode.Wrap,
        Filter = SamplerFilter.MinLinearMagLinearMipLinear,
        LodBias = 0,
        MinimumLod = 0,
        MaximumLod = uint.MaxValue,
        MaximumAnisotropy = 0
    };

    /// <summary>
    /// Describes a common 4x-anisotropic-filter sampler, with wrapping address mode.
    /// Settings:
    /// AddressModeU = SamplerAddressMode.Wrap
    /// AddressModeV = SamplerAddressMode.Wrap
    /// AddressModeW = SamplerAddressMode.Wrap
    /// Filter = SamplerFilter.Anisotropic
    /// LodBias = 0
    /// MinimumLod = 0
    /// MaximumLod = uint.MaxValue
    /// MaximumAnisotropy = 4
    /// </summary>
    public static readonly SamplerDescription ANISO4_X = new() {
        AddressModeU = SamplerAddressMode.Wrap,
        AddressModeV = SamplerAddressMode.Wrap,
        AddressModeW = SamplerAddressMode.Wrap,
        Filter = SamplerFilter.Anisotropic,
        LodBias = 0,
        MinimumLod = 0,
        MaximumLod = uint.MaxValue,
        MaximumAnisotropy = 4
    };

    /// <summary>
    /// Element-wise equality.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>True if all elements are equal; false otherswise.</returns>
    public bool Equals(SamplerDescription other) {
        return this.AddressModeU == other.AddressModeU
               && this.AddressModeV == other.AddressModeV
               && this.AddressModeW == other.AddressModeW
               && this.Filter == other.Filter
               && this.ComparisonKind.GetValueOrDefault() == other.ComparisonKind.GetValueOrDefault()
               && this.MaximumAnisotropy == other.MaximumAnisotropy
               && this.MinimumLod == other.MinimumLod
               && this.MaximumLod == other.MaximumLod
               && this.LodBias == other.LodBias
               && this.BorderColor == other.BorderColor;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine((int)this.AddressModeU, (int)this.AddressModeV, (int)this.AddressModeW, (int)this.Filter, this.ComparisonKind.GetHashCode(), this.MaximumAnisotropy.GetHashCode(), this.MinimumLod.GetHashCode(), this.MaximumLod.GetHashCode(), this.LodBias.GetHashCode(), (int)this.BorderColor);
    }
}