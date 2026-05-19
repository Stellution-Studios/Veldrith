using Veldrith.MetalBindings;

namespace Veldrith.MTL
{
    internal class MtlTextureView : TextureView
    {
        public MTLTexture TargetDeviceTexture { get; }

        public override bool IsDisposed => disposed;

        public override string Name { get; set; }
        private readonly bool hasTextureView;
        private bool disposed;

        public MtlTextureView(ref TextureViewDescription description, MtlGraphicsDevice gd)
            : base(ref description)
        {
            var targetMtlTexture = Util.AssertSubtype<Texture, MtlTexture>(description.Target);

            if (BaseMipLevel != 0 || MipLevels != Target.MipLevels
                                  || BaseArrayLayer != 0 || ArrayLayers != Target.ArrayLayers
                                  || Format != Target.Format)
            {
                hasTextureView = true;
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
            if (hasTextureView && !disposed)
            {
                disposed = true;
                ObjectiveCRuntime.release(TargetDeviceTexture.NativePtr);
            }
        }

        #endregion
    }
}
