using System.Collections.Generic;
using System.Diagnostics;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Represents the MtlSwapchainFramebuffer class.
/// </summary>
internal class MtlSwapchainFramebuffer : MtlFramebuffer {

    /// <summary>
    /// Represents the _colorTargets field.
    /// </summary>
    private readonly FramebufferAttachment[] _colorTargets;

    /// <summary>
    /// Represents the _colorTexture field.
    /// </summary>
    private readonly MtlSwapchainTexture _colorTexture = new();

    /// <summary>
    /// Represents the _parentSwapchain field.
    /// </summary>
    private readonly MtlSwapchain _parentSwapchain;

    /// <summary>
    /// Represents the colorFormat field.
    /// </summary>
    private readonly PixelFormat colorFormat;

    /// <summary>
    /// Represents the depthFormat field.
    /// </summary>
    private readonly PixelFormat? depthFormat;

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Represents the _depthTarget field.
    /// </summary>
    private FramebufferAttachment? _depthTarget;

    /// <summary>
    /// Represents the _depthTexture field.
    /// </summary>
    private MtlTexture _depthTexture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlSwapchainFramebuffer" /> class.
    /// </summary>
    public MtlSwapchainFramebuffer(MtlGraphicsDevice gd, MtlSwapchain parent, PixelFormat? depthFormat, PixelFormat colorFormat) {
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

    /// <summary>
    /// Gets or sets Width.
    /// </summary>
    public override uint Width => this._colorTexture.Width;

    /// <summary>
    /// Gets or sets Height.
    /// </summary>
    public override uint Height => this._colorTexture.Height;

    /// <summary>
    /// Gets or sets OutputDescription.
    /// </summary>
    public override OutputDescription OutputDescription { get; }

    /// <summary>
    /// Gets or sets ColorTargets.
    /// </summary>
    public override IReadOnlyList<FramebufferAttachment> ColorTargets => this._colorTargets;

    /// <summary>
    /// Gets or sets DepthTarget.
    /// </summary>
    public override FramebufferAttachment? DepthTarget => this._depthTarget;

    #region Disposal

    /// <summary>
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        this._depthTexture?.Dispose();
        base.Dispose();
    }

    #endregion

    /// <summary>
    /// Executes UpdateTextures.
    /// </summary>
    public void UpdateTextures(CAMetalDrawable drawable, CGSize size) {
        this._colorTexture.SetDrawable(drawable, size, this.colorFormat);

        if (this.depthFormat.HasValue && (size.width != this._depthTexture?.Width || size.height != this._depthTexture?.Height)) {
            this.RecreateDepthTexture((uint)size.width, (uint)size.height);
        }
    }

    /// <summary>
    /// Executes EnsureDrawableAvailable.
    /// </summary>
    public bool EnsureDrawableAvailable() {
        return this._parentSwapchain.EnsureDrawableAvailable();
    }

    /// <summary>
    /// Executes RecreateDepthTexture.
    /// </summary>
    private void RecreateDepthTexture(uint width, uint height) {
        Debug.Assert(this.depthFormat.HasValue);
        this._depthTexture?.Dispose();

        this._depthTexture = Util.AssertSubtype<Texture, MtlTexture>(this.gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(width, height, 1, 1, this.depthFormat.Value, TextureUsage.DepthStencil)));
        this._depthTarget = new FramebufferAttachment(this._depthTexture, 0);
    }
}