namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the HashHelper class.
/// </summary>
internal static class HashHelper {

    /// <summary>
    /// Executes the Combine operation.
    /// </summary>
    /// <param name="value1">Specifies the value of <paramref name="value1" />.</param>
    /// <param name="value2">Specifies the value of <paramref name="value2" />.</param>
    /// <returns>Returns the result produced by the Combine operation.</returns>
    public static int Combine(int value1, int value2) {
        uint rol5 = ((uint)value1 << 5) | ((uint)value1 >> 27);
        return ((int)rol5 + value1) ^ value2;
    }

    /// <summary>
    /// Executes the Combine operation.
    /// </summary>
    /// <param name="value1">Specifies the value of <paramref name="value1" />.</param>
    /// <param name="value2">Specifies the value of <paramref name="value2" />.</param>
    /// <param name="value3">Specifies the value of <paramref name="value3" />.</param>
    /// <returns>Returns the result produced by the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3) {
        return Combine(value1, Combine(value2, value3));
    }

    /// <summary>
    /// Executes the Combine operation.
    /// </summary>
    /// <param name="value1">Specifies the value of <paramref name="value1" />.</param>
    /// <param name="value2">Specifies the value of <paramref name="value2" />.</param>
    /// <param name="value3">Specifies the value of <paramref name="value3" />.</param>
    /// <param name="value4">Specifies the value of <paramref name="value4" />.</param>
    /// <returns>Returns the result produced by the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4) {
        return Combine(value1, Combine(value2, Combine(value3, value4)));
    }

    /// <summary>
    /// Executes the Combine operation.
    /// </summary>
    /// <param name="value1">Specifies the value of <paramref name="value1" />.</param>
    /// <param name="value2">Specifies the value of <paramref name="value2" />.</param>
    /// <param name="value3">Specifies the value of <paramref name="value3" />.</param>
    /// <param name="value4">Specifies the value of <paramref name="value4" />.</param>
    /// <param name="value5">Specifies the value of <paramref name="value5" />.</param>
    /// <returns>Returns the result produced by the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, value5))));
    }

    /// <summary>
    /// Executes the Combine operation.
    /// </summary>
    /// <param name="value1">Specifies the value of <paramref name="value1" />.</param>
    /// <param name="value2">Specifies the value of <paramref name="value2" />.</param>
    /// <param name="value3">Specifies the value of <paramref name="value3" />.</param>
    /// <param name="value4">Specifies the value of <paramref name="value4" />.</param>
    /// <param name="value5">Specifies the value of <paramref name="value5" />.</param>
    /// <param name="value6">Specifies the value of <paramref name="value6" />.</param>
    /// <returns>Returns the result produced by the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, value6)))));
    }

    /// <summary>
    /// Executes the Combine operation.
    /// </summary>
    /// <param name="value1">Specifies the value of <paramref name="value1" />.</param>
    /// <param name="value2">Specifies the value of <paramref name="value2" />.</param>
    /// <param name="value3">Specifies the value of <paramref name="value3" />.</param>
    /// <param name="value4">Specifies the value of <paramref name="value4" />.</param>
    /// <param name="value5">Specifies the value of <paramref name="value5" />.</param>
    /// <param name="value6">Specifies the value of <paramref name="value6" />.</param>
    /// <param name="value7">Specifies the value of <paramref name="value7" />.</param>
    /// <returns>Returns the result produced by the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, value7))))));
    }

    /// <summary>
    /// Executes the Combine operation.
    /// </summary>
    /// <param name="value1">Specifies the value of <paramref name="value1" />.</param>
    /// <param name="value2">Specifies the value of <paramref name="value2" />.</param>
    /// <param name="value3">Specifies the value of <paramref name="value3" />.</param>
    /// <param name="value4">Specifies the value of <paramref name="value4" />.</param>
    /// <param name="value5">Specifies the value of <paramref name="value5" />.</param>
    /// <param name="value6">Specifies the value of <paramref name="value6" />.</param>
    /// <param name="value7">Specifies the value of <paramref name="value7" />.</param>
    /// <param name="value8">Specifies the value of <paramref name="value8" />.</param>
    /// <returns>Returns the result produced by the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7, int value8) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, Combine(value7, value8)))))));
    }

    /// <summary>
    /// Executes the Combine operation.
    /// </summary>
    /// <param name="value1">Specifies the value of <paramref name="value1" />.</param>
    /// <param name="value2">Specifies the value of <paramref name="value2" />.</param>
    /// <param name="value3">Specifies the value of <paramref name="value3" />.</param>
    /// <param name="value4">Specifies the value of <paramref name="value4" />.</param>
    /// <param name="value5">Specifies the value of <paramref name="value5" />.</param>
    /// <param name="value6">Specifies the value of <paramref name="value6" />.</param>
    /// <param name="value7">Specifies the value of <paramref name="value7" />.</param>
    /// <param name="value8">Specifies the value of <paramref name="value8" />.</param>
    /// <param name="value9">Specifies the value of <paramref name="value9" />.</param>
    /// <returns>Returns the result produced by the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7, int value8, int value9) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, Combine(value7, Combine(value8, value9))))))));
    }

    /// <summary>
    /// Executes the Combine operation.
    /// </summary>
    /// <param name="value1">Specifies the value of <paramref name="value1" />.</param>
    /// <param name="value2">Specifies the value of <paramref name="value2" />.</param>
    /// <param name="value3">Specifies the value of <paramref name="value3" />.</param>
    /// <param name="value4">Specifies the value of <paramref name="value4" />.</param>
    /// <param name="value5">Specifies the value of <paramref name="value5" />.</param>
    /// <param name="value6">Specifies the value of <paramref name="value6" />.</param>
    /// <param name="value7">Specifies the value of <paramref name="value7" />.</param>
    /// <param name="value8">Specifies the value of <paramref name="value8" />.</param>
    /// <param name="value9">Specifies the value of <paramref name="value9" />.</param>
    /// <param name="value10">Specifies the value of <paramref name="value10" />.</param>
    /// <returns>Returns the result produced by the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7, int value8, int value9, int value10) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, Combine(value7, Combine(value8, Combine(value9, value10)))))))));
    }

    /// <summary>
    /// Combines the hash codes of all elements in an array into a single hash value.
    /// </summary>
    /// <param name="items">Specifies the value of <paramref name="items" />.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A combined hash code for <paramref name="items" />, or <c>0</c> if the array is null or empty.</returns>
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