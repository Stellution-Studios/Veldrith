using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk
{
    internal unsafe class VkBuffer : DeviceBuffer
    {
        public ResourceRefCount RefCount { get; }
        public override bool IsDisposed => this._destroyed;

        public override uint SizeInBytes { get; }
        public override BufferUsage Usage { get; }

        public Vulkan.VkBuffer DeviceBuffer => this._deviceBuffer;
        public VkMemoryBlock Memory => this._memory;

        public VkMemoryRequirements BufferMemoryRequirements => this._bufferMemoryRequirements;

        public override string Name
        {
            get => this._name;
            set
            {
                this._name = value;
                gd.SetResourceName(this, value);
            }
        }

        private readonly VkGraphicsDevice gd;
        private readonly Vulkan.VkBuffer _deviceBuffer;
        private readonly VkMemoryBlock _memory;
        private readonly VkMemoryRequirements _bufferMemoryRequirements;
        private bool _destroyed;
        private string _name;

        public VkBuffer(VkGraphicsDevice gd, uint sizeInBytes, BufferUsage usage, string callerMember = null)
        {
            this.gd = gd;
            SizeInBytes = sizeInBytes;
            Usage = usage;

            var vkUsage = VkBufferUsageFlags.TransferSrc | VkBufferUsageFlags.TransferDst;
            if ((usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer) vkUsage |= VkBufferUsageFlags.VertexBuffer;

            if ((usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer) vkUsage |= VkBufferUsageFlags.IndexBuffer;

            if ((usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer) vkUsage |= VkBufferUsageFlags.UniformBuffer;

            if ((usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite
                || (usage & BufferUsage.StructuredBufferReadOnly) == BufferUsage.StructuredBufferReadOnly)
                vkUsage |= VkBufferUsageFlags.StorageBuffer;

            if ((usage & BufferUsage.IndirectBuffer) == BufferUsage.IndirectBuffer) vkUsage |= VkBufferUsageFlags.IndirectBuffer;

            var bufferCi = VkBufferCreateInfo.New();
            bufferCi.size = sizeInBytes;
            bufferCi.usage = vkUsage;
            var result = vkCreateBuffer(gd.Device, ref bufferCi, null, out this._deviceBuffer);
            CheckResult(result);

            bool prefersDedicatedAllocation;

            if (this.gd.GetBufferMemoryRequirements2 != null)
            {
                var memReqInfo2 = VkBufferMemoryRequirementsInfo2KHR.New();
                memReqInfo2.buffer = this._deviceBuffer;
                var memReqs2 = VkMemoryRequirements2KHR.New();
                var dedicatedReqs = VkMemoryDedicatedRequirementsKHR.New();
                memReqs2.pNext = &dedicatedReqs;
                this.gd.GetBufferMemoryRequirements2(this.gd.Device, &memReqInfo2, &memReqs2);
                this._bufferMemoryRequirements = memReqs2.memoryRequirements;
                prefersDedicatedAllocation = dedicatedReqs.prefersDedicatedAllocation || dedicatedReqs.requiresDedicatedAllocation;
            }
            else
            {
                vkGetBufferMemoryRequirements(gd.Device, this._deviceBuffer, out this._bufferMemoryRequirements);
                prefersDedicatedAllocation = false;
            }

            bool isStaging = (usage & BufferUsage.Staging) == BufferUsage.Staging;
            bool hostVisible = isStaging || (usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;

            var memoryPropertyFlags =
                hostVisible
                    ? VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent
                    : VkMemoryPropertyFlags.DeviceLocal;

            if (isStaging)
            {
                // Use "host cached" memory for staging when available, for better performance of GPU -> CPU transfers
                bool hostCachedAvailable = TryFindMemoryType(
                    gd.PhysicalDeviceMemProperties,
                    this._bufferMemoryRequirements.memoryTypeBits,
                    memoryPropertyFlags | VkMemoryPropertyFlags.HostCached,
                    out _);
                if (hostCachedAvailable) memoryPropertyFlags |= VkMemoryPropertyFlags.HostCached;
            }

            var memoryToken = gd.MemoryManager.Allocate(
                gd.PhysicalDeviceMemProperties,
                this._bufferMemoryRequirements.memoryTypeBits,
                memoryPropertyFlags,
                hostVisible,
                this._bufferMemoryRequirements.size,
                this._bufferMemoryRequirements.alignment,
                prefersDedicatedAllocation,
                VkImage.Null,
                this._deviceBuffer);
            this._memory = memoryToken;
            result = vkBindBufferMemory(gd.Device, this._deviceBuffer, this._memory.DeviceMemory, this._memory.Offset);
            CheckResult(result);

            RefCount = new ResourceRefCount(DisposeCore);
        }

        #region Disposal

        public override void Dispose()
        {
            RefCount.Decrement();
        }

        #endregion

        private void DisposeCore()
        {
            if (!this._destroyed)
            {
                this._destroyed = true;
                vkDestroyBuffer(gd.Device, this._deviceBuffer, null);
                gd.MemoryManager.Free(Memory);
            }
        }
    }
}
