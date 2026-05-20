using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLPipelineBufferDescriptorArray data structure used by the graphics runtime.
/// </summary>
public struct MTLPipelineBufferDescriptorArray {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets this[uint index].
    /// </summary>
    public MTLPipelineBufferDescriptor this[uint index] {
        get {
            IntPtr value = IntPtr_objc_msgSend(this.NativePtr, Selectors.ObjectAtIndexedSubscript, (UIntPtr)index);
            return new MTLPipelineBufferDescriptor(value);
        }
        set => objc_msgSend(this.NativePtr, Selectors.SetObjectAtIndexedSubscript, value.NativePtr, (UIntPtr)index);
    }
}