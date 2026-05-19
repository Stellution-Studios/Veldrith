namespace Veldrith;

/// <summary>
/// Represents the HashHelper class.
/// </summary>
internal static class HashHelper {

    /// <summary>
    /// Performs the Combine operation.
    /// </summary>
    /// <param name="value1">The value of value1.</param>
    /// <param name="value2">The value of value2.</param>
    /// <returns>The result of the Combine operation.</returns>
    public static int Combine(int value1, int value2) {
        uint rol5 = ((uint)value1 << 5) | ((uint)value1 >> 27);
        return ((int)rol5 + value1) ^ value2;
    }

    /// <summary>
    /// Performs the Combine operation.
    /// </summary>
    /// <param name="value1">The value of value1.</param>
    /// <param name="value2">The value of value2.</param>
    /// <param name="value3">The value of value3.</param>
    /// <returns>The result of the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3) {
        return Combine(value1, Combine(value2, value3));
    }

    /// <summary>
    /// Performs the Combine operation.
    /// </summary>
    /// <param name="value1">The value of value1.</param>
    /// <param name="value2">The value of value2.</param>
    /// <param name="value3">The value of value3.</param>
    /// <param name="value4">The value of value4.</param>
    /// <returns>The result of the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4) {
        return Combine(value1, Combine(value2, Combine(value3, value4)));
    }

    /// <summary>
    /// Performs the Combine operation.
    /// </summary>
    /// <param name="value1">The value of value1.</param>
    /// <param name="value2">The value of value2.</param>
    /// <param name="value3">The value of value3.</param>
    /// <param name="value4">The value of value4.</param>
    /// <param name="value5">The value of value5.</param>
    /// <returns>The result of the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, value5))));
    }

    /// <summary>
    /// Performs the Combine operation.
    /// </summary>
    /// <param name="value1">The value of value1.</param>
    /// <param name="value2">The value of value2.</param>
    /// <param name="value3">The value of value3.</param>
    /// <param name="value4">The value of value4.</param>
    /// <param name="value5">The value of value5.</param>
    /// <param name="value6">The value of value6.</param>
    /// <returns>The result of the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, value6)))));
    }

    /// <summary>
    /// Performs the Combine operation.
    /// </summary>
    /// <param name="value1">The value of value1.</param>
    /// <param name="value2">The value of value2.</param>
    /// <param name="value3">The value of value3.</param>
    /// <param name="value4">The value of value4.</param>
    /// <param name="value5">The value of value5.</param>
    /// <param name="value6">The value of value6.</param>
    /// <param name="value7">The value of value7.</param>
    /// <returns>The result of the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, value7))))));
    }

    /// <summary>
    /// Performs the Combine operation.
    /// </summary>
    /// <param name="value1">The value of value1.</param>
    /// <param name="value2">The value of value2.</param>
    /// <param name="value3">The value of value3.</param>
    /// <param name="value4">The value of value4.</param>
    /// <param name="value5">The value of value5.</param>
    /// <param name="value6">The value of value6.</param>
    /// <param name="value7">The value of value7.</param>
    /// <param name="value8">The value of value8.</param>
    /// <returns>The result of the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7, int value8) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, Combine(value7, value8)))))));
    }

    /// <summary>
    /// Performs the Combine operation.
    /// </summary>
    /// <param name="value1">The value of value1.</param>
    /// <param name="value2">The value of value2.</param>
    /// <param name="value3">The value of value3.</param>
    /// <param name="value4">The value of value4.</param>
    /// <param name="value5">The value of value5.</param>
    /// <param name="value6">The value of value6.</param>
    /// <param name="value7">The value of value7.</param>
    /// <param name="value8">The value of value8.</param>
    /// <param name="value9">The value of value9.</param>
    /// <returns>The result of the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7, int value8, int value9) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, Combine(value7, Combine(value8, value9))))))));
    }

    /// <summary>
    /// Performs the Combine operation.
    /// </summary>
    /// <param name="value1">The value of value1.</param>
    /// <param name="value2">The value of value2.</param>
    /// <param name="value3">The value of value3.</param>
    /// <param name="value4">The value of value4.</param>
    /// <param name="value5">The value of value5.</param>
    /// <param name="value6">The value of value6.</param>
    /// <param name="value7">The value of value7.</param>
    /// <param name="value8">The value of value8.</param>
    /// <param name="value9">The value of value9.</param>
    /// <param name="value10">The value of value10.</param>
    /// <returns>The result of the Combine operation.</returns>
    public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7, int value8, int value9, int value10) {
        return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, Combine(value7, Combine(value8, Combine(value9, value10)))))))));
    }

    /// <summary>
    /// Combines the hash codes of all elements in an array into a single hash value.
    /// </summary>
    /// <param name="items">The array whose elements should be hashed.</param>
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