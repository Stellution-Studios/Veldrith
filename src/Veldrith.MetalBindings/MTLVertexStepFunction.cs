namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLVertexStepFunction enumeration.
/// </summary>
public enum MTLVertexStepFunction {

    /// <summary>
    /// Stores the constant state used by this instance.
    /// </summary>
    Constant = 0, PerVertex = 1, PerInstance = 2, PerPatch = 3, PerPatchControlPoint = 4
}