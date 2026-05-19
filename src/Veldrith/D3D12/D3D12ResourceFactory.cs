namespace Veldrith.D3D12;

internal sealed class D3D12ResourceFactory : ResourceFactory {
    private readonly D3D12GraphicsDevice gd;

    public D3D12ResourceFactory(D3D12GraphicsDevice gd, GraphicsDeviceFeatures features)
        : base(features) {
        this.gd = gd;
    }

    public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new D3D12Pipeline(this.gd, ref description);
    }

    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new D3D12Framebuffer(this.gd, ref description);
    }

    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new D3D12CommandList(this.gd, ref description, this.gd.Features, this.gd.UniformBufferMinOffsetAlignment,
            this.gd.StructuredBufferMinOffsetAlignment);
    }

    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new D3D12ResourceLayout(ref description);
    }

    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        return new D3D12ResourceSet(ref description);
    }

    public override Fence CreateFence(bool signaled) {
        return new D3D12Fence(this.gd, signaled);
    }

    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new D3D12Swapchain(this.gd, ref description);
    }

    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new D3D12Pipeline(this.gd, ref description);
    }

    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new D3D12Texture(this.gd, ref description, nativeTexture);
    }

    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new D3D12Texture(this.gd, ref description, null);
    }

    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new D3D12TextureView(this.gd, ref description);
    }

    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new D3D12DeviceBuffer(this.gd, ref description);
    }

    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new D3D12Sampler(this.gd, ref description);
    }

    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new D3D12Shader(ref description);
    }
}