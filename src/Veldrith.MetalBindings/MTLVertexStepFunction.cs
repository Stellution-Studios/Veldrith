namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLVertexStepFunction enumeration.
/// </summary>
public enum MTLVertexStepFunction {

    /// <summary>
    /// Stores the value associated with <c>Constant</c>.
    /// </summary>
    Constant = 0, PerVertex = 1, PerInstance = 2, PerPatch = 3, PerPatchControlPoint = 4
}