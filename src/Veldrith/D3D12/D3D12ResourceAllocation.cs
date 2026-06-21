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
    private ID3D12Resource _resource;

    /// <summary>
    /// Stores the memory block backing the resource.
    /// </summary>
    private D3D12MemoryBlock _memoryBlock;

    /// <summary>
    /// Stores the return callback for transient suballocations.
    /// </summary>
    private Action<D3D12ResourceAllocation> _returnCallback;

    /// <summary>
    /// Stores whether this allocation has already been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceAllocation" /> type.
    /// </summary>
    /// <param name="resource">The native resource.</param>
    /// <param name="memoryBlock">The memory block backing the resource.</param>
    /// <param name="mappedPointer">The persistently mapped pointer, or <see cref="IntPtr.Zero" /> when unmapped.</param>
    public D3D12ResourceAllocation(ID3D12Resource resource, D3D12MemoryBlock memoryBlock, IntPtr mappedPointer = default, ulong offset = 0, ulong size = 0, Action<D3D12ResourceAllocation> returnCallback = null) {
        this._resource = resource;
        this._memoryBlock = memoryBlock;
        this.MappedPointer = mappedPointer;
        this.Offset = offset;
        this.Size = size;
        this._returnCallback = returnCallback;
    }

    /// <summary>
    /// Gets the native resource.
    /// </summary>
    internal ID3D12Resource Resource => this._resource;

    /// <summary>
    /// Gets the memory block backing the resource.
    /// </summary>
    internal D3D12MemoryBlock MemoryBlock => this._memoryBlock;

    /// <summary>
    /// Gets whether this allocation returns to an owner instead of owning the native resource.
    /// </summary>
    internal bool IsTransient => this._returnCallback != null;

    /// <summary>
    /// Gets the persistently mapped pointer for CPU-visible resources.
    /// </summary>
    internal IntPtr MappedPointer { get; private set; }

    /// <summary>
    /// Gets the byte offset inside the resource used by this allocation.
    /// </summary>
    internal ulong Offset { get; }

    /// <summary>
    /// Gets the allocation size in bytes.
    /// </summary>
    internal ulong Size { get; }

    /// <summary>
    /// Releases the native resource and returns its memory to the manager.
    /// </summary>
    public void Dispose() {
        if (this._disposed) {
            return;
        }

        this._disposed = true;
        Action<D3D12ResourceAllocation> callback = this._returnCallback;
        this._returnCallback = null;
        if (callback != null) {
            callback(this);
            return;
        }

        ID3D12Resource resourceToDispose = this._resource;
        D3D12MemoryBlock memoryBlockToDispose = this._memoryBlock;
        IntPtr mappedPointer = this.MappedPointer;
        this._resource = null;
        this._memoryBlock = null;
        this.MappedPointer = IntPtr.Zero;
        if (mappedPointer != IntPtr.Zero) {
            resourceToDispose?.Unmap(0);
        }

        resourceToDispose?.Dispose();
        memoryBlockToDispose?.Dispose();
    }
}
