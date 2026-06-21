using System;

namespace Veldrith;

/// <summary>
/// Represents the ResourceLayout type used by the graphics runtime.
/// </summary>
public abstract class ResourceLayout : IDeviceResource, IDisposable {

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLayout" /> type.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    internal ResourceLayout(ref ResourceLayoutDescription description) {
        foreach (ResourceLayoutElementDescription element in description.Elements) {
            if ((element.Options & ResourceLayoutElementOptions.DynamicBinding) != 0) {
                this.DynamicBufferCount += 1;
            }
        }
#if VALIDATE_USAGE
        this.Description = description;
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

    /// <summary>
    /// Stores the dynamic buffer count value used during command execution.
    /// </summary>
    internal readonly uint DynamicBufferCount;

#if VALIDATE_USAGE

    /// <summary>
    /// Stores the description state used by this instance.
    /// </summary>
    internal readonly ResourceLayoutDescription Description;
#endif
}
