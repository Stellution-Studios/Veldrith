using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MTLRenderPassColorAttachmentDescriptorArray
    {
        public readonly IntPtr NativePtr;

        public MTLRenderPassColorAttachmentDescriptor this[uint index]
        {
            get
            {
                IntPtr value = IntPtr_objc_msgSend(NativePtr, Selectors.objectAtIndexedSubscript, (UIntPtr)index);
                return new MTLRenderPassColorAttachmentDescriptor(value);
            }
            set
            {
                objc_msgSend(NativePtr, Selectors.setObjectAtIndexedSubscript, value.NativePtr, (UIntPtr)index);
            }
        }
    }
}