using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLRenderPassDescriptor struct.
/// </summary>
public struct MTLRenderPassDescriptor {

    /// <summary>
    /// Represents the s_class field.
    /// </summary>
    private static readonly ObjCClass s_class = new(nameof(MTLRenderPassDescriptor));

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes New.
    /// </summary>
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
    /// Represents the sel_colorAttachments field.
    /// </summary>
    private static readonly Selector sel_colorAttachments = "colorAttachments";

    /// <summary>
    /// Represents the sel_depthAttachment field.
    /// </summary>
    private static readonly Selector sel_depthAttachment = "depthAttachment";

    /// <summary>
    /// Represents the sel_stencilAttachment field.
    /// </summary>
    private static readonly Selector sel_stencilAttachment = "stencilAttachment";
}