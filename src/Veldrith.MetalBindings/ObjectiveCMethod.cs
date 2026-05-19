using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the ObjectiveCMethod struct.
/// </summary>
public struct ObjectiveCMethod {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectiveCMethod" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public ObjectiveCMethod(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Performs the operator IntPtr operation.
    /// </summary>
    /// <param name="method">The value of method.</param>
    /// <returns>The result of the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(ObjectiveCMethod method) {
        return method.NativePtr;
    }

    /// <summary>
    /// Performs the operator ObjectiveCMethod operation.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    /// <returns>The result of the operator ObjectiveCMethod operation.</returns>
    public static implicit operator ObjectiveCMethod(IntPtr ptr) {
        return new ObjectiveCMethod(ptr);
    }

    /// <summary>
    /// Performs the GetSelector operation.
    /// </summary>
    /// <returns>The result of the GetSelector operation.</returns>
    public Selector GetSelector() {
        return ObjectiveCRuntime.method_getName(this);
    }

    /// <summary>
    /// Performs the GetName operation.
    /// </summary>
    /// <returns>The result of the GetName operation.</returns>
    public string GetName() {
        return this.GetSelector().Name;
    }
}