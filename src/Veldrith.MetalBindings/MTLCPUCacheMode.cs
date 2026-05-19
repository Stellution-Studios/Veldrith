namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLCPUCacheMode enumeration.
/// </summary>
public enum MTLCPUCacheMode {

    /// <summary>
    /// Caches default cache to reduce repeated allocations and lookups.
    /// </summary>
    DefaultCache = 0, WriteCombined = 1
}