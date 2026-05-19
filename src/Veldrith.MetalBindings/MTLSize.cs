using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLSize struct.
/// </summary>
public struct MTLSize {

    /// <summary>
    /// Represents the Width field.
    /// </summary>
    public UIntPtr Width;

    /// <summary>
    /// Represents the Height field.
    /// </summary>
    public UIntPtr Height;

    /// <summary>
    /// Represents the Depth field.
    /// </summary>
    public UIntPtr Depth;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLSize" /> type.
    /// </summary>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    public MTLSize(uint width, uint height, uint depth) {
        this.Width = width;
        this.Height = height;
        this.Depth = depth;
    }
}