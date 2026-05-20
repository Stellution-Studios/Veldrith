using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRenderCommandEncoder data structure used by the graphics runtime.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MTLRenderCommandEncoder {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderCommandEncoder" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLRenderCommandEncoder(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Sets the render pipeline state value.
    /// </summary>
    /// <param name="pipelineState">The pipeline state value used by this operation.</param>
    public void SetRenderPipelineState(MTLRenderPipelineState pipelineState) {
        objc_msgSend(this.NativePtr, sel_setRenderPipelineState, pipelineState.NativePtr);
    }

    /// <summary>
    /// Sets the vertex buffer value.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetVertexBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Sets the vertex buffer offset value.
    /// </summary>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetVertexBufferOffset(UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexBufferOffset, offset, index);
    }

    /// <summary>
    /// Sets the fragment buffer value.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetFragmentBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Sets the fragment buffer offset value.
    /// </summary>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetFragmentBufferOffset(UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentBufferOffset, offset, index);
    }

    /// <summary>
    /// Sets inline bytes for the vertex stage.
    /// </summary>
    /// <param name="bytes">The pointer to source bytes.</param>
    /// <param name="length">The number of bytes to bind.</param>
    /// <param name="index">The zero-based binding index.</param>
    public unsafe void SetVertexBytes(void* bytes, UIntPtr length, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexBytes, bytes, length, index);
    }

    /// <summary>
    /// Sets inline bytes for the fragment stage.
    /// </summary>
    /// <param name="bytes">The pointer to source bytes.</param>
    /// <param name="length">The number of bytes to bind.</param>
    /// <param name="index">The zero-based binding index.</param>
    public unsafe void SetFragmentBytes(void* bytes, UIntPtr length, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentBytes, bytes, length, index);
    }

    /// <summary>
    /// Sets the vertex texture value.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetVertexTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Sets the fragment texture value.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetFragmentTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Sets the vertex sampler state value.
    /// </summary>
    /// <param name="sampler">The sampler resource involved in this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetVertexSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Sets the fragment sampler state value.
    /// </summary>
    /// <param name="sampler">The sampler resource involved in this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public void SetFragmentSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Executes the draw primitives logic for this backend.
    /// </summary>
    /// <param name="primitiveType">The primitive type value used by this operation.</param>
    /// <param name="vertexStart">The vertex start value used by this operation.</param>
    /// <param name="vertexCount">The vertex count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="baseInstance">The base instance value used by this operation.</param>
    public void DrawPrimitives(MTLPrimitiveType primitiveType, UIntPtr vertexStart, UIntPtr vertexCount, UIntPtr instanceCount, UIntPtr baseInstance) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives0, primitiveType, vertexStart, vertexCount, instanceCount, baseInstance);
    }

    /// <summary>
    /// Executes the draw primitives logic for this backend.
    /// </summary>
    /// <param name="primitiveType">The primitive type value used by this operation.</param>
    /// <param name="vertexStart">The vertex start value used by this operation.</param>
    /// <param name="vertexCount">The vertex count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    public void DrawPrimitives(MTLPrimitiveType primitiveType, UIntPtr vertexStart, UIntPtr vertexCount, UIntPtr instanceCount) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives2, primitiveType, vertexStart, vertexCount, instanceCount);
    }

    /// <summary>
    /// Executes the draw primitives logic for this backend.
    /// </summary>
    /// <param name="primitiveType">The primitive type value used by this operation.</param>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="indirectBufferOffset">The indirect buffer offset value used by this operation.</param>
    public void DrawPrimitives(MTLPrimitiveType primitiveType, MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives1, primitiveType, indirectBuffer, indirectBufferOffset);
    }

    /// <summary>
    /// Executes the draw indexed primitives logic for this backend.
    /// </summary>
    /// <param name="primitiveType">The primitive type value used by this operation.</param>
    /// <param name="indexCount">The index count value used by this operation.</param>
    /// <param name="indexType">The index type value used by this operation.</param>
    /// <param name="indexBuffer">The index buffer value used by this operation.</param>
    /// <param name="indexBufferOffset">The index buffer offset value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    public void DrawIndexedPrimitives(MTLPrimitiveType primitiveType, UIntPtr indexCount, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, UIntPtr instanceCount) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives0, primitiveType, indexCount, indexType, indexBuffer.NativePtr, indexBufferOffset, instanceCount);
    }

    /// <summary>
    /// Executes the draw indexed primitives logic for this backend.
    /// </summary>
    /// <param name="primitiveType">The primitive type value used by this operation.</param>
    /// <param name="indexCount">The index count value used by this operation.</param>
    /// <param name="indexType">The index type value used by this operation.</param>
    /// <param name="indexBuffer">The index buffer value used by this operation.</param>
    /// <param name="indexBufferOffset">The index buffer offset value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="baseVertex">The base vertex value used by this operation.</param>
    /// <param name="baseInstance">The base instance value used by this operation.</param>
    public void DrawIndexedPrimitives(MTLPrimitiveType primitiveType, UIntPtr indexCount, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, UIntPtr instanceCount, IntPtr baseVertex, UIntPtr baseInstance) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives1, primitiveType, indexCount, indexType, indexBuffer.NativePtr, indexBufferOffset, instanceCount, baseVertex, baseInstance);
    }

    /// <summary>
    /// Executes the draw indexed primitives logic for this backend.
    /// </summary>
    /// <param name="primitiveType">The primitive type value used by this operation.</param>
    /// <param name="indexType">The index type value used by this operation.</param>
    /// <param name="indexBuffer">The index buffer value used by this operation.</param>
    /// <param name="indexBufferOffset">The index buffer offset value used by this operation.</param>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="indirectBufferOffset">The indirect buffer offset value used by this operation.</param>
    public void DrawIndexedPrimitives(MTLPrimitiveType primitiveType, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives2, primitiveType, indexType, indexBuffer, indexBufferOffset, indirectBuffer, indirectBufferOffset);
    }

    /// <summary>
    /// Sets the viewport value.
    /// </summary>
    /// <param name="viewport">The viewport value used by this operation.</param>
    public void SetViewport(MTLViewport viewport) {
        objc_msgSend(this.NativePtr, sel_setViewport, viewport);
    }

    /// <summary>
    /// Sets the viewports value.
    /// </summary>
    /// <param name="viewports">The viewports value used by this operation.</param>
    /// <param name="count">The number of items involved in this operation.</param>
    public unsafe void SetViewports(MTLViewport* viewports, UIntPtr count) {
        objc_msgSend(this.NativePtr, sel_setViewports, viewports, count);
    }

    /// <summary>
    /// Sets the scissor rect value.
    /// </summary>
    /// <param name="scissorRect">The scissor rect value used by this operation.</param>
    public void SetScissorRect(MTLScissorRect scissorRect) {
        objc_msgSend(this.NativePtr, sel_setScissorRect, scissorRect);
    }

    /// <summary>
    /// Sets the scissor rects value.
    /// </summary>
    /// <param name="scissorRects">The scissor rects value used by this operation.</param>
    /// <param name="count">The number of items involved in this operation.</param>
    public unsafe void SetScissorRects(MTLScissorRect* scissorRects, UIntPtr count) {
        objc_msgSend(this.NativePtr, sel_setScissorRects, scissorRects, count);
    }

    /// <summary>
    /// Sets the cull mode value.
    /// </summary>
    /// <param name="cullMode">The cull mode value used by this operation.</param>
    public void SetCullMode(MTLCullMode cullMode) {
        objc_msgSend(this.NativePtr, sel_setCullMode, (uint)cullMode);
    }

    /// <summary>
    /// Sets the front facing value.
    /// </summary>
    /// <param name="frontFaceWinding">The front face winding value used by this operation.</param>
    public void SetFrontFacing(MTLWinding frontFaceWinding) {
        objc_msgSend(this.NativePtr, sel_setFrontFacingWinding, (uint)frontFaceWinding);
    }

    /// <summary>
    /// Sets the depth stencil state value.
    /// </summary>
    /// <param name="depthStencilState">The depth stencil state value used by this operation.</param>
    public void SetDepthStencilState(MTLDepthStencilState depthStencilState) {
        objc_msgSend(this.NativePtr, sel_setDepthStencilState, depthStencilState.NativePtr);
    }

    /// <summary>
    /// Sets the depth clip mode value.
    /// </summary>
    /// <param name="depthClipMode">The depth clip mode value used by this operation.</param>
    public void SetDepthClipMode(MTLDepthClipMode depthClipMode) {
        objc_msgSend(this.NativePtr, sel_setDepthClipMode, (uint)depthClipMode);
    }

    /// <summary>
    /// Ends the encoding operation.
    /// </summary>
    public void EndEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Sets the stencil reference value value.
    /// </summary>
    /// <param name="stencilReference">The stencil reference value used by this operation.</param>
    public void SetStencilReferenceValue(uint stencilReference) {
        objc_msgSend(this.NativePtr, sel_setStencilReferenceValue, stencilReference);
    }

    /// <summary>
    /// Sets the blend color value.
    /// </summary>
    /// <param name="red">The Red value used by this operation.</param>
    /// <param name="green">The Green value used by this operation.</param>
    /// <param name="blue">The Blue value used by this operation.</param>
    /// <param name="alpha">The Alpha value used by this operation.</param>
    public void SetBlendColor(float red, float green, float blue, float alpha) {
        objc_msgSend(this.NativePtr, sel_setBlendColor, red, green, blue, alpha);
    }

    /// <summary>
    /// Sets the triangle fill mode value.
    /// </summary>
    /// <param name="fillMode">The fill mode value used by this operation.</param>
    public void SetTriangleFillMode(MTLTriangleFillMode fillMode) {
        objc_msgSend(this.NativePtr, sel_setTriangleFillMode, (uint)fillMode);
    }

    /// <summary>
    /// Executes the push debug group logic for this backend.
    /// </summary>
    /// <param name="string">The string value used by this operation.</param>
    public void PushDebugGroup(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.PushDebugGroup, @string.NativePtr);
    }

    /// <summary>
    /// Executes the pop debug group logic for this backend.
    /// </summary>
    public void PopDebugGroup() {
        objc_msgSend(this.NativePtr, Selectors.PopDebugGroup);
    }

    /// <summary>
    /// Executes the insert debug signpost logic for this backend.
    /// </summary>
    /// <param name="string">The string value used by this operation.</param>
    public void InsertDebugSignpost(NSString @string) {
        objc_msgSend(this.NativePtr, Selectors.InsertDebugSignpost, @string.NativePtr);
    }

    /// <summary>
    /// Stores the sel set render pipeline state state used by this instance.
    /// </summary>
    private static readonly Selector sel_setRenderPipelineState = "setRenderPipelineState:";

    /// <summary>
    /// Stores the sel set vertex buffer state used by this instance.
    /// </summary>
    private static readonly Selector sel_setVertexBuffer = "setVertexBuffer:offset:atIndex:";

    /// <summary>
    /// Stores the sel set vertex buffer offset value used during command execution.
    /// </summary>
    private static readonly Selector sel_setVertexBufferOffset = "setVertexBufferOffset:atIndex:";

    /// <summary>
    /// Stores the sel set fragment buffer state used by this instance.
    /// </summary>
    private static readonly Selector sel_setFragmentBuffer = "setFragmentBuffer:offset:atIndex:";

    /// <summary>
    /// Stores the sel set fragment buffer offset value used during command execution.
    /// </summary>
    private static readonly Selector sel_setFragmentBufferOffset = "setFragmentBufferOffset:atIndex:";

    /// <summary>
    /// Stores the selector used to bind inline vertex-stage bytes.
    /// </summary>
    private static readonly Selector sel_setVertexBytes = "setVertexBytes:length:atIndex:";

    /// <summary>
    /// Stores the selector used to bind inline fragment-stage bytes.
    /// </summary>
    private static readonly Selector sel_setFragmentBytes = "setFragmentBytes:length:atIndex:";

    /// <summary>
    /// Stores the sel set vertex texture state used by this instance.
    /// </summary>
    private static readonly Selector sel_setVertexTexture = "setVertexTexture:atIndex:";

    /// <summary>
    /// Stores the sel set fragment texture state used by this instance.
    /// </summary>
    private static readonly Selector sel_setFragmentTexture = "setFragmentTexture:atIndex:";

    /// <summary>
    /// Stores the sel set vertex sampler state collection used by this instance.
    /// </summary>
    private static readonly Selector sel_setVertexSamplerState = "setVertexSamplerState:atIndex:";

    /// <summary>
    /// Stores the sel set fragment sampler state collection used by this instance.
    /// </summary>
    private static readonly Selector sel_setFragmentSamplerState = "setFragmentSamplerState:atIndex:";

    /// <summary>
    /// Stores the sel draw primitives0 state used by this instance.
    /// </summary>
    private static readonly Selector sel_drawPrimitives0 = "drawPrimitives:vertexStart:vertexCount:instanceCount:baseInstance:";

    /// <summary>
    /// Stores the sel draw primitives1 state used by this instance.
    /// </summary>
    private static readonly Selector sel_drawPrimitives1 = "drawPrimitives:indirectBuffer:indirectBufferOffset:";

    /// <summary>
    /// Stores the sel draw primitives2 state used by this instance.
    /// </summary>
    private static readonly Selector sel_drawPrimitives2 = "drawPrimitives:vertexStart:vertexCount:instanceCount:";

    /// <summary>
    /// Stores the sel draw indexed primitives0 value used during command execution.
    /// </summary>
    private static readonly Selector sel_drawIndexedPrimitives0 = "drawIndexedPrimitives:indexCount:indexType:indexBuffer:indexBufferOffset:instanceCount:";

    /// <summary>
    /// Stores the sel draw indexed primitives1 value used during command execution.
    /// </summary>
    private static readonly Selector sel_drawIndexedPrimitives1 = "drawIndexedPrimitives:indexCount:indexType:indexBuffer:indexBufferOffset:instanceCount:baseVertex:baseInstance:";

    /// <summary>
    /// Stores the sel draw indexed primitives2 value used during command execution.
    /// </summary>
    private static readonly Selector sel_drawIndexedPrimitives2 = "drawIndexedPrimitives:indexType:indexBuffer:indexBufferOffset:indirectBuffer:indirectBufferOffset:";

    /// <summary>
    /// Stores the sel set viewport state used by this instance.
    /// </summary>
    private static readonly Selector sel_setViewport = "setViewport:";

    /// <summary>
    /// Stores the sel set viewports state used by this instance.
    /// </summary>
    private static readonly Selector sel_setViewports = "setViewports:count:";

    /// <summary>
    /// Stores the sel set scissor rect state used by this instance.
    /// </summary>
    private static readonly Selector sel_setScissorRect = "setScissorRect:";

    /// <summary>
    /// Stores the sel set scissor rects state used by this instance.
    /// </summary>
    private static readonly Selector sel_setScissorRects = "setScissorRects:count:";

    /// <summary>
    /// Stores the sel set cull mode state used by this instance.
    /// </summary>
    private static readonly Selector sel_setCullMode = "setCullMode:";

    /// <summary>
    /// Stores the sel set front facing winding state used by this instance.
    /// </summary>
    private static readonly Selector sel_setFrontFacingWinding = "setFrontFacingWinding:";

    /// <summary>
    /// Stores the sel set depth stencil state value used during command execution.
    /// </summary>
    private static readonly Selector sel_setDepthStencilState = "setDepthStencilState:";

    /// <summary>
    /// Stores the sel set depth clip mode value used during command execution.
    /// </summary>
    private static readonly Selector sel_setDepthClipMode = "setDepthClipMode:";

    /// <summary>
    /// Stores the sel end encoding state used by this instance.
    /// </summary>
    private static readonly Selector sel_endEncoding = "endEncoding";

    /// <summary>
    /// Stores the sel set stencil reference value state used by this instance.
    /// </summary>
    private static readonly Selector sel_setStencilReferenceValue = "setStencilReferenceValue:";

    /// <summary>
    /// Stores the sel set blend color state used by this instance.
    /// </summary>
    private static readonly Selector sel_setBlendColor = "setBlendColorRed:green:blue:alpha:";

    /// <summary>
    /// Stores the sel set triangle fill mode state used by this instance.
    /// </summary>
    private static readonly Selector sel_setTriangleFillMode = "setTriangleFillMode:";
}
