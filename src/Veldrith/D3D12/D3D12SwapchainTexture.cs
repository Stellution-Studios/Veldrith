namespace Veldrith.D3D12;

/// <summary>
/// Provides lightweight framebuffer metadata for a native D3D12 swapchain color target.
/// </summary>
internal sealed class D3D12SwapchainTexture : Texture {

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12SwapchainTexture" /> type.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="format">The format value used by this operation.</param>
    public D3D12SwapchainTexture(uint width, uint height, PixelFormat format) {
        this.Width = width;
        this.Height = height;
        this.Format = format;
    }

    /// <summary>
    /// Gets or sets Format.
    /// </summary>
    public override PixelFormat Format { get; }

    /// <summary>
    /// Gets or sets Width.
    /// </summary>
    public override uint Width { get; }

    /// <summary>
    /// Gets or sets Height.
    /// </summary>
    public override uint Height { get; }

    /// <summary>
    /// Gets or sets Depth.
    /// </summary>
    public override uint Depth => 1;

    /// <summary>
    /// Gets or sets MipLevels.
    /// </summary>
    public override uint MipLevels => 1;

    /// <summary>
    /// Gets or sets ArrayLayers.
    /// </summary>
    public override uint ArrayLayers => 1;

    /// <summary>
    /// Gets or sets Usage.
    /// </summary>
    public override TextureUsage Usage => TextureUsage.RenderTarget;

    /// <summary>
    /// Gets or sets Type.
    /// </summary>
    public override TextureType Type => TextureType.Texture2D;

    /// <summary>
    /// Gets or sets SampleCount.
    /// </summary>
    public override TextureSampleCount SampleCount => TextureSampleCount.Count1;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private protected override void DisposeCore() {
        this._disposed = true;
    }
}
