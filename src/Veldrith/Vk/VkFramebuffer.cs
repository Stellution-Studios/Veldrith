using System.Collections.Generic;
using System.Diagnostics;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

internal unsafe class VkFramebuffer : VkFramebufferBase {
    private readonly List<VkImageView> _attachmentViews = new();
    private readonly Vulkan.VkFramebuffer _deviceFramebuffer;
    private readonly VkRenderPass _renderPassClear;
    private readonly VkRenderPass _renderPassNoClear;
    private readonly VkRenderPass _renderPassNoClearLoad;

    private readonly VkGraphicsDevice gd;
    private bool _destroyed;
    private string _name;

    public VkFramebuffer(VkGraphicsDevice gd, ref FramebufferDescription description, bool isPresented)
        : base(description.DepthTarget, description.ColorTargets) {
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

        VkResult creationResult =
            vkCreateRenderPass(this.gd.Device, ref renderPassCi, null, out this._renderPassNoClear);
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
            imageViewCi.subresourceRange = new VkImageSubresourceRange(
                VkImageAspectFlags.Color,
                description.ColorTargets[i].MipLevel,
                1,
                description.ColorTargets[i].ArrayLayer);
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
            depthViewCi.subresourceRange = new VkImageSubresourceRange(
                hasStencil ? VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil : VkImageAspectFlags.Depth,
                description.DepthTarget.Value.MipLevel,
                1,
                description.DepthTarget.Value.ArrayLayer);
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

        Util.GetMipDimensions(
            dimTex,
            mipLevel,
            out uint mipWidth,
            out uint mipHeight,
            out _);

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

    public override Vulkan.VkFramebuffer CurrentFramebuffer => this._deviceFramebuffer;
    public override VkRenderPass RenderPassNoClearInit => this._renderPassNoClear;
    public override VkRenderPass RenderPassNoClearLoad => this._renderPassNoClearLoad;
    public override VkRenderPass RenderPassClear => this._renderPassClear;

    public override uint RenderableWidth => this.Width;
    public override uint RenderableHeight => this.Height;

    public override uint AttachmentCount { get; }

    public override bool IsDisposed => this._destroyed;

    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    public override void TransitionToIntermediateLayout(VkCommandBuffer cb) {
        for (int i = 0; i < this.ColorTargets.Count; i++) {
            FramebufferAttachment ca = this.ColorTargets[i];
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
            vkTex.SetImageLayout(ca.MipLevel, ca.ArrayLayer, VkImageLayout.ColorAttachmentOptimal);
        }

        if (this.DepthTarget != null) {
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(this.DepthTarget.Value.Target);
            vkTex.SetImageLayout(this.DepthTarget.Value.MipLevel, this.DepthTarget.Value.ArrayLayer,
                VkImageLayout.DepthStencilAttachmentOptimal);
        }
    }

    public override void TransitionToFinalLayout(VkCommandBuffer cb) {
        for (int i = 0; i < this.ColorTargets.Count; i++) {
            FramebufferAttachment ca = this.ColorTargets[i];
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);

            if ((vkTex.Usage & TextureUsage.Sampled) != 0) {
                vkTex.TransitionImageLayout(
                    cb,
                    ca.MipLevel, 1,
                    ca.ArrayLayer, 1,
                    VkImageLayout.ShaderReadOnlyOptimal);
            }
        }

        if (this.DepthTarget != null) {
            VkTexture vkTex = Util.AssertSubtype<Texture, VkTexture>(this.DepthTarget.Value.Target);

            if ((vkTex.Usage & TextureUsage.Sampled) != 0) {
                vkTex.TransitionImageLayout(
                    cb, this.DepthTarget.Value.MipLevel, 1, this.DepthTarget.Value.ArrayLayer, 1,
                    VkImageLayout.ShaderReadOnlyOptimal);
            }
        }
    }

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