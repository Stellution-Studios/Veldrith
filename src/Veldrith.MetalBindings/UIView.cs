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

    public CALayer Layer => ObjcMsgSend<CALayer>(this.NativePtr, _selLayer);

    /// <summary>
    /// Stores the frame state used by this instance.
    /// </summary>
    public CGRect Frame => RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? CGRect_objc_msgSend(this.NativePtr, _selFrame) : ObjcMsgSendStret<CGRect>(this.NativePtr, _selFrame);

    /// <summary>
    /// Stores the sel layer state used by this instance.
    /// </summary>
    private static readonly Selector _selLayer = "layer";

    /// <summary>
    /// Stores the sel frame state used by this instance.
    /// </summary>
    private static readonly Selector _selFrame = "frame";
}
