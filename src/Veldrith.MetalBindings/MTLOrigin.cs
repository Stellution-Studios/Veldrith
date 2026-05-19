using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLOrigin struct.
/// </summary>
public struct MTLOrigin {

    /// <summary>
    /// Stores the value associated with <c>x</c>.
    /// </summary>
    public UIntPtr x;

    /// <summary>
    /// Stores the value associated with <c>y</c>.
    /// </summary>
    public UIntPtr y;

    /// <summary>
    /// Stores the value associated with <c>z</c>.
    /// </summary>
    public UIntPtr z;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLOrigin" /> type.
    /// </summary>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="z">Specifies the value of <paramref name="z" />.</param>
    public MTLOrigin(uint x, uint y, uint z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}