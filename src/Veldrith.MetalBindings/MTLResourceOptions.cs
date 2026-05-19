namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLResourceOptions enumeration.
/// </summary>
public enum MTLResourceOptions : ulong {

    /// <summary>
    /// Stores the value associated with <c>CPUCacheModeDefaultCache</c>.
    /// </summary>
    CPUCacheModeDefaultCache = MTLCPUCacheMode.DefaultCache, CPUCacheModeWriteCombined = MTLCPUCacheMode.WriteCombined,

    /// <summary>
    /// Stores the value associated with <c>StorageModeShared</c>.
    /// </summary>
    StorageModeShared = MTLStorageMode.Shared << 4, StorageModeManaged = MTLStorageMode.Managed << 4, StorageModePrivate = MTLStorageMode.Private << 4, StorageModeMemoryless = MTLStorageMode.Memoryless << 4,

    /// <summary>
    /// Stores the value associated with <c>HazardTrackingModeUntracked</c>.
    /// </summary>
    HazardTrackingModeUntracked = (uint)(0x1UL << 8)
}