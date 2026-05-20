namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkResourceFactory.
/// </summary>
internal class VkResourceFactory : ResourceFactory {

    /// <summary>
    /// Stores the graphics device used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkResourceFactory" /> class.
    /// </summary>
    /// <param name="vkGraphicsDevice">The graphics device that owns this operation.</param>
    public VkResourceFactory(VkGraphicsDevice vkGraphicsDevice) : base(vkGraphicsDevice.Features) {
        this._gd = vkGraphicsDevice;
    }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Vulkan;

    /// <summary>
    /// Creates the command list instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new VkCommandList(this._gd, ref description);
    }

    /// <summary>
    /// Creates the framebuffer instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new VkFramebuffer(this._gd, ref description, false);
    }

    /// <summary>
    /// Creates the compute pipeline instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new VkPipeline(this._gd, ref description);
    }

    /// <summary>
    /// Creates the resource layout instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new VkResourceLayout(this._gd, ref description);
    }

    /// <summary>
    /// Creates the resource set instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        ValidationHelpers.ValidateResourceSet(this._gd, ref description);
        return new VkResourceSet(this._gd, ref description);
    }

    /// <summary>
    /// Creates the fence instance used by this backend.
    /// </summary>
    /// <param name="signaled">The signaled value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Fence CreateFence(bool signaled) {
        return new VkFence(this._gd, signaled);
    }

    /// <summary>
    /// Creates the swapchain instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new VkSwapchain(this._gd, ref description);
    }

    /// <summary>
    /// Creates the graphics pipeline core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new VkPipeline(this._gd, ref description);
    }

    /// <summary>
    /// Creates the sampler core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new VkSampler(this._gd, ref description);
    }

    /// <summary>
    /// Creates the shader core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new VkShader(this._gd, ref description);
    }

    /// <summary>
    /// Creates the texture core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new VkTexture(this._gd, ref description);
    }

    /// <summary>
    /// Creates the texture core instance used by this backend.
    /// </summary>
    /// <param name="nativeTexture">The native texture value used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new VkTexture(this._gd, description.Width, description.Height, description.MipLevels, description.ArrayLayers, VkFormats.VdToVkPixelFormat(description.Format, (description.Usage & TextureUsage.DepthStencil) != 0), description.Usage, description.SampleCount, nativeTexture);
    }

    /// <summary>
    /// Creates the texture view core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new VkTextureView(this._gd, ref description);
    }

    /// <summary>
    /// Creates the buffer core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new VkBuffer(this._gd, description.SizeInBytes, description.Usage);
    }
}
