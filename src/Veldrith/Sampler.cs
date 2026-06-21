using System;

namespace Veldrith;

/// <summary>
/// Represents the Sampler type used by the graphics runtime.
/// </summary>
public abstract class Sampler : IDeviceResource, IBindableResource, IDisposable {

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
    public abstract void Dispose();

    #endregion
}