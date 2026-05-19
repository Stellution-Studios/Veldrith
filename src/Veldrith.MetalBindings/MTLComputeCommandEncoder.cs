using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLComputeCommandEncoder data structure used by the graphics runtime.
/// </summary>
public struct MTLComputeCommandEncoder {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Stores the sel set compute pipeline state state used by this instance.
    /// </summary>
    private static readonly Selector sel_setComputePipelineState = "setComputePipelineState:";

    /// <summary>
    /// Stores the sel set buffer state used by this instance.
    /// </summary>
    private static readonly Selector sel_setBuffer = "setBuffer:offset:atIndex:";

    /// <summary>
    /// Stores the sel dispatch threadgroups0 state used by this instance.
    /// </summary>
    private static readonly Selector sel_dispatchThreadgroups0 = "dispatchThreadgroups:threadsPerThreadgroup:";

    /// <summary>
    /// Stores the sel dispatch threadgroups1 state used by this instance.
    /// </summary>
    private static readonly Selector sel_dispatchThreadgroups1 = "dispatchThreadgroupsWithIndirectBuffer:indirectBufferOffset:threadsPerThreadgroup:";

    /// <summary>
    /// Stores the sel end encoding state used by this instance.
    /// </summary>
    private static readonly Selector sel_endEncoding = "endEncoding";

    /// <summary>
    /// Stores the sel set texture state used by this instance.
    /// </summary>
    private static readonly Selector sel_setTexture = "setTexture:atIndex:";

    /// <summary>
    /// Stores the sel set sampler state collection used by this instance.
    /// </summary>
    private static readonly Selector sel_setSamplerState = "setSamplerState:atIndex:";

    /// <summary>
    /// Stores the sel set bytes state used by this instance.
    /// </summary>
    private static readonly Selector sel_setBytes = "setBytes:length:atIndex:";

    /// <summary>
    /// Sets the compute pipeline state value.
    /// </summary>
    /// <param name="state">The state value used by this operation.</param>
    public void setComputePipelineState(MTLComputePipelineState state) {
        objc_msgSend(this.NativePtr, sel_setComputePipelineState, state.NativePtr);
    }

    /// <summary>
    /// Sets the buffer value.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void setBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Sets the bytes value.
    /// </summary>
    /// <param name="bytes">The bytes value used by this operation.</param>
    /// <param name="length">The number of items involved in this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public unsafe void setBytes(void* bytes, UIntPtr length, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setBytes, bytes, length, index);
    }

    /// <summary>
    /// Executes the dispatch thread groups logic for this backend.
    /// </summary>
    /// <param name="threadgroupsPerGrid">The threadgroups per grid value used by this operation.</param>
    /// <param name="threadsPerThreadgroup">The threads per threadgroup value used by this operation.</param>
    public void dispatchThreadGroups(MTLSize threadgroupsPerGrid, MTLSize threadsPerThreadgroup) {
        objc_msgSend(this.NativePtr, sel_dispatchThreadgroups0, threadgroupsPerGrid, threadsPerThreadgroup);
    }

    /// <summary>
    /// Executes the dispatch threadgroups with indirect buffer logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="indirectBufferOffset">The indirect buffer offset value used by this operation.</param>
    /// <param name="threadsPerThreadgroup">The threads per threadgroup value used by this operation.</param>
    public void dispatchThreadgroupsWithIndirectBuffer(MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset, MTLSize threadsPerThreadgroup) {
        objc_msgSend(this.NativePtr, sel_dispatchThreadgroups1, indirectBuffer.NativePtr, indirectBufferOffset, threadsPerThreadgroup);
    }

    /// <summary>
    /// Ends the encoding operation.
    /// </summary>
    public void endEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Sets the texture value.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void setTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Sets the sampler state value.
    /// </summary>
    /// <param name="sampler">The sampler resource involved in this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void setSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Executes the push debug group logic for this backend.
    /// </summary>
    /// <param name="string">The string value used by this operation.</param>
    public void pushDebugGroup(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.pushDebugGroup, @string.NativePtr);
    }

    /// <summary>
    /// Executes the pop debug group logic for this backend.
    /// </summary>
    public void popDebugGroup() {
        objc_msgSend(this.NativePtr, Selectors.popDebugGroup);
    }

    /// <summary>
    /// Executes the insert debug signpost logic for this backend.
    /// </summary>
    /// <param name="string">The string value used by this operation.</param>
    public void insertDebugSignpost(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.insertDebugSignpost, @string.NativePtr);
    }
}