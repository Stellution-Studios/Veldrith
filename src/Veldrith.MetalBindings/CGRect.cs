namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CGRect data structure used by the graphics runtime.
/// </summary>
public struct CGRect {

    /// <summary>
    /// Stores the origin state used by this instance.
    /// </summary>
    public CGPoint Origin;

    /// <summary>
    /// Stores the size value used during command execution.
    /// </summary>
    public CGSize Size;

    /// <summary>
    /// Initializes a new instance of the <see cref="CGRect" /> type.
    /// </summary>
    /// <param name="origin">The origin value used by this operation.</param>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    public CGRect(CGPoint origin, CGSize size) {
        this.Origin = origin;
        this.Size = size;
    }

    /// <summary>
    /// Builds a string representation of this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override string ToString() {
        return string.Format("{0}, {1}", this.Origin, this.Size);
    }
}