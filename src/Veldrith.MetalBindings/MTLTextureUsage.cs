using System;

namespace Veldrith.MetalBindings;

[Flags]

/// <summary>
/// Represents the MTLTextureUsage enum.
/// </summary>
public enum MTLTextureUsage {

    /// <summary>
    /// Represents the Unknown field.
    /// </summary>
    Unknown = 0, ShaderRead = 1 << 0, ShaderWrite = 1 << 1, RenderTarget = 1 << 2, PixelFormatView = 0x10
}