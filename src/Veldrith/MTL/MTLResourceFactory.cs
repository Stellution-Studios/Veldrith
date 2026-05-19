namespace Veldrith.MTL;

/// <summary>
/// Represents the MtlResourceFactory class.
/// </summary>
internal class MtlResourceFactory : ResourceFactory {

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlResourceFactory" /> class.
    /// </summary>
    public MtlResourceFactory(MtlGraphicsDevice gd)

        /// <summary>
        /// Executes base.
        /// </summary>
        : base(gd.Features) {
        this.gd = gd;
    }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Metal;

    /// <summary>
    /// Executes CreateCommandList.
    /// </summary>
    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new MtlCommandList(ref description, this.gd);
    }

    /// <summary>
    /// Executes CreateComputePipeline.
    /// </summary>
    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new MtlPipeline(ref description, this.gd);
    }

    /// <summary>
    /// Executes CreateFramebuffer.
    /// </summary>
    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new MtlFramebuffer(this.gd, ref description);
    }

    /// <summary>
    /// Executes CreateResourceLayout.
    /// </summary>
    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new MtlResourceLayout(ref description, this.gd);
    }

    /// <summary>
    /// Executes CreateResourceSet.
    /// </summary>
    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        ValidationHelpers.ValidateResourceSet(this.gd, ref description);
        return new MtlResourceSet(ref description, this.gd);
    }

    /// <summary>
    /// Executes CreateFence.
    /// </summary>
    public override Fence CreateFence(bool signaled) {
        return new MtlFence(signaled);
    }

    /// <summary>
    /// Executes CreateSwapchain.
    /// </summary>
    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new MtlSwapchain(this.gd, ref description);
    }

    /// <summary>
    /// Executes CreateGraphicsPipelineCore.
    /// </summary>
    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new MtlPipeline(ref description, this.gd);
    }

    /// <summary>
    /// Executes CreateSamplerCore.
    /// </summary>
    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new MtlSampler(ref description, this.gd);
    }

    /// <summary>
    /// Executes CreateShaderCore.
    /// </summary>
    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new MtlShader(ref description, this.gd);
    }

    /// <summary>
    /// Executes CreateBufferCore.
    /// </summary>
    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new MtlBuffer(ref description, this.gd);
    }

    /// <summary>
    /// Executes CreateTextureCore.
    /// </summary>
    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new MtlTexture(ref description, this.gd);
    }

    /// <summary>
    /// Executes CreateTextureCore.
    /// </summary>
    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new MtlTexture(nativeTexture, ref description);
    }

    /// <summary>
    /// Executes CreateTextureViewCore.
    /// </summary>
    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new MtlTextureView(ref description, this.gd);
    }
}