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
    /// Initializes a new instance of the <see cref="MTLRenderCommandEncoder" /> class.
    /// </summary>
    public MTLRenderCommandEncoder(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Executes setRenderPipelineState.
    /// </summary>
    public void setRenderPipelineState(MTLRenderPipelineState pipelineState) {
        objc_msgSend(this.NativePtr, sel_setRenderPipelineState, pipelineState.NativePtr);
    }

    /// <summary>
    /// Executes setVertexBuffer.
    /// </summary>
    public void setVertexBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Executes setVertexBufferOffset.
    /// </summary>
    public void setVertexBufferOffset(UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexBufferOffset, offset, index);
    }

    /// <summary>
    /// Executes setFragmentBuffer.
    /// </summary>
    public void setFragmentBuffer(MTLBuffer buffer, UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentBuffer, buffer.NativePtr, offset, index);
    }

    /// <summary>
    /// Executes setFragmentBufferOffset.
    /// </summary>
    public void setFragmentBufferOffset(UIntPtr offset, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentBufferOffset, offset, index);
    }

    /// <summary>
    /// Executes setVertexTexture.
    /// </summary>
    public void setVertexTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Executes setFragmentTexture.
    /// </summary>
    public void setFragmentTexture(MTLTexture texture, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentTexture, texture.NativePtr, index);
    }

    /// <summary>
    /// Executes setVertexSamplerState.
    /// </summary>
    public void setVertexSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setVertexSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Executes setFragmentSamplerState.
    /// </summary>
    public void setFragmentSamplerState(MTLSamplerState sampler, UIntPtr index) {
        objc_msgSend(this.NativePtr, sel_setFragmentSamplerState, sampler.NativePtr, index);
    }

    /// <summary>
    /// Executes drawPrimitives.
    /// </summary>
    public void drawPrimitives(MTLPrimitiveType primitiveType, UIntPtr vertexStart, UIntPtr vertexCount, UIntPtr instanceCount, UIntPtr baseInstance) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives0, primitiveType, vertexStart, vertexCount, instanceCount, baseInstance);
    }

    /// <summary>
    /// Executes drawPrimitives.
    /// </summary>
    public void drawPrimitives(MTLPrimitiveType primitiveType, UIntPtr vertexStart, UIntPtr vertexCount, UIntPtr instanceCount) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives2, primitiveType, vertexStart, vertexCount, instanceCount);
    }

    /// <summary>
    /// Executes drawPrimitives.
    /// </summary>
    public void drawPrimitives(MTLPrimitiveType primitiveType, MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset) {
        objc_msgSend(this.NativePtr, sel_drawPrimitives1, primitiveType, indirectBuffer, indirectBufferOffset);
    }

    /// <summary>
    /// Executes drawIndexedPrimitives.
    /// </summary>
    public void drawIndexedPrimitives(MTLPrimitiveType primitiveType, UIntPtr indexCount, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, UIntPtr instanceCount) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives0, primitiveType, indexCount, indexType, indexBuffer.NativePtr, indexBufferOffset, instanceCount);
    }

    /// <summary>
    /// Executes drawIndexedPrimitives.
    /// </summary>
    public void drawIndexedPrimitives(MTLPrimitiveType primitiveType, UIntPtr indexCount, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, UIntPtr instanceCount, IntPtr baseVertex, UIntPtr baseInstance) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives1, primitiveType, indexCount, indexType, indexBuffer.NativePtr, indexBufferOffset, instanceCount, baseVertex, baseInstance);
    }

    /// <summary>
    /// Executes drawIndexedPrimitives.
    /// </summary>
    public void drawIndexedPrimitives(MTLPrimitiveType primitiveType, MTLIndexType indexType, MTLBuffer indexBuffer, UIntPtr indexBufferOffset, MTLBuffer indirectBuffer, UIntPtr indirectBufferOffset) {
        objc_msgSend(this.NativePtr, sel_drawIndexedPrimitives2, primitiveType, indexType, indexBuffer, indexBufferOffset, indirectBuffer, indirectBufferOffset);
    }

    /// <summary>
    /// Executes setViewport.
    /// </summary>
    public void setViewport(MTLViewport viewport) {
        objc_msgSend(this.NativePtr, sel_setViewport, viewport);
    }

    /// <summary>
    /// Executes setViewports.
    /// </summary>
    public unsafe void setViewports(MTLViewport* viewports, UIntPtr count) {
        objc_msgSend(this.NativePtr, sel_setViewports, viewports, count);
    }

    /// <summary>
    /// Executes setScissorRect.
    /// </summary>
    public void setScissorRect(MTLScissorRect scissorRect) {
        objc_msgSend(this.NativePtr, sel_setScissorRect, scissorRect);
    }

    /// <summary>
    /// Executes setScissorRects.
    /// </summary>
    public unsafe void setScissorRects(MTLScissorRect* scissorRects, UIntPtr count) {
        objc_msgSend(this.NativePtr, sel_setScissorRects, scissorRects, count);
    }

    /// <summary>
    /// Executes setCullMode.
    /// </summary>
    public void setCullMode(MTLCullMode cullMode) {
        objc_msgSend(this.NativePtr, sel_setCullMode, (uint)cullMode);
    }

    /// <summary>
    /// Executes setFrontFacing.
    /// </summary>
    public void setFrontFacing(MTLWinding frontFaceWinding) {
        objc_msgSend(this.NativePtr, sel_setFrontFacingWinding, (uint)frontFaceWinding);
    }

    /// <summary>
    /// Executes setDepthStencilState.
    /// </summary>
    public void setDepthStencilState(MTLDepthStencilState depthStencilState) {
        objc_msgSend(this.NativePtr, sel_setDepthStencilState, depthStencilState.NativePtr);
    }

    /// <summary>
    /// Executes setDepthClipMode.
    /// </summary>
    public void setDepthClipMode(MTLDepthClipMode depthClipMode) {
        objc_msgSend(this.NativePtr, sel_setDepthClipMode, (uint)depthClipMode);
    }

    /// <summary>
    /// Executes endEncoding.
    /// </summary>
    public void endEncoding() {
        objc_msgSend(this.NativePtr, sel_endEncoding);
    }

    /// <summary>
    /// Executes setStencilReferenceValue.
    /// </summary>
    public void setStencilReferenceValue(uint stencilReference) {
        objc_msgSend(this.NativePtr, sel_setStencilReferenceValue, stencilReference);
    }

    /// <summary>
    /// Executes setBlendColor.
    /// </summary>
    public void setBlendColor(float red, float green, float blue, float alpha) {
        objc_msgSend(this.NativePtr, sel_setBlendColor, red, green, blue, alpha);
    }

    /// <summary>
    /// Executes setTriangleFillMode.
    /// </summary>
    public void setTriangleFillMode(MTLTriangleFillMode fillMode) {
        objc_msgSend(this.NativePtr, sel_setTriangleFillMode, (uint)fillMode);
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