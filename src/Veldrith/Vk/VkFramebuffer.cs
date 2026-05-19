using System.Collections.Generic;
using System.Diagnostics;
using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkFramebuffer.
/// </summary>
internal unsafe class VkFramebuffer : VkFramebufferBase {

    /// <summary>
    /// Stores the attachment views state used by this instance.
    /// </summary>
    private readonly List<VkImageView> _attachmentViews = new();

    /// <summary>
    /// Stores the device framebuffer state used by this instance.
    /// </summary>
    private readonly Vulkan.VkFramebuffer _deviceFramebuffer;

    /// <summary>
    /// Stores the render pass clear state used by this instance.
    /// </summary>
    private readonly VkRenderPass _renderPassClear;

    /// <summary>
    /// Stores the render pass no clear state used by this instance.
    /// </summary>
    private readonly VkRenderPass _renderPassNoClear;

    /// <summary>
    /// Stores the render pass no clear load state used by this instance.
    /// </summary>
    private readonly VkRenderPass _renderPassNoClearLoad;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the destroyed state used by this instance.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkFramebuffer" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="isPresented">The is presented value used by this operation.</param>
    public VkFramebuffer(VkGraphicsDevice gd, ref FramebufferDescription description, bool isPresented) : base(description.DepthTarget, description.ColorTargets) {
        this.gd = gd;

        VkRenderPassCreateInfo renderPassCi = VkRenderPassCreateInfo.New();

        StackList<VkAttachmentDescription> attachments = new();

        uint colorAttachmentCount = (uint)this.ColorTargets.Count;
        StackList<VkAttachmentReference> colorAttachmentRefs = new();

        for (int i = 0; i < colorAttachmentCount; i++) {
            VkTexture vkColorTex = Util.AssertSubtype<Texture, VkTexture>(this.ColorTargets[i].Target);
            VkAttachmentDescription colorAttachmentDesc = new() {
                format = vkColorTex.VkFormat,
                samples = vkColorTex.VkSampleCount,
                loadOp = VkAttachmentLoadOp.Load,
                storeOp = VkAttachmentStoreOp.Store,
                stencilLoadOp = VkAttachmentLoadOp.DontCare,
                stencilStoreOp = VkAttachmentStoreOp.DontCare,
                initialLayout = isPresented
                    ? VkImageLayout.PresentSrcKHR
                    : (vkColorTex.Usage & TextureUsage.Sampled) != 0
                        ? VkImageLayout.ShaderReadOnlyOptimal
                        : VkImageLayout.ColorAttachmentOptimal,
                finalLayout = VkImageLayout.ColorAttachmentOptimal
            };
            attachments.Add(colorAttachmentDesc);

            VkAttachmentReference colorAttachmentRef = new() {
                attachment = (uint)i,
                layout = VkImageLayout.ColorAttachmentOptimal
            };
            colorAttachmentRefs.Add(colorAttachmentRef);
        }

        VkAttachmentDescription depthAttachmentDesc = new();
        VkAttachmentReference depthAttachmentRef = new();

        if (this.DepthTarget != null) {
            VkTexture vkDepthTex = Util.AssertSubtype<Texture, VkTexture>(this.DepthTarget.Value.Target);
            bool hasStencil = FormatHelpers.IsStencilFormat(vkDepthTex.Format);
            depthAttachmentDesc.format = vkDepthTex.VkFormat;
            depthAttachmentDesc.samples = vkDepthTex.VkSampleCount;
            depthAttachmentDesc.loadOp = VkAttachmentLoadOp.Load;
            depthAttachmentDesc.storeOp = VkAttachmentStoreOp.Store;
            depthAttachmentDesc.stencilLoadOp = VkAttachmentLoadOp.DontCare;
            depthAttachmentDesc.stencilStoreOp = hasStencil
                ? VkAttachmentStoreOp.Store
                : VkAttachmentStoreOp.DontCare;
            depthAttachmentDesc.initialLayout = (vkDepthTex.Usage & TextureUsage.Sampled) != 0
                ? VkImageLayout.ShaderReadOnlyOptimal
                : VkImageLayout.DepthStencilAttachmentOptimal;
            depthAttachmentDesc.finalLayout = VkImageLayout.DepthStencilAttachmentOptimal;

            depthAttachmentRef.attachment = (uint)description.ColorTargets.Length;
            depthAttachmentRef.layout = VkImageLayout.DepthStencilAttachmentOptimal;
        }

        VkSubpassDescription subpass = new() {
            pipelineBindPoint = VkPipelineBindPoint.Graphics
        };

        if (this.ColorTargets.Count > 0) {
            subpass.colorAttachmentCount = colorAttachmentCount;
            subpass.pColorAttachments = (VkAttachmentReference*)colorAttachmentRefs.Data;
        }

        if (this.DepthTarget != null) {
            subpass.pDepthStencilAttachment = &depthAttachmentRef;
            attachments.Add(depthAttachmentDesc);
        }

        VkSubpassDependency subpassDependency = new() {
            srcSubpass = SubpassExternal,
            srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
            dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
            dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite
        };

        renderPassCi.attachmentCount = attachments.Count;
        renderPassCi.pAttachments = (VkAttachmentDescription*)attachments.Data;
        renderPassCi.subpassCount = 1;
        renderPassCi.pSubpasses = &subpass;
        renderPassCi.dependencyCount = 1;
        renderPassCi.pDependencies = &subpassDependency;

        VkResult creationResult = vkCreateRenderPass(this.gd.Device, ref renderPassCi, null, out this._renderPassNoClear);
        CheckResult(creationResult);

        for (int i = 0; i < colorAttachmentCount; i++) {
            attachments[i].loadOp = VkAttachmentLoadOp.Load;
            attachments[i].initialLayout = VkImageLayout.ColorAttachmentOptimal;
        }

        if (this.DepthTarget != null) {
            attachments[attachments.Count - 1].loadOp = VkAttachmentLoadOp.Load;
            attachments[attachments.Count - 1].initialLayout = VkImageLayout.DepthStencilAttachmentOptimal;
            bool hasStencil = FormatHelpers.IsStencilFormat(this.DepthTarget.Value.Target.Format);
            if (hasStencil) {
                attachments[attachments.Count - 1].stencilLoadOp = VkAttachmentLoadOp.Load;
            }
        }

        creationResult = vkCreateRenderPass(this.gd.Device, ref renderPassCi, null, out this._renderPassNoClearLoad);
        CheckResult(creationResult);

        // Load version

        if (this.DepthTarget != null) {
            attachments[attachments.Count - 1].loadOp = VkAttachmentLoadOp.Clear;
            attachments[attachments.Count - 1].initialLayout = VkImageLayout.Undefined;
            bool hasStencil = FormatHelpers.IsStencilFormat(this.DepthTarget.Value.Target.Format);
            if (hasStencil) {
                attachments[attachments.Count - 1].stencilLoadOp = VkAttachmentLoadOp.Clear;
            }
        }

        for (int i = 0; i < colorAttachmentCount; i++) {
            attachments[i].loadOp = VkAttachmentLoadOp.Clear;
            attachments[i].initialLayout = VkImageLayout.Undefined;
        }

        creationResult = vkCreateRenderPass(this.gd.Device, ref renderPassCi, null, out this._renderPassClear);
        CheckResult(creationResult);

        VkFramebufferCreateInfo fbCi = VkFramebufferCreateInfo.New();
        uint fbAttachmentsCount = (uint)description.ColorTargets.Length;
        if (description.DepthTarget != null) {
            fbAttachmentsCount += 1;
        }

        VkImageView* fbAttachments = stackalloc VkImageView[(int)fbAttachmentsCount];

        for (int i = 0; i < colorAttachmentCount; i++) {
            VkTexture vkColorTarget = Util.AssertSubtype<Texture, VkTexture>(description.ColorTargets[i].Target);
            VkImageViewCreateInfo imageViewCi = VkImageViewCreateInfo.New();
            imageViewCi.image = vkColorTarget.OptimalDeviceImage;
            imageViewCi.format = vkColorTarget.VkFormat;
            imageViewCi.viewType = VkImageViewType.Image2D;
            imageViewCi.subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, description.ColorTargets[i].MipLevel, 1, description.ColorTargets[i].ArrayLayer);
            VkImageView* dest = fbAttachments + i;
            VkResult result = vkCreateImageView(this.gd.Device, ref imageViewCi, null, dest);
            CheckResult(result);
            this._attachmentViews.Add(*dest);
        }

