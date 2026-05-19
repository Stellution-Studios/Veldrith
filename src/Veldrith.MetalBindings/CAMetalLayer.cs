using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct CAMetalLayer {
    public readonly IntPtr NativePtr;

    public CAMetalLayer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public static CAMetalLayer New() {
        return s_class.AllocInit<CAMetalLayer>();
    }

    public static bool TryCast(IntPtr layerPointer, out CAMetalLayer metalLayer) {
        NSObject layerObject = new(layerPointer);

        if (layerObject.IsKindOfClass(s_class)) {
            metalLayer = new CAMetalLayer(layerPointer);
            return true;
        }

        metalLayer = default;
        return false;
    }

    public MTLDevice device {
        get => objc_msgSend<MTLDevice>(this.NativePtr, sel_device);
        set => objc_msgSend(this.NativePtr, sel_setDevice, value);
    }

    public MTLPixelFormat pixelFormat {
        get => (MTLPixelFormat)uint_objc_msgSend(this.NativePtr, sel_pixelFormat);
        set => objc_msgSend(this.NativePtr, sel_setPixelFormat, (uint)value);
    }

    public Bool8 framebufferOnly {
        get => bool8_objc_msgSend(this.NativePtr, sel_framebufferOnly);
        set => objc_msgSend(this.NativePtr, sel_setFramebufferOnly, value);
    }

    public CGSize drawableSize {
        get => CGSize_objc_msgSend(this.NativePtr, sel_drawableSize);
        set => objc_msgSend(this.NativePtr, sel_setDrawableSize, value);
    }

    public CGRect frame {
        get => CGRect_objc_msgSend(this.NativePtr, sel_frame);
        set => objc_msgSend(this.NativePtr, sel_setFrame, value);
    }

    public Bool8 opaque {
        get => bool8_objc_msgSend(this.NativePtr, sel_isOpaque);
        set => objc_msgSend(this.NativePtr, sel_setOpaque, value);
    }

    public CAMetalDrawable nextDrawable() {
        return objc_msgSend<CAMetalDrawable>(this.NativePtr, sel_nextDrawable);
    }

    public Bool8 displaySyncEnabled {
        get => bool8_objc_msgSend(this.NativePtr, sel_displaySyncEnabled);
        set => objc_msgSend(this.NativePtr, sel_setDisplaySyncEnabled, value);
    }

    private static readonly ObjCClass s_class = new(nameof(CAMetalLayer));
    private static readonly Selector sel_device = "device";
    private static readonly Selector sel_setDevice = "setDevice:";
    private static readonly Selector sel_pixelFormat = "pixelFormat";
    private static readonly Selector sel_setPixelFormat = "setPixelFormat:";
    private static readonly Selector sel_framebufferOnly = "framebufferOnly";
    private static readonly Selector sel_setFramebufferOnly = "setFramebufferOnly:";
    private static readonly Selector sel_drawableSize = "drawableSize";
    private static readonly Selector sel_setDrawableSize = "setDrawableSize:";
    private static readonly Selector sel_frame = "frame";
    private static readonly Selector sel_setFrame = "setFrame:";
    private static readonly Selector sel_isOpaque = "isOpaque";
    private static readonly Selector sel_setOpaque = "setOpaque:";
    private static readonly Selector sel_displaySyncEnabled = "displaySyncEnabled";
    private static readonly Selector sel_setDisplaySyncEnabled = "setDisplaySyncEnabled:";
    private static readonly Selector sel_nextDrawable = "nextDrawable";
}