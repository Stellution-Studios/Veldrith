using System;
using System.Runtime.InteropServices;

namespace Veldrith.MetalBindings;

/// <summary>
/// Provides Objective-C interop bindings for MTLBuffer.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct MTLBuffer {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLBuffer" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public MTLBuffer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets IsNull.
    /// </summary>
    public bool IsNull => this.NativePtr == IntPtr.Zero;

    /// <summary>
    /// Executes the contents logic for this backend.
    /// </summary>
    public void* Contents() {
        return ObjectiveCRuntime.IntPtr_objc_msgSend(this.NativePtr, sel_contents).ToPointer();
    }

    /// <summary>
    /// Executes the uint ptr objc msg send logic for this backend.
    /// </summary>

    public UIntPtr Length => ObjectiveCRuntime.UIntPtr_objc_msgSend(this.NativePtr, sel_length);

    /// <summary>
    /// Executes the did modify range logic for this backend.
    /// </summary>
    /// <param name="range">The range value used by this operation.</param>
    public void DidModifyRange(NSRange range) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_didModifyRange, range);
    }

    /// <summary>
    /// Executes the add debug marker logic for this backend.
    /// </summary>
    /// <param name="marker">The marker value used by this operation.</param>
    /// <param name="range">The range value used by this operation.</param>
    public void AddDebugMarker(NSString marker, NSRange range) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_addDebugMarker, marker.NativePtr, range);
    }

    /// <summary>
    /// Executes the remove all debug markers logic for this backend.
    /// </summary>
    public void RemoveAllDebugMarkers() {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_removeAllDebugMarkers);
    }

    /// <summary>
    /// Stores the sel contents state used by this instance.
    /// </summary>
    private static readonly Selector sel_contents = "contents";

    /// <summary>
    /// Stores the sel length state used by this instance.
    /// </summary>
    private static readonly Selector sel_length = "length";

    /// <summary>
    /// Stores the sel did modify range state used by this instance.
    /// </summary>
    private static readonly Selector sel_didModifyRange = "didModifyRange:";

    /// <summary>
    /// Stores the sel add debug marker state used by this instance.
    /// </summary>
    private static readonly Selector sel_addDebugMarker = "addDebugMarker:range:";

    /// <summary>
    /// Stores the sel remove all debug markers state used by this instance.
    /// </summary>
    private static readonly Selector sel_removeAllDebugMarkers = "removeAllDebugMarkers";
}