namespace Veldrith.Vk;

internal class VkResourceFactory : ResourceFactory {

    /// <summary>
    /// Represents the _gd field.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkResourceFactory" /> class.
    /// </summary>
    public VkResourceFactory(VkGraphicsDevice vkGraphicsDevice)
        : base(vkGraphicsDevice.Features) {
        this._gd = vkGraphicsDevice;
    }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Vulkan;

    /// <summary>
    /// Executes CreateCommandList.
    /// </summary>
    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new VkCommandList(this._gd, ref description);
    }

    /// <summary>
    /// Executes CreateFramebuffer.
    /// </summary>
    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new VkFramebuffer(this._gd, ref description, false);
    }

    /// <summary>
    /// Executes CreateComputePipeline.
    /// </summary>
    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        return new VkPipeline(this._gd, ref description);
    }

    /// <summary>
    /// Executes CreateResourceLayout.
    /// </summary>
    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new VkResourceLayout(this._gd, ref description);
    }

    /// <summary>
    /// Executes CreateResourceSet.
    /// </summary>
    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        ValidationHelpers.ValidateResourceSet(this._gd, ref description);
        return new VkResourceSet(this._gd, ref description);
    }

    /// <summary>
    /// Executes CreateFence.
    /// </summary>
    public override Fence CreateFence(bool signaled) {
        return new VkFence(this._gd, signaled);
    }

    /// <summary>
    /// Executes CreateSwapchain.
    /// </summary>
    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new VkSwapchain(this._gd, ref description);
    }

    /// <summary>
    /// Executes CreateGraphicsPipelineCore.
    /// </summary>
    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        return new VkPipeline(this._gd, ref description);
    }

    /// <summary>
    /// Executes CreateSamplerCore.
    /// </summary>
    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new VkSampler(this._gd, ref description);
    }

    /// <summary>
    /// Executes CreateShaderCore.
    /// </summary>
    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        return new VkShader(this._gd, ref description);
    }

    /// <summary>
    /// Executes CreateTextureCore.
    /// </summary>
    protected override Texture CreateTextureCore(ref TextureDescription description) {
        return new VkTexture(this._gd, ref description);
    }

    /// <summary>
    /// Executes CreateTextureCore.
    /// </summary>
    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        return new VkTexture(this._gd, description.Width, description.Height, description.MipLevels, description.ArrayLayers, VkFormats.VdToVkPixelFormat(description.Format, (description.Usage & TextureUsage.DepthStencil) != 0), description.Usage, description.SampleCount, nativeTexture);
    }

    /// <summary>
    /// Executes CreateTextureViewCore.
    /// </summary>
    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new VkTextureView(this._gd, ref description);
    }

    /// <summary>
    /// Executes CreateBufferCore.
    /// </summary>
    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        return new VkBuffer(this._gd, description.SizeInBytes, description.Usage);
    }
}