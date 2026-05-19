namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the Bool8 struct.
/// </summary>
public struct Bool8 {

    /// <summary>
    /// Represents the Value field.
    /// </summary>
    public readonly byte Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bool8" /> type.
    /// </summary>
    /// <param name="value">The value of value.</param>
    public Bool8(byte value) {
        this.Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Bool8" /> type.
    /// </summary>
    /// <param name="value">The value of value.</param>
    public Bool8(bool value) {
        this.Value = value ? (byte)1 : (byte)0;
    }

    /// <summary>
    /// Performs the operator bool operation.
    /// </summary>
    /// <param name="b">The value of b.</param>
    /// <returns>The result of the operator bool operation.</returns>
    public static implicit operator bool(Bool8 b) {
        return b.Value != 0;
    }

    /// <summary>
    /// Performs the operator byte operation.
    /// </summary>
    /// <param name="b">The value of b.</param>
    /// <returns>The result of the operator byte operation.</returns>
    public static implicit operator byte(Bool8 b) {
        return b.Value;
    }

    /// <summary>
    /// Performs the operator Bool8 operation.
    /// </summary>
    /// <param name="b">The value of b.</param>
    /// <returns>The result of the operator Bool8 operation.</returns>
    public static implicit operator Bool8(bool b) {
        return new Bool8(b);
    }
}