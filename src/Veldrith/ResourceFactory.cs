namespace Veldrith;

/// <summary>
/// Represents the ResourceFactory type used by the graphics runtime.
/// </summary>
public abstract class ResourceFactory {
    /// <summary></summary>
    /// <param name="features">The features value used by this operation.</param>

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceFactory" /> type.
    /// </summary>
    /// <param name="features">The features value used by this operation.</param>
    protected ResourceFactory(GraphicsDeviceFeatures features) {
        this.Features = features;
    }

    /// <summary>
    /// Gets the <see cref="GraphicsBackend" /> of this instance.
    /// </summary>
    public abstract GraphicsBackend BackendType { get; }

    /// <summary>
    /// Gets the <see cref="GraphicsDeviceFeatures" /> this instance was created with.
    /// </summary>
    public GraphicsDeviceFeatures Features { get; }

    /// <summary>
    /// Creates the graphics pipeline instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Pipeline CreateGraphicsPipeline(GraphicsPipelineDescription description) {
        return this.CreateGraphicsPipeline(ref description);
    }

    /// <summary>
    /// Creates the graphics pipeline instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Pipeline CreateGraphicsPipeline(ref GraphicsPipelineDescription description) {
#if VALIDATE_USAGE
        if (!description.RasterizerState.DepthClipEnabled && !this.Features.DepthClipDisable) {
            throw new VeldridException("RasterizerState.DepthClipEnabled must be true if GraphicsDeviceFeatures.DepthClipDisable is not supported.");
        }

        if (description.RasterizerState.FillMode == PolygonFillMode.Wireframe && !this.Features.FillModeWireframe) {
            throw new VeldridException("PolygonFillMode.Wireframe requires GraphicsDeviceFeatures.FillModeWireframe.");
        }

        if (!this.Features.IndependentBlend) {
            if (description.BlendState.AttachmentStates.Length > 0) {
                BlendAttachmentDescription attachmentState = description.BlendState.AttachmentStates[0];

                for (int i = 1; i < description.BlendState.AttachmentStates.Length; i++) {
                    if (!attachmentState.Equals(description.BlendState.AttachmentStates[i])) {
                        throw new VeldridException("If GraphcsDeviceFeatures.IndependentBlend is false, then all members of BlendState.AttachmentStates must be equal.");
                    }
                }
            }
        }

        foreach (VertexLayoutDescription layoutDesc in description.ShaderSet.VertexLayouts) {
            bool hasExplicitLayout = false;
            uint minOffset = 0;

            foreach (VertexElementDescription elementDesc in layoutDesc.Elements) {
                if (hasExplicitLayout && elementDesc.Offset == 0) {
                    throw new VeldridException("If any vertex element has an explicit offset, then all elements must have an explicit offset.");
                }

                if (elementDesc.Offset != 0 && elementDesc.Offset < minOffset) {
                    throw new VeldridException($"Vertex element \"{elementDesc.Name}\" has an explicit offset which overlaps with the previous element.");
                }

                minOffset = elementDesc.Offset + FormatSizeHelpers.GetSizeInBytes(elementDesc.Format);
                hasExplicitLayout |= elementDesc.Offset != 0;
            }

            if (minOffset > layoutDesc.Stride) {
                throw new VeldridException($"The vertex layout's stride ({layoutDesc.Stride}) is less than the full size of the vertex ({minOffset})");
            }
        }
#endif
        return this.CreateGraphicsPipelineCore(ref description);
    }

    /// <summary>
    /// Creates the compute pipeline instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Pipeline CreateComputePipeline(ComputePipelineDescription description) {
        return this.CreateComputePipeline(ref description);
    }

    /// <summary>
    /// Creates the compute pipeline instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public abstract Pipeline CreateComputePipeline(ref ComputePipelineDescription description);

    /// <summary>
    /// Creates the framebuffer instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Framebuffer CreateFramebuffer(FramebufferDescription description) {
        return this.CreateFramebuffer(ref description);
    }

    /// <summary>
    /// Creates the framebuffer instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public abstract Framebuffer CreateFramebuffer(ref FramebufferDescription description);

    /// <summary>
    /// Creates the texture instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Texture CreateTexture(TextureDescription description) {
        return this.CreateTexture(ref description);
    }

    /// <summary>
    /// Creates the texture instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Texture CreateTexture(ref TextureDescription description) {
