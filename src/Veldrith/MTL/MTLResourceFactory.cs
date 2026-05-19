namespace Veldrith.MTL
{
    internal class MtlResourceFactory : ResourceFactory
    {
        public override GraphicsBackend BackendType => GraphicsBackend.Metal;
        private readonly MtlGraphicsDevice gd;

        public MtlResourceFactory(MtlGraphicsDevice gd)
            : base(gd.Features)
        {
            this.gd = gd;
        }

        public override CommandList CreateCommandList(ref CommandListDescription description)
        {
            return new MtlCommandList(ref description, gd);
        }

        public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description)
        {
            return new MtlPipeline(ref description, gd);
        }

        public override Framebuffer CreateFramebuffer(ref FramebufferDescription description)
        {
            return new MtlFramebuffer(gd, ref description);
        }

        public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description)
        {
            return new MtlResourceLayout(ref description, gd);
        }

        public override ResourceSet CreateResourceSet(ref ResourceSetDescription description)
        {
            ValidationHelpers.ValidateResourceSet(gd, ref description);
            return new MtlResourceSet(ref description, gd);
        }

        public override Fence CreateFence(bool signaled)
        {
            return new MtlFence(signaled);
        }

        public override Swapchain CreateSwapchain(ref SwapchainDescription description)
        {
            return new MtlSwapchain(gd, ref description);
        }

        protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description)
        {
            return new MtlPipeline(ref description, gd);
        }

        protected override Sampler CreateSamplerCore(ref SamplerDescription description)
        {
            return new MtlSampler(ref description, gd);
        }

        protected override Shader CreateShaderCore(ref ShaderDescription description)
        {
            return new MtlShader(ref description, gd);
        }

        protected override DeviceBuffer CreateBufferCore(ref BufferDescription description)
        {
            return new MtlBuffer(ref description, gd);
        }

        protected override Texture CreateTextureCore(ref TextureDescription description)
        {
            return new MtlTexture(ref description, gd);
        }

        protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description)
        {
            return new MtlTexture(nativeTexture, ref description);
        }

        protected override TextureView CreateTextureViewCore(ref TextureViewDescription description)
        {
            return new MtlTextureView(ref description, gd);
        }
    }
}
