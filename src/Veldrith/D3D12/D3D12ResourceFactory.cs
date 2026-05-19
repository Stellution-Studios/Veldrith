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
    public D3D12ResourceFactory(D3D12GraphicsDevice gd, GraphicsDeviceFeatures features)

        /// <summary>
        /// Executes base.
        /// </summary>
        : base(features) {
        this.gd = gd;
    }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

    /// <summary>
    /// Executes CreateComputePipeline.
    /// </summary>
    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new D3D12Pipeline(this.gd, ref description);
    }

    /// <summary>
    /// Executes CreateFramebuffer.
    /// </summary>
    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new D3D12Framebuffer(this.gd, ref description);
    }

    /// <summary>
    /// Executes CreateCommandList.
    /// </summary>
    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new D3D12CommandList(this.gd, ref description, this.gd.Features, this.gd.UniformBufferMinOffsetAlignment, this.gd.StructuredBufferMinOffsetAlignment);
    }

    /// <summary>
    /// Executes CreateResourceLayout.
    /// </summary>
    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new D3D12ResourceLayout(ref description);
    }

    /// <summary>
    /// Executes CreateResourceSet.
    /// </summary>
    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        return new D3D12ResourceSet(ref description);
    }

    /// <summary>
    /// Executes CreateFence.
    /// </summary>
    public override Fence CreateFence(bool signaled) {
        return new D3D12Fence(this.gd, signaled);
    }

    /// <summary>
    /// Executes CreateSwapchain.
    /// </summary>
    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new D3D12Swapchain(this.gd, ref description);
    }

    /// <summary>
    /// Executes CreateGraphicsPipelineCore.
    /// </summary>
    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new D3D12Pipeline(this.gd, ref description);
    }

    /// <summary>
    /// Executes CreateTextureCore.
    /// </summary>
    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new D3D12Texture(this.gd, ref description, nativeTexture);
    }

    /// <summary>
    /// Executes CreateTextureCore.
    /// </summary>
    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new D3D12Texture(this.gd, ref description, null);
    }

    /// <summary>
    /// Executes CreateTextureViewCore.
    /// </summary>
    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new D3D12TextureView(this.gd, ref description);
    }

    /// <summary>
    /// Executes CreateBufferCore.
    /// </summary>
    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new D3D12DeviceBuffer(this.gd, ref description);
    }

    /// <summary>
    /// Executes CreateSamplerCore.
    /// </summary>
    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new D3D12Sampler(this.gd, ref description);
    }

    /// <summary>
    /// Executes CreateShaderCore.
    /// </summary>
    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new D3D12Shader(ref description);
    }
}