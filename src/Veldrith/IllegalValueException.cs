using System;

namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the Illegal class.
/// </summary>
internal static class Illegal {

    /// <summary>
    /// Creates an exception indicating that a value of type <typeparamref name="T" /> is not valid in the current context.
    /// </summary>
    /// <typeparam name="T">The enum or value type that contains the invalid value.</typeparam>
    /// <returns>An exception that can be thrown for invalid values.</returns>
    internal static Exception Value<T>() {
        return new IllegalValueException<T>();
    }

    // ReSharper disable once UnusedTypeParameter

    /// <summary>
    /// Defines the behavior and responsibilities of the IllegalValueException class.
    /// </summary>
    internal class IllegalValueException<T> : VeldridException { }
}