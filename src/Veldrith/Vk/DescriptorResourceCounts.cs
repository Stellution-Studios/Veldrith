namespace Veldrith.Vk;

/// <summary>
/// Represents the DescriptorResourceCounts struct.
/// </summary>
internal struct DescriptorResourceCounts {

    /// <summary>
    /// Represents the UniformBufferCount field.
    /// </summary>
    public readonly uint UniformBufferCount;

    /// <summary>
    /// Represents the SampledImageCount field.
    /// </summary>
    public readonly uint SampledImageCount;

    /// <summary>
    /// Represents the SamplerCount field.
    /// </summary>
    public readonly uint SamplerCount;

    /// <summary>
    /// Represents the StorageBufferCount field.
    /// </summary>
    public readonly uint StorageBufferCount;

    /// <summary>
    /// Represents the StorageImageCount field.
    /// </summary>
    public readonly uint StorageImageCount;

    /// <summary>
    /// Represents the UniformBufferDynamicCount field.
    /// </summary>
    public readonly uint UniformBufferDynamicCount;

    /// <summary>
    /// Represents the StorageBufferDynamicCount field.
    /// </summary>
    public readonly uint StorageBufferDynamicCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorResourceCounts" /> type.
    /// </summary>
    /// <param name="uniformBufferCount">The value of uniformBufferCount.</param>
    /// <param name="uniformBufferDynamicCount">The value of uniformBufferDynamicCount.</param>
    /// <param name="sampledImageCount">The value of sampledImageCount.</param>
    /// <param name="samplerCount">The value of samplerCount.</param>
    /// <param name="storageBufferCount">The value of storageBufferCount.</param>
    /// <param name="storageBufferDynamicCount">The value of storageBufferDynamicCount.</param>
    /// <param name="storageImageCount">The value of storageImageCount.</param>
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