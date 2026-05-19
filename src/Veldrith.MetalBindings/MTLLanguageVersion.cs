namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLLanguageVersion enum.
/// </summary>
public enum MTLLanguageVersion : uint {

    /// <summary>
    /// Represents the Version1_0 field.
    /// </summary>
    Version1_0 = 1 << 16, Version1_1 = (1 << 16) + 1, Version1_2 = (1 << 16) + 2
}