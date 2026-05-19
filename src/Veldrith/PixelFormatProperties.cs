namespace Veldrith;

/// <summary>
/// Describes the properties that are supported for a particular combination of <see cref="PixelFormat" />,
/// <see cref="TextureType" />, and <see cref="TextureUsage" /> by a <see cref="GraphicsDevice" />.
/// See
/// <see cref="GraphicsDevice.GetPixelFormatSupport(PixelFormat, TextureType, TextureUsage, out PixelFormatProperties)" />
/// .
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
    /// Represents the sampleCounts field.
    /// </summary>
    private readonly uint sampleCounts;

    /// <summary>
    /// Performs the IsSampleCountSupported operation.
    /// </summary>
    /// <param name="count">The value of count.</param>
    /// <returns>The result of the IsSampleCountSupported operation.</returns>
    public bool IsSampleCountSupported(TextureSampleCount count) {
        int bit = (int)count;
        return (this.sampleCounts & (1 << bit)) != 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PixelFormatProperties" /> type.
    /// </summary>
    /// <param name="maxWidth">The value of maxWidth.</param>
    /// <param name="maxHeight">The value of maxHeight.</param>
    /// <param name="maxDepth">The value of maxDepth.</param>
    /// <param name="maxMipLevels">The value of maxMipLevels.</param>
    /// <param name="maxArrayLayers">The value of maxArrayLayers.</param>
    /// <param name="sampleCounts">The value of sampleCounts.</param>
    internal PixelFormatProperties(uint maxWidth, uint maxHeight, uint maxDepth, uint maxMipLevels, uint maxArrayLayers, uint sampleCounts) {
        this.MaxWidth = maxWidth;
        this.MaxHeight = maxHeight;
        this.MaxDepth = maxDepth;
        this.MaxMipLevels = maxMipLevels;
        this.MaxArrayLayers = maxArrayLayers;
        this.sampleCounts = sampleCounts;
    }
}