using System;
using System.Collections.Generic;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Provides fence-recycled upload-buffer pooling and transient upload-ring suballocations.
/// </summary>
internal sealed class D3D12UploadBufferPool : IDisposable {

    /// <summary>
    /// Stores the largest upload buffer size that is retained for reuse.
    /// </summary>
    private const ulong MaxPooledUploadBufferSize = 16UL * 1024UL * 1024UL;

    /// <summary>
    /// Stores the size of each transient upload ring page.
    /// </summary>
    private const ulong UploadRingPageSize = 16UL * 1024UL * 1024UL;

    /// <summary>
    /// Stores the total upload-buffer pool budget.
    /// </summary>
    private const ulong MaxPooledUploadBufferBytes = 128UL * 1024UL * 1024UL;

    /// <summary>
    /// Stores the default upload-buffer alignment used by D3D12 buffer copies.
    /// </summary>
    private const ulong DefaultUploadAlignment = 256UL;

    /// <summary>
    /// Stores the graphics device that owns resource allocations.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Stores reusable standalone upload buffers after their GPU fence has completed.
    /// </summary>
    private readonly List<D3D12ResourceAllocation> _availableUploadBuffers = new();

    /// <summary>
    /// Stores transient upload pages used as a fence-recycled ring.
    /// </summary>
    private readonly List<UploadRingPage> _uploadRingPages = new();

    /// <summary>
    /// Protects reusable upload-buffer pool access.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Tracks total bytes retained by the standalone upload-buffer pool.
    /// </summary>
    private ulong _availableUploadBufferBytes;

    /// <summary>
    /// Stores the upload ring page index preferred for the next allocation.
    /// </summary>
    private int _currentUploadRingPageIndex = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12UploadBufferPool" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns resource allocations.</param>
    internal D3D12UploadBufferPool(D3D12GraphicsDevice gd) {
        this._gd = gd;
    }

    /// <summary>
    /// Rents an upload buffer that is at least the requested size.
    /// </summary>
    /// <param name="sizeInBytes">The required upload-buffer size in bytes.</param>
    /// <returns>An upload heap resource ready for CPU writes.</returns>
    internal D3D12ResourceAllocation Rent(ulong sizeInBytes) {
        return this.Rent(sizeInBytes, DefaultUploadAlignment);
    }

    /// <summary>
    /// Rents an upload buffer with an offset aligned for the target D3D12 copy operation.
    /// </summary>
    /// <param name="sizeInBytes">The required upload-buffer size in bytes.</param>
    /// <param name="alignment">The required byte alignment for the returned allocation offset.</param>
    /// <returns>An upload heap resource ready for CPU writes.</returns>
    internal D3D12ResourceAllocation Rent(ulong sizeInBytes, ulong alignment) {
        if (sizeInBytes == 0) {
            sizeInBytes = 1;
        }

        alignment = NormalizeAlignment(alignment);
        ulong allocationSize = AlignUp(sizeInBytes, alignment);
        if (allocationSize <= UploadRingPageSize) {
            lock (this._lock) {
                D3D12ResourceAllocation ringAllocation = this.TryRentUploadRingAllocation(allocationSize, alignment);
                if (ringAllocation != null) {
                    return ringAllocation;
                }
            }
        }

        lock (this._lock) {
            int bestIndex = -1;
            ulong bestSize = ulong.MaxValue;
            for (int i = 0; i < this._availableUploadBuffers.Count; i++) {
                D3D12ResourceAllocation candidate = this._availableUploadBuffers[i];
                ulong candidateSize = candidate.Resource.Description.Width;
                if (candidateSize >= sizeInBytes && candidateSize < bestSize) {
                    bestIndex = i;
                    bestSize = candidateSize;
                }
            }

            if (bestIndex >= 0) {
                D3D12ResourceAllocation buffer = this._availableUploadBuffers[bestIndex];
                this._availableUploadBuffers.RemoveAt(bestIndex);
                this._availableUploadBufferBytes -= buffer.Resource.Description.Width;
                return buffer;
            }
        }

        ResourceDescription description = ResourceDescription.Buffer(allocationSize);
        return this._gd.MemoryManager.CreateResource(ref description, ResourceStates.GenericRead, HeapType.Upload, HeapFlags.AllowOnlyBuffers);
    }

    /// <summary>
    /// Returns an upload buffer to the reusable pool or disposes it when it is too large.
    /// </summary>
    /// <param name="buffer">The upload buffer to return.</param>
    internal void Return(D3D12ResourceAllocation buffer) {
        if (buffer == null) {
            return;
        }

        if (buffer.IsTransient) {
            buffer.Dispose();
            return;
        }

        ulong size = buffer.Resource.Description.Width;
        if (size > MaxPooledUploadBufferSize) {
            buffer.Dispose();
            return;
        }

        lock (this._lock) {
            if (this._availableUploadBufferBytes + size > MaxPooledUploadBufferBytes) {
                buffer.Dispose();
                return;
            }

            this._availableUploadBuffers.Add(buffer);
            this._availableUploadBufferBytes += size;
        }
    }

