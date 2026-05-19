using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Defines the behavior and responsibilities of the D3D12Sampler class.
/// </summary>
internal sealed class D3D12Sampler : Sampler {

    /// <summary>
    /// Stores the value associated with <c>description</c>.
    /// </summary>
    private readonly SamplerDescription description;

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_descriptorHeap</c>.
    /// </summary>
    private ID3D12DescriptorHeap _descriptorHeap;

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Sampler" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    public D3D12Sampler(D3D12GraphicsDevice gd, ref SamplerDescription description) {
        this.gd = gd;
        this.description = description;
    }

    /// <summary>
    /// Stores the value associated with <c>Description</c>.
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
    /// Executes the GetOrCreateDescriptor operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetOrCreateDescriptor operation.</returns>
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this._descriptorHeap?.Dispose();
        this._disposed = true;
    }
}