#if VALIDATE_USAGE
        if (description.Width == 0 || description.Height == 0 || description.Depth == 0) {
            throw new VeldridException("Width, Height, and Depth must be non-zero.");
        }

        if ((description.Format == PixelFormat.D24UNormS8UInt || description.Format == PixelFormat.D32FloatS8UInt)
            && (description.Usage & TextureUsage.DepthStencil) == 0) {
            throw new VeldridException("The givel PixelFormat can only be used in a Texture with DepthStencil usage.");
        }

        if ((description.Type == TextureType.Texture1D || description.Type == TextureType.Texture3D)
            && description.SampleCount != TextureSampleCount.Count1) {
            throw new VeldridException($"1D and 3D Textures must use {nameof(TextureSampleCount)}.{nameof(TextureSampleCount.Count1)}.");
        }

        if (description.Type == TextureType.Texture1D && !this.Features.Texture1D) {
            throw new VeldridException("1D Textures are not supported by this device.");
        }

        if ((description.Usage & TextureUsage.Staging) != 0 && description.Usage != TextureUsage.Staging) {
            throw new VeldridException($"{nameof(TextureUsage)}.{nameof(TextureUsage.Staging)} cannot be combined with any other flags.");
        }

        if ((description.Usage & TextureUsage.DepthStencil) != 0 && (description.Usage & TextureUsage.GenerateMipmaps) != 0) {
            throw new VeldridException($"{nameof(TextureUsage)}.{nameof(TextureUsage.DepthStencil)} and {nameof(TextureUsage)}.{nameof(TextureUsage.GenerateMipmaps)} cannot be combined.");
        }
#endif
        return this.CreateTextureCore(ref description);
    }

    /// <summary>
    /// Creates the texture instance used by this backend.
    /// </summary>
    /// <param name="nativeTexture">The native texture value used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Texture CreateTexture(ulong nativeTexture, TextureDescription description) {
        return this.CreateTextureCore(nativeTexture, ref description);
    }

    /// <summary>
    /// Creates the texture instance used by this backend.
    /// </summary>
    /// <param name="nativeTexture">The native texture value used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Texture CreateTexture(ulong nativeTexture, ref TextureDescription description) {
        return this.CreateTextureCore(nativeTexture, ref description);
    }

    /// <summary>
    /// Creates the texture view instance used by this backend.
    /// </summary>
    /// <param name="target">The target value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public TextureView CreateTextureView(Texture target) {
        return this.CreateTextureView(new TextureViewDescription(target));
    }

    /// <summary>
    /// Creates the texture view instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public TextureView CreateTextureView(TextureViewDescription description) {
        return this.CreateTextureView(ref description);
    }

    /// <summary>
    /// Creates the texture view instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public TextureView CreateTextureView(ref TextureViewDescription description) {
