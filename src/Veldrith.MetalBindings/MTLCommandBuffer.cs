using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLCommandBuffer struct.
/// </summary>
public struct MTLCommandBuffer : IEquatable<MTLCommandBuffer> {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes renderCommandEncoderWithDescriptor.
    /// </summary>
    public MTLRenderCommandEncoder renderCommandEncoderWithDescriptor(MTLRenderPassDescriptor desc) {
        return new MTLRenderCommandEncoder(IntPtr_objc_msgSend(this.NativePtr, sel_renderCommandEncoderWithDescriptor, desc.NativePtr));
    }

    /// <summary>
    /// Executes presentDrawable.
    /// </summary>
    public void presentDrawable(IntPtr drawable) {
        objc_msgSend(this.NativePtr, sel_presentDrawable, drawable);
    }

    /// <summary>
    /// Executes commit.
    /// </summary>
    public void commit() {
        objc_msgSend(this.NativePtr, sel_commit);
    }

    /// <summary>
    /// Executes blitCommandEncoder.
    /// </summary>
    public MTLBlitCommandEncoder blitCommandEncoder() {
        return objc_msgSend<MTLBlitCommandEncoder>(this.NativePtr, sel_blitCommandEncoder);
    }

    /// <summary>
    /// Executes computeCommandEncoder.
    /// </summary>
    public MTLComputeCommandEncoder computeCommandEncoder() {
        return objc_msgSend<MTLComputeCommandEncoder>(this.NativePtr, sel_computeCommandEncoder);
    }

    /// <summary>
    /// Executes waitUntilCompleted.
    /// </summary>
    public void waitUntilCompleted() {
        objc_msgSend(this.NativePtr, sel_waitUntilCompleted);
    }

    /// <summary>
    /// Executes addCompletedHandler.
    /// </summary>
    public void addCompletedHandler(MTLCommandBufferHandler block) {
        objc_msgSend(this.NativePtr, sel_addCompletedHandler, block);
    }

    /// <summary>
    /// Executes addCompletedHandler.
    /// </summary>
    public void addCompletedHandler(IntPtr block) {
        objc_msgSend(this.NativePtr, sel_addCompletedHandler, block);
    }

    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public MTLCommandBufferStatus status => (MTLCommandBufferStatus)uint_objc_msgSend(this.NativePtr, sel_status);

    /// <summary>
    /// Represents the sel_renderCommandEncoderWithDescriptor field.
    /// </summary>
    private static readonly Selector sel_renderCommandEncoderWithDescriptor = "renderCommandEncoderWithDescriptor:";

    /// <summary>
    /// Represents the sel_presentDrawable field.
    /// </summary>
    private static readonly Selector sel_presentDrawable = "presentDrawable:";

    /// <summary>
    /// Represents the sel_commit field.
    /// </summary>
    private static readonly Selector sel_commit = "commit";

    /// <summary>
    /// Represents the sel_blitCommandEncoder field.
    /// </summary>
    private static readonly Selector sel_blitCommandEncoder = "blitCommandEncoder";

    /// <summary>
    /// Represents the sel_computeCommandEncoder field.
    /// </summary>
    private static readonly Selector sel_computeCommandEncoder = "computeCommandEncoder";

    /// <summary>
    /// Represents the sel_waitUntilCompleted field.
    /// </summary>
    private static readonly Selector sel_waitUntilCompleted = "waitUntilCompleted";

    /// <summary>
    /// Represents the sel_addCompletedHandler field.
    /// </summary>
    private static readonly Selector sel_addCompletedHandler = "addCompletedHandler:";

    /// <summary>
    /// Represents the sel_status field.
    /// </summary>
    private static readonly Selector sel_status = "status";

    /// <summary>
    /// Executes Equals.
    /// </summary>
    public bool Equals(MTLCommandBuffer other) {
        return this.NativePtr == other.NativePtr;
    }

    /// <summary>
    /// Executes Equals.
    /// </summary>
    public override bool Equals(object obj) {
        return obj is MTLCommandBuffer other && this.Equals(other);
    }

    /// <summary>
    /// Executes GetHashCode.
    /// </summary>
    public override int GetHashCode() {
        return this.NativePtr.GetHashCode();
    }
}