using System.Collections.Generic;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

internal unsafe class VkResourceSet : ResourceSet {
    private readonly DescriptorAllocationToken _descriptorAllocationToken;
    private readonly DescriptorResourceCounts _descriptorCounts;

    private readonly VkGraphicsDevice gd;

    private bool _destroyed;
    private string _name;

    public VkResourceSet(VkGraphicsDevice gd, ref ResourceSetDescription description)
        : base(ref description) {
        this.gd = gd;
        this.RefCount = new ResourceRefCount(this.DisposeCore);
        VkResourceLayout vkLayout = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(description.Layout);

        VkDescriptorSetLayout dsl = vkLayout.DescriptorSetLayout;
        this._descriptorCounts = vkLayout.DescriptorResourceCounts;
        this._descriptorAllocationToken = this.gd.DescriptorPoolManager.Allocate(this._descriptorCounts, dsl);

        IBindableResource[] boundResources = description.BoundResources;
        uint descriptorWriteCount = (uint)boundResources.Length;
        VkWriteDescriptorSet* descriptorWrites = stackalloc VkWriteDescriptorSet[(int)descriptorWriteCount];
        VkDescriptorBufferInfo* bufferInfos = stackalloc VkDescriptorBufferInfo[(int)descriptorWriteCount];
        VkDescriptorImageInfo* imageInfos = stackalloc VkDescriptorImageInfo[(int)descriptorWriteCount];

        for (int i = 0; i < descriptorWriteCount; i++) {
            VkDescriptorType type = vkLayout.DescriptorTypes[i];

            descriptorWrites[i].sType = VkStructureType.WriteDescriptorSet;
            descriptorWrites[i].descriptorCount = 1;
            descriptorWrites[i].descriptorType = type;
            descriptorWrites[i].dstBinding = (uint)i;
            descriptorWrites[i].dstSet = this._descriptorAllocationToken.Set;

            if (type == VkDescriptorType.UniformBuffer || type == VkDescriptorType.UniformBufferDynamic
                                                       || type == VkDescriptorType.StorageBuffer ||
                                                       type == VkDescriptorType.StorageBufferDynamic) {
                DeviceBufferRange range = Util.GetBufferRange(boundResources[i], 0);
                VkBuffer rangedVkBuffer = Util.AssertSubtype<DeviceBuffer, VkBuffer>(range.Buffer);
                bufferInfos[i].buffer = rangedVkBuffer.DeviceBuffer;
                bufferInfos[i].offset = range.Offset;
                bufferInfos[i].range = range.SizeInBytes;
                descriptorWrites[i].pBufferInfo = &bufferInfos[i];
                this.RefCounts.Add(rangedVkBuffer.RefCount);
            }
            else if (type == VkDescriptorType.SampledImage) {
                TextureView texView = Util.GetTextureView(this.gd, boundResources[i]);
                VkTextureView vkTexView = Util.AssertSubtype<TextureView, VkTextureView>(texView);
                imageInfos[i].imageView = vkTexView.ImageView;
                imageInfos[i].imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
                descriptorWrites[i].pImageInfo = &imageInfos[i];
                this.SampledTextures.Add(Util.AssertSubtype<Texture, VkTexture>(texView.Target));
                this.RefCounts.Add(vkTexView.RefCount);
            }
            else if (type == VkDescriptorType.StorageImage) {
                TextureView texView = Util.GetTextureView(this.gd, boundResources[i]);
                VkTextureView vkTexView = Util.AssertSubtype<TextureView, VkTextureView>(texView);
                imageInfos[i].imageView = vkTexView.ImageView;
                imageInfos[i].imageLayout = VkImageLayout.General;
                descriptorWrites[i].pImageInfo = &imageInfos[i];
                this.StorageTextures.Add(Util.AssertSubtype<Texture, VkTexture>(texView.Target));
                this.RefCounts.Add(vkTexView.RefCount);
            }
            else if (type == VkDescriptorType.Sampler) {
                VkSampler sampler = Util.AssertSubtype<IBindableResource, VkSampler>(boundResources[i]);
                imageInfos[i].sampler = sampler.DeviceSampler;
                descriptorWrites[i].pImageInfo = &imageInfos[i];
                this.RefCounts.Add(sampler.RefCount);
            }
        }

        vkUpdateDescriptorSets(this.gd.Device, descriptorWriteCount, descriptorWrites, 0, null);
    }

    public VkDescriptorSet DescriptorSet => this._descriptorAllocationToken.Set;
    public List<VkTexture> SampledTextures { get; } = new();

    public List<VkTexture> StorageTextures { get; } = new();

    public ResourceRefCount RefCount { get; }
    public List<ResourceRefCount> RefCounts { get; } = new();

    public override bool IsDisposed => this._destroyed;

    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    private void DisposeCore() {
        if (!this._destroyed) {
            this._destroyed = true;
            this.gd.DescriptorPoolManager.Free(this._descriptorAllocationToken, this._descriptorCounts);
        }
    }
}