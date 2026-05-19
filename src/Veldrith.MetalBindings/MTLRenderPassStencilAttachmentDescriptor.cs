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
    public MTLTexture texture {
        get => objc_msgSend<MTLTexture>(this.NativePtr, Selectors.texture);
        set => objc_msgSend(this.NativePtr, Selectors.setTexture, value.NativePtr);
    }

    /// <summary>
    /// Gets or sets loadAction.
    /// </summary>
    public MTLLoadAction loadAction {
        get => (MTLLoadAction)uint_objc_msgSend(this.NativePtr, Selectors.loadAction);
        set => objc_msgSend(this.NativePtr, Selectors.setLoadAction, (uint)value);
    }

    /// <summary>
    /// Gets or sets storeAction.
    /// </summary>
    public MTLStoreAction storeAction {
        get => (MTLStoreAction)uint_objc_msgSend(this.NativePtr, Selectors.storeAction);
        set => objc_msgSend(this.NativePtr, Selectors.setStoreAction, (uint)value);
    }

    /// <summary>
    /// Gets or sets clearStencil.
    /// </summary>
    public uint clearStencil {
        get => uint_objc_msgSend(this.NativePtr, sel_clearStencil);
        set => objc_msgSend(this.NativePtr, sel_setClearStencil, value);
    }

    /// <summary>
    /// Gets or sets slice.
    /// </summary>
    public UIntPtr slice {
        get => UIntPtr_objc_msgSend(this.NativePtr, Selectors.slice);
        set => objc_msgSend(this.NativePtr, Selectors.setSlice, value);
    }

    /// <summary>
    /// Stores the sel clear stencil state used by this instance.
    /// </summary>
    private static readonly Selector sel_clearStencil = "clearStencil";

    /// <summary>
    /// Stores the sel set clear stencil state used by this instance.
    /// </summary>
    private static readonly Selector sel_setClearStencil = "setClearStencil:";
}