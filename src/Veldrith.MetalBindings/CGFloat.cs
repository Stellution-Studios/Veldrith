namespace Veldrith.MetalBindings;

// TODO: Technically this should be "pointer-sized",
// but there are no non-64-bit platforms that anyone cares about.

/// <summary>
/// Represents the CGFloat struct.
/// </summary>
public struct CGFloat {

    /// <summary>
    /// Initializes a new instance of the <see cref="CGFloat" /> class.
    /// </summary>
    public CGFloat(double value) {
        this.Value = value;
    }

    /// <summary>
    /// Gets or sets Value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Executes CGFloat.
    /// </summary>
    public static implicit operator CGFloat(double value) {
        return new CGFloat(value);
    }

    /// <summary>
    /// Executes double.
    /// </summary>
    public static implicit operator double(CGFloat cgf) {
        return cgf.Value;
    }

    /// <summary>
    /// Executes ToString.
    /// </summary>
    public override string ToString() {
        return this.Value.ToString();
    }
}