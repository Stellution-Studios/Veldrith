using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLClearColor data structure used by the graphics runtime.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MTLClearColor {

    /// <summary>
    /// Stores the Red state used by this instance.
    /// </summary>
    public double Red;

    /// <summary>
    /// Stores the Green state used by this instance.
    /// </summary>
    public double Green;

    /// <summary>
    /// Stores the Blue state used by this instance.
    /// </summary>
    public double Blue;

    /// <summary>
    /// Stores the Alpha state used by this instance.
    /// </summary>
    public double Alpha;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLClearColor" /> type.
    /// </summary>
    /// <param name="r">The r value used by this operation.</param>
    /// <param name="g">The g value used by this operation.</param>
    /// <param name="b">The b value used by this operation.</param>
    /// <param name="a">The a value used by this operation.</param>
    public MTLClearColor(double r, double g, double b, double a) {
        this.Red = r;
        this.Green = g;
        this.Blue = b;
        this.Alpha = a;
    }
}