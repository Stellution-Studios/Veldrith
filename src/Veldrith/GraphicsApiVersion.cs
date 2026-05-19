namespace Veldrith;

/// <summary>
/// Represents the GraphicsApiVersion struct.
/// </summary>
public readonly struct GraphicsApiVersion {

    /// <summary>
    /// Gets or sets Unknown.
    /// </summary>
    public static GraphicsApiVersion Unknown => default;

    /// <summary>
    /// Gets or sets Major.
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Gets or sets Minor.
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Gets or sets Subminor.
    /// </summary>
    public int Subminor { get; }

    /// <summary>
    /// Gets or sets Patch.
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// Gets or sets IsKnown.
    /// </summary>
    public bool IsKnown => this.Major != 0 && this.Minor != 0 && this.Subminor != 0 && this.Patch != 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsApiVersion" /> type.
    /// </summary>
    /// <param name="major">The value of major.</param>
    /// <param name="minor">The value of minor.</param>
    /// <param name="subminor">The value of subminor.</param>
    /// <param name="patch">The value of patch.</param>
    public GraphicsApiVersion(int major, int minor, int subminor, int patch) {
        this.Major = major;
        this.Minor = minor;
        this.Subminor = subminor;
        this.Patch = patch;
    }

    /// <summary>
    /// Performs the ToString operation.
    /// </summary>
    /// <returns>The result of the ToString operation.</returns>
    public override string ToString() {
        return $"{this.Major}.{this.Minor}.{this.Subminor}.{this.Patch}";
    }

}