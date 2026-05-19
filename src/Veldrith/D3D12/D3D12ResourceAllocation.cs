using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Owns a D3D12 resource and the memory allocation used by that resource.
/// </summary>
internal sealed class D3D12ResourceAllocation : IDisposable {

    /// <summary>
    /// Stores the native resource.
    /// </summary>
    private ID3D12Resource resource;

    /// <summary>
    /// Stores the memory block backing the resource.
    /// </summary>
    private D3D12MemoryBlock memoryBlock;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceAllocation" /> type.
    /// </summary>
    /// <param name="resource">The native resource.</param>
    /// <param name="memoryBlock">The memory block backing the resource.</param>
    public D3D12ResourceAllocation(ID3D12Resource resource, D3D12MemoryBlock memoryBlock) {
        this.resource = resource;
        this.memoryBlock = memoryBlock;
    }

    /// <summary>
    /// Gets the native resource.
    /// </summary>
    internal ID3D12Resource Resource => this.resource;

    /// <summary>
    /// Gets the memory block backing the resource.
    /// </summary>
    internal D3D12MemoryBlock MemoryBlock => this.memoryBlock;

    /// <summary>
    /// Releases the native resource and returns its memory to the manager.
    /// </summary>
    public void Dispose() {
        ID3D12Resource resourceToDispose = this.resource;
        D3D12MemoryBlock memoryBlockToDispose = this.memoryBlock;
        this.resource = null;
        this.memoryBlock = null;
        resourceToDispose?.Dispose();
        memoryBlockToDispose?.Dispose();
    }
}
