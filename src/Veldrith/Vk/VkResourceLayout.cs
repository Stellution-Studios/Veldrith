using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

internal unsafe class VkResourceLayout : ResourceLayout {
    private readonly VkDescriptorSetLayout _dsl;

    private readonly VkGraphicsDevice gd;
    private bool _disposed;
    private string _name;

    public VkResourceLayout(VkGraphicsDevice gd, ref ResourceLayoutDescription description)
        : base(ref description) {
        this.gd = gd;
        VkDescriptorSetLayoutCreateInfo dslCi = VkDescriptorSetLayoutCreateInfo.New();
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

        this.DescriptorResourceCounts = new DescriptorResourceCounts(
            uniformBufferCount,
            uniformBufferDynamicCount,
            sampledImageCount,
            samplerCount,
            storageBufferCount,
            storageBufferDynamicCount,
            storageImageCount);

        dslCi.bindingCount = (uint)elements.Length;
        dslCi.pBindings = bindings;

        VkResult result = vkCreateDescriptorSetLayout(this.gd.Device, ref dslCi, null, out this._dsl);
        CheckResult(result);
    }

    public VkDescriptorSetLayout DescriptorSetLayout => this._dsl;
    public VkDescriptorType[] DescriptorTypes { get; }

    public DescriptorResourceCounts DescriptorResourceCounts { get; }
    public new int DynamicBufferCount { get; }

    public override bool IsDisposed => this._disposed;

    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            vkDestroyDescriptorSetLayout(this.gd.Device, this._dsl, null);
        }
    }

    #endregion
}