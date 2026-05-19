using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLBuffer struct.
/// </summary>
public unsafe struct MTLBuffer {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLBuffer" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public MTLBuffer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Executes the contents operation.
    /// </summary>
    /// <returns>Returns the result produced by the contents operation.</returns>
    public void* contents() {
        return ObjectiveCRuntime.IntPtr_objc_msgSend(this.NativePtr, sel_contents).ToPointer();
    }

    /// <summary>
    /// Executes the UIntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">Specifies the value of <paramref name="NativePtr" />.</param>
    /// <param name="sel_length">Specifies the value of <paramref name="sel_length" />.</param>
    /// <returns>Returns the result produced by the UIntPtr_objc_msgSend operation.</returns>
    public UIntPtr length => ObjectiveCRuntime.UIntPtr_objc_msgSend(this.NativePtr, sel_length);

    /// <summary>
    /// Executes the didModifyRange operation.
    /// </summary>
    /// <param name="range">Specifies the value of <paramref name="range" />.</param>
    public void didModifyRange(NSRange range) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_didModifyRange, range);
    }

    /// <summary>
    /// Executes the addDebugMarker operation.
    /// </summary>
    /// <param name="marker">Specifies the value of <paramref name="marker" />.</param>
    /// <param name="range">Specifies the value of <paramref name="range" />.</param>
    public void addDebugMarker(NSString marker, NSRange range) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_addDebugMarker, marker.NativePtr, range);
    }

    /// <summary>
    /// Executes the removeAllDebugMarkers operation.
    /// </summary>
    public void removeAllDebugMarkers() {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_removeAllDebugMarkers);
    }

    /// <summary>
    /// Stores the value associated with <c>sel_contents</c>.
    /// </summary>
    private static readonly Selector sel_contents = "contents";

    /// <summary>
    /// Stores the value associated with <c>sel_length</c>.
    /// </summary>
    private static readonly Selector sel_length = "length";

    /// <summary>
    /// Stores the value associated with <c>sel_didModifyRange</c>.
    /// </summary>
    private static readonly Selector sel_didModifyRange = "didModifyRange:";

    /// <summary>
    /// Stores the value associated with <c>sel_addDebugMarker</c>.
    /// </summary>
    private static readonly Selector sel_addDebugMarker = "addDebugMarker:range:";

    /// <summary>
    /// Stores the value associated with <c>sel_removeAllDebugMarkers</c>.
    /// </summary>
    private static readonly Selector sel_removeAllDebugMarkers = "removeAllDebugMarkers";
}