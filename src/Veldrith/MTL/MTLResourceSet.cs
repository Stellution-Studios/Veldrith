namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlResourceSet.
/// </summary>
internal class MtlResourceSet : ResourceSet {

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlResourceSet" /> class.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="gd">The graphics device that owns this operation.</param>
    public MtlResourceSet(ref ResourceSetDescription description, MtlGraphicsDevice gd) : base(ref description) {
        this.Resources = Util.ShallowClone(description.BoundResources);
        this.Layout = Util.AssertSubtype<ResourceLayout, MtlResourceLayout>(description.Layout);
    }

    /// <summary>
    /// Gets or sets Resources.
    /// </summary>
    public new IBindableResource[] Resources { get; }

    /// <summary>
    /// Gets or sets Layout.
    /// </summary>
    public new MtlResourceLayout Layout { get; }

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
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }

    #endregion
}