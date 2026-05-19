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
    /// Gets or sets clearDepth.
    /// </summary>
    public double clearDepth {
        get => double_objc_msgSend(this.NativePtr, sel_clearDepth);
        set => objc_msgSend(this.NativePtr, sel_setClearDepth, value);
    }

    /// <summary>
    /// Gets or sets slice.
    /// </summary>
    public UIntPtr slice {
        get => UIntPtr_objc_msgSend(this.NativePtr, Selectors.slice);
        set => objc_msgSend(this.NativePtr, Selectors.setSlice, value);
    }

    /// <summary>
    /// Gets or sets level.
    /// </summary>
    public UIntPtr level {
        get => UIntPtr_objc_msgSend(this.NativePtr, Selectors.level);
        set => objc_msgSend(this.NativePtr, Selectors.setLevel, value);
    }

    /// <summary>
    /// Stores the sel clear depth value used during command execution.
    /// </summary>
    private static readonly Selector sel_clearDepth = "clearDepth";

    /// <summary>
    /// Stores the sel set clear depth value used during command execution.
    /// </summary>
    private static readonly Selector sel_setClearDepth = "setClearDepth:";
}