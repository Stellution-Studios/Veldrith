using System.Collections.Generic;
using System.Diagnostics;
using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal class MtlSwapchainFramebuffer : MtlFramebuffer
    {
        public override uint Width => this._colorTexture.Width;
        public override uint Height => this._colorTexture.Height;

        public override OutputDescription OutputDescription { get; }

        public override IReadOnlyList<FramebufferAttachment> ColorTargets => this._colorTargets;
        public override FramebufferAttachment? DepthTarget => this._depthTarget;
        private readonly MtlGraphicsDevice gd;
        private readonly MtlSwapchain _parentSwapchain;
        private readonly PixelFormat colorFormat;

        private readonly PixelFormat? depthFormat;
        private readonly MtlSwapchainTexture _colorTexture = new MtlSwapchainTexture();
        private MtlTexture _depthTexture;

        private readonly FramebufferAttachment[] _colorTargets;
        private FramebufferAttachment? _depthTarget;

        public MtlSwapchainFramebuffer(
            MtlGraphicsDevice gd,
            MtlSwapchain parent,
            PixelFormat? depthFormat,
            PixelFormat colorFormat)
        {
            this.gd = gd;
            this._parentSwapchain = parent;
            this.colorFormat = colorFormat;

            OutputAttachmentDescription? depthAttachment = null;

            if (depthFormat != null)
            {
                this.depthFormat = depthFormat;
                depthAttachment = new OutputAttachmentDescription(depthFormat.Value);
            }

            var colorAttachment = new OutputAttachmentDescription(colorFormat);

            this._colorTargets = new[] { new FramebufferAttachment(this._colorTexture, 0) };

            OutputDescription = new OutputDescription(depthAttachment, colorAttachment);
        }

        #region Disposal

        public override void Dispose()
        {
            this._depthTexture?.Dispose();
            base.Dispose();
        }

        #endregion

        public void UpdateTextures(CAMetalDrawable drawable, CGSize size)
        {
            this._colorTexture.SetDrawable(drawable, size, colorFormat);

            if (depthFormat.HasValue && (size.width != this._depthTexture?.Width || size.height != this._depthTexture?.Height))
                RecreateDepthTexture((uint)size.width, (uint)size.height);
        }

        public bool EnsureDrawableAvailable()
        {
            return this._parentSwapchain.EnsureDrawableAvailable();
        }

        private void RecreateDepthTexture(uint width, uint height)
        {
            Debug.Assert(depthFormat.HasValue);
            this._depthTexture?.Dispose();

            this._depthTexture = Util.AssertSubtype<Texture, MtlTexture>(
                gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                    width, height, 1, 1, depthFormat.Value, TextureUsage.DepthStencil)));
            this._depthTarget = new FramebufferAttachment(this._depthTexture, 0);
        }
    }
}
