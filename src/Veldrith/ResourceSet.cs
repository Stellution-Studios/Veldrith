using System;

namespace Veldrith;

/// <summary>
///     A device resource used to bind a particular set of <see cref="IBindableResource" /> objects to a
///     <see cref="CommandList" />.
///     See <see cref="ResourceSetDescription" />.
/// </summary>
public abstract class ResourceSet : IDeviceResource, IDisposable {
    internal ResourceSet(ref ResourceSetDescription description) {
#if VALIDATE_USAGE
        this.Layout = description.Layout;
        this.Resources = description.BoundResources;
#endif
    }

    /// <summary>
    ///     A bool indicating whether this instance has been disposed.
    /// </summary>
    public abstract bool IsDisposed { get; }

    /// <summary>
    ///     A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
    ///     tools.
    /// </summary>
    public abstract string Name { get; set; }

    #region Disposal

    /// <summary>
    ///     Frees unmanaged device resources controlled by this instance.
    /// </summary>
    public abstract void Dispose();

    #endregion

#if VALIDATE_USAGE
    internal ResourceLayout Layout { get; }
    internal IBindableResource[] Resources { get; }
#endif
}