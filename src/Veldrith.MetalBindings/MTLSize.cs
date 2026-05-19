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
    /// Initializes a new instance of the <see cref="MTLSize" /> class.
    /// </summary>
    public MTLSize(uint width, uint height, uint depth) {
        this.Width = width;
        this.Height = height;
        this.Depth = depth;
    }
}