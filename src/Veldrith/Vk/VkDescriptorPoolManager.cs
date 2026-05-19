using System.Collections.Generic;
using System.Diagnostics;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Defines the behavior and responsibilities of the VkDescriptorPoolManager class.
/// </summary>
internal class VkDescriptorPoolManager {

    /// <summary>
    /// Stores the value associated with <c>_pools</c>.
    /// </summary>
    private readonly List<PoolInfo> _pools = new();

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    /// <returns>Returns the result produced by the new operation.</returns>
    private readonly object @lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VkDescriptorPoolManager" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    public VkDescriptorPoolManager(VkGraphicsDevice gd) {
        this.gd = gd;
        this._pools.Add(this.CreateNewPool());
    }

    /// <summary>
    /// Executes the Allocate operation.
    /// </summary>
    /// <param name="counts">Specifies the value of <paramref name="counts" />.</param>
    /// <param name="setLayout">Specifies the value of <paramref name="setLayout" />.</param>
    /// <returns>Returns the result produced by the Allocate operation.</returns>
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
    /// Executes the Free operation.
    /// </summary>
    /// <param name="token">Specifies the value of <paramref name="token" />.</param>
    /// <param name="counts">Specifies the value of <paramref name="counts" />.</param>
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
    /// Executes the DestroyAll operation.
    /// </summary>
    internal unsafe void DestroyAll() {
        foreach (PoolInfo poolInfo in this._pools) {
            vkDestroyDescriptorPool(this.gd.Device, poolInfo.Pool, null);
        }
    }

    /// <summary>
    /// Executes the GetPool operation.
    /// </summary>
    /// <param name="counts">Specifies the value of <paramref name="counts" />.</param>
    /// <returns>Returns the result produced by the GetPool operation.</returns>
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
    /// Executes the CreateNewPool operation.
    /// </summary>
    /// <returns>Returns the result produced by the CreateNewPool operation.</returns>
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
    /// Defines the behavior and responsibilities of the PoolInfo class.
    /// </summary>
    private class PoolInfo {

        /// <summary>
        /// Stores the value associated with <c>Pool</c>.
        /// </summary>
        public readonly VkDescriptorPool Pool;

        /// <summary>
        /// Stores the value associated with <c>RemainingSets</c>.
        /// </summary>
        public uint RemainingSets;

        /// <summary>
        /// Stores the value associated with <c>SampledImageCount</c>.
        /// </summary>
        public uint SampledImageCount;

        /// <summary>
        /// Stores the value associated with <c>SamplerCount</c>.
        /// </summary>
        public uint SamplerCount;

        /// <summary>
        /// Stores the value associated with <c>StorageBufferCount</c>.
        /// </summary>
        public uint StorageBufferCount;

        /// <summary>
        /// Stores the value associated with <c>StorageBufferDynamicCount</c>.
        /// </summary>
        public uint StorageBufferDynamicCount;

        /// <summary>
        /// Stores the value associated with <c>StorageImageCount</c>.
        /// </summary>
        public uint StorageImageCount;

        /// <summary>
        /// Stores the value associated with <c>UniformBufferCount</c>.
        /// </summary>
        public uint UniformBufferCount;

        /// <summary>
        /// Stores the value associated with <c>UniformBufferDynamicCount</c>.
        /// </summary>
        public uint UniformBufferDynamicCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolInfo" /> type.
        /// </summary>
        /// <param name="pool">Specifies the value of <paramref name="pool" />.</param>
        /// <param name="totalSets">Specifies the value of <paramref name="totalSets" />.</param>
        /// <param name="descriptorCount">Specifies the value of <paramref name="descriptorCount" />.</param>
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
        /// Executes the Allocate operation.
        /// </summary>
        /// <param name="counts">Specifies the value of <paramref name="counts" />.</param>
        /// <returns>Returns the result produced by the Allocate operation.</returns>
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
        /// Executes the Free operation.
        /// </summary>
        /// <param name="device">Specifies the value of <paramref name="device" />.</param>
        /// <param name="token">Specifies the value of <paramref name="token" />.</param>
        /// <param name="counts">Specifies the value of <paramref name="counts" />.</param>
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
/// Defines the data layout and behavior of the DescriptorAllocationToken struct.
/// </summary>
internal struct DescriptorAllocationToken {

    /// <summary>
    /// Stores the value associated with <c>Set</c>.
    /// </summary>
    public readonly VkDescriptorSet Set;

    /// <summary>
    /// Stores the value associated with <c>Pool</c>.
    /// </summary>
    public readonly VkDescriptorPool Pool;

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorAllocationToken" /> type.
    /// </summary>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="pool">Specifies the value of <paramref name="pool" />.</param>
    public DescriptorAllocationToken(VkDescriptorSet set, VkDescriptorPool pool) {
        this.Set = set;
        this.Pool = pool;
    }
}
