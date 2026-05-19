namespace Veldrith.SPIRV;

/// <summary>
/// Defines the behavior and responsibilities of the SpirvCompilationResult class.
/// </summary>
public class SpirvCompilationResult {

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationResult" /> type.
    /// </summary>
    /// <param name="spirvBytes">Specifies the value of <paramref name="spirvBytes" />.</param>
    public SpirvCompilationResult(byte[] spirvBytes) {
        this.SpirvBytes = spirvBytes;
    }

    /// <summary>
    /// The compiled SPIR-V bytecode.
    /// </summary>
    public byte[] SpirvBytes { get; }
}