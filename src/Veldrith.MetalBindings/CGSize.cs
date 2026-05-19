namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CGSize struct.
/// </summary>
public struct CGSize {

    /// <summary>
    /// Represents the width field.
    /// </summary>
    public double width;

    /// <summary>
    /// Represents the height field.
    /// </summary>
    public double height;

    /// <summary>
    /// Initializes a new instance of the <see cref="CGSize" /> type.
    /// </summary>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    public CGSize(double width, double height) {
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Performs the ToString operation.
    /// </summary>
    /// <returns>The result of the ToString operation.</returns>
    public override string ToString() {
        return string.Format("{0} x {1}", this.width, this.height);
    }
}