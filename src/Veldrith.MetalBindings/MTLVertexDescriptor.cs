using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLVertexDescriptor struct.
/// </summary>
public struct MTLVertexDescriptor {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    public MTLVertexBufferLayoutDescriptorArray layouts
        => objc_msgSend<MTLVertexBufferLayoutDescriptorArray>(this.NativePtr, sel_layouts);

    public MTLVertexAttributeDescriptorArray attributes
        => objc_msgSend<MTLVertexAttributeDescriptorArray>(this.NativePtr, sel_attributes);

    /// <summary>
    /// Stores the value associated with <c>sel_layouts</c>.
    /// </summary>
    private static readonly Selector sel_layouts = "layouts";

    /// <summary>
    /// Stores the value associated with <c>sel_attributes</c>.
    /// </summary>
    private static readonly Selector sel_attributes = "attributes";
}