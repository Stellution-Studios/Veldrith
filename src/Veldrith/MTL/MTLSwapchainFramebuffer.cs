using System.Collections.Generic;
using System.Diagnostics;
using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal class MtlSwapchainFramebuffer : MtlFramebuffer
    {
        public override uint Width => _colorTexture.Width;
        public override uint Height => _colorTexture.Height;

        public override OutputDescription OutputDescription { get; }

        public override IReadOnlyList<FramebufferAttachment> ColorTargets => _colorTargets;
        public override FramebufferAttachment? DepthTarget => _depthTarget;
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
            _parentSwapchain = parent;
            this.colorFormat = colorFormat;

            OutputAttachmentDescription? depthAttachment = null;

            if (depthFormat != null)
            {
                this.depthFormat = depthFormat;
                depthAttachment = new OutputAttachmentDescription(depthFormat.Value);
            }

            var colorAttachment = new OutputAttachmentDescription(colorFormat);

            _colorTargets = new[] { new FramebufferAttachment(_colorTexture, 0) };

            OutputDescription = new OutputDescription(depthAttachment, colorAttachment);
        }

        #region Disposal

        public override void Dispose()
        {
            _depthTexture?.Dispose();
            base.Dispose();
        }

        #endregion

        public void UpdateTextures(CAMetalDrawable drawable, CGSize size)
        {
            _colorTexture.SetDrawable(drawable, size, colorFormat);

            if (depthFormat.HasValue && (size.width != _depthTexture?.Width || size.height != _depthTexture?.Height))
                recreateDepthTexture((uint)size.width, (uint)size.height);
        }

        public bool EnsureDrawableAvailable()
        {
            return _parentSwapchain.EnsureDrawableAvailable();
        }

        private void recreateDepthTexture(uint width, uint height)
        {
            Debug.Assert(depthFormat.HasValue);
            _depthTexture?.Dispose();

            _depthTexture = Util.AssertSubtype<Texture, MtlTexture>(
                gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                    width, height, 1, 1, depthFormat.Value, TextureUsage.DepthStencil)));
            _depthTarget = new FramebufferAttachment(_depthTexture, 0);
        }
    }
}
