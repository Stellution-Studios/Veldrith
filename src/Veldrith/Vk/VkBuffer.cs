using Vortice.Vulkan;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkBuffer.
/// </summary>
internal unsafe class VkBuffer : DeviceBuffer {

    /// <summary>
    /// Stores the buffer memory requirements state used by this instance.
    /// </summary>
    private readonly VkMemoryRequirements _bufferMemoryRequirements;

    /// <summary>
    /// Stores the device buffer state used by this instance.
    /// </summary>
    private readonly global::Vortice.Vulkan.VkBuffer _deviceBuffer;

    /// <summary>
    /// Stores the memory state used by this instance.
    /// </summary>
    private readonly VkMemoryBlock _memory;

    /// <summary>
    /// Stores the graphics device used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

    /// <summary>
    /// Stores the destroyed state used by this instance.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkBuffer" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="callerMember">The caller member value used by this operation.</param>
    public VkBuffer(VkGraphicsDevice gd, uint sizeInBytes, BufferUsage usage, string callerMember = null) {
        this._gd = gd;
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

        VkBufferCreateInfo bufferCi = new VkBufferCreateInfo();
        bufferCi.size = sizeInBytes;
        bufferCi.usage = vkUsage;
        VkResult result = gd.DeviceApi.vkCreateBuffer(ref bufferCi, null, out this._deviceBuffer);
        CheckResult(result);

        bool prefersDedicatedAllocation;

        if (this._gd.GetBufferMemoryRequirements2 != null) {
            VkBufferMemoryRequirementsInfo2 memReqInfo2 = new VkBufferMemoryRequirementsInfo2();
            memReqInfo2.buffer = this._deviceBuffer;
            VkMemoryRequirements2 memReqs2 = new VkMemoryRequirements2();
            VkMemoryDedicatedRequirements dedicatedReqs = new VkMemoryDedicatedRequirements();
            memReqs2.pNext = &dedicatedReqs;
            this._gd.GetBufferMemoryRequirements2(this._gd.Device, &memReqInfo2, &memReqs2);
            this._bufferMemoryRequirements = memReqs2.memoryRequirements;
            prefersDedicatedAllocation = dedicatedReqs.prefersDedicatedAllocation || dedicatedReqs.requiresDedicatedAllocation;
        }
        else {
            gd.DeviceApi.vkGetBufferMemoryRequirements(this._deviceBuffer, out this._bufferMemoryRequirements);
            prefersDedicatedAllocation = false;
        }

        bool isStaging = (usage & BufferUsage.Staging) == BufferUsage.Staging;
        bool isDynamic = (usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
        bool isUniform = (usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer;
        bool preferHostVisibleUniform = isUniform && !isStaging && sizeInBytes <= 4 * 1024 * 1024;
        bool hostVisible = isStaging || isDynamic || preferHostVisibleUniform;

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

        VkMemoryBlock memoryToken = gd.MemoryManager.Allocate(gd.PhysicalDeviceMemProperties, this._bufferMemoryRequirements.memoryTypeBits, memoryPropertyFlags, hostVisible, this._bufferMemoryRequirements.size, this._bufferMemoryRequirements.alignment, prefersDedicatedAllocation, default, this._deviceBuffer);
        this._memory = memoryToken;
        result = gd.DeviceApi.vkBindBufferMemory(this._deviceBuffer, this._memory.DeviceMemory, this._memory.Offset);
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
    /// Stores the device buffer state used by this instance.
    /// </summary>
    public global::Vortice.Vulkan.VkBuffer DeviceBuffer => this._deviceBuffer;

    /// <summary>
    /// Stores the memory state used by this instance.
    /// </summary>
    public VkMemoryBlock Memory => this._memory;

    /// <summary>
    /// Stores the buffer memory requirements state used by this instance.
    /// </summary>
    public VkMemoryRequirements BufferMemoryRequirements => this._bufferMemoryRequirements;

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
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private void DisposeCore() {
        if (!this._destroyed) {
            this._destroyed = true;
            this._gd.DeviceApi.vkDestroyBuffer(this._deviceBuffer, null);
            this._gd.MemoryManager.Free(this.Memory);
        }
    }
}
