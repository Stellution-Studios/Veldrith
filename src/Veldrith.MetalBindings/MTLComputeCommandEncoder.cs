using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLComputeCommandEncoder struct.
/// </summary>
public struct MTLComputeCommandEncoder {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Stores the value associated with <c>sel_setComputePipelineState</c>.
    /// </summary>
    private static readonly Selector sel_setComputePipelineState = "setComputePipelineState:";

    /// <summary>
    /// Stores the value associated with <c>sel_setBuffer</c>.
    /// </summary>
    private static readonly Selector sel_setBuffer = "setBuffer:offset:atIndex:";

    /// <summary>
    /// Stores the value associated with <c>sel_dispatchThreadgroups0</c>.
    /// </summary>
    private static readonly Selector sel_dispatchThreadgroups0 = "dispatchThreadgroups:threadsPerThreadgroup:";

    /// <summary>
    /// Stores the value associated with <c>sel_dispatchThreadgroups1</c>.
    /// </summary>
    private static readonly Selector sel_dispatchThreadgroups1 = "dispatchThreadgroupsWithIndirectBuffer:indirectBufferOffset:threadsPerThreadgroup:";

    /// <summary>
    /// Stores the value associated with <c>sel_endEncoding</c>.
    /// </summary>
    private static readonly Selector sel_endEncoding = "endEncoding";

    /// <summary>
    /// Stores the value associated with <c>sel_setTexture</c>.
    /// </summary>
    private static readonly Selector sel_setTexture = "setTexture:atIndex:";

    /// <summary>
    /// Stores the value associated with <c>sel_setSamplerState</c>.
    /// </summary>
    private static readonly Selector sel_setSamplerState = "setSamplerState:atIndex:";

    /// <summary>
    /// Stores the value associated with <c>sel_setBytes</c>.
    /// </summary>
    private static readonly Selector sel_setBytes = "setBytes:length:atIndex:";

    /// <summary>
    /// Executes the setComputePipelineState operation.
    /// </summary>
    /// <param name="state">Specifies the value of <paramref name="state" />.</param>
    public void setComputePipelineState(MTLComputePipelineState state) {
        objc_msgSend(this.NativePtr, sel_setComputePipelineState, state.NativePtr);
    }

    /// <summary>
    /// Executes the setBuffer operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void setBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Executes the setBytes operation.
    /// </summary>
    /// <param name="bytes">Specifies the value of <paramref name="bytes" />.</param>
    /// <param name="length">Specifies the value of <paramref name="length" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public unsafe void setBytes(void* bytes, UIntPtr length, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setBytes, bytes, length, index);
    }

    /// <summary>
    /// Executes the dispatchThreadGroups operation.
    /// </summary>
    /// <param name="threadgroupsPerGrid">Specifies the value of <paramref name="threadgroupsPerGrid" />.</param>
    /// <param name="threadsPerThreadgroup">Specifies the value of <paramref name="threadsPerThreadgroup" />.</param>
    public void dispatchThreadGroups(MTLSize threadgroupsPerGrid, MTLSize threadsPerThreadgroup) {
        objc_msgSend(this.NativePtr, sel_dispatchThreadgroups0, threadgroupsPerGrid, threadsPerThreadgroup);
    }

    /// <summary>
    /// Executes the dispatchThreadgroupsWithIndirectBuffer operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="indirectBufferOffset">Specifies the value of <paramref name="indirectBufferOffset" />.</param>
    /// <param name="threadsPerThreadgroup">Specifies the value of <paramref name="threadsPerThreadgroup" />.</param>
    public void dispatchThreadgroupsWithIndirectBuffer(MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset, MTLSize threadsPerThreadgroup) {
        objc_msgSend(this.NativePtr, sel_dispatchThreadgroups1, indirectBuffer.NativePtr, indirectBufferOffset, threadsPerThreadgroup);
    }

    /// <summary>
    /// Executes the endEncoding operation.
    /// </summary>
    public void endEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Executes the setTexture operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void setTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Executes the setSamplerState operation.
    /// </summary>
    /// <param name="sampler">Specifies the value of <paramref name="sampler" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void setSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Executes the pushDebugGroup operation.
    /// </summary>
    /// <param name="string">Specifies the value of <paramref name="string" />.</param>
    public void pushDebugGroup(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.pushDebugGroup, @string.NativePtr);
    }

    /// <summary>
    /// Executes the popDebugGroup operation.
    /// </summary>
    public void popDebugGroup() {
        objc_msgSend(this.NativePtr, Selectors.popDebugGroup);
    }

    /// <summary>
    /// Executes the insertDebugSignpost operation.
    /// </summary>
    /// <param name="string">Specifies the value of <paramref name="string" />.</param>
    public void insertDebugSignpost(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.insertDebugSignpost, @string.NativePtr);
    }
}