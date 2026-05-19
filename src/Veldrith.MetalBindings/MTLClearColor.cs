using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLClearColor struct.
/// </summary>
public struct MTLClearColor {

    /// <summary>
    /// Stores the value associated with <c>red</c>.
    /// </summary>
    public double red;

    /// <summary>
    /// Stores the value associated with <c>green</c>.
    /// </summary>
    public double green;

    /// <summary>
    /// Stores the value associated with <c>blue</c>.
    /// </summary>
    public double blue;

    /// <summary>
    /// Stores the value associated with <c>alpha</c>.
    /// </summary>
    public double alpha;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLClearColor" /> type.
    /// </summary>
    /// <param name="r">Specifies the value of <paramref name="r" />.</param>
    /// <param name="g">Specifies the value of <paramref name="g" />.</param>
    /// <param name="b">Specifies the value of <paramref name="b" />.</param>
    /// <param name="a">Specifies the value of <paramref name="a" />.</param>
    public MTLClearColor(double r, double g, double b, double a) {
        this.red = r;
        this.green = g;
        this.blue = b;
        this.alpha = a;
    }
}