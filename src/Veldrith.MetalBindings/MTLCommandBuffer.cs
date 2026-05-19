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
    /// Performs the renderCommandEncoderWithDescriptor operation.
    /// </summary>
    /// <param name="desc">The value of desc.</param>
    /// <returns>The result of the renderCommandEncoderWithDescriptor operation.</returns>
    public MTLRenderCommandEncoder renderCommandEncoderWithDescriptor(MTLRenderPassDescriptor desc) {
        return new MTLRenderCommandEncoder(IntPtr_objc_msgSend(this.NativePtr, sel_renderCommandEncoderWithDescriptor, desc.NativePtr));
    }

    /// <summary>
    /// Performs the presentDrawable operation.
    /// </summary>
    /// <param name="drawable">The value of drawable.</param>
    public void presentDrawable(IntPtr drawable) {
        objc_msgSend(this.NativePtr, sel_presentDrawable, drawable);
    }

    /// <summary>
    /// Performs the commit operation.
    /// </summary>
    public void commit() {
        objc_msgSend(this.NativePtr, sel_commit);
    }

    /// <summary>
    /// Performs the blitCommandEncoder operation.
    /// </summary>
    /// <returns>The result of the blitCommandEncoder operation.</returns>
    public MTLBlitCommandEncoder blitCommandEncoder() {
        return objc_msgSend<MTLBlitCommandEncoder>(this.NativePtr, sel_blitCommandEncoder);
    }

    /// <summary>
    /// Performs the computeCommandEncoder operation.
    /// </summary>
    /// <returns>The result of the computeCommandEncoder operation.</returns>
    public MTLComputeCommandEncoder computeCommandEncoder() {
        return objc_msgSend<MTLComputeCommandEncoder>(this.NativePtr, sel_computeCommandEncoder);
    }

    /// <summary>
    /// Performs the waitUntilCompleted operation.
    /// </summary>
    public void waitUntilCompleted() {
        objc_msgSend(this.NativePtr, sel_waitUntilCompleted);
    }

    /// <summary>
    /// Performs the addCompletedHandler operation.
    /// </summary>
    /// <param name="block">The value of block.</param>
    public void addCompletedHandler(MTLCommandBufferHandler block) {
        objc_msgSend(this.NativePtr, sel_addCompletedHandler, block);
    }

    /// <summary>
    /// Performs the addCompletedHandler operation.
    /// </summary>
    /// <param name="block">The value of block.</param>
    public void addCompletedHandler(IntPtr block) {
        objc_msgSend(this.NativePtr, sel_addCompletedHandler, block);
    }

    /// <summary>
    /// Performs the uint_objc_msgSend operation.
    /// </summary>
    /// <param name="MTLCommandBufferStatus">The value of MTLCommandBufferStatus.</param>
    /// <returns>The result of the uint_objc_msgSend operation.</returns>
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
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(MTLCommandBuffer other) {
        return this.NativePtr == other.NativePtr;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="obj">The value of obj.</param>
    /// <returns>The result of the Equals operation.</returns>
    public override bool Equals(object obj) {
        return obj is MTLCommandBuffer other && this.Equals(other);
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return this.NativePtr.GetHashCode();
    }
}