using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Represents the MtlSwapchainTexture class.
/// </summary>
internal class MtlSwapchainTexture : MtlTexture {

    /// <summary>
    /// Represents the _deviceTexture field.
    /// </summary>
    private MTLTexture _deviceTexture;

    /// <summary>
    /// Represents the _height field.
    /// </summary>
    private uint _height;

    /// <summary>
    /// Represents the _mtlPixelFormat field.
    /// </summary>
    private MTLPixelFormat _mtlPixelFormat;

    /// <summary>
    /// Represents the _width field.
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
    /// Executes SetDrawable.
    /// </summary>
    public void SetDrawable(CAMetalDrawable drawable, CGSize size, PixelFormat format) {
        this._deviceTexture = drawable.texture;
        this._width = (uint)size.width;
        this._height = (uint)size.height;
        this._mtlPixelFormat = MtlFormats.VdToMtlPixelFormat(this.Format, false);
    }
}