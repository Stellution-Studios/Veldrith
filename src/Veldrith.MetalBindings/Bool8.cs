namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the Bool8 data structure used by the graphics runtime.
/// </summary>
public struct Bool8 {

    /// <summary>
    /// Stores the value state used by this instance.
    /// </summary>
    public readonly byte Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bool8" /> type.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    public Bool8(byte value) {
        this.Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Bool8" /> type.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    public Bool8(bool value) {
        this.Value = value ? (byte)1 : (byte)0;
    }

    /// <summary>
    /// Executes the bool logic for this backend.
    /// </summary>
    /// <param name="b">The b value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator bool(Bool8 b) {
        return b.Value != 0;
    }

    /// <summary>
    /// Executes the byte logic for this backend.
    /// </summary>
    /// <param name="b">The b value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator byte(Bool8 b) {
        return b.Value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Bool8" /> class.
    /// </summary>
    /// <param name="b">The b value used by this operation.</param>
    public static implicit operator Bool8(bool b) {
        return new Bool8(b);
    }
}