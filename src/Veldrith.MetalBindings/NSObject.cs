using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct NSObject {
    public readonly IntPtr NativePtr;

    public NSObject(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public Bool8 IsKindOfClass(IntPtr @class) {
        return bool8_objc_msgSend(this.NativePtr, sel_isKindOfClass, @class);
    }

    private static readonly Selector sel_isKindOfClass = "isKindOfClass:";
}