namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the CGRect struct.
/// </summary>
public struct CGRect {

    /// <summary>
    /// Stores the value associated with <c>origin</c>.
    /// </summary>
    public CGPoint origin;

    /// <summary>
    /// Stores the value associated with <c>size</c>.
    /// </summary>
    public CGSize size;

    /// <summary>
    /// Initializes a new instance of the <see cref="CGRect" /> type.
    /// </summary>
    /// <param name="origin">Specifies the value of <paramref name="origin" />.</param>
    /// <param name="size">Specifies the value of <paramref name="size" />.</param>
    public CGRect(CGPoint origin, CGSize size) {
        this.origin = origin;
        this.size = size;
    }

    /// <summary>
    /// Executes the ToString operation.
    /// </summary>
    /// <returns>Returns the result produced by the ToString operation.</returns>
    public override string ToString() {
        return string.Format("{0}, {1}", this.origin, this.size);
    }
}