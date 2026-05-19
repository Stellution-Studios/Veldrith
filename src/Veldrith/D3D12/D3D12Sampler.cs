using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12Sampler.
/// </summary>
internal sealed class D3D12Sampler : Sampler {

    /// <summary>
    /// Stores the description state used by this instance.
    /// </summary>
    private readonly SamplerDescription description;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Stores the descriptor heap state used by this instance.
    /// </summary>
    private ID3D12DescriptorHeap _descriptorHeap;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Sampler" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12Sampler(D3D12GraphicsDevice gd, ref SamplerDescription description) {
        this.gd = gd;
        this.description = description;
    }

    /// <summary>
    /// Stores the description state used by this instance.
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
    /// Gets the or create descriptor value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
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
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this._descriptorHeap?.Dispose();
        this._disposed = true;
    }
}