namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLSamplerAddressMode enumeration.
/// </summary>
public enum MTLSamplerAddressMode {

    /// <summary>
    /// Stores the value associated with <c>ClampToEdge</c>.
    /// </summary>
    ClampToEdge = 0, MirrorClampToEdge = 1, Repeat = 2, MirrorRepeat = 3, ClampToZero = 4, ClampToBorderColor = 5
}