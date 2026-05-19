using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

public struct MTLVertexDescriptor {
    public readonly IntPtr NativePtr;

    public MTLVertexBufferLayoutDescriptorArray layouts
        => objc_msgSend<MTLVertexBufferLayoutDescriptorArray>(this.NativePtr, sel_layouts);

    public MTLVertexAttributeDescriptorArray attributes
        => objc_msgSend<MTLVertexAttributeDescriptorArray>(this.NativePtr, sel_attributes);

    private static readonly Selector sel_layouts = "layouts";
    private static readonly Selector sel_attributes = "attributes";
}