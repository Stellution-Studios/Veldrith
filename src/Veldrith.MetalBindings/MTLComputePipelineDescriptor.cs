using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct MTLComputePipelineDescriptor {
    public readonly IntPtr NativePtr;

    public MTLFunction computeFunction {
        get => objc_msgSend<MTLFunction>(this.NativePtr, sel_computeFunction);
        set => objc_msgSend(this.NativePtr, sel_setComputeFunction, value.NativePtr);
    }

    public MTLPipelineBufferDescriptorArray buffers
        => objc_msgSend<MTLPipelineBufferDescriptorArray>(this.NativePtr, sel_buffers);

    private static readonly Selector sel_computeFunction = "computeFunction";
    private static readonly Selector sel_setComputeFunction = "setComputeFunction:";
    private static readonly Selector sel_buffers = "buffers";
}