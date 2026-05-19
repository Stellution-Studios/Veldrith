namespace Veldrith.SPIRV;

/// <summary>
/// Represents the SpirvCompilationResult class.
/// </summary>
public class SpirvCompilationResult {

    /// <summary>
    /// Constructs a new <see cref="SpirvCompilationResult" />.
    /// </summary>
    public SpirvCompilationResult(byte[] spirvBytes) {
        this.SpirvBytes = spirvBytes;
    }

    /// <summary>
    /// The compiled SPIR-V bytecode.
    /// </summary>
    public byte[] SpirvBytes { get; }
}