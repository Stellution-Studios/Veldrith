namespace Veldrith.SPIRV;

/// <summary>
/// Represents the SpirvCompilationException class.
/// </summary>
public class SpirvCompilationException : Exception {

    /// <summary>
    /// Constructs a new <see cref="SpirvCompilationException" />.
    /// </summary>
    public SpirvCompilationException() { }

    /// <summary>
    /// Constructs a new <see cref="SpirvCompilationException" /> with the given message.
    /// </summary>
    public SpirvCompilationException(string message) : base(message) { }

    /// <summary>
    /// Constructs a new <see cref="SpirvCompilationException" /> with the given message and inner exception.
    /// </summary>
    public SpirvCompilationException(string message, Exception innerException) : base(message, innerException) { }
}