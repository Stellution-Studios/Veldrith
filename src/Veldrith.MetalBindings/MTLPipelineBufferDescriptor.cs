using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct MTLPipelineBufferDescriptor {
    public readonly IntPtr NativePtr;

    public MTLPipelineBufferDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public MTLMutability mutability {
        get => (MTLMutability)uint_objc_msgSend(this.NativePtr, sel_mutability);
        set => objc_msgSend(this.NativePtr, sel_setMutability, (uint)value);
    }

    private static readonly Selector sel_mutability = "mutability";
    private static readonly Selector sel_setMutability = "setMutability:";
}