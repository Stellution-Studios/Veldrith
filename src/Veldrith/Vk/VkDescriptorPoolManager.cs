using System.Collections.Generic;
using System.Diagnostics;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

internal class VkDescriptorPoolManager {
    private readonly List<PoolInfo> _pools = new();
    private readonly VkGraphicsDevice gd;
    private readonly object @lock = new();

    public VkDescriptorPoolManager(VkGraphicsDevice gd) {
        this.gd = gd;
        this._pools.Add(this.CreateNewPool());
    }

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

    public void Free(DescriptorAllocationToken token, DescriptorResourceCounts counts) {
        lock (this.@lock) {
            foreach (PoolInfo poolInfo in this._pools) {
                if (poolInfo.Pool == token.Pool) {
                    poolInfo.Free(this.gd.Device, token, counts);
                }
            }
        }
    }

    internal unsafe void DestroyAll() {
        foreach (PoolInfo poolInfo in this._pools) {
            vkDestroyDescriptorPool(this.gd.Device, poolInfo.Pool, null);
        }
    }

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

    private class PoolInfo {
        public readonly VkDescriptorPool Pool;

        public uint RemainingSets;
        public uint SampledImageCount;
        public uint SamplerCount;
        public uint StorageBufferCount;
        public uint StorageBufferDynamicCount;
        public uint StorageImageCount;

        public uint UniformBufferCount;
        public uint UniformBufferDynamicCount;

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

internal struct DescriptorAllocationToken {
    public readonly VkDescriptorSet Set;
    public readonly VkDescriptorPool Pool;

    public DescriptorAllocationToken(VkDescriptorSet set, VkDescriptorPool pool) {
        this.Set = set;
        this.Pool = pool;
    }
}