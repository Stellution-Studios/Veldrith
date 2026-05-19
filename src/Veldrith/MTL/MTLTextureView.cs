using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Defines the behavior and responsibilities of the MtlTextureView class.
/// </summary>
internal class MtlTextureView : TextureView {

    /// <summary>
    /// Stores the value associated with <c>_hasTextureView</c>.
    /// </summary>
    private readonly bool _hasTextureView;

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlTextureView" /> class.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
    public MtlTextureView(ref TextureViewDescription description, MtlGraphicsDevice gd) : base(ref description) {
        MtlTexture targetMtlTexture = Util.AssertSubtype<Texture, MtlTexture>(description.Target);

        if (this.BaseMipLevel != 0 || this.MipLevels != this.Target.MipLevels
                                   || this.BaseArrayLayer != 0 || this.ArrayLayers != this.Target.ArrayLayers
                                   || this.Format != this.Target.Format) {
            this._hasTextureView = true;
            uint effectiveArrayLayers = this.Target.Usage.HasFlag(TextureUsage.Cubemap)
                ? this.ArrayLayers * 6
                : this.ArrayLayers;
            this.TargetDeviceTexture = targetMtlTexture.DeviceTexture.newTextureView(MtlFormats.VdToMtlPixelFormat(this.Format, (description.Target.Usage & TextureUsage.DepthStencil) != 0), targetMtlTexture.MtlTextureType, new NSRange(this.BaseMipLevel, this.MipLevels), new NSRange(this.BaseArrayLayer, effectiveArrayLayers));
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        if (this._hasTextureView && !this._disposed) {
            this._disposed = true;
            ObjectiveCRuntime.release(this.TargetDeviceTexture.NativePtr);
        }
    }

    #endregion
}
