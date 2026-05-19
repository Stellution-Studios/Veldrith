using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLFunctionConstantValues struct.
/// </summary>
public struct MTLFunctionConstantValues {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>Returns the result produced by the New operation.</returns>
    public static MTLFunctionConstantValues New() {
        return s_class.AllocInit<MTLFunctionConstantValues>();
    }

    /// <summary>
    /// Executes the setConstantValuetypeatIndex operation.
    /// </summary>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    /// <param name="type">Specifies the value of <paramref name="type" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    public unsafe void setConstantValuetypeatIndex(void* value, MTLDataType type, UIntPtr index) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_setConstantValuetypeatIndex, value, (uint)type, index);
    }

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="MTLFunctionConstantValues">Specifies the value of <paramref name="MTLFunctionConstantValues" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(MTLFunctionConstantValues));

    /// <summary>
    /// Stores the value associated with <c>sel_setConstantValuetypeatIndex</c>.
    /// </summary>
    private static readonly Selector sel_setConstantValuetypeatIndex = "setConstantValue:type:atIndex:";
}
