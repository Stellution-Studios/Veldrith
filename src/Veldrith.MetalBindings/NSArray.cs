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
    /// Initializes a new instance of the <see cref="NSArray" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public NSArray(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Performs the UIntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">The value of NativePtr.</param>
    /// <param name="sel_count">The value of sel_count.</param>
    /// <returns>The result of the UIntPtr_objc_msgSend operation.</returns>
    public UIntPtr count => UIntPtr_objc_msgSend(this.NativePtr, sel_count);

    /// <summary>
    /// Represents the sel_count field.
    /// </summary>
    private static readonly Selector sel_count = "count";
}