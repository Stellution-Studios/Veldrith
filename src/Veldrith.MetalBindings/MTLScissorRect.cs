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
    /// Initializes a new instance of the <see cref="MTLScissorRect" /> class.
    /// </summary>
    public MTLScissorRect(uint x, uint y, uint width, uint height) {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Executes Equals.
    /// </summary>
    public bool Equals(MTLScissorRect other) {
        return this.x == other.x
               && this.y == other.y
               && this.width == other.width
               && this.height == other.height;
    }
}