using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLRenderPipelineColorAttachmentDescriptor struct.
/// </summary>
public struct MTLRenderPipelineColorAttachmentDescriptor {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderPipelineColorAttachmentDescriptor" /> class.
    /// </summary>
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
    /// Represents the sel_isBlendingEnabled field.
    /// </summary>
    private static readonly Selector sel_isBlendingEnabled = "isBlendingEnabled";

    /// <summary>
    /// Represents the sel_setBlendingEnabled field.
    /// </summary>
    private static readonly Selector sel_setBlendingEnabled = "setBlendingEnabled:";

    /// <summary>
    /// Represents the sel_writeMask field.
    /// </summary>
    private static readonly Selector sel_writeMask = "writeMask";

    /// <summary>
    /// Represents the sel_setWriteMask field.
    /// </summary>
    private static readonly Selector sel_setWriteMask = "setWriteMask:";

    /// <summary>
    /// Represents the sel_alphaBlendOperation field.
    /// </summary>
    private static readonly Selector sel_alphaBlendOperation = "alphaBlendOperation";

    /// <summary>
    /// Represents the sel_setAlphaBlendOperation field.
    /// </summary>
    private static readonly Selector sel_setAlphaBlendOperation = "setAlphaBlendOperation:";

    /// <summary>
    /// Represents the sel_rgbBlendOperation field.
    /// </summary>
    private static readonly Selector sel_rgbBlendOperation = "rgbBlendOperation";

    /// <summary>
    /// Represents the sel_setRGBBlendOperation field.
    /// </summary>
    private static readonly Selector sel_setRGBBlendOperation = "setRgbBlendOperation:";

    /// <summary>
    /// Represents the sel_destinationAlphaBlendFactor field.
    /// </summary>
    private static readonly Selector sel_destinationAlphaBlendFactor = "destinationAlphaBlendFactor";

    /// <summary>
    /// Represents the sel_setDestinationAlphaBlendFactor field.
    /// </summary>
    private static readonly Selector sel_setDestinationAlphaBlendFactor = "setDestinationAlphaBlendFactor:";

    /// <summary>
    /// Represents the sel_destinationRGBBlendFactor field.
    /// </summary>
    private static readonly Selector sel_destinationRGBBlendFactor = "destinationRGBBlendFactor";

    /// <summary>
    /// Represents the sel_setDestinationRGBBlendFactor field.
    /// </summary>
    private static readonly Selector sel_setDestinationRGBBlendFactor = "setDestinationRGBBlendFactor:";

    /// <summary>
    /// Represents the sel_sourceAlphaBlendFactor field.
    /// </summary>
    private static readonly Selector sel_sourceAlphaBlendFactor = "sourceAlphaBlendFactor";

    /// <summary>
    /// Represents the sel_setSourceAlphaBlendFactor field.
    /// </summary>
    private static readonly Selector sel_setSourceAlphaBlendFactor = "setSourceAlphaBlendFactor:";

    /// <summary>
    /// Represents the sel_sourceRGBBlendFactor field.
    /// </summary>
    private static readonly Selector sel_sourceRGBBlendFactor = "sourceRGBBlendFactor";

    /// <summary>
    /// Represents the sel_setSourceRGBBlendFactor field.
    /// </summary>
    private static readonly Selector sel_setSourceRGBBlendFactor = "setSourceRGBBlendFactor:";
}