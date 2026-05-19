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
    /// Initializes a new instance of the <see cref="Bool8" /> class.
    /// </summary>
    public Bool8(byte value) {
        this.Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Bool8" /> class.
    /// </summary>
    public Bool8(bool value) {
        this.Value = value ? (byte)1 : (byte)0;
    }

    /// <summary>
    /// Executes bool.
    /// </summary>
    public static implicit operator bool(Bool8 b) {
        return b.Value != 0;
    }

    /// <summary>
    /// Executes byte.
    /// </summary>
    public static implicit operator byte(Bool8 b) {
        return b.Value;
    }

    /// <summary>
    /// Executes Bool8.
    /// </summary>
    public static implicit operator Bool8(bool b) {
        return new Bool8(b);
    }
}