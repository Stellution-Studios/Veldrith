using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct CALayer {
    public readonly IntPtr NativePtr;

    public static implicit operator IntPtr(CALayer c) {
        return c.NativePtr;
    }

    public CALayer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public void addSublayer(IntPtr layer) {
        objc_msgSend(this.NativePtr, sel_addSublayer, layer);
    }

    private static readonly Selector sel_addSublayer = "addSublayer:";
}