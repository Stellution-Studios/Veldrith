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
    /// Gets or sets count.
    /// </summary>
    public UIntPtr count => ObjectiveCRuntime.UIntPtr_objc_msgSend(this.NativePtr, sel_count);

    /// <summary>
    /// Represents the sel_count field.
    /// </summary>
    private static readonly Selector sel_count = "count";
}