    /// <summary>
    /// Releases every retained upload resource.
    /// </summary>
    public void Dispose() {
        List<D3D12ResourceAllocation> buffers = null;
        List<UploadRingPage> ringPages = null;
        lock (this._lock) {
            if (this._availableUploadBuffers.Count > 0) {
                buffers = new List<D3D12ResourceAllocation>(this._availableUploadBuffers);
                this._availableUploadBuffers.Clear();
                this._availableUploadBufferBytes = 0;
            }

            if (this._uploadRingPages.Count > 0) {
                ringPages = new List<UploadRingPage>(this._uploadRingPages);
                this._uploadRingPages.Clear();
                this._currentUploadRingPageIndex = -1;
            }
        }

        if (buffers != null) {
            for (int i = 0; i < buffers.Count; i++) {
                buffers[i].Dispose();
            }
        }

        if (ringPages == null) {
            return;
        }

        for (int i = 0; i < ringPages.Count; i++) {
            ringPages[i].Dispose();
        }
    }

    /// <summary>
    /// Attempts to rent a suballocation from the transient upload ring.
    /// </summary>
    /// <param name="sizeInBytes">The aligned allocation size.</param>
    /// <param name="alignment">The required byte alignment for the returned offset.</param>
    /// <returns>The transient allocation, or null when a new page should be allocated.</returns>
    private D3D12ResourceAllocation TryRentUploadRingAllocation(ulong sizeInBytes, ulong alignment) {
        if (this._currentUploadRingPageIndex >= 0 && this._currentUploadRingPageIndex < this._uploadRingPages.Count) {
            D3D12ResourceAllocation allocation = this._uploadRingPages[this._currentUploadRingPageIndex].TryAllocate(sizeInBytes, alignment);
            if (allocation != null) {
                return allocation;
            }
        }

        for (int i = 0; i < this._uploadRingPages.Count; i++) {
            D3D12ResourceAllocation allocation = this._uploadRingPages[i].TryAllocate(sizeInBytes, alignment);
            if (allocation != null) {
                this._currentUploadRingPageIndex = i;
                return allocation;
            }
        }

        ResourceDescription description = ResourceDescription.Buffer(UploadRingPageSize);
        D3D12ResourceAllocation pageAllocation = this._gd.MemoryManager.CreateResource(ref description, ResourceStates.GenericRead, HeapType.Upload, HeapFlags.AllowOnlyBuffers);
        UploadRingPage page = new(pageAllocation);
        this._uploadRingPages.Add(page);
        this._currentUploadRingPageIndex = this._uploadRingPages.Count - 1;
        return page.TryAllocate(sizeInBytes, alignment);
    }

    /// <summary>
    /// Normalizes an alignment to a non-zero power-of-two value.
    /// </summary>
    /// <param name="alignment">The requested alignment.</param>
    /// <returns>A valid alignment.</returns>
    private static ulong NormalizeAlignment(ulong alignment) {
        if (alignment == 0) {
            return DefaultUploadAlignment;
        }

        if ((alignment & (alignment - 1)) == 0) {
            return alignment;
        }

        ulong normalized = 1;
        while (normalized < alignment) {
            normalized <<= 1;
        }

        return normalized;
    }

    /// <summary>
    /// Aligns a value upward to the specified power-of-two alignment.
    /// </summary>
    /// <param name="value">The value to align.</param>
    /// <param name="alignment">The alignment boundary.</param>
    /// <returns>The aligned value.</returns>
    private static ulong AlignUp(ulong value, ulong alignment) {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    /// <summary>
    /// Represents a persistently mapped transient upload page.
    /// </summary>
    private sealed class UploadRingPage : IDisposable {

        /// <summary>
        /// Stores the backing upload resource allocation.
        /// </summary>
        private readonly D3D12ResourceAllocation _allocation;

        /// <summary>
        /// Protects page suballocation state.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// Stores the current write offset.
        /// </summary>
        private ulong _offset;

        /// <summary>
        /// Stores the number of live transient allocations on this page.
        /// </summary>
        private int _activeAllocations;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadRingPage" /> type.
        /// </summary>
        /// <param name="allocation">The backing allocation.</param>
        public UploadRingPage(D3D12ResourceAllocation allocation) {
            this._allocation = allocation;
        }

        /// <summary>
        /// Attempts to allocate a transient region from this page.
        /// </summary>
        /// <param name="sizeInBytes">The allocation size in bytes.</param>
        /// <param name="alignment">The required byte alignment for the returned offset.</param>
        /// <returns>The allocation, or null when this page has no room.</returns>
        public D3D12ResourceAllocation TryAllocate(ulong sizeInBytes, ulong alignment) {
            lock (this._lock) {
                ulong alignedOffset = AlignUp(this._offset, alignment);
                if (alignedOffset + sizeInBytes > UploadRingPageSize) {
                    if (this._activeAllocations == 0) {
                        alignedOffset = 0;
                        this._offset = 0;
                    }

                    if (alignedOffset + sizeInBytes > UploadRingPageSize) {
                        return null;
                    }
                }

                this._offset = alignedOffset + sizeInBytes;
                this._activeAllocations++;
                IntPtr mappedPointer = IntPtr.Add(this._allocation.MappedPointer, checked((int)alignedOffset));
                return new D3D12ResourceAllocation(this._allocation.Resource, null, mappedPointer, alignedOffset, sizeInBytes, this.ReturnAllocation);
            }
        }

        /// <summary>
        /// Returns a transient allocation to this page.
        /// </summary>
        /// <param name="returnedAllocation">The returned allocation.</param>
        private void ReturnAllocation(D3D12ResourceAllocation returnedAllocation) {
            lock (this._lock) {
                this._activeAllocations--;
                if (this._activeAllocations == 0 && this._offset >= UploadRingPageSize) {
                    this._offset = 0;
                }
            }
        }

        /// <summary>
        /// Releases the backing allocation.
        /// </summary>
        public void Dispose() {
            this._allocation.Dispose();
        }
    }
}
