namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12ResourceLayout.
/// </summary>
internal sealed class D3D12ResourceLayout : ResourceLayout {

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceLayout" /> class.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12ResourceLayout(ref ResourceLayoutDescription description) : base(ref description) {
        this.Elements = Util.ShallowClone(description.Elements);
    }

    /// <summary>
    /// Gets or sets Elements.
    /// </summary>
    internal ResourceLayoutElementDescription[] Elements { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }
}