namespace Veldrith.SPIRV;

public class ComputeCompilationResult {

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeCompilationResult" /> class.
    /// </summary>
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