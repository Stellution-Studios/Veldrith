namespace Veldrith;

/// <summary>
/// Defines the data layout and behavior of the MappedResourceInfo struct.
/// </summary>
internal struct MappedResourceInfo {

    /// <summary>
    /// Stores the value associated with <c>RefCount</c>.
    /// </summary>
    public int RefCount;

    /// <summary>
    /// Stores the value associated with <c>Mode</c>.
    /// </summary>
    public MapMode Mode;

    /// <summary>
    /// Stores the value associated with <c>MappedResource</c>.
    /// </summary>
    public MappedResource MappedResource;
}