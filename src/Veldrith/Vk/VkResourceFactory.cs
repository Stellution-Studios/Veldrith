namespace Veldrith.Vk
{
    internal class VkResourceFactory : ResourceFactory
    {
        public override GraphicsBackend BackendType => GraphicsBackend.Vulkan;
        private readonly VkGraphicsDevice _gd;

        public VkResourceFactory(VkGraphicsDevice vkGraphicsDevice)
            : base(vkGraphicsDevice.Features)
        {
            this._gd = vkGraphicsDevice;
        }

        public override CommandList CreateCommandList(ref CommandListDescription description)
        {
            return new VkCommandList(this._gd, ref description);
        }

        public override Framebuffer CreateFramebuffer(ref FramebufferDescription description)
        {
            return new VkFramebuffer(this._gd, ref description, false);
        }

        public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description)
        {
            return new VkPipeline(this._gd, ref description);
        }

        public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description)
        {
            return new VkResourceLayout(this._gd, ref description);
        }

        public override ResourceSet CreateResourceSet(ref ResourceSetDescription description)
        {
            ValidationHelpers.ValidateResourceSet(this._gd, ref description);
            return new VkResourceSet(this._gd, ref description);
        }

        public override Fence CreateFence(bool signaled)
        {
            return new VkFence(this._gd, signaled);
        }

        public override Swapchain CreateSwapchain(ref SwapchainDescription description)
        {
            return new VkSwapchain(this._gd, ref description);
        }

        protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description)
        {
            return new VkPipeline(this._gd, ref description);
        }

        protected override Sampler CreateSamplerCore(ref SamplerDescription description)
        {
            return new VkSampler(this._gd, ref description);
        }

        protected override Shader CreateShaderCore(ref ShaderDescription description)
        {
            return new VkShader(this._gd, ref description);
        }

        protected override Texture CreateTextureCore(ref TextureDescription description)
        {
            return new VkTexture(this._gd, ref description);
        }

        protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description)
        {
            return new VkTexture(
                this._gd,
                description.Width, description.Height,
                description.MipLevels, description.ArrayLayers,
                VkFormats.VdToVkPixelFormat(description.Format, (description.Usage & TextureUsage.DepthStencil) != 0),
                description.Usage,
                description.SampleCount,
                nativeTexture);
        }

        protected override TextureView CreateTextureViewCore(ref TextureViewDescription description)
        {
            return new VkTextureView(this._gd, ref description);
        }

        protected override DeviceBuffer CreateBufferCore(ref BufferDescription description)
        {
            return new VkBuffer(this._gd, description.SizeInBytes, description.Usage);
        }
    }
}
