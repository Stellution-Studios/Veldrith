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
    public MTLVertexFormat Format {
        get => (MTLVertexFormat)uint_objc_msgSend(this.NativePtr, _selFormat);
        set => objc_msgSend(this.NativePtr, _selSetFormat, (uint)value);
    }

    /// <summary>
    /// Gets or sets offset.
    /// </summary>
    public UIntPtr Offset {
        get => UIntPtr_objc_msgSend(this.NativePtr, _selOffset);
        set => objc_msgSend(this.NativePtr, _selSetOffset, value);
    }

    /// <summary>
    /// Gets or sets bufferIndex.
    /// </summary>
    public UIntPtr BufferIndex {
        get => UIntPtr_objc_msgSend(this.NativePtr, _selBufferIndex);
        set => objc_msgSend(this.NativePtr, _selSetBufferIndex, value);
    }

    /// <summary>
    /// Stores the sel format state used by this instance.
    /// </summary>
    private static readonly Selector _selFormat = "format";

    /// <summary>
    /// Stores the sel set format state used by this instance.
    /// </summary>
    private static readonly Selector _selSetFormat = "setFormat:";

    /// <summary>
    /// Stores the sel offset value used during command execution.
    /// </summary>
    private static readonly Selector _selOffset = "offset";

    /// <summary>
    /// Stores the sel set offset value used during command execution.
    /// </summary>
    private static readonly Selector _selSetOffset = "setOffset:";

    /// <summary>
    /// Stores the sel buffer index value used during command execution.
    /// </summary>
    private static readonly Selector _selBufferIndex = "bufferIndex";

    /// <summary>
    /// Stores the sel set buffer index value used during command execution.
    /// </summary>
    private static readonly Selector _selSetBufferIndex = "setBufferIndex:";
}