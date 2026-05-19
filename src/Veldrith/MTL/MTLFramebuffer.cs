using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal class MtlFramebuffer : Framebuffer {
    private bool _disposed;

    public MtlFramebuffer(MtlGraphicsDevice gd, ref FramebufferDescription description)
        : base(description.DepthTarget, description.ColorTargets) { }

    public MtlFramebuffer() { }

    public override bool IsDisposed => this._disposed;

    public override string Name { get; set; }

    #region Disposal

    public override void Dispose() {
        this._disposed = true;
    }

    #endregion

    public MTLRenderPassDescriptor CreateRenderPassDescriptor() {
        MTLRenderPassDescriptor ret = MTLRenderPassDescriptor.New();

        for (int i = 0; i < this.ColorTargets.Count; i++) {
            FramebufferAttachment colorTarget = this.ColorTargets[i];
            MtlTexture mtlTarget = Util.AssertSubtype<Texture, MtlTexture>(colorTarget.Target);
            MTLRenderPassColorAttachmentDescriptor colorDescriptor = ret.colorAttachments[(uint)i];
            colorDescriptor.texture = mtlTarget.DeviceTexture;
            colorDescriptor.loadAction = MTLLoadAction.Load;
            colorDescriptor.slice = colorTarget.ArrayLayer;
            colorDescriptor.level = colorTarget.MipLevel;
        }

        if (this.DepthTarget != null) {
            MtlTexture mtlDepthTarget = Util.AssertSubtype<Texture, MtlTexture>(this.DepthTarget.Value.Target);
            MTLRenderPassDepthAttachmentDescriptor depthDescriptor = ret.depthAttachment;
            depthDescriptor.loadAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless
                ? MTLLoadAction.DontCare
                : MTLLoadAction.Load;
            depthDescriptor.storeAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless
                ? MTLStoreAction.DontCare
                : MTLStoreAction.Store;
            depthDescriptor.texture = mtlDepthTarget.DeviceTexture;
            depthDescriptor.slice = this.DepthTarget.Value.ArrayLayer;
            depthDescriptor.level = this.DepthTarget.Value.MipLevel;

            if (FormatHelpers.IsStencilFormat(mtlDepthTarget.Format)) {
                MTLRenderPassStencilAttachmentDescriptor stencilDescriptor = ret.stencilAttachment;
                stencilDescriptor.loadAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless
                    ? MTLLoadAction.DontCare
                    : MTLLoadAction.Load;
                stencilDescriptor.storeAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless
                    ? MTLStoreAction.DontCare
                    : MTLStoreAction.Store;
                stencilDescriptor.texture = mtlDepthTarget.DeviceTexture;
                stencilDescriptor.slice = this.DepthTarget.Value.ArrayLayer;
            }
        }

        return ret;
    }
}