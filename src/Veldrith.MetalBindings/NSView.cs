using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the NSView struct.
/// </summary>
public struct NSView {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the operator IntPtr operation.
    /// </summary>
    /// <param name="nsView">Specifies the value of <paramref name="nsView" />.</param>
    /// <returns>Returns the result produced by the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(NSView nsView) {
        return nsView.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NSView" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
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
    /// Stores the value associated with <c>frame</c>.
    /// </summary>
    public CGRect frame =>
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64

            /// <summary>
            /// Executes the CGRect_objc_msgSend operation.
            /// </summary>
            /// <param name="NativePtr">Specifies the value of <paramref name="NativePtr" />.</param>
            /// <param name="sel_frame">Specifies the value of <paramref name="sel_frame" />.</param>
            /// <returns>Returns the result produced by the CGRect_objc_msgSend operation.</returns>
            ? CGRect_objc_msgSend(this.NativePtr, sel_frame)
            : objc_msgSend_stret<CGRect>(this.NativePtr, sel_frame);

    /// <summary>
    /// Stores the value associated with <c>sel_wantsLayer</c>.
    /// </summary>
    private static readonly Selector sel_wantsLayer = "wantsLayer";

    /// <summary>
    /// Stores the value associated with <c>sel_setWantsLayer</c>.
    /// </summary>
    private static readonly Selector sel_setWantsLayer = "setWantsLayer:";

    /// <summary>
    /// Stores the value associated with <c>sel_layer</c>.
    /// </summary>
    private static readonly Selector sel_layer = "layer";

    /// <summary>
    /// Stores the value associated with <c>sel_setLayer</c>.
    /// </summary>
    private static readonly Selector sel_setLayer = "setLayer:";

    /// <summary>
    /// Stores the value associated with <c>sel_frame</c>.
    /// </summary>
    private static readonly Selector sel_frame = "frame";
}