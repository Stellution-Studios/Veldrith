namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CGSize data structure used by the graphics runtime.
/// </summary>
public struct CGSize {

    /// <summary>
    /// Stores the width value used during command execution.
    /// </summary>
    public double Width;

    /// <summary>
    /// Stores the height value used during command execution.
    /// </summary>
    public double Height;

    /// <summary>
    /// Initializes a new instance of the <see cref="CGSize" /> type.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    public CGSize(double width, double height) {
        this.Width = width;
        this.Height = height;
    }

    /// <summary>
    /// Builds a string representation of this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override string ToString() {
        return $"{this.Width} x {this.Height}";
    }
}