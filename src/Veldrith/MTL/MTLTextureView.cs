using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal class MtlTextureView : TextureView {
    private readonly bool _hasTextureView;
    private bool _disposed;

    public MtlTextureView(ref TextureViewDescription description, MtlGraphicsDevice gd)
        : base(ref description) {
        MtlTexture targetMtlTexture = Util.AssertSubtype<Texture, MtlTexture>(description.Target);

        if (this.BaseMipLevel != 0 || this.MipLevels != this.Target.MipLevels
                                   || this.BaseArrayLayer != 0 || this.ArrayLayers != this.Target.ArrayLayers
                                   || this.Format != this.Target.Format) {
            this._hasTextureView = true;
            uint effectiveArrayLayers = this.Target.Usage.HasFlag(TextureUsage.Cubemap)
                ? this.ArrayLayers * 6
                : this.ArrayLayers;
            this.TargetDeviceTexture = targetMtlTexture.DeviceTexture.newTextureView(
                MtlFormats.VdToMtlPixelFormat(this.Format, (description.Target.Usage & TextureUsage.DepthStencil) != 0),
                targetMtlTexture.MtlTextureType,
                new NSRange(this.BaseMipLevel, this.MipLevels),
                new NSRange(this.BaseArrayLayer, effectiveArrayLayers));
        }
        else {
            this.TargetDeviceTexture = targetMtlTexture.DeviceTexture;
        }
    }

    public MTLTexture TargetDeviceTexture { get; }

    public override bool IsDisposed => this._disposed;

    public override string Name { get; set; }

    #region Disposal

    public override void Dispose() {
        if (this._hasTextureView && !this._disposed) {
            this._disposed = true;
            ObjectiveCRuntime.release(this.TargetDeviceTexture.NativePtr);
        }
    }

    #endregion
}