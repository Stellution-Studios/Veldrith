using System;

namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the VeldridException class.
/// </summary>
public class VeldridException : Exception {

    /// <summary>
    /// Initializes a new instance of the <see cref="VeldridException" /> type.
    /// </summary>
    public VeldridException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VeldridException" /> type.
    /// </summary>
    /// <param name="message">Specifies the value of <paramref name="message" />.</param>
    public VeldridException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VeldridException" /> type.
    /// </summary>
    /// <param name="message">Specifies the value of <paramref name="message" />.</param>
    /// <param name="innerException">Specifies the value of <paramref name="innerException" />.</param>
    public VeldridException(string message, Exception innerException)
        : base(message, innerException) { }
}