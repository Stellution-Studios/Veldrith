using System;

namespace Veldrith;

/// <summary>
/// Represents the Illegal class.
/// </summary>
internal static class Illegal {
    internal static Exception Value<T>() {
        return new IllegalValueException<T>();
    }

    // ReSharper disable once UnusedTypeParameter

    /// <summary>
    /// Represents the IllegalValueException class.
    /// </summary>
    internal class IllegalValueException<T> : VeldridException { }
}