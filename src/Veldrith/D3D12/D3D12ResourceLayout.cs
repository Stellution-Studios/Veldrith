namespace Veldrith.D3D12;

/// <summary>
/// Represents the D3D12ResourceLayout class.
/// </summary>
internal sealed class D3D12ResourceLayout : ResourceLayout {

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceLayout" /> class.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the base operation.</returns>
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
    /// Performs the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }
}
