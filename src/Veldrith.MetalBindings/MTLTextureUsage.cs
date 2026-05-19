using System;

namespace Veldrith.MetalBindings;

[Flags]

/// <summary>
/// Defines the available values of the MTLTextureUsage enumeration.
/// </summary>
public enum MTLTextureUsage {

    /// <summary>
    /// Stores the value associated with <c>Unknown</c>.
    /// </summary>
    Unknown = 0, ShaderRead = 1 << 0, ShaderWrite = 1 << 1, RenderTarget = 1 << 2, PixelFormatView = 0x10
}