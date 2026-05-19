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
    /// For Direct3D12 shaders, this array must contain HLSL bytecode or HLSL text.
    /// For Vulkan shaders, this array must contain SPIR-V bytecode.
    /// For Metal shaders, this array must contain Metal bitcode (a "metallib" file), or UTF8-encoded Metal shading
    /// language
    /// text.
    /// </summary>
    public byte[] ShaderBytes;

    /// <summary>
    /// The name of the entry point function in the shader module to be used in this stage.
    /// </summary>
    public string EntryPoint;

    /// <summary>
    /// Indicates whether the shader should be debuggable. This flag only has an effect if <see cref="ShaderBytes" />
    /// contains
    /// shader code that will be compiled.
    /// </summary>
    public bool Debug;

    /// <summary>
    /// Constructs a new ShaderDescription.
    /// </summary>
    /// <param name="stage">The shader stage to create.</param>
    /// <param name="shaderBytes">An array containing the raw shader bytes.</param>
    /// <param name="entryPoint">The name of the entry point function in the shader module to be used in this stage.</param>
    public ShaderDescription(ShaderStages stage, byte[] shaderBytes, string entryPoint) {
        this.Stage = stage;
        this.ShaderBytes = shaderBytes;
        this.EntryPoint = entryPoint;
        this.Debug = false;
    }

    /// <summary>
    /// Constructs a new ShaderDescription.
    /// </summary>
    /// <param name="stage">The shader stage to create.</param>
    /// <param name="shaderBytes">An array containing the raw shader bytes.</param>
    /// <param name="entryPoint">The name of the entry point function in the shader module to be used in this stage.</param>
    /// <param name="debug">
    /// Indicates whether the shader should be debuggable. This flag only has an effect if
    /// <paramref name="shaderBytes" /> contains shader code that will be compiled.
    /// </param>
    public ShaderDescription(ShaderStages stage, byte[] shaderBytes, string entryPoint, bool debug) {
        this.Stage = stage;
        this.ShaderBytes = shaderBytes;
        this.EntryPoint = entryPoint;
        this.Debug = debug;
    }

    /// <summary>
    /// Element-wise equality.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>True if all elements and if array instances are equal; false otherswise.</returns>
    public bool Equals(ShaderDescription other) {
        return this.Stage == other.Stage
               && this.ShaderBytes == other.ShaderBytes
               && this.EntryPoint.Equals(other.EntryPoint)
               && this.Debug.Equals(other.Debug);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine((int)this.Stage, this.ShaderBytes.GetHashCode(), this.EntryPoint.GetHashCode(), this.Debug.GetHashCode());
    }
}
