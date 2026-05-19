namespace Veldrith.MetalBindings;

public struct CGRect {
    public CGPoint origin;
    public CGSize size;

    public CGRect(CGPoint origin, CGSize size) {
        this.origin = origin;
        this.size = size;
    }

    public override string ToString() {
        return string.Format("{0}, {1}", this.origin, this.size);
    }
}