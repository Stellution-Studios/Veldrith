using System.Diagnostics;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12ResourceFactory.
/// </summary>
internal sealed class D3D12ResourceFactory : ResourceFactory {

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceFactory" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="features">The features value used by this operation.</param>
    public D3D12ResourceFactory(D3D12GraphicsDevice gd, GraphicsDeviceFeatures features) : base(features) {
        this.gd = gd;
    }

    /// <summary>
    /// Captures a timestamp when D3D12 performance logging is enabled.
    /// </summary>
    /// <returns>The start timestamp, or zero when logging is disabled.</returns>
    private static long GetPerfStartTicks() {
        return D3D12GraphicsDevice.PerfLogEnabled ? Stopwatch.GetTimestamp() : 0;
    }

    /// <summary>
    /// Records resource creation time when D3D12 performance logging is enabled.
    /// </summary>
    /// <param name="kind">The resource creation category.</param>
    /// <param name="startTicks">The timestamp captured before resource creation.</param>
    private void RecordCreationPerf(D3D12ResourceCreationKind kind, long startTicks) {
        if (startTicks == 0) {
            return;
        }

        double elapsedMs = (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;
        this.gd.RecordResourceCreationPerf(kind, elapsedMs);
    }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

    /// <summary>
    /// Creates the compute pipeline instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description) {
        long startTicks = GetPerfStartTicks();
        try {
            return new D3D12Pipeline(this.gd, ref description);
        }
        finally {
            this.RecordCreationPerf(D3D12ResourceCreationKind.Pipeline, startTicks);
        }
    }

    /// <summary>
    /// Creates the framebuffer instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Framebuffer CreateFramebuffer(ref FramebufferDescription description) {
        return new D3D12Framebuffer(this.gd, ref description);
    }

    /// <summary>
    /// Creates the command list instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override CommandList CreateCommandList(ref CommandListDescription description) {
        return new D3D12CommandList(this.gd, ref description, this.gd.Features, this.gd.UniformBufferMinOffsetAlignment, this.gd.StructuredBufferMinOffsetAlignment);
    }

    /// <summary>
    /// Creates the resource layout instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description) {
        return new D3D12ResourceLayout(ref description);
    }

    /// <summary>
    /// Creates the resource set instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override ResourceSet CreateResourceSet(ref ResourceSetDescription description) {
        long startTicks = GetPerfStartTicks();
        try {
            return new D3D12ResourceSet(this.gd, ref description);
        }
        finally {
            this.RecordCreationPerf(D3D12ResourceCreationKind.ResourceSet, startTicks);
        }
    }

    /// <summary>
    /// Creates the fence instance used by this backend.
    /// </summary>
    /// <param name="signaled">The signaled value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Fence CreateFence(bool signaled) {
        return new D3D12Fence(this.gd, signaled);
    }

    /// <summary>
    /// Creates the swapchain instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override Swapchain CreateSwapchain(ref SwapchainDescription description) {
        return new D3D12Swapchain(this.gd, ref description);
    }

    /// <summary>
    /// Creates the graphics pipeline core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description) {
        long startTicks = GetPerfStartTicks();
        try {
            return new D3D12Pipeline(this.gd, ref description);
        }
        finally {
            this.RecordCreationPerf(D3D12ResourceCreationKind.Pipeline, startTicks);
        }
    }

    /// <summary>
    /// Creates the texture core instance used by this backend.
    /// </summary>
    /// <param name="nativeTexture">The native texture value used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description) {
        long startTicks = GetPerfStartTicks();
        try {
            return new D3D12Texture(this.gd, ref description, nativeTexture);
        }
        finally {
            this.RecordCreationPerf(D3D12ResourceCreationKind.Texture, startTicks);
        }
    }

    /// <summary>
    /// Creates the texture core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Texture CreateTextureCore(ref TextureDescription description) {
        long startTicks = GetPerfStartTicks();
        try {
            return new D3D12Texture(this.gd, ref description, null);
        }
        finally {
            this.RecordCreationPerf(D3D12ResourceCreationKind.Texture, startTicks);
        }
    }

    /// <summary>
    /// Creates the texture view core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override TextureView CreateTextureViewCore(ref TextureViewDescription description) {
        return new D3D12TextureView(this.gd, ref description);
    }

    /// <summary>
    /// Creates the buffer core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override DeviceBuffer CreateBufferCore(ref BufferDescription description) {
        long startTicks = GetPerfStartTicks();
        try {
            return new D3D12DeviceBuffer(this.gd, ref description);
        }
        finally {
            this.RecordCreationPerf(D3D12ResourceCreationKind.Buffer, startTicks);
        }
    }

    /// <summary>
    /// Creates the sampler core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Sampler CreateSamplerCore(ref SamplerDescription description) {
        return new D3D12Sampler(this.gd, ref description);
    }

    /// <summary>
    /// Creates the shader core instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override Shader CreateShaderCore(ref ShaderDescription description) {
        long startTicks = GetPerfStartTicks();
        try {
            return new D3D12Shader(ref description);
        }
        finally {
            this.RecordCreationPerf(D3D12ResourceCreationKind.Shader, startTicks);
        }
    }
}
