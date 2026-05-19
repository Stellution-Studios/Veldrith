using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrith.Vk.VulkanUtil;

namespace Veldrith.Vk;

internal unsafe class VkDeviceMemoryManager : IDisposable {
    private const ulong _min_dedicated_allocation_size_dynamic = 1024 * 1024 * 64;
    private const ulong _min_dedicated_allocation_size_non_dynamic = 1024 * 1024 * 256;
    private readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryType = new();
    private readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryTypeUnmapped = new();
    private readonly ulong bufferImageGranularity;
    private readonly VkDevice device;

    private readonly VkGetBufferMemoryRequirements2T getBufferMemoryRequirements2;
    private readonly VkGetImageMemoryRequirements2T getImageMemoryRequirements2;
    private readonly object @lock = new();

    public VkDeviceMemoryManager(
        VkDevice device,
        ulong bufferImageGranularity,
        VkGetBufferMemoryRequirements2T getBufferMemoryRequirements2,
        VkGetImageMemoryRequirements2T getImageMemoryRequirements2) {
        this.device = device;
        this.bufferImageGranularity = bufferImageGranularity;
        this.getBufferMemoryRequirements2 = getBufferMemoryRequirements2;
        this.getImageMemoryRequirements2 = getImageMemoryRequirements2;
    }

    #region Disposal

    public void Dispose() {
        foreach (KeyValuePair<uint, ChunkAllocatorSet> kvp in this._allocatorsByMemoryType) {
            kvp.Value.Dispose();
        }

        foreach (KeyValuePair<uint, ChunkAllocatorSet> kvp in this._allocatorsByMemoryTypeUnmapped) {
            kvp.Value.Dispose();
        }
    }

    #endregion

