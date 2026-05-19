using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vulkan;
using static Veldrith.Vk.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkDeviceMemoryManager.
/// </summary>
internal unsafe class VkDeviceMemoryManager : IDisposable {

    /// <summary>
    /// Stores the min dedicated allocation size dynamic value used during command execution.
    /// </summary>
    private const ulong _min_dedicated_allocation_size_dynamic = 1024 * 1024 * 64;

    /// <summary>
    /// Stores the min dedicated allocation size non dynamic value used during command execution.
    /// </summary>
    private const ulong _min_dedicated_allocation_size_non_dynamic = 1024 * 1024 * 256;

    /// <summary>
    /// Stores the allocators by memory type state used by this instance.
    /// </summary>
    private readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryType = new();

    /// <summary>
    /// Stores the allocators by memory type unmapped state used by this instance.
    /// </summary>
    private readonly Dictionary<uint, ChunkAllocatorSet> _allocatorsByMemoryTypeUnmapped = new();

    /// <summary>
    /// Stores the buffer image granularity state used by this instance.
    /// </summary>
    private readonly ulong bufferImageGranularity;

    /// <summary>
    /// Stores the device state used by this instance.
    /// </summary>
    private readonly VkDevice device;

    /// <summary>
    /// Stores the get buffer memory requirements2 state used by this instance.
    /// </summary>
    private readonly VkGetBufferMemoryRequirements2T getBufferMemoryRequirements2;

    /// <summary>
    /// Stores the get image memory requirements2 state used by this instance.
    /// </summary>
    private readonly VkGetImageMemoryRequirements2T getImageMemoryRequirements2;

    /// <summary>
    /// Creates and returns a new instance.
    /// </summary>
    private readonly object @lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VkDeviceMemoryManager" /> type.
    /// </summary>
    /// <param name="device">The device value used by this operation.</param>
    /// <param name="bufferImageGranularity">The buffer image granularity value used by this operation.</param>
    /// <param name="getBufferMemoryRequirements2">The get buffer memory requirements2 value used by this operation.</param>
    /// <param name="getImageMemoryRequirements2">The get image memory requirements2 value used by this operation.</param>
    public VkDeviceMemoryManager(VkDevice device, ulong bufferImageGranularity, VkGetBufferMemoryRequirements2T getBufferMemoryRequirements2, VkGetImageMemoryRequirements2T getImageMemoryRequirements2) {
        this.device = device;
        this.bufferImageGranularity = bufferImageGranularity;
        this.getBufferMemoryRequirements2 = getBufferMemoryRequirements2;
        this.getImageMemoryRequirements2 = getImageMemoryRequirements2;
    }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public void Dispose() {
        foreach (KeyValuePair<uint, ChunkAllocatorSet> kvp in this._allocatorsByMemoryType) {
            kvp.Value.Dispose();
        }

        foreach (KeyValuePair<uint, ChunkAllocatorSet> kvp in this._allocatorsByMemoryTypeUnmapped) {
            kvp.Value.Dispose();
        }
    }

    #endregion

