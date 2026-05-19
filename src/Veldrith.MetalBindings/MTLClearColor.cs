using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]
public struct MTLClearColor {
    public double red;
    public double green;
    public double blue;
    public double alpha;

    public MTLClearColor(double r, double g, double b, double a) {
        this.red = r;
        this.green = g;
        this.blue = b;
        this.alpha = a;
    }
}