        // Depth
        if (description.DepthTarget != null) {
            VkTexture vkDepthTarget = Util.AssertSubtype<Texture, VkTexture>(description.DepthTarget.Value.Target);
            bool hasStencil = FormatHelpers.IsStencilFormat(vkDepthTarget.Format);
            VkImageViewCreateInfo depthViewCi = VkImageViewCreateInfo.New();
            depthViewCi.image = vkDepthTarget.OptimalDeviceImage;
            depthViewCi.format = vkDepthTarget.VkFormat;
            depthViewCi.viewType = description.DepthTarget.Value.Target.ArrayLayers == 1
                ? VkImageViewType.Image2D
                : VkImageViewType.Image2DArray;
            depthViewCi.subresourceRange = new VkImageSubresourceRange(hasStencil ? VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil : VkImageAspectFlags.Depth, description.DepthTarget.Value.MipLevel, 1, description.DepthTarget.Value.ArrayLayer);
            VkImageView* dest = fbAttachments + (fbAttachmentsCount - 1);
            VkResult result = vkCreateImageView(this.gd.Device, ref depthViewCi, null, dest);
            CheckResult(result);
            this._attachmentViews.Add(*dest);
        }

        Texture dimTex;
        uint mipLevel;

        if (this.ColorTargets.Count > 0) {
            dimTex = this.ColorTargets[0].Target;
            mipLevel = this.ColorTargets[0].MipLevel;
        }
        else {
            Debug.Assert(this.DepthTarget != null);
            dimTex = this.DepthTarget.Value.Target;
            mipLevel = this.DepthTarget.Value.MipLevel;
        }

