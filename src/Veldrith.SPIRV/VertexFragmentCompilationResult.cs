namespace Veldrith.SPIRV;

/// <summary>
/// Represents the VertexFragmentCompilationResult type used by the graphics runtime.
/// </summary>
public class VertexFragmentCompilationResult {

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexFragmentCompilationResult" /> type.
    /// </summary>
    /// <param name="vertexCode">The vertex code value used by this operation.</param>
    /// <param name="fragmentCode">The fragment code value used by this operation.</param>
    /// <param name="reflection">The reflection value used by this operation.</param>
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