using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLVertexAttributeDescriptorArray data structure used by the graphics runtime.
/// </summary>
public struct MTLVertexAttributeDescriptorArray {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets this[uint index].
    /// </summary>
    public MTLVertexAttributeDescriptor this[uint index] {
        get {
            IntPtr value = IntPtr_objc_msgSend(this.NativePtr, Selectors.objectAtIndexedSubscript, index);
            return new MTLVertexAttributeDescriptor(value);
        }
        set => objc_msgSend(this.NativePtr, Selectors.setObjectAtIndexedSubscript, value.NativePtr, index);
    }
}