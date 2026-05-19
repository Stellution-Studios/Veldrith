namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the BlendHelper class.
/// </summary>
internal static class BlendHelper {

    /// <summary>
    /// Executes the GetOrDefault operation.
    /// </summary>
    /// <param name="mask">Specifies the value of <paramref name="mask" />.</param>
    /// <returns>Returns the result produced by the GetOrDefault operation.</returns>
    public static ColorWriteMask GetOrDefault(this ColorWriteMask? mask) {
        return mask ?? ColorWriteMask.All;
    }
}