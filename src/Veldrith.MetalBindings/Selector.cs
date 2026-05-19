using System;
using System.Text;

namespace Veldrith.MetalBindings;

public unsafe struct Selector {
    public readonly IntPtr NativePtr;

    public Selector(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public Selector(string name) {
        int byteCount = Encoding.UTF8.GetMaxByteCount(name.Length);
        byte* utf8BytesPtr = stackalloc byte[byteCount];
        fixed (char* namePtr = name) {
            Encoding.UTF8.GetBytes(namePtr, name.Length, utf8BytesPtr, byteCount);
        }

        this.NativePtr = ObjectiveCRuntime.sel_registerName(utf8BytesPtr);
    }

    public string Name {
        get {
            byte* name = ObjectiveCRuntime.sel_getName(this.NativePtr);
            return MTLUtil.GetUtf8String(name);
        }
    }

    public static implicit operator Selector(string s) {
        return new Selector(s);
    }
}