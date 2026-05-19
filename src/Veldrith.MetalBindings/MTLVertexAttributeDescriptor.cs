using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLVertexAttributeDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLVertexAttributeDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLVertexAttributeDescriptor" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
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
    /// Stores the sel format state used by this instance.
    /// </summary>
    private static readonly Selector sel_format = "format";

    /// <summary>
    /// Stores the sel set format state used by this instance.
    /// </summary>
    private static readonly Selector sel_setFormat = "setFormat:";

    /// <summary>
    /// Stores the sel offset value used during command execution.
    /// </summary>
    private static readonly Selector sel_offset = "offset";

    /// <summary>
    /// Stores the sel set offset value used during command execution.
    /// </summary>
    private static readonly Selector sel_setOffset = "setOffset:";

    /// <summary>
    /// Stores the sel buffer index value used during command execution.
    /// </summary>
    private static readonly Selector sel_bufferIndex = "bufferIndex";

    /// <summary>
    /// Stores the sel set buffer index value used during command execution.
    /// </summary>
    private static readonly Selector sel_setBufferIndex = "setBufferIndex:";
}