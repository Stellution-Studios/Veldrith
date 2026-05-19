using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLCommandBuffer struct.
/// </summary>
public struct MTLCommandBuffer : IEquatable<MTLCommandBuffer> {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the renderCommandEncoderWithDescriptor operation.
    /// </summary>
    /// <param name="desc">Specifies the value of <paramref name="desc" />.</param>
    /// <returns>Returns the result produced by the renderCommandEncoderWithDescriptor operation.</returns>
    public MTLRenderCommandEncoder renderCommandEncoderWithDescriptor(MTLRenderPassDescriptor desc) {
        return new MTLRenderCommandEncoder(IntPtr_objc_msgSend(this.NativePtr, sel_renderCommandEncoderWithDescriptor, desc.NativePtr));
    }

    /// <summary>
    /// Executes the presentDrawable operation.
    /// </summary>
    /// <param name="drawable">Specifies the value of <paramref name="drawable" />.</param>
    public void presentDrawable(IntPtr drawable) {
        objc_msgSend(this.NativePtr, sel_presentDrawable, drawable);
    }

    /// <summary>
    /// Executes the commit operation.
    /// </summary>
    public void commit() {
        objc_msgSend(this.NativePtr, sel_commit);
    }

    /// <summary>
    /// Executes the blitCommandEncoder operation.
    /// </summary>
    /// <returns>Returns the result produced by the blitCommandEncoder operation.</returns>
    public MTLBlitCommandEncoder blitCommandEncoder() {
        return objc_msgSend<MTLBlitCommandEncoder>(this.NativePtr, sel_blitCommandEncoder);
    }

    /// <summary>
    /// Executes the computeCommandEncoder operation.
    /// </summary>
    /// <returns>Returns the result produced by the computeCommandEncoder operation.</returns>
    public MTLComputeCommandEncoder computeCommandEncoder() {
        return objc_msgSend<MTLComputeCommandEncoder>(this.NativePtr, sel_computeCommandEncoder);
    }

    /// <summary>
    /// Executes the waitUntilCompleted operation.
    /// </summary>
    public void waitUntilCompleted() {
        objc_msgSend(this.NativePtr, sel_waitUntilCompleted);
    }

    /// <summary>
    /// Executes the addCompletedHandler operation.
    /// </summary>
    /// <param name="block">Specifies the value of <paramref name="block" />.</param>
    public void addCompletedHandler(MTLCommandBufferHandler block) {
        objc_msgSend(this.NativePtr, sel_addCompletedHandler, block);
    }

    /// <summary>
    /// Executes the addCompletedHandler operation.
    /// </summary>
    /// <param name="block">Specifies the value of <paramref name="block" />.</param>
    public void addCompletedHandler(IntPtr block) {
        objc_msgSend(this.NativePtr, sel_addCompletedHandler, block);
    }

    /// <summary>
    /// Executes the uint_objc_msgSend operation.
    /// </summary>
    /// <param name="MTLCommandBufferStatus">Specifies the value of <paramref name="MTLCommandBufferStatus" />.</param>
    /// <returns>Returns the result produced by the uint_objc_msgSend operation.</returns>
    public MTLCommandBufferStatus status => (MTLCommandBufferStatus)uint_objc_msgSend(this.NativePtr, sel_status);

    /// <summary>
    /// Stores the value associated with <c>sel_renderCommandEncoderWithDescriptor</c>.
    /// </summary>
    private static readonly Selector sel_renderCommandEncoderWithDescriptor = "renderCommandEncoderWithDescriptor:";

    /// <summary>
    /// Stores the value associated with <c>sel_presentDrawable</c>.
    /// </summary>
    private static readonly Selector sel_presentDrawable = "presentDrawable:";

    /// <summary>
    /// Stores the value associated with <c>sel_commit</c>.
    /// </summary>
    private static readonly Selector sel_commit = "commit";

    /// <summary>
    /// Stores the value associated with <c>sel_blitCommandEncoder</c>.
    /// </summary>
    private static readonly Selector sel_blitCommandEncoder = "blitCommandEncoder";

    /// <summary>
    /// Stores the value associated with <c>sel_computeCommandEncoder</c>.
    /// </summary>
    private static readonly Selector sel_computeCommandEncoder = "computeCommandEncoder";

    /// <summary>
    /// Stores the value associated with <c>sel_waitUntilCompleted</c>.
    /// </summary>
    private static readonly Selector sel_waitUntilCompleted = "waitUntilCompleted";

    /// <summary>
    /// Stores the value associated with <c>sel_addCompletedHandler</c>.
    /// </summary>
    private static readonly Selector sel_addCompletedHandler = "addCompletedHandler:";

    /// <summary>
    /// Stores the value associated with <c>sel_status</c>.
    /// </summary>
    private static readonly Selector sel_status = "status";

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(MTLCommandBuffer other) {
        return this.NativePtr == other.NativePtr;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="obj">Specifies the value of <paramref name="obj" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public override bool Equals(object obj) {
        return obj is MTLCommandBuffer other && this.Equals(other);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return this.NativePtr.GetHashCode();
    }
}