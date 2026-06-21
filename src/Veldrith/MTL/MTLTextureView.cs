using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlTextureView.
/// </summary>
internal class MtlTextureView : TextureView {

    /// <summary>
    /// Tracks whether has texture view is currently enabled.
    /// </summary>
    private readonly bool _hasTextureView;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlTextureView" /> class.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="gd">The graphics device that owns this operation.</param>
    public MtlTextureView(ref TextureViewDescription description, MtlGraphicsDevice gd) : base(ref description) {
        MtlTexture targetMtlTexture = Util.AssertSubtype<Texture, MtlTexture>(description.Target);

        if (this.BaseMipLevel != 0 || this.MipLevels != this.Target.MipLevels
                                   || this.BaseArrayLayer != 0 || this.ArrayLayers != this.Target.ArrayLayers
                                   || this.Format != this.Target.Format) {
            this._hasTextureView = true;
            uint effectiveArrayLayers = this.Target.Usage.HasFlag(TextureUsage.Cubemap)
                ? this.ArrayLayers * 6
                : this.ArrayLayers;
            this.TargetDeviceTexture = targetMtlTexture.DeviceTexture.NewTextureView(MtlFormats.VdToMtlPixelFormat(this.Format, (description.Target.Usage & TextureUsage.DepthStencil) != 0), targetMtlTexture.MtlTextureType, new NSRange(this.BaseMipLevel, this.MipLevels), new NSRange(this.BaseArrayLayer, effectiveArrayLayers));
        }
        else {
            this.TargetDeviceTexture = targetMtlTexture.DeviceTexture;
        }
    }

    /// <summary>
    /// Gets or sets TargetDeviceTexture.
    /// </summary>
    public MTLTexture TargetDeviceTexture { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (this._hasTextureView && !this._disposed) {
            this._disposed = true;
            ObjectiveCRuntime.Release(this.TargetDeviceTexture.NativePtr);
        }
    }

    #endregion
}