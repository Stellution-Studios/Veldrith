using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLClearColor data structure used by the graphics runtime.
/// </summary>
public struct MTLClearColor {

    /// <summary>
    /// Stores the red state used by this instance.
    /// </summary>
    public double red;

    /// <summary>
    /// Stores the green state used by this instance.
    /// </summary>
    public double green;

    /// <summary>
    /// Stores the blue state used by this instance.
    /// </summary>
    public double blue;

    /// <summary>
    /// Stores the alpha state used by this instance.
    /// </summary>
    public double alpha;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLClearColor" /> type.
    /// </summary>
    /// <param name="r">The r value used by this operation.</param>
    /// <param name="g">The g value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    public MTLClearColor(double r, double g, double b, double a) {
        this.red = r;
        this.green = g;
        this.blue = b;
        this.alpha = a;
    }
}