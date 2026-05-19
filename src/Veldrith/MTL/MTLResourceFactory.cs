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
    /// <param name="gd">The the grpahisc device.</param>
    /// <returns>The result of the base operation.</returns>
    public MtlResourceFactory(MtlGraphicsDevice gd) : base(gd.Features) {
        this.gd = gd;
    }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Metal;

    /// <summary>
    /// Performs the CreateCommandList operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateCommandList operation.</returns>
    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new MtlCommandList(ref description, this.gd);
    }

    /// <summary>
    /// Performs the CreateComputePipeline operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateComputePipeline operation.</returns>
    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new MtlPipeline(ref description, this.gd);
    }

    /// <summary>
    /// Performs the CreateFramebuffer operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateFramebuffer operation.</returns>
    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new MtlFramebuffer(this.gd, ref description);
    }

    /// <summary>
    /// Performs the CreateResourceLayout operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateResourceLayout operation.</returns>
    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new MtlResourceLayout(ref description, this.gd);
    }

    /// <summary>
    /// Performs the CreateResourceSet operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateResourceSet operation.</returns>
    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        ValidationHelpers.ValidateResourceSet(this.gd, ref description);
        return new MtlResourceSet(ref description, this.gd);
    }

    /// <summary>
    /// Performs the CreateFence operation.
    /// </summary>
    /// <param name="signaled">The value of signaled.</param>
    /// <returns>The result of the CreateFence operation.</returns>
    public override Fence CreateFence(bool signaled) {
        return new MtlFence(signaled);
    }

    /// <summary>
    /// Performs the CreateSwapchain operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateSwapchain operation.</returns>
    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new MtlSwapchain(this.gd, ref description);
    }

    /// <summary>
    /// Performs the CreateGraphicsPipelineCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateGraphicsPipelineCore operation.</returns>
    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new MtlPipeline(ref description, this.gd);
    }

    /// <summary>
    /// Performs the CreateSamplerCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateSamplerCore operation.</returns>
    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new MtlSampler(ref description, this.gd);
    }

    /// <summary>
    /// Performs the CreateShaderCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateShaderCore operation.</returns>
    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new MtlShader(ref description, this.gd);
    }

    /// <summary>
    /// Performs the CreateBufferCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateBufferCore operation.</returns>
    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new MtlBuffer(ref description, this.gd);
    }

    /// <summary>
    /// Performs the CreateTextureCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateTextureCore operation.</returns>
    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new MtlTexture(ref description, this.gd);
    }

    /// <summary>
    /// Performs the CreateTextureCore operation.
    /// </summary>
    /// <param name="nativeTexture">The value of nativeTexture.</param>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateTextureCore operation.</returns>
    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new MtlTexture(nativeTexture, ref description);
    }

    /// <summary>
    /// Performs the CreateTextureViewCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateTextureViewCore operation.</returns>
    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new MtlTextureView(ref description, this.gd);
    }
}