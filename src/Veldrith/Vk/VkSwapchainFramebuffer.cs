using System;
using System.Collections.Generic;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk
{
    internal unsafe class VkSwapchainFramebuffer : VkFramebufferBase
    {
        public override Vulkan.VkFramebuffer CurrentFramebuffer => scFramebuffers[(int)ImageIndex].CurrentFramebuffer;

        public override VkRenderPass RenderPassNoClearInit => scFramebuffers[0].RenderPassNoClearInit;
        public override VkRenderPass RenderPassNoClearLoad => scFramebuffers[0].RenderPassNoClearLoad;
        public override VkRenderPass RenderPassClear => scFramebuffers[0].RenderPassClear;

        public override IReadOnlyList<FramebufferAttachment> ColorTargets => scColorTextures[(int)ImageIndex];

        public override FramebufferAttachment? DepthTarget => depthAttachment;

        public override uint RenderableWidth => scExtent.width;
        public override uint RenderableHeight => scExtent.height;

        public override uint Width => desiredWidth;
        public override uint Height => desiredHeight;

        public uint ImageIndex { get; private set; }

        public override OutputDescription OutputDescription => outputDescription;

        public override uint AttachmentCount { get; }

        public VkSwapchain Swapchain { get; }

        public override bool IsDisposed => destroyed;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                gd.SetResourceName(this, value);
            }
        }

        private readonly VkGraphicsDevice gd;
        private readonly PixelFormat? depthFormat;

        private VkFramebuffer[] scFramebuffers;
        private VkImage[] scImages = { };
        private VkFormat scImageFormat;
        private VkExtent2D scExtent;
        private FramebufferAttachment[][] scColorTextures;

        private FramebufferAttachment? depthAttachment;
        private uint desiredWidth;
        private uint desiredHeight;
        private bool destroyed;
        private string name;
        private OutputDescription outputDescription;

        public VkSwapchainFramebuffer(
            VkGraphicsDevice gd,
            VkSwapchain swapchain,
            VkSurfaceKHR surface,
            uint width,
            uint height,
            PixelFormat? depthFormat)
        {
            this.gd = gd;
            Swapchain = swapchain;
            this.depthFormat = depthFormat;

            AttachmentCount = depthFormat.HasValue ? 2u : 1u; // 1 Color + 1 Depth
        }

        public override void TransitionToIntermediateLayout(VkCommandBuffer cb)
        {
            for (int i = 0; i < ColorTargets.Count; i++)
            {
                var ca = ColorTargets[i];
                var vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
                vkTex.SetImageLayout(0, ca.ArrayLayer, VkImageLayout.ColorAttachmentOptimal);
            }
        }

        public override void TransitionToFinalLayout(VkCommandBuffer cb)
        {
            for (int i = 0; i < ColorTargets.Count; i++)
            {
                var ca = ColorTargets[i];
                var vkTex = Util.AssertSubtype<Texture, VkTexture>(ca.Target);
                vkTex.TransitionImageLayout(cb, 0, 1, ca.ArrayLayer, 1, VkImageLayout.PresentSrcKHR);
            }
        }

        internal void SetImageIndex(uint index)
        {
            ImageIndex = index;
        }

        internal void SetNewSwapchain(
            VkSwapchainKHR deviceSwapchain,
            uint width,
            uint height,
            VkSurfaceFormatKHR surfaceFormat,
            VkExtent2D swapchainExtent)
        {
            desiredWidth = width;
            desiredHeight = height;

            // Get the images
            uint scImageCount = 0;
            var result = vkGetSwapchainImagesKHR(gd.Device, deviceSwapchain, ref scImageCount, null);
            CheckResult(result);
            if (scImages.Length < scImageCount) scImages = new VkImage[(int)scImageCount];
            result = vkGetSwapchainImagesKHR(gd.Device, deviceSwapchain, ref scImageCount, out scImages[0]);
            CheckResult(result);

            scImageFormat = surfaceFormat.format;
            scExtent = swapchainExtent;

            createDepthTexture();
            createFramebuffers();

            outputDescription = OutputDescription.CreateFromFramebuffer(this);
        }

        protected override void DisposeCore()
        {
            if (!destroyed)
            {
                destroyed = true;
                depthAttachment?.Target.Dispose();
                destroySwapchainFramebuffers();
            }
        }

        private void destroySwapchainFramebuffers()
        {
            if (scFramebuffers != null)
            {
                for (int i = 0; i < scFramebuffers.Length; i++)
                {
                    scFramebuffers[i]?.Dispose();
                    scFramebuffers[i] = null;
                }

                Array.Clear(scFramebuffers, 0, scFramebuffers.Length);
            }
        }

        private void createDepthTexture()
        {
            if (depthFormat.HasValue)
            {
                depthAttachment?.Target.Dispose();
                var depthTexture = (VkTexture)gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                    Math.Max(1, scExtent.width),
                    Math.Max(1, scExtent.height),
                    1,
                    1,
                    depthFormat.Value,
                    TextureUsage.DepthStencil));
                depthAttachment = new FramebufferAttachment(depthTexture, 0);
            }
        }

        private void createFramebuffers()
        {
            if (scFramebuffers != null)
            {
                for (int i = 0; i < scFramebuffers.Length; i++)
                {
                    scFramebuffers[i]?.Dispose();
                    scFramebuffers[i] = null;
                }

                Array.Clear(scFramebuffers, 0, scFramebuffers.Length);
            }

            Util.EnsureArrayMinimumSize(ref scFramebuffers, (uint)scImages.Length);
            Util.EnsureArrayMinimumSize(ref scColorTextures, (uint)scImages.Length);

            for (uint i = 0; i < scImages.Length; i++)
            {
                var colorTex = new VkTexture(
                    gd,
                    Math.Max(1, scExtent.width),
                    Math.Max(1, scExtent.height),
                    1,
                    1,
                    scImageFormat,
                    TextureUsage.RenderTarget,
                    TextureSampleCount.Count1,
                    scImages[i]);
                var desc = new FramebufferDescription(depthAttachment?.Target, colorTex);
                var fb = new VkFramebuffer(gd, ref desc, true);
                scFramebuffers[i] = fb;
                scColorTextures[i] = new[] { new FramebufferAttachment(colorTex, 0) };
            }
        }
    }
}
