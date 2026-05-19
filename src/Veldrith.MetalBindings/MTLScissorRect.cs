using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLScissorRect struct.
/// </summary>
public struct MTLScissorRect : IEquatable<MTLScissorRect> {

    /// <summary>
    /// Stores the value associated with <c>x</c>.
    /// </summary>
    public UIntPtr x;

    /// <summary>
    /// Stores the value associated with <c>y</c>.
    /// </summary>
    public UIntPtr y;

    /// <summary>
    /// Stores the value associated with <c>width</c>.
    /// </summary>
    public UIntPtr width;

    /// <summary>
    /// Stores the value associated with <c>height</c>.
    /// </summary>
    public UIntPtr height;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLScissorRect" /> type.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    public MTLScissorRect(uint x, uint y, uint width, uint height) {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(MTLScissorRect other) {
        return this.x == other.x
               && this.y == other.y
               && this.width == other.width
               && this.height == other.height;
    }
}