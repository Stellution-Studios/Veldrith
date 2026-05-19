using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLStencilDescriptor struct.
/// </summary>
public struct MTLStencilDescriptor {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets stencilFailureOperation.
    /// </summary>
    public MTLStencilOperation stencilFailureOperation {
        get => (MTLStencilOperation)uint_objc_msgSend(this.NativePtr, sel_stencilFailureOperation);
        set => objc_msgSend(this.NativePtr, sel_setStencilFailureOperation, (uint)value);
    }

    /// <summary>
    /// Gets or sets depthFailureOperation.
    /// </summary>
    public MTLStencilOperation depthFailureOperation {
        get => (MTLStencilOperation)uint_objc_msgSend(this.NativePtr, sel_depthFailureOperation);
        set => objc_msgSend(this.NativePtr, sel_setDepthFailureOperation, (uint)value);
    }

    /// <summary>
    /// Gets or sets depthStencilPassOperation.
    /// </summary>
    public MTLStencilOperation depthStencilPassOperation {
        get => (MTLStencilOperation)uint_objc_msgSend(this.NativePtr, sel_depthStencilPassOperation);
        set => objc_msgSend(this.NativePtr, sel_setDepthStencilPassOperation, (uint)value);
    }

    /// <summary>
    /// Gets or sets stencilCompareFunction.
    /// </summary>
    public MTLCompareFunction stencilCompareFunction {
        get => (MTLCompareFunction)uint_objc_msgSend(this.NativePtr, sel_stencilCompareFunction);
        set => objc_msgSend(this.NativePtr, sel_setStencilCompareFunction, (uint)value);
    }

    /// <summary>
    /// Gets or sets readMask.
    /// </summary>
    public uint readMask {
        get => uint_objc_msgSend(this.NativePtr, sel_readMask);
        set => objc_msgSend(this.NativePtr, sel_setReadMask, value);
    }

    /// <summary>
    /// Gets or sets writeMask.
    /// </summary>
    public uint writeMask {
        get => uint_objc_msgSend(this.NativePtr, sel_writeMask);
        set => objc_msgSend(this.NativePtr, sel_setWriteMask, value);
    }

    /// <summary>
    /// Represents the sel_depthFailureOperation field.
    /// </summary>
    private static readonly Selector sel_depthFailureOperation = "depthFailureOperation";

    /// <summary>
    /// Represents the sel_stencilFailureOperation field.
    /// </summary>
    private static readonly Selector sel_stencilFailureOperation = "stencilFailureOperation";

    /// <summary>
    /// Represents the sel_setStencilFailureOperation field.
    /// </summary>
    private static readonly Selector sel_setStencilFailureOperation = "setStencilFailureOperation:";

    /// <summary>
    /// Represents the sel_setDepthFailureOperation field.
    /// </summary>
    private static readonly Selector sel_setDepthFailureOperation = "setDepthFailureOperation:";

    /// <summary>
    /// Represents the sel_depthStencilPassOperation field.
    /// </summary>
    private static readonly Selector sel_depthStencilPassOperation = "depthStencilPassOperation";

    /// <summary>
    /// Represents the sel_setDepthStencilPassOperation field.
    /// </summary>
    private static readonly Selector sel_setDepthStencilPassOperation = "setDepthStencilPassOperation:";

    /// <summary>
    /// Represents the sel_stencilCompareFunction field.
    /// </summary>
    private static readonly Selector sel_stencilCompareFunction = "stencilCompareFunction";

    /// <summary>
    /// Represents the sel_setStencilCompareFunction field.
    /// </summary>
    private static readonly Selector sel_setStencilCompareFunction = "setStencilCompareFunction:";

    /// <summary>
    /// Represents the sel_readMask field.
    /// </summary>
    private static readonly Selector sel_readMask = "readMask";

    /// <summary>
    /// Represents the sel_setReadMask field.
    /// </summary>
    private static readonly Selector sel_setReadMask = "setReadMask:";

    /// <summary>
    /// Represents the sel_writeMask field.
    /// </summary>
    private static readonly Selector sel_writeMask = "writeMask";

    /// <summary>
    /// Represents the sel_setWriteMask field.
    /// </summary>
    private static readonly Selector sel_setWriteMask = "setWriteMask:";
}