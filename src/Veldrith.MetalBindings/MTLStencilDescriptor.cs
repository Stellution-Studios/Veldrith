using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLStencilDescriptor struct.
/// </summary>
public struct MTLStencilDescriptor {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
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
    /// Stores the value associated with <c>sel_depthFailureOperation</c>.
    /// </summary>
    private static readonly Selector sel_depthFailureOperation = "depthFailureOperation";

    /// <summary>
    /// Stores the value associated with <c>sel_stencilFailureOperation</c>.
    /// </summary>
    private static readonly Selector sel_stencilFailureOperation = "stencilFailureOperation";

    /// <summary>
    /// Stores the value associated with <c>sel_setStencilFailureOperation</c>.
    /// </summary>
    private static readonly Selector sel_setStencilFailureOperation = "setStencilFailureOperation:";

    /// <summary>
    /// Stores the value associated with <c>sel_setDepthFailureOperation</c>.
    /// </summary>
    private static readonly Selector sel_setDepthFailureOperation = "setDepthFailureOperation:";

    /// <summary>
    /// Stores the value associated with <c>sel_depthStencilPassOperation</c>.
    /// </summary>
    private static readonly Selector sel_depthStencilPassOperation = "depthStencilPassOperation";

    /// <summary>
    /// Stores the value associated with <c>sel_setDepthStencilPassOperation</c>.
    /// </summary>
    private static readonly Selector sel_setDepthStencilPassOperation = "setDepthStencilPassOperation:";

    /// <summary>
    /// Stores the value associated with <c>sel_stencilCompareFunction</c>.
    /// </summary>
    private static readonly Selector sel_stencilCompareFunction = "stencilCompareFunction";

    /// <summary>
    /// Stores the value associated with <c>sel_setStencilCompareFunction</c>.
    /// </summary>
    private static readonly Selector sel_setStencilCompareFunction = "setStencilCompareFunction:";

    /// <summary>
    /// Stores the value associated with <c>sel_readMask</c>.
    /// </summary>
    private static readonly Selector sel_readMask = "readMask";

    /// <summary>
    /// Stores the value associated with <c>sel_setReadMask</c>.
    /// </summary>
    private static readonly Selector sel_setReadMask = "setReadMask:";

    /// <summary>
    /// Stores the value associated with <c>sel_writeMask</c>.
    /// </summary>
    private static readonly Selector sel_writeMask = "writeMask";

    /// <summary>
    /// Stores the value associated with <c>sel_setWriteMask</c>.
    /// </summary>
    private static readonly Selector sel_setWriteMask = "setWriteMask:";
}