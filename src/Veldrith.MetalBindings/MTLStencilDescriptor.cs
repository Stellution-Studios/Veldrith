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
    public MTLStencilOperation StencilFailureOperation {
        get => (MTLStencilOperation)UIntObjcMsgSend(this.NativePtr, _selStencilFailureOperation);
        set => ObjcMsgSend(this.NativePtr, _selSetStencilFailureOperation, (uint)value);
    }

    /// <summary>
    /// Gets or sets depthFailureOperation.
    /// </summary>
    public MTLStencilOperation DepthFailureOperation {
        get => (MTLStencilOperation)UIntObjcMsgSend(this.NativePtr, _selDepthFailureOperation);
        set => ObjcMsgSend(this.NativePtr, _selSetDepthFailureOperation, (uint)value);
    }

    /// <summary>
    /// Gets or sets depthStencilPassOperation.
    /// </summary>
    public MTLStencilOperation DepthStencilPassOperation {
        get => (MTLStencilOperation)UIntObjcMsgSend(this.NativePtr, _selDepthStencilPassOperation);
        set => ObjcMsgSend(this.NativePtr, _selSetDepthStencilPassOperation, (uint)value);
    }

    /// <summary>
    /// Gets or sets stencilCompareFunction.
    /// </summary>
    public MTLCompareFunction StencilCompareFunction {
        get => (MTLCompareFunction)UIntObjcMsgSend(this.NativePtr, _selStencilCompareFunction);
        set => ObjcMsgSend(this.NativePtr, _selSetStencilCompareFunction, (uint)value);
    }

    /// <summary>
    /// Gets or sets readMask.
    /// </summary>
    public uint ReadMask {
        get => UIntObjcMsgSend(this.NativePtr, _selReadMask);
        set => ObjcMsgSend(this.NativePtr, _selSetReadMask, value);
    }

    /// <summary>
    /// Gets or sets writeMask.
    /// </summary>
    public uint WriteMask {
        get => UIntObjcMsgSend(this.NativePtr, _selWriteMask);
        set => ObjcMsgSend(this.NativePtr, _selSetWriteMask, value);
    }

    /// <summary>
    /// Stores the sel depth failure operation value used during command execution.
    /// </summary>
    private static readonly Selector _selDepthFailureOperation = "depthFailureOperation";

    /// <summary>
    /// Stores the sel stencil failure operation state used by this instance.
    /// </summary>
    private static readonly Selector _selStencilFailureOperation = "stencilFailureOperation";

    /// <summary>
    /// Stores the sel set stencil failure operation state used by this instance.
    /// </summary>
    private static readonly Selector _selSetStencilFailureOperation = "setStencilFailureOperation:";

    /// <summary>
    /// Stores the sel set depth failure operation value used during command execution.
    /// </summary>
    private static readonly Selector _selSetDepthFailureOperation = "setDepthFailureOperation:";

    /// <summary>
    /// Stores the sel depth stencil pass operation value used during command execution.
    /// </summary>
    private static readonly Selector _selDepthStencilPassOperation = "depthStencilPassOperation";

    /// <summary>
    /// Stores the sel set depth stencil pass operation value used during command execution.
    /// </summary>
    private static readonly Selector _selSetDepthStencilPassOperation = "setDepthStencilPassOperation:";

    /// <summary>
    /// Stores the sel stencil compare function state used by this instance.
    /// </summary>
    private static readonly Selector _selStencilCompareFunction = "stencilCompareFunction";

    /// <summary>
    /// Stores the sel set stencil compare function state used by this instance.
    /// </summary>
    private static readonly Selector _selSetStencilCompareFunction = "setStencilCompareFunction:";

    /// <summary>
    /// Stores the sel read mask state used by this instance.
    /// </summary>
    private static readonly Selector _selReadMask = "readMask";

    /// <summary>
    /// Stores the sel set read mask state used by this instance.
    /// </summary>
    private static readonly Selector _selSetReadMask = "setReadMask:";

    /// <summary>
    /// Stores the sel write mask state used by this instance.
    /// </summary>
    private static readonly Selector _selWriteMask = "writeMask";

    /// <summary>
    /// Stores the sel set write mask state used by this instance.
    /// </summary>
    private static readonly Selector _selSetWriteMask = "setWriteMask:";
}
