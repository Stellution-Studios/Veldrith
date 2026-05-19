namespace Veldrith.D3D12;

/// <summary>
/// Represents the D3D12ResourceSet class.
/// </summary>
internal sealed class D3D12ResourceSet : ResourceSet {

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceSet" /> class.
    /// </summary>
    public D3D12ResourceSet(ref ResourceSetDescription description)

        /// <summary>
        /// Executes base.
        /// </summary>
        : base(ref description) {
        this.ResourceLayoutInfo = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(description.Layout);
        this.BoundResources = Util.ShallowClone(description.BoundResources);
    }

    /// <summary>
    /// Gets or sets ResourceLayoutInfo.
    /// </summary>
    internal D3D12ResourceLayout ResourceLayoutInfo { get; }

    /// <summary>
    /// Gets or sets BoundResources.
    /// </summary>
    internal IBindableResource[] BoundResources { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }
}