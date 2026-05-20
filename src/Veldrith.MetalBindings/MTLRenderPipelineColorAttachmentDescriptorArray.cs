using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRenderPipelineColorAttachmentDescriptorArray data structure used by the graphics runtime.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MTLRenderPipelineColorAttachmentDescriptorArray {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Gets or sets this[uint index].
    /// </summary>
    public MTLRenderPipelineColorAttachmentDescriptor this[uint index] {
        get {
            IntPtr ptr = IntPtr_objc_msgSend(this.NativePtr, Selectors.ObjectAtIndexedSubscript, index);
            return new MTLRenderPipelineColorAttachmentDescriptor(ptr);
        }
        set => objc_msgSend(this.NativePtr, Selectors.SetObjectAtIndexedSubscript, value.NativePtr, index);
    }
}