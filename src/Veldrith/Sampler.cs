using System;

namespace Veldrith
{
    /// <summary>
    ///     A bindable device resource which controls how texture values are sampled within a shader.
    ///     See <see cref="SamplerDescription" />.
    /// </summary>
    public abstract class Sampler : IDeviceResource, IBindableResource, IDisposable
    {
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
    }
}
