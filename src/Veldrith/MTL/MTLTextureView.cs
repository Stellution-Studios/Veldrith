using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal class MtlTextureView : TextureView
    {
        public MTLTexture TargetDeviceTexture { get; }

        public override bool IsDisposed => _disposed;

        public override string Name { get; set; }
        private readonly bool _hasTextureView;
        private bool _disposed;

        public MtlTextureView(ref TextureViewDescription description, MtlGraphicsDevice gd)
            : base(ref description)
        {
            var targetMtlTexture = Util.AssertSubtype<Texture, MtlTexture>(description.Target);

            if (BaseMipLevel != 0 || MipLevels != Target.MipLevels
                                  || BaseArrayLayer != 0 || ArrayLayers != Target.ArrayLayers
                                  || Format != Target.Format)
            {
                _hasTextureView = true;
                uint effectiveArrayLayers = Target.Usage.HasFlag(TextureUsage.Cubemap) ? ArrayLayers * 6 : ArrayLayers;
                TargetDeviceTexture = targetMtlTexture.DeviceTexture.newTextureView(
                    MtlFormats.VdToMtlPixelFormat(Format, (description.Target.Usage & TextureUsage.DepthStencil) != 0),
                    targetMtlTexture.MtlTextureType,
                    new NSRange(BaseMipLevel, MipLevels),
                    new NSRange(BaseArrayLayer, effectiveArrayLayers));
            }
            else
                TargetDeviceTexture = targetMtlTexture.DeviceTexture;
        }

        #region Disposal

        public override void Dispose()
        {
            if (_hasTextureView && !_disposed)
            {
                _disposed = true;
                ObjectiveCRuntime.release(TargetDeviceTexture.NativePtr);
            }
        }

        #endregion
    }
}
