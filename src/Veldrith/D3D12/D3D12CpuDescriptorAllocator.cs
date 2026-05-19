using System;
using System.Collections.Generic;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Pools CPU-only D3D12 descriptor heaps and suballocates descriptor ranges.
/// </summary>
internal sealed class D3D12CpuDescriptorAllocator : IDisposable {

    /// <summary>
    /// Stores the device used to create descriptor heaps.
    /// </summary>
    private readonly ID3D12Device _device;

    /// <summary>
    /// Stores allocator pages.
    /// </summary>
    private readonly List<Page> _pages = new();

    /// <summary>
    /// Stores the descriptor heap type.
    /// </summary>
    private readonly DescriptorHeapType _heapType;

    /// <summary>
    /// Stores the descriptor count per page.
    /// </summary>
    private readonly uint _descriptorsPerPage;

    /// <summary>
    /// Stores the descriptor size in bytes.
    /// </summary>
    private readonly int _descriptorSize;

    /// <summary>
    /// Protects allocator state.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12CpuDescriptorAllocator"/> type.
    /// </summary>
    /// <param name="device">The device used to create descriptor heaps.</param>
    /// <param name="heapType">The descriptor heap type.</param>
    /// <param name="descriptorsPerPage">The descriptor count per page.</param>
    public D3D12CpuDescriptorAllocator(ID3D12Device device, DescriptorHeapType heapType, uint descriptorsPerPage) {
        this._device = device;
        this._heapType = heapType;
        this._descriptorsPerPage = descriptorsPerPage;
        this._descriptorSize = (int)device.GetDescriptorHandleIncrementSize(heapType);
    }

    /// <summary>
    /// Allocates a contiguous CPU descriptor range.
    /// </summary>
    /// <param name="count">The number of descriptors.</param>
    /// <returns>The allocation.</returns>
    public D3D12CpuDescriptorAllocation Allocate(uint count) {
        if (count == 0) {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        lock (this._lock) {
            for (int i = 0; i < this._pages.Count; i++) {
                if (this._pages[i].TryAllocate(count, out D3D12CpuDescriptorAllocation allocation)) {
                    return allocation;
                }
            }

            uint pageSize = Math.Max(this._descriptorsPerPage, count);
            Page page = new(this._device, this._heapType, pageSize, this._descriptorSize);
            this._pages.Add(page);
            if (!page.TryAllocate(count, out D3D12CpuDescriptorAllocation newAllocation)) {
                throw new VeldridException("Unable to allocate D3D12 CPU descriptors.");
            }

            return newAllocation;
        }
    }

    /// <summary>
    /// Releases all descriptor heaps.
    /// </summary>
    public void Dispose() {
        lock (this._lock) {
            for (int i = 0; i < this._pages.Count; i++) {
                this._pages[i].Dispose();
            }

            this._pages.Clear();
        }
    }

    /// <summary>
    /// Represents a CPU descriptor heap page.
    /// </summary>
    internal sealed class Page : IDisposable {

        /// <summary>
        /// Stores the heap.
        /// </summary>
        private readonly ID3D12DescriptorHeap _heap;

        /// <summary>
        /// Stores the descriptor size in bytes.
        /// </summary>
        private readonly int _descriptorSize;

        /// <summary>
        /// Stores free descriptor ranges.
        /// </summary>
        private readonly List<FreeRange> _freeRanges = new();

        /// <summary>
        /// Protects page state.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Page"/> type.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="heapType">The heap type.</param>
        /// <param name="count">The descriptor count.</param>
        /// <param name="descriptorSize">The descriptor size.</param>
        public Page(ID3D12Device device, DescriptorHeapType heapType, uint count, int descriptorSize) {
            this._heap = device.CreateDescriptorHeap(new DescriptorHeapDescription(heapType, count));
            this._descriptorSize = descriptorSize;
            this._freeRanges.Add(new FreeRange(0, count));
        }

        /// <summary>
        /// Attempts to allocate descriptors from this page.
        /// </summary>
        /// <param name="count">The descriptor count.</param>
        /// <param name="allocation">The allocation.</param>
        /// <returns><see langword="true"/> when allocation succeeded.</returns>
        public bool TryAllocate(uint count, out D3D12CpuDescriptorAllocation allocation) {
            lock (this._lock) {
                for (int i = 0; i < this._freeRanges.Count; i++) {
                    FreeRange range = this._freeRanges[i];
                    if (range.Count < count) {
                        continue;
                    }

                    uint offset = range.Offset;
                    if (range.Count == count) {
                        this._freeRanges.RemoveAt(i);
                    }
                    else {
                        this._freeRanges[i] = new FreeRange(range.Offset + count, range.Count - count);
                    }

                    allocation = new D3D12CpuDescriptorAllocation(this, offset, count, this._heap.GetCPUDescriptorHandleForHeapStart() + (int)offset * this._descriptorSize);
                    return true;
                }
            }

            allocation = null;
            return false;
        }

        /// <summary>
        /// Frees a descriptor range.
        /// </summary>
        /// <param name="offset">The first descriptor index.</param>
        /// <param name="count">The descriptor count.</param>
        internal void Free(uint offset, uint count) {
            lock (this._lock) {
                int insertIndex = 0;
                while (insertIndex < this._freeRanges.Count && this._freeRanges[insertIndex].Offset < offset) {
                    insertIndex++;
                }

                this._freeRanges.Insert(insertIndex, new FreeRange(offset, count));
                this.Coalesce();
            }
        }

        /// <summary>
        /// Releases the heap.
        /// </summary>
        public void Dispose() {
            this._heap.Dispose();
        }

        /// <summary>
        /// Coalesces adjacent free ranges.
        /// </summary>
        private void Coalesce() {
            for (int i = 0; i < this._freeRanges.Count - 1;) {
                FreeRange current = this._freeRanges[i];
                FreeRange next = this._freeRanges[i + 1];
                if (current.Offset + current.Count == next.Offset) {
                    this._freeRanges[i] = new FreeRange(current.Offset, current.Count + next.Count);
                    this._freeRanges.RemoveAt(i + 1);
                    continue;
                }

                i++;
            }
        }
    }

    /// <summary>
    /// Represents a free descriptor range.
    /// </summary>
    private readonly struct FreeRange {

        /// <summary>
        /// Initializes a new instance of the <see cref="FreeRange"/> struct.
        /// </summary>
        /// <param name="offset">The first descriptor index.</param>
        /// <param name="count">The descriptor count.</param>
        public FreeRange(uint offset, uint count) {
            this.Offset = offset;
            this.Count = count;
        }

        /// <summary>
        /// Gets the first descriptor index.
        /// </summary>
        public uint Offset { get; }

        /// <summary>
        /// Gets the descriptor count.
        /// </summary>
        public uint Count { get; }
    }
}
