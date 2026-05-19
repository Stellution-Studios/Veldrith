namespace Veldrith.D3D12;

/// <summary>
/// Represents the D3D12ResourceFactory class.
/// </summary>
internal sealed class D3D12ResourceFactory : ResourceFactory {

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceFactory" /> class.
    /// </summary>
    /// <param name="gd">The value of gd.</param>
    /// <param name="features">The value of features.</param>
    /// <returns>The result of the base operation.</returns>
    public D3D12ResourceFactory(D3D12GraphicsDevice gd, GraphicsDeviceFeatures features) : base(features) {
        this.gd = gd;
    }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

    /// <summary>
    /// Performs the CreateComputePipeline operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateComputePipeline operation.</returns>
    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new D3D12Pipeline(this.gd, ref description);
    }

    /// <summary>
    /// Performs the CreateFramebuffer operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateFramebuffer operation.</returns>
    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new D3D12Framebuffer(this.gd, ref description);
    }

    /// <summary>
    /// Performs the CreateCommandList operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateCommandList operation.</returns>
    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new D3D12CommandList(this.gd, ref description, this.gd.Features, this.gd.UniformBufferMinOffsetAlignment, this.gd.StructuredBufferMinOffsetAlignment);
    }

    /// <summary>
    /// Performs the CreateResourceLayout operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateResourceLayout operation.</returns>
    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new D3D12ResourceLayout(ref description);
    }

    /// <summary>
    /// Performs the CreateResourceSet operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateResourceSet operation.</returns>
    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        return new D3D12ResourceSet(ref description);
    }

    /// <summary>
    /// Performs the CreateFence operation.
    /// </summary>
    /// <param name="signaled">The value of signaled.</param>
    /// <returns>The result of the CreateFence operation.</returns>
    public override Fence CreateFence(bool signaled) {
        return new D3D12Fence(this.gd, signaled);
    }

    /// <summary>
    /// Performs the CreateSwapchain operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateSwapchain operation.</returns>
    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new D3D12Swapchain(this.gd, ref description);
    }

    /// <summary>
    /// Performs the CreateGraphicsPipelineCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateGraphicsPipelineCore operation.</returns>
    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new D3D12Pipeline(this.gd, ref description);
    }

    /// <summary>
    /// Performs the CreateTextureCore operation.
    /// </summary>
    /// <param name="nativeTexture">The value of nativeTexture.</param>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateTextureCore operation.</returns>
    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new D3D12Texture(this.gd, ref description, nativeTexture);
    }

    /// <summary>
    /// Performs the CreateTextureCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateTextureCore operation.</returns>
    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new D3D12Texture(this.gd, ref description, null);
    }

    /// <summary>
    /// Performs the CreateTextureViewCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateTextureViewCore operation.</returns>
    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new D3D12TextureView(this.gd, ref description);
    }

    /// <summary>
    /// Performs the CreateBufferCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateBufferCore operation.</returns>
    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new D3D12DeviceBuffer(this.gd, ref description);
    }

    /// <summary>
    /// Performs the CreateSamplerCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateSamplerCore operation.</returns>
    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new D3D12Sampler(this.gd, ref description);
    }

    /// <summary>
    /// Performs the CreateShaderCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateShaderCore operation.</returns>
    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new D3D12Shader(ref description);
    }
}
