using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLScissorRect struct.
/// </summary>
public struct MTLScissorRect : IEquatable<MTLScissorRect> {

    /// <summary>
    /// Represents the x field.
    /// </summary>
    public UIntPtr x;

    /// <summary>
    /// Represents the y field.
    /// </summary>
    public UIntPtr y;

    /// <summary>
    /// Represents the width field.
    /// </summary>
    public UIntPtr width;

    /// <summary>
    /// Represents the height field.
    /// </summary>
    public UIntPtr height;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLScissorRect" /> type.
    /// </summary>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    public MTLScissorRect(uint x, uint y, uint width, uint height) {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(MTLScissorRect other) {
        return this.x == other.x
               && this.y == other.y
               && this.width == other.width
               && this.height == other.height;
    }
}