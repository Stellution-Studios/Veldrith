using System;

namespace Veldrith;

/// <summary>
/// Represents the Illegal type used by the graphics runtime.
/// </summary>
internal static class Illegal {

    /// <summary>
    /// Creates an exception indicating that a value of type <typeparamref name="T" /> is not valid in the current context.
    /// </summary>
    internal static Exception Value<T>() {
        return new IllegalValueException<T>();
    }

    // ReSharper disable once UnusedTypeParameter

    /// <summary>
    /// Represents the IllegalValueException type used by the graphics runtime.
    /// </summary>
    internal class IllegalValueException<T> : VeldridException { }
}