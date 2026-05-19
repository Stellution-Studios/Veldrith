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
    /// Initializes a new instance of the <see cref="SamplerDescription" /> type.
    /// </summary>
    /// <param name="addressModeU">Specifies the value of <paramref name="addressModeU" />.</param>
    /// <param name="addressModeV">Specifies the value of <paramref name="addressModeV" />.</param>
    /// <param name="addressModeW">Specifies the value of <paramref name="addressModeW" />.</param>
    /// <param name="filter">Specifies the value of <paramref name="filter" />.</param>
    /// <param name="comparisonKind">Specifies the value of <paramref name="comparisonKind" />.</param>
    /// <param name="maximumAnisotropy">Specifies the value of <paramref name="maximumAnisotropy" />.</param>
    /// <param name="minimumLod">Specifies the value of <paramref name="minimumLod" />.</param>
    /// <param name="maximumLod">Specifies the value of <paramref name="maximumLod" />.</param>
    /// <param name="lodBias">Specifies the value of <paramref name="lodBias" />.</param>
    /// <param name="borderColor">Specifies the value of <paramref name="borderColor" />.</param>
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
    /// Defines the predefined value exposed by <c>POINT</c>.
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
    /// Defines the predefined value exposed by <c>LINEAR</c>.
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
    /// Defines the predefined value exposed by <c>ANISO4_X</c>.
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
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
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
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine((int)this.AddressModeU, (int)this.AddressModeV, (int)this.AddressModeW, (int)this.Filter, this.ComparisonKind.GetHashCode(), this.MaximumAnisotropy.GetHashCode(), this.MinimumLod.GetHashCode(), this.MaximumLod.GetHashCode(), this.LodBias.GetHashCode(), (int)this.BorderColor);
    }
}
