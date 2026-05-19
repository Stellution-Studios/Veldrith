namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLStorageMode enumeration.
/// </summary>
public enum MTLStorageMode : ulong {

    /// <summary>
    /// Stores the shared state used by this instance.
    /// </summary>
    Shared = 0, Managed = 1, Private = 2, Memoryless = 3
}