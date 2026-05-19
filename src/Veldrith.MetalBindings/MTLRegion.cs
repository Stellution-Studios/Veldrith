namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLRegion data structure used by the graphics runtime.
/// </summary>
public struct MTLRegion {

    /// <summary>
    /// Stores the origin state used by this instance.
    /// </summary>
    public MTLOrigin origin;

    /// <summary>
    /// Stores the size value used during command execution.
    /// </summary>
    public MTLSize size;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRegion" /> type.
    /// </summary>
    /// <param name="origin">The origin value used by this operation.</param>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    public MTLRegion(MTLOrigin origin, MTLSize size) {
        this.origin = origin;
        this.size = size;
    }
}