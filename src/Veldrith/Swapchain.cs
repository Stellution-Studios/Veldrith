using System;

namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the Swapchain class.
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
    /// rate.
    /// </summary>
    public abstract bool SyncToVerticalBlank { get; set; }

    /// <summary>
    /// A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
    /// tools.
    /// </summary>
    public abstract string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public abstract void Dispose();

    #endregion

    /// <summary>
    /// Executes the Resize operation.
    /// </summary>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    public abstract void Resize(uint width, uint height);
}