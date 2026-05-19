using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]
public struct MTLRenderPassDescriptor {
    private static readonly ObjCClass s_class = new(nameof(MTLRenderPassDescriptor));
    public readonly IntPtr NativePtr;

    public static MTLRenderPassDescriptor New() {
        return s_class.AllocInit<MTLRenderPassDescriptor>();
    }

    public MTLRenderPassColorAttachmentDescriptorArray colorAttachments
        => objc_msgSend<MTLRenderPassColorAttachmentDescriptorArray>(this.NativePtr, sel_colorAttachments);

    public MTLRenderPassDepthAttachmentDescriptor depthAttachment
        => objc_msgSend<MTLRenderPassDepthAttachmentDescriptor>(this.NativePtr, sel_depthAttachment);

    public MTLRenderPassStencilAttachmentDescriptor stencilAttachment
        => objc_msgSend<MTLRenderPassStencilAttachmentDescriptor>(this.NativePtr, sel_stencilAttachment);

    private static readonly Selector sel_colorAttachments = "colorAttachments";
    private static readonly Selector sel_depthAttachment = "depthAttachment";
    private static readonly Selector sel_stencilAttachment = "stencilAttachment";
}