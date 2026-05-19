using System;

namespace Veldrith.MetalBindings;

public struct ObjectiveCMethod {
    public readonly IntPtr NativePtr;

    public ObjectiveCMethod(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public static implicit operator IntPtr(ObjectiveCMethod method) {
        return method.NativePtr;
    }

    public static implicit operator ObjectiveCMethod(IntPtr ptr) {
        return new ObjectiveCMethod(ptr);
    }

    public Selector GetSelector() {
        return ObjectiveCRuntime.method_getName(this);
    }

    public string GetName() {
        return this.GetSelector().Name;
    }
}