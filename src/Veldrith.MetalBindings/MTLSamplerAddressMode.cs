namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLSamplerAddressMode enumeration.
/// </summary>
public enum MTLSamplerAddressMode {

    /// <summary>
    /// Stores the clamp to edge state used by this instance.
    /// </summary>
    ClampToEdge = 0, MirrorClampToEdge = 1, Repeat = 2, MirrorRepeat = 3, ClampToZero = 4, ClampToBorderColor = 5
}