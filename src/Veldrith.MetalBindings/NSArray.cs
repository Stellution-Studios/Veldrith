using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSArray struct.
/// </summary>
public struct NSArray {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSArray" /> class.
    /// </summary>
    public NSArray(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets count.
    /// </summary>
    public UIntPtr count => UIntPtr_objc_msgSend(this.NativePtr, sel_count);

    /// <summary>
    /// Represents the sel_count field.
    /// </summary>
    private static readonly Selector sel_count = "count";
}