namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CGPoint struct.
/// </summary>
public struct CGPoint {

    /// <summary>
    /// Represents the x field.
    /// </summary>
    public CGFloat x;

    /// <summary>
    /// Represents the y field.
    /// </summary>
    public CGFloat y;

    /// <summary>
    /// Initializes a new instance of the <see cref="CGPoint" /> class.
    /// </summary>
    public CGPoint(double x, double y) {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// Executes ToString.
    /// </summary>
    public override string ToString() {
        return string.Format("({0},{1})", this.x, this.y);
    }
}