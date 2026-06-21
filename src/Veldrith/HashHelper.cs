namespace Veldrith;

/// <summary>
/// Represents the HashHelper type used by the graphics runtime.
/// </summary>
internal static class HashHelper {

    /// <summary>
    /// Executes the combine logic for this backend.
    /// </summary>
    /// <param name="value1">The value1 value used by this operation.</param>
    /// <param name="value2">The value2 value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static int Combine(int value1, int value2) {
        uint rol5 = ((uint)value1 << 5) | ((uint)value1 >> 27);
        return ((int)rol5 + value1) ^ value2;
    }

    /// <summary>
    /// Executes the combine logic for this backend.
    /// </summary>
    /// <param name="value1">The value1 value used by this operation.</param>
    /// <param name="value2">The value2 value used by this operation.</param>
    /// <param name="value3">The value3 value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static int Combine(int value1, int value2, int value3) {
        return Combine(value1, Combine(value2, value3));
    }

    /// <summary>
    /// Executes the combine logic for this backend.
    /// </summary>
    /// <param name="value1">The value1 value used by this operation.</param>
    /// <param name="value2">The value2 value used by this operation.</param>
    /// <param name="value3">The value3 value used by this operation.</param>
    /// <param name="value4">The value4 value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4) {
        return Combine(value1, Combine(value2, Combine(value3, value4)));
    }

    /// <summary>
    /// Executes the combine logic for this backend.
    /// </summary>
    /// <param name="value1">The value1 value used by this operation.</param>
    /// <param name="value2">The value2 value used by this operation.</param>
    /// <param name="value3">The value3 value used by this operation.</param>
    /// <param name="value4">The value4 value used by this operation.</param>
    /// <param name="value5">The value5 value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, value5))));
    }

    /// <summary>
    /// Executes the combine logic for this backend.
    /// </summary>
    /// <param name="value1">The value1 value used by this operation.</param>
    /// <param name="value2">The value2 value used by this operation.</param>
    /// <param name="value3">The value3 value used by this operation.</param>
    /// <param name="value4">The value4 value used by this operation.</param>
    /// <param name="value5">The value5 value used by this operation.</param>
    /// <param name="value6">The value6 value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, value6)))));
    }

    /// <summary>
    /// Executes the combine logic for this backend.
    /// </summary>
    /// <param name="value1">The value1 value used by this operation.</param>
    /// <param name="value2">The value2 value used by this operation.</param>
    /// <param name="value3">The value3 value used by this operation.</param>
    /// <param name="value4">The value4 value used by this operation.</param>
    /// <param name="value5">The value5 value used by this operation.</param>
    /// <param name="value6">The value6 value used by this operation.</param>
    /// <param name="value7">The value7 value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, value7))))));
    }

    /// <summary>
    /// Executes the combine logic for this backend.
    /// </summary>
    /// <param name="value1">The value1 value used by this operation.</param>
    /// <param name="value2">The value2 value used by this operation.</param>
    /// <param name="value3">The value3 value used by this operation.</param>
    /// <param name="value4">The value4 value used by this operation.</param>
    /// <param name="value5">The value5 value used by this operation.</param>
    /// <param name="value6">The value6 value used by this operation.</param>
    /// <param name="value7">The value7 value used by this operation.</param>
    /// <param name="value8">The value8 value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7, int value8) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, Combine(value7, value8)))))));
    }

    /// <summary>
    /// Executes the combine logic for this backend.
    /// </summary>
    /// <param name="value1">The value1 value used by this operation.</param>
    /// <param name="value2">The value2 value used by this operation.</param>
    /// <param name="value3">The value3 value used by this operation.</param>
    /// <param name="value4">The value4 value used by this operation.</param>
    /// <param name="value5">The value5 value used by this operation.</param>
    /// <param name="value6">The value6 value used by this operation.</param>
    /// <param name="value7">The value7 value used by this operation.</param>
    /// <param name="value8">The value8 value used by this operation.</param>
    /// <param name="value9">The value9 value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7, int value8, int value9) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, Combine(value7, Combine(value8, value9))))))));
    }

    /// <summary>
    /// Executes the combine logic for this backend.
    /// </summary>
    /// <param name="value1">The value1 value used by this operation.</param>
    /// <param name="value2">The value2 value used by this operation.</param>
    /// <param name="value3">The value3 value used by this operation.</param>
    /// <param name="value4">The value4 value used by this operation.</param>
    /// <param name="value5">The value5 value used by this operation.</param>
    /// <param name="value6">The value6 value used by this operation.</param>
    /// <param name="value7">The value7 value used by this operation.</param>
    /// <param name="value8">The value8 value used by this operation.</param>
    /// <param name="value9">The value9 value used by this operation.</param>
    /// <param name="value10">The value10 value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7, int value8, int value9, int value10) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, Combine(value7, Combine(value8, Combine(value9, value10)))))))));
    }

    /// <summary>
    /// Combines the hash codes of all elements in an array into a single hash value.
    /// </summary>
    /// <param name="items">The items value used by this operation.</param>
    public static int Array<T>(T[] items) {
        if (items == null || items.Length == 0) {
            return 0;
        }

        int hash = items[0].GetHashCode();
        for (int i = 1; i < items.Length; i++) {
            hash = Combine(hash, items[i]?.GetHashCode() ?? i);
        }

        return hash;
    }
}