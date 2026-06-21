namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CGPoint data structure used by the graphics runtime.
/// </summary>
public struct CGPoint {

    /// <summary>
    /// Stores the x state used by this instance.
    /// </summary>
    public CGFloat X;

    /// <summary>
    /// Stores the y state used by this instance.
    /// </summary>
    public CGFloat Y;

    /// <summary>
    /// Initializes a new instance of the <see cref="CGPoint" /> type.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public CGPoint(double x, double y) {
        this.X = x;
        this.Y = y;
    }

    /// <summary>
    /// Builds a string representation of this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override string ToString() {
        return string.Format("({0},{1})", this.X, this.Y);
    }
}