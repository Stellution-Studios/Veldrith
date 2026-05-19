using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the UIView struct.
/// </summary>
public struct UIView {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIView" /> class.
    /// </summary>
    public UIView(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets layer.
    /// </summary>
    public CALayer layer => objc_msgSend<CALayer>(this.NativePtr, sel_layer);

    /// <summary>
    /// Represents the frame field.
    /// </summary>
    public CGRect frame =>
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? CGRect_objc_msgSend(this.NativePtr, sel_frame)
            : objc_msgSend_stret<CGRect>(this.NativePtr, sel_frame);

    /// <summary>
    /// Represents the sel_layer field.
    /// </summary>
    private static readonly Selector sel_layer = "layer";

    /// <summary>
    /// Represents the sel_frame field.
    /// </summary>
    private static readonly Selector sel_frame = "frame";
}