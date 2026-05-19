using System.Collections.Generic;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkResourceSet.
/// </summary>
internal unsafe class VkResourceSet : ResourceSet {

    /// <summary>
    /// Stores the descriptor allocation token state used by this instance.
    /// </summary>
    private readonly DescriptorAllocationToken _descriptorAllocationToken;

    /// <summary>
    /// Stores the descriptor counts value used during command execution.
    /// </summary>
    private readonly DescriptorResourceCounts _descriptorCounts;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the destroyed state used by this instance.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkResourceSet" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public VkResourceSet(VkGraphicsDevice gd, ref ResourceSetDescription description) : base(ref description) {
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
    /// Stores the descriptor set state used by this instance.
    /// </summary>
    public VkDescriptorSet DescriptorSet => this._descriptorAllocationToken.Set;

    /// <summary>
    /// Stores the sampled textures collection used by this instance.
    /// </summary>
    public List<VkTexture> SampledTextures { get; } = new();

    /// <summary>
    /// Stores the storage textures collection used by this instance.
    /// </summary>
    public List<VkTexture> StorageTextures { get; } = new();

    /// <summary>
    /// Gets or sets RefCount.
    /// </summary>
    public ResourceRefCount RefCount { get; }

    /// <summary>
    /// Stores the ref counts value used during command processing.
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
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private void DisposeCore() {
        if (!this._destroyed) {
            this._destroyed = true;
            this.gd.DescriptorPoolManager.Free(this._descriptorAllocationToken, this._descriptorCounts);
        }
    }
}