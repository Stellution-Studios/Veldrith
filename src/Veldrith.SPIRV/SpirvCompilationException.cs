namespace Veldrith.SPIRV;

/// <summary>
/// Defines the behavior and responsibilities of the SpirvCompilationException class.
/// </summary>
public class SpirvCompilationException : Exception {

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationException" /> type.
    /// </summary>
    public SpirvCompilationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationException" /> type.
    /// </summary>
    /// <param name="message">Specifies the value of <paramref name="message" />.</param>
    public SpirvCompilationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvCompilationException" /> type.
    /// </summary>
    /// <param name="message">Specifies the value of <paramref name="message" />.</param>
    /// <param name="innerException">Specifies the value of <paramref name="innerException" />.</param>
    public SpirvCompilationException(string message, Exception innerException) : base(message, innerException) { }
}