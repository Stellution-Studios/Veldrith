using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSDictionary struct.
/// </summary>
public struct NSDictionary {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Performs the UIntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">The value of NativePtr.</param>
    /// <param name="sel_count">The value of sel_count.</param>
    /// <returns>The result of the UIntPtr_objc_msgSend operation.</returns>
    public UIntPtr count => ObjectiveCRuntime.UIntPtr_objc_msgSend(this.NativePtr, sel_count);

    /// <summary>
    /// Represents the sel_count field.
    /// </summary>
    private static readonly Selector sel_count = "count";
}