using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSWindow data structure used by the graphics runtime.
/// </summary>
public struct NSWindow {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSWindow" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public NSWindow(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets contentView.
    /// </summary>

    public NSView ContentView => ObjcMsgSend<NSView>(this.NativePtr, _selContentView);

    /// <summary>
    /// Stores the sel content view state used by this instance.
    /// </summary>
    private static readonly Selector _selContentView = "contentView";
}