    /// <summary>
    /// Executes the allocate logic for this backend.
    /// </summary>
    /// <param name="memProperties">The mem properties value used by this operation.</param>
    /// <param name="memoryTypeBits">The memory type bits value used by this operation.</param>
    /// <param name="flags">The flags value used by this operation.</param>
    /// <param name="persistentMapped">The persistent mapped value used by this operation.</param>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    /// <param name="alignment">The alignment value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public VkMemoryBlock Allocate(VkPhysicalDeviceMemoryProperties memProperties, uint memoryTypeBits, VkMemoryPropertyFlags flags, bool persistentMapped, ulong size, ulong alignment) {
        return this.Allocate(memProperties, memoryTypeBits, flags, persistentMapped, size, alignment, false, VkImage.Null, Vulkan.VkBuffer.Null);
    }

    /// <summary>
    /// Executes the allocate logic for this backend.
    /// </summary>
    /// <param name="memProperties">The mem properties value used by this operation.</param>
    /// <param name="memoryTypeBits">The memory type bits value used by this operation.</param>
    /// <param name="flags">The flags value used by this operation.</param>
    /// <param name="persistentMapped">The persistent mapped value used by this operation.</param>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    /// <param name="alignment">The alignment value used by this operation.</param>
    /// <param name="dedicated">The dedicated value used by this operation.</param>
    /// <param name="dedicatedImage">The dedicated image value used by this operation.</param>
    /// <param name="dedicatedBuffer">The dedicated buffer value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public VkMemoryBlock Allocate(VkPhysicalDeviceMemoryProperties memProperties, uint memoryTypeBits, VkMemoryPropertyFlags flags, bool persistentMapped, ulong size, ulong alignment, bool dedicated, VkImage dedicatedImage, Vulkan.VkBuffer dedicatedBuffer) {
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

                VkResult allocationResult = vkAllocateMemory(this.device, ref allocateInfo, null, out VkDeviceMemory memory);
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

    /// <summary>
    /// Executes the free logic for this backend.
    /// </summary>
    /// <param name="block">The block value used by this operation.</param>
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

    /// <summary>
    /// Maps the value resource for CPU access.
    /// </summary>
    /// <param name="memoryBlock">The memory block value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal IntPtr Map(VkMemoryBlock memoryBlock) {
        void* ret;
        VkResult result = vkMapMemory(this.device, memoryBlock.DeviceMemory, memoryBlock.Offset, memoryBlock.Size, 0, &ret);
        CheckResult(result);
        return (IntPtr)ret;
    }

    /// <summary>
    /// Gets the allocator value.
    /// </summary>
    /// <param name="memoryTypeIndex">The memory type index value used by this operation.</param>
    /// <param name="persistentMapped">The persistent mapped value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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

    /// <summary>
    /// Represents the ChunkAllocatorSet type used by the graphics runtime.
    /// </summary>
    private class ChunkAllocatorSet : IDisposable {

        /// <summary>
        /// Stores the allocators state used by this instance.
        /// </summary>
        private readonly List<ChunkAllocator> _allocators = new();

        /// <summary>
        /// Stores the device state used by this instance.
        /// </summary>
        private readonly VkDevice device;

        /// <summary>
        /// Stores the memory type index value used during command execution.
        /// </summary>
        private readonly uint memoryTypeIndex;

        /// <summary>
        /// Stores the persistent mapped state used by this instance.
        /// </summary>
        private readonly bool persistentMapped;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkAllocatorSet" /> type.
        /// </summary>
        /// <param name="device">The device value used by this operation.</param>
        /// <param name="memoryTypeIndex">The memory type index value used by this operation.</param>
        /// <param name="persistentMapped">The persistent mapped value used by this operation.</param>
        public ChunkAllocatorSet(VkDevice device, uint memoryTypeIndex, bool persistentMapped) {
            this.device = device;
            this.memoryTypeIndex = memoryTypeIndex;
            this.persistentMapped = persistentMapped;
        }

        #region Disposal

        /// <summary>
        /// Releases resources held by this instance.
        /// </summary>
        public void Dispose() {
            foreach (ChunkAllocator allocator in this._allocators) {
                allocator.Dispose();
            }
        }

        #endregion

        /// <summary>
        /// Executes the allocate logic for this backend.
        /// </summary>
        /// <param name="size">The size, in bytes, used by this operation.</param>
        /// <param name="alignment">The alignment value used by this operation.</param>
        /// <param name="block">The block value used by this operation.</param>
        /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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

        /// <summary>
        /// Executes the free logic for this backend.
        /// </summary>
        /// <param name="block">The block value used by this operation.</param>
        public void Free(VkMemoryBlock block) {
            foreach (ChunkAllocator chunk in this._allocators) {
                if (chunk.Memory == block.DeviceMemory) {
                    chunk.Free(block);
                }
            }
        }
    }

    /// <summary>
    /// Represents the ChunkAllocator type used by the graphics runtime.
    /// </summary>
    private class ChunkAllocator : IDisposable {

        /// <summary>
        /// Stores the memory state used by this instance.
        /// </summary>
        public VkDeviceMemory Memory => this.memory;

        /// <summary>
        /// Stores the persistent mapped chunk size value used during command execution.
        /// </summary>
        private const ulong _persistent_mapped_chunk_size = 1024 * 1024 * 64;

        /// <summary>
        /// Stores the unmapped chunk size value used during command execution.
        /// </summary>
        private const ulong _unmapped_chunk_size = 1024 * 1024 * 256;

        /// <summary>
        /// Stores the device state used by this instance.
        /// </summary>
        private readonly VkDevice device;

        /// <summary>
        /// Stores the memory type index value used during command execution.
        /// </summary>
        private readonly uint memoryTypeIndex;

        /// <summary>
        /// Synchronizes access to the free blocks state.
        /// </summary>
        private readonly List<VkMemoryBlock> _freeBlocks = new();

        /// <summary>
        /// Stores the memory state used by this instance.
        /// </summary>
        private readonly VkDeviceMemory memory;

        /// <summary>
        /// Stores the mapped ptr state used by this instance.
        /// </summary>
        private readonly void* mappedPtr;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkAllocator" /> type.
        /// </summary>
        /// <param name="device">The device value used by this operation.</param>
        /// <param name="memoryTypeIndex">The memory type index value used by this operation.</param>
        /// <param name="persistentMapped">The persistent mapped value used by this operation.</param>
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

            VkMemoryBlock initialBlock = new(this.memory, 0, totalMemorySize, this.memoryTypeIndex, this.mappedPtr, false);
            this._freeBlocks.Add(initialBlock);
        }

        #region Disposal

        /// <summary>
        /// Releases resources held by this instance.
        /// </summary>
        public void Dispose() {
            vkFreeMemory(this.device, this.memory, null);
        }

        #endregion

        /// <summary>
        /// Executes the allocate logic for this backend.
        /// </summary>
        /// <param name="size">The size, in bytes, used by this operation.</param>
        /// <param name="alignment">The alignment value used by this operation.</param>
        /// <param name="block">The block value used by this operation.</param>
        /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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

                    if (alignedBlockSize >= size) { // Valid match -- split it and return.
                        this._freeBlocks.RemoveAt(i);

                        freeBlock.Size = alignedBlockSize;
                        if (freeBlock.Offset % alignment != 0) {
                            freeBlock.Offset += alignment - freeBlock.Offset % alignment;
                        }

                        block = freeBlock;

                        if (alignedBlockSize != size) {
                            VkMemoryBlock splitBlock = new(freeBlock.DeviceMemory, freeBlock.Offset + size, freeBlock.Size - size, this.memoryTypeIndex, freeBlock.BaseMappedPointer, false);
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
            }

            block = default;
            return false;
        }

        /// <summary>
        /// Executes the free logic for this backend.
        /// </summary>
        /// <param name="block">The block value used by this operation.</param>
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

        /// <summary>
        /// Executes the merge contiguous blocks logic for this backend.
        /// </summary>
        private void MergeContiguousBlocks() {
            int contiguousLength = 1;

            for (int i = 0; i < this._freeBlocks.Count - 1; i++) {
                ulong blockStart = this._freeBlocks[i].Offset;
                while (i + contiguousLength < this._freeBlocks.Count
                       && this._freeBlocks[i + contiguousLength - 1].End == this._freeBlocks[i + contiguousLength].Offset) {
                    contiguousLength += 1;
                }

                if (contiguousLength > 1) {
                    ulong blockEnd = this._freeBlocks[i + contiguousLength - 1].End;
                    this._freeBlocks.RemoveRange(i, contiguousLength);
                    VkMemoryBlock mergedBlock = new(this.Memory, blockStart, blockEnd - blockStart, this.memoryTypeIndex, this.mappedPtr, false);
                    this._freeBlocks.Insert(i, mergedBlock);
                    contiguousLength = 0;
                }
            }
        }

