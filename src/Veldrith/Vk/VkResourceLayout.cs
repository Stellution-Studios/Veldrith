using Vortice.Vulkan;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkResourceLayout.
/// </summary>
internal unsafe class VkResourceLayout : ResourceLayout {

    /// <summary>
    /// Stores the dsl state used by this instance.
    /// </summary>
    private readonly VkDescriptorSetLayout _dsl;

    /// <summary>
    /// Stores the graphics device used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkResourceLayout" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public VkResourceLayout(VkGraphicsDevice gd, ref ResourceLayoutDescription description) : base(ref description) {
        this._gd = gd;
        VkDescriptorSetLayoutCreateInfo dslCi = new VkDescriptorSetLayoutCreateInfo();
        ResourceLayoutElementDescription[] elements = description.Elements;
        this.DescriptorTypes = new VkDescriptorType[elements.Length];
        VkDescriptorSetLayoutBinding* bindings = stackalloc VkDescriptorSetLayoutBinding[elements.Length];

        uint uniformBufferCount = 0;
        uint uniformBufferDynamicCount = 0;
        uint sampledImageCount = 0;
        uint samplerCount = 0;
        uint storageBufferCount = 0;
        uint storageBufferDynamicCount = 0;
        uint storageImageCount = 0;

        for (uint i = 0; i < elements.Length; i++) {
            bindings[i].binding = i;
            bindings[i].descriptorCount = 1;
            VkDescriptorType descriptorType = VkFormats.VdToVkDescriptorType(elements[i].Kind, elements[i].Options);
            bindings[i].descriptorType = descriptorType;
            bindings[i].stageFlags = VkFormats.VdToVkShaderStages(elements[i].Stages);
            if ((elements[i].Options & ResourceLayoutElementOptions.DynamicBinding) != 0) {
                this.DynamicBufferCount += 1;
            }

            this.DescriptorTypes[i] = descriptorType;

            switch (descriptorType) {
                case VkDescriptorType.Sampler:
                    samplerCount += 1;
                    break;

                case VkDescriptorType.SampledImage:
                    sampledImageCount += 1;
                    break;

                case VkDescriptorType.StorageImage:
                    storageImageCount += 1;
                    break;

                case VkDescriptorType.UniformBuffer:
                    uniformBufferCount += 1;
                    break;

                case VkDescriptorType.UniformBufferDynamic:
                    uniformBufferDynamicCount += 1;
                    break;

                case VkDescriptorType.StorageBuffer:
                    storageBufferCount += 1;
                    break;

                case VkDescriptorType.StorageBufferDynamic:
                    storageBufferDynamicCount += 1;
                    break;
            }
        }

        this.DescriptorResourceCounts = new DescriptorResourceCounts(uniformBufferCount, uniformBufferDynamicCount, sampledImageCount, samplerCount, storageBufferCount, storageBufferDynamicCount, storageImageCount);

        dslCi.bindingCount = (uint)elements.Length;
        dslCi.pBindings = bindings;

        VkResult result = this._gd.DeviceApi.vkCreateDescriptorSetLayout(ref dslCi, null, out this._dsl);
        CheckResult(result);
    }

    /// <summary>
    /// Stores the descriptor set layout state used by this instance.
    /// </summary>
    public VkDescriptorSetLayout DescriptorSetLayout => this._dsl;

    /// <summary>
    /// Gets or sets DescriptorTypes.
    /// </summary>
    public VkDescriptorType[] DescriptorTypes { get; }

    /// <summary>
    /// Gets or sets DescriptorResourceCounts.
    /// </summary>
    public DescriptorResourceCounts DescriptorResourceCounts { get; }

    /// <summary>
    /// Gets or sets DynamicBufferCount.
    /// </summary>
    public new int DynamicBufferCount { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this._gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            this._gd.DeviceApi.vkDestroyDescriptorSetLayout(this._dsl, null);
        }
    }

    #endregion
}