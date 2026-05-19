using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct NSWindow {
    public readonly IntPtr NativePtr;

    public NSWindow(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public NSView contentView => objc_msgSend<NSView>(this.NativePtr, sel_contentView);

    private static readonly Selector sel_contentView = "contentView";
}