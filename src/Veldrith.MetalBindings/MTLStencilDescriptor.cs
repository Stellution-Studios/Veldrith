using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLStencilDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLStencilDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
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
    /// Stores the sel depth failure operation value used during command execution.
    /// </summary>
    private static readonly Selector sel_depthFailureOperation = "depthFailureOperation";

    /// <summary>
    /// Stores the sel stencil failure operation state used by this instance.
    /// </summary>
    private static readonly Selector sel_stencilFailureOperation = "stencilFailureOperation";

    /// <summary>
    /// Stores the sel set stencil failure operation state used by this instance.
    /// </summary>
    private static readonly Selector sel_setStencilFailureOperation = "setStencilFailureOperation:";

    /// <summary>
    /// Stores the sel set depth failure operation value used during command execution.
    /// </summary>
    private static readonly Selector sel_setDepthFailureOperation = "setDepthFailureOperation:";

    /// <summary>
    /// Stores the sel depth stencil pass operation value used during command execution.
    /// </summary>
    private static readonly Selector sel_depthStencilPassOperation = "depthStencilPassOperation";

    /// <summary>
    /// Stores the sel set depth stencil pass operation value used during command execution.
    /// </summary>
    private static readonly Selector sel_setDepthStencilPassOperation = "setDepthStencilPassOperation:";

    /// <summary>
    /// Stores the sel stencil compare function state used by this instance.
    /// </summary>
    private static readonly Selector sel_stencilCompareFunction = "stencilCompareFunction";

    /// <summary>
    /// Stores the sel set stencil compare function state used by this instance.
    /// </summary>
    private static readonly Selector sel_setStencilCompareFunction = "setStencilCompareFunction:";

    /// <summary>
    /// Stores the sel read mask state used by this instance.
    /// </summary>
    private static readonly Selector sel_readMask = "readMask";

    /// <summary>
    /// Stores the sel set read mask state used by this instance.
    /// </summary>
    private static readonly Selector sel_setReadMask = "setReadMask:";

    /// <summary>
    /// Stores the sel write mask state used by this instance.
    /// </summary>
    private static readonly Selector sel_writeMask = "writeMask";

    /// <summary>
    /// Stores the sel set write mask state used by this instance.
    /// </summary>
    private static readonly Selector sel_setWriteMask = "setWriteMask:";
}