#if VALIDATE_USAGE
        if (description.MipLevels == 0 || description.ArrayLayers == 0
                                       || description.BaseMipLevel + description.MipLevels >
                                       description.Target.MipLevels
                                       || description.BaseArrayLayer + description.ArrayLayers >
                                       description.Target.ArrayLayers) {
            throw new VeldridException("TextureView mip level and array layer range must be contained in the target Texture.");
        }

        if ((description.Target.Usage & TextureUsage.Sampled) == 0
            && (description.Target.Usage & TextureUsage.Storage) == 0) {
            throw new VeldridException("To create a TextureView, the target texture must have either Sampled or Storage usage flags.");
        }

        if (!this.Features.SubsetTextureView && (description.BaseMipLevel != 0 || description.MipLevels != description.Target.MipLevels
                                           || description.BaseArrayLayer != 0 || description.ArrayLayers != description.Target.ArrayLayers)) {
            throw new VeldridException("GraphicsDevice does not support subset TextureViews.");
        }

        if (description.Format != null && description.Format != description.Target.Format) {
            if (!FormatHelpers.IsFormatViewCompatible(description.Format.Value, description.Target.Format)) {
                throw new VeldridException($"Cannot create a TextureView with format {description.Format.Value} targeting a Texture with format " + $"{description.Target.Format}. A TextureView's format must have the same size and number of " + "components as the underlying Texture's format, or the same format.");
            }
        }
#endif

        return this.CreateTextureViewCore(ref description);
    }

    /// <summary>
    /// Creates the buffer instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public DeviceBuffer CreateBuffer(BufferDescription description) {
        return this.CreateBuffer(ref description);
    }

    /// <summary>
    /// Creates the buffer instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public DeviceBuffer CreateBuffer(ref BufferDescription description) {
#if VALIDATE_USAGE
        BufferUsage usage = description.Usage;

        if ((usage & BufferUsage.StructuredBufferReadOnly) == BufferUsage.StructuredBufferReadOnly
            || (usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite) {
            if (!this.Features.StructuredBuffer) {
                throw new VeldridException("GraphicsDevice does not support structured buffers.");
            }

            if (description.StructureByteStride == 0) {
                throw new VeldridException("Structured Buffer objects must have a non-zero StructureByteStride.");
            }

        }
        else if (description.StructureByteStride != 0) {
            throw new VeldridException("Non-structured Buffers must have a StructureByteStride of zero.");
        }

        if ((usage & BufferUsage.Staging) != 0 && usage != BufferUsage.Staging) {
            throw new VeldridException("Buffers with Staging Usage must not specify any other Usage flags.");
        }

        if (((usage & BufferUsage.StructuredBufferReadOnly) != 0 || (usage & BufferUsage.StructuredBufferReadWrite) != 0)
            && (usage & BufferUsage.UniformBuffer) != 0) {
            throw new VeldridException($"Structured Buffer objects cannot specify {nameof(BufferUsage)}.{nameof(BufferUsage.UniformBuffer)}.");
        }

        if ((usage & BufferUsage.Dynamic) != 0 && (usage & BufferUsage.StructuredBufferReadWrite) != 0) {
            throw new VeldridException($"{nameof(BufferUsage)}.{nameof(BufferUsage.Dynamic)} cannot be combined with {nameof(BufferUsage)}.{nameof(BufferUsage.StructuredBufferReadWrite)}.");
        }

        if ((usage & BufferUsage.Dynamic) != 0 && (usage & BufferUsage.IndirectBuffer) != 0) {
            throw new VeldridException($"{nameof(BufferUsage)}.{nameof(BufferUsage.Dynamic)} cannot be combined with {nameof(BufferUsage)}.{nameof(BufferUsage.IndirectBuffer)}.");
        }

        if ((usage & BufferUsage.UniformBuffer) != 0 && description.SizeInBytes % 16 != 0) {
            throw new VeldridException("Uniform buffer size must be a multiple of 16 bytes.");
        }
#endif
        return this.CreateBufferCore(ref description);
    }

    /// <summary>
    /// Creates the sampler instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Sampler CreateSampler(SamplerDescription description) {
        return this.CreateSampler(ref description);
    }

    /// <summary>
    /// Creates the sampler instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Sampler CreateSampler(ref SamplerDescription description) {
#if VALIDATE_USAGE
        if (!this.Features.SamplerLodBias && description.LodBias != 0) {
            throw new VeldridException("GraphicsDevice does not support Sampler LOD bias. SamplerDescription.LodBias must be 0.");
        }

        if (!this.Features.SamplerAnisotropy && description.Filter == SamplerFilter.Anisotropic) {
            throw new VeldridException("SamplerFilter.Anisotropic cannot be used unless GraphicsDeviceFeatures.SamplerAnisotropy is supported.");
        }
