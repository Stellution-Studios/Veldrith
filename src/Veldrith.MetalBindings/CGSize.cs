namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CGSize data structure used by the graphics runtime.
/// </summary>
public struct CGSize {

    /// <summary>
    /// Stores the width value used during command execution.
    /// </summary>
    public double width;

    /// <summary>
    /// Stores the height value used during command execution.
    /// </summary>
    public double height;

    /// <summary>
    /// Initializes a new instance of the <see cref="CGSize" /> type.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    public CGSize(double width, double height) {
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Builds a string representation of this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override string ToString() {
        return string.Format("{0} x {1}", this.width, this.height);
    }
}