namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLResourceOptions enum.
/// </summary>
public enum MTLResourceOptions : ulong {

    /// <summary>
    /// Represents the CPUCacheModeDefaultCache field.
    /// </summary>
    CPUCacheModeDefaultCache = MTLCPUCacheMode.DefaultCache, CPUCacheModeWriteCombined = MTLCPUCacheMode.WriteCombined,

    /// <summary>
    /// Represents the StorageModeShared field.
    /// </summary>
    StorageModeShared = MTLStorageMode.Shared << 4, StorageModeManaged = MTLStorageMode.Managed << 4, StorageModePrivate = MTLStorageMode.Private << 4, StorageModeMemoryless = MTLStorageMode.Memoryless << 4,

    /// <summary>
    /// Represents the HazardTrackingModeUntracked field.
    /// </summary>
    HazardTrackingModeUntracked = (uint)(0x1UL << 8)
}