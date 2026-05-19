using System;
using System.Collections.Generic;
using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Defines the behavior and responsibilities of the VkSwapchainFramebuffer class.
/// </summary>
internal unsafe class VkSwapchainFramebuffer : VkFramebufferBase {

    /// <summary>
    /// Stores the value associated with <c>depthFormat</c>.
    /// </summary>
    private readonly PixelFormat? depthFormat;

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_depthAttachment</c>.
    /// </summary>
    private FramebufferAttachment? _depthAttachment;

    /// <summary>
    /// Stores the value associated with <c>_desiredHeight</c>.
    /// </summary>
    private uint _desiredHeight;

    /// <summary>
    /// Stores the value associated with <c>_desiredWidth</c>.
    /// </summary>
    private uint _desiredWidth;

    /// <summary>
    /// Stores the value associated with <c>_destroyed</c>.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the value associated with <c>_name</c>.
    /// </summary>
    private string _name;

    /// <summary>
    /// Stores the value associated with <c>_outputDescription</c>.
    /// </summary>
    private OutputDescription _outputDescription;

    /// <summary>
    /// Stores the value associated with <c>_scColorTextures</c>.
    /// </summary>
    private FramebufferAttachment[][] _scColorTextures;

    /// <summary>
    /// Stores the value associated with <c>_scExtent</c>.
    /// </summary>
    private VkExtent2D _scExtent;

    /// <summary>
    /// Stores the value associated with <c>_scFramebuffers</c>.
    /// </summary>
    private VkFramebuffer[] _scFramebuffers;

    /// <summary>
    /// Stores the value associated with <c>_scImageFormat</c>.
    /// </summary>
    private VkFormat _scImageFormat;

    /// <summary>
    /// Stores the value associated with <c>_scImages</c>.
    /// </summary>
    private VkImage[] _scImages = { };

    /// <summary>
    /// Initializes a new instance of the <see cref="VkSwapchainFramebuffer" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="swapchain">Specifies the value of <paramref name="swapchain" />.</param>
    /// <param name="surface">Specifies the value of <paramref name="surface" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depthFormat">Specifies the value of <paramref name="depthFormat" />.</param>
    public VkSwapchainFramebuffer(VkGraphicsDevice gd, VkSwapchain swapchain, VkSurfaceKHR surface, uint width, uint height, PixelFormat? depthFormat) {
        this.gd = gd;
        this.Swapchain = swapchain;
        this.depthFormat = depthFormat;

        this.AttachmentCount = depthFormat.HasValue ? 2u : 1u; // 1 Color + 1 Depth
    }

    /// <summary>
    /// Stores the value associated with <c>CurrentFramebuffer</c>.
    /// </summary>
    public override Vulkan.VkFramebuffer CurrentFramebuffer =>
        this._scFramebuffers[(int)this.ImageIndex].CurrentFramebuffer;

    /// <summary>
    /// Gets or sets RenderPassNoClearInit.
    /// </summary>
    public override VkRenderPass RenderPassNoClearInit => this._scFramebuffers[0].RenderPassNoClearInit;

    /// <summary>
    /// Gets or sets RenderPassNoClearLoad.
    /// </summary>
    public override VkRenderPass RenderPassNoClearLoad => this._scFramebuffers[0].RenderPassNoClearLoad;

    /// <summary>
    /// Gets or sets RenderPassClear.
    /// </summary>
    public override VkRenderPass RenderPassClear => this._scFramebuffers[0].RenderPassClear;

    /// <summary>
    /// Gets or sets ColorTargets.
    /// </summary>
    public override IReadOnlyList<FramebufferAttachment> ColorTargets => this._scColorTextures[(int)this.ImageIndex];

    /// <summary>
    /// Gets or sets DepthTarget.
    /// </summary>
    public override FramebufferAttachment? DepthTarget => this._depthAttachment;

    /// <summary>
    /// Gets or sets RenderableWidth.
    /// </summary>
    public override uint RenderableWidth => this._scExtent.width;

    /// <summary>
    /// Gets or sets RenderableHeight.
    /// </summary>
    public override uint RenderableHeight => this._scExtent.height;

    /// <summary>
    /// Gets or sets Width.
    /// </summary>
    public override uint Width => this._desiredWidth;

    /// <summary>
    /// Gets or sets Height.
    /// </summary>
    public override uint Height => this._desiredHeight;

    /// <summary>
    /// Gets or sets ImageIndex.
    /// </summary>
    public uint ImageIndex { get; private set; }

    /// <summary>
    /// Gets or sets OutputDescription.
    /// </summary>
    public override OutputDescription OutputDescription => this._outputDescription;

    /// <summary>
    /// Gets or sets AttachmentCount.
    /// </summary>
    public override uint AttachmentCount { get; }

