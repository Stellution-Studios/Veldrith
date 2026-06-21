namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlResourceFactory.
/// </summary>
internal class MtlResourceFactory : ResourceFactory {

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly MtlGraphicsDevice gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlResourceFactory" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    public MtlResourceFactory(MtlGraphicsDevice gd) : base(gd.Features) {
        this.gd = gd;
    }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Metal;

    /// <summary>
    /// Creates the command list instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new MtlCommandList(ref description, this.gd);
    }

    /// <summary>
    /// Creates the compute pipeline instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new MtlPipeline(ref description, this.gd);
    }

    /// <summary>
    /// Creates the framebuffer instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new MtlFramebuffer(this.gd, ref description);
    }

    /// <summary>
    /// Creates the resource layout instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new MtlResourceLayout(ref description, this.gd);
    }

    /// <summary>
    /// Creates the resource set instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        ValidationHelpers.ValidateResourceSet(this.gd, ref description);
        return new MtlResourceSet(ref description, this.gd);
    }

    /// <summary>
    /// Creates the fence instance used by this backend.
    /// </summary>
    /// <param name="signaled">The signaled value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Fence CreateFence(bool signaled) {
        return new MtlFence(signaled);
    }

    /// <summary>
    /// Creates the swapchain instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new MtlSwapchain(this.gd, ref description);
    }

    /// <summary>
    /// Creates the graphics pipeline core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new MtlPipeline(ref description, this.gd);
    }

    /// <summary>
    /// Creates the sampler core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new MtlSampler(ref description, this.gd);
    }

    /// <summary>
    /// Creates the shader core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new MtlShader(ref description, this.gd);
    }

    /// <summary>
    /// Creates the buffer core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new MtlBuffer(ref description, this.gd);
    }

    /// <summary>
    /// Creates the texture core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new MtlTexture(ref description, this.gd);
    }

    /// <summary>
    /// Creates the texture core instance used by this backend.
    /// </summary>
    /// <param name="nativeTexture">The native texture value used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new MtlTexture(nativeTexture, ref description);
    }

    /// <summary>
    /// Creates the texture view core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new MtlTextureView(ref description, this.gd);
    }
}