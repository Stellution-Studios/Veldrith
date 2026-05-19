using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the NSWindow struct.
/// </summary>
public struct NSWindow {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSWindow" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public NSWindow(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets contentView.
    /// </summary>
    public NSView contentView => objc_msgSend<NSView>(this.NativePtr, sel_contentView);

    /// <summary>
    /// Stores the value associated with <c>sel_contentView</c>.
    /// </summary>
    private static readonly Selector sel_contentView = "contentView";
}