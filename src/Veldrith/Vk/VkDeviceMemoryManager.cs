using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk
{
    internal unsafe class VkDeviceMemoryManager : IDisposable
    {
        private const ulong _min_dedicated_allocation_size_dynamic = 1024 * 1024 * 64;
        private const ulong _min_dedicated_allocation_size_non_dynamic = 1024 * 1024 * 256;
        private readonly VkDevice device;
        private readonly ulong bufferImageGranularity;
        private readonly object @lock = new object();
        private readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryTypeUnmapped = new Dictionary<uint, ChunkAllocatorSet>();
        private readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryType = new Dictionary<uint, ChunkAllocatorSet>();

        private readonly VkGetBufferMemoryRequirements2T getBufferMemoryRequirements2;
        private readonly VkGetImageMemoryRequirements2T getImageMemoryRequirements2;

        public VkDeviceMemoryManager(
            VkDevice device,
            ulong bufferImageGranularity,
            VkGetBufferMemoryRequirements2T getBufferMemoryRequirements2,
            VkGetImageMemoryRequirements2T getImageMemoryRequirements2)
        {
            this.device = device;
            this.bufferImageGranularity = bufferImageGranularity;
            this.getBufferMemoryRequirements2 = getBufferMemoryRequirements2;
            this.getImageMemoryRequirements2 = getImageMemoryRequirements2;
        }

        #region Disposal

        public void Dispose()
        {
            foreach (var kvp in _allocatorsByMemoryType) kvp.Value.Dispose();

            foreach (var kvp in _allocatorsByMemoryTypeUnmapped) kvp.Value.Dispose();
        }

        #endregion

        public VkMemoryBlock Allocate(
            VkPhysicalDeviceMemoryProperties memProperties,
            uint memoryTypeBits,
            VkMemoryPropertyFlags flags,
            bool persistentMapped,
            ulong size,
            ulong alignment)
        {
            return Allocate(
                memProperties,
                memoryTypeBits,
                flags,
                persistentMapped,
                size,
                alignment,
                false,
                VkImage.Null,
                Vulkan.VkBuffer.Null);
        }

        public VkMemoryBlock Allocate(
            VkPhysicalDeviceMemoryProperties memProperties,
            uint memoryTypeBits,
            VkMemoryPropertyFlags flags,
            bool persistentMapped,
            ulong size,
            ulong alignment,
            bool dedicated,
            VkImage dedicatedImage,
            Vulkan.VkBuffer dedicatedBuffer)
        {
            if (dedicated)
            {
                if (dedicatedImage != VkImage.Null && getImageMemoryRequirements2 != null)
                {
                    var requirementsInfo = VkImageMemoryRequirementsInfo2KHR.New();
                    requirementsInfo.image = dedicatedImage;
                    var requirements = VkMemoryRequirements2KHR.New();
                    getImageMemoryRequirements2(device, &requirementsInfo, &requirements);
                    size = requirements.memoryRequirements.size;
                }
                else if (dedicatedBuffer != Vulkan.VkBuffer.Null && getBufferMemoryRequirements2 != null)
                {
                    var requirementsInfo = VkBufferMemoryRequirementsInfo2KHR.New();
                    requirementsInfo.buffer = dedicatedBuffer;
                    var requirements = VkMemoryRequirements2KHR.New();
                    getBufferMemoryRequirements2(device, &requirementsInfo, &requirements);
                    size = requirements.memoryRequirements.size;
                }
            }
            else
            {
                // Round up to the nearest multiple of bufferImageGranularity.
                size = (size / bufferImageGranularity + 1) * bufferImageGranularity;
            }

            lock (@lock)
            {
                if (!TryFindMemoryType(memProperties, memoryTypeBits, flags, out uint memoryTypeIndex)) throw new VeldridException("No suitable memory type.");

                ulong minDedicatedAllocationSize = persistentMapped
                    ? _min_dedicated_allocation_size_dynamic
                    : _min_dedicated_allocation_size_non_dynamic;

                if (dedicated || size >= minDedicatedAllocationSize)
                {
                    var allocateInfo = VkMemoryAllocateInfo.New();
                    allocateInfo.allocationSize = size;
                    allocateInfo.memoryTypeIndex = memoryTypeIndex;

                    // ReSharper disable once TooWideLocalVariableScope
                    VkMemoryDedicatedAllocateInfoKHR dedicatedAi;

                    if (dedicated)
                    {
                        dedicatedAi = VkMemoryDedicatedAllocateInfoKHR.New();
                        dedicatedAi.buffer = dedicatedBuffer;
                        dedicatedAi.image = dedicatedImage;
                        allocateInfo.pNext = &dedicatedAi;
                    }

                    var allocationResult = vkAllocateMemory(device, ref allocateInfo, null, out var memory);
                    if (allocationResult != VkResult.Success) throw new VeldridException("Unable to allocate sufficient Vulkan memory.");

                    void* mappedPtr = null;

                    if (persistentMapped)
                    {
                        var mapResult = vkMapMemory(device, memory, 0, size, 0, &mappedPtr);
                        if (mapResult != VkResult.Success) throw new VeldridException("Unable to map newly-allocated Vulkan memory.");
                    }

                    return new VkMemoryBlock(memory, 0, size, memoryTypeBits, mappedPtr, true);
                }

                var allocator = getAllocator(memoryTypeIndex, persistentMapped);
                bool result = allocator.Allocate(size, alignment, out var ret);
                if (!result) throw new VeldridException("Unable to allocate sufficient Vulkan memory.");

                return ret;
            }
        }

        public void Free(VkMemoryBlock block)
        {
            lock (@lock)
            {
                if (block.DedicatedAllocation)
                    vkFreeMemory(device, block.DeviceMemory, null);
                else
                    getAllocator(block.MemoryTypeIndex, block.IsPersistentMapped).Free(block);
            }
        }

        internal IntPtr Map(VkMemoryBlock memoryBlock)
        {
            void* ret;
            var result = vkMapMemory(device, memoryBlock.DeviceMemory, memoryBlock.Offset, memoryBlock.Size, 0, &ret);
            CheckResult(result);
            return (IntPtr)ret;
        }

        private ChunkAllocatorSet getAllocator(uint memoryTypeIndex, bool persistentMapped)
        {
            ChunkAllocatorSet ret;

            if (persistentMapped)
            {
                if (!_allocatorsByMemoryType.TryGetValue(memoryTypeIndex, out ret))
                {
                    ret = new ChunkAllocatorSet(device, memoryTypeIndex, true);
                    _allocatorsByMemoryType.Add(memoryTypeIndex, ret);
                }
            }
            else
            {
                if (!_allocatorsByMemoryTypeUnmapped.TryGetValue(memoryTypeIndex, out ret))
                {
                    ret = new ChunkAllocatorSet(device, memoryTypeIndex, false);
                    _allocatorsByMemoryTypeUnmapped.Add(memoryTypeIndex, ret);
                }
            }

            return ret;
        }

        private class ChunkAllocatorSet : IDisposable
        {
            private readonly VkDevice device;
            private readonly uint memoryTypeIndex;
            private readonly bool persistentMapped;
            private readonly List<ChunkAllocator> _allocators = new List<ChunkAllocator>();

            public ChunkAllocatorSet(VkDevice device, uint memoryTypeIndex, bool persistentMapped)
            {
                this.device = device;
                this.memoryTypeIndex = memoryTypeIndex;
                this.persistentMapped = persistentMapped;
            }

            #region Disposal

            public void Dispose()
            {
                foreach (var allocator in _allocators) allocator.Dispose();
            }

            #endregion

            public bool Allocate(ulong size, ulong alignment, out VkMemoryBlock block)
            {
                foreach (var allocator in _allocators)
                {
                    if (allocator.Allocate(size, alignment, out block))
                        return true;
                }

                var newAllocator = new ChunkAllocator(device, memoryTypeIndex, persistentMapped);
                _allocators.Add(newAllocator);
                return newAllocator.Allocate(size, alignment, out block);
            }

            public void Free(VkMemoryBlock block)
            {
                foreach (var chunk in _allocators)
                {
                    if (chunk.Memory == block.DeviceMemory)
                        chunk.Free(block);
                }
            }
        }

        private class ChunkAllocator : IDisposable
        {
            public VkDeviceMemory Memory => memory;
            private const ulong _persistent_mapped_chunk_size = 1024 * 1024 * 64;
            private const ulong _unmapped_chunk_size = 1024 * 1024 * 256;
            private readonly VkDevice device;
            private readonly uint memoryTypeIndex;
            private readonly List<VkMemoryBlock> _freeBlocks = new List<VkMemoryBlock>();
            private readonly VkDeviceMemory memory;
            private readonly void* mappedPtr;

            public ChunkAllocator(VkDevice device, uint memoryTypeIndex, bool persistentMapped)
            {
                this.device = device;
                this.memoryTypeIndex = memoryTypeIndex;
                ulong totalMemorySize = persistentMapped ? _persistent_mapped_chunk_size : _unmapped_chunk_size;

                var memoryAi = VkMemoryAllocateInfo.New();
                memoryAi.allocationSize = totalMemorySize;
                memoryAi.memoryTypeIndex = this.memoryTypeIndex;
                var result = vkAllocateMemory(this.device, ref memoryAi, null, out memory);
                CheckResult(result);

                if (persistentMapped)
                {
                    void* ptr = null;

                    result = vkMapMemory(this.device, memory, 0, totalMemorySize, 0, &ptr);
                    CheckResult(result);

                    mappedPtr = ptr;
                }

                var initialBlock = new VkMemoryBlock(
                    memory,
                    0,
                    totalMemorySize,
                    this.memoryTypeIndex,
                    mappedPtr,
                    false);
                _freeBlocks.Add(initialBlock);
            }

            #region Disposal

            public void Dispose()
            {
                vkFreeMemory(device, memory, null);
            }

            #endregion

            public bool Allocate(ulong size, ulong alignment, out VkMemoryBlock block)
            {
                checked
                {
                    for (int i = 0; i < _freeBlocks.Count; i++)
                    {
                        var freeBlock = _freeBlocks[i];
                        ulong alignedBlockSize = freeBlock.Size;

                        if (freeBlock.Offset % alignment != 0)
                        {
                            ulong alignmentCorrection = alignment - freeBlock.Offset % alignment;
                            if (alignedBlockSize <= alignmentCorrection) continue;

                            alignedBlockSize -= alignmentCorrection;
                        }

                        if (alignedBlockSize >= size) // Valid match -- split it and return.
                        {
                            _freeBlocks.RemoveAt(i);

                            freeBlock.Size = alignedBlockSize;
                            if (freeBlock.Offset % alignment != 0) freeBlock.Offset += alignment - freeBlock.Offset % alignment;

                            block = freeBlock;

                            if (alignedBlockSize != size)
                            {
                                var splitBlock = new VkMemoryBlock(
                                    freeBlock.DeviceMemory,
                                    freeBlock.Offset + size,
                                    freeBlock.Size - size,
                                    memoryTypeIndex,
                                    freeBlock.BaseMappedPointer,
                                    false);
                                _freeBlocks.Insert(i, splitBlock);
                                block = freeBlock;
                                block.Size = size;
                            }

#if DEBUG
                            checkAllocatedBlock(block);
#endif
                            return true;
                        }
                    }

                    block = default;
                    return false;
                }
            }

            public void Free(VkMemoryBlock block)
            {
                for (int i = 0; i < _freeBlocks.Count; i++)
                {
                    if (_freeBlocks[i].Offset > block.Offset)
                    {
                        _freeBlocks.Insert(i, block);
                        mergeContiguousBlocks();
#if DEBUG
                        removeAllocatedBlock(block);
#endif
                        return;
                    }
                }

                _freeBlocks.Add(block);
#if DEBUG
                removeAllocatedBlock(block);
#endif
            }

            private void mergeContiguousBlocks()
            {
                int contiguousLength = 1;

                for (int i = 0; i < _freeBlocks.Count - 1; i++)
                {
                    ulong blockStart = _freeBlocks[i].Offset;
                    while (i + contiguousLength < _freeBlocks.Count
                           && _freeBlocks[i + contiguousLength - 1].End == _freeBlocks[i + contiguousLength].Offset)
                        contiguousLength += 1;

                    if (contiguousLength > 1)
                    {
                        ulong blockEnd = _freeBlocks[i + contiguousLength - 1].End;
                        _freeBlocks.RemoveRange(i, contiguousLength);
                        var mergedBlock = new VkMemoryBlock(
                            Memory,
                            blockStart,
                            blockEnd - blockStart,
                            memoryTypeIndex,
                            mappedPtr,
                            false);
                        _freeBlocks.Insert(i, mergedBlock);
                        contiguousLength = 0;
                    }
                }
            }

#if DEBUG
            private readonly List<VkMemoryBlock> allocatedBlocks = new List<VkMemoryBlock>();

            private void checkAllocatedBlock(VkMemoryBlock block)
            {
                foreach (var oldBlock in allocatedBlocks) Debug.Assert(!blocksOverlap(block, oldBlock), "Allocated blocks have overlapped.");

                allocatedBlocks.Add(block);
            }

            private bool blocksOverlap(VkMemoryBlock first, VkMemoryBlock second)
            {
                ulong firstStart = first.Offset;
                ulong firstEnd = first.Offset + first.Size;
                ulong secondStart = second.Offset;
                ulong secondEnd = second.Offset + second.Size;

                return (firstStart <= secondStart && firstEnd > secondStart)
                       || (firstStart >= secondStart && firstEnd <= secondEnd)
                       || (firstStart < secondEnd && firstEnd >= secondEnd)
                       || (firstStart <= secondStart && firstEnd >= secondEnd);
            }

            private void removeAllocatedBlock(VkMemoryBlock block)
            {
                Debug.Assert(allocatedBlocks.Remove(block), "Unable to remove a supposedly allocated block.");
            }
#endif
        }
    }

    [DebuggerDisplay("[Mem:{DeviceMemory.Handle}] Off:{Offset}, Size:{Size} End:{Offset+Size}")]
    internal unsafe struct VkMemoryBlock : IEquatable<VkMemoryBlock>
    {
        public readonly uint MemoryTypeIndex;
        public readonly VkDeviceMemory DeviceMemory;
        public readonly void* BaseMappedPointer;
        public readonly bool DedicatedAllocation;

        public ulong Offset;
        public ulong Size;

        public void* BlockMappedPointer => (byte*)BaseMappedPointer + Offset;
        public bool IsPersistentMapped => BaseMappedPointer != null;
        public ulong End => Offset + Size;

        public VkMemoryBlock(
            VkDeviceMemory memory,
            ulong offset,
            ulong size,
            uint memoryTypeIndex,
            void* mappedPtr,
            bool dedicatedAllocation)
        {
            DeviceMemory = memory;
            Offset = offset;
            Size = size;
            MemoryTypeIndex = memoryTypeIndex;
            BaseMappedPointer = mappedPtr;
            DedicatedAllocation = dedicatedAllocation;
        }

        public bool Equals(VkMemoryBlock other)
        {
            return DeviceMemory.Equals(other.DeviceMemory)
                   && Offset.Equals(other.Offset)
                   && Size.Equals(other.Size);
        }
    }
}
