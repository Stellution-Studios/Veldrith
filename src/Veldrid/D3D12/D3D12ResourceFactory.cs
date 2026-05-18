namespace Veldrid.D3D12
{
    internal sealed class D3D12ResourceFactory : ResourceFactory
    {
        private readonly D3D12GraphicsDevice gd;

        public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

        public D3D12ResourceFactory(D3D12GraphicsDevice gd, GraphicsDeviceFeatures features)
            : base(features)
        {
            this.gd = gd;
        }

        public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) => new D3D12Pipeline(gd, ref description);

        public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) => new D3D12Framebuffer(gd, ref description);

        public override CommandList CreateCommandList(ref CommandListDescription description)
            => new D3D12CommandList(gd, ref description, gd.Features, gd.UniformBufferMinOffsetAlignment, gd.StructuredBufferMinOffsetAlignment);

        public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) => new D3D12ResourceLayout(ref description);

        public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) => new D3D12ResourceSet(ref description);

        public override Fence CreateFence(bool signaled) => new D3D12Fence(gd, signaled);

        public override Swapchain CreateSwapchain(ref SwapchainDescription description) => new D3D12Swapchain(gd, ref description);

        protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) => new D3D12Pipeline(gd, ref description);

        protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) => new D3D12Texture(gd, ref description, nativeTexture);

        protected override Texture CreateTextureCore(ref TextureDescription description) => new D3D12Texture(gd, ref description, null);

        protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) => new D3D12TextureView(ref description);

        protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) => new D3D12DeviceBuffer(gd, ref description);

        protected override Sampler CreateSamplerCore(ref SamplerDescription description) => new D3D12Sampler(ref description);

        protected override Shader CreateShaderCore(ref ShaderDescription description) => new D3D12Shader(ref description);
    }
}
