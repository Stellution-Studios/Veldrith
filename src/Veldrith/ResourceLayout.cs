using System;

namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the ResourceLayout class.
/// </summary>
public abstract class ResourceLayout : IDeviceResource, IDisposable {

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLayout" /> type.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
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
    /// Executes the Dispose operation.
    /// </summary>
    public abstract void Dispose();

    #endregion

#if VALIDATE_USAGE

    /// <summary>
    /// Stores the value associated with <c>Description</c>.
    /// </summary>
    internal readonly ResourceLayoutDescription Description;

    /// <summary>
    /// Stores the value associated with <c>DynamicBufferCount</c>.
    /// </summary>
    internal readonly uint DynamicBufferCount;
#endif
}