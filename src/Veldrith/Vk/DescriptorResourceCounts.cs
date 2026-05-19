namespace Veldrith.Vk;

/// <summary>
/// Defines the data layout and behavior of the DescriptorResourceCounts struct.
/// </summary>
internal struct DescriptorResourceCounts {

    /// <summary>
    /// Stores the value associated with <c>UniformBufferCount</c>.
    /// </summary>
    public readonly uint UniformBufferCount;

    /// <summary>
    /// Stores the value associated with <c>SampledImageCount</c>.
    /// </summary>
    public readonly uint SampledImageCount;

    /// <summary>
    /// Stores the value associated with <c>SamplerCount</c>.
    /// </summary>
    public readonly uint SamplerCount;

    /// <summary>
    /// Stores the value associated with <c>StorageBufferCount</c>.
    /// </summary>
    public readonly uint StorageBufferCount;

    /// <summary>
    /// Stores the value associated with <c>StorageImageCount</c>.
    /// </summary>
    public readonly uint StorageImageCount;

    /// <summary>
    /// Stores the value associated with <c>UniformBufferDynamicCount</c>.
    /// </summary>
    public readonly uint UniformBufferDynamicCount;

    /// <summary>
    /// Stores the value associated with <c>StorageBufferDynamicCount</c>.
    /// </summary>
    public readonly uint StorageBufferDynamicCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorResourceCounts" /> type.
    /// </summary>
    /// <param name="uniformBufferCount">Specifies the value of <paramref name="uniformBufferCount" />.</param>
    /// <param name="uniformBufferDynamicCount">Specifies the value of <paramref name="uniformBufferDynamicCount" />.</param>
    /// <param name="sampledImageCount">Specifies the value of <paramref name="sampledImageCount" />.</param>
    /// <param name="samplerCount">Specifies the value of <paramref name="samplerCount" />.</param>
    /// <param name="storageBufferCount">Specifies the value of <paramref name="storageBufferCount" />.</param>
    /// <param name="storageBufferDynamicCount">Specifies the value of <paramref name="storageBufferDynamicCount" />.</param>
    /// <param name="storageImageCount">Specifies the value of <paramref name="storageImageCount" />.</param>
    public DescriptorResourceCounts(uint uniformBufferCount, uint uniformBufferDynamicCount, uint sampledImageCount, uint samplerCount, uint storageBufferCount, uint storageBufferDynamicCount, uint storageImageCount) {
        this.UniformBufferCount = uniformBufferCount;
        this.UniformBufferDynamicCount = uniformBufferDynamicCount;
        this.SampledImageCount = sampledImageCount;
        this.SamplerCount = samplerCount;
        this.StorageBufferCount = storageBufferCount;
        this.StorageBufferDynamicCount = storageBufferDynamicCount;
        this.StorageImageCount = storageImageCount;
    }
}