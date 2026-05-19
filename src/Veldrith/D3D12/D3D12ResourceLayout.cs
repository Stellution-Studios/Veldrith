namespace Veldrith.D3D12;

internal sealed class D3D12ResourceLayout : ResourceLayout {
    private bool _disposed;

    public D3D12ResourceLayout(ref ResourceLayoutDescription description)
        : base(ref description) {
        this.Elements = Util.ShallowClone(description.Elements);
    }

    internal ResourceLayoutElementDescription[] Elements { get; }

    public override bool IsDisposed => this._disposed;

    public override string Name { get; set; }

    public override void Dispose() {
        this._disposed = true;
    }
}