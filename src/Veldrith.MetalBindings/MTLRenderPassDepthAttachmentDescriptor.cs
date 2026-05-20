using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRenderPassDepthAttachmentDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLRenderPassDepthAttachmentDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRenderPassDepthAttachmentDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLRenderPassDepthAttachmentDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets texture.
    /// </summary>
    public MTLTexture Texture {
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
    /// Gets or sets clearDepth.
    /// </summary>
    public double ClearDepth {
        get => double_objc_msgSend(this.NativePtr, _selClearDepth);
        set => objc_msgSend(this.NativePtr, _selSetClearDepth, value);
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
    public UIntPtr Level {
        get => UIntPtr_objc_msgSend(this.NativePtr, Selectors.Level);
        set => objc_msgSend(this.NativePtr, Selectors.SetLevel, value);
    }

    /// <summary>
    /// Stores the sel clear depth value used during command execution.
    /// </summary>
    private static readonly Selector _selClearDepth = "clearDepth";

    /// <summary>
    /// Stores the sel set clear depth value used during command execution.
    /// </summary>
    private static readonly Selector _selSetClearDepth = "setClearDepth:";
}