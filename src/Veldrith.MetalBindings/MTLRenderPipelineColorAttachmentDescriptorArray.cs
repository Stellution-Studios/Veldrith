using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLRenderPipelineColorAttachmentDescriptorArray struct.
/// </summary>
public struct MTLRenderPipelineColorAttachmentDescriptorArray {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets this[uint index].
    /// </summary>
    public MTLRenderPipelineColorAttachmentDescriptor this[uint index] {
        get {
            IntPtr ptr = IntPtr_objc_msgSend(this.NativePtr, Selectors.objectAtIndexedSubscript, index);
            return new MTLRenderPipelineColorAttachmentDescriptor(ptr);
        }
        set => objc_msgSend(this.NativePtr, Selectors.setObjectAtIndexedSubscript, value.NativePtr, index);
    }
}