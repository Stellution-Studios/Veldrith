using System.Collections.Generic;
using System.Diagnostics;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal class MtlSwapchainFramebuffer : MtlFramebuffer {
    private readonly FramebufferAttachment[] _colorTargets;
    private readonly MtlSwapchainTexture _colorTexture = new();
    private readonly MtlSwapchain _parentSwapchain;
    private readonly PixelFormat colorFormat;

    private readonly PixelFormat? depthFormat;
    private readonly MtlGraphicsDevice gd;
    private FramebufferAttachment? _depthTarget;
    private MtlTexture _depthTexture;

    public MtlSwapchainFramebuffer(
        MtlGraphicsDevice gd,
        MtlSwapchain parent,
        PixelFormat? depthFormat,
        PixelFormat colorFormat) {
        this.gd = gd;
        this._parentSwapchain = parent;
        this.colorFormat = colorFormat;

        OutputAttachmentDescription? depthAttachment = null;

        if (depthFormat != null) {
            this.depthFormat = depthFormat;
            depthAttachment = new OutputAttachmentDescription(depthFormat.Value);
        }

        OutputAttachmentDescription colorAttachment = new(colorFormat);

        this._colorTargets = new[] { new FramebufferAttachment(this._colorTexture, 0) };

        this.OutputDescription = new OutputDescription(depthAttachment, colorAttachment);
    }

    public override uint Width => this._colorTexture.Width;
    public override uint Height => this._colorTexture.Height;

    public override OutputDescription OutputDescription { get; }

    public override IReadOnlyList<FramebufferAttachment> ColorTargets => this._colorTargets;
    public override FramebufferAttachment? DepthTarget => this._depthTarget;

    #region Disposal

    public override void Dispose() {
        this._depthTexture?.Dispose();
        base.Dispose();
    }

    #endregion

    public void UpdateTextures(CAMetalDrawable drawable, CGSize size) {
        this._colorTexture.SetDrawable(drawable, size, this.colorFormat);

        if (this.depthFormat.HasValue &&
            (size.width != this._depthTexture?.Width || size.height != this._depthTexture?.Height)) {
            this.RecreateDepthTexture((uint)size.width, (uint)size.height);
        }
    }

    public bool EnsureDrawableAvailable() {
        return this._parentSwapchain.EnsureDrawableAvailable();
    }

    private void RecreateDepthTexture(uint width, uint height) {
        Debug.Assert(this.depthFormat.HasValue);
        this._depthTexture?.Dispose();

        this._depthTexture = Util.AssertSubtype<Texture, MtlTexture>(this.gd.ResourceFactory.CreateTexture(
            TextureDescription.Texture2D(
                width, height, 1, 1, this.depthFormat.Value, TextureUsage.DepthStencil)));
        this._depthTarget = new FramebufferAttachment(this._depthTexture, 0);
    }
}