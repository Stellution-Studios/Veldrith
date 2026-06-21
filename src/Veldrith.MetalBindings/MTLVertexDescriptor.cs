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

    public MTLVertexBufferLayoutDescriptorArray Layouts => ObjcMsgSend<MTLVertexBufferLayoutDescriptorArray>(this.NativePtr, _selLayouts);

    public MTLVertexAttributeDescriptorArray Attributes => ObjcMsgSend<MTLVertexAttributeDescriptorArray>(this.NativePtr, _selAttributes);

    /// <summary>
    /// Stores the sel layouts state used by this instance.
    /// </summary>
    private static readonly Selector _selLayouts = "layouts";

    /// <summary>
    /// Stores the sel attributes state used by this instance.
    /// </summary>
    private static readonly Selector _selAttributes = "attributes";
}
