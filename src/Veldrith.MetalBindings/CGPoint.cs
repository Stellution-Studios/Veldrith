namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the CGPoint struct.
/// </summary>
public struct CGPoint {

    /// <summary>
    /// Stores the value associated with <c>x</c>.
    /// </summary>
    public CGFloat x;

    /// <summary>
    /// Stores the value associated with <c>y</c>.
    /// </summary>
    public CGFloat y;

    /// <summary>
    /// Initializes a new instance of the <see cref="CGPoint" /> type.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    public CGPoint(double x, double y) {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// Executes the ToString operation.
    /// </summary>
    /// <returns>Returns the result produced by the ToString operation.</returns>
    public override string ToString() {
        return string.Format("({0},{1})", this.x, this.y);
    }
}