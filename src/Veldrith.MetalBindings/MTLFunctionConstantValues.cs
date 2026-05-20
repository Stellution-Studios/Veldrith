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
        return _sClass.AllocInit<MTLFunctionConstantValues>();
    }

    /// <summary>
    /// Sets the constant valuetypeat index value.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    public unsafe void SetConstantValueTypeAtIndex(void* value, MTLDataType type, UIntPtr index) {
        ObjectiveCRuntime.objc_msgSend(this.NativePtr, _selSetConstantValueTypeAtIndex, value, (uint)type, index);
    }

    /// <summary>
    /// Stores the s class state used by this instance.
    /// </summary>
    private static readonly ObjCClass _sClass = new(nameof(MTLFunctionConstantValues));

    /// <summary>
    /// Stores the sel set constant valuetypeat index value used during command execution.
    /// </summary>
    private static readonly Selector _selSetConstantValueTypeAtIndex = "setConstantValue:type:atIndex:";
}