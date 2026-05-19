using System;

namespace Veldrith;

/// <summary>
/// Represents the Sampler class.
/// </summary>
public abstract class Sampler : IDeviceResource, IBindableResource, IDisposable {

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
    /// Performs the Dispose operation.
    /// </summary>
    public abstract void Dispose();

    #endregion
}