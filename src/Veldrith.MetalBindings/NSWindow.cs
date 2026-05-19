using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings
{
    public struct NSWindow
    {
        public readonly IntPtr NativePtr;
        public NSWindow(IntPtr ptr)
        {
            NativePtr = ptr;
        }

        public NSView contentView => objc_msgSend<NSView>(NativePtr, sel_contentView);

        private static readonly Selector sel_contentView = "contentView";
    }
}