#endif

        return this.CreateSamplerCore(ref description);
    }

    /// <summary>
    /// Creates the shader instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Shader CreateShader(ShaderDescription description) {
        return this.CreateShader(ref description);
    }

    /// <summary>
    /// Creates the shader instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Shader CreateShader(ref ShaderDescription description) {
#if VALIDATE_USAGE
        if (!this.Features.ComputeShader && description.Stage == ShaderStages.Compute) {
            throw new VeldridException("GraphicsDevice does not support Compute Shaders.");
        }

        if (!this.Features.GeometryShader && description.Stage == ShaderStages.Geometry) {
            throw new VeldridException("GraphicsDevice does not support Compute Shaders.");
        }

        if (!this.Features.TessellationShaders
            && (description.Stage == ShaderStages.TessellationControl
                || description.Stage == ShaderStages.TessellationEvaluation)) {
            throw new VeldridException("GraphicsDevice does not support Tessellation Shaders.");
        }
#endif
        return this.CreateShaderCore(ref description);
    }

    /// <summary>
    /// Creates the command list instance used by this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public CommandList CreateCommandList() {
        return this.CreateCommandList(new CommandListDescription());
    }

    /// <summary>
    /// Creates the command list instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public CommandList CreateCommandList(CommandListDescription description) {
        return this.CreateCommandList(ref description);
    }

    /// <summary>
    /// Creates the command list instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public abstract CommandList CreateCommandList(ref CommandListDescription description);

    /// <summary>
    /// Creates the resource layout instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public ResourceLayout CreateResourceLayout(ResourceLayoutDescription description) {
        return this.CreateResourceLayout(ref description);
    }

    /// <summary>
    /// Creates the resource layout instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public abstract ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description);

    /// <summary>
    /// Creates the resource set instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public ResourceSet CreateResourceSet(ResourceSetDescription description) {
        return this.CreateResourceSet(ref description);
    }

    /// <summary>
    /// Creates the resource set instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public abstract ResourceSet CreateResourceSet(ref ResourceSetDescription description);

    /// <summary>
    /// Creates the fence instance used by this backend.
    /// </summary>
    /// <param name="signaled">The signaled value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public abstract Fence CreateFence(bool signaled);

    /// <summary>
    /// Creates the swapchain instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Swapchain CreateSwapchain(SwapchainDescription description) {
        return this.CreateSwapchain(ref description);
    }

    /// <summary>
    /// Creates the swapchain instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public abstract Swapchain CreateSwapchain(ref SwapchainDescription description);

    /// <summary></summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns></returns>

    /// <summary>
    /// Creates the graphics pipeline core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected abstract Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description);

    /// <summary></summary>
    /// <param name="nativeTexture">The native texture value used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns></returns>

    /// <summary>
    /// Creates the texture core instance used by this backend.
    /// </summary>
    /// <param name="nativeTexture">The native texture value used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected abstract Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description);

    // TODO: private protected

    /// <summary>
    /// Creates the texture core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected abstract Texture CreateTextureCore(ref TextureDescription description);

    // TODO: private protected

    /// <summary>
    /// Creates the texture view core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected abstract TextureView CreateTextureViewCore(ref TextureViewDescription description);

    // TODO: private protected

    /// <summary>
    /// Creates the buffer core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected abstract DeviceBuffer CreateBufferCore(ref BufferDescription description);

    /// <summary></summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns></returns>

    /// <summary>
    /// Creates the sampler core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected abstract Sampler CreateSamplerCore(ref SamplerDescription description);

    /// <summary></summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns></returns>

    /// <summary>
    /// Creates the shader core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected abstract Shader CreateShaderCore(ref ShaderDescription description);
}
