using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRenderPassColorAttachmentDescriptorArray data structure used by the graphics runtime.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MTLRenderPassColorAttachmentDescriptorArray {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets this[uint index].
    /// </summary>
    public MTLRenderPassColorAttachmentDescriptor this[uint index] {
        get {
            IntPtr value = IntPtr_objc_msgSend(this.NativePtr, Selectors.ObjectAtIndexedSubscript, (UIntPtr)index);
            return new MTLRenderPassColorAttachmentDescriptor(value);
        }
        set => objc_msgSend(this.NativePtr, Selectors.SetObjectAtIndexedSubscript, value.NativePtr, (UIntPtr)index);
    }
}