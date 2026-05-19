namespace Veldrith;

public readonly struct GraphicsApiVersion {
    public static GraphicsApiVersion Unknown => default;

    public int Major { get; }
    public int Minor { get; }
    public int Subminor { get; }
    public int Patch { get; }

    public bool IsKnown => this.Major != 0 && this.Minor != 0 && this.Subminor != 0 && this.Patch != 0;

    public GraphicsApiVersion(int major, int minor, int subminor, int patch) {
        this.Major = major;
        this.Minor = minor;
        this.Subminor = subminor;
        this.Patch = patch;
    }

    public override string ToString() {
        return $"{this.Major}.{this.Minor}.{this.Subminor}.{this.Patch}";
    }

    /// <summary>
    ///     Parses OpenGL version strings with either of following formats:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>major_number.minor_number</description>
    ///         </item>
    ///         <item>
    ///             <description>major_number.minor_number.release_number</description>
    ///         </item>
    ///     </list>
    /// </summary>
    /// <param name="versionString">The OpenGL version string.</param>
    /// <param name="version">The parsed <see cref="GraphicsApiVersion" />.</param>
    /// <returns>True whether the parse succeeded; otherwise false.</returns>
    public static bool TryParseGLVersion(string versionString, out GraphicsApiVersion version) {
        string[] versionParts = versionString.Split(' ')[0].Split('.');

        if (!int.TryParse(versionParts[0], out int major) ||
            !int.TryParse(versionParts[1], out int minor)) {
            version = default;
            return false;
        }

        int releaseNumber = 0;

        if (versionParts.Length == 3) {
            if (!int.TryParse(versionParts[2], out releaseNumber)) {
                version = default;
                return false;
            }
        }

        version = new GraphicsApiVersion(major, minor, 0, releaseNumber);
        return true;
    }
}