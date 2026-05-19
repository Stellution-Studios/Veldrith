using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLVertexAttributeDescriptorArray struct.
/// </summary>
public struct MTLVertexAttributeDescriptorArray {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
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