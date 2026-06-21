using System;

namespace Veldrith;

/// <summary>
/// A GPU-CPU sync point
/// </summary>
public abstract class Fence : IDeviceResource, IDisposable {

    /// <summary>
    /// Gets a value indicating whether the Fence is currently signaled. A Fence is signaled after a CommandList finishes
    /// </summary>
    public abstract bool Signaled { get; }

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

    /// <summary>
    /// Resets this instance to its initial state.
    /// </summary>
    public abstract void Reset();
}