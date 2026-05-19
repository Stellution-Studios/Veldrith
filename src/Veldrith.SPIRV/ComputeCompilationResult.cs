namespace Veldrith.SPIRV;

/// <summary>
/// Defines the behavior and responsibilities of the ComputeCompilationResult class.
/// </summary>
public class ComputeCompilationResult {

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeCompilationResult" /> type.
    /// </summary>
    /// <param name="computeCode">Specifies the value of <paramref name="computeCode" />.</param>
    /// <param name="reflection">Specifies the value of <paramref name="reflection" />.</param>
    internal ComputeCompilationResult(string computeCode, SpirvReflection reflection) {
        this.ComputeShader = computeCode;
        this.Reflection = reflection;
    }

    /// <summary>
    /// The translated shader source code.
    /// </summary>
    public string ComputeShader { get; }

    /// <summary>
    /// Information about the resources used in the compiled shader.
    /// </summary>
    public SpirvReflection Reflection { get; }
}