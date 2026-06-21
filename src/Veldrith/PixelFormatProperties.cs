namespace Veldrith;

/// <summary>
/// Describes the properties that are supported for a particular combination of <see cref="PixelFormat" />,
/// </summary>
public struct PixelFormatProperties {

    /// <summary>
    /// The maximum supported width.
    /// </summary>
    public readonly uint MaxWidth;

    /// <summary>
    /// The maximum supported height.
    /// </summary>
    public readonly uint MaxHeight;

    /// <summary>
    /// The maximum supported depth.
    /// </summary>
    public readonly uint MaxDepth;

    /// <summary>
    /// The maximum supported number of mipmap levels.
    /// </summary>
    public readonly uint MaxMipLevels;

    /// <summary>
    /// The maximum supported number of array layers.
    /// </summary>
    public readonly uint MaxArrayLayers;

    /// <summary>
    /// Stores the sample counts value used during command execution.
    /// </summary>
    private readonly uint sampleCounts;

    /// <summary>
    /// Executes the is sample count supported logic for this backend.
    /// </summary>
    /// <param name="count">The number of items involved in this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool IsSampleCountSupported(TextureSampleCount count) {
        int bit = (int)count;
        return (this.sampleCounts & (1 << bit)) != 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PixelFormatProperties" /> type.
    /// </summary>
    /// <param name="maxWidth">The max width value used by this operation.</param>
    /// <param name="maxHeight">The max height value used by this operation.</param>
    /// <param name="maxDepth">The max depth value used by this operation.</param>
    /// <param name="maxMipLevels">The max mip levels value used by this operation.</param>
    /// <param name="maxArrayLayers">The max array layers value used by this operation.</param>
    /// <param name="sampleCounts">The sample counts value used by this operation.</param>
    internal PixelFormatProperties(uint maxWidth, uint maxHeight, uint maxDepth, uint maxMipLevels, uint maxArrayLayers, uint sampleCounts) {
        this.MaxWidth = maxWidth;
        this.MaxHeight = maxHeight;
        this.MaxDepth = maxDepth;
        this.MaxMipLevels = maxMipLevels;
        this.MaxArrayLayers = maxArrayLayers;
        this.sampleCounts = sampleCounts;
    }
}