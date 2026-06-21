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
    private static readonly Selector _selSetComputePipelineState = "setComputePipelineState:";

    /// <summary>
    /// Stores the sel set buffer state used by this instance.
    /// </summary>
    private static readonly Selector _selSetBuffer = "setBuffer:offset:atIndex:";

    /// <summary>
    /// Stores the sel dispatch threadgroups0 state used by this instance.
    /// </summary>
    private static readonly Selector _selDispatchThreadgroups0 = "dispatchThreadgroups:threadsPerThreadgroup:";

    /// <summary>
    /// Stores the sel dispatch threadgroups1 state used by this instance.
    /// </summary>
    private static readonly Selector _selDispatchThreadgroups1 = "dispatchThreadgroupsWithIndirectBuffer:indirectBufferOffset:threadsPerThreadgroup:";

    /// <summary>
    /// Stores the sel end encoding state used by this instance.
    /// </summary>
    private static readonly Selector _selEndEncoding = "endEncoding";

    /// <summary>
    /// Stores the sel set texture state used by this instance.
    /// </summary>
    private static readonly Selector _selSetTexture = "setTexture:atIndex:";

    /// <summary>
    /// Stores the sel set sampler state collection used by this instance.
    /// </summary>
    private static readonly Selector _selSetSamplerState = "setSamplerState:atIndex:";

    /// <summary>
    /// Stores the sel set bytes state used by this instance.
    /// </summary>
    private static readonly Selector _selSetBytes = "setBytes:length:atIndex:";

    /// <summary>
    /// Sets the compute pipeline state value.
    /// </summary>
    /// <param name="state">The state value used by this operation.</param>
    public void SetComputePipelineState(MTLComputePipelineState state) {
        ObjcMsgSend(this.NativePtr, _selSetComputePipelineState, state.NativePtr);
    }

    /// <summary>
    /// Sets the buffer value.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        ObjcMsgSend(this.NativePtr, _selSetBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Sets the bytes value.
    /// </summary>
    /// <param name="bytes">The bytes value used by this operation.</param>
    /// <param name="length">The number of items involved in this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public unsafe void SetBytes(void* bytes, UIntPtr length, UIntPtr index) {
        ObjcMsgSend(this.NativePtr, _selSetBytes, bytes, length, index);
    }

    /// <summary>
    /// Executes the dispatch thread groups logic for this backend.
    /// </summary>
    /// <param name="threadgroupsPerGrid">The threadgroups per grid value used by this operation.</param>
    /// <param name="threadsPerThreadgroup">The threads per threadgroup value used by this operation.</param>
    public void DispatchThreadGroups(MTLSize threadgroupsPerGrid, MTLSize threadsPerThreadgroup) {
        ObjcMsgSend(this.NativePtr, _selDispatchThreadgroups0, threadgroupsPerGrid, threadsPerThreadgroup);
    }

    /// <summary>
    /// Executes the dispatch threadgroups with indirect buffer logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="indirectBufferOffset">The indirect buffer offset value used by this operation.</param>
    /// <param name="threadsPerThreadgroup">The threads per threadgroup value used by this operation.</param>
    public void DispatchThreadgroupsWithIndirectBuffer(MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset, MTLSize threadsPerThreadgroup) {
        ObjcMsgSend(this.NativePtr, _selDispatchThreadgroups1, indirectBuffer.NativePtr, indirectBufferOffset, threadsPerThreadgroup);
    }

    /// <summary>
    /// Ends the encoding operation.
    /// </summary>
    public void EndEncoding() {
        ObjcMsgSend(this.NativePtr, _selEndEncoding);
    }

    /// <summary>
    /// Sets the texture value.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetTexture(MTLTexture texture, UIntPtr index) {
        ObjcMsgSend(this.NativePtr, _selSetTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Sets the sampler state value.
    /// </summary>
    /// <param name="sampler">The sampler resource involved in this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetSamplerState(MTLSamplerState sampler, UIntPtr index) {
        ObjcMsgSend(this.NativePtr, _selSetSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Executes the push debug group logic for this backend.
    /// </summary>
    /// <param name="string">The string value used by this operation.</param>
    public void PushDebugGroup(NSString @string) {
        ObjcMsgSend(this.NativePtr, Selectors.PushDebugGroup, @string.NativePtr);
    }

    /// <summary>
    /// Executes the pop debug group logic for this backend.
    /// </summary>
    public void PopDebugGroup() {
        ObjcMsgSend(this.NativePtr, Selectors.PopDebugGroup);
    }

    /// <summary>
    /// Executes the insert debug signpost logic for this backend.
    /// </summary>
    /// <param name="string">The string value used by this operation.</param>
    public void InsertDebugSignpost(NSString @string) {
        ObjcMsgSend(this.NativePtr, Selectors.InsertDebugSignpost, @string.NativePtr);
    }
}
