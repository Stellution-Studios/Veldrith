namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLResourceOptions enumeration.
/// </summary>
public enum MTLResourceOptions : ulong {

    /// <summary>
    /// Caches cpucache mode default cache to reduce repeated allocations and lookups.
    /// </summary>
    CPUCacheModeDefaultCache = MTLCPUCacheMode.DefaultCache,
    CPUCacheModeWriteCombined = MTLCPUCacheMode.WriteCombined,

    /// <summary>
    /// Stores the storage mode shared state used by this instance.
    /// </summary>
    StorageModeShared = MTLStorageMode.Shared << 4,
    StorageModeManaged = MTLStorageMode.Managed << 4,
    StorageModePrivate = MTLStorageMode.Private << 4,
    StorageModeMemoryless = MTLStorageMode.Memoryless << 4,
    
    /// <summary>
    /// Executes the value logic for this backend.
    /// </summary>
    HazardTrackingModeUntracked = (uint)(0x1UL << 8)
}