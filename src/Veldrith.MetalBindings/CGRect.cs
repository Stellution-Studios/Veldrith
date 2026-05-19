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
    /// Initializes a new instance of the <see cref="CGRect" /> type.
    /// </summary>
    /// <param name="origin">The value of origin.</param>
    /// <param name="size">The value of size.</param>
    public CGRect(CGPoint origin, CGSize size) {
        this.origin = origin;
        this.size = size;
    }

    /// <summary>
    /// Performs the ToString operation.
    /// </summary>
    /// <returns>The result of the ToString operation.</returns>
    public override string ToString() {
        return string.Format("{0}, {1}", this.origin, this.size);
    }
}