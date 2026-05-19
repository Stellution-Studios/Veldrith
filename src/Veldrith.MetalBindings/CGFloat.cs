namespace Veldrith.MetalBindings;

// TODO: Technically this should be "pointer-sized",
// but there are no non-64-bit platforms that anyone cares about.
public struct CGFloat {
    public CGFloat(double value) {
        this.Value = value;
    }

    public double Value { get; }

    public static implicit operator CGFloat(double value) {
        return new CGFloat(value);
    }

    public static implicit operator double(CGFloat cgf) {
        return cgf.Value;
    }

    public override string ToString() {
        return this.Value.ToString();
    }
}