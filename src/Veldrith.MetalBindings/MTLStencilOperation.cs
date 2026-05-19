namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLStencilOperation enumeration.
/// </summary>
public enum MTLStencilOperation {

    /// <summary>
    /// Stores the keep state used by this instance.
    /// </summary>
    Keep = 0, Zero = 1, Replace = 2, IncrementClamp = 3, DecrementClamp = 4, Invert = 5, IncrementWrap = 6, DecrementWrap = 7
}