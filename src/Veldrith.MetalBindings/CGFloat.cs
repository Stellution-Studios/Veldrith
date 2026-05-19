namespace Veldrith.MetalBindings;

// TODO: Technically this should be "pointer-sized",
// but there are no non-64-bit platforms that anyone cares about.

/// <summary>
/// Defines the data layout and behavior of the CGFloat struct.
/// </summary>
public struct CGFloat {

    /// <summary>
    /// Initializes a new instance of the <see cref="CGFloat" /> type.
    /// </summary>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public CGFloat(double value) {
        this.Value = value;
    }

    /// <summary>
    /// Gets or sets Value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Executes the operator CGFloat operation.
    /// </summary>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    /// <returns>Returns the result produced by the operator CGFloat operation.</returns>
    public static implicit operator CGFloat(double value) {
        return new CGFloat(value);
    }

    /// <summary>
    /// Executes the operator double operation.
    /// </summary>
    /// <param name="cgf">Specifies the value of <paramref name="cgf" />.</param>
    /// <returns>Returns the result produced by the operator double operation.</returns>
    public static implicit operator double(CGFloat cgf) {
        return cgf.Value;
    }

    /// <summary>
    /// Executes the ToString operation.
    /// </summary>
    /// <returns>Returns the result produced by the ToString operation.</returns>
    public override string ToString() {
        return this.Value.ToString();
    }
}