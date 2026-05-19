using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLDepthStencilDescriptor struct.
/// </summary>
public struct MTLDepthStencilDescriptor {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLDepthStencilDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public MTLDepthStencilDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets depthCompareFunction.
    /// </summary>
    public MTLCompareFunction depthCompareFunction {
        get => (MTLCompareFunction)uint_objc_msgSend(this.NativePtr, sel_depthCompareFunction);
        set => objc_msgSend(this.NativePtr, sel_setDepthCompareFunction, (uint)value);
    }

    /// <summary>
    /// Gets or sets depthWriteEnabled.
    /// </summary>
    public Bool8 depthWriteEnabled {
        get => bool8_objc_msgSend(this.NativePtr, sel_isDepthWriteEnabled);
        set => objc_msgSend(this.NativePtr, sel_setDepthWriteEnabled, value);
    }

    /// <summary>
    /// Gets or sets backFaceStencil.
    /// </summary>
    public MTLStencilDescriptor backFaceStencil {
        get => objc_msgSend<MTLStencilDescriptor>(this.NativePtr, sel_backFaceStencil);
        set => objc_msgSend(this.NativePtr, sel_setBackFaceStencil, value.NativePtr);
    }

    /// <summary>
    /// Gets or sets frontFaceStencil.
    /// </summary>
    public MTLStencilDescriptor frontFaceStencil {
        get => objc_msgSend<MTLStencilDescriptor>(this.NativePtr, sel_frontFaceStencil);
        set => objc_msgSend(this.NativePtr, sel_setFrontFaceStencil, value.NativePtr);
    }

    /// <summary>
    /// Represents the sel_depthCompareFunction field.
    /// </summary>
    private static readonly Selector sel_depthCompareFunction = "depthCompareFunction";

    /// <summary>
    /// Represents the sel_setDepthCompareFunction field.
    /// </summary>
    private static readonly Selector sel_setDepthCompareFunction = "setDepthCompareFunction:";

    /// <summary>
    /// Represents the sel_isDepthWriteEnabled field.
    /// </summary>
    private static readonly Selector sel_isDepthWriteEnabled = "isDepthWriteEnabled";

    /// <summary>
    /// Represents the sel_setDepthWriteEnabled field.
    /// </summary>
    private static readonly Selector sel_setDepthWriteEnabled = "setDepthWriteEnabled:";

    /// <summary>
    /// Represents the sel_backFaceStencil field.
    /// </summary>
    private static readonly Selector sel_backFaceStencil = "backFaceStencil";

    /// <summary>
    /// Represents the sel_setBackFaceStencil field.
    /// </summary>
    private static readonly Selector sel_setBackFaceStencil = "setBackFaceStencil:";

    /// <summary>
    /// Represents the sel_frontFaceStencil field.
    /// </summary>
    private static readonly Selector sel_frontFaceStencil = "frontFaceStencil";

    /// <summary>
    /// Represents the sel_setFrontFaceStencil field.
    /// </summary>
    private static readonly Selector sel_setFrontFaceStencil = "setFrontFaceStencil:";
}