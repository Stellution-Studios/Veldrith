using System;

namespace Veldrith.MetalBindings;

public struct MTLComputePipelineState {
    public readonly IntPtr NativePtr;

    public MTLComputePipelineState(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public bool IsNull => this.NativePtr == IntPtr.Zero;
}