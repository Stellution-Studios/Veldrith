namespace Veldrith.D3D12;

internal sealed class D3D12ResourceLayout : ResourceLayout {

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceLayout" /> class.
    /// </summary>
    public D3D12ResourceLayout(ref ResourceLayoutDescription description)
        : base(ref description) {
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
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }
}