#if DEBUG

        /// <summary>
        /// Executes the list logic for this backend.
        /// </summary>
        private readonly List<VkMemoryBlock> allocatedBlocks = new List<VkMemoryBlock>();

        /// <summary>
        /// Executes the check allocated block logic for this backend.
        /// </summary>
        /// <param name="block">The block value used by this operation.</param>
        private void checkAllocatedBlock(VkMemoryBlock block) {
            foreach (var oldBlock in allocatedBlocks) Debug.Assert(!blocksOverlap(block, oldBlock), "Allocated blocks have overlapped.");

            allocatedBlocks.Add(block);
        }

        /// <summary>
        /// Executes the blocks overlap logic for this backend.
        /// </summary>
        /// <param name="first">The first value used by this operation.</param>
        /// <param name="second">The second value used by this operation.</param>
        /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
        private bool blocksOverlap(VkMemoryBlock first, VkMemoryBlock second) {
            ulong firstStart = first.Offset;
            ulong firstEnd = first.Offset + first.Size;
            ulong secondStart = second.Offset;
            ulong secondEnd = second.Offset + second.Size;

            return (firstStart <= secondStart && firstEnd > secondStart)
                   || (firstStart >= secondStart && firstEnd <= secondEnd)
                   || (firstStart < secondEnd && firstEnd >= secondEnd)
                   || (firstStart <= secondStart && firstEnd >= secondEnd);
        }

        /// <summary>
        /// Executes the remove allocated block logic for this backend.
        /// </summary>
        /// <param name="block">The block value used by this operation.</param>
        private void removeAllocatedBlock(VkMemoryBlock block) {
            Debug.Assert(allocatedBlocks.Remove(block), "Unable to remove a supposedly allocated block.");
        }
