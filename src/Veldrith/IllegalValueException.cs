using System;

namespace Veldrith;

internal static class Illegal {
    internal static Exception Value<T>() {
        return new IllegalValueException<T>();
    }

    // ReSharper disable once UnusedTypeParameter
    internal class IllegalValueException<T> : VeldridException { }
}