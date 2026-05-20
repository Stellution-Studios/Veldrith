using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSDictionary data structure used by the graphics runtime.
/// </summary>
public struct NSDictionary {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the uint ptr objc msg send logic for this backend.
    /// </summary>

    public UIntPtr Count => ObjectiveCRuntime.UIntPtr_objc_msgSend(this.NativePtr, _selCount);

    /// <summary>
    /// Stores the sel count value used during command execution.
    /// </summary>
    private static readonly Selector _selCount = "count";
}