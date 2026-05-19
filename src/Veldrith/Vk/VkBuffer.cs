using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk
{
    internal unsafe class VkBuffer : DeviceBuffer
    {
        public ResourceRefCount RefCount { get; }
        public override bool IsDisposed => destroyed;

        public override uint SizeInBytes { get; }
        public override BufferUsage Usage { get; }

        public Vulkan.VkBuffer DeviceBuffer => deviceBuffer;
        public VkMemoryBlock Memory => memory;

        public VkMemoryRequirements BufferMemoryRequirements => bufferMemoryRequirements;

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
        private readonly Vulkan.VkBuffer deviceBuffer;
        private readonly VkMemoryBlock memory;
        private readonly VkMemoryRequirements bufferMemoryRequirements;
        private bool destroyed;
        private string name;

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
            var result = vkCreateBuffer(gd.Device, ref bufferCi, null, out deviceBuffer);
            CheckResult(result);

            bool prefersDedicatedAllocation;

            if (this.gd.GetBufferMemoryRequirements2 != null)
            {
                var memReqInfo2 = VkBufferMemoryRequirementsInfo2KHR.New();
                memReqInfo2.buffer = deviceBuffer;
                var memReqs2 = VkMemoryRequirements2KHR.New();
                var dedicatedReqs = VkMemoryDedicatedRequirementsKHR.New();
                memReqs2.pNext = &dedicatedReqs;
                this.gd.GetBufferMemoryRequirements2(this.gd.Device, &memReqInfo2, &memReqs2);
                bufferMemoryRequirements = memReqs2.memoryRequirements;
                prefersDedicatedAllocation = dedicatedReqs.prefersDedicatedAllocation || dedicatedReqs.requiresDedicatedAllocation;
            }
            else
            {
                vkGetBufferMemoryRequirements(gd.Device, deviceBuffer, out bufferMemoryRequirements);
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
                    bufferMemoryRequirements.memoryTypeBits,
                    memoryPropertyFlags | VkMemoryPropertyFlags.HostCached,
                    out _);
                if (hostCachedAvailable) memoryPropertyFlags |= VkMemoryPropertyFlags.HostCached;
            }

            var memoryToken = gd.MemoryManager.Allocate(
                gd.PhysicalDeviceMemProperties,
                bufferMemoryRequirements.memoryTypeBits,
                memoryPropertyFlags,
                hostVisible,
                bufferMemoryRequirements.size,
                bufferMemoryRequirements.alignment,
                prefersDedicatedAllocation,
                VkImage.Null,
                deviceBuffer);
            memory = memoryToken;
            result = vkBindBufferMemory(gd.Device, deviceBuffer, memory.DeviceMemory, memory.Offset);
            CheckResult(result);

            RefCount = new ResourceRefCount(disposeCore);
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
                vkDestroyBuffer(gd.Device, deviceBuffer, null);
                gd.MemoryManager.Free(Memory);
            }
        }
    }
}
