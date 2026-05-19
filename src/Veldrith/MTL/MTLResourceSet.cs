namespace Veldrith.MTL;

internal class MtlResourceSet : ResourceSet {
    private bool _disposed;

    public MtlResourceSet(ref ResourceSetDescription description, MtlGraphicsDevice gd)
        : base(ref description) {
        this.Resources = Util.ShallowClone(description.BoundResources);
        this.Layout = Util.AssertSubtype<ResourceLayout, MtlResourceLayout>(description.Layout);
    }

    public new IBindableResource[] Resources { get; }
    public new MtlResourceLayout Layout { get; }

    public override bool IsDisposed => this._disposed;

    public override string Name { get; set; }

    #region Disposal

    public override void Dispose() {
        this._disposed = true;
    }

    #endregion
}