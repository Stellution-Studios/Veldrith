using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLRenderPassColorAttachmentDescriptorArray struct.
/// </summary>
public struct MTLRenderPassColorAttachmentDescriptorArray {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets this[uint index].
    /// </summary>
    public MTLRenderPassColorAttachmentDescriptor this[uint index] {
        get {
            IntPtr value = IntPtr_objc_msgSend(this.NativePtr, Selectors.objectAtIndexedSubscript, (UIntPtr)index);
            return new MTLRenderPassColorAttachmentDescriptor(value);
        }
        set => objc_msgSend(this.NativePtr, Selectors.setObjectAtIndexedSubscript, value.NativePtr, (UIntPtr)index);
    }
}