#endif
    }
}

[DebuggerDisplay("[Mem:{DeviceMemory.Handle}] Off:{Offset}, Size:{Size} End:{Offset+Size}")]

/// <summary>
/// Provides the Vulkan backend implementation for VkMemoryBlock.
/// </summary>
internal unsafe struct VkMemoryBlock : IEquatable<VkMemoryBlock> {

    /// <summary>
    /// Stores the memory type index value used during command execution.
    /// </summary>
    public readonly uint MemoryTypeIndex;

    /// <summary>
    /// Stores the device memory state used by this instance.
    /// </summary>
    public readonly VkDeviceMemory DeviceMemory;

    /// <summary>
    /// Stores the base mapped pointer state used by this instance.
    /// </summary>
    public readonly void* BaseMappedPointer;

    /// <summary>
    /// Stores the dedicated allocation state used by this instance.
    /// </summary>
    public readonly bool DedicatedAllocation;

    /// <summary>
    /// Stores the offset value used during command execution.
    /// </summary>
    public ulong Offset;

    /// <summary>
    /// Stores the size value used during command execution.
    /// </summary>
    public ulong Size;

    /// <summary>
    /// Executes the value logic for this backend.
    /// </summary>
    public void* BlockMappedPointer => (byte*)this.BaseMappedPointer + this.Offset;

    /// <summary>
    /// Gets or sets IsPersistentMapped.
    /// </summary>
    public bool IsPersistentMapped => this.BaseMappedPointer != null;

    /// <summary>
    /// Gets or sets End.
    /// </summary>
    public ulong End => this.Offset + this.Size;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkMemoryBlock" /> type.
    /// </summary>
    /// <param name="memory">The memory value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    /// <param name="memoryTypeIndex">The memory type index value used by this operation.</param>
    /// <param name="mappedPtr">The mapped ptr value used by this operation.</param>
    /// <param name="dedicatedAllocation">The dedicated allocation value used by this operation.</param>
    public VkMemoryBlock(VkDeviceMemory memory, ulong offset, ulong size, uint memoryTypeIndex, void* mappedPtr, bool dedicatedAllocation) {
        this.DeviceMemory = memory;
        this.Offset = offset;
        this.Size = size;
        this.MemoryTypeIndex = memoryTypeIndex;
        this.BaseMappedPointer = mappedPtr;
        this.DedicatedAllocation = dedicatedAllocation;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(VkMemoryBlock other) {
        return this.DeviceMemory.Equals(other.DeviceMemory)
               && this.Offset.Equals(other.Offset)
               && this.Size.Equals(other.Size);
    }
}