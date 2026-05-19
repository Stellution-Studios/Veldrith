using System.Collections.Generic;
using System.Diagnostics;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Represents the VkDescriptorPoolManager class.
/// </summary>
internal class VkDescriptorPoolManager {

    /// <summary>
    /// Represents the _pools field.
    /// </summary>
    private readonly List<PoolInfo> _pools = new();

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Represents the @lock field.
    /// </summary>
    private readonly object @lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VkDescriptorPoolManager" /> class.
    /// </summary>
    public VkDescriptorPoolManager(VkGraphicsDevice gd) {
        this.gd = gd;
        this._pools.Add(this.CreateNewPool());
    }

    /// <summary>
    /// Executes Allocate.
    /// </summary>
    public unsafe DescriptorAllocationToken Allocate(DescriptorResourceCounts counts, VkDescriptorSetLayout setLayout) {
        lock (this.@lock) {
            VkDescriptorPool pool = this.GetPool(counts);
            VkDescriptorSetAllocateInfo dsAi = VkDescriptorSetAllocateInfo.New();
            dsAi.descriptorSetCount = 1;
            dsAi.pSetLayouts = &setLayout;
            dsAi.descriptorPool = pool;
            VkResult result = vkAllocateDescriptorSets(this.gd.Device, ref dsAi, out VkDescriptorSet set);
            VulkanUtil.CheckResult(result);

            return new DescriptorAllocationToken(set, pool);
        }
    }

    /// <summary>
    /// Executes Free.
    /// </summary>
    public void Free(DescriptorAllocationToken token, DescriptorResourceCounts counts) {
        lock (this.@lock) {
            foreach (PoolInfo poolInfo in this._pools) {
                if (poolInfo.Pool == token.Pool) {
                    poolInfo.Free(this.gd.Device, token, counts);
                }
            }
        }
    }

    /// <summary>
    /// Executes DestroyAll.
    /// </summary>
    internal unsafe void DestroyAll() {
        foreach (PoolInfo poolInfo in this._pools) {
            vkDestroyDescriptorPool(this.gd.Device, poolInfo.Pool, null);
        }
    }

    /// <summary>
    /// Executes GetPool.
    /// </summary>
    private VkDescriptorPool GetPool(DescriptorResourceCounts counts) {
        lock (this.@lock) {
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
    /// Executes CreateNewPool.
    /// </summary>
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

        VkDescriptorPoolCreateInfo poolCi = VkDescriptorPoolCreateInfo.New();
        poolCi.flags = VkDescriptorPoolCreateFlags.FreeDescriptorSet;
        poolCi.maxSets = total_sets;
        poolCi.pPoolSizes = sizes;
        poolCi.poolSizeCount = pool_size_count;

        VkResult result = vkCreateDescriptorPool(this.gd.Device, ref poolCi, null, out VkDescriptorPool descriptorPool);
        VulkanUtil.CheckResult(result);

        return new PoolInfo(descriptorPool, total_sets, descriptor_count);
    }

    /// <summary>
    /// Represents the PoolInfo class.
    /// </summary>
    private class PoolInfo {

        /// <summary>
        /// Represents the Pool field.
        /// </summary>
        public readonly VkDescriptorPool Pool;

        /// <summary>
        /// Represents the RemainingSets field.
        /// </summary>
        public uint RemainingSets;

        /// <summary>
        /// Represents the SampledImageCount field.
        /// </summary>
        public uint SampledImageCount;

        /// <summary>
        /// Represents the SamplerCount field.
        /// </summary>
        public uint SamplerCount;

        /// <summary>
        /// Represents the StorageBufferCount field.
        /// </summary>
        public uint StorageBufferCount;

        /// <summary>
        /// Represents the StorageBufferDynamicCount field.
        /// </summary>
        public uint StorageBufferDynamicCount;

        /// <summary>
        /// Represents the StorageImageCount field.
        /// </summary>
        public uint StorageImageCount;

        /// <summary>
        /// Represents the UniformBufferCount field.
        /// </summary>
        public uint UniformBufferCount;

        /// <summary>
        /// Represents the UniformBufferDynamicCount field.
        /// </summary>
        public uint UniformBufferDynamicCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolInfo" /> class.
        /// </summary>
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
        /// Executes Allocate.
        /// </summary>
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
        /// Executes Free.
        /// </summary>
        internal void Free(VkDevice device, DescriptorAllocationToken token, DescriptorResourceCounts counts) {
            VkDescriptorSet set = token.Set;
            vkFreeDescriptorSets(device, this.Pool, 1, ref set);

            this.RemainingSets += 1;

            this.UniformBufferCount += counts.UniformBufferCount;
            this.SampledImageCount += counts.SampledImageCount;
            this.SamplerCount += counts.SamplerCount;
            this.StorageBufferCount += counts.StorageBufferCount;
            this.StorageImageCount += counts.StorageImageCount;
        }
    }
}

/// <summary>
/// Represents the DescriptorAllocationToken struct.
/// </summary>
internal struct DescriptorAllocationToken {

    /// <summary>
    /// Represents the Set field.
    /// </summary>
    public readonly VkDescriptorSet Set;

    /// <summary>
    /// Represents the Pool field.
    /// </summary>
    public readonly VkDescriptorPool Pool;

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorAllocationToken" /> class.
    /// </summary>
    public DescriptorAllocationToken(VkDescriptorSet set, VkDescriptorPool pool) {
        this.Set = set;
        this.Pool = pool;
    }
}