namespace Veldrith.SPIRV;

/// <summary>
/// Represents the SpirvCompilationException class.
/// </summary>
public class SpirvCompilationException : Exception {

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationException" /> type.
    /// </summary>
    public SpirvCompilationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationException" /> type.
    /// </summary>
    /// <param name="message">The value of message.</param>
    public SpirvCompilationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationException" /> type.
    /// </summary>
    /// <param name="message">The value of message.</param>
    /// <param name="innerException">The value of innerException.</param>
    public SpirvCompilationException(string message, Exception innerException) : base(message, innerException) { }
}