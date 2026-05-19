using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct MTLVertexBufferLayoutDescriptor {
    public readonly IntPtr NativePtr;

    public MTLVertexBufferLayoutDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public MTLVertexStepFunction stepFunction {
        get => (MTLVertexStepFunction)uint_objc_msgSend(this.NativePtr, sel_stepFunction);
        set => objc_msgSend(this.NativePtr, sel_setStepFunction, (uint)value);
    }

    public UIntPtr stride {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_stride);
        set => objc_msgSend(this.NativePtr, sel_setStride, value);
    }

    public UIntPtr stepRate {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_stepRate);
        set => objc_msgSend(this.NativePtr, sel_setStepRate, value);
    }

    private static readonly Selector sel_stepFunction = "stepFunction";
    private static readonly Selector sel_setStepFunction = "setStepFunction:";
    private static readonly Selector sel_stride = "stride";
    private static readonly Selector sel_setStride = "setStride:";
    private static readonly Selector sel_stepRate = "stepRate";
    private static readonly Selector sel_setStepRate = "setStepRate:";
}