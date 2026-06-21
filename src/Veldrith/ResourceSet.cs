using System;

namespace Veldrith;

/// <summary>
/// Represents the ResourceSet type used by the graphics runtime.
/// </summary>
public abstract class ResourceSet : IDeviceResource, IDisposable {

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceSet" /> type.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    internal ResourceSet(ref ResourceSetDescription description) {
#if VALIDATE_USAGE
        this.Layout = description.Layout;
        this.Resources = description.BoundResources;
#endif
    }

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

#if VALIDATE_USAGE

    /// <summary>
    /// Gets or sets Layout.
    /// </summary>
    internal ResourceLayout Layout { get; }

    /// <summary>
    /// Gets or sets Resources.
    /// </summary>
    internal IBindableResource[] Resources { get; }
#endif
}