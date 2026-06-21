using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Represents a suballocation inside a D3D12 heap.
/// </summary>
internal sealed class D3D12MemoryBlock : IDisposable {

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12MemoryBlock" /> type.
    /// </summary>
    /// <param name="manager">The manager that owns this block.</param>
    /// <param name="chunk">The chunk that contains this block.</param>
    /// <param name="heap">The native heap that contains this block.</param>
    /// <param name="offset">The byte offset inside the heap.</param>
    /// <param name="size">The allocation size in bytes.</param>
    internal D3D12MemoryBlock(D3D12DeviceMemoryManager manager, D3D12DeviceMemoryManager.Chunk chunk, ID3D12Heap heap, ulong offset, ulong size) {
        this.Manager = manager;
        this.Chunk = chunk;
        this.Heap = heap;
        this.Offset = offset;
        this.Size = size;
    }

    /// <summary>
    /// Gets the manager that owns this block.
    /// </summary>
    private D3D12DeviceMemoryManager Manager { get; }

    /// <summary>
    /// Gets the chunk that contains this block.
    /// </summary>
    internal D3D12DeviceMemoryManager.Chunk Chunk { get; }

    /// <summary>
    /// Gets the native heap that contains this block.
    /// </summary>
    internal ID3D12Heap Heap { get; }

    /// <summary>
    /// Gets the byte offset inside the heap.
    /// </summary>
    internal ulong Offset { get; }

    /// <summary>
    /// Gets the allocation size in bytes.
    /// </summary>
    internal ulong Size { get; }

    /// <summary>
    /// Returns this block to its manager.
    /// </summary>
    public void Dispose() {
        this.Manager.Free(this);
    }
}
