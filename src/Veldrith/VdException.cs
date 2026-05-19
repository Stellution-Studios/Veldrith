using System;

namespace Veldrith;

/// <summary>
/// Represents the VeldridException type used by the graphics runtime.
/// </summary>
public class VeldridException : Exception {

    /// <summary>
    /// Initializes a new instance of the <see cref="VeldridException" /> type.
    /// </summary>
    public VeldridException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VeldridException" /> type.
    /// </summary>
    /// <param name="message">The message value used by this operation.</param>
    public VeldridException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VeldridException" /> type.
    /// </summary>
    /// <param name="message">The message value used by this operation.</param>
    /// <param name="innerException">The inner exception value used by this operation.</param>
    public VeldridException(string message, Exception innerException)
        : base(message, innerException) { }
}