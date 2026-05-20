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
    public MTLCompareFunction DepthCompareFunction {
        get => (MTLCompareFunction)uint_objc_msgSend(this.NativePtr, _selDepthCompareFunction);
        set => objc_msgSend(this.NativePtr, _selSetDepthCompareFunction, (uint)value);
    }

    /// <summary>
    /// Gets or sets depthWriteEnabled.
    /// </summary>
    public Bool8 DepthWriteEnabled {
        get => bool8_objc_msgSend(this.NativePtr, _selIsDepthWriteEnabled);
        set => objc_msgSend(this.NativePtr, _selSetDepthWriteEnabled, value);
    }

    /// <summary>
    /// Gets or sets backFaceStencil.
    /// </summary>
    public MTLStencilDescriptor BackFaceStencil {
        get => objc_msgSend<MTLStencilDescriptor>(this.NativePtr, _selBackFaceStencil);
        set => objc_msgSend(this.NativePtr, _selSetBackFaceStencil, value.NativePtr);
    }

    /// <summary>
    /// Gets or sets frontFaceStencil.
    /// </summary>
    public MTLStencilDescriptor FrontFaceStencil {
        get => objc_msgSend<MTLStencilDescriptor>(this.NativePtr, _selFrontFaceStencil);
        set => objc_msgSend(this.NativePtr, _selSetFrontFaceStencil, value.NativePtr);
    }

    /// <summary>
    /// Stores the sel depth compare function value used during command execution.
    /// </summary>
    private static readonly Selector _selDepthCompareFunction = "depthCompareFunction";

    /// <summary>
    /// Stores the sel set depth compare function value used during command execution.
    /// </summary>
    private static readonly Selector _selSetDepthCompareFunction = "setDepthCompareFunction:";

    /// <summary>
    /// Stores the sel is depth write enabled value used during command execution.
    /// </summary>
    private static readonly Selector _selIsDepthWriteEnabled = "isDepthWriteEnabled";

    /// <summary>
    /// Stores the sel set depth write enabled value used during command execution.
    /// </summary>
    private static readonly Selector _selSetDepthWriteEnabled = "setDepthWriteEnabled:";

    /// <summary>
    /// Stores the sel back face stencil state used by this instance.
    /// </summary>
    private static readonly Selector _selBackFaceStencil = "backFaceStencil";

    /// <summary>
    /// Stores the sel set back face stencil state used by this instance.
    /// </summary>
    private static readonly Selector _selSetBackFaceStencil = "setBackFaceStencil:";

    /// <summary>
    /// Stores the sel front face stencil state used by this instance.
    /// </summary>
    private static readonly Selector _selFrontFaceStencil = "frontFaceStencil";

    /// <summary>
    /// Stores the sel set front face stencil state used by this instance.
    /// </summary>
    private static readonly Selector _selSetFrontFaceStencil = "setFrontFaceStencil:";
}