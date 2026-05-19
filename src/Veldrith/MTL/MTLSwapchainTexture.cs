using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlSwapchainTexture.
/// </summary>
internal class MtlSwapchainTexture : MtlTexture {

    /// <summary>
    /// Stores the device texture state used by this instance.
    /// </summary>
    private MTLTexture _deviceTexture;

    /// <summary>
    /// Stores the height value used during command execution.
    /// </summary>
    private uint _height;

    /// <summary>
    /// Stores the mtl pixel format state used by this instance.
    /// </summary>
    private MTLPixelFormat _mtlPixelFormat;

    /// <summary>
    /// Stores the width value used during command execution.
    /// </summary>
    private uint _width;

    /// <summary>
    /// Gets or sets DeviceTexture.
    /// </summary>
    public override MTLTexture DeviceTexture => this._deviceTexture;

    /// <summary>
    /// Gets or sets Width.
    /// </summary>
    public override uint Width => this._width;

    /// <summary>
    /// Gets or sets Height.
    /// </summary>
    public override uint Height => this._height;

    /// <summary>
    /// Gets or sets Depth.
    /// </summary>
    public override uint Depth => 1;

    /// <summary>
    /// Gets or sets ArrayLayers.
    /// </summary>
    public override uint ArrayLayers => 1;

    /// <summary>
    /// Gets or sets MipLevels.
    /// </summary>
    public override uint MipLevels => 1;

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
    /// Gets or sets MtlPixelFormat.
    /// </summary>
    public override MTLPixelFormat MtlPixelFormat => this._mtlPixelFormat;

    /// <summary>
    /// Gets or sets MtlTextureType.
    /// </summary>
    public override MTLTextureType MtlTextureType => MTLTextureType.Type2D;

    /// <summary>
    /// Sets the drawable value.
    /// </summary>
    /// <param name="drawable">The drawable value used by this operation.</param>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    public void SetDrawable(CAMetalDrawable drawable, CGSize size, PixelFormat format) {
        this._deviceTexture = drawable.texture;
        this._width = (uint)size.width;
        this._height = (uint)size.height;
        this._mtlPixelFormat = MtlFormats.VdToMtlPixelFormat(this.Format, false);
    }
}