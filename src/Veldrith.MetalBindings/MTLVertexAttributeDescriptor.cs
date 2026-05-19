using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLVertexAttributeDescriptor struct.
/// </summary>
public struct MTLVertexAttributeDescriptor {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLVertexAttributeDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public MTLVertexAttributeDescriptor(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets format.
    /// </summary>
    public MTLVertexFormat format {
        get => (MTLVertexFormat)uint_objc_msgSend(this.NativePtr, sel_format);
        set => objc_msgSend(this.NativePtr, sel_setFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets offset.
    /// </summary>
    public UIntPtr offset {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_offset);
        set => objc_msgSend(this.NativePtr, sel_setOffset, value);
    }

    /// <summary>
    /// Gets or sets bufferIndex.
    /// </summary>
    public UIntPtr bufferIndex {
        get => UIntPtr_objc_msgSend(this.NativePtr, sel_bufferIndex);
        set => objc_msgSend(this.NativePtr, sel_setBufferIndex, value);
    }

    /// <summary>
    /// Represents the sel_format field.
    /// </summary>
    private static readonly Selector sel_format = "format";

    /// <summary>
    /// Represents the sel_setFormat field.
    /// </summary>
    private static readonly Selector sel_setFormat = "setFormat:";

    /// <summary>
    /// Represents the sel_offset field.
    /// </summary>
    private static readonly Selector sel_offset = "offset";

    /// <summary>
    /// Represents the sel_setOffset field.
    /// </summary>
    private static readonly Selector sel_setOffset = "setOffset:";

    /// <summary>
    /// Represents the sel_bufferIndex field.
    /// </summary>
    private static readonly Selector sel_bufferIndex = "bufferIndex";

    /// <summary>
    /// Represents the sel_setBufferIndex field.
    /// </summary>
    private static readonly Selector sel_setBufferIndex = "setBufferIndex:";
}