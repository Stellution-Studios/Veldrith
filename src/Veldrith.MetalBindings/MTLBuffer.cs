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
    /// Initializes a new instance of the <see cref="MTLBuffer" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public MTLBuffer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Performs the contents operation.
    /// </summary>
    /// <returns>The result of the contents operation.</returns>
    public void* contents() {
        return ObjectiveCRuntime.IntPtr_objc_msgSend(this.NativePtr, sel_contents).ToPointer();
    }

    /// <summary>
    /// Performs the UIntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">The value of NativePtr.</param>
    /// <param name="sel_length">The value of sel_length.</param>
    /// <returns>The result of the UIntPtr_objc_msgSend operation.</returns>
    public UIntPtr length => ObjectiveCRuntime.UIntPtr_objc_msgSend(this.NativePtr, sel_length);

    /// <summary>
    /// Performs the didModifyRange operation.
    /// </summary>
    /// <param name="range">The value of range.</param>
    public void didModifyRange(NSRange range) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_didModifyRange, range);
    }

    /// <summary>
    /// Performs the addDebugMarker operation.
    /// </summary>
    /// <param name="marker">The value of marker.</param>
    /// <param name="range">The value of range.</param>
    public void addDebugMarker(NSString marker, NSRange range) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_addDebugMarker, marker.NativePtr, range);
    }

    /// <summary>
    /// Performs the removeAllDebugMarkers operation.
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