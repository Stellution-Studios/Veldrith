using System;

namespace Veldrith;

/// <summary>
/// Represents the Shader class.
/// </summary>
public abstract class Shader : IDeviceResource, IDisposable {

    /// <summary>
    /// Initializes a new instance of the <see cref="Shader" /> type.
    /// </summary>
    /// <param name="stage">The value of stage.</param>
    /// <param name="entryPoint">The value of entryPoint.</param>
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
    /// Performs the Dispose operation.
    /// </summary>
    public abstract void Dispose();

    #endregion
}