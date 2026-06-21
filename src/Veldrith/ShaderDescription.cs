using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="Shader" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct ShaderDescription : IEquatable<ShaderDescription> {

    /// <summary>
    /// The shader stage this instance describes.
    /// </summary>
    public ShaderStages Stage;

    /// <summary>
    /// An array containing the raw shader bytes.
    /// </summary>
    public byte[] ShaderBytes;

    /// <summary>
    /// The name of the entry point function in the shader module to be used in this stage.
    /// </summary>
    public string EntryPoint;

    /// <summary>
    /// Indicates whether the shader should be debuggable. This flag only has an effect if <see cref="ShaderBytes" />
    /// </summary>
    public bool Debug;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShaderDescription" /> type.
    /// </summary>
    /// <param name="stage">The stage value used by this operation.</param>
    /// <param name="shaderBytes">The shader bytes value used by this operation.</param>
    /// <param name="entryPoint">The entry point value used by this operation.</param>
    public ShaderDescription(ShaderStages stage, byte[] shaderBytes, string entryPoint) {
        this.Stage = stage;
        this.ShaderBytes = shaderBytes;
        this.EntryPoint = entryPoint;
        this.Debug = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShaderDescription" /> type.
    /// </summary>
    /// <param name="stage">The stage value used by this operation.</param>
    /// <param name="shaderBytes">The shader bytes value used by this operation.</param>
    /// <param name="entryPoint">The entry point value used by this operation.</param>
    /// <param name="debug">The debug value used by this operation.</param>
    public ShaderDescription(ShaderStages stage, byte[] shaderBytes, string entryPoint, bool debug) {
        this.Stage = stage;
        this.ShaderBytes = shaderBytes;
        this.EntryPoint = entryPoint;
        this.Debug = debug;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(ShaderDescription other) {
        return this.Stage == other.Stage
               && this.ShaderBytes == other.ShaderBytes
               && this.EntryPoint.Equals(other.EntryPoint)
               && this.Debug.Equals(other.Debug);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine((int)this.Stage, this.ShaderBytes.GetHashCode(), this.EntryPoint.GetHashCode(), this.Debug.GetHashCode());
    }
}