using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLRenderPipelineDescriptor struct.
/// </summary>
public struct MTLRenderPipelineDescriptor {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderPipelineDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public MTLRenderPipelineDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Performs the New operation.
    /// </summary>
    /// <returns>The result of the New operation.</returns>
    public static MTLRenderPipelineDescriptor New() {
        ObjCClass cls = new("MTLRenderPipelineDescriptor");
        MTLRenderPipelineDescriptor ret = cls.AllocInit<MTLRenderPipelineDescriptor>();
        return ret;
    }

    /// <summary>
    /// Gets or sets vertexFunction.
    /// </summary>
    public MTLFunction vertexFunction {
        get => objc_msgSend<MTLFunction>(this.NativePtr, sel_vertexFunction);
        set => objc_msgSend(this.NativePtr, sel_setVertexFunction, value.NativePtr);
    }

    /// <summary>
    /// Gets or sets fragmentFunction.
    /// </summary>
    public MTLFunction fragmentFunction {
        get => objc_msgSend<MTLFunction>(this.NativePtr, sel_fragmentFunction);
        set => objc_msgSend(this.NativePtr, sel_setFragmentFunction, value.NativePtr);
    }

    public MTLRenderPipelineColorAttachmentDescriptorArray colorAttachments
        => objc_msgSend<MTLRenderPipelineColorAttachmentDescriptorArray>(this.NativePtr, sel_colorAttachments);

    /// <summary>
    /// Gets or sets depthAttachmentPixelFormat.
    /// </summary>
    public MTLPixelFormat depthAttachmentPixelFormat {
        get => (MTLPixelFormat)uint_objc_msgSend(this.NativePtr, sel_depthAttachmentPixelFormat);
        set => objc_msgSend(this.NativePtr, sel_setDepthAttachmentPixelFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets stencilAttachmentPixelFormat.
    /// </summary>
    public MTLPixelFormat stencilAttachmentPixelFormat {
        get => (MTLPixelFormat)uint_objc_msgSend(this.NativePtr, sel_stencilAttachmentPixelFormat);
        set => objc_msgSend(this.NativePtr, sel_setStencilAttachmentPixelFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets sampleCount.
    /// </summary>
    public UIntPtr sampleCount {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_sampleCount);
        set => objc_msgSend(this.NativePtr, sel_setSampleCount, value);
    }

    /// <summary>
    /// Gets or sets vertexDescriptor.
    /// </summary>
    public MTLVertexDescriptor vertexDescriptor => objc_msgSend<MTLVertexDescriptor>(this.NativePtr, sel_vertexDescriptor);

    /// <summary>
    /// Gets or sets alphaToCoverageEnabled.
    /// </summary>
    public Bool8 alphaToCoverageEnabled {
        get => bool8_objc_msgSend(this.NativePtr, sel_isAlphaToCoverageEnabled);
        set => objc_msgSend(this.NativePtr, sel_setAlphaToCoverageEnabled, value);
    }

    /// <summary>
    /// Represents the sel_vertexFunction field.
    /// </summary>
    private static readonly Selector sel_vertexFunction = "vertexFunction";

    /// <summary>
    /// Represents the sel_setVertexFunction field.
    /// </summary>
    private static readonly Selector sel_setVertexFunction = "setVertexFunction:";

    /// <summary>
    /// Represents the sel_fragmentFunction field.
    /// </summary>
    private static readonly Selector sel_fragmentFunction = "fragmentFunction";

    /// <summary>
    /// Represents the sel_setFragmentFunction field.
    /// </summary>
    private static readonly Selector sel_setFragmentFunction = "setFragmentFunction:";

    /// <summary>
    /// Represents the sel_colorAttachments field.
    /// </summary>
    private static readonly Selector sel_colorAttachments = "colorAttachments";

    /// <summary>
    /// Represents the sel_depthAttachmentPixelFormat field.
    /// </summary>
    private static readonly Selector sel_depthAttachmentPixelFormat = "depthAttachmentPixelFormat";

    /// <summary>
    /// Represents the sel_setDepthAttachmentPixelFormat field.
    /// </summary>
    private static readonly Selector sel_setDepthAttachmentPixelFormat = "setDepthAttachmentPixelFormat:";

    /// <summary>
    /// Represents the sel_stencilAttachmentPixelFormat field.
    /// </summary>
    private static readonly Selector sel_stencilAttachmentPixelFormat = "stencilAttachmentPixelFormat";

    /// <summary>
    /// Represents the sel_setStencilAttachmentPixelFormat field.
    /// </summary>
    private static readonly Selector sel_setStencilAttachmentPixelFormat = "setStencilAttachmentPixelFormat:";

    /// <summary>
    /// Represents the sel_sampleCount field.
    /// </summary>
    private static readonly Selector sel_sampleCount = "sampleCount";

    /// <summary>
    /// Represents the sel_setSampleCount field.
    /// </summary>
    private static readonly Selector sel_setSampleCount = "setSampleCount:";

    /// <summary>
    /// Represents the sel_vertexDescriptor field.
    /// </summary>
    private static readonly Selector sel_vertexDescriptor = "vertexDescriptor";

    /// <summary>
    /// Represents the sel_isAlphaToCoverageEnabled field.
    /// </summary>
    private static readonly Selector sel_isAlphaToCoverageEnabled = "isAlphaToCoverageEnabled";

    /// <summary>
    /// Represents the sel_setAlphaToCoverageEnabled field.
    /// </summary>
    private static readonly Selector sel_setAlphaToCoverageEnabled = "setAlphaToCoverageEnabled:";
}