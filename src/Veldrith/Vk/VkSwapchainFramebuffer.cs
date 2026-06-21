using System;
using System.Collections.Generic;
using Vortice.Vulkan;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkSwapchainFramebuffer.
/// </summary>
internal unsafe class VkSwapchainFramebuffer : VkFramebufferBase {

    /// <summary>
    /// Stores the depth format value used during command execution.
    /// </summary>
    private readonly PixelFormat? _depthFormat;

    /// <summary>
    /// Stores the graphics device used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

    /// <summary>
    /// Stores the depth attachment value used during command execution.
    /// </summary>
    private FramebufferAttachment? _depthAttachment;

    /// <summary>
    /// Stores the desired height value used during command execution.
    /// </summary>
    private uint _desiredHeight;

    /// <summary>
    /// Stores the desired width value used during command execution.
    /// </summary>
    private uint _desiredWidth;

    /// <summary>
    /// Stores the destroyed state used by this instance.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Stores the output description state used by this instance.
    /// </summary>
    private OutputDescription _outputDescription;

    /// <summary>
    /// Stores the sc color textures collection used by this instance.
    /// </summary>
    private FramebufferAttachment[][] _scColorTextures;

    /// <summary>
    /// Stores the sc extent state used by this instance.
    /// </summary>
    private VkExtent2D _scExtent;

    /// <summary>
    /// Stores the sc framebuffers collection used by this instance.
    /// </summary>
    private VkFramebuffer[] _scFramebuffers;

    /// <summary>
    /// Stores the sc image format state used by this instance.
    /// </summary>
    private VkFormat _scImageFormat;

    /// <summary>
    /// Stores the sc images state used by this instance.
    /// </summary>
    private VkImage[] _scImages = { };

    /// <summary>
    /// Initializes a new instance of the <see cref="VkSwapchainFramebuffer" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="swapchain">The swapchain used by this operation.</param>
    /// <param name="surface">The surface value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depthFormat">The depth format value used by this operation.</param>
    public VkSwapchainFramebuffer(VkGraphicsDevice gd, VkSwapchain swapchain, VkSurfaceKHR surface, uint width, uint height, PixelFormat? depthFormat) {
        this._gd = gd;
        this.Swapchain = swapchain;
        this._depthFormat = depthFormat;

        this.AttachmentCount = depthFormat.HasValue ? 2u : 1u; // 1 Color + 1 Depth
    }

    /// <summary>
    /// Stores the current framebuffer state used by this instance.
    /// </summary>
    public override global::Vortice.Vulkan.VkFramebuffer CurrentFramebuffer =>
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
            this._gd.SetResourceName(this, value);
        }
    }

    /// <summary>
    /// Executes the transition to intermediate layout logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    public override void TransitionToIntermediateLayout(VkCommandBuffer cb) {
        for (int i = 0; i < this.ColorTargets.Count; i++) {
            FramebufferAttachment ca = this.ColorTargets[i];
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
            vkTex.SetImageLayout(0, ca.ArrayLayer, VkImageLayout.ColorAttachmentOptimal);
        }
    }

    /// <summary>
    /// Executes the transition to final layout logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    public override void TransitionToFinalLayout(VkCommandBuffer cb) {
        for (int i = 0; i < this.ColorTargets.Count; i++) {
            FramebufferAttachment ca = this.ColorTargets[i];
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
            vkTex.TransitionImageLayout(cb, 0, 1, ca.ArrayLayer, 1, VkImageLayout.PresentSrcKHR);
        }
    }

    /// <summary>
    /// Sets the image index value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    internal void SetImageIndex(uint index) {
        this.ImageIndex = index;
    }

    /// <summary>
    /// Sets the new swapchain value.
    /// </summary>
    /// <param name="deviceSwapchain">The device swapchain value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="surfaceFormat">The surface format value used by this operation.</param>
    /// <param name="swapchainExtent">The swapchain extent value used by this operation.</param>
    internal void SetNewSwapchain(VkSwapchainKHR deviceSwapchain, uint width, uint height, VkSurfaceFormatKHR surfaceFormat, VkExtent2D swapchainExtent) {
        this._desiredWidth = width;
        this._desiredHeight = height;

        // Get the images
        uint scImageCount = 0;
        VkResult result = this._gd.DeviceApi.vkGetSwapchainImagesKHR(deviceSwapchain, &scImageCount, null);
        CheckResult(result);
        if (this._scImages.Length < scImageCount) {
            this._scImages = new VkImage[(int)scImageCount];
        }

        fixed (VkImage* scImagesPtr = this._scImages) {
            result = this._gd.DeviceApi.vkGetSwapchainImagesKHR(deviceSwapchain, &scImageCount, scImagesPtr);
            CheckResult(result);
        }

        this._scImageFormat = surfaceFormat.format;
        this._scExtent = swapchainExtent;

        this.CreateDepthTexture();
        this.CreateFramebuffers();

        this._outputDescription = OutputDescription.CreateFromFramebuffer(this);
    }

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    protected override void DisposeCore() {
        if (!this._destroyed) {
            this._destroyed = true;
            this._depthAttachment?.Target.Dispose();
            this.DestroySwapchainFramebuffers();
        }
    }

    /// <summary>
    /// Executes the destroy swapchain framebuffers logic for this backend.
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
    /// Creates the depth texture instance used by this backend.
    /// </summary>
    private void CreateDepthTexture() {
        if (this._depthFormat.HasValue) {
            this._depthAttachment?.Target.Dispose();
            VkTexture depthTexture = (VkTexture)this._gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(Math.Max(1, this._scExtent.width), Math.Max(1, this._scExtent.height), 1, 1, this._depthFormat.Value, TextureUsage.DepthStencil));
            this._depthAttachment = new FramebufferAttachment(depthTexture, 0);
        }
    }

    /// <summary>
    /// Creates the framebuffers instance used by this backend.
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
            VkTexture colorTex = new(this._gd, Math.Max(1, this._scExtent.width), Math.Max(1, this._scExtent.height), 1, 1, this._scImageFormat, TextureUsage.RenderTarget, TextureSampleCount.Count1, this._scImages[i]);
            FramebufferDescription desc = new(this._depthAttachment?.Target, colorTex);
            VkFramebuffer fb = new(this._gd, ref desc, true);
            this._scFramebuffers[i] = fb;
            this._scColorTextures[i] = new[] { new FramebufferAttachment(colorTex, 0) };
        }
    }
}
