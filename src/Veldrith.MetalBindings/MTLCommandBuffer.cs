using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]
public struct MTLCommandBuffer : IEquatable<MTLCommandBuffer> {
    public readonly IntPtr NativePtr;

    public MTLRenderCommandEncoder renderCommandEncoderWithDescriptor(MTLRenderPassDescriptor desc) {
        return new MTLRenderCommandEncoder(
            IntPtr_objc_msgSend(this.NativePtr, sel_renderCommandEncoderWithDescriptor, desc.NativePtr));
    }

    public void presentDrawable(IntPtr drawable) {
        objc_msgSend(this.NativePtr, sel_presentDrawable, drawable);
    }

    public void commit() {
        objc_msgSend(this.NativePtr, sel_commit);
    }

    public MTLBlitCommandEncoder blitCommandEncoder() {
        return objc_msgSend<MTLBlitCommandEncoder>(this.NativePtr, sel_blitCommandEncoder);
    }

    public MTLComputeCommandEncoder computeCommandEncoder() {
        return objc_msgSend<MTLComputeCommandEncoder>(this.NativePtr, sel_computeCommandEncoder);
    }

    public void waitUntilCompleted() {
        objc_msgSend(this.NativePtr, sel_waitUntilCompleted);
    }

    public void addCompletedHandler(MTLCommandBufferHandler block) {
        objc_msgSend(this.NativePtr, sel_addCompletedHandler, block);
    }

    public void addCompletedHandler(IntPtr block) {
        objc_msgSend(this.NativePtr, sel_addCompletedHandler, block);
    }

    public MTLCommandBufferStatus status => (MTLCommandBufferStatus)uint_objc_msgSend(this.NativePtr, sel_status);

    private static readonly Selector sel_renderCommandEncoderWithDescriptor = "renderCommandEncoderWithDescriptor:";
    private static readonly Selector sel_presentDrawable = "presentDrawable:";
    private static readonly Selector sel_commit = "commit";
    private static readonly Selector sel_blitCommandEncoder = "blitCommandEncoder";
    private static readonly Selector sel_computeCommandEncoder = "computeCommandEncoder";
    private static readonly Selector sel_waitUntilCompleted = "waitUntilCompleted";
    private static readonly Selector sel_addCompletedHandler = "addCompletedHandler:";
    private static readonly Selector sel_status = "status";

    public bool Equals(MTLCommandBuffer other) {
        return this.NativePtr == other.NativePtr;
    }

    public override bool Equals(object obj) {
        return obj is MTLCommandBuffer other && this.Equals(other);
    }

    public override int GetHashCode() {
        return this.NativePtr.GetHashCode();
    }
}