using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLScissorRect data structure used by the graphics runtime.
/// </summary>
public struct MTLScissorRect : IEquatable<MTLScissorRect> {

    /// <summary>
    /// Stores the x state used by this instance.
    /// </summary>
    public UIntPtr x;

    /// <summary>
    /// Stores the y state used by this instance.
    /// </summary>
    public UIntPtr y;

    /// <summary>
    /// Stores the width value used during command execution.
    /// </summary>
    public UIntPtr width;

    /// <summary>
    /// Stores the height value used during command execution.
    /// </summary>
    public UIntPtr height;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLScissorRect" /> type.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    public MTLScissorRect(uint x, uint y, uint width, uint height) {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(MTLScissorRect other) {
        return this.x == other.x
               && this.y == other.y
               && this.width == other.width
               && this.height == other.height;
    }
}