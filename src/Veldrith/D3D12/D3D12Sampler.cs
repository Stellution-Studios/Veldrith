using Vortice.Direct3D12;

namespace Veldrith.D3D12
{
    internal sealed class D3D12Sampler : Sampler
    {
        private readonly D3D12GraphicsDevice gd;
        private readonly SamplerDescription description;
        private ID3D12DescriptorHeap _descriptorHeap;
        private bool _disposed;
        private string _name;

        public D3D12Sampler(D3D12GraphicsDevice gd, ref SamplerDescription description)
        {
            this.gd = gd;
            this.description = description;
        }

        internal SamplerDescription Description => description;
        internal CpuDescriptorHandle GetOrCreateDescriptor()
        {
            if (this._descriptorHeap == null)
            {
                this._descriptorHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(
                    DescriptorHeapType.Sampler,
                    1,
                    DescriptorHeapFlags.None));
                bool comparison = description.ComparisonKind != null;
                ComparisonFunction comparisonFunction = description.ComparisonKind != null
                    ? D3D12Formats.ToComparison(description.ComparisonKind.Value)
                    : ComparisonFunction.Always;

                var samplerDescription = new Vortice.Direct3D12.SamplerDescription
                {
                    Filter = D3D12Formats.ToFilter(description.Filter, comparison),
                    AddressU = D3D12Formats.ToTextureAddressMode(description.AddressModeU),
                    AddressV = D3D12Formats.ToTextureAddressMode(description.AddressModeV),
                    AddressW = D3D12Formats.ToTextureAddressMode(description.AddressModeW),
                    MipLODBias = description.LodBias,
                    MaxAnisotropy = description.MaximumAnisotropy == 0 ? 1u : description.MaximumAnisotropy,
                    ComparisonFunction = comparisonFunction,
                    BorderColor = D3D12Formats.ToBorderColor(description.BorderColor),
                    MinLOD = description.MinimumLod,
                    MaxLOD = description.MaximumLod
                };
                gd.Device.CreateSampler(ref samplerDescription, this._descriptorHeap.GetCPUDescriptorHandleForHeapStart());
            }

            return this._descriptorHeap.GetCPUDescriptorHandleForHeapStart();
        }

        public override bool IsDisposed => this._disposed;

        public override string Name
        {
            get => this._name;
            set => this._name = value;
        }

        public override void Dispose()
        {
            this._descriptorHeap?.Dispose();
            this._disposed = true;
        }
    }
}