        Util.GetMipDimensions(dimTex, mipLevel, out uint mipWidth, out uint mipHeight, out _);

        fbCi.width = mipWidth;
        fbCi.height = mipHeight;

        fbCi.attachmentCount = fbAttachmentsCount;
        fbCi.pAttachments = fbAttachments;
        fbCi.layers = 1;
        fbCi.renderPass = this._renderPassNoClear;

        creationResult = vkCreateFramebuffer(this.gd.Device, ref fbCi, null, out this._deviceFramebuffer);
        CheckResult(creationResult);

        if (this.DepthTarget != null) {
            this.AttachmentCount += 1;
        }

        this.AttachmentCount += (uint)this.ColorTargets.Count;
    }

    /// <summary>
    /// Gets or sets CurrentFramebuffer.
    /// </summary>
    public override Vulkan.VkFramebuffer CurrentFramebuffer => this._deviceFramebuffer;

    /// <summary>
    /// Gets or sets RenderPassNoClearInit.
    /// </summary>
    public override VkRenderPass RenderPassNoClearInit => this._renderPassNoClear;

    /// <summary>
    /// Gets or sets RenderPassNoClearLoad.
    /// </summary>
    public override VkRenderPass RenderPassNoClearLoad => this._renderPassNoClearLoad;

    /// <summary>
    /// Gets or sets RenderPassClear.
    /// </summary>
    public override VkRenderPass RenderPassClear => this._renderPassClear;

    /// <summary>
    /// Gets or sets RenderableWidth.
    /// </summary>
    public override uint RenderableWidth => this.Width;

    /// <summary>
    /// Gets or sets RenderableHeight.
    /// </summary>
    public override uint RenderableHeight => this.Height;

    /// <summary>
    /// Gets or sets AttachmentCount.
    /// </summary>
    public override uint AttachmentCount { get; }

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
    /// Executes the transition to intermediate layout logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    public override void TransitionToIntermediateLayout(VkCommandBuffer cb) {
        for (int i = 0; i < this.ColorTargets.Count; i++) {
            FramebufferAttachment ca = this.ColorTargets[i];
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
            vkTex.SetImageLayout(ca.MipLevel, ca.ArrayLayer, VkImageLayout.ColorAttachmentOptimal);
        }

        if (this.DepthTarget != null) {
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(this.DepthTarget.Value.Target);
            vkTex.SetImageLayout(this.DepthTarget.Value.MipLevel, this.DepthTarget.Value.ArrayLayer, VkImageLayout.DepthStencilAttachmentOptimal);
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

            if ((vkTex.Usage & TextureUsage.Sampled) != 0) {
                vkTex.TransitionImageLayout(cb, ca.MipLevel, 1, ca.ArrayLayer, 1, VkImageLayout.ShaderReadOnlyOptimal);
            }
        }

        if (this.DepthTarget != null) {
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(this.DepthTarget.Value.Target);

            if ((vkTex.Usage & TextureUsage.Sampled) != 0) {
                vkTex.TransitionImageLayout(cb, this.DepthTarget.Value.MipLevel, 1, this.DepthTarget.Value.ArrayLayer, 1, VkImageLayout.ShaderReadOnlyOptimal);
            }
        }
    }

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    protected override void DisposeCore() {
        if (!this._destroyed) {
            vkDestroyFramebuffer(this.gd.Device, this._deviceFramebuffer, null);
            vkDestroyRenderPass(this.gd.Device, this._renderPassNoClear, null);
            vkDestroyRenderPass(this.gd.Device, this._renderPassNoClearLoad, null);
            vkDestroyRenderPass(this.gd.Device, this._renderPassClear, null);
            foreach (VkImageView view in this._attachmentViews) {
                vkDestroyImageView(this.gd.Device, view, null);
            }

            this._destroyed = true;
        }
    }
}