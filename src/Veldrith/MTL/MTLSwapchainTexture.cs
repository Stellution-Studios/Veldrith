// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal class MtlSwapchainTexture : MtlTexture
    {
        public override MTLTexture DeviceTexture => _deviceTexture;

        public override uint Width => _width;

        public override uint Height => _height;

        public override uint Depth => 1;

        public override uint ArrayLayers => 1;

        public override uint MipLevels => 1;

        public override TextureUsage Usage => TextureUsage.RenderTarget;

        public override TextureType Type => TextureType.Texture2D;

        public override TextureSampleCount SampleCount => TextureSampleCount.Count1;

        public override MTLPixelFormat MtlPixelFormat => _mtlPixelFormat;

        public override MTLTextureType MtlTextureType => MTLTextureType.Type2D;

        private MTLTexture _deviceTexture;
        private uint _width;
        private uint _height;
        private MTLPixelFormat _mtlPixelFormat;

        public void SetDrawable(CAMetalDrawable drawable, CGSize size, PixelFormat format)
        {
            _deviceTexture = drawable.texture;
            _width = (uint)size.width;
            _height = (uint)size.height;
            _mtlPixelFormat = MtlFormats.VdToMtlPixelFormat(Format, false);
        }
    }
}
