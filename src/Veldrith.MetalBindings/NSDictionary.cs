using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the NSDictionary struct.
/// </summary>
public struct NSDictionary {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the UIntPtr_objc_msgSend operation.
    /// </summary>
    /// <param name="NativePtr">Specifies the value of <paramref name="NativePtr" />.</param>
    /// <param name="sel_count">Specifies the value of <paramref name="sel_count" />.</param>
    /// <returns>Returns the result produced by the UIntPtr_objc_msgSend operation.</returns>
    public UIntPtr count => ObjectiveCRuntime.UIntPtr_objc_msgSend(this.NativePtr, sel_count);

    /// <summary>
    /// Stores the value associated with <c>sel_count</c>.
    /// </summary>
    private static readonly Selector sel_count = "count";
}