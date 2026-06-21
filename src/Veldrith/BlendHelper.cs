namespace Veldrith;

/// <summary>
/// Represents the BlendHelper type used by the graphics runtime.
/// </summary>
internal static class BlendHelper {

    /// <summary>
    /// Gets the or default value.
    /// </summary>
    /// <param name="mask">The mask value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static ColorWriteMask GetOrDefault(this ColorWriteMask? mask) {
        return mask ?? ColorWriteMask.All;
    }
}