    public VkMemoryBlock Allocate(
        VkPhysicalDeviceMemoryProperties memProperties,
        uint memoryTypeBits,
        VkMemoryPropertyFlags flags,
        bool persistentMapped,
        ulong size,
        ulong alignment) {
        return this.Allocate(
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
        Vulkan.VkBuffer dedicatedBuffer) {
        if (dedicated) {
            if (dedicatedImage != VkImage.Null && this.getImageMemoryRequirements2 != null) {
                VkImageMemoryRequirementsInfo2KHR requirementsInfo = VkImageMemoryRequirementsInfo2KHR.New();
                requirementsInfo.image = dedicatedImage;
                VkMemoryRequirements2KHR requirements = VkMemoryRequirements2KHR.New();
                this.getImageMemoryRequirements2(this.device, &requirementsInfo, &requirements);
                size = requirements.memoryRequirements.size;
            }
            else if (dedicatedBuffer != Vulkan.VkBuffer.Null && this.getBufferMemoryRequirements2 != null) {
                VkBufferMemoryRequirementsInfo2KHR requirementsInfo = VkBufferMemoryRequirementsInfo2KHR.New();
                requirementsInfo.buffer = dedicatedBuffer;
                VkMemoryRequirements2KHR requirements = VkMemoryRequirements2KHR.New();
                this.getBufferMemoryRequirements2(this.device, &requirementsInfo, &requirements);
                size = requirements.memoryRequirements.size;
            }
        }
        else {
            // Round up to the nearest multiple of bufferImageGranularity.
            size = (size / this.bufferImageGranularity + 1) * this.bufferImageGranularity;
        }

        lock (this.@lock) {
            if (!TryFindMemoryType(memProperties, memoryTypeBits, flags, out uint memoryTypeIndex)) {
                throw new VeldridException("No suitable memory type.");
            }

            ulong minDedicatedAllocationSize = persistentMapped
                ? _min_dedicated_allocation_size_dynamic
                : _min_dedicated_allocation_size_non_dynamic;

            if (dedicated || size >= minDedicatedAllocationSize) {
                VkMemoryAllocateInfo allocateInfo = VkMemoryAllocateInfo.New();
                allocateInfo.allocationSize = size;
                allocateInfo.memoryTypeIndex = memoryTypeIndex;

                // ReSharper disable once TooWideLocalVariableScope
                VkMemoryDedicatedAllocateInfoKHR dedicatedAi;

                if (dedicated) {
                    dedicatedAi = VkMemoryDedicatedAllocateInfoKHR.New();
                    dedicatedAi.buffer = dedicatedBuffer;
                    dedicatedAi.image = dedicatedImage;
                    allocateInfo.pNext = &dedicatedAi;
                }

                VkResult allocationResult =
                    vkAllocateMemory(this.device, ref allocateInfo, null, out VkDeviceMemory memory);
                if (allocationResult != VkResult.Success) {
                    throw new VeldridException("Unable to allocate sufficient Vulkan memory.");
                }

                void* mappedPtr = null;

                if (persistentMapped) {
                    VkResult mapResult = vkMapMemory(this.device, memory, 0, size, 0, &mappedPtr);
                    if (mapResult != VkResult.Success) {
                        throw new VeldridException("Unable to map newly-allocated Vulkan memory.");
                    }
                }

                return new VkMemoryBlock(memory, 0, size, memoryTypeBits, mappedPtr, true);
            }

            ChunkAllocatorSet allocator = this.GetAllocator(memoryTypeIndex, persistentMapped);
            bool result = allocator.Allocate(size, alignment, out VkMemoryBlock ret);
            if (!result) {
                throw new VeldridException("Unable to allocate sufficient Vulkan memory.");
            }

            return ret;
        }
    }

    public void Free(VkMemoryBlock block) {
        lock (this.@lock) {
            if (block.DedicatedAllocation) {
                vkFreeMemory(this.device, block.DeviceMemory, null);
            }
            else {
                this.GetAllocator(block.MemoryTypeIndex, block.IsPersistentMapped).Free(block);
            }
        }
    }

    internal IntPtr Map(VkMemoryBlock memoryBlock) {
        void* ret;
        VkResult result = vkMapMemory(this.device, memoryBlock.DeviceMemory, memoryBlock.Offset, memoryBlock.Size, 0,
            &ret);
        CheckResult(result);
        return (IntPtr)ret;
    }

    private ChunkAllocatorSet GetAllocator(uint memoryTypeIndex, bool persistentMapped) {
        ChunkAllocatorSet ret;

        if (persistentMapped) {
            if (!this._allocatorsByMemoryType.TryGetValue(memoryTypeIndex, out ret)) {
                ret = new ChunkAllocatorSet(this.device, memoryTypeIndex, true);
                this._allocatorsByMemoryType.Add(memoryTypeIndex, ret);
            }
        }
        else {
            if (!this._allocatorsByMemoryTypeUnmapped.TryGetValue(memoryTypeIndex, out ret)) {
                ret = new ChunkAllocatorSet(this.device, memoryTypeIndex, false);
                this._allocatorsByMemoryTypeUnmapped.Add(memoryTypeIndex, ret);
            }
        }

        return ret;
    }

    private class ChunkAllocatorSet : IDisposable {
        private readonly List<ChunkAllocator> _allocators = new();
        private readonly VkDevice device;
        private readonly uint memoryTypeIndex;
        private readonly bool persistentMapped;

        public ChunkAllocatorSet(VkDevice device, uint memoryTypeIndex, bool persistentMapped) {
            this.device = device;
            this.memoryTypeIndex = memoryTypeIndex;
            this.persistentMapped = persistentMapped;
        }

        #region Disposal

        public void Dispose() {
            foreach (ChunkAllocator allocator in this._allocators) {
                allocator.Dispose();
            }
        }

        #endregion

        public bool Allocate(ulong size, ulong alignment, out VkMemoryBlock block) {
            foreach (ChunkAllocator allocator in this._allocators) {
                if (allocator.Allocate(size, alignment, out block)) {
                    return true;
                }
            }

            ChunkAllocator newAllocator = new(this.device, this.memoryTypeIndex, this.persistentMapped);
            this._allocators.Add(newAllocator);
            return newAllocator.Allocate(size, alignment, out block);
        }

        public void Free(VkMemoryBlock block) {
            foreach (ChunkAllocator chunk in this._allocators) {
                if (chunk.Memory == block.DeviceMemory) {
                    chunk.Free(block);
                }
            }
        }
    }

    private class ChunkAllocator : IDisposable {
        public VkDeviceMemory Memory => this.memory;
        private const ulong _persistent_mapped_chunk_size = 1024 * 1024 * 64;
        private const ulong _unmapped_chunk_size = 1024 * 1024 * 256;
        private readonly VkDevice device;
        private readonly uint memoryTypeIndex;
        private readonly List<VkMemoryBlock> _freeBlocks = new();
        private readonly VkDeviceMemory memory;
        private readonly void* mappedPtr;

        public ChunkAllocator(VkDevice device, uint memoryTypeIndex, bool persistentMapped) {
            this.device = device;
            this.memoryTypeIndex = memoryTypeIndex;
            ulong totalMemorySize = persistentMapped ? _persistent_mapped_chunk_size : _unmapped_chunk_size;

            VkMemoryAllocateInfo memoryAi = VkMemoryAllocateInfo.New();
            memoryAi.allocationSize = totalMemorySize;
            memoryAi.memoryTypeIndex = this.memoryTypeIndex;
            VkResult result = vkAllocateMemory(this.device, ref memoryAi, null, out this.memory);
            CheckResult(result);

            if (persistentMapped) {
                void* ptr = null;

                result = vkMapMemory(this.device, this.memory, 0, totalMemorySize, 0, &ptr);
                CheckResult(result);

                this.mappedPtr = ptr;
            }

            VkMemoryBlock initialBlock = new(this.memory,
                0,
                totalMemorySize,
                this.memoryTypeIndex, this.mappedPtr,
                false);
            this._freeBlocks.Add(initialBlock);
        }

        #region Disposal

        public void Dispose() {
            vkFreeMemory(this.device, this.memory, null);
        }

        #endregion

        public bool Allocate(ulong size, ulong alignment, out VkMemoryBlock block) {
            checked {
                for (int i = 0; i < this._freeBlocks.Count; i++) {
                    VkMemoryBlock freeBlock = this._freeBlocks[i];
                    ulong alignedBlockSize = freeBlock.Size;

                    if (freeBlock.Offset % alignment != 0) {
                        ulong alignmentCorrection = alignment - freeBlock.Offset % alignment;
                        if (alignedBlockSize <= alignmentCorrection) {
                            continue;
                        }

                        alignedBlockSize -= alignmentCorrection;
                    }

                    if (alignedBlockSize >= size) // Valid match -- split it and return.
                    {
                        this._freeBlocks.RemoveAt(i);

                        freeBlock.Size = alignedBlockSize;
                        if (freeBlock.Offset % alignment != 0) {
                            freeBlock.Offset += alignment - freeBlock.Offset % alignment;
                        }

                        block = freeBlock;

                        if (alignedBlockSize != size) {
                            VkMemoryBlock splitBlock = new(
                                freeBlock.DeviceMemory,
                                freeBlock.Offset + size,
                                freeBlock.Size - size, this.memoryTypeIndex,
                                freeBlock.BaseMappedPointer,
                                false);
                            this._freeBlocks.Insert(i, splitBlock);
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

        public void Free(VkMemoryBlock block) {
            for (int i = 0; i < this._freeBlocks.Count; i++) {
                if (this._freeBlocks[i].Offset > block.Offset) {
                    this._freeBlocks.Insert(i, block);
                    this.MergeContiguousBlocks();
#if DEBUG
                        removeAllocatedBlock(block);
#endif
                    return;
                }
            }

            this._freeBlocks.Add(block);
#if DEBUG
                removeAllocatedBlock(block);
#endif
        }

        private void MergeContiguousBlocks() {
            int contiguousLength = 1;

            for (int i = 0; i < this._freeBlocks.Count - 1; i++) {
                ulong blockStart = this._freeBlocks[i].Offset;
                while (i + contiguousLength < this._freeBlocks.Count
                       && this._freeBlocks[i + contiguousLength - 1].End ==
                       this._freeBlocks[i + contiguousLength].Offset) {
                    contiguousLength += 1;
                }

                if (contiguousLength > 1) {
                    ulong blockEnd = this._freeBlocks[i + contiguousLength - 1].End;
                    this._freeBlocks.RemoveRange(i, contiguousLength);
                    VkMemoryBlock mergedBlock = new(this.Memory,
                        blockStart,
                        blockEnd - blockStart, this.memoryTypeIndex, this.mappedPtr,
                        false);
                    this._freeBlocks.Insert(i, mergedBlock);
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
internal unsafe struct VkMemoryBlock : IEquatable<VkMemoryBlock> {
    public readonly uint MemoryTypeIndex;
    public readonly VkDeviceMemory DeviceMemory;
    public readonly void* BaseMappedPointer;
    public readonly bool DedicatedAllocation;

    public ulong Offset;
    public ulong Size;

    public void* BlockMappedPointer => (byte*)this.BaseMappedPointer + this.Offset;
    public bool IsPersistentMapped => this.BaseMappedPointer != null;
    public ulong End => this.Offset + this.Size;

    public VkMemoryBlock(
        VkDeviceMemory memory,
        ulong offset,
        ulong size,
        uint memoryTypeIndex,
        void* mappedPtr,
        bool dedicatedAllocation) {
        this.DeviceMemory = memory;
        this.Offset = offset;
        this.Size = size;
        this.MemoryTypeIndex = memoryTypeIndex;
        this.BaseMappedPointer = mappedPtr;
        this.DedicatedAllocation = dedicatedAllocation;
    }

    public bool Equals(VkMemoryBlock other) {
        return this.DeviceMemory.Equals(other.DeviceMemory)
               && this.Offset.Equals(other.Offset)
               && this.Size.Equals(other.Size);
    }
}