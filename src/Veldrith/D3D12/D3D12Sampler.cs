using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Represents the D3D12Sampler class.
/// </summary>
internal sealed class D3D12Sampler : Sampler {

    /// <summary>
    /// Represents the description field.
    /// </summary>
    private readonly SamplerDescription description;

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Represents the _descriptorHeap field.
    /// </summary>
    private ID3D12DescriptorHeap _descriptorHeap;

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Sampler" /> class.
    /// </summary>
    public D3D12Sampler(D3D12GraphicsDevice gd, ref SamplerDescription description) {
        this.gd = gd;
        this.description = description;
    }

    /// <summary>
    /// Represents the Description field.
    /// </summary>
    internal SamplerDescription Description => this.description;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Executes GetOrCreateDescriptor.
    /// </summary>
    internal CpuDescriptorHandle GetOrCreateDescriptor() {
        if (this._descriptorHeap == null) {
            this._descriptorHeap = this.gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.Sampler, 1));
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
            this.gd.Device.CreateSampler(ref samplerDescription, this._descriptorHeap.GetCPUDescriptorHandleForHeapStart());
        }

        return this._descriptorHeap.GetCPUDescriptorHandleForHeapStart();
    }

    /// <summary>
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        this._descriptorHeap?.Dispose();
        this._disposed = true;
    }
}