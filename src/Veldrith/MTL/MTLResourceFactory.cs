namespace Veldrith.MTL;

/// <summary>
/// Defines the behavior and responsibilities of the MtlResourceFactory class.
/// </summary>
internal class MtlResourceFactory : ResourceFactory {

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlResourceFactory" /> class.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
    public MtlResourceFactory(MtlGraphicsDevice gd) : base(gd.Features) {
        this.gd = gd;
    }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Metal;

    /// <summary>
    /// Executes the CreateCommandList operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateCommandList operation.</returns>
    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new MtlCommandList(ref description, this.gd);
    }

    /// <summary>
    /// Executes the CreateComputePipeline operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateComputePipeline operation.</returns>
    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new MtlPipeline(ref description, this.gd);
    }

    /// <summary>
    /// Executes the CreateFramebuffer operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateFramebuffer operation.</returns>
    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new MtlFramebuffer(this.gd, ref description);
    }

    /// <summary>
    /// Executes the CreateResourceLayout operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateResourceLayout operation.</returns>
    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new MtlResourceLayout(ref description, this.gd);
    }

    /// <summary>
    /// Executes the CreateResourceSet operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateResourceSet operation.</returns>
    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        ValidationHelpers.ValidateResourceSet(this.gd, ref description);
        return new MtlResourceSet(ref description, this.gd);
    }

    /// <summary>
    /// Executes the CreateFence operation.
    /// </summary>
    /// <param name="signaled">Specifies the value of <paramref name="signaled" />.</param>
    /// <returns>Returns the result produced by the CreateFence operation.</returns>
    public override Fence CreateFence(bool signaled) {
        return new MtlFence(signaled);
    }

    /// <summary>
    /// Executes the CreateSwapchain operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateSwapchain operation.</returns>
    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new MtlSwapchain(this.gd, ref description);
    }

    /// <summary>
    /// Executes the CreateGraphicsPipelineCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateGraphicsPipelineCore operation.</returns>
    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new MtlPipeline(ref description, this.gd);
    }

    /// <summary>
    /// Executes the CreateSamplerCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateSamplerCore operation.</returns>
    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new MtlSampler(ref description, this.gd);
    }

    /// <summary>
    /// Executes the CreateShaderCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateShaderCore operation.</returns>
    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new MtlShader(ref description, this.gd);
    }

    /// <summary>
    /// Executes the CreateBufferCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateBufferCore operation.</returns>
    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new MtlBuffer(ref description, this.gd);
    }

    /// <summary>
    /// Executes the CreateTextureCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateTextureCore operation.</returns>
    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new MtlTexture(ref description, this.gd);
    }

    /// <summary>
    /// Executes the CreateTextureCore operation.
    /// </summary>
    /// <param name="nativeTexture">Specifies the value of <paramref name="nativeTexture" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateTextureCore operation.</returns>
    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new MtlTexture(nativeTexture, ref description);
    }

    /// <summary>
    /// Executes the CreateTextureViewCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateTextureViewCore operation.</returns>
    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new MtlTextureView(ref description, this.gd);
    }
}