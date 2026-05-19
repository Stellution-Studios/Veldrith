using System;

namespace Veldrith.MetalBindings;

public struct MTLRenderPipelineState {
    public readonly IntPtr NativePtr;

    public MTLRenderPipelineState(IntPtr ptr) {
        this.NativePtr = ptr;
    }
}