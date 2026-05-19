namespace Veldrith.D3D12;

/// <summary>
/// Defines the behavior and responsibilities of the D3D12ResourceFactory class.
/// </summary>
internal sealed class D3D12ResourceFactory : ResourceFactory {

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceFactory" /> class.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="features">Specifies the value of <paramref name="features" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
    public D3D12ResourceFactory(D3D12GraphicsDevice gd, GraphicsDeviceFeatures features) : base(features) {
        this.gd = gd;
    }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

    /// <summary>
    /// Executes the CreateComputePipeline operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateComputePipeline operation.</returns>
    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new D3D12Pipeline(this.gd, ref description);
    }

    /// <summary>
    /// Executes the CreateFramebuffer operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateFramebuffer operation.</returns>
    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new D3D12Framebuffer(this.gd, ref description);
    }

    /// <summary>
    /// Executes the CreateCommandList operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateCommandList operation.</returns>
    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new D3D12CommandList(this.gd, ref description, this.gd.Features, this.gd.UniformBufferMinOffsetAlignment, this.gd.StructuredBufferMinOffsetAlignment);
    }

    /// <summary>
    /// Executes the CreateResourceLayout operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateResourceLayout operation.</returns>
    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new D3D12ResourceLayout(ref description);
    }

    /// <summary>
    /// Executes the CreateResourceSet operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateResourceSet operation.</returns>
    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        return new D3D12ResourceSet(ref description);
    }

    /// <summary>
    /// Executes the CreateFence operation.
    /// </summary>
    /// <param name="signaled">Specifies the value of <paramref name="signaled" />.</param>
    /// <returns>Returns the result produced by the CreateFence operation.</returns>
    public override Fence CreateFence(bool signaled) {
        return new D3D12Fence(this.gd, signaled);
    }

    /// <summary>
    /// Executes the CreateSwapchain operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateSwapchain operation.</returns>
    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new D3D12Swapchain(this.gd, ref description);
    }

    /// <summary>
    /// Executes the CreateGraphicsPipelineCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateGraphicsPipelineCore operation.</returns>
    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new D3D12Pipeline(this.gd, ref description);
    }

    /// <summary>
    /// Executes the CreateTextureCore operation.
    /// </summary>
    /// <param name="nativeTexture">Specifies the value of <paramref name="nativeTexture" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateTextureCore operation.</returns>
    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new D3D12Texture(this.gd, ref description, nativeTexture);
    }

    /// <summary>
    /// Executes the CreateTextureCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateTextureCore operation.</returns>
    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new D3D12Texture(this.gd, ref description, null);
    }

    /// <summary>
    /// Executes the CreateTextureViewCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateTextureViewCore operation.</returns>
    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new D3D12TextureView(this.gd, ref description);
    }

    /// <summary>
    /// Executes the CreateBufferCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateBufferCore operation.</returns>
    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new D3D12DeviceBuffer(this.gd, ref description);
    }

    /// <summary>
    /// Executes the CreateSamplerCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateSamplerCore operation.</returns>
    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new D3D12Sampler(this.gd, ref description);
    }

    /// <summary>
    /// Executes the CreateShaderCore operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateShaderCore operation.</returns>
    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new D3D12Shader(ref description);
    }
}
