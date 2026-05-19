using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLVertexDescriptor struct.
/// </summary>
public struct MTLVertexDescriptor {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    public MTLVertexBufferLayoutDescriptorArray layouts
        => objc_msgSend<MTLVertexBufferLayoutDescriptorArray>(this.NativePtr, sel_layouts);

    public MTLVertexAttributeDescriptorArray attributes
        => objc_msgSend<MTLVertexAttributeDescriptorArray>(this.NativePtr, sel_attributes);

    /// <summary>
    /// Represents the sel_layouts field.
    /// </summary>
    private static readonly Selector sel_layouts = "layouts";

    /// <summary>
    /// Represents the sel_attributes field.
    /// </summary>
    private static readonly Selector sel_attributes = "attributes";
}