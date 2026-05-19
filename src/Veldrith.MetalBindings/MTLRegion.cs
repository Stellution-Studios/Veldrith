namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRegion struct.
/// </summary>
public struct MTLRegion {

    /// <summary>
    /// Represents the origin field.
    /// </summary>
    public MTLOrigin origin;

    /// <summary>
    /// Represents the size field.
    /// </summary>
    public MTLSize size;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRegion" /> class.
    /// </summary>
    public MTLRegion(MTLOrigin origin, MTLSize size) {
        this.origin = origin;
        this.size = size;
    }
}