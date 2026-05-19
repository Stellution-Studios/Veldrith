using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

internal unsafe class VkBuffer : DeviceBuffer {

    /// <summary>
    /// Represents the _bufferMemoryRequirements field.
    /// </summary>
    private readonly VkMemoryRequirements _bufferMemoryRequirements;

    /// <summary>
    /// Represents the _deviceBuffer field.
    /// </summary>
    private readonly Vulkan.VkBuffer _deviceBuffer;

    /// <summary>
    /// Represents the _memory field.
    /// </summary>
    private readonly VkMemoryBlock _memory;

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
    /// Initializes a new instance of the <see cref="VkBuffer" /> class.
    /// </summary>
    public VkBuffer(VkGraphicsDevice gd, uint sizeInBytes, BufferUsage usage, string callerMember = null) {
        this.gd = gd;
        this.SizeInBytes = sizeInBytes;
        this.Usage = usage;

        VkBufferUsageFlags vkUsage = VkBufferUsageFlags.TransferSrc | VkBufferUsageFlags.TransferDst;
        if ((usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer) {
            vkUsage |= VkBufferUsageFlags.VertexBuffer;
        }

        if ((usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer) {
            vkUsage |= VkBufferUsageFlags.IndexBuffer;
        }

        if ((usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer) {
            vkUsage |= VkBufferUsageFlags.UniformBuffer;
        }

        if ((usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite
            || (usage & BufferUsage.StructuredBufferReadOnly) == BufferUsage.StructuredBufferReadOnly) {
            vkUsage |= VkBufferUsageFlags.StorageBuffer;
        }

        if ((usage & BufferUsage.IndirectBuffer) == BufferUsage.IndirectBuffer) {
            vkUsage |= VkBufferUsageFlags.IndirectBuffer;
        }

        VkBufferCreateInfo bufferCi = VkBufferCreateInfo.New();
        bufferCi.size = sizeInBytes;
        bufferCi.usage = vkUsage;
        VkResult result = vkCreateBuffer(gd.Device, ref bufferCi, null, out this._deviceBuffer);
        CheckResult(result);

        bool prefersDedicatedAllocation;

        if (this.gd.GetBufferMemoryRequirements2 != null) {
            VkBufferMemoryRequirementsInfo2KHR memReqInfo2 = VkBufferMemoryRequirementsInfo2KHR.New();
            memReqInfo2.buffer = this._deviceBuffer;
            VkMemoryRequirements2KHR memReqs2 = VkMemoryRequirements2KHR.New();
            VkMemoryDedicatedRequirementsKHR dedicatedReqs = VkMemoryDedicatedRequirementsKHR.New();
            memReqs2.pNext = &dedicatedReqs;
            this.gd.GetBufferMemoryRequirements2(this.gd.Device, &memReqInfo2, &memReqs2);
            this._bufferMemoryRequirements = memReqs2.memoryRequirements;
            prefersDedicatedAllocation = dedicatedReqs.prefersDedicatedAllocation || dedicatedReqs.requiresDedicatedAllocation;
        }
        else {
            vkGetBufferMemoryRequirements(gd.Device, this._deviceBuffer, out this._bufferMemoryRequirements);
            prefersDedicatedAllocation = false;
        }

        bool isStaging = (usage & BufferUsage.Staging) == BufferUsage.Staging;
        bool hostVisible = isStaging || (usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;

        VkMemoryPropertyFlags memoryPropertyFlags = hostVisible
                ? VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent
                : VkMemoryPropertyFlags.DeviceLocal;

        if (isStaging) {
            // Use "host cached" memory for staging when available, for better performance of GPU -> CPU transfers
            bool hostCachedAvailable = TryFindMemoryType(gd.PhysicalDeviceMemProperties, this._bufferMemoryRequirements.memoryTypeBits, memoryPropertyFlags | VkMemoryPropertyFlags.HostCached, out _);
            if (hostCachedAvailable) {
                memoryPropertyFlags |= VkMemoryPropertyFlags.HostCached;
            }
        }

        VkMemoryBlock memoryToken = gd.MemoryManager.Allocate(gd.PhysicalDeviceMemProperties, this._bufferMemoryRequirements.memoryTypeBits, memoryPropertyFlags, hostVisible, this._bufferMemoryRequirements.size, this._bufferMemoryRequirements.alignment, prefersDedicatedAllocation, VkImage.Null, this._deviceBuffer);
        this._memory = memoryToken;
        result = vkBindBufferMemory(gd.Device, this._deviceBuffer, this._memory.DeviceMemory, this._memory.Offset);
        CheckResult(result);

        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    /// <summary>
    /// Gets or sets RefCount.
    /// </summary>
    public ResourceRefCount RefCount { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._destroyed;

    /// <summary>
    /// Gets or sets SizeInBytes.
    /// </summary>
    public override uint SizeInBytes { get; }

    /// <summary>
    /// Gets or sets Usage.
    /// </summary>
    public override BufferUsage Usage { get; }

    /// <summary>
    /// Represents the DeviceBuffer field.
    /// </summary>
    public Vulkan.VkBuffer DeviceBuffer => this._deviceBuffer;

    /// <summary>
    /// Represents the Memory field.
    /// </summary>
    public VkMemoryBlock Memory => this._memory;

    /// <summary>
    /// Represents the BufferMemoryRequirements field.
    /// </summary>
    public VkMemoryRequirements BufferMemoryRequirements => this._bufferMemoryRequirements;

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
            vkDestroyBuffer(this.gd.Device, this._deviceBuffer, null);
            this.gd.MemoryManager.Free(this.Memory);
        }
    }
}