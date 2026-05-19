using System.Collections.Generic;
using System.Diagnostics;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Defines the behavior and responsibilities of the MtlSwapchainFramebuffer class.
/// </summary>
internal class MtlSwapchainFramebuffer : MtlFramebuffer {

    /// <summary>
    /// Stores the value associated with <c>_colorTargets</c>.
    /// </summary>
    private readonly FramebufferAttachment[] _colorTargets;

    /// <summary>
    /// Stores the value associated with <c>_colorTexture</c>.
    /// </summary>
    private readonly MtlSwapchainTexture _colorTexture = new();

    /// <summary>
    /// Stores the value associated with <c>_parentSwapchain</c>.
    /// </summary>
    private readonly MtlSwapchain _parentSwapchain;

    /// <summary>
    /// Stores the value associated with <c>colorFormat</c>.
    /// </summary>
    private readonly PixelFormat colorFormat;

    /// <summary>
    /// Stores the value associated with <c>depthFormat</c>.
    /// </summary>
    private readonly PixelFormat? depthFormat;

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_depthTarget</c>.
    /// </summary>
    private FramebufferAttachment? _depthTarget;

    /// <summary>
    /// Stores the value associated with <c>_depthTexture</c>.
    /// </summary>
    private MtlTexture _depthTexture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlSwapchainFramebuffer" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="parent">Specifies the value of <paramref name="parent" />.</param>
    /// <param name="depthFormat">Specifies the value of <paramref name="depthFormat" />.</param>
    /// <param name="colorFormat">Specifies the value of <paramref name="colorFormat" />.</param>
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this._depthTexture?.Dispose();
        base.Dispose();
    }

    #endregion

    /// <summary>
    /// Executes the UpdateTextures operation.
    /// </summary>
    /// <param name="drawable">Specifies the value of <paramref name="drawable" />.</param>
    /// <param name="size">Specifies the value of <paramref name="size" />.</param>
    public void UpdateTextures(CAMetalDrawable drawable, CGSize size) {
        this._colorTexture.SetDrawable(drawable, size, this.colorFormat);

        if (this.depthFormat.HasValue && (size.width != this._depthTexture?.Width || size.height != this._depthTexture?.Height)) {
            this.RecreateDepthTexture((uint)size.width, (uint)size.height);
        }
    }

    /// <summary>
    /// Executes the EnsureDrawableAvailable operation.
    /// </summary>
    /// <returns>Returns the result produced by the EnsureDrawableAvailable operation.</returns>
    public bool EnsureDrawableAvailable() {
        return this._parentSwapchain.EnsureDrawableAvailable();
    }

    /// <summary>
    /// Executes the RecreateDepthTexture operation.
    /// </summary>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    private void RecreateDepthTexture(uint width, uint height) {
        Debug.Assert(this.depthFormat.HasValue);
        this._depthTexture?.Dispose();

        this._depthTexture = Util.AssertSubtype<Texture, MtlTexture>(this.gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(width, height, 1, 1, this.depthFormat.Value, TextureUsage.DepthStencil)));
        this._depthTarget = new FramebufferAttachment(this._depthTexture, 0);
    }
}
