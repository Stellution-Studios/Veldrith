using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRenderPipelineDescriptor data structure used by the graphics runtime.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MTLRenderPipelineDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderPipelineDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLRenderPipelineDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static MTLRenderPipelineDescriptor New() {
        ObjCClass cls = new("MTLRenderPipelineDescriptor");
        MTLRenderPipelineDescriptor ret = cls.AllocInit<MTLRenderPipelineDescriptor>();
        return ret;
    }

    /// <summary>
    /// Gets or sets vertexFunction.
    /// </summary>
    public MTLFunction VertexFunction {
        get => objc_msgSend<MTLFunction>(this.NativePtr, sel_vertexFunction);
        set => objc_msgSend(this.NativePtr, sel_setVertexFunction, value.NativePtr);
    }

    /// <summary>
    /// Gets or sets fragmentFunction.
    /// </summary>
    public MTLFunction FragmentFunction {
        get => objc_msgSend<MTLFunction>(this.NativePtr, sel_fragmentFunction);
        set => objc_msgSend(this.NativePtr, sel_setFragmentFunction, value.NativePtr);
    }

    public MTLRenderPipelineColorAttachmentDescriptorArray ColorAttachments => objc_msgSend<MTLRenderPipelineColorAttachmentDescriptorArray>(this.NativePtr, sel_colorAttachments);

    /// <summary>
    /// Gets or sets depthAttachmentPixelFormat.
    /// </summary>
    public MTLPixelFormat DepthAttachmentPixelFormat {
        get => (MTLPixelFormat)uint_objc_msgSend(this.NativePtr, sel_depthAttachmentPixelFormat);
        set => objc_msgSend(this.NativePtr, sel_setDepthAttachmentPixelFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets stencilAttachmentPixelFormat.
    /// </summary>
    public MTLPixelFormat StencilAttachmentPixelFormat {
        get => (MTLPixelFormat)uint_objc_msgSend(this.NativePtr, sel_stencilAttachmentPixelFormat);
        set => objc_msgSend(this.NativePtr, sel_setStencilAttachmentPixelFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets sampleCount.
    /// </summary>
    public UIntPtr SampleCount {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_sampleCount);
        set => objc_msgSend(this.NativePtr, sel_setSampleCount, value);
    }

    /// <summary>
    /// Gets or sets vertexDescriptor.
    /// </summary>

    public MTLVertexDescriptor VertexDescriptor => objc_msgSend<MTLVertexDescriptor>(this.NativePtr, sel_vertexDescriptor);

    /// <summary>
    /// Gets or sets alphaToCoverageEnabled.
    /// </summary>
    public Bool8 AlphaToCoverageEnabled {
        get => bool8_objc_msgSend(this.NativePtr, sel_isAlphaToCoverageEnabled);
        set => objc_msgSend(this.NativePtr, sel_setAlphaToCoverageEnabled, value);
    }

    /// <summary>
    /// Stores the sel vertex function state used by this instance.
    /// </summary>
    private static readonly Selector sel_vertexFunction = "vertexFunction";

    /// <summary>
    /// Stores the sel set vertex function state used by this instance.
    /// </summary>
    private static readonly Selector sel_setVertexFunction = "setVertexFunction:";

    /// <summary>
    /// Stores the sel fragment function state used by this instance.
    /// </summary>
    private static readonly Selector sel_fragmentFunction = "fragmentFunction";

    /// <summary>
    /// Stores the sel set fragment function state used by this instance.
    /// </summary>
    private static readonly Selector sel_setFragmentFunction = "setFragmentFunction:";

    /// <summary>
    /// Stores the sel color attachments state used by this instance.
    /// </summary>
    private static readonly Selector sel_colorAttachments = "colorAttachments";

    /// <summary>
    /// Stores the sel depth attachment pixel format value used during command execution.
    /// </summary>
    private static readonly Selector sel_depthAttachmentPixelFormat = "depthAttachmentPixelFormat";

    /// <summary>
    /// Stores the sel set depth attachment pixel format value used during command execution.
    /// </summary>
    private static readonly Selector sel_setDepthAttachmentPixelFormat = "setDepthAttachmentPixelFormat:";

    /// <summary>
    /// Stores the sel stencil attachment pixel format state used by this instance.
    /// </summary>
    private static readonly Selector sel_stencilAttachmentPixelFormat = "stencilAttachmentPixelFormat";

    /// <summary>
    /// Stores the sel set stencil attachment pixel format state used by this instance.
    /// </summary>
    private static readonly Selector sel_setStencilAttachmentPixelFormat = "setStencilAttachmentPixelFormat:";

    /// <summary>
    /// Stores the sel sample count value used during command execution.
    /// </summary>
    private static readonly Selector sel_sampleCount = "sampleCount";

    /// <summary>
    /// Stores the sel set sample count value used during command execution.
    /// </summary>
    private static readonly Selector sel_setSampleCount = "setSampleCount:";

    /// <summary>
    /// Stores the sel vertex descriptor state used by this instance.
    /// </summary>
    private static readonly Selector sel_vertexDescriptor = "vertexDescriptor";

    /// <summary>
    /// Stores the sel is Alpha to coverage enabled state used by this instance.
    /// </summary>
    private static readonly Selector sel_isAlphaToCoverageEnabled = "isAlphaToCoverageEnabled";

    /// <summary>
    /// Stores the sel set Alpha to coverage enabled state used by this instance.
    /// </summary>
    private static readonly Selector sel_setAlphaToCoverageEnabled = "setAlphaToCoverageEnabled:";
}