using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLRenderPipelineColorAttachmentDescriptor struct.
/// </summary>
public struct MTLRenderPipelineColorAttachmentDescriptor {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderPipelineColorAttachmentDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public MTLRenderPipelineColorAttachmentDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets pixelFormat.
    /// </summary>
    public MTLPixelFormat pixelFormat {
        get => (MTLPixelFormat)uint_objc_msgSend(this.NativePtr, Selectors.pixelFormat);
        set => objc_msgSend(this.NativePtr, Selectors.setPixelFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets writeMask.
    /// </summary>
    public MTLColorWriteMask writeMask {
        get => (MTLColorWriteMask)uint_objc_msgSend(this.NativePtr, sel_writeMask);
        set => objc_msgSend(this.NativePtr, sel_setWriteMask, (uint)value);
    }

    /// <summary>
    /// Gets or sets blendingEnabled.
    /// </summary>
    public Bool8 blendingEnabled {
        get => bool8_objc_msgSend(this.NativePtr, sel_isBlendingEnabled);
        set => objc_msgSend(this.NativePtr, sel_setBlendingEnabled, value);
    }

    /// <summary>
    /// Gets or sets alphaBlendOperation.
    /// </summary>
    public MTLBlendOperation alphaBlendOperation {
        get => (MTLBlendOperation)uint_objc_msgSend(this.NativePtr, sel_alphaBlendOperation);
        set => objc_msgSend(this.NativePtr, sel_setAlphaBlendOperation, (uint)value);
    }

    /// <summary>
    /// Gets or sets rgbBlendOperation.
    /// </summary>
    public MTLBlendOperation rgbBlendOperation {
        get => (MTLBlendOperation)uint_objc_msgSend(this.NativePtr, sel_rgbBlendOperation);
        set => objc_msgSend(this.NativePtr, sel_setRGBBlendOperation, (uint)value);
    }

    /// <summary>
    /// Gets or sets destinationAlphaBlendFactor.
    /// </summary>
    public MTLBlendFactor destinationAlphaBlendFactor {
        get => (MTLBlendFactor)uint_objc_msgSend(this.NativePtr, sel_destinationAlphaBlendFactor);
        set => objc_msgSend(this.NativePtr, sel_setDestinationAlphaBlendFactor, (uint)value);
    }

    /// <summary>
    /// Gets or sets destinationRGBBlendFactor.
    /// </summary>
    public MTLBlendFactor destinationRGBBlendFactor {
        get => (MTLBlendFactor)uint_objc_msgSend(this.NativePtr, sel_destinationRGBBlendFactor);
        set => objc_msgSend(this.NativePtr, sel_setDestinationRGBBlendFactor, (uint)value);
    }

    /// <summary>
    /// Gets or sets sourceAlphaBlendFactor.
    /// </summary>
    public MTLBlendFactor sourceAlphaBlendFactor {
        get => (MTLBlendFactor)uint_objc_msgSend(this.NativePtr, sel_sourceAlphaBlendFactor);
        set => objc_msgSend(this.NativePtr, sel_setSourceAlphaBlendFactor, (uint)value);
    }

    /// <summary>
    /// Gets or sets sourceRGBBlendFactor.
    /// </summary>
    public MTLBlendFactor sourceRGBBlendFactor {
        get => (MTLBlendFactor)uint_objc_msgSend(this.NativePtr, sel_sourceRGBBlendFactor);
        set => objc_msgSend(this.NativePtr, sel_setSourceRGBBlendFactor, (uint)value);
    }

    /// <summary>
    /// Stores the value associated with <c>sel_isBlendingEnabled</c>.
    /// </summary>
    private static readonly Selector sel_isBlendingEnabled = "isBlendingEnabled";

    /// <summary>
    /// Stores the value associated with <c>sel_setBlendingEnabled</c>.
    /// </summary>
    private static readonly Selector sel_setBlendingEnabled = "setBlendingEnabled:";

    /// <summary>
    /// Stores the value associated with <c>sel_writeMask</c>.
    /// </summary>
    private static readonly Selector sel_writeMask = "writeMask";

    /// <summary>
    /// Stores the value associated with <c>sel_setWriteMask</c>.
    /// </summary>
    private static readonly Selector sel_setWriteMask = "setWriteMask:";

    /// <summary>
    /// Stores the value associated with <c>sel_alphaBlendOperation</c>.
    /// </summary>
    private static readonly Selector sel_alphaBlendOperation = "alphaBlendOperation";

    /// <summary>
    /// Stores the value associated with <c>sel_setAlphaBlendOperation</c>.
    /// </summary>
    private static readonly Selector sel_setAlphaBlendOperation = "setAlphaBlendOperation:";

    /// <summary>
    /// Stores the value associated with <c>sel_rgbBlendOperation</c>.
    /// </summary>
    private static readonly Selector sel_rgbBlendOperation = "rgbBlendOperation";

    /// <summary>
    /// Stores the value associated with <c>sel_setRGBBlendOperation</c>.
    /// </summary>
    private static readonly Selector sel_setRGBBlendOperation = "setRgbBlendOperation:";

    /// <summary>
    /// Stores the value associated with <c>sel_destinationAlphaBlendFactor</c>.
    /// </summary>
    private static readonly Selector sel_destinationAlphaBlendFactor = "destinationAlphaBlendFactor";

    /// <summary>
    /// Stores the value associated with <c>sel_setDestinationAlphaBlendFactor</c>.
    /// </summary>
    private static readonly Selector sel_setDestinationAlphaBlendFactor = "setDestinationAlphaBlendFactor:";

    /// <summary>
    /// Stores the value associated with <c>sel_destinationRGBBlendFactor</c>.
    /// </summary>
    private static readonly Selector sel_destinationRGBBlendFactor = "destinationRGBBlendFactor";

    /// <summary>
    /// Stores the value associated with <c>sel_setDestinationRGBBlendFactor</c>.
    /// </summary>
    private static readonly Selector sel_setDestinationRGBBlendFactor = "setDestinationRGBBlendFactor:";

    /// <summary>
    /// Stores the value associated with <c>sel_sourceAlphaBlendFactor</c>.
    /// </summary>
    private static readonly Selector sel_sourceAlphaBlendFactor = "sourceAlphaBlendFactor";

    /// <summary>
    /// Stores the value associated with <c>sel_setSourceAlphaBlendFactor</c>.
    /// </summary>
    private static readonly Selector sel_setSourceAlphaBlendFactor = "setSourceAlphaBlendFactor:";

    /// <summary>
    /// Stores the value associated with <c>sel_sourceRGBBlendFactor</c>.
    /// </summary>
    private static readonly Selector sel_sourceRGBBlendFactor = "sourceRGBBlendFactor";

    /// <summary>
    /// Stores the value associated with <c>sel_setSourceRGBBlendFactor</c>.
    /// </summary>
    private static readonly Selector sel_setSourceRGBBlendFactor = "setSourceRGBBlendFactor:";
}