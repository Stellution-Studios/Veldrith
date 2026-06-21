using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSArray data structure used by the graphics runtime.
/// </summary>
public struct NSArray {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSArray" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public NSArray(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the uint ptr objc msg send logic for this backend.
    /// </summary>

    public UIntPtr Count => UIntPtr_objc_msgSend(this.NativePtr, _selCount);

    /// <summary>
    /// Stores the sel count value used during command execution.
    /// </summary>
    private static readonly Selector _selCount = "count";
}