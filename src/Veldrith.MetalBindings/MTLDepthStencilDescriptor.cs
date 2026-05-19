using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLDepthStencilDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLDepthStencilDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLDepthStencilDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
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
    /// Stores the sel depth compare function value used during command execution.
    /// </summary>
    private static readonly Selector sel_depthCompareFunction = "depthCompareFunction";

    /// <summary>
    /// Stores the sel set depth compare function value used during command execution.
    /// </summary>
    private static readonly Selector sel_setDepthCompareFunction = "setDepthCompareFunction:";

    /// <summary>
    /// Stores the sel is depth write enabled value used during command execution.
    /// </summary>
    private static readonly Selector sel_isDepthWriteEnabled = "isDepthWriteEnabled";

    /// <summary>
    /// Stores the sel set depth write enabled value used during command execution.
    /// </summary>
    private static readonly Selector sel_setDepthWriteEnabled = "setDepthWriteEnabled:";

    /// <summary>
    /// Stores the sel back face stencil state used by this instance.
    /// </summary>
    private static readonly Selector sel_backFaceStencil = "backFaceStencil";

    /// <summary>
    /// Stores the sel set back face stencil state used by this instance.
    /// </summary>
    private static readonly Selector sel_setBackFaceStencil = "setBackFaceStencil:";

    /// <summary>
    /// Stores the sel front face stencil state used by this instance.
    /// </summary>
    private static readonly Selector sel_frontFaceStencil = "frontFaceStencil";

    /// <summary>
    /// Stores the sel set front face stencil state used by this instance.
    /// </summary>
    private static readonly Selector sel_setFrontFaceStencil = "setFrontFaceStencil:";
}