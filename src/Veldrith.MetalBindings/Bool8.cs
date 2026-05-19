namespace Veldrith.MetalBindings;

public struct Bool8 {
    public readonly byte Value;

    public Bool8(byte value) {
        this.Value = value;
    }

    public Bool8(bool value) {
        this.Value = value ? (byte)1 : (byte)0;
    }

    public static implicit operator bool(Bool8 b) {
        return b.Value != 0;
    }

    public static implicit operator byte(Bool8 b) {
        return b.Value;
    }

    public static implicit operator Bool8(bool b) {
        return new Bool8(b);
    }
}