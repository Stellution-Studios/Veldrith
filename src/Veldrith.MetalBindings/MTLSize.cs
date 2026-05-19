using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLSize struct.
/// </summary>
public struct MTLSize {

    /// <summary>
    /// Stores the value associated with <c>Width</c>.
    /// </summary>
    public UIntPtr Width;

    /// <summary>
    /// Stores the value associated with <c>Height</c>.
    /// </summary>
    public UIntPtr Height;

    /// <summary>
    /// Stores the value associated with <c>Depth</c>.
    /// </summary>
    public UIntPtr Depth;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLSize" /> type.
    /// </summary>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    public MTLSize(uint width, uint height, uint depth) {
        this.Width = width;
        this.Height = height;
        this.Depth = depth;
    }
}