using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Represents the MtlSampler class.
/// </summary>
internal class MtlSampler : Sampler {

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlSampler" /> type.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <param name="gd">The value of gd.</param>
    public MtlSampler(ref SamplerDescription description, MtlGraphicsDevice gd) {
        MtlFormats.GetMinMagMipFilter(description.Filter, out MTLSamplerMinMagFilter min, out MTLSamplerMinMagFilter mag, out MTLSamplerMipFilter mip);

        MTLSamplerDescriptor mtlDesc = MTLSamplerDescriptor.New();
        mtlDesc.sAddressMode = MtlFormats.VdToMtlAddressMode(description.AddressModeU);
        mtlDesc.tAddressMode = MtlFormats.VdToMtlAddressMode(description.AddressModeV);
        mtlDesc.rAddressMode = MtlFormats.VdToMtlAddressMode(description.AddressModeW);
        mtlDesc.minFilter = min;
        mtlDesc.magFilter = mag;
        mtlDesc.mipFilter = mip;
        if (gd.MetalFeatures.IsMacOS) {
            mtlDesc.borderColor = MtlFormats.VdToMtlBorderColor(description.BorderColor);
        }

        if (description.ComparisonKind != null) {
            mtlDesc.compareFunction = MtlFormats.VdToMtlCompareFunction(description.ComparisonKind.Value);
        }

        mtlDesc.lodMinClamp = description.MinimumLod;
        mtlDesc.lodMaxClamp = description.MaximumLod;
        mtlDesc.maxAnisotropy = Math.Max(1, description.MaximumAnisotropy);
        this.DeviceSampler = gd.Device.newSamplerStateWithDescriptor(mtlDesc);
        ObjectiveCRuntime.release(mtlDesc.NativePtr);
    }

    /// <summary>
    /// Gets or sets DeviceSampler.
    /// </summary>
    public MTLSamplerState DeviceSampler { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Performs the Dispose operation.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            ObjectiveCRuntime.release(this.DeviceSampler.NativePtr);
        }
    }

    #endregion
}