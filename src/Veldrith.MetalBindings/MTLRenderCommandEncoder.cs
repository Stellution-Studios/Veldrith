using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLRenderCommandEncoder struct.
/// </summary>
public struct MTLRenderCommandEncoder {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderCommandEncoder" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public MTLRenderCommandEncoder(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Performs the setRenderPipelineState operation.
    /// </summary>
    /// <param name="pipelineState">The value of pipelineState.</param>
    public void setRenderPipelineState(MTLRenderPipelineState pipelineState) {
        objc_msgSend(this.NativePtr, sel_setRenderPipelineState, pipelineState.NativePtr);
    }

    /// <summary>
    /// Performs the setVertexBuffer operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="offset">The value of offset.</param>
    /// <param name="index">The value of index.</param>
    public void setVertexBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Performs the setVertexBufferOffset operation.
    /// </summary>
    /// <param name="offset">The value of offset.</param>
    /// <param name="index">The value of index.</param>
    public void setVertexBufferOffset(UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexBufferOffset, offset, index);
    }

    /// <summary>
    /// Performs the setFragmentBuffer operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="offset">The value of offset.</param>
    /// <param name="index">The value of index.</param>
    public void setFragmentBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Performs the setFragmentBufferOffset operation.
    /// </summary>
    /// <param name="offset">The value of offset.</param>
    /// <param name="index">The value of index.</param>
    public void setFragmentBufferOffset(UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentBufferOffset, offset, index);
    }

    /// <summary>
    /// Performs the setVertexTexture operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="index">The value of index.</param>
    public void setVertexTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Performs the setFragmentTexture operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="index">The value of index.</param>
    public void setFragmentTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Performs the setVertexSamplerState operation.
    /// </summary>
    /// <param name="sampler">The value of sampler.</param>
    /// <param name="index">The value of index.</param>
    public void setVertexSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Performs the setFragmentSamplerState operation.
    /// </summary>
    /// <param name="sampler">The value of sampler.</param>
    /// <param name="index">The value of index.</param>
    public void setFragmentSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Performs the drawPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">The value of primitiveType.</param>
    /// <param name="vertexStart">The value of vertexStart.</param>
    /// <param name="vertexCount">The value of vertexCount.</param>
    /// <param name="instanceCount">The value of instanceCount.</param>
    /// <param name="baseInstance">The value of baseInstance.</param>
    public void drawPrimitives(MTLPrimitiveType primitiveType, UIntPtr vertexStart, UIntPtr vertexCount, UIntPtr instanceCount, UIntPtr baseInstance) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives0, primitiveType, vertexStart, vertexCount, instanceCount, baseInstance);
    }

    /// <summary>
    /// Performs the drawPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">The value of primitiveType.</param>
    /// <param name="vertexStart">The value of vertexStart.</param>
    /// <param name="vertexCount">The value of vertexCount.</param>
    /// <param name="instanceCount">The value of instanceCount.</param>
    public void drawPrimitives(MTLPrimitiveType primitiveType, UIntPtr vertexStart, UIntPtr vertexCount, UIntPtr instanceCount) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives2, primitiveType, vertexStart, vertexCount, instanceCount);
    }

    /// <summary>
    /// Performs the drawPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">The value of primitiveType.</param>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="indirectBufferOffset">The value of indirectBufferOffset.</param>
    public void drawPrimitives(MTLPrimitiveType primitiveType, MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives1, primitiveType, indirectBuffer, indirectBufferOffset);
    }

    /// <summary>
    /// Performs the drawIndexedPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">The value of primitiveType.</param>
    /// <param name="indexCount">The value of indexCount.</param>
    /// <param name="indexType">The value of indexType.</param>
    /// <param name="indexBuffer">The value of indexBuffer.</param>
    /// <param name="indexBufferOffset">The value of indexBufferOffset.</param>
    /// <param name="instanceCount">The value of instanceCount.</param>
    public void drawIndexedPrimitives(MTLPrimitiveType primitiveType, UIntPtr indexCount, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, UIntPtr instanceCount) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives0, primitiveType, indexCount, indexType, indexBuffer.NativePtr, indexBufferOffset, instanceCount);
    }

    /// <summary>
    /// Performs the drawIndexedPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">The value of primitiveType.</param>
    /// <param name="indexCount">The value of indexCount.</param>
    /// <param name="indexType">The value of indexType.</param>
    /// <param name="indexBuffer">The value of indexBuffer.</param>
    /// <param name="indexBufferOffset">The value of indexBufferOffset.</param>
    /// <param name="instanceCount">The value of instanceCount.</param>
    /// <param name="baseVertex">The value of baseVertex.</param>
    /// <param name="baseInstance">The value of baseInstance.</param>
    public void drawIndexedPrimitives(MTLPrimitiveType primitiveType, UIntPtr indexCount, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, UIntPtr instanceCount, IntPtr baseVertex, UIntPtr baseInstance) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives1, primitiveType, indexCount, indexType, indexBuffer.NativePtr, indexBufferOffset, instanceCount, baseVertex, baseInstance);
    }

    /// <summary>
    /// Performs the drawIndexedPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">The value of primitiveType.</param>
    /// <param name="indexType">The value of indexType.</param>
    /// <param name="indexBuffer">The value of indexBuffer.</param>
    /// <param name="indexBufferOffset">The value of indexBufferOffset.</param>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="indirectBufferOffset">The value of indirectBufferOffset.</param>
    public void drawIndexedPrimitives(MTLPrimitiveType primitiveType, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives2, primitiveType, indexType, indexBuffer, indexBufferOffset, indirectBuffer, indirectBufferOffset);
    }

    /// <summary>
    /// Performs the setViewport operation.
    /// </summary>
    /// <param name="viewport">The value of viewport.</param>
    public void setViewport(MTLViewport viewport) {
        objc_msgSend(this.NativePtr, sel_setViewport, viewport);
    }

    /// <summary>
    /// Performs the setViewports operation.
    /// </summary>
    /// <param name="viewports">The value of viewports.</param>
    /// <param name="count">The value of count.</param>
    public unsafe void setViewports(MTLViewport* viewports, UIntPtr count) {
        objc_msgSend(this.NativePtr, sel_setViewports, viewports, count);
    }

    /// <summary>
    /// Performs the setScissorRect operation.
    /// </summary>
    /// <param name="scissorRect">The value of scissorRect.</param>
    public void setScissorRect(MTLScissorRect scissorRect) {
        objc_msgSend(this.NativePtr, sel_setScissorRect, scissorRect);
    }

    /// <summary>
    /// Performs the setScissorRects operation.
    /// </summary>
    /// <param name="scissorRects">The value of scissorRects.</param>
    /// <param name="count">The value of count.</param>
    public unsafe void setScissorRects(MTLScissorRect* scissorRects, UIntPtr count) {
        objc_msgSend(this.NativePtr, sel_setScissorRects, scissorRects, count);
    }

    /// <summary>
    /// Performs the setCullMode operation.
    /// </summary>
    /// <param name="cullMode">The value of cullMode.</param>
    public void setCullMode(MTLCullMode cullMode) {
        objc_msgSend(this.NativePtr, sel_setCullMode, (uint)cullMode);
    }

    /// <summary>
    /// Performs the setFrontFacing operation.
    /// </summary>
    /// <param name="frontFaceWinding">The value of frontFaceWinding.</param>
    public void setFrontFacing(MTLWinding frontFaceWinding) {
        objc_msgSend(this.NativePtr, sel_setFrontFacingWinding, (uint)frontFaceWinding);
    }

    /// <summary>
    /// Performs the setDepthStencilState operation.
    /// </summary>
    /// <param name="depthStencilState">The value of depthStencilState.</param>
    public void setDepthStencilState(MTLDepthStencilState depthStencilState) {
        objc_msgSend(this.NativePtr, sel_setDepthStencilState, depthStencilState.NativePtr);
    }

    /// <summary>
    /// Performs the setDepthClipMode operation.
    /// </summary>
    /// <param name="depthClipMode">The value of depthClipMode.</param>
    public void setDepthClipMode(MTLDepthClipMode depthClipMode) {
        objc_msgSend(this.NativePtr, sel_setDepthClipMode, (uint)depthClipMode);
    }

    /// <summary>
    /// Performs the endEncoding operation.
    /// </summary>
    public void endEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Performs the setStencilReferenceValue operation.
    /// </summary>
    /// <param name="stencilReference">The value of stencilReference.</param>
    public void setStencilReferenceValue(uint stencilReference) {
        objc_msgSend(this.NativePtr, sel_setStencilReferenceValue, stencilReference);
    }

    /// <summary>
    /// Performs the setBlendColor operation.
    /// </summary>
    /// <param name="red">The value of red.</param>
    /// <param name="green">The value of green.</param>
    /// <param name="blue">The value of blue.</param>
    /// <param name="alpha">The value of alpha.</param>
    public void setBlendColor(float red, float green, float blue, float alpha) {
        objc_msgSend(this.NativePtr, sel_setBlendColor, red, green, blue, alpha);
    }

    /// <summary>
    /// Performs the setTriangleFillMode operation.
    /// </summary>
    /// <param name="fillMode">The value of fillMode.</param>
    public void setTriangleFillMode(MTLTriangleFillMode fillMode) {
        objc_msgSend(this.NativePtr, sel_setTriangleFillMode, (uint)fillMode);
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

    /// <summary>
    /// Represents the sel_setRenderPipelineState field.
    /// </summary>
    private static readonly Selector sel_setRenderPipelineState = "setRenderPipelineState:";

    /// <summary>
    /// Represents the sel_setVertexBuffer field.
    /// </summary>
    private static readonly Selector sel_setVertexBuffer = "setVertexBuffer:offset:atIndex:";

    /// <summary>
    /// Represents the sel_setVertexBufferOffset field.
    /// </summary>
    private static readonly Selector sel_setVertexBufferOffset = "setVertexBufferOffset:atIndex:";

    /// <summary>
    /// Represents the sel_setFragmentBuffer field.
    /// </summary>
    private static readonly Selector sel_setFragmentBuffer = "setFragmentBuffer:offset:atIndex:";

    /// <summary>
    /// Represents the sel_setFragmentBufferOffset field.
    /// </summary>
    private static readonly Selector sel_setFragmentBufferOffset = "setFragmentBufferOffset:atIndex:";

    /// <summary>
    /// Represents the sel_setVertexTexture field.
    /// </summary>
    private static readonly Selector sel_setVertexTexture = "setVertexTexture:atIndex:";

    /// <summary>
    /// Represents the sel_setFragmentTexture field.
    /// </summary>
    private static readonly Selector sel_setFragmentTexture = "setFragmentTexture:atIndex:";

    /// <summary>
    /// Represents the sel_setVertexSamplerState field.
    /// </summary>
    private static readonly Selector sel_setVertexSamplerState = "setVertexSamplerState:atIndex:";

    /// <summary>
    /// Represents the sel_setFragmentSamplerState field.
    /// </summary>
    private static readonly Selector sel_setFragmentSamplerState = "setFragmentSamplerState:atIndex:";

    /// <summary>
    /// Represents the sel_drawPrimitives0 field.
    /// </summary>
    private static readonly Selector sel_drawPrimitives0 = "drawPrimitives:vertexStart:vertexCount:instanceCount:baseInstance:";

    /// <summary>
    /// Represents the sel_drawPrimitives1 field.
    /// </summary>
    private static readonly Selector sel_drawPrimitives1 = "drawPrimitives:indirectBuffer:indirectBufferOffset:";

    /// <summary>
    /// Represents the sel_drawPrimitives2 field.
    /// </summary>
    private static readonly Selector sel_drawPrimitives2 = "drawPrimitives:vertexStart:vertexCount:instanceCount:";

    /// <summary>
    /// Represents the sel_drawIndexedPrimitives0 field.
    /// </summary>
    private static readonly Selector sel_drawIndexedPrimitives0 = "drawIndexedPrimitives:indexCount:indexType:indexBuffer:indexBufferOffset:instanceCount:";

    /// <summary>
    /// Represents the sel_drawIndexedPrimitives1 field.
    /// </summary>
    private static readonly Selector sel_drawIndexedPrimitives1 = "drawIndexedPrimitives:indexCount:indexType:indexBuffer:indexBufferOffset:instanceCount:baseVertex:baseInstance:";

    /// <summary>
    /// Represents the sel_drawIndexedPrimitives2 field.
    /// </summary>
    private static readonly Selector sel_drawIndexedPrimitives2 = "drawIndexedPrimitives:indexType:indexBuffer:indexBufferOffset:indirectBuffer:indirectBufferOffset:";

    /// <summary>
    /// Represents the sel_setViewport field.
    /// </summary>
    private static readonly Selector sel_setViewport = "setViewport:";

    /// <summary>
    /// Represents the sel_setViewports field.
    /// </summary>
    private static readonly Selector sel_setViewports = "setViewports:count:";

    /// <summary>
    /// Represents the sel_setScissorRect field.
    /// </summary>
    private static readonly Selector sel_setScissorRect = "setScissorRect:";

    /// <summary>
    /// Represents the sel_setScissorRects field.
    /// </summary>
    private static readonly Selector sel_setScissorRects = "setScissorRects:count:";

    /// <summary>
    /// Represents the sel_setCullMode field.
    /// </summary>
    private static readonly Selector sel_setCullMode = "setCullMode:";

    /// <summary>
    /// Represents the sel_setFrontFacingWinding field.
    /// </summary>
    private static readonly Selector sel_setFrontFacingWinding = "setFrontFacingWinding:";

    /// <summary>
    /// Represents the sel_setDepthStencilState field.
    /// </summary>
    private static readonly Selector sel_setDepthStencilState = "setDepthStencilState:";

    /// <summary>
    /// Represents the sel_setDepthClipMode field.
    /// </summary>
    private static readonly Selector sel_setDepthClipMode = "setDepthClipMode:";

    /// <summary>
    /// Represents the sel_endEncoding field.
    /// </summary>
    private static readonly Selector sel_endEncoding = "endEncoding";

    /// <summary>
    /// Represents the sel_setStencilReferenceValue field.
    /// </summary>
    private static readonly Selector sel_setStencilReferenceValue = "setStencilReferenceValue:";

    /// <summary>
    /// Represents the sel_setBlendColor field.
    /// </summary>
    private static readonly Selector sel_setBlendColor = "setBlendColorRed:green:blue:alpha:";

    /// <summary>
    /// Represents the sel_setTriangleFillMode field.
    /// </summary>
    private static readonly Selector sel_setTriangleFillMode = "setTriangleFillMode:";
}