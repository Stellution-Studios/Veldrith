using System.Collections.Generic;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk
{
    internal unsafe class VkResourceSet : ResourceSet
    {
        public VkDescriptorSet DescriptorSet => descriptorAllocationToken.Set;
        public List<VkTexture> SampledTextures { get; } = new List<VkTexture>();

        public List<VkTexture> StorageTextures { get; } = new List<VkTexture>();

        public ResourceRefCount RefCount { get; }
        public List<ResourceRefCount> RefCounts { get; } = new List<ResourceRefCount>();

        public override bool IsDisposed => destroyed;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                gd.SetResourceName(this, value);
            }
        }

        private readonly VkGraphicsDevice gd;
        private readonly DescriptorResourceCounts descriptorCounts;
        private readonly DescriptorAllocationToken descriptorAllocationToken;

        private bool destroyed;
        private string name;

        public VkResourceSet(VkGraphicsDevice gd, ref ResourceSetDescription description)
            : base(ref description)
        {
            this.gd = gd;
            RefCount = new ResourceRefCount(disposeCore);
            var vkLayout = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(description.Layout);

            var dsl = vkLayout.DescriptorSetLayout;
            descriptorCounts = vkLayout.DescriptorResourceCounts;
            descriptorAllocationToken = this.gd.DescriptorPoolManager.Allocate(descriptorCounts, dsl);

            var boundResources = description.BoundResources;
            uint descriptorWriteCount = (uint)boundResources.Length;
            var descriptorWrites = stackalloc VkWriteDescriptorSet[(int)descriptorWriteCount];
            var bufferInfos = stackalloc VkDescriptorBufferInfo[(int)descriptorWriteCount];
            var imageInfos = stackalloc VkDescriptorImageInfo[(int)descriptorWriteCount];

            for (int i = 0; i < descriptorWriteCount; i++)
            {
                var type = vkLayout.DescriptorTypes[i];

                descriptorWrites[i].sType = VkStructureType.WriteDescriptorSet;
                descriptorWrites[i].descriptorCount = 1;
                descriptorWrites[i].descriptorType = type;
                descriptorWrites[i].dstBinding = (uint)i;
                descriptorWrites[i].dstSet = descriptorAllocationToken.Set;

                if (type == VkDescriptorType.UniformBuffer || type == VkDescriptorType.UniformBufferDynamic
                                                           || type == VkDescriptorType.StorageBuffer || type == VkDescriptorType.StorageBufferDynamic)
                {
                    var range = Util.GetBufferRange(boundResources[i], 0);
                    var rangedVkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(range.Buffer);
                    bufferInfos[i].buffer = rangedVkBuffer.DeviceBuffer;
                    bufferInfos[i].offset = range.Offset;
                    bufferInfos[i].range = range.SizeInBytes;
                    descriptorWrites[i].pBufferInfo = &bufferInfos[i];
                    RefCounts.Add(rangedVkBuffer.RefCount);
                }
                else if (type == VkDescriptorType.SampledImage)
                {
                    var texView = Util.GetTextureView(this.gd, boundResources[i]);
                    var vkTexView = Util.AssertSubtype<TextureView, VkTextureView>(texView);
                    imageInfos[i].imageView = vkTexView.ImageView;
                    imageInfos[i].imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
                    descriptorWrites[i].pImageInfo = &imageInfos[i];
                    SampledTextures.Add(Util.AssertSubtype<Texture, VkTexture>(texView.Target));
                    RefCounts.Add(vkTexView.RefCount);
                }
                else if (type == VkDescriptorType.StorageImage)
                {
                    var texView = Util.GetTextureView(this.gd, boundResources[i]);
                    var vkTexView = Util.AssertSubtype<TextureView, VkTextureView>(texView);
                    imageInfos[i].imageView = vkTexView.ImageView;
                    imageInfos[i].imageLayout = VkImageLayout.General;
                    descriptorWrites[i].pImageInfo = &imageInfos[i];
                    StorageTextures.Add(Util.AssertSubtype<Texture, VkTexture>(texView.Target));
                    RefCounts.Add(vkTexView.RefCount);
                }
                else if (type == VkDescriptorType.Sampler)
                {
                    var sampler = Util.AssertSubtype<IBindableResource, VkSampler>(boundResources[i]);
                    imageInfos[i].sampler = sampler.DeviceSampler;
                    descriptorWrites[i].pImageInfo = &imageInfos[i];
                    RefCounts.Add(sampler.RefCount);
                }
            }

            vkUpdateDescriptorSets(this.gd.Device, descriptorWriteCount, descriptorWrites, 0, null);
        }

        #region Disposal

        public override void Dispose()
        {
            RefCount.Decrement();
        }

        #endregion

        private void disposeCore()
        {
            if (!destroyed)
            {
                destroyed = true;
                gd.DescriptorPoolManager.Free(descriptorAllocationToken, descriptorCounts);
            }
        }
    }
}
