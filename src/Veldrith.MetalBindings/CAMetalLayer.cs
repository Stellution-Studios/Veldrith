using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CAMetalLayer data structure used by the graphics runtime.
/// </summary>
public struct CAMetalLayer {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="CAMetalLayer" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public CAMetalLayer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static CAMetalLayer New() {
        return s_class.AllocInit<CAMetalLayer>();
    }

    /// <summary>
    /// Attempts to cast and reports whether it succeeded.
    /// </summary>
    /// <param name="layerPointer">The layer pointer value used by this operation.</param>
    /// <param name="metalLayer">The metal layer value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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
    /// Executes the next drawable logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
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
    /// Stores the s class state used by this instance.
    /// </summary>
    private static readonly ObjCClass s_class = new(nameof(CAMetalLayer));

    /// <summary>
    /// Stores the sel device state used by this instance.
    /// </summary>
    private static readonly Selector sel_device = "device";

    /// <summary>
    /// Stores the sel set device state used by this instance.
    /// </summary>
    private static readonly Selector sel_setDevice = "setDevice:";

    /// <summary>
    /// Stores the sel pixel format state used by this instance.
    /// </summary>
    private static readonly Selector sel_pixelFormat = "pixelFormat";

    /// <summary>
    /// Stores the sel set pixel format state used by this instance.
    /// </summary>
    private static readonly Selector sel_setPixelFormat = "setPixelFormat:";

    /// <summary>
    /// Stores the sel framebuffer only state used by this instance.
    /// </summary>
    private static readonly Selector sel_framebufferOnly = "framebufferOnly";

    /// <summary>
    /// Stores the sel set framebuffer only state used by this instance.
    /// </summary>
    private static readonly Selector sel_setFramebufferOnly = "setFramebufferOnly:";

    /// <summary>
    /// Stores the sel drawable size value used during command execution.
    /// </summary>
    private static readonly Selector sel_drawableSize = "drawableSize";

    /// <summary>
    /// Stores the sel set drawable size value used during command execution.
    /// </summary>
    private static readonly Selector sel_setDrawableSize = "setDrawableSize:";

    /// <summary>
    /// Stores the sel frame state used by this instance.
    /// </summary>
    private static readonly Selector sel_frame = "frame";

    /// <summary>
    /// Stores the sel set frame state used by this instance.
    /// </summary>
    private static readonly Selector sel_setFrame = "setFrame:";

    /// <summary>
    /// Stores the sel is opaque state used by this instance.
    /// </summary>
    private static readonly Selector sel_isOpaque = "isOpaque";

    /// <summary>
    /// Stores the sel set opaque state used by this instance.
    /// </summary>
    private static readonly Selector sel_setOpaque = "setOpaque:";

    /// <summary>
    /// Stores the sel display sync enabled state used by this instance.
    /// </summary>
    private static readonly Selector sel_displaySyncEnabled = "displaySyncEnabled";

    /// <summary>
    /// Stores the sel set display sync enabled state used by this instance.
    /// </summary>
    private static readonly Selector sel_setDisplaySyncEnabled = "setDisplaySyncEnabled:";

    /// <summary>
    /// Stores the sel next drawable state used by this instance.
    /// </summary>
    private static readonly Selector sel_nextDrawable = "nextDrawable";
}