namespace Veldrith.Vk
{
    internal class VkResourceFactory : ResourceFactory
    {
        public override GraphicsBackend BackendType => GraphicsBackend.Vulkan;
        private readonly VkGraphicsDevice gd;

        public VkResourceFactory(VkGraphicsDevice vkGraphicsDevice)
            : base(vkGraphicsDevice.Features)
        {
            gd = vkGraphicsDevice;
        }

        public override CommandList CreateCommandList(ref CommandListDescription description)
        {
            return new VkCommandList(gd, ref description);
        }

        public override Framebuffer CreateFramebuffer(ref FramebufferDescription description)
        {
            return new VkFramebuffer(gd, ref description, false);
        }

        public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description)
        {
            return new VkPipeline(gd, ref description);
        }

        public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description)
        {
            return new VkResourceLayout(gd, ref description);
        }

        public override ResourceSet CreateResourceSet(ref ResourceSetDescription description)
        {
            ValidationHelpers.ValidateResourceSet(gd, ref description);
            return new VkResourceSet(gd, ref description);
        }

        public override Fence CreateFence(bool signaled)
        {
            return new VkFence(gd, signaled);
        }

        public override Swapchain CreateSwapchain(ref SwapchainDescription description)
        {
            return new VkSwapchain(gd, ref description);
        }

        protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description)
        {
            return new VkPipeline(gd, ref description);
        }

        protected override Sampler CreateSamplerCore(ref SamplerDescription description)
        {
            return new VkSampler(gd, ref description);
        }

        protected override Shader CreateShaderCore(ref ShaderDescription description)
        {
            return new VkShader(gd, ref description);
        }

        protected override Texture CreateTextureCore(ref TextureDescription description)
        {
            return new VkTexture(gd, ref description);
        }

        protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description)
        {
            return new VkTexture(
                gd,
                description.Width, description.Height,
                description.MipLevels, description.ArrayLayers,
                VkFormats.VdToVkPixelFormat(description.Format, (description.Usage & TextureUsage.DepthStencil) != 0),
                description.Usage,
                description.SampleCount,
                nativeTexture);
        }

        protected override TextureView CreateTextureViewCore(ref TextureViewDescription description)
        {
            return new VkTextureView(gd, ref description);
        }

        protected override DeviceBuffer CreateBufferCore(ref BufferDescription description)
        {
            return new VkBuffer(gd, description.SizeInBytes, description.Usage);
        }
    }
}
