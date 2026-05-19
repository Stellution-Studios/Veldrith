using System;

namespace Veldrith;

/// <summary>
///     A device resource which describes the layout and kind of <see cref="IBindableResource" /> objects available
///     to a shader set.
///     See <see cref="ResourceLayoutDescription" />.
/// </summary>
public abstract class ResourceLayout : IDeviceResource, IDisposable {
    internal ResourceLayout(ref ResourceLayoutDescription description) {
#if VALIDATE_USAGE
        this.Description = description;

        foreach (ResourceLayoutElementDescription element in description.Elements) {
            if ((element.Options & ResourceLayoutElementOptions.DynamicBinding) != 0) {
                this.DynamicBufferCount += 1;
            }
        }
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
    internal readonly ResourceLayoutDescription Description;
    internal readonly uint DynamicBufferCount;
#endif
}