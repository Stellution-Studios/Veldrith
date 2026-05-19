using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLRenderPipelineDescriptor struct.
/// </summary>
public struct MTLRenderPipelineDescriptor {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderPipelineDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public MTLRenderPipelineDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>Returns the result produced by the New operation.</returns>
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
    /// Stores the value associated with <c>sel_vertexFunction</c>.
    /// </summary>
    private static readonly Selector sel_vertexFunction = "vertexFunction";

    /// <summary>
    /// Stores the value associated with <c>sel_setVertexFunction</c>.
    /// </summary>
    private static readonly Selector sel_setVertexFunction = "setVertexFunction:";

    /// <summary>
    /// Stores the value associated with <c>sel_fragmentFunction</c>.
    /// </summary>
    private static readonly Selector sel_fragmentFunction = "fragmentFunction";

    /// <summary>
    /// Stores the value associated with <c>sel_setFragmentFunction</c>.
    /// </summary>
    private static readonly Selector sel_setFragmentFunction = "setFragmentFunction:";

    /// <summary>
    /// Stores the value associated with <c>sel_colorAttachments</c>.
    /// </summary>
    private static readonly Selector sel_colorAttachments = "colorAttachments";

    /// <summary>
    /// Stores the value associated with <c>sel_depthAttachmentPixelFormat</c>.
    /// </summary>
    private static readonly Selector sel_depthAttachmentPixelFormat = "depthAttachmentPixelFormat";

    /// <summary>
    /// Stores the value associated with <c>sel_setDepthAttachmentPixelFormat</c>.
    /// </summary>
    private static readonly Selector sel_setDepthAttachmentPixelFormat = "setDepthAttachmentPixelFormat:";

    /// <summary>
    /// Stores the value associated with <c>sel_stencilAttachmentPixelFormat</c>.
    /// </summary>
    private static readonly Selector sel_stencilAttachmentPixelFormat = "stencilAttachmentPixelFormat";

    /// <summary>
    /// Stores the value associated with <c>sel_setStencilAttachmentPixelFormat</c>.
    /// </summary>
    private static readonly Selector sel_setStencilAttachmentPixelFormat = "setStencilAttachmentPixelFormat:";

    /// <summary>
    /// Stores the value associated with <c>sel_sampleCount</c>.
    /// </summary>
    private static readonly Selector sel_sampleCount = "sampleCount";

    /// <summary>
    /// Stores the value associated with <c>sel_setSampleCount</c>.
    /// </summary>
    private static readonly Selector sel_setSampleCount = "setSampleCount:";

    /// <summary>
    /// Stores the value associated with <c>sel_vertexDescriptor</c>.
    /// </summary>
    private static readonly Selector sel_vertexDescriptor = "vertexDescriptor";

    /// <summary>
    /// Stores the value associated with <c>sel_isAlphaToCoverageEnabled</c>.
    /// </summary>
    private static readonly Selector sel_isAlphaToCoverageEnabled = "isAlphaToCoverageEnabled";

    /// <summary>
    /// Stores the value associated with <c>sel_setAlphaToCoverageEnabled</c>.
    /// </summary>
    private static readonly Selector sel_setAlphaToCoverageEnabled = "setAlphaToCoverageEnabled:";
}