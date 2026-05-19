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
    private readonly ID3D12Device _device;

    /// <summary>
    /// Stores pooled chunks by heap compatibility.
    /// </summary>
    private readonly Dictionary<ChunkKey, List<Chunk>> _chunksByKey = new();

    /// <summary>
    /// Protects allocator state.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12DeviceMemoryManager" /> type.
    /// </summary>
    /// <param name="device">The device used to allocate native resources.</param>
    public D3D12DeviceMemoryManager(ID3D12Device device) {
        this._device = device;
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
        ResourceAllocationInfo allocationInfo = this._device.GetResourceAllocationInfo(0, description);
        if (ShouldUseDedicatedResource(ref description, allocationInfo, heapType, heapFlags)) {
            ID3D12Resource dedicatedResource = this._device.CreateCommittedResource(heapType, HeapFlags.None, description, initialState);
            return new D3D12ResourceAllocation(dedicatedResource, null, MapCpuVisibleResource(dedicatedResource, heapType));
        }

        D3D12MemoryBlock allocation = this.Allocate(allocationInfo.SizeInBytes, allocationInfo.Alignment, heapType, heapFlags);
        ID3D12Resource resource = this._device.CreatePlacedResource<ID3D12Resource>(allocation.Heap, allocation.Offset, description, initialState);
        try {
            return new D3D12ResourceAllocation(resource, allocation, MapCpuVisibleResource(resource, heapType));
        }
        catch {
            resource.Dispose();
            allocation.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Releases all native heaps owned by this manager.
    /// </summary>
    public void Dispose() {
        lock (this._lock) {
            foreach (List<Chunk> chunks in this._chunksByKey.Values) {
                foreach (Chunk chunk in chunks) {
                    chunk.Dispose();
                }
            }

            this._chunksByKey.Clear();
        }
    }

    /// <summary>
    /// Frees an allocation block.
    /// </summary>
    /// <param name="block">The block to free.</param>
    internal void Free(D3D12MemoryBlock block) {
        lock (this._lock) {
            block.Chunk.Free(block.Offset, block.Size);
        }
    }

    /// <summary>
    /// Gets a compact allocator statistics string for performance logging.
    /// </summary>
    /// <returns>A compact allocator statistics string.</returns>
    internal string GetStatsString() {
        lock (this._lock) {
            ulong defaultBytes = 0;
            ulong defaultFreeBytes = 0;
            int defaultChunks = 0;
            ulong uploadBytes = 0;
            ulong uploadFreeBytes = 0;
            int uploadChunks = 0;
            ulong readbackBytes = 0;
            ulong readbackFreeBytes = 0;
            int readbackChunks = 0;

            foreach (KeyValuePair<ChunkKey, List<Chunk>> pair in this._chunksByKey) {
                for (int i = 0; i < pair.Value.Count; i++) {
                    Chunk chunk = pair.Value[i];
                    switch (pair.Key.HeapType) {
                        case HeapType.Default:
                            defaultChunks++;
                            defaultBytes += chunk.Size;
                            defaultFreeBytes += chunk.GetFreeBytes();
                            break;
                        case HeapType.Upload:
                            uploadChunks++;
                            uploadBytes += chunk.Size;
                            uploadFreeBytes += chunk.GetFreeBytes();
                            break;
                        case HeapType.Readback:
                            readbackChunks++;
                            readbackBytes += chunk.Size;
                            readbackFreeBytes += chunk.GetFreeBytes();
                            break;
                    }
                }
            }

            return $"memChunks={defaultChunks}/{uploadChunks}/{readbackChunks}, "
                   + $"memUsedMB={BytesToMiB(defaultBytes - defaultFreeBytes):F1}/{BytesToMiB(uploadBytes - uploadFreeBytes):F1}/{BytesToMiB(readbackBytes - readbackFreeBytes):F1}, "
                   + $"memFreeMB={BytesToMiB(defaultFreeBytes):F1}/{BytesToMiB(uploadFreeBytes):F1}/{BytesToMiB(readbackFreeBytes):F1}";
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
        lock (this._lock) {
            ulong heapAlignment = GetHeapAlignment(alignment);
            ulong allocationAlignment = Math.Max(alignment, heapAlignment);
            ChunkKey key = new(heapType, heapFlags, heapAlignment);
            if (!this._chunksByKey.TryGetValue(key, out List<Chunk> chunks)) {
                chunks = new List<Chunk>();
                this._chunksByKey.Add(key, chunks);
            }

            for (int i = 0; i < chunks.Count; i++) {
                if (chunks[i].TryAllocate(size, allocationAlignment, out ulong offset)) {
                    return new D3D12MemoryBlock(this, chunks[i], chunks[i].Heap, offset, size);
                }
            }

            ulong preferredChunkSize = heapType == HeapType.Default ? DefaultChunkSize : TransferChunkSize;
            ulong chunkSize = AlignUp(Math.Max(preferredChunkSize, size), allocationAlignment);
            Chunk chunk = new(this._device, chunkSize, heapAlignment, heapType, heapFlags);
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
    /// Converts bytes to mebibytes.
    /// </summary>
    /// <param name="bytes">The byte count.</param>
    /// <returns>The mebibyte count.</returns>
    private static double BytesToMiB(ulong bytes) {
        return bytes / (1024.0 * 1024.0);
    }

    /// <summary>
    /// Determines whether a resource should bypass pooled placed-resource heaps.
    /// </summary>
    /// <param name="description">The resource description.</param>
    /// <param name="allocationInfo">The resource allocation requirements.</param>
    /// <param name="heapType">The heap type.</param>
    /// <param name="heapFlags">The heap flags.</param>
    /// <returns><see langword="true" /> when the resource should use a dedicated committed allocation.</returns>
    private static bool ShouldUseDedicatedResource(ref ResourceDescription description, ResourceAllocationInfo allocationInfo, HeapType heapType, HeapFlags heapFlags) {
        if (allocationInfo.SizeInBytes >= DedicatedThreshold) {
            return true;
        }

        if (heapType != HeapType.Default) {
            return false;
        }

        bool renderTargetOrDepth = heapFlags == HeapFlags.AllowOnlyRenderTargetDepthStencilTextures;
        if (!renderTargetOrDepth) {
            return false;
        }

        // D3D12 drivers often prefer dedicated heaps for MSAA and large RT/DS resources.
        // MSAA resources normally report 4MB placement alignment.
        if (allocationInfo.Alignment > 65536UL) {
            return true;
        }

        const ulong largeRenderTargetThreshold = 64UL * 1024UL * 1024UL;
        return allocationInfo.SizeInBytes >= largeRenderTargetThreshold;
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
    /// Persistently maps CPU-visible D3D12 resources.
    /// </summary>
    /// <param name="resource">The resource to map.</param>
    /// <param name="heapType">The resource heap type.</param>
    /// <returns>The mapped pointer, or <see cref="IntPtr.Zero" /> for GPU-only resources.</returns>
    private static unsafe IntPtr MapCpuVisibleResource(ID3D12Resource resource, HeapType heapType) {
        if (heapType != HeapType.Upload && heapType != HeapType.Readback) {
            return IntPtr.Zero;
        }

        void* pointer = null;
        resource.Map(0, &pointer).CheckError();
        return (IntPtr)pointer;
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
        internal HeapType HeapType { get; }

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
        private readonly List<FreeBlock> _freeBlocks = new();

#if DEBUG
        /// <summary>
        /// Stores allocated blocks for overlap validation in debug builds.
        /// </summary>
        private readonly List<FreeBlock> _allocatedBlocks = new();
#endif

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
            this.Size = size;
            this._freeBlocks.Add(new FreeBlock(0, size));
        }

        /// <summary>
        /// Gets the native heap.
        /// </summary>
        internal ID3D12Heap Heap { get; }

        /// <summary>
        /// Gets the chunk size.
        /// </summary>
        internal ulong Size { get; }

        /// <summary>
        /// Attempts to allocate from this chunk.
        /// </summary>
        /// <param name="size">The allocation size.</param>
        /// <param name="alignment">The allocation alignment.</param>
        /// <param name="offset">The allocated offset.</param>
        /// <returns><see langword="true" /> when allocation succeeded.</returns>
        public bool TryAllocate(ulong size, ulong alignment, out ulong offset) {
            for (int i = 0; i < this._freeBlocks.Count; i++) {
                FreeBlock block = this._freeBlocks[i];
                ulong alignedOffset = AlignUp(block.Offset, alignment);
                ulong padding = alignedOffset - block.Offset;
                if (padding + size > block.Size) {
                    continue;
                }

                offset = alignedOffset;
                ulong suffixOffset = alignedOffset + size;
                ulong suffixSize = block.Offset + block.Size - suffixOffset;

                this._freeBlocks.RemoveAt(i);
                if (padding > 0) {
                    this._freeBlocks.Insert(i++, new FreeBlock(block.Offset, padding));
                }

                if (suffixSize > 0) {
                    this._freeBlocks.Insert(i, new FreeBlock(suffixOffset, suffixSize));
                }

#if DEBUG
                this.CheckAllocatedBlock(new FreeBlock(offset, size));
#endif
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
            while (insertIndex < this._freeBlocks.Count && this._freeBlocks[insertIndex].Offset < offset) {
                insertIndex++;
            }

            this._freeBlocks.Insert(insertIndex, new FreeBlock(offset, size));
            this.Coalesce();
#if DEBUG
            this.RemoveAllocatedBlock(new FreeBlock(offset, size));
#endif
        }

        /// <summary>
        /// Gets the total free bytes inside this chunk.
        /// </summary>
        /// <returns>The free byte count.</returns>
        public ulong GetFreeBytes() {
            ulong freeBytes = 0;
            for (int i = 0; i < this._freeBlocks.Count; i++) {
                freeBytes += this._freeBlocks[i].Size;
            }

            return freeBytes;
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
            for (int i = 0; i < this._freeBlocks.Count - 1;) {
                FreeBlock current = this._freeBlocks[i];
                FreeBlock next = this._freeBlocks[i + 1];
                if (current.Offset + current.Size == next.Offset) {
                    this._freeBlocks[i] = new FreeBlock(current.Offset, current.Size + next.Size);
                    this._freeBlocks.RemoveAt(i + 1);
                    continue;
                }

                i++;
            }
        }

#if DEBUG
        /// <summary>
        /// Verifies that a new allocation does not overlap an existing allocation.
        /// </summary>
        /// <param name="block">The allocated block.</param>
        private void CheckAllocatedBlock(FreeBlock block) {
            for (int i = 0; i < this._allocatedBlocks.Count; i++) {
                if (BlocksOverlap(block, this._allocatedBlocks[i])) {
                    throw new VeldridException("D3D12 memory allocation blocks overlap.");
                }
            }

            this._allocatedBlocks.Add(block);
        }

        /// <summary>
        /// Removes a block from debug allocation tracking.
        /// </summary>
        /// <param name="block">The block to remove.</param>
        private void RemoveAllocatedBlock(FreeBlock block) {
            for (int i = 0; i < this._allocatedBlocks.Count; i++) {
                FreeBlock allocated = this._allocatedBlocks[i];
                if (allocated.Offset == block.Offset && allocated.Size == block.Size) {
                    this._allocatedBlocks.RemoveAt(i);
                    return;
                }
            }

            throw new VeldridException("D3D12 memory allocation block was freed twice or was not allocated.");
        }

        /// <summary>
        /// Determines whether two blocks overlap.
        /// </summary>
        /// <param name="first">The first block.</param>
        /// <param name="second">The second block.</param>
        /// <returns><see langword="true" /> when the blocks overlap.</returns>
        private static bool BlocksOverlap(FreeBlock first, FreeBlock second) {
            ulong firstEnd = first.Offset + first.Size;
            ulong secondEnd = second.Offset + second.Size;
            return first.Offset < secondEnd && second.Offset < firstEnd;
        }
#endif
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
