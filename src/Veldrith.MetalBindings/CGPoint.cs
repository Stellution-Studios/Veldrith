namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CGPoint data structure used by the graphics runtime.
/// </summary>
public struct CGPoint {

    /// <summary>
    /// Stores the x state used by this instance.
    /// </summary>
    public CGFloat x;

    /// <summary>
    /// Stores the y state used by this instance.
    /// </summary>
    public CGFloat y;

    /// <summary>
    /// Initializes a new instance of the <see cref="CGPoint" /> type.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public CGPoint(double x, double y) {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// Builds a string representation of this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override string ToString() {
        return string.Format("({0},{1})", this.x, this.y);
    }
}