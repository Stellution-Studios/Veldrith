using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLFunctionConstantValues struct.
/// </summary>
public struct MTLFunctionConstantValues {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Performs the New operation.
    /// </summary>
    /// <returns>The result of the New operation.</returns>
    public static MTLFunctionConstantValues New() {
        return s_class.AllocInit<MTLFunctionConstantValues>();
    }

    /// <summary>
    /// Performs the setConstantValuetypeatIndex operation.
    /// </summary>
    /// <param name="value">The value of value.</param>
    /// <param name="type">The value of type.</param>
    /// <param name="index">The value of index.</param>
    public unsafe void setConstantValuetypeatIndex(void* value, MTLDataType type, UIntPtr index) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_setConstantValuetypeatIndex, value, (uint)type, index);
    }

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="MTLFunctionConstantValues">The value of MTLFunctionConstantValues.</param>
    /// <returns>The result of the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(MTLFunctionConstantValues));

    /// <summary>
    /// Represents the sel_setConstantValuetypeatIndex field.
    /// </summary>
    private static readonly Selector sel_setConstantValuetypeatIndex = "setConstantValue:type:atIndex:";
}