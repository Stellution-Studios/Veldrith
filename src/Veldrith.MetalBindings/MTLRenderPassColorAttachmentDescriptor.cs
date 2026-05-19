using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct MTLRenderPassColorAttachmentDescriptor {
    public readonly IntPtr NativePtr;

    public MTLRenderPassColorAttachmentDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public MTLTexture texture {
        get => objc_msgSend<MTLTexture>(this.NativePtr, Selectors.texture);
        set => objc_msgSend(this.NativePtr, Selectors.setTexture, value.NativePtr);
    }

    public MTLLoadAction loadAction {
        get => (MTLLoadAction)uint_objc_msgSend(this.NativePtr, Selectors.loadAction);
        set => objc_msgSend(this.NativePtr, Selectors.setLoadAction, (uint)value);
    }

    public MTLStoreAction storeAction {
        get => (MTLStoreAction)uint_objc_msgSend(this.NativePtr, Selectors.storeAction);
        set => objc_msgSend(this.NativePtr, Selectors.setStoreAction, (uint)value);
    }

    public MTLTexture resolveTexture {
        get => objc_msgSend<MTLTexture>(this.NativePtr, Selectors.resolveTexture);
        set => objc_msgSend(this.NativePtr, Selectors.setResolveTexture, value.NativePtr);
    }

    public MTLClearColor clearColor {
        get {
            if (UseStret<MTLClearColor>()) {
                return objc_msgSend_stret<MTLClearColor>(this.NativePtr, sel_clearColor);
            }

            return MTLClearColor_objc_msgSend(this.NativePtr, sel_clearColor);
        }
        set => objc_msgSend(this.NativePtr, sel_setClearColor, value);
    }

    public UIntPtr slice {
        get => UIntPtr_objc_msgSend(this.NativePtr, Selectors.slice);
        set => objc_msgSend(this.NativePtr, Selectors.setSlice, value);
    }

    public UIntPtr level {
        get => UIntPtr_objc_msgSend(this.NativePtr, Selectors.level);
        set => objc_msgSend(this.NativePtr, Selectors.setLevel, value);
    }

    private static readonly Selector sel_clearColor = "clearColor";
    private static readonly Selector sel_setClearColor = "setClearColor:";
}