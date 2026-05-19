namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CGRect struct.
/// </summary>
public struct CGRect {

    /// <summary>
    /// Represents the origin field.
    /// </summary>
    public CGPoint origin;

    /// <summary>
    /// Represents the size field.
    /// </summary>
    public CGSize size;

    /// <summary>
    /// Initializes a new instance of the <see cref="CGRect" /> class.
    /// </summary>
    public CGRect(CGPoint origin, CGSize size) {
        this.origin = origin;
        this.size = size;
    }

    /// <summary>
    /// Executes ToString.
    /// </summary>
    public override string ToString() {
        return string.Format("{0}, {1}", this.origin, this.size);
    }
}