using System;
using System.Collections.Generic;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Provides simple D3D12 default-heap suballocation for placed resources.
/// </summary>
internal sealed class D3D12DeviceMemoryManager : IDisposable {

    /// <summary>
    /// Stores the default chunk size for pooled heaps.
    /// </summary>
    private const ulong DefaultChunkSize = 256UL * 1024UL * 1024UL;

    /// <summary>
    /// Stores the chunk size for persistently CPU-visible transfer heaps.
    /// </summary>
    private const ulong TransferChunkSize = 64UL * 1024UL * 1024UL;

    /// <summary>
    /// Stores the minimum size that should use a dedicated committed resource instead of the pool.
    /// </summary>
    private const ulong DedicatedThreshold = 192UL * 1024UL * 1024UL;

    /// <summary>
    /// Stores the device used to allocate native resources.
    /// </summary>
    private readonly ID3D12Device device;

    /// <summary>
    /// Stores pooled chunks by heap compatibility.
    /// </summary>
    private readonly Dictionary<ChunkKey, List<Chunk>> chunksByKey = new();

    /// <summary>
    /// Protects allocator state.
    /// </summary>
    private readonly object @lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12DeviceMemoryManager" /> type.
    /// </summary>
    /// <param name="device">The device used to allocate native resources.</param>
    public D3D12DeviceMemoryManager(ID3D12Device device) {
        this.device = device;
    }

    /// <summary>
    /// Creates a placed resource when pooling is appropriate; otherwise creates a committed resource.
    /// </summary>
    /// <param name="description">The resource description.</param>
    /// <param name="initialState">The initial resource state.</param>
    /// <param name="heapFlags">The heap flags required by the resource kind.</param>
    /// <param name="allocation">The allocation block, or null when a committed resource was used.</param>
    /// <returns>The created resource.</returns>
    internal D3D12ResourceAllocation CreateResource(ref ResourceDescription description, ResourceStates initialState, HeapType heapType, HeapFlags heapFlags) {
        ResourceAllocationInfo allocationInfo = this.device.GetResourceAllocationInfo(0, description);
        if (allocationInfo.SizeInBytes >= DedicatedThreshold) {
            ID3D12Resource dedicatedResource = this.device.CreateCommittedResource(heapType, HeapFlags.None, description, initialState);
            return new D3D12ResourceAllocation(dedicatedResource, null);
        }

        D3D12MemoryBlock allocation = this.Allocate(allocationInfo.SizeInBytes, allocationInfo.Alignment, heapType, heapFlags);
        ID3D12Resource resource = this.device.CreatePlacedResource<ID3D12Resource>(allocation.Heap, allocation.Offset, description, initialState);
        return new D3D12ResourceAllocation(resource, allocation);
    }

    /// <summary>
    /// Releases all native heaps owned by this manager.
    /// </summary>
    public void Dispose() {
        lock (this.@lock) {
            foreach (List<Chunk> chunks in this.chunksByKey.Values) {
                foreach (Chunk chunk in chunks) {
                    chunk.Dispose();
                }
            }

            this.chunksByKey.Clear();
        }
    }

    /// <summary>
    /// Frees an allocation block.
    /// </summary>
    /// <param name="block">The block to free.</param>
    internal void Free(D3D12MemoryBlock block) {
        lock (this.@lock) {
            block.Chunk.Free(block.Offset, block.Size);
        }
    }

