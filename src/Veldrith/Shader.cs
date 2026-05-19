using System;

namespace Veldrith;

public abstract class Shader : IDeviceResource, IDisposable {

    /// <summary>
    /// Initializes a new instance of the <see cref="Shader" /> class.
    /// </summary>
    internal Shader(ShaderStages stage, string entryPoint) {
        this.Stage = stage;
        this.EntryPoint = entryPoint;
    }

    /// <summary>
    /// The shader stage this instance can be used in.
    /// </summary>
    public ShaderStages Stage { get; }

    /// <summary>
    /// The name of the entry point function.
    /// </summary>
    public string EntryPoint { get; }

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