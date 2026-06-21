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
        return _sClass.AllocInit<CAMetalLayer>();
    }

    /// <summary>
    /// Attempts to cast and reports whether it succeeded.
    /// </summary>
    /// <param name="layerPointer">The layer pointer value used by this operation.</param>
    /// <param name="metalLayer">The metal layer value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public static bool TryCast(IntPtr layerPointer, out CAMetalLayer metalLayer) {
        NSObject layerObject = new(layerPointer);

        if (layerObject.IsKindOfClass(_sClass)) {
            metalLayer = new CAMetalLayer(layerPointer);
            return true;
        }

        metalLayer = default;
        return false;
    }

    /// <summary>
    /// Gets or sets device.
    /// </summary>
    public MTLDevice Device {
        get => ObjcMsgSend<MTLDevice>(this.NativePtr, _selDevice);
        set => ObjcMsgSend(this.NativePtr, _selSetDevice, value);
    }

    /// <summary>
    /// Gets or sets pixelFormat.
    /// </summary>
    public MTLPixelFormat PixelFormat {
        get => (MTLPixelFormat)UIntObjcMsgSend(this.NativePtr, _selPixelFormat);
        set => ObjcMsgSend(this.NativePtr, _selSetPixelFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets framebufferOnly.
    /// </summary>
    public Bool8 FramebufferOnly {
        get => Bool8ObjcMsgSend(this.NativePtr, _selFramebufferOnly);
        set => ObjcMsgSend(this.NativePtr, _selSetFramebufferOnly, value);
    }

    /// <summary>
    /// Gets or sets drawableSize.
    /// </summary>
    public CGSize DrawableSize {
        get => CGSize_objc_msgSend(this.NativePtr, _selDrawableSize);
        set => ObjcMsgSend(this.NativePtr, _selSetDrawableSize, value);
    }

    /// <summary>
    /// Gets or sets frame.
    /// </summary>
    public CGRect Frame {
        get => CGRect_objc_msgSend(this.NativePtr, _selFrame);
        set => ObjcMsgSend(this.NativePtr, _selSetFrame, value);
    }

    /// <summary>
    /// Gets or sets opaque.
    /// </summary>
    public Bool8 Opaque {
        get => Bool8ObjcMsgSend(this.NativePtr, _selIsOpaque);
        set => ObjcMsgSend(this.NativePtr, _selSetOpaque, value);
    }

    /// <summary>
    /// Executes the next drawable logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public CAMetalDrawable NextDrawable() {
        return ObjcMsgSend<CAMetalDrawable>(this.NativePtr, _selNextDrawable);
    }

    /// <summary>
    /// Gets or sets displaySyncEnabled.
    /// </summary>
    public Bool8 DisplaySyncEnabled {
        get => Bool8ObjcMsgSend(this.NativePtr, _selDisplaySyncEnabled);
        set => ObjcMsgSend(this.NativePtr, _selSetDisplaySyncEnabled, value);
    }

    /// <summary>
    /// Stores the s class state used by this instance.
    /// </summary>
    private static readonly ObjCClass _sClass = new(nameof(CAMetalLayer));

    /// <summary>
    /// Stores the sel device state used by this instance.
    /// </summary>
    private static readonly Selector _selDevice = "device";

    /// <summary>
    /// Stores the sel set device state used by this instance.
    /// </summary>
    private static readonly Selector _selSetDevice = "setDevice:";

    /// <summary>
    /// Stores the sel pixel format state used by this instance.
    /// </summary>
    private static readonly Selector _selPixelFormat = "pixelFormat";

    /// <summary>
    /// Stores the sel set pixel format state used by this instance.
    /// </summary>
    private static readonly Selector _selSetPixelFormat = "setPixelFormat:";

    /// <summary>
    /// Stores the sel framebuffer only state used by this instance.
    /// </summary>
    private static readonly Selector _selFramebufferOnly = "framebufferOnly";

    /// <summary>
    /// Stores the sel set framebuffer only state used by this instance.
    /// </summary>
    private static readonly Selector _selSetFramebufferOnly = "setFramebufferOnly:";

    /// <summary>
    /// Stores the sel drawable size value used during command execution.
    /// </summary>
    private static readonly Selector _selDrawableSize = "drawableSize";

    /// <summary>
    /// Stores the sel set drawable size value used during command execution.
    /// </summary>
    private static readonly Selector _selSetDrawableSize = "setDrawableSize:";

    /// <summary>
    /// Stores the sel frame state used by this instance.
    /// </summary>
    private static readonly Selector _selFrame = "frame";

    /// <summary>
    /// Stores the sel set frame state used by this instance.
    /// </summary>
    private static readonly Selector _selSetFrame = "setFrame:";

    /// <summary>
    /// Stores the sel is opaque state used by this instance.
    /// </summary>
    private static readonly Selector _selIsOpaque = "isOpaque";

    /// <summary>
    /// Stores the sel set opaque state used by this instance.
    /// </summary>
    private static readonly Selector _selSetOpaque = "setOpaque:";

    /// <summary>
    /// Stores the sel display sync enabled state used by this instance.
    /// </summary>
    private static readonly Selector _selDisplaySyncEnabled = "displaySyncEnabled";

    /// <summary>
    /// Stores the sel set display sync enabled state used by this instance.
    /// </summary>
    private static readonly Selector _selSetDisplaySyncEnabled = "setDisplaySyncEnabled:";

    /// <summary>
    /// Stores the sel next drawable state used by this instance.
    /// </summary>
    private static readonly Selector _selNextDrawable = "nextDrawable";
}

