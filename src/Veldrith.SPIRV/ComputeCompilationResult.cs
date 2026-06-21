namespace Veldrith.SPIRV;

/// <summary>
/// Represents the ComputeCompilationResult type used by the graphics runtime.
/// </summary>
public class ComputeCompilationResult {

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeCompilationResult" /> type.
    /// </summary>
    /// <param name="computeCode">The compute code value used by this operation.</param>
    /// <param name="reflection">The reflection value used by this operation.</param>
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