namespace Veldrith;

/// <summary>
/// Represents the BlendHelper class.
/// </summary>
internal static class BlendHelper {

    /// <summary>
    /// Performs the GetOrDefault operation.
    /// </summary>
    /// <param name="mask">The value of mask.</param>
    /// <returns>The result of the GetOrDefault operation.</returns>
    public static ColorWriteMask GetOrDefault(this ColorWriteMask? mask) {
        return mask ?? ColorWriteMask.All;
    }
}