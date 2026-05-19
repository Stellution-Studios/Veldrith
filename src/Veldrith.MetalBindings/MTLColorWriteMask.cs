using System;

namespace Veldrith.MetalBindings;

[Flags]

/// <summary>
/// Represents the MTLColorWriteMask enum.
/// </summary>
public enum MTLColorWriteMask {

    /// <summary>
    /// Represents the None field.
    /// </summary>
    None = 0, Red = 1 << 3, Green = 1 << 2, Blue = 1 << 1, Alpha = 1 << 0, All = Red | Green | Blue | Alpha
}