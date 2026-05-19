namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLRegion struct.
/// </summary>
public struct MTLRegion {

    /// <summary>
    /// Stores the value associated with <c>origin</c>.
    /// </summary>
    public MTLOrigin origin;

    /// <summary>
    /// Stores the value associated with <c>size</c>.
    /// </summary>
    public MTLSize size;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLRegion" /> type.
    /// </summary>
    /// <param name="origin">Specifies the value of <paramref name="origin" />.</param>
    /// <param name="size">Specifies the value of <paramref name="size" />.</param>
    public MTLRegion(MTLOrigin origin, MTLSize size) {
        this.origin = origin;
        this.size = size;
    }
}