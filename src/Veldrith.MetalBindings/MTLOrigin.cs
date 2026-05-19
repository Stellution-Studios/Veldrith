using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLOrigin data structure used by the graphics runtime.
/// </summary>
public struct MTLOrigin {

    /// <summary>
    /// Stores the x state used by this instance.
    /// </summary>
    public UIntPtr x;

    /// <summary>
    /// Stores the y state used by this instance.
    /// </summary>
    public UIntPtr y;

    /// <summary>
    /// Stores the z state used by this instance.
    /// </summary>
    public UIntPtr z;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLOrigin" /> type.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    public MTLOrigin(uint x, uint y, uint z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}