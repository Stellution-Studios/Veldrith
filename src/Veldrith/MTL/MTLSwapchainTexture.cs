// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal class MtlSwapchainTexture : MtlTexture {
    private MTLTexture _deviceTexture;
    private uint _height;
    private MTLPixelFormat _mtlPixelFormat;
    private uint _width;
    public override MTLTexture DeviceTexture => this._deviceTexture;

    public override uint Width => this._width;

    public override uint Height => this._height;

    public override uint Depth => 1;

    public override uint ArrayLayers => 1;

    public override uint MipLevels => 1;

    public override TextureUsage Usage => TextureUsage.RenderTarget;

    public override TextureType Type => TextureType.Texture2D;

    public override TextureSampleCount SampleCount => TextureSampleCount.Count1;

    public override MTLPixelFormat MtlPixelFormat => this._mtlPixelFormat;

    public override MTLTextureType MtlTextureType => MTLTextureType.Type2D;

    public void SetDrawable(CAMetalDrawable drawable, CGSize size, PixelFormat format) {
        this._deviceTexture = drawable.texture;
        this._width = (uint)size.width;
        this._height = (uint)size.height;
        this._mtlPixelFormat = MtlFormats.VdToMtlPixelFormat(this.Format, false);
    }
}