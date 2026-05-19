namespace Veldrith.MTL;

/// <summary>
/// Defines the behavior and responsibilities of the MtlResourceSet class.
/// </summary>
internal class MtlResourceSet : ResourceSet {

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlResourceSet" /> class.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }

    #endregion
}
