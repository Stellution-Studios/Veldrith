using System;

namespace Veldrith.MetalBindings;

public struct NSDictionary {
    public readonly IntPtr NativePtr;

    public UIntPtr count => ObjectiveCRuntime.UIntPtr_objc_msgSend(this.NativePtr, sel_count);

    private static readonly Selector sel_count = "count";
}