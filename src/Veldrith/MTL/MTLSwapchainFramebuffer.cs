using System.Collections.Generic;
using System.Diagnostics;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlSwapchainFramebuffer.
/// </summary>
internal class MtlSwapchainFramebuffer : MtlFramebuffer {

    /// <summary>
    /// Stores the color targets state used by this instance.
    /// </summary>
    private readonly FramebufferAttachment[] _colorTargets;

    /// <summary>
    /// Stores the color texture state used by this instance.
    /// </summary>
    private readonly MtlSwapchainTexture _colorTexture = new();

    /// <summary>
    /// Stores the parent swapchain state used by this instance.
    /// </summary>
    private readonly MtlSwapchain _parentSwapchain;

    /// <summary>
    /// Stores the color format state used by this instance.
    /// </summary>
    private readonly PixelFormat colorFormat;

    /// <summary>
    /// Stores the depth format value used during command execution.
    /// </summary>
    private readonly PixelFormat? depthFormat;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Stores the depth target value used during command execution.
    /// </summary>
    private FramebufferAttachment? _depthTarget;

    /// <summary>
    /// Stores the depth texture value used during command execution.
    /// </summary>
    private MtlTexture _depthTexture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlSwapchainFramebuffer" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="parent">The parent value used by this operation.</param>
    /// <param name="depthFormat">The depth format value used by this operation.</param>
    /// <param name="colorFormat">The color format value used by this operation.</param>
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
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this._depthTexture?.Dispose();
        base.Dispose();
    }

    #endregion

    /// <summary>
    /// Updates the textures state for this command sequence.
    /// </summary>
    /// <param name="drawable">The drawable value used by this operation.</param>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    public void UpdateTextures(CAMetalDrawable drawable, CGSize size) {
        this._colorTexture.SetDrawable(drawable, size, this.colorFormat);

        if (this.depthFormat.HasValue && (size.Width != this._depthTexture?.Width || size.Height != this._depthTexture?.Height)) {
            this.RecreateDepthTexture((uint)size.Width, (uint)size.Height);
        }
    }

    /// <summary>
    /// Executes the ensure drawable available logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool EnsureDrawableAvailable() {
        return this._parentSwapchain.EnsureDrawableAvailable();
    }

    /// <summary>
    /// Executes the recreate depth texture logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    private void RecreateDepthTexture(uint width, uint height) {
        Debug.Assert(this.depthFormat.HasValue);
        this._depthTexture?.Dispose();

        this._depthTexture = Util.AssertSubtype<Texture, MtlTexture>(this.gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(width, height, 1, 1, this.depthFormat.Value, TextureUsage.DepthStencil)));
        this._depthTarget = new FramebufferAttachment(this._depthTexture, 0);
    }
}