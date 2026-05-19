using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLFunctionConstantValues data structure used by the graphics runtime.
/// </summary>
public struct MTLFunctionConstantValues {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static MTLFunctionConstantValues New() {
        return s_class.AllocInit<MTLFunctionConstantValues>();
    }

    /// <summary>
    /// Sets the constant valuetypeat index value.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public unsafe void setConstantValuetypeatIndex(void* value, MTLDataType type, UIntPtr index) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_setConstantValuetypeatIndex, value, (uint)type, index);
    }

    /// <summary>
    /// Stores the s class state used by this instance.
    /// </summary>
    private static readonly ObjCClass s_class = new(nameof(MTLFunctionConstantValues));

    /// <summary>
    /// Stores the sel set constant valuetypeat index value used during command execution.
    /// </summary>
    private static readonly Selector sel_setConstantValuetypeatIndex = "setConstantValue:type:atIndex:";
}