using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the ObjectiveCMethod data structure used by the graphics runtime.
/// </summary>
public struct ObjectiveCMethod {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectiveCMethod" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public ObjectiveCMethod(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the int ptr logic for this backend.
    /// </summary>
    /// <param name="method">The method value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator IntPtr(ObjectiveCMethod method) {
        return method.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectiveCMethod" /> class.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public static implicit operator ObjectiveCMethod(IntPtr ptr) {
        return new ObjectiveCMethod(ptr);
    }

    /// <summary>
    /// Gets the selector value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public Selector GetSelector() {
        return ObjectiveCRuntime.method_getName(this);
    }

    /// <summary>
    /// Gets the name value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public string GetName() {
        return this.GetSelector().Name;
    }
}