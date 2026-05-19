using System;
using System.Runtime.InteropServices;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

[StructLayout(LayoutKind.Sequential)]

/// <summary>
/// Represents the MTLRenderPassDescriptor data structure used by the graphics runtime.
/// </summary>
public struct MTLRenderPassDescriptor {

    /// <summary>
    /// Stores the s class state used by this instance.
    /// </summary>
    private static readonly ObjCClass s_class = new(nameof(MTLRenderPassDescriptor));

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
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
    /// Stores the sel color attachments state used by this instance.
    /// </summary>
    private static readonly Selector sel_colorAttachments = "colorAttachments";

    /// <summary>
    /// Stores the sel depth attachment value used during command execution.
    /// </summary>
    private static readonly Selector sel_depthAttachment = "depthAttachment";

    /// <summary>
    /// Stores the sel stencil attachment state used by this instance.
    /// </summary>
    private static readonly Selector sel_stencilAttachment = "stencilAttachment";
}