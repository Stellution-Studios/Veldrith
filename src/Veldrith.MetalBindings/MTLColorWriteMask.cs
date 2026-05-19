using System;

namespace Veldrith.MetalBindings;

[Flags]

/// <summary>
/// Defines the available values of the MTLColorWriteMask enumeration.
/// </summary>
public enum MTLColorWriteMask {

    /// <summary>
    /// Stores the value associated with <c>None</c>.
    /// </summary>
    None = 0, Red = 1 << 3, Green = 1 << 2, Blue = 1 << 1, Alpha = 1 << 0, All = Red | Green | Blue | Alpha
}