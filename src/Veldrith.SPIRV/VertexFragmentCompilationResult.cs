namespace Veldrith.SPIRV;

/// <summary>
/// Represents the VertexFragmentCompilationResult class.
/// </summary>
public class VertexFragmentCompilationResult {

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexFragmentCompilationResult" /> type.
    /// </summary>
    /// <param name="vertexCode">The value of vertexCode.</param>
    /// <param name="fragmentCode">The value of fragmentCode.</param>
    /// <param name="reflection">The value of reflection.</param>
    internal VertexFragmentCompilationResult(string vertexCode, string fragmentCode, SpirvReflection reflection) {
        this.VertexShader = vertexCode;
        this.FragmentShader = fragmentCode;
        this.Reflection = reflection;
    }

    /// <summary>
    /// The translated vertex shader source code.
    /// </summary>
    public string VertexShader { get; }

    /// <summary>
    /// The translated fragment shader source code.
    /// </summary>
    public string FragmentShader { get; }

    /// <summary>
    /// Information about the resources used in the compiled shaders.
    /// </summary>
    public SpirvReflection Reflection { get; }
}