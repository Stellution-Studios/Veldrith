using System;

namespace Veldrith;

/// <summary>
/// Represents the Texture type used by the graphics runtime.
/// </summary>
public abstract class Texture : IDeviceResource, IMappableResource, IDisposable, IBindableResource {

    /// <summary>
    /// Synchronizes access to the full texture view lock state.
    /// </summary>
    private readonly object _fullTextureViewLock = new();

    /// <summary>
    /// Stores the full texture view state used by this instance.
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
    /// </summary>
    public abstract TextureUsage Usage { get; }

    /// <summary>
    /// The <see cref="TextureType" /> of this instance.
    /// </summary>
    public abstract TextureType Type { get; }

    /// <summary>
    /// The number of samples in this instance. If this returns any value other than
    /// </summary>
    public abstract TextureSampleCount SampleCount { get; }

    /// <summary>
    /// A bool indicating whether this instance has been disposed.
    /// </summary>
    public abstract bool IsDisposed { get; }

    /// <summary>
    /// A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
    /// </summary>
    public abstract string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public virtual void Dispose() {
        lock (this._fullTextureViewLock) {
            this._fullTextureView?.Dispose();
            
            // Held through DisposeCore so a concurrent GetFullTextureView can't
            // build a view on a texture whose device resource is being freed.
            this.DisposeCore();
        }
    }

    #endregion

    /// <summary>
    /// Executes the calculate subresource logic for this backend.
    /// </summary>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    /// <returns>The value produced by this operation.</returns>
    public uint CalculateSubresource(uint mipLevel, uint arrayLayer) {
        return arrayLayer * this.MipLevels + mipLevel;
    }

    /// <summary>
    /// Gets the full texture view value.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal TextureView GetFullTextureView(GraphicsDevice gd) {
        lock (this._fullTextureViewLock) {
            return this._fullTextureView ??= this.CreateFullTextureView(gd);
        }
    }

    /// <summary>
    /// Creates the full texture view instance used by this backend.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private protected virtual TextureView CreateFullTextureView(GraphicsDevice gd) {
        return gd.ResourceFactory.CreateTextureView(this);
    }

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private protected abstract void DisposeCore();
}