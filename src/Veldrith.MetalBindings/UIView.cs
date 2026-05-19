using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the UIView struct.
/// </summary>
public struct UIView {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIView" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public UIView(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets layer.
    /// </summary>
    public CALayer layer => objc_msgSend<CALayer>(this.NativePtr, sel_layer);

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
    /// Stores the value associated with <c>sel_layer</c>.
    /// </summary>
    private static readonly Selector sel_layer = "layer";

    /// <summary>
    /// Stores the value associated with <c>sel_frame</c>.
    /// </summary>
    private static readonly Selector sel_frame = "frame";
}