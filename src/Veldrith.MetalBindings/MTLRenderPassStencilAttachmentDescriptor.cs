using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRenderPassStencilAttachmentDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLRenderPassStencilAttachmentDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets texture.
    /// </summary>
    public MTLTexture Texture {
        get => ObjcMsgSend<MTLTexture>(this.NativePtr, Selectors.Texture);
        set => ObjcMsgSend(this.NativePtr, Selectors.SetTexture, value.NativePtr);
    }

    /// <summary>
    /// Gets or sets loadAction.
    /// </summary>
    public MTLLoadAction LoadAction {
        get => (MTLLoadAction)UIntObjcMsgSend(this.NativePtr, Selectors.LoadAction);
        set => ObjcMsgSend(this.NativePtr, Selectors.SetLoadAction, (uint)value);
    }

    /// <summary>
    /// Gets or sets storeAction.
    /// </summary>
    public MTLStoreAction StoreAction {
        get => (MTLStoreAction)UIntObjcMsgSend(this.NativePtr, Selectors.StoreAction);
        set => ObjcMsgSend(this.NativePtr, Selectors.SetStoreAction, (uint)value);
    }

    /// <summary>
    /// Gets or sets clearStencil.
    /// </summary>
    public uint ClearStencil {
        get => UIntObjcMsgSend(this.NativePtr, _selClearStencil);
        set => ObjcMsgSend(this.NativePtr, _selSetClearStencil, value);
    }

    /// <summary>
    /// Gets or sets slice.
    /// </summary>
    public UIntPtr Slice {
        get => UIntPtr_objc_msgSend(this.NativePtr, Selectors.Slice);
        set => ObjcMsgSend(this.NativePtr, Selectors.SetSlice, value);
    }

    /// <summary>
    /// Stores the sel clear stencil state used by this instance.
    /// </summary>
    private static readonly Selector _selClearStencil = "clearStencil";

    /// <summary>
    /// Stores the sel set clear stencil state used by this instance.
    /// </summary>
    private static readonly Selector _selSetClearStencil = "setClearStencil:";
}
