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
    /// Executes setComputePipelineState.
    /// </summary>
    public void setComputePipelineState(MTLComputePipelineState state) {
        objc_msgSend(this.NativePtr, sel_setComputePipelineState, state.NativePtr);
    }

    /// <summary>
    /// Executes setBuffer.
    /// </summary>
    public void setBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Executes setBytes.
    /// </summary>
    public unsafe void setBytes(void* bytes, UIntPtr length, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setBytes, bytes, length, index);
    }

    /// <summary>
    /// Executes dispatchThreadGroups.
    /// </summary>
    public void dispatchThreadGroups(MTLSize threadgroupsPerGrid, MTLSize threadsPerThreadgroup) {
        objc_msgSend(this.NativePtr, sel_dispatchThreadgroups0, threadgroupsPerGrid, threadsPerThreadgroup);
    }

    /// <summary>
    /// Executes dispatchThreadgroupsWithIndirectBuffer.
    /// </summary>
    public void dispatchThreadgroupsWithIndirectBuffer(MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset, MTLSize threadsPerThreadgroup) {
        objc_msgSend(this.NativePtr, sel_dispatchThreadgroups1, indirectBuffer.NativePtr, indirectBufferOffset, threadsPerThreadgroup);
    }

    /// <summary>
    /// Executes endEncoding.
    /// </summary>
    public void endEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Executes setTexture.
    /// </summary>
    public void setTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Executes setSamplerState.
    /// </summary>
    public void setSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Executes pushDebugGroup.
    /// </summary>
    public void pushDebugGroup(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.pushDebugGroup, @string.NativePtr);
    }

    /// <summary>
    /// Executes popDebugGroup.
    /// </summary>
    public void popDebugGroup() {
        objc_msgSend(this.NativePtr, Selectors.popDebugGroup);
    }

    /// <summary>
    /// Executes insertDebugSignpost.
    /// </summary>
    public void insertDebugSignpost(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.insertDebugSignpost, @string.NativePtr);
    }
}