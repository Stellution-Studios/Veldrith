namespace Veldrith.D3D12;

/// <summary>
/// Defines the behavior and responsibilities of the D3D12ResourceLayout class.
/// </summary>
internal sealed class D3D12ResourceLayout : ResourceLayout {

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceLayout" /> class.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }
}
