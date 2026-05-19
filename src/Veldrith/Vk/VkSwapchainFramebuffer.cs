using System;
using System.Collections.Generic;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk
{
    internal unsafe class VkSwapchainFramebuffer : VkFramebufferBase
    {
        public override Vulkan.VkFramebuffer CurrentFramebuffer => this._scFramebuffers[(int)ImageIndex].CurrentFramebuffer;

        public override VkRenderPass RenderPassNoClearInit => this._scFramebuffers[0].RenderPassNoClearInit;
        public override VkRenderPass RenderPassNoClearLoad => this._scFramebuffers[0].RenderPassNoClearLoad;
        public override VkRenderPass RenderPassClear => this._scFramebuffers[0].RenderPassClear;

        public override IReadOnlyList<FramebufferAttachment> ColorTargets => this._scColorTextures[(int)ImageIndex];

        public override FramebufferAttachment? DepthTarget => this._depthAttachment;

        public override uint RenderableWidth => this._scExtent.width;
        public override uint RenderableHeight => this._scExtent.height;

        public override uint Width => this._desiredWidth;
        public override uint Height => this._desiredHeight;

        public uint ImageIndex { get; private set; }

        public override OutputDescription OutputDescription => this._outputDescription;

        public override uint AttachmentCount { get; }

        public VkSwapchain Swapchain { get; }

        public override bool IsDisposed => this._destroyed;

        public override string Name
        {
            get => this._name;
            set
            {
                this._name = value;
                gd.SetResourceName(this, value);
            }
        }

        private readonly VkGraphicsDevice gd;
        private readonly PixelFormat? depthFormat;

        private VkFramebuffer[] _scFramebuffers;
        private VkImage[] _scImages = { };
        private VkFormat _scImageFormat;
        private VkExtent2D _scExtent;
        private FramebufferAttachment[][] _scColorTextures;

        private FramebufferAttachment? _depthAttachment;
        private uint _desiredWidth;
        private uint _desiredHeight;
        private bool _destroyed;
        private string _name;
        private OutputDescription _outputDescription;

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
            this._desiredWidth = width;
            this._desiredHeight = height;

            // Get the images
            uint scImageCount = 0;
            var result = vkGetSwapchainImagesKHR(gd.Device, deviceSwapchain, ref scImageCount, null);
            CheckResult(result);
            if (this._scImages.Length < scImageCount) this._scImages = new VkImage[(int)scImageCount];
            result = vkGetSwapchainImagesKHR(gd.Device, deviceSwapchain, ref scImageCount, out this._scImages[0]);
            CheckResult(result);

            this._scImageFormat = surfaceFormat.format;
            this._scExtent = swapchainExtent;

            CreateDepthTexture();
            CreateFramebuffers();

            this._outputDescription = OutputDescription.CreateFromFramebuffer(this);
        }

        protected override void DisposeCore()
        {
            if (!this._destroyed)
            {
                this._destroyed = true;
                this._depthAttachment?.Target.Dispose();
                DestroySwapchainFramebuffers();
            }
        }

        private void DestroySwapchainFramebuffers()
        {
            if (this._scFramebuffers != null)
            {
                for (int i = 0; i < this._scFramebuffers.Length; i++)
                {
                    this._scFramebuffers[i]?.Dispose();
                    this._scFramebuffers[i] = null;
                }

                Array.Clear(this._scFramebuffers, 0, this._scFramebuffers.Length);
            }
        }

        private void CreateDepthTexture()
        {
            if (depthFormat.HasValue)
            {
                this._depthAttachment?.Target.Dispose();
                var depthTexture = (VkTexture)gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                    Math.Max(1, this._scExtent.width),
                    Math.Max(1, this._scExtent.height),
                    1,
                    1,
                    depthFormat.Value,
                    TextureUsage.DepthStencil));
                this._depthAttachment = new FramebufferAttachment(depthTexture, 0);
            }
        }

        private void CreateFramebuffers()
        {
            if (this._scFramebuffers != null)
            {
                for (int i = 0; i < this._scFramebuffers.Length; i++)
                {
                    this._scFramebuffers[i]?.Dispose();
                    this._scFramebuffers[i] = null;
                }

                Array.Clear(this._scFramebuffers, 0, this._scFramebuffers.Length);
            }

            Util.EnsureArrayMinimumSize(ref this._scFramebuffers, (uint)this._scImages.Length);
            Util.EnsureArrayMinimumSize(ref this._scColorTextures, (uint)this._scImages.Length);

            for (uint i = 0; i < this._scImages.Length; i++)
            {
                var colorTex = new VkTexture(
                    gd,
                    Math.Max(1, this._scExtent.width),
                    Math.Max(1, this._scExtent.height),
                    1,
                    1,
                    this._scImageFormat,
                    TextureUsage.RenderTarget,
                    TextureSampleCount.Count1,
                    this._scImages[i]);
                var desc = new FramebufferDescription(this._depthAttachment?.Target, colorTex);
                var fb = new VkFramebuffer(gd, ref desc, true);
                this._scFramebuffers[i] = fb;
                this._scColorTextures[i] = new[] { new FramebufferAttachment(colorTex, 0) };
            }
        }
    }
}
