using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the ObjectiveCMethod struct.
/// </summary>
public struct ObjectiveCMethod {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectiveCMethod" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public ObjectiveCMethod(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the operator IntPtr operation.
    /// </summary>
    /// <param name="method">Specifies the value of <paramref name="method" />.</param>
    /// <returns>Returns the result produced by the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(ObjectiveCMethod method) {
        return method.NativePtr;
    }

    /// <summary>
    /// Executes the operator ObjectiveCMethod operation.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    /// <returns>Returns the result produced by the operator ObjectiveCMethod operation.</returns>
    public static implicit operator ObjectiveCMethod(IntPtr ptr) {
        return new ObjectiveCMethod(ptr);
    }

    /// <summary>
    /// Executes the GetSelector operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetSelector operation.</returns>
    public Selector GetSelector() {
        return ObjectiveCRuntime.method_getName(this);
    }

    /// <summary>
    /// Executes the GetName operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetName operation.</returns>
    public string GetName() {
        return this.GetSelector().Name;
    }
}