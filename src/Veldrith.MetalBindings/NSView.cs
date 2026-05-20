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
    public Bool8 WantsLayer {
        get => bool8_objc_msgSend(this.NativePtr, _selWantsLayer);
        set => objc_msgSend(this.NativePtr, _selSetWantsLayer, value);
    }

    /// <summary>
    /// Gets or sets layer.
    /// </summary>
    public IntPtr Layer {
        get => IntPtr_objc_msgSend(this.NativePtr, _selLayer);
        set => objc_msgSend(this.NativePtr, _selSetLayer, value);
    }

    /// <summary>
    /// Stores the frame state used by this instance.
    /// </summary>
    public CGRect Frame => RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? CGRect_objc_msgSend(this.NativePtr, _selFrame) : objc_msgSend_stret<CGRect>(this.NativePtr, _selFrame);

    /// <summary>
    /// Stores the sel wants layer state used by this instance.
    /// </summary>
    private static readonly Selector _selWantsLayer = "wantsLayer";

    /// <summary>
    /// Stores the sel set wants layer state used by this instance.
    /// </summary>
    private static readonly Selector _selSetWantsLayer = "setWantsLayer:";

    /// <summary>
    /// Stores the sel layer state used by this instance.
    /// </summary>
    private static readonly Selector _selLayer = "layer";

    /// <summary>
    /// Stores the sel set layer state used by this instance.
    /// </summary>
    private static readonly Selector _selSetLayer = "setLayer:";

    /// <summary>
    /// Stores the sel frame state used by this instance.
    /// </summary>
    private static readonly Selector _selFrame = "frame";
}