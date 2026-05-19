using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct NSArray {
    public readonly IntPtr NativePtr;

    public NSArray(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public UIntPtr count => UIntPtr_objc_msgSend(this.NativePtr, sel_count);
    private static readonly Selector sel_count = "count";
}