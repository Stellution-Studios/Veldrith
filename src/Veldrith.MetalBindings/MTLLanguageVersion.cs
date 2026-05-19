namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLLanguageVersion enumeration.
/// </summary>
public enum MTLLanguageVersion : uint {

    /// <summary>
    /// Stores the value associated with <c>Version1_0</c>.
    /// </summary>
    Version1_0 = 1 << 16, Version1_1 = (1 << 16) + 1, Version1_2 = (1 << 16) + 2
}