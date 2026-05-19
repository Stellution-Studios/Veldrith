using System.Collections.Generic;
using System.Diagnostics;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk
{
    internal class VkDescriptorPoolManager
    {
        private readonly VkGraphicsDevice gd;
        private readonly List<PoolInfo> pools = new List<PoolInfo>();
        private readonly object @lock = new object();

        public VkDescriptorPoolManager(VkGraphicsDevice gd)
        {
            this.gd = gd;
            pools.Add(createNewPool());
        }

        public unsafe DescriptorAllocationToken Allocate(DescriptorResourceCounts counts, VkDescriptorSetLayout setLayout)
        {
            lock (@lock)
            {
                var pool = getPool(counts);
                var dsAi = VkDescriptorSetAllocateInfo.New();
                dsAi.descriptorSetCount = 1;
                dsAi.pSetLayouts = &setLayout;
                dsAi.descriptorPool = pool;
                var result = vkAllocateDescriptorSets(gd.Device, ref dsAi, out var set);
                VulkanUtil.CheckResult(result);

                return new DescriptorAllocationToken(set, pool);
            }
        }

        public void Free(DescriptorAllocationToken token, DescriptorResourceCounts counts)
        {
            lock (@lock)
            {
                foreach (var poolInfo in pools)
                {
                    if (poolInfo.Pool == token.Pool)
                        poolInfo.Free(gd.Device, token, counts);
                }
            }
        }

        internal unsafe void DestroyAll()
        {
            foreach (var poolInfo in pools)
                vkDestroyDescriptorPool(gd.Device, poolInfo.Pool, null);
        }

        private VkDescriptorPool getPool(DescriptorResourceCounts counts)
        {
            lock (@lock)
            {
                foreach (var poolInfo in pools)
                {
                    if (poolInfo.Allocate(counts))
                        return poolInfo.Pool;
                }

                var newPool = createNewPool();
                pools.Add(newPool);
                bool result = newPool.Allocate(counts);
                Debug.Assert(result);
                return newPool.Pool;
            }
        }

        private unsafe PoolInfo createNewPool()
        {
            const uint total_sets = 1000;
            const uint descriptor_count = 100;
            const uint pool_size_count = 7;
            var sizes = stackalloc VkDescriptorPoolSize[(int)pool_size_count];
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

            var poolCi = VkDescriptorPoolCreateInfo.New();
            poolCi.flags = VkDescriptorPoolCreateFlags.FreeDescriptorSet;
            poolCi.maxSets = total_sets;
            poolCi.pPoolSizes = sizes;
            poolCi.poolSizeCount = pool_size_count;

            var result = vkCreateDescriptorPool(gd.Device, ref poolCi, null, out var descriptorPool);
            VulkanUtil.CheckResult(result);

            return new PoolInfo(descriptorPool, total_sets, descriptor_count);
        }

        private class PoolInfo
        {
            public readonly VkDescriptorPool Pool;

            public uint RemainingSets;

            public uint UniformBufferCount;
            public uint UniformBufferDynamicCount;
            public uint SampledImageCount;
            public uint SamplerCount;
            public uint StorageBufferCount;
            public uint StorageBufferDynamicCount;
            public uint StorageImageCount;

            public PoolInfo(VkDescriptorPool pool, uint totalSets, uint descriptorCount)
            {
                Pool = pool;
                RemainingSets = totalSets;
                UniformBufferCount = descriptorCount;
                UniformBufferDynamicCount = descriptorCount;
                SampledImageCount = descriptorCount;
                SamplerCount = descriptorCount;
                StorageBufferCount = descriptorCount;
                StorageBufferDynamicCount = descriptorCount;
                StorageImageCount = descriptorCount;
            }

            internal bool Allocate(DescriptorResourceCounts counts)
            {
                if (RemainingSets > 0
                    && UniformBufferCount >= counts.UniformBufferCount
                    && UniformBufferDynamicCount >= counts.UniformBufferDynamicCount
                    && SampledImageCount >= counts.SampledImageCount
                    && SamplerCount >= counts.SamplerCount
                    && StorageBufferCount >= counts.StorageBufferCount
                    && StorageBufferDynamicCount >= counts.StorageBufferDynamicCount
                    && StorageImageCount >= counts.StorageImageCount)
                {
                    RemainingSets -= 1;
                    UniformBufferCount -= counts.UniformBufferCount;
                    UniformBufferDynamicCount -= counts.UniformBufferDynamicCount;
                    SampledImageCount -= counts.SampledImageCount;
                    SamplerCount -= counts.SamplerCount;
                    StorageBufferCount -= counts.StorageBufferCount;
                    StorageBufferDynamicCount -= counts.StorageBufferDynamicCount;
                    StorageImageCount -= counts.StorageImageCount;
                    return true;
                }

                return false;
            }

            internal void Free(VkDevice device, DescriptorAllocationToken token, DescriptorResourceCounts counts)
            {
                var set = token.Set;
                vkFreeDescriptorSets(device, Pool, 1, ref set);

                RemainingSets += 1;

                UniformBufferCount += counts.UniformBufferCount;
                SampledImageCount += counts.SampledImageCount;
                SamplerCount += counts.SamplerCount;
                StorageBufferCount += counts.StorageBufferCount;
                StorageImageCount += counts.StorageImageCount;
            }
        }
    }

    internal struct DescriptorAllocationToken
    {
        public readonly VkDescriptorSet Set;
        public readonly VkDescriptorPool Pool;

        public DescriptorAllocationToken(VkDescriptorSet set, VkDescriptorPool pool)
        {
            Set = set;
            Pool = pool;
        }
    }
}
