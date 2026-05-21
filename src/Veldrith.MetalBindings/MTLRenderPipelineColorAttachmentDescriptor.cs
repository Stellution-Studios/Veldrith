using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRenderPipelineColorAttachmentDescriptor data structure used by the graphics runtime.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MTLRenderPipelineColorAttachmentDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderPipelineColorAttachmentDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLRenderPipelineColorAttachmentDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets pixelFormat.
    /// </summary>
    public MTLPixelFormat PixelFormat {
        get => (MTLPixelFormat)UIntObjcMsgSend(this.NativePtr, Selectors.PixelFormat);
        set => ObjcMsgSend(this.NativePtr, Selectors.SetPixelFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets writeMask.
    /// </summary>
    public MTLColorWriteMask WriteMask {
        get => (MTLColorWriteMask)UIntObjcMsgSend(this.NativePtr, sel_writeMask);
        set => ObjcMsgSend(this.NativePtr, sel_setWriteMask, (uint)value);
    }

    /// <summary>
    /// Gets or sets blendingEnabled.
    /// </summary>
    public Bool8 BlendingEnabled {
        get => Bool8ObjcMsgSend(this.NativePtr, sel_isBlendingEnabled);
        set => ObjcMsgSend(this.NativePtr, sel_setBlendingEnabled, value);
    }

    /// <summary>
    /// Gets or sets alphaBlendOperation.
    /// </summary>
    public MTLBlendOperation AlphaBlendOperation {
        get => (MTLBlendOperation)UIntObjcMsgSend(this.NativePtr, sel_alphaBlendOperation);
        set => ObjcMsgSend(this.NativePtr, sel_setAlphaBlendOperation, (uint)value);
    }

    /// <summary>
    /// Gets or sets rgbBlendOperation.
    /// </summary>
    public MTLBlendOperation RgbBlendOperation {
        get => (MTLBlendOperation)UIntObjcMsgSend(this.NativePtr, sel_rgbBlendOperation);
        set => ObjcMsgSend(this.NativePtr, sel_setRGBBlendOperation, (uint)value);
    }

    /// <summary>
    /// Gets or sets destinationAlphaBlendFactor.
    /// </summary>
    public MTLBlendFactor DestinationAlphaBlendFactor {
        get => (MTLBlendFactor)UIntObjcMsgSend(this.NativePtr, sel_destinationAlphaBlendFactor);
        set => ObjcMsgSend(this.NativePtr, sel_setDestinationAlphaBlendFactor, (uint)value);
    }

    /// <summary>
    /// Gets or sets destinationRGBBlendFactor.
    /// </summary>
    public MTLBlendFactor DestinationRgbBlendFactor {
        get => (MTLBlendFactor)UIntObjcMsgSend(this.NativePtr, sel_destinationRGBBlendFactor);
        set => ObjcMsgSend(this.NativePtr, sel_setDestinationRGBBlendFactor, (uint)value);
    }

    /// <summary>
    /// Gets or sets sourceAlphaBlendFactor.
    /// </summary>
    public MTLBlendFactor SourceAlphaBlendFactor {
        get => (MTLBlendFactor)UIntObjcMsgSend(this.NativePtr, sel_sourceAlphaBlendFactor);
        set => ObjcMsgSend(this.NativePtr, sel_setSourceAlphaBlendFactor, (uint)value);
    }

    /// <summary>
    /// Gets or sets sourceRGBBlendFactor.
    /// </summary>
    public MTLBlendFactor SourceRgbBlendFactor {
        get => (MTLBlendFactor)UIntObjcMsgSend(this.NativePtr, sel_sourceRGBBlendFactor);
        set => ObjcMsgSend(this.NativePtr, sel_setSourceRGBBlendFactor, (uint)value);
    }

    /// <summary>
    /// Stores the sel is blending enabled state used by this instance.
    /// </summary>
    private static readonly Selector sel_isBlendingEnabled = "isBlendingEnabled";

    /// <summary>
    /// Stores the sel set blending enabled state used by this instance.
    /// </summary>
    private static readonly Selector sel_setBlendingEnabled = "setBlendingEnabled:";

    /// <summary>
    /// Stores the sel write mask state used by this instance.
    /// </summary>
    private static readonly Selector sel_writeMask = "writeMask";

    /// <summary>
    /// Stores the sel set write mask state used by this instance.
    /// </summary>
    private static readonly Selector sel_setWriteMask = "setWriteMask:";

    /// <summary>
    /// Stores the sel Alpha blend operation state used by this instance.
    /// </summary>
    private static readonly Selector sel_alphaBlendOperation = "alphaBlendOperation";

    /// <summary>
    /// Stores the sel set Alpha blend operation state used by this instance.
    /// </summary>
    private static readonly Selector sel_setAlphaBlendOperation = "setAlphaBlendOperation:";

    /// <summary>
    /// Stores the sel rgb blend operation state used by this instance.
    /// </summary>
    private static readonly Selector sel_rgbBlendOperation = "rgbBlendOperation";

    /// <summary>
    /// Stores the sel set rgbblend operation state used by this instance.
    /// </summary>
    private static readonly Selector sel_setRGBBlendOperation = "setRgbBlendOperation:";

    /// <summary>
    /// Stores the sel destination Alpha blend factor state used by this instance.
    /// </summary>
    private static readonly Selector sel_destinationAlphaBlendFactor = "destinationAlphaBlendFactor";

    /// <summary>
    /// Stores the sel set destination Alpha blend factor state used by this instance.
    /// </summary>
    private static readonly Selector sel_setDestinationAlphaBlendFactor = "setDestinationAlphaBlendFactor:";

    /// <summary>
    /// Stores the sel destination rgbblend factor state used by this instance.
    /// </summary>
    private static readonly Selector sel_destinationRGBBlendFactor = "destinationRGBBlendFactor";

    /// <summary>
    /// Stores the sel set destination rgbblend factor state used by this instance.
    /// </summary>
    private static readonly Selector sel_setDestinationRGBBlendFactor = "setDestinationRGBBlendFactor:";

    /// <summary>
    /// Stores the sel source Alpha blend factor state used by this instance.
    /// </summary>
    private static readonly Selector sel_sourceAlphaBlendFactor = "sourceAlphaBlendFactor";

    /// <summary>
    /// Stores the sel set source Alpha blend factor state used by this instance.
    /// </summary>
    private static readonly Selector sel_setSourceAlphaBlendFactor = "setSourceAlphaBlendFactor:";

    /// <summary>
    /// Stores the sel source rgbblend factor state used by this instance.
    /// </summary>
    private static readonly Selector sel_sourceRGBBlendFactor = "sourceRGBBlendFactor";

    /// <summary>
    /// Stores the sel set source rgbblend factor state used by this instance.
    /// </summary>
    private static readonly Selector sel_setSourceRGBBlendFactor = "setSourceRGBBlendFactor:";
}