    /// <summary>
    /// Allocates a block from a compatible heap.
    /// </summary>
    /// <param name="size">The allocation size.</param>
    /// <param name="alignment">The allocation alignment.</param>
    /// <param name="heapFlags">The heap flags required by the resource kind.</param>
    /// <returns>The allocated block.</returns>
    private D3D12MemoryBlock Allocate(ulong size, ulong alignment, HeapType heapType, HeapFlags heapFlags) {
        lock (this.@lock) {
            ulong heapAlignment = GetHeapAlignment(alignment);
            ulong allocationAlignment = Math.Max(alignment, heapAlignment);
            ChunkKey key = new(heapType, heapFlags, heapAlignment);
            if (!this.chunksByKey.TryGetValue(key, out List<Chunk> chunks)) {
                chunks = new List<Chunk>();
                this.chunksByKey.Add(key, chunks);
            }

            for (int i = 0; i < chunks.Count; i++) {
                if (chunks[i].TryAllocate(size, allocationAlignment, out ulong offset)) {
                    return new D3D12MemoryBlock(this, chunks[i], chunks[i].Heap, offset, size);
                }
            }

            ulong preferredChunkSize = heapType == HeapType.Default ? DefaultChunkSize : TransferChunkSize;
            ulong chunkSize = AlignUp(Math.Max(preferredChunkSize, size), allocationAlignment);
            Chunk chunk = new(this.device, chunkSize, heapAlignment, heapType, heapFlags);
            chunks.Add(chunk);
            if (!chunk.TryAllocate(size, allocationAlignment, out ulong newOffset)) {
                throw new VeldridException("Unable to allocate sufficient D3D12 heap memory.");
            }

            return new D3D12MemoryBlock(this, chunk, chunk.Heap, newOffset, size);
        }
    }

    /// <summary>
    /// Aligns a value upward.
    /// </summary>
    /// <param name="value">The value to align.</param>
    /// <param name="alignment">The alignment.</param>
    /// <returns>The aligned value.</returns>
    private static ulong AlignUp(ulong value, ulong alignment) {
        return alignment == 0 ? value : (value + alignment - 1) / alignment * alignment;
    }

    /// <summary>
    /// Gets the heap alignment class required for a resource allocation.
    /// </summary>
    /// <param name="resourceAlignment">The resource placement alignment.</param>
    /// <returns>The heap alignment to use for compatible pooled chunks.</returns>
    private static ulong GetHeapAlignment(ulong resourceAlignment) {
        const ulong defaultAlignment = 65536UL;
        const ulong msaaAlignment = 4194304UL;
        return resourceAlignment > defaultAlignment ? msaaAlignment : defaultAlignment;
    }

