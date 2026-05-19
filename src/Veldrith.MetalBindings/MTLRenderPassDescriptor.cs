using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Defines the data layout and behavior of the MTLRenderPassDescriptor struct.
/// </summary>
public struct MTLRenderPassDescriptor {

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="MTLRenderPassDescriptor">Specifies the value of <paramref name="MTLRenderPassDescriptor" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(MTLRenderPassDescriptor));

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>Returns the result produced by the New operation.</returns>
    public static MTLRenderPassDescriptor New() {
        return s_class.AllocInit<MTLRenderPassDescriptor>();
    }

    public MTLRenderPassColorAttachmentDescriptorArray colorAttachments
        => objc_msgSend<MTLRenderPassColorAttachmentDescriptorArray>(this.NativePtr, sel_colorAttachments);

    public MTLRenderPassDepthAttachmentDescriptor depthAttachment
        => objc_msgSend<MTLRenderPassDepthAttachmentDescriptor>(this.NativePtr, sel_depthAttachment);

    public MTLRenderPassStencilAttachmentDescriptor stencilAttachment
        => objc_msgSend<MTLRenderPassStencilAttachmentDescriptor>(this.NativePtr, sel_stencilAttachment);

    /// <summary>
    /// Stores the value associated with <c>sel_colorAttachments</c>.
    /// </summary>
    private static readonly Selector sel_colorAttachments = "colorAttachments";

    /// <summary>
    /// Stores the value associated with <c>sel_depthAttachment</c>.
    /// </summary>
    private static readonly Selector sel_depthAttachment = "depthAttachment";

    /// <summary>
    /// Stores the value associated with <c>sel_stencilAttachment</c>.
    /// </summary>
    private static readonly Selector sel_stencilAttachment = "stencilAttachment";
}
