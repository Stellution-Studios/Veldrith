namespace Veldrith.MTL;

/// <summary>
/// Represents the MtlResourceSet class.
/// </summary>
internal class MtlResourceSet : ResourceSet {

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlResourceSet" /> class.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <param name="gd">The value of gd.</param>
    /// <returns>The result of the base operation.</returns>
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
    /// Performs the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }

    #endregion
}
