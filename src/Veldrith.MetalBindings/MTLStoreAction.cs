namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLStoreAction enumeration.
/// </summary>
public enum MTLStoreAction {

    /// <summary>
    /// Stores the value associated with <c>DontCare</c>.
    /// </summary>
    DontCare = 0, Store = 1, MultisampleResolve = 2, StoreAndMultisampleResolve = 3, Unknown = 4, CustomSampleDepthStore = 5
}