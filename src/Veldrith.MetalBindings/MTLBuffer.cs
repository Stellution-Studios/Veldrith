using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLBuffer struct.
/// </summary>
public unsafe struct MTLBuffer {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLBuffer" /> class.
    /// </summary>
    public MTLBuffer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Executes contents.
    /// </summary>
    public void* contents() {
        return ObjectiveCRuntime.IntPtr_objc_msgSend(this.NativePtr, sel_contents).ToPointer();
    }

    /// <summary>
    /// Gets or sets length.
    /// </summary>
    public UIntPtr length => ObjectiveCRuntime.UIntPtr_objc_msgSend(this.NativePtr, sel_length);

    /// <summary>
    /// Executes didModifyRange.
    /// </summary>
    public void didModifyRange(NSRange range) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_didModifyRange, range);
    }

    /// <summary>
    /// Executes addDebugMarker.
    /// </summary>
    public void addDebugMarker(NSString marker, NSRange range) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_addDebugMarker, marker.NativePtr, range);
    }

    /// <summary>
    /// Executes removeAllDebugMarkers.
    /// </summary>
    public void removeAllDebugMarkers() {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_removeAllDebugMarkers);
    }

    /// <summary>
    /// Represents the sel_contents field.
    /// </summary>
    private static readonly Selector sel_contents = "contents";

    /// <summary>
    /// Represents the sel_length field.
    /// </summary>
    private static readonly Selector sel_length = "length";

    /// <summary>
    /// Represents the sel_didModifyRange field.
    /// </summary>
    private static readonly Selector sel_didModifyRange = "didModifyRange:";

    /// <summary>
    /// Represents the sel_addDebugMarker field.
    /// </summary>
    private static readonly Selector sel_addDebugMarker = "addDebugMarker:range:";

    /// <summary>
    /// Represents the sel_removeAllDebugMarkers field.
    /// </summary>
    private static readonly Selector sel_removeAllDebugMarkers = "removeAllDebugMarkers";
}