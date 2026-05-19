using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLVertexAttributeDescriptor struct.
/// </summary>
public struct MTLVertexAttributeDescriptor {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLVertexAttributeDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
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
    /// Stores the value associated with <c>sel_format</c>.
    /// </summary>
    private static readonly Selector sel_format = "format";

    /// <summary>
    /// Stores the value associated with <c>sel_setFormat</c>.
    /// </summary>
    private static readonly Selector sel_setFormat = "setFormat:";

    /// <summary>
    /// Stores the value associated with <c>sel_offset</c>.
    /// </summary>
    private static readonly Selector sel_offset = "offset";

    /// <summary>
    /// Stores the value associated with <c>sel_setOffset</c>.
    /// </summary>
    private static readonly Selector sel_setOffset = "setOffset:";

    /// <summary>
    /// Stores the value associated with <c>sel_bufferIndex</c>.
    /// </summary>
    private static readonly Selector sel_bufferIndex = "bufferIndex";

    /// <summary>
    /// Stores the value associated with <c>sel_setBufferIndex</c>.
    /// </summary>
    private static readonly Selector sel_setBufferIndex = "setBufferIndex:";
}