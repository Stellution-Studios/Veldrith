namespace Veldrith.MTL;

internal class MtlResourceFactory : ResourceFactory {
    private readonly MtlGraphicsDevice gd;

    public MtlResourceFactory(MtlGraphicsDevice gd)
        : base(gd.Features) {
        this.gd = gd;
    }

    public override GraphicsBackend BackendType => GraphicsBackend.Metal;

    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new MtlCommandList(ref description, this.gd);
    }

    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new MtlPipeline(ref description, this.gd);
    }

    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new MtlFramebuffer(this.gd, ref description);
    }

    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new MtlResourceLayout(ref description, this.gd);
    }

    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        ValidationHelpers.ValidateResourceSet(this.gd, ref description);
        return new MtlResourceSet(ref description, this.gd);
    }

    public override Fence CreateFence(bool signaled) {
        return new MtlFence(signaled);
    }

    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new MtlSwapchain(this.gd, ref description);
    }

    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new MtlPipeline(ref description, this.gd);
    }

    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new MtlSampler(ref description, this.gd);
    }

    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new MtlShader(ref description, this.gd);
    }

    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new MtlBuffer(ref description, this.gd);
    }

    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new MtlTexture(ref description, this.gd);
    }

    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new MtlTexture(nativeTexture, ref description);
    }

    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new MtlTextureView(ref description, this.gd);
    }
}