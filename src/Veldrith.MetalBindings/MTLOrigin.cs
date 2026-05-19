using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLOrigin struct.
/// </summary>
public struct MTLOrigin {

    /// <summary>
    /// Represents the x field.
    /// </summary>
    public UIntPtr x;

    /// <summary>
    /// Represents the y field.
    /// </summary>
    public UIntPtr y;

    /// <summary>
    /// Represents the z field.
    /// </summary>
    public UIntPtr z;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLOrigin" /> type.
    /// </summary>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="z">The value of z.</param>
    public MTLOrigin(uint x, uint y, uint z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}