    /// <summary>
    /// Identifies a group of compatible D3D12 heaps.
    /// </summary>
    private readonly struct ChunkKey : IEquatable<ChunkKey> {

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkKey" /> struct.
        /// </summary>
        /// <param name="flags">The heap flags.</param>
        /// <param name="alignment">The heap alignment.</param>
        public ChunkKey(HeapType heapType, HeapFlags flags, ulong alignment) {
            this.HeapType = heapType;
            this.Flags = flags;
            this.Alignment = alignment;
        }

        /// <summary>
        /// Gets the heap type.
        /// </summary>
        private HeapType HeapType { get; }

        /// <summary>
        /// Gets the heap flags.
        /// </summary>
        private HeapFlags Flags { get; }

        /// <summary>
        /// Gets the heap alignment.
        /// </summary>
        private ulong Alignment { get; }

        /// <summary>
        /// Determines whether this key equals another key.
        /// </summary>
        /// <param name="other">The key to compare with.</param>
        /// <returns><see langword="true" /> when both keys are equal.</returns>
        public bool Equals(ChunkKey other) {
            return this.HeapType == other.HeapType && this.Flags == other.Flags && this.Alignment == other.Alignment;
        }

        /// <summary>
        /// Determines whether this key equals another object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><see langword="true" /> when both objects are equal.</returns>
        public override bool Equals(object obj) {
            return obj is ChunkKey other && this.Equals(other);
        }

        /// <summary>
        /// Gets the hash code for this key.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() {
            return HashCode.Combine(this.HeapType, this.Flags, this.Alignment);
        }
    }

    /// <summary>
    /// Represents a pooled D3D12 heap.
    /// </summary>
    internal sealed class Chunk : IDisposable {

        /// <summary>
        /// Stores free regions inside this chunk.
        /// </summary>
        private readonly List<FreeBlock> freeBlocks = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Chunk" /> type.
        /// </summary>
        /// <param name="device">The device used to create the heap.</param>
        /// <param name="size">The heap size.</param>
        /// <param name="alignment">The heap alignment.</param>
        /// <param name="heapType">The heap type.</param>
        /// <param name="heapFlags">The heap flags.</param>
        public Chunk(ID3D12Device device, ulong size, ulong alignment, HeapType heapType, HeapFlags heapFlags) {
            HeapDescription description = new() {
                SizeInBytes = size,
                Properties = new HeapProperties(heapType),
                Alignment = alignment,
                Flags = heapFlags
            };

            this.Heap = device.CreateHeap<ID3D12Heap>(description);
            this.freeBlocks.Add(new FreeBlock(0, size));
        }

        /// <summary>
        /// Gets the native heap.
        /// </summary>
        internal ID3D12Heap Heap { get; }

        /// <summary>
        /// Attempts to allocate from this chunk.
        /// </summary>
        /// <param name="size">The allocation size.</param>
        /// <param name="alignment">The allocation alignment.</param>
        /// <param name="offset">The allocated offset.</param>
        /// <returns><see langword="true" /> when allocation succeeded.</returns>
        public bool TryAllocate(ulong size, ulong alignment, out ulong offset) {
            for (int i = 0; i < this.freeBlocks.Count; i++) {
                FreeBlock block = this.freeBlocks[i];
                ulong alignedOffset = AlignUp(block.Offset, alignment);
                ulong padding = alignedOffset - block.Offset;
                if (padding + size > block.Size) {
                    continue;
                }

                offset = alignedOffset;
                ulong suffixOffset = alignedOffset + size;
                ulong suffixSize = block.Offset + block.Size - suffixOffset;

                this.freeBlocks.RemoveAt(i);
                if (padding > 0) {
                    this.freeBlocks.Insert(i++, new FreeBlock(block.Offset, padding));
                }

                if (suffixSize > 0) {
                    this.freeBlocks.Insert(i, new FreeBlock(suffixOffset, suffixSize));
                }

                return true;
            }

            offset = 0;
            return false;
        }

        /// <summary>
        /// Frees a block inside this chunk.
        /// </summary>
        /// <param name="offset">The block offset.</param>
        /// <param name="size">The block size.</param>
        public void Free(ulong offset, ulong size) {
            int insertIndex = 0;
            while (insertIndex < this.freeBlocks.Count && this.freeBlocks[insertIndex].Offset < offset) {
                insertIndex++;
            }

            this.freeBlocks.Insert(insertIndex, new FreeBlock(offset, size));
            this.Coalesce();
        }

        /// <summary>
        /// Releases resources held by this chunk.
        /// </summary>
        public void Dispose() {
            this.Heap.Dispose();
        }

        /// <summary>
        /// Coalesces adjacent free regions.
        /// </summary>
        private void Coalesce() {
            for (int i = 0; i < this.freeBlocks.Count - 1;) {
                FreeBlock current = this.freeBlocks[i];
                FreeBlock next = this.freeBlocks[i + 1];
                if (current.Offset + current.Size == next.Offset) {
                    this.freeBlocks[i] = new FreeBlock(current.Offset, current.Size + next.Size);
                    this.freeBlocks.RemoveAt(i + 1);
                    continue;
                }

                i++;
            }
        }
    }

    /// <summary>
    /// Represents a free region inside a chunk.
    /// </summary>
    private readonly struct FreeBlock {

        /// <summary>
        /// Initializes a new instance of the <see cref="FreeBlock" /> struct.
        /// </summary>
        /// <param name="offset">The offset value.</param>
        /// <param name="size">The size value.</param>
        public FreeBlock(ulong offset, ulong size) {
            this.Offset = offset;
            this.Size = size;
        }

        /// <summary>
        /// Gets the offset value.
        /// </summary>
        public ulong Offset { get; }

        /// <summary>
        /// Gets the size value.
        /// </summary>
        public ulong Size { get; }
    }
}
