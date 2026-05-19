namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLStoreAction enumeration.
/// </summary>
public enum MTLStoreAction {

    /// <summary>
    /// Stores the dont care state used by this instance.
    /// </summary>
    DontCare = 0, Store = 1, MultisampleResolve = 2, StoreAndMultisampleResolve = 3, Unknown = 4, CustomSampleDepthStore = 5
}