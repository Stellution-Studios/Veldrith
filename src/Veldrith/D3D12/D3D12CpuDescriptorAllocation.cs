using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Represents a contiguous range of CPU descriptors.
/// </summary>
internal sealed class D3D12CpuDescriptorAllocation : IDisposable {

    /// <summary>
    /// Stores the owner page.
    /// </summary>
    private readonly D3D12CpuDescriptorAllocator.Page _page;

    /// <summary>
    /// Stores the first descriptor index.
    /// </summary>
    private readonly uint _offset;

    /// <summary>
    /// Stores the descriptor count.
    /// </summary>
    private readonly uint _count;

    /// <summary>
    /// Stores whether this allocation has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12CpuDescriptorAllocation"/> type.
    /// </summary>
    /// <param name="page">The owner page.</param>
    /// <param name="offset">The first descriptor index.</param>
    /// <param name="count">The descriptor count.</param>
    /// <param name="handle">The first descriptor handle.</param>
    internal D3D12CpuDescriptorAllocation(D3D12CpuDescriptorAllocator.Page page, uint offset, uint count, CpuDescriptorHandle handle) {
        this._page = page;
        this._offset = offset;
        this._count = count;
        this.Handle = handle;
    }

    /// <summary>
    /// Gets the first descriptor handle.
    /// </summary>
    internal CpuDescriptorHandle Handle { get; }

    /// <summary>
    /// Returns this descriptor range to its owner.
    /// </summary>
    public void Dispose() {
        if (this._disposed) {
            return;
        }

        this._disposed = true;
        this._page.Free(this._offset, this._count);
    }
}
