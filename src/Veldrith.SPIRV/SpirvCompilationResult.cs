namespace Veldrith.SPIRV;

/// <summary>
/// Represents the SpirvCompilationResult class.
/// </summary>
public class SpirvCompilationResult {

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationResult" /> type.
    /// </summary>
    /// <param name="spirvBytes">The value of spirvBytes.</param>
    public SpirvCompilationResult(byte[] spirvBytes) {
        this.SpirvBytes = spirvBytes;
    }

    /// <summary>
    /// The compiled SPIR-V bytecode.
    /// </summary>
    public byte[] SpirvBytes { get; }
}