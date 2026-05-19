using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLClearColor struct.
/// </summary>
public struct MTLClearColor {

    /// <summary>
    /// Represents the red field.
    /// </summary>
    public double red;

    /// <summary>
    /// Represents the green field.
    /// </summary>
    public double green;

    /// <summary>
    /// Represents the blue field.
    /// </summary>
    public double blue;

    /// <summary>
    /// Represents the alpha field.
    /// </summary>
    public double alpha;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLClearColor" /> class.
    /// </summary>
    public MTLClearColor(double r, double g, double b, double a) {
        this.red = r;
        this.green = g;
        this.blue = b;
        this.alpha = a;
    }
}