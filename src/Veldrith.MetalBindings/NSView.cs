using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSView data structure used by the graphics runtime.
/// </summary>
public struct NSView {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the int ptr logic for this backend.
    /// </summary>
    /// <param name="nsView">The ns view value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator IntPtr(NSView nsView) {
        return nsView.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NSView" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
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
    /// Stores the frame state used by this instance.
    /// </summary>
    public CGRect frame =>
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64

            /// <summary>
            /// Executes the cgrect objc msg send logic for this backend.
            /// </summary>
            /// <param name="sel_frame">The sel frame value used by this operation.</param>
            ? CGRect_objc_msgSend(this.NativePtr, sel_frame)
            : objc_msgSend_stret<CGRect>(this.NativePtr, sel_frame);

    /// <summary>
    /// Stores the sel wants layer state used by this instance.
    /// </summary>
    private static readonly Selector sel_wantsLayer = "wantsLayer";

    /// <summary>
    /// Stores the sel set wants layer state used by this instance.
    /// </summary>
    private static readonly Selector sel_setWantsLayer = "setWantsLayer:";

    /// <summary>
    /// Stores the sel layer state used by this instance.
    /// </summary>
    private static readonly Selector sel_layer = "layer";

    /// <summary>
    /// Stores the sel set layer state used by this instance.
    /// </summary>
    private static readonly Selector sel_setLayer = "setLayer:";

    /// <summary>
    /// Stores the sel frame state used by this instance.
    /// </summary>
    private static readonly Selector sel_frame = "frame";
}