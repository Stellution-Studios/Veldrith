using System.Collections.Generic;
using System.Diagnostics;
using Vortice.Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkDescriptorPoolManager.
/// </summary>
internal unsafe class VkDescriptorPoolManager {

    /// <summary>
    /// Stores the pools collection used by this instance.
    /// </summary>
    private readonly List<PoolInfo> _pools = new();

    /// <summary>
    /// Stores the graphics device used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VkDescriptorPoolManager" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    public VkDescriptorPoolManager(VkGraphicsDevice gd) {
        this._gd = gd;
        this._pools.Add(this.CreateNewPool());
    }

    /// <summary>
    /// Executes the allocate logic for this backend.
    /// </summary>
    /// <param name="counts">The counts value used by this operation.</param>
    /// <param name="setLayout">The set layout value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public unsafe DescriptorAllocationToken Allocate(DescriptorResourceCounts counts, VkDescriptorSetLayout setLayout) {
        lock (this._lock) {
            VkDescriptorPool pool = this.GetPool(counts);
            VkDescriptorSetAllocateInfo dsAi = new VkDescriptorSetAllocateInfo();
            dsAi.descriptorSetCount = 1;
            dsAi.pSetLayouts = &setLayout;
            dsAi.descriptorPool = pool;
            VkResult result = this._gd.DeviceApi.vkAllocateDescriptorSets(ref dsAi, out VkDescriptorSet set);
            VulkanUtil.CheckResult(result);

            return new DescriptorAllocationToken(set, pool);
        }
    }

    /// <summary>
    /// Executes the free logic for this backend.
    /// </summary>
    /// <param name="token">The token value used by this operation.</param>
    /// <param name="counts">The counts value used by this operation.</param>
    public void Free(DescriptorAllocationToken token, DescriptorResourceCounts counts) {
        lock (this._lock) {
            foreach (PoolInfo poolInfo in this._pools) {
                if (poolInfo.Pool == token.Pool) {
                    poolInfo.Free(this._gd.Device, token, counts);
                }
            }
        }
    }

    /// <summary>
    /// Executes the destroy all logic for this backend.
    /// </summary>
    internal unsafe void DestroyAll() {
        foreach (PoolInfo poolInfo in this._pools) {
            this._gd.DeviceApi.vkDestroyDescriptorPool(poolInfo.Pool, null);
        }
    }

    /// <summary>
    /// Gets the pool value.
    /// </summary>
    /// <param name="counts">The counts value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private VkDescriptorPool GetPool(DescriptorResourceCounts counts) {
        lock (this._lock) {
            foreach (PoolInfo poolInfo in this._pools) {
                if (poolInfo.Allocate(counts)) {
                    return poolInfo.Pool;
                }
            }

            PoolInfo newPool = this.CreateNewPool();
            this._pools.Add(newPool);
            bool result = newPool.Allocate(counts);
            Debug.Assert(result);
            return newPool.Pool;
        }
    }

    /// <summary>
    /// Creates the new pool instance used by this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private unsafe PoolInfo CreateNewPool() {
        const uint total_sets = 1000;
        const uint descriptor_count = 100;
        const uint pool_size_count = 7;
        VkDescriptorPoolSize* sizes = stackalloc VkDescriptorPoolSize[(int)pool_size_count];
        sizes[0].type = VkDescriptorType.UniformBuffer;
        sizes[0].descriptorCount = descriptor_count;
        sizes[1].type = VkDescriptorType.SampledImage;
        sizes[1].descriptorCount = descriptor_count;
        sizes[2].type = VkDescriptorType.Sampler;
        sizes[2].descriptorCount = descriptor_count;
        sizes[3].type = VkDescriptorType.StorageBuffer;
        sizes[3].descriptorCount = descriptor_count;
        sizes[4].type = VkDescriptorType.StorageImage;
        sizes[4].descriptorCount = descriptor_count;
        sizes[5].type = VkDescriptorType.UniformBufferDynamic;
        sizes[5].descriptorCount = descriptor_count;
        sizes[6].type = VkDescriptorType.StorageBufferDynamic;
        sizes[6].descriptorCount = descriptor_count;

        VkDescriptorPoolCreateInfo poolCi = new VkDescriptorPoolCreateInfo();
        poolCi.flags = VkDescriptorPoolCreateFlags.FreeDescriptorSet;
        poolCi.maxSets = total_sets;
        poolCi.pPoolSizes = sizes;
        poolCi.poolSizeCount = pool_size_count;

        VkResult result = this._gd.DeviceApi.vkCreateDescriptorPool(ref poolCi, null, out VkDescriptorPool descriptorPool);
        VulkanUtil.CheckResult(result);

        return new PoolInfo(descriptorPool, total_sets, descriptor_count);
    }

    /// <summary>
    /// Represents the PoolInfo type used by the graphics runtime.
    /// </summary>
    private class PoolInfo {

        /// <summary>
        /// Stores the pool state used by this instance.
        /// </summary>
        public readonly VkDescriptorPool Pool;

        /// <summary>
        /// Stores the remaining sets state used by this instance.
        /// </summary>
        public uint RemainingSets;

        /// <summary>
        /// Stores the sampled image count value used during command execution.
        /// </summary>
        public uint SampledImageCount;

        /// <summary>
        /// Stores the sampler count value used during command execution.
        /// </summary>
        public uint SamplerCount;

        /// <summary>
        /// Stores the storage buffer count value used during command execution.
        /// </summary>
        public uint StorageBufferCount;

        /// <summary>
        /// Stores the storage buffer dynamic count value used during command execution.
        /// </summary>
        public uint StorageBufferDynamicCount;

        /// <summary>
        /// Stores the storage image count value used during command execution.
        /// </summary>
        public uint StorageImageCount;

        /// <summary>
        /// Stores the uniform buffer count value used during command execution.
        /// </summary>
        public uint UniformBufferCount;

        /// <summary>
        /// Stores the uniform buffer dynamic count value used during command execution.
        /// </summary>
        public uint UniformBufferDynamicCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolInfo" /> type.
        /// </summary>
        /// <param name="pool">The pool value used by this operation.</param>
        /// <param name="totalSets">The total sets value used by this operation.</param>
        /// <param name="descriptorCount">The descriptor count value used by this operation.</param>
        public PoolInfo(VkDescriptorPool pool, uint totalSets, uint descriptorCount) {
            this.Pool = pool;
            this.RemainingSets = totalSets;
            this.UniformBufferCount = descriptorCount;
            this.UniformBufferDynamicCount = descriptorCount;
            this.SampledImageCount = descriptorCount;
            this.SamplerCount = descriptorCount;
            this.StorageBufferCount = descriptorCount;
            this.StorageBufferDynamicCount = descriptorCount;
            this.StorageImageCount = descriptorCount;
        }

        /// <summary>
        /// Executes the allocate logic for this backend.
        /// </summary>
        /// <param name="counts">The counts value used by this operation.</param>
        /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
        internal bool Allocate(DescriptorResourceCounts counts) {
            if (this.RemainingSets > 0
                && this.UniformBufferCount >= counts.UniformBufferCount
                && this.UniformBufferDynamicCount >= counts.UniformBufferDynamicCount
                && this.SampledImageCount >= counts.SampledImageCount
                && this.SamplerCount >= counts.SamplerCount
                && this.StorageBufferCount >= counts.StorageBufferCount
                && this.StorageBufferDynamicCount >= counts.StorageBufferDynamicCount
                && this.StorageImageCount >= counts.StorageImageCount) {
                this.RemainingSets -= 1;
                this.UniformBufferCount -= counts.UniformBufferCount;
                this.UniformBufferDynamicCount -= counts.UniformBufferDynamicCount;
                this.SampledImageCount -= counts.SampledImageCount;
                this.SamplerCount -= counts.SamplerCount;
                this.StorageBufferCount -= counts.StorageBufferCount;
                this.StorageBufferDynamicCount -= counts.StorageBufferDynamicCount;
                this.StorageImageCount -= counts.StorageImageCount;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Executes the free logic for this backend.
        /// </summary>
        /// <param name="device">The device value used by this operation.</param>
        /// <param name="token">The token value used by this operation.</param>
        /// <param name="counts">The counts value used by this operation.</param>
        internal void Free(VkDevice device, DescriptorAllocationToken token, DescriptorResourceCounts counts) {
            VkDescriptorSet set = token.Set;
            VulkanDispatch.GetApi(device).vkFreeDescriptorSets(this.Pool, 1, &set);

            this.RemainingSets += 1;

            this.UniformBufferCount += counts.UniformBufferCount;
            this.UniformBufferDynamicCount += counts.UniformBufferDynamicCount;
            this.SampledImageCount += counts.SampledImageCount;
            this.SamplerCount += counts.SamplerCount;
            this.StorageBufferCount += counts.StorageBufferCount;
            this.StorageBufferDynamicCount += counts.StorageBufferDynamicCount;
            this.StorageImageCount += counts.StorageImageCount;
        }
    }
}

/// <summary>
/// Represents the DescriptorAllocationToken data structure used by the graphics runtime.
/// </summary>
internal struct DescriptorAllocationToken {

    /// <summary>
    /// Stores the set state used by this instance.
    /// </summary>
    public readonly VkDescriptorSet Set;

    /// <summary>
    /// Stores the pool state used by this instance.
    /// </summary>
    public readonly VkDescriptorPool Pool;

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorAllocationToken" /> type.
    /// </summary>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="pool">The pool value used by this operation.</param>
    public DescriptorAllocationToken(VkDescriptorSet set, VkDescriptorPool pool) {
        this.Set = set;
        this.Pool = pool;
    }
}
