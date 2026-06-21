namespace Veldrith.MetalBindings;

// TODO: Technically this should be "pointer-sized",
// but there are no non-64-bit platforms that anyone cares about.

/// <summary>
/// Represents the CGFloat data structure used by the graphics runtime.
/// </summary>
public struct CGFloat {

    /// <summary>
    /// Initializes a new instance of the <see cref="CGFloat" /> type.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    public CGFloat(double value) {
        this.Value = value;
    }

    /// <summary>
    /// Gets or sets Value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CGFloat" /> class.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    public static implicit operator CGFloat(double value) {
        return new CGFloat(value);
    }

    /// <summary>
    /// Executes the double logic for this backend.
    /// </summary>
    /// <param name="cgf">The cgf value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator double(CGFloat cgf) {
        return cgf.Value;
    }

    /// <summary>
    /// Builds a string representation of this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override string ToString() {
        return this.Value.ToString();
    }
}