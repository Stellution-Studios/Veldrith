using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the CAMetalLayer struct.
/// </summary>
public struct CAMetalLayer {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="CAMetalLayer" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public CAMetalLayer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>Returns the result produced by the New operation.</returns>
    public static CAMetalLayer New() {
        return s_class.AllocInit<CAMetalLayer>();
    }

    /// <summary>
    /// Executes the TryCast operation.
    /// </summary>
    /// <param name="layerPointer">Specifies the value of <paramref name="layerPointer" />.</param>
    /// <param name="metalLayer">Specifies the value of <paramref name="metalLayer" />.</param>
    /// <returns>Returns the result produced by the TryCast operation.</returns>
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
    /// Executes the nextDrawable operation.
    /// </summary>
    /// <returns>Returns the result produced by the nextDrawable operation.</returns>
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
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="CAMetalLayer">Specifies the value of <paramref name="CAMetalLayer" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(CAMetalLayer));

    /// <summary>
    /// Stores the value associated with <c>sel_device</c>.
    /// </summary>
    private static readonly Selector sel_device = "device";

    /// <summary>
    /// Stores the value associated with <c>sel_setDevice</c>.
    /// </summary>
    private static readonly Selector sel_setDevice = "setDevice:";

    /// <summary>
    /// Stores the value associated with <c>sel_pixelFormat</c>.
    /// </summary>
    private static readonly Selector sel_pixelFormat = "pixelFormat";

    /// <summary>
    /// Stores the value associated with <c>sel_setPixelFormat</c>.
    /// </summary>
    private static readonly Selector sel_setPixelFormat = "setPixelFormat:";

    /// <summary>
    /// Stores the value associated with <c>sel_framebufferOnly</c>.
    /// </summary>
    private static readonly Selector sel_framebufferOnly = "framebufferOnly";

    /// <summary>
    /// Stores the value associated with <c>sel_setFramebufferOnly</c>.
    /// </summary>
    private static readonly Selector sel_setFramebufferOnly = "setFramebufferOnly:";

    /// <summary>
    /// Stores the value associated with <c>sel_drawableSize</c>.
    /// </summary>
    private static readonly Selector sel_drawableSize = "drawableSize";

    /// <summary>
    /// Stores the value associated with <c>sel_setDrawableSize</c>.
    /// </summary>
    private static readonly Selector sel_setDrawableSize = "setDrawableSize:";

    /// <summary>
    /// Stores the value associated with <c>sel_frame</c>.
    /// </summary>
    private static readonly Selector sel_frame = "frame";

    /// <summary>
    /// Stores the value associated with <c>sel_setFrame</c>.
    /// </summary>
    private static readonly Selector sel_setFrame = "setFrame:";

    /// <summary>
    /// Stores the value associated with <c>sel_isOpaque</c>.
    /// </summary>
    private static readonly Selector sel_isOpaque = "isOpaque";

    /// <summary>
    /// Stores the value associated with <c>sel_setOpaque</c>.
    /// </summary>
    private static readonly Selector sel_setOpaque = "setOpaque:";

    /// <summary>
    /// Stores the value associated with <c>sel_displaySyncEnabled</c>.
    /// </summary>
    private static readonly Selector sel_displaySyncEnabled = "displaySyncEnabled";

    /// <summary>
    /// Stores the value associated with <c>sel_setDisplaySyncEnabled</c>.
    /// </summary>
    private static readonly Selector sel_setDisplaySyncEnabled = "setDisplaySyncEnabled:";

    /// <summary>
    /// Stores the value associated with <c>sel_nextDrawable</c>.
    /// </summary>
    private static readonly Selector sel_nextDrawable = "nextDrawable";
}
