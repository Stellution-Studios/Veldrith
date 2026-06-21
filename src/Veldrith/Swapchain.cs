using System;

namespace Veldrith;

/// <summary>
/// Represents the Swapchain type used by the graphics runtime.
/// </summary>
public abstract class Swapchain : IDeviceResource, IDisposable {

    /// <summary>
    /// Gets a <see cref="Framebuffer" /> representing the render targets of this instance.
    /// </summary>
    public abstract Framebuffer Framebuffer { get; }

    /// <summary>
    /// A bool indicating whether this instance has been disposed.
    /// </summary>
    public abstract bool IsDisposed { get; }

    /// <summary>
    /// Gets or sets whether presentation of this Swapchain will be synchronized to the window system's vertical refresh
    /// </summary>
    public abstract bool SyncToVerticalBlank { get; set; }

    /// <summary>
    /// A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
    /// </summary>
    public abstract string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public abstract void Dispose();

    #endregion

    /// <summary>
    /// Executes the resize logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    public abstract void Resize(uint width, uint height);
}