using System;

namespace Veldrith;

/// <summary>
/// Represents the ResourceSet class.
/// </summary>
public abstract class ResourceSet : IDeviceResource, IDisposable {

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceSet" /> type.
    /// </summary>
    /// <param name="description">The value of description.</param>
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
    /// tools.
    /// </summary>
    public abstract string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Performs the Dispose operation.
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