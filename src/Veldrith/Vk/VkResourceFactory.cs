namespace Veldrith.Vk;

/// <summary>
/// Represents the VkResourceFactory class.
/// </summary>
internal class VkResourceFactory : ResourceFactory {

    /// <summary>
    /// Represents the _gd field.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkResourceFactory" /> class.
    /// </summary>
    /// <param name="vkGraphicsDevice">The value of vkGraphicsDevice.</param>
    /// <returns>The result of the base operation.</returns>
    public VkResourceFactory(VkGraphicsDevice vkGraphicsDevice) : base(vkGraphicsDevice.Features) {
        this._gd = vkGraphicsDevice;
    }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Vulkan;

    /// <summary>
    /// Performs the CreateCommandList operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateCommandList operation.</returns>
    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new VkCommandList(this._gd, ref description);
    }

    /// <summary>
    /// Performs the CreateFramebuffer operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateFramebuffer operation.</returns>
    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new VkFramebuffer(this._gd, ref description, false);
    }

    /// <summary>
    /// Performs the CreateComputePipeline operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateComputePipeline operation.</returns>
    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new VkPipeline(this._gd, ref description);
    }

    /// <summary>
    /// Performs the CreateResourceLayout operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateResourceLayout operation.</returns>
    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new VkResourceLayout(this._gd, ref description);
    }

    /// <summary>
    /// Performs the CreateResourceSet operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateResourceSet operation.</returns>
    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        ValidationHelpers.ValidateResourceSet(this._gd, ref description);
        return new VkResourceSet(this._gd, ref description);
    }

    /// <summary>
    /// Performs the CreateFence operation.
    /// </summary>
    /// <param name="signaled">The value of signaled.</param>
    /// <returns>The result of the CreateFence operation.</returns>
    public override Fence CreateFence(bool signaled) {
        return new VkFence(this._gd, signaled);
    }

    /// <summary>
    /// Performs the CreateSwapchain operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateSwapchain operation.</returns>
    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new VkSwapchain(this._gd, ref description);
    }

    /// <summary>
    /// Performs the CreateGraphicsPipelineCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateGraphicsPipelineCore operation.</returns>
    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new VkPipeline(this._gd, ref description);
    }

    /// <summary>
    /// Performs the CreateSamplerCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateSamplerCore operation.</returns>
    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new VkSampler(this._gd, ref description);
    }

    /// <summary>
    /// Performs the CreateShaderCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateShaderCore operation.</returns>
    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new VkShader(this._gd, ref description);
    }

    /// <summary>
    /// Performs the CreateTextureCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateTextureCore operation.</returns>
    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new VkTexture(this._gd, ref description);
    }

    /// <summary>
    /// Performs the CreateTextureCore operation.
    /// </summary>
    /// <param name="nativeTexture">The value of nativeTexture.</param>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateTextureCore operation.</returns>
    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new VkTexture(this._gd, description.Width, description.Height, description.MipLevels, description.ArrayLayers, VkFormats.VdToVkPixelFormat(description.Format, (description.Usage & TextureUsage.DepthStencil) != 0), description.Usage, description.SampleCount, nativeTexture);
    }

    /// <summary>
    /// Performs the CreateTextureViewCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateTextureViewCore operation.</returns>
    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new VkTextureView(this._gd, ref description);
    }

    /// <summary>
    /// Performs the CreateBufferCore operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateBufferCore operation.</returns>
    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new VkBuffer(this._gd, description.SizeInBytes, description.Usage);
    }
}
