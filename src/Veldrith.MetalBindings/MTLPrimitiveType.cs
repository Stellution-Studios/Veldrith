namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLPrimitiveType enumeration.
/// </summary>
public enum MTLPrimitiveType : uint {

    /// <summary>
    /// Stores the point state used by this instance.
    /// </summary>
    Point = 0, Line = 1, LineStrip = 2, Triangle = 3, TriangleStrip = 4
}