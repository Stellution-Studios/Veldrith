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
    /// Initializes a new instance of the <see cref="CGSize" /> class.
    /// </summary>
    public CGSize(double width, double height) {
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Executes ToString.
    /// </summary>
    public override string ToString() {
        return string.Format("{0} x {1}", this.width, this.height);
    }
}