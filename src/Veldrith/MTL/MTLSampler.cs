using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal class MtlSampler : Sampler
    {
        public MTLSamplerState DeviceSampler { get; }

        public override bool IsDisposed => this._disposed;

        public override string Name { get; set; }
        private bool _disposed;

        public MtlSampler(ref SamplerDescription description, MtlGraphicsDevice gd)
        {
            MtlFormats.GetMinMagMipFilter(
                description.Filter,
                out var min,
                out var mag,
                out var mip);

            var mtlDesc = MTLSamplerDescriptor.New();
            mtlDesc.sAddressMode = MtlFormats.VdToMtlAddressMode(description.AddressModeU);
            mtlDesc.tAddressMode = MtlFormats.VdToMtlAddressMode(description.AddressModeV);
            mtlDesc.rAddressMode = MtlFormats.VdToMtlAddressMode(description.AddressModeW);
            mtlDesc.minFilter = min;
            mtlDesc.magFilter = mag;
            mtlDesc.mipFilter = mip;
            if (gd.MetalFeatures.IsMacOS) mtlDesc.borderColor = MtlFormats.VdToMtlBorderColor(description.BorderColor);

            if (description.ComparisonKind != null) mtlDesc.compareFunction = MtlFormats.VdToMtlCompareFunction(description.ComparisonKind.Value);
            mtlDesc.lodMinClamp = description.MinimumLod;
            mtlDesc.lodMaxClamp = description.MaximumLod;
            mtlDesc.maxAnisotropy = Math.Max(1, description.MaximumAnisotropy);
            DeviceSampler = gd.Device.newSamplerStateWithDescriptor(mtlDesc);
            ObjectiveCRuntime.release(mtlDesc.NativePtr);
        }

        #region Disposal

        public override void Dispose()
        {
            if (!this._disposed)
            {
                this._disposed = true;
                ObjectiveCRuntime.release(DeviceSampler.NativePtr);
            }
        }

        #endregion
    }
}
