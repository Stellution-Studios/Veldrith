namespace Veldrith.SPIRV;

/// <summary>
/// Provides SPIR-V compilation support for SpirvCompilationException.
/// </summary>
public class SpirvCompilationException : Exception {

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationException" /> type.
    /// </summary>
    public SpirvCompilationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationException" /> type.
    /// </summary>
    /// <param name="message">The message value used by this operation.</param>
    public SpirvCompilationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationException" /> type.
    /// </summary>
    /// <param name="message">The message value used by this operation.</param>
    /// <param name="innerException">The inner exception value used by this operation.</param>
    public SpirvCompilationException(string message, Exception innerException) : base(message, innerException) { }
}