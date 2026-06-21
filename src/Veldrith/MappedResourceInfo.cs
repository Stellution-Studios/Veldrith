namespace Veldrith;

/// <summary>
/// Represents the MappedResourceInfo data structure used by the graphics runtime.
/// </summary>
internal struct MappedResourceInfo {

    /// <summary>
    /// Stores the ref count value used during command execution.
    /// </summary>
    public int RefCount;

    /// <summary>
    /// Stores the mode state used by this instance.
    /// </summary>
    public MapMode Mode;

    /// <summary>
    /// Stores the mapped resource state used by this instance.
    /// </summary>
    public MappedResource MappedResource;
}