using System.Collections.Generic;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

internal unsafe class VkResourceSet : ResourceSet {

    /// <summary>
    /// Represents the _descriptorAllocationToken field.
    /// </summary>
    private readonly DescriptorAllocationToken _descriptorAllocationToken;

    /// <summary>
    /// Represents the _descriptorCounts field.
    /// </summary>
    private readonly DescriptorResourceCounts _descriptorCounts;

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Represents the _destroyed field.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Represents the _name field.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkResourceSet" /> class.
    /// </summary>
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
                                                       || type == VkDescriptorType.StorageBuffer || type == VkDescriptorType.StorageBufferDynamic) {
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

    /// <summary>
    /// Represents the DescriptorSet field.
    /// </summary>
    public VkDescriptorSet DescriptorSet => this._descriptorAllocationToken.Set;

    /// <summary>
    /// Gets or sets SampledTextures.
    /// </summary>
    public List<VkTexture> SampledTextures { get; } = new();

    /// <summary>
    /// Gets or sets StorageTextures.
    /// </summary>
    public List<VkTexture> StorageTextures { get; } = new();

    /// <summary>
    /// Gets or sets RefCount.
    /// </summary>
    public ResourceRefCount RefCount { get; }

    /// <summary>
    /// Gets or sets RefCounts.
    /// </summary>
    public List<ResourceRefCount> RefCounts { get; } = new();

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._destroyed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    /// <summary>
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes DisposeCore.
    /// </summary>
    private void DisposeCore() {
        if (!this._destroyed) {
            this._destroyed = true;
            this.gd.DescriptorPoolManager.Free(this._descriptorAllocationToken, this._descriptorCounts);
        }
    }
}