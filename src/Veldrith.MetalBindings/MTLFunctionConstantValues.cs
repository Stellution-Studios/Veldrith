using System;

namespace Veldrith.MetalBindings;

public struct MTLFunctionConstantValues {
    public readonly IntPtr NativePtr;

    public static MTLFunctionConstantValues New() {
        return s_class.AllocInit<MTLFunctionConstantValues>();
    }

    public unsafe void setConstantValuetypeatIndex(void* value, MTLDataType type, UIntPtr index) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_setConstantValuetypeatIndex, value, (uint)type, index);
    }

    private static readonly ObjCClass s_class = new(nameof(MTLFunctionConstantValues));
    private static readonly Selector sel_setConstantValuetypeatIndex = "setConstantValue:type:atIndex:";
}