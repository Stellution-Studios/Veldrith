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
    /// Executes New.
    /// </summary>
    public static MTLFunctionConstantValues New() {
        return s_class.AllocInit<MTLFunctionConstantValues>();
    }

    /// <summary>
    /// Executes setConstantValuetypeatIndex.
    /// </summary>
    public unsafe void setConstantValuetypeatIndex(void* value, MTLDataType type, UIntPtr index) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, sel_setConstantValuetypeatIndex, value, (uint)type, index);
    }

    /// <summary>
    /// Represents the s_class field.
    /// </summary>
    private static readonly ObjCClass s_class = new(nameof(MTLFunctionConstantValues));

    /// <summary>
    /// Represents the sel_setConstantValuetypeatIndex field.
    /// </summary>
    private static readonly Selector sel_setConstantValuetypeatIndex = "setConstantValue:type:atIndex:";
}