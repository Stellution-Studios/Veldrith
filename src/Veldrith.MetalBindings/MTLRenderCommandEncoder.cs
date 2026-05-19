using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLRenderCommandEncoder struct.
/// </summary>
public struct MTLRenderCommandEncoder {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderCommandEncoder" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public MTLRenderCommandEncoder(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Executes the setRenderPipelineState operation.
    /// </summary>
    /// <param name="pipelineState">Specifies the value of <paramref name="pipelineState" />.</param>
    public void setRenderPipelineState(MTLRenderPipelineState pipelineState) {
        objc_msgSend(this.NativePtr, sel_setRenderPipelineState, pipelineState.NativePtr);
    }

    /// <summary>
    /// Executes the setVertexBuffer operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void setVertexBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Executes the setVertexBufferOffset operation.
    /// </summary>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void setVertexBufferOffset(UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexBufferOffset, offset, index);
    }

    /// <summary>
    /// Executes the setFragmentBuffer operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void setFragmentBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Executes the setFragmentBufferOffset operation.
    /// </summary>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void setFragmentBufferOffset(UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentBufferOffset, offset, index);
    }

    /// <summary>
    /// Executes the setVertexTexture operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void setVertexTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Executes the setFragmentTexture operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void setFragmentTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Executes the setVertexSamplerState operation.
    /// </summary>
    /// <param name="sampler">Specifies the value of <paramref name="sampler" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void setVertexSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Executes the setFragmentSamplerState operation.
    /// </summary>
    /// <param name="sampler">Specifies the value of <paramref name="sampler" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public void setFragmentSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Executes the drawPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">Specifies the value of <paramref name="primitiveType" />.</param>
    /// <param name="vertexStart">Specifies the value of <paramref name="vertexStart" />.</param>
    /// <param name="vertexCount">Specifies the value of <paramref name="vertexCount" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    /// <param name="baseInstance">Specifies the value of <paramref name="baseInstance" />.</param>
    public void drawPrimitives(MTLPrimitiveType primitiveType, UIntPtr vertexStart, UIntPtr vertexCount, UIntPtr instanceCount, UIntPtr baseInstance) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives0, primitiveType, vertexStart, vertexCount, instanceCount, baseInstance);
    }

    /// <summary>
    /// Executes the drawPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">Specifies the value of <paramref name="primitiveType" />.</param>
    /// <param name="vertexStart">Specifies the value of <paramref name="vertexStart" />.</param>
    /// <param name="vertexCount">Specifies the value of <paramref name="vertexCount" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    public void drawPrimitives(MTLPrimitiveType primitiveType, UIntPtr vertexStart, UIntPtr vertexCount, UIntPtr instanceCount) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives2, primitiveType, vertexStart, vertexCount, instanceCount);
    }

    /// <summary>
    /// Executes the drawPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">Specifies the value of <paramref name="primitiveType" />.</param>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="indirectBufferOffset">Specifies the value of <paramref name="indirectBufferOffset" />.</param>
    public void drawPrimitives(MTLPrimitiveType primitiveType, MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives1, primitiveType, indirectBuffer, indirectBufferOffset);
    }

    /// <summary>
    /// Executes the drawIndexedPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">Specifies the value of <paramref name="primitiveType" />.</param>
    /// <param name="indexCount">Specifies the value of <paramref name="indexCount" />.</param>
    /// <param name="indexType">Specifies the value of <paramref name="indexType" />.</param>
    /// <param name="indexBuffer">Specifies the value of <paramref name="indexBuffer" />.</param>
    /// <param name="indexBufferOffset">Specifies the value of <paramref name="indexBufferOffset" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    public void drawIndexedPrimitives(MTLPrimitiveType primitiveType, UIntPtr indexCount, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, UIntPtr instanceCount) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives0, primitiveType, indexCount, indexType, indexBuffer.NativePtr, indexBufferOffset, instanceCount);
    }

    /// <summary>
    /// Executes the drawIndexedPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">Specifies the value of <paramref name="primitiveType" />.</param>
    /// <param name="indexCount">Specifies the value of <paramref name="indexCount" />.</param>
    /// <param name="indexType">Specifies the value of <paramref name="indexType" />.</param>
    /// <param name="indexBuffer">Specifies the value of <paramref name="indexBuffer" />.</param>
    /// <param name="indexBufferOffset">Specifies the value of <paramref name="indexBufferOffset" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    /// <param name="baseVertex">Specifies the value of <paramref name="baseVertex" />.</param>
    /// <param name="baseInstance">Specifies the value of <paramref name="baseInstance" />.</param>
    public void drawIndexedPrimitives(MTLPrimitiveType primitiveType, UIntPtr indexCount, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, UIntPtr instanceCount, IntPtr baseVertex, UIntPtr baseInstance) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives1, primitiveType, indexCount, indexType, indexBuffer.NativePtr, indexBufferOffset, instanceCount, baseVertex, baseInstance);
    }

    /// <summary>
    /// Executes the drawIndexedPrimitives operation.
    /// </summary>
    /// <param name="primitiveType">Specifies the value of <paramref name="primitiveType" />.</param>
    /// <param name="indexType">Specifies the value of <paramref name="indexType" />.</param>
    /// <param name="indexBuffer">Specifies the value of <paramref name="indexBuffer" />.</param>
    /// <param name="indexBufferOffset">Specifies the value of <paramref name="indexBufferOffset" />.</param>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="indirectBufferOffset">Specifies the value of <paramref name="indirectBufferOffset" />.</param>
    public void drawIndexedPrimitives(MTLPrimitiveType primitiveType, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives2, primitiveType, indexType, indexBuffer, indexBufferOffset, indirectBuffer, indirectBufferOffset);
    }

    /// <summary>
    /// Executes the setViewport operation.
    /// </summary>
    /// <param name="viewport">Specifies the value of <paramref name="viewport" />.</param>
    public void setViewport(MTLViewport viewport) {
        objc_msgSend(this.NativePtr, sel_setViewport, viewport);
    }

    /// <summary>
    /// Executes the setViewports operation.
    /// </summary>
    /// <param name="viewports">Specifies the value of <paramref name="viewports" />.</param>
    /// <param name="count">Specifies the value of <paramref name="count" />.</param>
    public unsafe void setViewports(MTLViewport* viewports, UIntPtr count) {
        objc_msgSend(this.NativePtr, sel_setViewports, viewports, count);
    }

    /// <summary>
    /// Executes the setScissorRect operation.
    /// </summary>
    /// <param name="scissorRect">Specifies the value of <paramref name="scissorRect" />.</param>
    public void setScissorRect(MTLScissorRect scissorRect) {
        objc_msgSend(this.NativePtr, sel_setScissorRect, scissorRect);
    }

    /// <summary>
    /// Executes the setScissorRects operation.
    /// </summary>
    /// <param name="scissorRects">Specifies the value of <paramref name="scissorRects" />.</param>
    /// <param name="count">Specifies the value of <paramref name="count" />.</param>
    public unsafe void setScissorRects(MTLScissorRect* scissorRects, UIntPtr count) {
        objc_msgSend(this.NativePtr, sel_setScissorRects, scissorRects, count);
    }

    /// <summary>
    /// Executes the setCullMode operation.
    /// </summary>
    /// <param name="cullMode">Specifies the value of <paramref name="cullMode" />.</param>
    public void setCullMode(MTLCullMode cullMode) {
        objc_msgSend(this.NativePtr, sel_setCullMode, (uint)cullMode);
    }

    /// <summary>
    /// Executes the setFrontFacing operation.
    /// </summary>
    /// <param name="frontFaceWinding">Specifies the value of <paramref name="frontFaceWinding" />.</param>
    public void setFrontFacing(MTLWinding frontFaceWinding) {
        objc_msgSend(this.NativePtr, sel_setFrontFacingWinding, (uint)frontFaceWinding);
    }

    /// <summary>
    /// Executes the setDepthStencilState operation.
    /// </summary>
    /// <param name="depthStencilState">Specifies the value of <paramref name="depthStencilState" />.</param>
    public void setDepthStencilState(MTLDepthStencilState depthStencilState) {
        objc_msgSend(this.NativePtr, sel_setDepthStencilState, depthStencilState.NativePtr);
    }

    /// <summary>
    /// Executes the setDepthClipMode operation.
    /// </summary>
    /// <param name="depthClipMode">Specifies the value of <paramref name="depthClipMode" />.</param>
    public void setDepthClipMode(MTLDepthClipMode depthClipMode) {
        objc_msgSend(this.NativePtr, sel_setDepthClipMode, (uint)depthClipMode);
    }

    /// <summary>
    /// Executes the endEncoding operation.
    /// </summary>
    public void endEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Executes the setStencilReferenceValue operation.
    /// </summary>
    /// <param name="stencilReference">Specifies the value of <paramref name="stencilReference" />.</param>
    public void setStencilReferenceValue(uint stencilReference) {
        objc_msgSend(this.NativePtr, sel_setStencilReferenceValue, stencilReference);
    }

    /// <summary>
    /// Executes the setBlendColor operation.
    /// </summary>
    /// <param name="red">Specifies the value of <paramref name="red" />.</param>
    /// <param name="green">Specifies the value of <paramref name="green" />.</param>
    /// <param name="blue">Specifies the value of <paramref name="blue" />.</param>
    /// <param name="alpha">Specifies the value of <paramref name="alpha" />.</param>
    public void setBlendColor(float red, float green, float blue, float alpha) {
        objc_msgSend(this.NativePtr, sel_setBlendColor, red, green, blue, alpha);
    }

    /// <summary>
    /// Executes the setTriangleFillMode operation.
    /// </summary>
    /// <param name="fillMode">Specifies the value of <paramref name="fillMode" />.</param>
    public void setTriangleFillMode(MTLTriangleFillMode fillMode) {
        objc_msgSend(this.NativePtr, sel_setTriangleFillMode, (uint)fillMode);
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

    /// <summary>
    /// Stores the value associated with <c>sel_setRenderPipelineState</c>.
    /// </summary>
    private static readonly Selector sel_setRenderPipelineState = "setRenderPipelineState:";

    /// <summary>
    /// Stores the value associated with <c>sel_setVertexBuffer</c>.
    /// </summary>
    private static readonly Selector sel_setVertexBuffer = "setVertexBuffer:offset:atIndex:";

    /// <summary>
    /// Stores the value associated with <c>sel_setVertexBufferOffset</c>.
    /// </summary>
    private static readonly Selector sel_setVertexBufferOffset = "setVertexBufferOffset:atIndex:";

    /// <summary>
    /// Stores the value associated with <c>sel_setFragmentBuffer</c>.
    /// </summary>
    private static readonly Selector sel_setFragmentBuffer = "setFragmentBuffer:offset:atIndex:";

    /// <summary>
    /// Stores the value associated with <c>sel_setFragmentBufferOffset</c>.
    /// </summary>
    private static readonly Selector sel_setFragmentBufferOffset = "setFragmentBufferOffset:atIndex:";

    /// <summary>
    /// Stores the value associated with <c>sel_setVertexTexture</c>.
    /// </summary>
    private static readonly Selector sel_setVertexTexture = "setVertexTexture:atIndex:";

    /// <summary>
    /// Stores the value associated with <c>sel_setFragmentTexture</c>.
    /// </summary>
    private static readonly Selector sel_setFragmentTexture = "setFragmentTexture:atIndex:";

    /// <summary>
    /// Stores the value associated with <c>sel_setVertexSamplerState</c>.
    /// </summary>
    private static readonly Selector sel_setVertexSamplerState = "setVertexSamplerState:atIndex:";

    /// <summary>
    /// Stores the value associated with <c>sel_setFragmentSamplerState</c>.
    /// </summary>
    private static readonly Selector sel_setFragmentSamplerState = "setFragmentSamplerState:atIndex:";

    /// <summary>
    /// Stores the value associated with <c>sel_drawPrimitives0</c>.
    /// </summary>
    private static readonly Selector sel_drawPrimitives0 = "drawPrimitives:vertexStart:vertexCount:instanceCount:baseInstance:";

    /// <summary>
    /// Stores the value associated with <c>sel_drawPrimitives1</c>.
    /// </summary>
    private static readonly Selector sel_drawPrimitives1 = "drawPrimitives:indirectBuffer:indirectBufferOffset:";

    /// <summary>
    /// Stores the value associated with <c>sel_drawPrimitives2</c>.
    /// </summary>
    private static readonly Selector sel_drawPrimitives2 = "drawPrimitives:vertexStart:vertexCount:instanceCount:";

    /// <summary>
    /// Stores the value associated with <c>sel_drawIndexedPrimitives0</c>.
    /// </summary>
    private static readonly Selector sel_drawIndexedPrimitives0 = "drawIndexedPrimitives:indexCount:indexType:indexBuffer:indexBufferOffset:instanceCount:";

    /// <summary>
    /// Stores the value associated with <c>sel_drawIndexedPrimitives1</c>.
    /// </summary>
    private static readonly Selector sel_drawIndexedPrimitives1 = "drawIndexedPrimitives:indexCount:indexType:indexBuffer:indexBufferOffset:instanceCount:baseVertex:baseInstance:";

    /// <summary>
    /// Stores the value associated with <c>sel_drawIndexedPrimitives2</c>.
    /// </summary>
    private static readonly Selector sel_drawIndexedPrimitives2 = "drawIndexedPrimitives:indexType:indexBuffer:indexBufferOffset:indirectBuffer:indirectBufferOffset:";

    /// <summary>
    /// Stores the value associated with <c>sel_setViewport</c>.
    /// </summary>
    private static readonly Selector sel_setViewport = "setViewport:";

    /// <summary>
    /// Stores the value associated with <c>sel_setViewports</c>.
    /// </summary>
    private static readonly Selector sel_setViewports = "setViewports:count:";

    /// <summary>
    /// Stores the value associated with <c>sel_setScissorRect</c>.
    /// </summary>
    private static readonly Selector sel_setScissorRect = "setScissorRect:";

    /// <summary>
    /// Stores the value associated with <c>sel_setScissorRects</c>.
    /// </summary>
    private static readonly Selector sel_setScissorRects = "setScissorRects:count:";

    /// <summary>
    /// Stores the value associated with <c>sel_setCullMode</c>.
    /// </summary>
    private static readonly Selector sel_setCullMode = "setCullMode:";

    /// <summary>
    /// Stores the value associated with <c>sel_setFrontFacingWinding</c>.
    /// </summary>
    private static readonly Selector sel_setFrontFacingWinding = "setFrontFacingWinding:";

    /// <summary>
    /// Stores the value associated with <c>sel_setDepthStencilState</c>.
    /// </summary>
    private static readonly Selector sel_setDepthStencilState = "setDepthStencilState:";

    /// <summary>
    /// Stores the value associated with <c>sel_setDepthClipMode</c>.
    /// </summary>
    private static readonly Selector sel_setDepthClipMode = "setDepthClipMode:";

    /// <summary>
    /// Stores the value associated with <c>sel_endEncoding</c>.
    /// </summary>
    private static readonly Selector sel_endEncoding = "endEncoding";

    /// <summary>
    /// Stores the value associated with <c>sel_setStencilReferenceValue</c>.
    /// </summary>
    private static readonly Selector sel_setStencilReferenceValue = "setStencilReferenceValue:";

    /// <summary>
    /// Stores the value associated with <c>sel_setBlendColor</c>.
    /// </summary>
    private static readonly Selector sel_setBlendColor = "setBlendColorRed:green:blue:alpha:";

    /// <summary>
    /// Stores the value associated with <c>sel_setTriangleFillMode</c>.
    /// </summary>
    private static readonly Selector sel_setTriangleFillMode = "setTriangleFillMode:";
}