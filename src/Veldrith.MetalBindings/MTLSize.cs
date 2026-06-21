using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLSize data structure used by the graphics runtime.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MTLSize {

    /// <summary>
    /// Stores the width value used during command execution.
    /// </summary>
    public UIntPtr Width;

    /// <summary>
    /// Stores the height value used during command execution.
    /// </summary>
    public UIntPtr Height;

    /// <summary>
    /// Stores the depth value used during command execution.
    /// </summary>
    public UIntPtr Depth;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLSize" /> type.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    public MTLSize(uint width, uint height, uint depth) {
        this.Width = width;
        this.Height = height;
        this.Depth = depth;
    }
}