using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLDepthStencilDescriptor struct.
/// </summary>
public struct MTLDepthStencilDescriptor {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLDepthStencilDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
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
    /// Stores the value associated with <c>sel_depthCompareFunction</c>.
    /// </summary>
    private static readonly Selector sel_depthCompareFunction = "depthCompareFunction";

    /// <summary>
    /// Stores the value associated with <c>sel_setDepthCompareFunction</c>.
    /// </summary>
    private static readonly Selector sel_setDepthCompareFunction = "setDepthCompareFunction:";

    /// <summary>
    /// Stores the value associated with <c>sel_isDepthWriteEnabled</c>.
    /// </summary>
    private static readonly Selector sel_isDepthWriteEnabled = "isDepthWriteEnabled";

    /// <summary>
    /// Stores the value associated with <c>sel_setDepthWriteEnabled</c>.
    /// </summary>
    private static readonly Selector sel_setDepthWriteEnabled = "setDepthWriteEnabled:";

    /// <summary>
    /// Stores the value associated with <c>sel_backFaceStencil</c>.
    /// </summary>
    private static readonly Selector sel_backFaceStencil = "backFaceStencil";

    /// <summary>
    /// Stores the value associated with <c>sel_setBackFaceStencil</c>.
    /// </summary>
    private static readonly Selector sel_setBackFaceStencil = "setBackFaceStencil:";

    /// <summary>
    /// Stores the value associated with <c>sel_frontFaceStencil</c>.
    /// </summary>
    private static readonly Selector sel_frontFaceStencil = "frontFaceStencil";

    /// <summary>
    /// Stores the value associated with <c>sel_setFrontFaceStencil</c>.
    /// </summary>
    private static readonly Selector sel_setFrontFaceStencil = "setFrontFaceStencil:";
}