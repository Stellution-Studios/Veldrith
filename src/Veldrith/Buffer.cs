using System;

namespace Veldrith;

/// <summary>
/// Represents the DeviceBuffer class.
/// </summary>
public abstract class DeviceBuffer : IDeviceResource, IBindableResource, IMappableResource, IDisposable {

    /// <summary>
    /// The total capacity, in bytes, of the buffer. This value is fixed upon creation.
    /// </summary>
    public abstract uint SizeInBytes { get; }

    /// <summary>
    /// A bitmask indicating how this instance is permitted to be used.
    /// </summary>
    public abstract BufferUsage Usage { get; }

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
    /// Frees unmanaged device resources controlled by this instance.
    /// </summary>
    public abstract void Dispose();

    #endregion
}