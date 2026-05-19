using System.Collections.Generic;
using System.Diagnostics;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk
{
    internal unsafe class VkFramebuffer : VkFramebufferBase
    {
        public override Vulkan.VkFramebuffer CurrentFramebuffer => _deviceFramebuffer;
        public override VkRenderPass RenderPassNoClearInit => _renderPassNoClear;
        public override VkRenderPass RenderPassNoClearLoad => _renderPassNoClearLoad;
        public override VkRenderPass RenderPassClear => _renderPassClear;

        public override uint RenderableWidth => Width;
        public override uint RenderableHeight => Height;

        public override uint AttachmentCount { get; }

        public override bool IsDisposed => _destroyed;

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                gd.SetResourceName(this, value);
            }
        }

        private readonly VkGraphicsDevice gd;
        private readonly Vulkan.VkFramebuffer _deviceFramebuffer;
        private readonly VkRenderPass _renderPassNoClearLoad;
        private readonly VkRenderPass _renderPassNoClear;
        private readonly VkRenderPass _renderPassClear;
        private readonly List<VkImageView> _attachmentViews = new List<VkImageView>();
        private bool _destroyed;
        private string _name;

        public VkFramebuffer(VkGraphicsDevice gd, ref FramebufferDescription description, bool isPresented)
            : base(description.DepthTarget, description.ColorTargets)
        {
            this.gd = gd;

            var renderPassCi = VkRenderPassCreateInfo.New();

            var attachments = new StackList<VkAttachmentDescription>();

            uint colorAttachmentCount = (uint)ColorTargets.Count;
            var colorAttachmentRefs = new StackList<VkAttachmentReference>();

            for (int i = 0; i < colorAttachmentCount; i++)
            {
                var vkColorTex = Util.AssertSubtype<Texture, VkTexture>(ColorTargets[i].Target);
                var colorAttachmentDesc = new VkAttachmentDescription
                {
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

                var colorAttachmentRef = new VkAttachmentReference
                {
                    attachment = (uint)i,
                    layout = VkImageLayout.ColorAttachmentOptimal
                };
                colorAttachmentRefs.Add(colorAttachmentRef);
            }

            var depthAttachmentDesc = new VkAttachmentDescription();
            var depthAttachmentRef = new VkAttachmentReference();

            if (DepthTarget != null)
            {
                var vkDepthTex = Util.AssertSubtype<Texture, VkTexture>(DepthTarget.Value.Target);
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

            var subpass = new VkSubpassDescription
            {
                pipelineBindPoint = VkPipelineBindPoint.Graphics
            };

            if (ColorTargets.Count > 0)
            {
                subpass.colorAttachmentCount = colorAttachmentCount;
                subpass.pColorAttachments = (VkAttachmentReference*)colorAttachmentRefs.Data;
            }

            if (DepthTarget != null)
            {
                subpass.pDepthStencilAttachment = &depthAttachmentRef;
                attachments.Add(depthAttachmentDesc);
            }

            var subpassDependency = new VkSubpassDependency
            {
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

            var creationResult = vkCreateRenderPass(this.gd.Device, ref renderPassCi, null, out _renderPassNoClear);
            CheckResult(creationResult);

            for (int i = 0; i < colorAttachmentCount; i++)
            {
                attachments[i].loadOp = VkAttachmentLoadOp.Load;
                attachments[i].initialLayout = VkImageLayout.ColorAttachmentOptimal;
            }

            if (DepthTarget != null)
            {
                attachments[attachments.Count - 1].loadOp = VkAttachmentLoadOp.Load;
                attachments[attachments.Count - 1].initialLayout = VkImageLayout.DepthStencilAttachmentOptimal;
                bool hasStencil = FormatHelpers.IsStencilFormat(DepthTarget.Value.Target.Format);
                if (hasStencil) attachments[attachments.Count - 1].stencilLoadOp = VkAttachmentLoadOp.Load;
            }

            creationResult = vkCreateRenderPass(this.gd.Device, ref renderPassCi, null, out _renderPassNoClearLoad);
            CheckResult(creationResult);

            // Load version

            if (DepthTarget != null)
            {
                attachments[attachments.Count - 1].loadOp = VkAttachmentLoadOp.Clear;
                attachments[attachments.Count - 1].initialLayout = VkImageLayout.Undefined;
                bool hasStencil = FormatHelpers.IsStencilFormat(DepthTarget.Value.Target.Format);
                if (hasStencil) attachments[attachments.Count - 1].stencilLoadOp = VkAttachmentLoadOp.Clear;
            }

            for (int i = 0; i < colorAttachmentCount; i++)
            {
                attachments[i].loadOp = VkAttachmentLoadOp.Clear;
                attachments[i].initialLayout = VkImageLayout.Undefined;
            }

            creationResult = vkCreateRenderPass(this.gd.Device, ref renderPassCi, null, out _renderPassClear);
            CheckResult(creationResult);

            var fbCi = VkFramebufferCreateInfo.New();
            uint fbAttachmentsCount = (uint)description.ColorTargets.Length;
            if (description.DepthTarget != null) fbAttachmentsCount += 1;

            var fbAttachments = stackalloc VkImageView[(int)fbAttachmentsCount];

            for (int i = 0; i < colorAttachmentCount; i++)
            {
                var vkColorTarget = Util.AssertSubtype<Texture, VkTexture>(description.ColorTargets[i].Target);
                var imageViewCi = VkImageViewCreateInfo.New();
                imageViewCi.image = vkColorTarget.OptimalDeviceImage;
                imageViewCi.format = vkColorTarget.VkFormat;
                imageViewCi.viewType = VkImageViewType.Image2D;
                imageViewCi.subresourceRange = new VkImageSubresourceRange(
                    VkImageAspectFlags.Color,
                    description.ColorTargets[i].MipLevel,
                    1,
                    description.ColorTargets[i].ArrayLayer);
                var dest = fbAttachments + i;
                var result = vkCreateImageView(this.gd.Device, ref imageViewCi, null, dest);
                CheckResult(result);
                _attachmentViews.Add(*dest);
            }

            // Depth
            if (description.DepthTarget != null)
            {
                var vkDepthTarget = Util.AssertSubtype<Texture, VkTexture>(description.DepthTarget.Value.Target);
                bool hasStencil = FormatHelpers.IsStencilFormat(vkDepthTarget.Format);
                var depthViewCi = VkImageViewCreateInfo.New();
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
                var dest = fbAttachments + (fbAttachmentsCount - 1);
                var result = vkCreateImageView(this.gd.Device, ref depthViewCi, null, dest);
                CheckResult(result);
                _attachmentViews.Add(*dest);
            }

            Texture dimTex;
            uint mipLevel;

            if (ColorTargets.Count > 0)
            {
                dimTex = ColorTargets[0].Target;
                mipLevel = ColorTargets[0].MipLevel;
            }
            else
            {
                Debug.Assert(DepthTarget != null);
                dimTex = DepthTarget.Value.Target;
                mipLevel = DepthTarget.Value.MipLevel;
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
            fbCi.renderPass = _renderPassNoClear;

            creationResult = vkCreateFramebuffer(this.gd.Device, ref fbCi, null, out _deviceFramebuffer);
            CheckResult(creationResult);

            if (DepthTarget != null) AttachmentCount += 1;
            AttachmentCount += (uint)ColorTargets.Count;
        }

        public override void TransitionToIntermediateLayout(VkCommandBuffer cb)
        {
            for (int i = 0; i < ColorTargets.Count; i++)
            {
                var ca = ColorTargets[i];
                var vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
                vkTex.SetImageLayout(ca.MipLevel, ca.ArrayLayer, VkImageLayout.ColorAttachmentOptimal);
            }

            if (DepthTarget != null)
            {
                var vkTex = Util.AssertSubtype<Texture, VkTexture>(DepthTarget.Value.Target);
                vkTex.SetImageLayout(
                    DepthTarget.Value.MipLevel,
                    DepthTarget.Value.ArrayLayer,
                    VkImageLayout.DepthStencilAttachmentOptimal);
            }
        }

        public override void TransitionToFinalLayout(VkCommandBuffer cb)
        {
            for (int i = 0; i < ColorTargets.Count; i++)
            {
                var ca = ColorTargets[i];
                var vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);

                if ((vkTex.Usage & TextureUsage.Sampled) != 0)
                {
                    vkTex.TransitionImageLayout(
                        cb,
                        ca.MipLevel, 1,
                        ca.ArrayLayer, 1,
                        VkImageLayout.ShaderReadOnlyOptimal);
                }
            }

            if (DepthTarget != null)
            {
                var vkTex = Util.AssertSubtype<Texture, VkTexture>(DepthTarget.Value.Target);

                if ((vkTex.Usage & TextureUsage.Sampled) != 0)
                {
                    vkTex.TransitionImageLayout(
                        cb,
                        DepthTarget.Value.MipLevel, 1,
                        DepthTarget.Value.ArrayLayer, 1,
                        VkImageLayout.ShaderReadOnlyOptimal);
                }
            }
        }

        protected override void DisposeCore()
        {
            if (!_destroyed)
            {
                vkDestroyFramebuffer(gd.Device, _deviceFramebuffer, null);
                vkDestroyRenderPass(gd.Device, _renderPassNoClear, null);
                vkDestroyRenderPass(gd.Device, _renderPassNoClearLoad, null);
                vkDestroyRenderPass(gd.Device, _renderPassClear, null);
                foreach (var view in _attachmentViews) vkDestroyImageView(gd.Device, view, null);

                _destroyed = true;
            }
        }
    }
}
