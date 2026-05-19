using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct MTLVertexAttributeDescriptor {
    public readonly IntPtr NativePtr;

    public MTLVertexAttributeDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    public MTLVertexFormat format {
        get => (MTLVertexFormat)uint_objc_msgSend(this.NativePtr, sel_format);
        set => objc_msgSend(this.NativePtr, sel_setFormat, (uint)value);
    }

    public UIntPtr offset {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_offset);
        set => objc_msgSend(this.NativePtr, sel_setOffset, value);
    }

    public UIntPtr bufferIndex {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_bufferIndex);
        set => objc_msgSend(this.NativePtr, sel_setBufferIndex, value);
    }

    private static readonly Selector sel_format = "format";
    private static readonly Selector sel_setFormat = "setFormat:";
    private static readonly Selector sel_offset = "offset";
    private static readonly Selector sel_setOffset = "setOffset:";
    private static readonly Selector sel_bufferIndex = "bufferIndex";
    private static readonly Selector sel_setBufferIndex = "setBufferIndex:";
}