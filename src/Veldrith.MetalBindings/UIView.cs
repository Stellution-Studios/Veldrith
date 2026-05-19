using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the UIView data structure used by the graphics runtime.
/// </summary>
public struct UIView {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIView" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public UIView(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets layer.
    /// </summary>

    public CALayer layer => objc_msgSend<CALayer>(this.NativePtr, sel_layer);

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
    /// Stores the sel layer state used by this instance.
    /// </summary>
    private static readonly Selector sel_layer = "layer";

    /// <summary>
    /// Stores the sel frame state used by this instance.
    /// </summary>
    private static readonly Selector sel_frame = "frame";
}