using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CAMetalLayer struct.
/// </summary>
public struct CAMetalLayer {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="CAMetalLayer" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public CAMetalLayer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Performs the New operation.
    /// </summary>
    /// <returns>The result of the New operation.</returns>
    public static CAMetalLayer New() {
        return s_class.AllocInit<CAMetalLayer>();
    }

    /// <summary>
    /// Performs the TryCast operation.
    /// </summary>
    /// <param name="layerPointer">The value of layerPointer.</param>
    /// <param name="metalLayer">The value of metalLayer.</param>
    /// <returns>The result of the TryCast operation.</returns>
    public static bool TryCast(IntPtr layerPointer, out CAMetalLayer metalLayer) {
        NSObject layerObject = new(layerPointer);

        if (layerObject.IsKindOfClass(s_class)) {
            metalLayer = new CAMetalLayer(layerPointer);
            return true;
        }

        metalLayer = default;
        return false;
    }

    /// <summary>
    /// Gets or sets device.
    /// </summary>
    public MTLDevice device {
        get => objc_msgSend<MTLDevice>(this.NativePtr, sel_device);
        set => objc_msgSend(this.NativePtr, sel_setDevice, value);
    }

    /// <summary>
    /// Gets or sets pixelFormat.
    /// </summary>
    public MTLPixelFormat pixelFormat {
        get => (MTLPixelFormat)uint_objc_msgSend(this.NativePtr, sel_pixelFormat);
        set => objc_msgSend(this.NativePtr, sel_setPixelFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets framebufferOnly.
    /// </summary>
    public Bool8 framebufferOnly {
        get => bool8_objc_msgSend(this.NativePtr, sel_framebufferOnly);
        set => objc_msgSend(this.NativePtr, sel_setFramebufferOnly, value);
    }

    /// <summary>
    /// Gets or sets drawableSize.
    /// </summary>
    public CGSize drawableSize {
        get => CGSize_objc_msgSend(this.NativePtr, sel_drawableSize);
        set => objc_msgSend(this.NativePtr, sel_setDrawableSize, value);
    }

    /// <summary>
    /// Gets or sets frame.
    /// </summary>
    public CGRect frame {
        get => CGRect_objc_msgSend(this.NativePtr, sel_frame);
        set => objc_msgSend(this.NativePtr, sel_setFrame, value);
    }

    /// <summary>
    /// Gets or sets opaque.
    /// </summary>
    public Bool8 opaque {
        get => bool8_objc_msgSend(this.NativePtr, sel_isOpaque);
        set => objc_msgSend(this.NativePtr, sel_setOpaque, value);
    }

    /// <summary>
    /// Performs the nextDrawable operation.
    /// </summary>
    /// <returns>The result of the nextDrawable operation.</returns>
    public CAMetalDrawable nextDrawable() {
        return objc_msgSend<CAMetalDrawable>(this.NativePtr, sel_nextDrawable);
    }

    /// <summary>
    /// Gets or sets displaySyncEnabled.
    /// </summary>
    public Bool8 displaySyncEnabled {
        get => bool8_objc_msgSend(this.NativePtr, sel_displaySyncEnabled);
        set => objc_msgSend(this.NativePtr, sel_setDisplaySyncEnabled, value);
    }

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="CAMetalLayer">The value of CAMetalLayer.</param>
    /// <returns>The result of the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(CAMetalLayer));

    /// <summary>
    /// Represents the sel_device field.
    /// </summary>
    private static readonly Selector sel_device = "device";

    /// <summary>
    /// Represents the sel_setDevice field.
    /// </summary>
    private static readonly Selector sel_setDevice = "setDevice:";

    /// <summary>
    /// Represents the sel_pixelFormat field.
    /// </summary>
    private static readonly Selector sel_pixelFormat = "pixelFormat";

    /// <summary>
    /// Represents the sel_setPixelFormat field.
    /// </summary>
    private static readonly Selector sel_setPixelFormat = "setPixelFormat:";

    /// <summary>
    /// Represents the sel_framebufferOnly field.
    /// </summary>
    private static readonly Selector sel_framebufferOnly = "framebufferOnly";

    /// <summary>
    /// Represents the sel_setFramebufferOnly field.
    /// </summary>
    private static readonly Selector sel_setFramebufferOnly = "setFramebufferOnly:";

    /// <summary>
    /// Represents the sel_drawableSize field.
    /// </summary>
    private static readonly Selector sel_drawableSize = "drawableSize";

    /// <summary>
    /// Represents the sel_setDrawableSize field.
    /// </summary>
    private static readonly Selector sel_setDrawableSize = "setDrawableSize:";

    /// <summary>
    /// Represents the sel_frame field.
    /// </summary>
    private static readonly Selector sel_frame = "frame";

    /// <summary>
    /// Represents the sel_setFrame field.
    /// </summary>
    private static readonly Selector sel_setFrame = "setFrame:";

    /// <summary>
    /// Represents the sel_isOpaque field.
    /// </summary>
    private static readonly Selector sel_isOpaque = "isOpaque";

    /// <summary>
    /// Represents the sel_setOpaque field.
    /// </summary>
    private static readonly Selector sel_setOpaque = "setOpaque:";

    /// <summary>
    /// Represents the sel_displaySyncEnabled field.
    /// </summary>
    private static readonly Selector sel_displaySyncEnabled = "displaySyncEnabled";

    /// <summary>
    /// Represents the sel_setDisplaySyncEnabled field.
    /// </summary>
    private static readonly Selector sel_setDisplaySyncEnabled = "setDisplaySyncEnabled:";

    /// <summary>
    /// Represents the sel_nextDrawable field.
    /// </summary>
    private static readonly Selector sel_nextDrawable = "nextDrawable";
}