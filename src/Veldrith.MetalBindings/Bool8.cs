namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the Bool8 struct.
/// </summary>
public struct Bool8 {

    /// <summary>
    /// Stores the value associated with <c>Value</c>.
    /// </summary>
    public readonly byte Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bool8" /> type.
    /// </summary>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public Bool8(byte value) {
        this.Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Bool8" /> type.
    /// </summary>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    public Bool8(bool value) {
        this.Value = value ? (byte)1 : (byte)0;
    }

    /// <summary>
    /// Executes the operator bool operation.
    /// </summary>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <returns>Returns the result produced by the operator bool operation.</returns>
    public static implicit operator bool(Bool8 b) {
        return b.Value != 0;
    }

    /// <summary>
    /// Executes the operator byte operation.
    /// </summary>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <returns>Returns the result produced by the operator byte operation.</returns>
    public static implicit operator byte(Bool8 b) {
        return b.Value;
    }

    /// <summary>
    /// Executes the operator Bool8 operation.
    /// </summary>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <returns>Returns the result produced by the operator Bool8 operation.</returns>
    public static implicit operator Bool8(bool b) {
        return new Bool8(b);
    }
}