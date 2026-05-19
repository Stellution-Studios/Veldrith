using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSView struct.
/// </summary>
public struct NSView {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes IntPtr.
    /// </summary>
    public static implicit operator IntPtr(NSView nsView) {
        return nsView.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NSView" /> class.
    /// </summary>
    public NSView(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets wantsLayer.
    /// </summary>
    public Bool8 wantsLayer {
        get => bool8_objc_msgSend(this.NativePtr, sel_wantsLayer);
        set => objc_msgSend(this.NativePtr, sel_setWantsLayer, value);
    }

    /// <summary>
    /// Gets or sets layer.
    /// </summary>
    public IntPtr layer {
        get => IntPtr_objc_msgSend(this.NativePtr, sel_layer);
        set => objc_msgSend(this.NativePtr, sel_setLayer, value);
    }

    /// <summary>
    /// Represents the frame field.
    /// </summary>
    public CGRect frame =>
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? CGRect_objc_msgSend(this.NativePtr, sel_frame)
            : objc_msgSend_stret<CGRect>(this.NativePtr, sel_frame);

    /// <summary>
    /// Represents the sel_wantsLayer field.
    /// </summary>
    private static readonly Selector sel_wantsLayer = "wantsLayer";

    /// <summary>
    /// Represents the sel_setWantsLayer field.
    /// </summary>
    private static readonly Selector sel_setWantsLayer = "setWantsLayer:";

    /// <summary>
    /// Represents the sel_layer field.
    /// </summary>
    private static readonly Selector sel_layer = "layer";

    /// <summary>
    /// Represents the sel_setLayer field.
    /// </summary>
    private static readonly Selector sel_setLayer = "setLayer:";

    /// <summary>
    /// Represents the sel_frame field.
    /// </summary>
    private static readonly Selector sel_frame = "frame";
}