using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLCommandBuffer data structure used by the graphics runtime.
/// </summary>
public struct MTLCommandBuffer : IEquatable<MTLCommandBuffer> {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the render command encoder with descriptor logic for this backend.
    /// </summary>
    /// <param name="desc">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public MTLRenderCommandEncoder renderCommandEncoderWithDescriptor(MTLRenderPassDescriptor desc) {
        return new MTLRenderCommandEncoder(IntPtr_objc_msgSend(this.NativePtr, sel_renderCommandEncoderWithDescriptor, desc.NativePtr));
    }

    /// <summary>
    /// Executes the present drawable logic for this backend.
    /// </summary>
    /// <param name="drawable">The drawable value used by this operation.</param>
    public void presentDrawable(IntPtr drawable) {
        objc_msgSend(this.NativePtr, sel_presentDrawable, drawable);
    }

    /// <summary>
    /// Executes the commit logic for this backend.
    /// </summary>
    public void commit() {
        objc_msgSend(this.NativePtr, sel_commit);
    }

    /// <summary>
    /// Executes the blit command encoder logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public MTLBlitCommandEncoder blitCommandEncoder() {
        return objc_msgSend<MTLBlitCommandEncoder>(this.NativePtr, sel_blitCommandEncoder);
    }

    /// <summary>
    /// Computes the command encoder value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public MTLComputeCommandEncoder computeCommandEncoder() {
        return objc_msgSend<MTLComputeCommandEncoder>(this.NativePtr, sel_computeCommandEncoder);
    }

    /// <summary>
    /// Executes the wait until completed logic for this backend.
    /// </summary>
    public void waitUntilCompleted() {
        objc_msgSend(this.NativePtr, sel_waitUntilCompleted);
    }

    /// <summary>
    /// Executes the add completed handler logic for this backend.
    /// </summary>
    /// <param name="block">The block value used by this operation.</param>
    public void addCompletedHandler(MTLCommandBufferHandler block) {
        objc_msgSend(this.NativePtr, sel_addCompletedHandler, block);
    }

    /// <summary>
    /// Executes the add completed handler logic for this backend.
    /// </summary>
    /// <param name="block">The block value used by this operation.</param>
    public void addCompletedHandler(IntPtr block) {
        objc_msgSend(this.NativePtr, sel_addCompletedHandler, block);
    }

    /// <summary>
    /// Executes the uint objc msg send logic for this backend.
    /// </summary>

    public MTLCommandBufferStatus status => (MTLCommandBufferStatus)uint_objc_msgSend(this.NativePtr, sel_status);

    /// <summary>
    /// Stores the sel render command encoder with descriptor state used by this instance.
    /// </summary>
    private static readonly Selector sel_renderCommandEncoderWithDescriptor = "renderCommandEncoderWithDescriptor:";

    /// <summary>
    /// Stores the sel present drawable state used by this instance.
    /// </summary>
    private static readonly Selector sel_presentDrawable = "presentDrawable:";

    /// <summary>
    /// Stores the sel commit state used by this instance.
    /// </summary>
    private static readonly Selector sel_commit = "commit";

    /// <summary>
    /// Stores the sel blit command encoder state used by this instance.
    /// </summary>
    private static readonly Selector sel_blitCommandEncoder = "blitCommandEncoder";

    /// <summary>
    /// Stores the sel compute command encoder state used by this instance.
    /// </summary>
    private static readonly Selector sel_computeCommandEncoder = "computeCommandEncoder";

    /// <summary>
    /// Stores the sel wait until completed state used by this instance.
    /// </summary>
    private static readonly Selector sel_waitUntilCompleted = "waitUntilCompleted";

    /// <summary>
    /// Stores the sel add completed handler state used by this instance.
    /// </summary>
    private static readonly Selector sel_addCompletedHandler = "addCompletedHandler:";

    /// <summary>
    /// Stores the sel status state used by this instance.
    /// </summary>
    private static readonly Selector sel_status = "status";

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(MTLCommandBuffer other) {
        return this.NativePtr == other.NativePtr;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="obj">The object instance to evaluate.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object obj) {
        return obj is MTLCommandBuffer other && this.Equals(other);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return this.NativePtr.GetHashCode();
    }
}