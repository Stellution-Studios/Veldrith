using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlFramebuffer.
/// </summary>
internal class MtlFramebuffer : Framebuffer {

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlFramebuffer" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public MtlFramebuffer(MtlGraphicsDevice gd, ref FramebufferDescription description) : base(description.DepthTarget, description.ColorTargets) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlFramebuffer" /> type.
    /// </summary>
    public MtlFramebuffer() { }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }

    #endregion

    /// <summary>
    /// Creates the render pass descriptor instance used by this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
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
            depthDescriptor.loadAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless ? MTLLoadAction.DontCare : MTLLoadAction.Load;
            depthDescriptor.storeAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless ? MTLStoreAction.DontCare : MTLStoreAction.Store;
            depthDescriptor.texture = mtlDepthTarget.DeviceTexture;
            depthDescriptor.slice = this.DepthTarget.Value.ArrayLayer;
            depthDescriptor.level = this.DepthTarget.Value.MipLevel;

            if (FormatHelpers.IsStencilFormat(mtlDepthTarget.Format)) {
                MTLRenderPassStencilAttachmentDescriptor stencilDescriptor = ret.stencilAttachment;
                stencilDescriptor.loadAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless ? MTLLoadAction.DontCare : MTLLoadAction.Load;
                stencilDescriptor.storeAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless ? MTLStoreAction.DontCare : MTLStoreAction.Store;
                stencilDescriptor.texture = mtlDepthTarget.DeviceTexture;
                stencilDescriptor.slice = this.DepthTarget.Value.ArrayLayer;
            }
        }

        return ret;
    }
}