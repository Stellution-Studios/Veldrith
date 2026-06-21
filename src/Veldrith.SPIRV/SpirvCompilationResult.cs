namespace Veldrith.SPIRV;

/// <summary>
/// Provides SPIR-V compilation support for SpirvCompilationResult.
/// </summary>
public class SpirvCompilationResult {

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationResult" /> type.
    /// </summary>
    /// <param name="spirvBytes">The spirv bytes value used by this operation.</param>
    public SpirvCompilationResult(byte[] spirvBytes) {
        this.SpirvBytes = spirvBytes;
    }

    /// <summary>
    /// The compiled SPIR-V bytecode.
    /// </summary>
    public byte[] SpirvBytes { get; }
}