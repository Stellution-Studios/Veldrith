namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the CGSize struct.
/// </summary>
public struct CGSize {

    /// <summary>
    /// Stores the value associated with <c>width</c>.
    /// </summary>
    public double width;

    /// <summary>
    /// Stores the value associated with <c>height</c>.
    /// </summary>
    public double height;

    /// <summary>
    /// Initializes a new instance of the <see cref="CGSize" /> type.
    /// </summary>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    public CGSize(double width, double height) {
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Executes the ToString operation.
    /// </summary>
    /// <returns>Returns the result produced by the ToString operation.</returns>
    public override string ToString() {
        return string.Format("{0} x {1}", this.width, this.height);
    }
}