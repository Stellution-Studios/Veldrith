namespace Veldrith.SPIRV;

/// <summary>
/// Defines the behavior and responsibilities of the VertexFragmentCompilationResult class.
/// </summary>
public class VertexFragmentCompilationResult {

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexFragmentCompilationResult" /> type.
    /// </summary>
    /// <param name="vertexCode">Specifies the value of <paramref name="vertexCode" />.</param>
    /// <param name="fragmentCode">Specifies the value of <paramref name="fragmentCode" />.</param>
    /// <param name="reflection">Specifies the value of <paramref name="reflection" />.</param>
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