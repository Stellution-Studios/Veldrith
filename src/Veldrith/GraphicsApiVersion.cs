namespace Veldrith;

/// <summary>
/// Represents the GraphicsApiVersion data structure used by the graphics runtime.
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
    /// <param name="major">The major value used by this operation.</param>
    /// <param name="minor">The minor value used by this operation.</param>
    /// <param name="subminor">The subminor value used by this operation.</param>
    /// <param name="patch">The patch value used by this operation.</param>
    public GraphicsApiVersion(int major, int minor, int subminor, int patch) {
        this.Major = major;
        this.Minor = minor;
        this.Subminor = subminor;
        this.Patch = patch;
    }

    /// <summary>
    /// Builds a string representation of this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override string ToString() {
        return $"{this.Major}.{this.Minor}.{this.Subminor}.{this.Patch}";
    }

}