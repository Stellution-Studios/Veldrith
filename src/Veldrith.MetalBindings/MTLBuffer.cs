using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct MTLBuffer {
    public readonly IntPtr NativePtr;

    public MTLBuffer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public bool IsNull => this.NativePtr == IntPtr.Zero;

    public void* contents() {
        return ObjectiveCRuntime.IntPtr_objc_msgSend(this.NativePtr, sel_contents).ToPointer();
    }

    public UIntPtr length => ObjectiveCRuntime.UIntPtr_objc_msgSend(this.NativePtr, sel_length);

    public void didModifyRange(NSRange range) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_didModifyRange, range);
    }

    public void addDebugMarker(NSString marker, NSRange range) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_addDebugMarker, marker.NativePtr, range);
    }

    public void removeAllDebugMarkers() {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_removeAllDebugMarkers);
    }

    private static readonly Selector sel_contents = "contents";
    private static readonly Selector sel_length = "length";
    private static readonly Selector sel_didModifyRange = "didModifyRange:";
    private static readonly Selector sel_addDebugMarker = "addDebugMarker:range:";
    private static readonly Selector sel_removeAllDebugMarkers = "removeAllDebugMarkers";
}