    /// <summary>
    /// Gets or sets Swapchain.
    /// </summary>
    public VkSwapchain Swapchain { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._destroyed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    /// <summary>
    /// Executes the TransitionToIntermediateLayout operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    public override void TransitionToIntermediateLayout(VkCommandBuffer cb) {
        for (int i = 0; i < this.ColorTargets.Count; i++) {
            FramebufferAttachment ca = this.ColorTargets[i];
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
            vkTex.SetImageLayout(0, ca.ArrayLayer, VkImageLayout.ColorAttachmentOptimal);
        }
    }

    /// <summary>
    /// Executes the TransitionToFinalLayout operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    public override void TransitionToFinalLayout(VkCommandBuffer cb) {
        for (int i = 0; i < this.ColorTargets.Count; i++) {
            FramebufferAttachment ca = this.ColorTargets[i];
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
            vkTex.TransitionImageLayout(cb, 0, 1, ca.ArrayLayer, 1, VkImageLayout.PresentSrcKHR);
        }
    }

    /// <summary>
    /// Executes the SetImageIndex operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    internal void SetImageIndex(uint index) {
        this.ImageIndex = index;
    }

    /// <summary>
    /// Executes the SetNewSwapchain operation.
    /// </summary>
    /// <param name="deviceSwapchain">Specifies the value of <paramref name="deviceSwapchain" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="surfaceFormat">Specifies the value of <paramref name="surfaceFormat" />.</param>
    /// <param name="swapchainExtent">Specifies the value of <paramref name="swapchainExtent" />.</param>
    internal void SetNewSwapchain(VkSwapchainKHR deviceSwapchain, uint width, uint height, VkSurfaceFormatKHR surfaceFormat, VkExtent2D swapchainExtent) {
        this._desiredWidth = width;
        this._desiredHeight = height;

        // Get the images
        uint scImageCount = 0;
        VkResult result = vkGetSwapchainImagesKHR(this.gd.Device, deviceSwapchain, ref scImageCount, null);
        CheckResult(result);
        if (this._scImages.Length < scImageCount) {
            this._scImages = new VkImage[(int)scImageCount];
        }

        result = vkGetSwapchainImagesKHR(this.gd.Device, deviceSwapchain, ref scImageCount, out this._scImages[0]);
        CheckResult(result);

        this._scImageFormat = surfaceFormat.format;
        this._scExtent = swapchainExtent;

        this.CreateDepthTexture();
        this.CreateFramebuffers();

        this._outputDescription = OutputDescription.CreateFromFramebuffer(this);
    }

    /// <summary>
    /// Executes the DisposeCore operation.
    /// </summary>
    protected override void DisposeCore() {
        if (!this._destroyed) {
            this._destroyed = true;
            this._depthAttachment?.Target.Dispose();
            this.DestroySwapchainFramebuffers();
        }
    }

    /// <summary>
    /// Executes the DestroySwapchainFramebuffers operation.
    /// </summary>
    private void DestroySwapchainFramebuffers() {
        if (this._scFramebuffers != null) {
            for (int i = 0; i < this._scFramebuffers.Length; i++) {
                this._scFramebuffers[i]?.Dispose();
                this._scFramebuffers[i] = null;
            }

            Array.Clear(this._scFramebuffers, 0, this._scFramebuffers.Length);
        }
    }

    /// <summary>
    /// Executes the CreateDepthTexture operation.
    /// </summary>
    private void CreateDepthTexture() {
        if (this.depthFormat.HasValue) {
            this._depthAttachment?.Target.Dispose();
            VkTexture depthTexture = (VkTexture)this.gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(Math.Max(1, this._scExtent.width), Math.Max(1, this._scExtent.height), 1, 1, this.depthFormat.Value, TextureUsage.DepthStencil));
            this._depthAttachment = new FramebufferAttachment(depthTexture, 0);
        }
    }

    /// <summary>
    /// Executes the CreateFramebuffers operation.
    /// </summary>
    private void CreateFramebuffers() {
        if (this._scFramebuffers != null) {
            for (int i = 0; i < this._scFramebuffers.Length; i++) {
                this._scFramebuffers[i]?.Dispose();
                this._scFramebuffers[i] = null;
            }

            Array.Clear(this._scFramebuffers, 0, this._scFramebuffers.Length);
        }

        Util.EnsureArrayMinimumSize(ref this._scFramebuffers, (uint)this._scImages.Length);
        Util.EnsureArrayMinimumSize(ref this._scColorTextures, (uint)this._scImages.Length);

        for (uint i = 0; i < this._scImages.Length; i++) {
            VkTexture colorTex = new(this.gd, Math.Max(1, this._scExtent.width), Math.Max(1, this._scExtent.height), 1, 1, this._scImageFormat, TextureUsage.RenderTarget, TextureSampleCount.Count1, this._scImages[i]);
            FramebufferDescription desc = new(this._depthAttachment?.Target, colorTex);
            VkFramebuffer fb = new(this.gd, ref desc, true);
            this._scFramebuffers[i] = fb;
            this._scColorTextures[i] = new[] { new FramebufferAttachment(colorTex, 0) };
        }
    }
}