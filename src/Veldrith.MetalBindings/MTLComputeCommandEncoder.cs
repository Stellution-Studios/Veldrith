using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLComputeCommandEncoder struct.
/// </summary>
public struct MTLComputeCommandEncoder {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Represents the sel_setComputePipelineState field.
    /// </summary>
    private static readonly Selector sel_setComputePipelineState = "setComputePipelineState:";

    /// <summary>
    /// Represents the sel_setBuffer field.
    /// </summary>
    private static readonly Selector sel_setBuffer = "setBuffer:offset:atIndex:";

    /// <summary>
    /// Represents the sel_dispatchThreadgroups0 field.
    /// </summary>
    private static readonly Selector sel_dispatchThreadgroups0 = "dispatchThreadgroups:threadsPerThreadgroup:";

    /// <summary>
    /// Represents the sel_dispatchThreadgroups1 field.
    /// </summary>
    private static readonly Selector sel_dispatchThreadgroups1 = "dispatchThreadgroupsWithIndirectBuffer:indirectBufferOffset:threadsPerThreadgroup:";

    /// <summary>
    /// Represents the sel_endEncoding field.
    /// </summary>
    private static readonly Selector sel_endEncoding = "endEncoding";

    /// <summary>
    /// Represents the sel_setTexture field.
    /// </summary>
    private static readonly Selector sel_setTexture = "setTexture:atIndex:";

    /// <summary>
    /// Represents the sel_setSamplerState field.
    /// </summary>
    private static readonly Selector sel_setSamplerState = "setSamplerState:atIndex:";

    /// <summary>
    /// Represents the sel_setBytes field.
    /// </summary>
    private static readonly Selector sel_setBytes = "setBytes:length:atIndex:";

    /// <summary>
    /// Performs the setComputePipelineState operation.
    /// </summary>
    /// <param name="state">The value of state.</param>
    public void setComputePipelineState(MTLComputePipelineState state) {
        objc_msgSend(this.NativePtr, sel_setComputePipelineState, state.NativePtr);
    }

    /// <summary>
    /// Performs the setBuffer operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="offset">The value of offset.</param>
    /// <param name="index">The value of index.</param>
    public void setBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Performs the setBytes operation.
    /// </summary>
    /// <param name="bytes">The value of bytes.</param>
    /// <param name="length">The value of length.</param>
    /// <param name="index">The value of index.</param>
    public unsafe void setBytes(void* bytes, UIntPtr length, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setBytes, bytes, length, index);
    }

    /// <summary>
    /// Performs the dispatchThreadGroups operation.
    /// </summary>
    /// <param name="threadgroupsPerGrid">The value of threadgroupsPerGrid.</param>
    /// <param name="threadsPerThreadgroup">The value of threadsPerThreadgroup.</param>
    public void dispatchThreadGroups(MTLSize threadgroupsPerGrid, MTLSize threadsPerThreadgroup) {
        objc_msgSend(this.NativePtr, sel_dispatchThreadgroups0, threadgroupsPerGrid, threadsPerThreadgroup);
    }

    /// <summary>
    /// Performs the dispatchThreadgroupsWithIndirectBuffer operation.
    /// </summary>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="indirectBufferOffset">The value of indirectBufferOffset.</param>
    /// <param name="threadsPerThreadgroup">The value of threadsPerThreadgroup.</param>
    public void dispatchThreadgroupsWithIndirectBuffer(MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset, MTLSize threadsPerThreadgroup) {
        objc_msgSend(this.NativePtr, sel_dispatchThreadgroups1, indirectBuffer.NativePtr, indirectBufferOffset, threadsPerThreadgroup);
    }

    /// <summary>
    /// Performs the endEncoding operation.
    /// </summary>
    public void endEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Performs the setTexture operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="index">The value of index.</param>
    public void setTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Performs the setSamplerState operation.
    /// </summary>
    /// <param name="sampler">The value of sampler.</param>
    /// <param name="index">The value of index.</param>
    public void setSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Performs the pushDebugGroup operation.
    /// </summary>
    /// <param name="string">The value of string.</param>
    public void pushDebugGroup(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.pushDebugGroup, @string.NativePtr);
    }

    /// <summary>
    /// Performs the popDebugGroup operation.
    /// </summary>
    public void popDebugGroup() {
        objc_msgSend(this.NativePtr, Selectors.popDebugGroup);
    }

    /// <summary>
    /// Performs the insertDebugSignpost operation.
    /// </summary>
    /// <param name="string">The value of string.</param>
    public void insertDebugSignpost(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.insertDebugSignpost, @string.NativePtr);
    }
}