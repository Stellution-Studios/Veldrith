using System;

namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the Texture class.
/// </summary>
public abstract class Texture : IDeviceResource, IMappableResource, IDisposable, IBindableResource {

    /// <summary>
    /// Stores the value associated with <c>_fullTextureViewLock</c>.
    /// </summary>
    private readonly object _fullTextureViewLock = new();

    /// <summary>
    /// Stores the value associated with <c>_fullTextureView</c>.
    /// </summary>
    private TextureView _fullTextureView;

    /// <summary>
    /// The format of individual texture elements stored in this instance.
    /// </summary>
    public abstract PixelFormat Format { get; }

    /// <summary>
    /// The total width of this instance, in texels.
    /// </summary>
    public abstract uint Width { get; }

    /// <summary>
    /// The total height of this instance, in texels.
    /// </summary>
    public abstract uint Height { get; }

    /// <summary>
    /// The total depth of this instance, in texels.
    /// </summary>
    public abstract uint Depth { get; }

    /// <summary>
    /// The total number of mipmap levels in this instance.
    /// </summary>
    public abstract uint MipLevels { get; }

    /// <summary>
    /// The total number of array layers in this instance.
    /// </summary>
    public abstract uint ArrayLayers { get; }

    /// <summary>
    /// The usage flags given when this instance was created. This property controls how this instance is permitted to be
    /// used, and it is an error to attempt to use the Texture outside of those contexts.
    /// </summary>
    public abstract TextureUsage Usage { get; }

    /// <summary>
    /// The <see cref="TextureType" /> of this instance.
    /// </summary>
    public abstract TextureType Type { get; }

    /// <summary>
    /// The number of samples in this instance. If this returns any value other than
    /// <see cref="TextureSampleCount.Count1" />,
    /// then this instance is a multipsample texture.
    /// </summary>
    public abstract TextureSampleCount SampleCount { get; }

    /// <summary>
    /// A bool indicating whether this instance has been disposed.
    /// </summary>
    public abstract bool IsDisposed { get; }

    /// <summary>
    /// A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
    /// tools.
    /// </summary>
    public abstract string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public virtual void Dispose() {
        lock (this._fullTextureViewLock) {
            this._fullTextureView?.Dispose();
        }

        this.DisposeCore();
    }

    #endregion

    /// <summary>
    /// Executes the CalculateSubresource operation.
    /// </summary>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    /// <returns>Returns the result produced by the CalculateSubresource operation.</returns>
    public uint CalculateSubresource(uint mipLevel, uint arrayLayer) {
        return arrayLayer * this.MipLevels + mipLevel;
    }

    /// <summary>
    /// Executes the GetFullTextureView operation.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <returns>Returns the result produced by the GetFullTextureView operation.</returns>
    internal TextureView GetFullTextureView(GraphicsDevice gd) {
        lock (this._fullTextureViewLock) {
            return this._fullTextureView ??= this.CreateFullTextureView(gd);
        }
    }

    /// <summary>
    /// Executes the CreateFullTextureView operation.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <returns>Returns the result produced by the CreateFullTextureView operation.</returns>
    private protected virtual TextureView CreateFullTextureView(GraphicsDevice gd) {
        return gd.ResourceFactory.CreateTextureView(this);
    }

    /// <summary>
    /// Executes the DisposeCore operation.
    /// </summary>
    private protected abstract void DisposeCore();
}
