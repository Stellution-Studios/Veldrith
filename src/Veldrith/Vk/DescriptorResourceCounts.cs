namespace Veldrith.Vk;

/// <summary>
/// Represents the DescriptorResourceCounts data structure used by the graphics runtime.
/// </summary>
internal struct DescriptorResourceCounts {

    /// <summary>
    /// Stores the uniform buffer count value used during command execution.
    /// </summary>
    public readonly uint UniformBufferCount;

    /// <summary>
    /// Stores the sampled image count value used during command execution.
    /// </summary>
    public readonly uint SampledImageCount;

    /// <summary>
    /// Stores the sampler count value used during command execution.
    /// </summary>
    public readonly uint SamplerCount;

    /// <summary>
    /// Stores the storage buffer count value used during command execution.
    /// </summary>
    public readonly uint StorageBufferCount;

    /// <summary>
    /// Stores the storage image count value used during command execution.
    /// </summary>
    public readonly uint StorageImageCount;

    /// <summary>
    /// Stores the uniform buffer dynamic count value used during command execution.
    /// </summary>
    public readonly uint UniformBufferDynamicCount;

    /// <summary>
    /// Stores the storage buffer dynamic count value used during command execution.
    /// </summary>
    public readonly uint StorageBufferDynamicCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorResourceCounts" /> type.
    /// </summary>
    /// <param name="uniformBufferCount">The uniform buffer count value used by this operation.</param>
    /// <param name="uniformBufferDynamicCount">The uniform buffer dynamic count value used by this operation.</param>
    /// <param name="sampledImageCount">The sampled image count value used by this operation.</param>
    /// <param name="samplerCount">The sampler count value used by this operation.</param>
    /// <param name="storageBufferCount">The storage buffer count value used by this operation.</param>
    /// <param name="storageBufferDynamicCount">The storage buffer dynamic count value used by this operation.</param>
    /// <param name="storageImageCount">The storage image count value used by this operation.</param>
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