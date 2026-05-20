using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRenderPassColorAttachmentDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLRenderPassColorAttachmentDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderPassColorAttachmentDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLRenderPassColorAttachmentDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets texture.
    /// </summary>
    public MTLTexture texture {
        get => objc_msgSend<MTLTexture>(this.NativePtr, Selectors.Texture);
        set => objc_msgSend(this.NativePtr, Selectors.SetTexture, value.NativePtr);
    }

    /// <summary>
    /// Gets or sets loadAction.
    /// </summary>
    public MTLLoadAction LoadAction {
        get => (MTLLoadAction)uint_objc_msgSend(this.NativePtr, Selectors.LoadAction);
        set => objc_msgSend(this.NativePtr, Selectors.SetLoadAction, (uint)value);
    }

    /// <summary>
    /// Gets or sets storeAction.
    /// </summary>
    public MTLStoreAction StoreAction {
        get => (MTLStoreAction)uint_objc_msgSend(this.NativePtr, Selectors.StoreAction);
        set => objc_msgSend(this.NativePtr, Selectors.SetStoreAction, (uint)value);
    }

    /// <summary>
    /// Gets or sets resolveTexture.
    /// </summary>
    public MTLTexture ResolveTexture {
        get => objc_msgSend<MTLTexture>(this.NativePtr, Selectors.ResolveTexture);
        set => objc_msgSend(this.NativePtr, Selectors.SetResolveTexture, value.NativePtr);
    }

    /// <summary>
    /// Gets or sets clearColor.
    /// </summary>
    public MTLClearColor ClearColor {
        get {
            if (UseStret<MTLClearColor>()) {
                return objc_msgSend_stret<MTLClearColor>(this.NativePtr, _selClearColor);
            }

            return MTLClearColor_objc_msgSend(this.NativePtr, _selClearColor);
        }
        set => objc_msgSend(this.NativePtr, _selSetClearColor, value);
    }

    /// <summary>
    /// Gets or sets slice.
    /// </summary>
    public UIntPtr Slice {
        get => UIntPtr_objc_msgSend(this.NativePtr, Selectors.Slice);
        set => objc_msgSend(this.NativePtr, Selectors.SetSlice, value);
    }

    /// <summary>
    /// Gets or sets level.
    /// </summary>
    public UIntPtr level {
        get => UIntPtr_objc_msgSend(this.NativePtr, Selectors.Level);
        set => objc_msgSend(this.NativePtr, Selectors.SetLevel, value);
    }

    /// <summary>
    /// Stores the sel clear color state used by this instance.
    /// </summary>
    private static readonly Selector _selClearColor = "clearColor";

    /// <summary>
    /// Stores the sel set clear color state used by this instance.
    /// </summary>
    private static readonly Selector _selSetClearColor = "setClearColor:";
}