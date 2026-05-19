using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSWindow struct.
/// </summary>
public struct NSWindow {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSWindow" /> class.
    /// </summary>
    public NSWindow(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets contentView.
    /// </summary>
    public NSView contentView => objc_msgSend<NSView>(this.NativePtr, sel_contentView);

    /// <summary>
    /// Represents the sel_contentView field.
    /// </summary>
    private static readonly Selector sel_contentView = "contentView";
}