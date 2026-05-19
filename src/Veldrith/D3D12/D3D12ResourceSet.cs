namespace Veldrith.D3D12;

internal sealed class D3D12ResourceSet : ResourceSet {
    private bool _disposed;

    public D3D12ResourceSet(ref ResourceSetDescription description)
        : base(ref description) {
        this.ResourceLayoutInfo = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(description.Layout);
        this.BoundResources = Util.ShallowClone(description.BoundResources);
    }

    internal D3D12ResourceLayout ResourceLayoutInfo { get; }

    internal IBindableResource[] BoundResources { get; }

    public override bool IsDisposed => this._disposed;

    public override string Name { get; set; }

    public override void Dispose() {
        this._disposed = true;
    }
}