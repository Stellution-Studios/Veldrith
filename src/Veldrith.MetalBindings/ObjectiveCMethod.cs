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
    /// Initializes a new instance of the <see cref="ObjectiveCMethod" /> class.
    /// </summary>
    public ObjectiveCMethod(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes IntPtr.
    /// </summary>
    public static implicit operator IntPtr(ObjectiveCMethod method) {
        return method.NativePtr;
    }

    /// <summary>
    /// Executes ObjectiveCMethod.
    /// </summary>
    public static implicit operator ObjectiveCMethod(IntPtr ptr) {
        return new ObjectiveCMethod(ptr);
    }

    /// <summary>
    /// Executes GetSelector.
    /// </summary>
    public Selector GetSelector() {
        return ObjectiveCRuntime.method_getName(this);
    }

    /// <summary>
    /// Executes GetName.
    /// </summary>
    public string GetName() {
        return this.GetSelector().Name;
    }
}