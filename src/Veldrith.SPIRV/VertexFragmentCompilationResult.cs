namespace Veldrith.SPIRV;

/// <summary>
/// Represents the VertexFragmentCompilationResult class.
/// </summary>
public class VertexFragmentCompilationResult {

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexFragmentCompilationResult" /> class.
    /// </summary>
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