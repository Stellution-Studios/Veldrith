namespace Veldrith.MetalBindings;

// TODO: Technically this should be "pointer-sized",
// but there are no non-64-bit platforms that anyone cares about.

/// <summary>
/// Represents the CGFloat struct.
/// </summary>
public struct CGFloat {

    /// <summary>
    /// Initializes a new instance of the <see cref="CGFloat" /> type.
    /// </summary>
    /// <param name="value">The value of value.</param>
    public CGFloat(double value) {
        this.Value = value;
    }

    /// <summary>
    /// Gets or sets Value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Performs the operator CGFloat operation.
    /// </summary>
    /// <param name="value">The value of value.</param>
    /// <returns>The result of the operator CGFloat operation.</returns>
    public static implicit operator CGFloat(double value) {
        return new CGFloat(value);
    }

    /// <summary>
    /// Performs the operator double operation.
    /// </summary>
    /// <param name="cgf">The value of cgf.</param>
    /// <returns>The result of the operator double operation.</returns>
    public static implicit operator double(CGFloat cgf) {
        return cgf.Value;
    }

    /// <summary>
    /// Performs the ToString operation.
    /// </summary>
    /// <returns>The result of the ToString operation.</returns>
    public override string ToString() {
        return this.Value.ToString();
    }
}