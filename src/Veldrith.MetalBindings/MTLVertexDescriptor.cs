using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLVertexDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLVertexDescriptor {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    public MTLVertexBufferLayoutDescriptorArray layouts
        => objc_msgSend<MTLVertexBufferLayoutDescriptorArray>(this.NativePtr, sel_layouts);

    public MTLVertexAttributeDescriptorArray attributes
        => objc_msgSend<MTLVertexAttributeDescriptorArray>(this.NativePtr, sel_attributes);

    /// <summary>
    /// Stores the sel layouts state used by this instance.
    /// </summary>
    private static readonly Selector sel_layouts = "layouts";

    /// <summary>
    /// Stores the sel attributes state used by this instance.
    /// </summary>
    private static readonly Selector sel_attributes = "attributes";
}