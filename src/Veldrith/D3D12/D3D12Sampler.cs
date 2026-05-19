using Vortice.Direct3D12;

namespace Veldrith.D3D12;

internal sealed class D3D12Sampler : Sampler {
    private readonly SamplerDescription description;
    private readonly D3D12GraphicsDevice gd;
    private ID3D12DescriptorHeap _descriptorHeap;
    private bool _disposed;

    public D3D12Sampler(D3D12GraphicsDevice gd, ref SamplerDescription description) {
        this.gd = gd;
        this.description = description;
    }

    internal SamplerDescription Description => this.description;

    public override bool IsDisposed => this._disposed;

    public override string Name { get; set; }

    internal CpuDescriptorHandle GetOrCreateDescriptor() {
        if (this._descriptorHeap == null) {
            this._descriptorHeap = this.gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(
                DescriptorHeapType.Sampler,
                1));
            bool comparison = this.description.ComparisonKind != null;
            ComparisonFunction comparisonFunction = this.description.ComparisonKind != null
                ? D3D12Formats.ToComparison(this.description.ComparisonKind.Value)
                : ComparisonFunction.Always;

            Vortice.Direct3D12.SamplerDescription samplerDescription = new() {
                Filter = D3D12Formats.ToFilter(this.description.Filter, comparison),
                AddressU = D3D12Formats.ToTextureAddressMode(this.description.AddressModeU),
                AddressV = D3D12Formats.ToTextureAddressMode(this.description.AddressModeV),
                AddressW = D3D12Formats.ToTextureAddressMode(this.description.AddressModeW),
                MipLODBias = this.description.LodBias,
                MaxAnisotropy = this.description.MaximumAnisotropy == 0 ? 1u : this.description.MaximumAnisotropy,
                ComparisonFunction = comparisonFunction,
                BorderColor = D3D12Formats.ToBorderColor(this.description.BorderColor),
                MinLOD = this.description.MinimumLod,
                MaxLOD = this.description.MaximumLod
            };
            this.gd.Device.CreateSampler(ref samplerDescription,
                this._descriptorHeap.GetCPUDescriptorHandleForHeapStart());
        }

        return this._descriptorHeap.GetCPUDescriptorHandleForHeapStart();
    }

    public override void Dispose() {
        this._descriptorHeap?.Dispose();
        this._disposed = true;
    }
}