using System;

namespace Veldrith;

/// <summary>
/// Represents the VeldridException class.
/// </summary>
public class VeldridException : Exception {

    /// <summary>
    /// Initializes a new instance of the <see cref="VeldridException" /> type.
    /// </summary>
    public VeldridException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VeldridException" /> type.
    /// </summary>
    /// <param name="message">The value of message.</param>
    public VeldridException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VeldridException" /> type.
    /// </summary>
    /// <param name="message">The value of message.</param>
    /// <param name="innerException">The value of innerException.</param>
    public VeldridException(string message, Exception innerException)
        : base(message, innerException) { }
}