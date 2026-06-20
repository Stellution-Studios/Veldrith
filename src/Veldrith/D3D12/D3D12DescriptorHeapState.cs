using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Owns the device-global shader-visible descriptor heaps used by D3D12 command lists.
/// </summary>
internal sealed class D3D12DescriptorHeapState : IDisposable {

    /// <summary>
    /// Stores the next shader-visible descriptor heap cache id.
    /// </summary>
    private static int s_nextCacheId;

    /// <summary>
    /// Stores the default number of sampler descriptors retained by the global shader-visible heap.
    /// </summary>
    private const uint MaxSamplerDescriptors = 2048;

    /// <summary>
    /// Stores the default number of SRV/UAV descriptors retained by the global shader-visible heap.
    /// </summary>
    private const uint MaxSrvUavDescriptors = 262144;

    /// <summary>
    /// Stores the graphics device used to create descriptors and descriptor heaps.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Serializes global descriptor-table allocation and descriptor-copy scratch storage.
    /// </summary>
    private readonly object _allocationLock = new();

    /// <summary>
    /// Identifies cached descriptor table handles that belong to this heap state.
    /// </summary>
    private readonly uint _cacheId;

    /// <summary>
    /// Stores the descriptor heaps currently passed to D3D12.
    /// </summary>
    private readonly ID3D12DescriptorHeap[] _boundDescriptorHeaps = new ID3D12DescriptorHeap[2];

    /// <summary>
    /// Reusable source descriptor handle batch for CopyDescriptors.
    /// </summary>
    private CpuDescriptorHandle[] _descriptorCopySources = new CpuDescriptorHandle[16];

    /// <summary>
    /// Reusable destination descriptor handle batch for CopyDescriptors.
    /// </summary>
    private CpuDescriptorHandle[] _descriptorCopyDests = new CpuDescriptorHandle[16];

    /// <summary>
    /// Reusable per-range size array for batched descriptor copies.
    /// </summary>
    private uint[] _descriptorCopyRangeSizes = CreateDescriptorCopyRangeSizes(16);

    /// <summary>
    /// Stores the persistent shader-visible sampler descriptor heap.
    /// </summary>
    private readonly ID3D12DescriptorHeap _shaderVisibleSamplerHeap;

    /// <summary>
    /// Stores the persistent shader-visible SRV/UAV descriptor heap.
    /// </summary>
    private readonly ID3D12DescriptorHeap _shaderVisibleSrvUavHeap;

    /// <summary>
    /// Stores the CPU start handle for the shader-visible SRV/UAV heap.
    /// </summary>
    private readonly CpuDescriptorHandle _srvUavHeapCpuStart;

    /// <summary>
    /// Stores the GPU start handle for the shader-visible SRV/UAV heap.
    /// </summary>
    private readonly GpuDescriptorHandle _srvUavHeapGpuStart;

    /// <summary>
    /// Stores the CPU start handle for the shader-visible sampler heap.
    /// </summary>
    private readonly CpuDescriptorHandle _samplerHeapCpuStart;

    /// <summary>
    /// Stores the GPU start handle for the shader-visible sampler heap.
    /// </summary>
    private readonly GpuDescriptorHandle _samplerHeapGpuStart;

    /// <summary>
    /// Stores the descriptor size for the shader-visible SRV/UAV heap.
    /// </summary>
    private readonly int _srvUavDescriptorSize;

    /// <summary>
    /// Stores the descriptor size for the shader-visible sampler heap.
    /// </summary>
    private readonly int _samplerDescriptorSize;

    /// <summary>
    /// <summary>
    /// Stores the next free sampler descriptor in the persistent heap.
    /// </summary>
    private uint _nextSamplerDescriptor;

    /// <summary>
    /// Stores the next free SRV/UAV descriptor in the persistent heap.
    /// </summary>
    private uint _nextSrvUavDescriptor;

    /// <summary>
    /// Stores reusable SRV/UAV descriptor ranges whose previous GPU users have completed.
    /// </summary>
    private readonly List<DescriptorRange> _freeSrvUavRanges = new();

    /// <summary>
    /// Stores reusable sampler descriptor ranges whose previous GPU users have completed.
    /// </summary>
    private readonly List<DescriptorRange> _freeSamplerRanges = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12DescriptorHeapState" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns the descriptor heaps.</param>
    internal D3D12DescriptorHeapState(D3D12GraphicsDevice gd) {
        this._gd = gd;
        this._cacheId = (uint)Interlocked.Increment(ref s_nextCacheId);
        this._shaderVisibleSrvUavHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, MaxSrvUavDescriptors, DescriptorHeapFlags.ShaderVisible));
        this._shaderVisibleSamplerHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.Sampler, MaxSamplerDescriptors, DescriptorHeapFlags.ShaderVisible));
        this._srvUavDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        this._samplerDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler);
        this._srvUavHeapCpuStart = this._shaderVisibleSrvUavHeap.GetCPUDescriptorHandleForHeapStart();
        this._srvUavHeapGpuStart = this._shaderVisibleSrvUavHeap.GetGPUDescriptorHandleForHeapStart();
        this._samplerHeapCpuStart = this._shaderVisibleSamplerHeap.GetCPUDescriptorHandleForHeapStart();
        this._samplerHeapGpuStart = this._shaderVisibleSamplerHeap.GetGPUDescriptorHandleForHeapStart();
        this._boundDescriptorHeaps[0] = this._shaderVisibleSrvUavHeap;
        this._boundDescriptorHeaps[1] = this._shaderVisibleSamplerHeap;
    }

    /// <summary>
    /// Gets the cache id used to validate resource-set descriptor table handles owned by this heap state.
    /// </summary>
    internal uint CacheId => this._cacheId;

    /// <summary>
    /// <summary>
    /// Ensures the shader-visible descriptor heaps are bound on the command list.
    /// </summary>
    /// <param name="commandList">The native command list to bind heaps on.</param>
    internal void BindDescriptorHeaps(ID3D12GraphicsCommandList commandList) {
        SetDescriptorHeapsNoAlloc(commandList, this._boundDescriptorHeaps);
    }

    /// <summary>
    /// Prepopulates persistent descriptor tables for a resource set from its layout.
    /// </summary>
    /// <param name="set">The resource set whose descriptor tables should be copied.</param>
    internal void PrepopulateDescriptorTables(D3D12ResourceSet set) {
        ResourceLayoutElementDescription[] elements = set.ResourceLayoutInfo.Elements;
        D3D12DescriptorTableBindingInfo srvUavTable = D3D12ResourceSetBindingPlan.CreateLayoutDescriptorTableBindingInfo(elements, D3D12Pipeline.DescriptorTableKind.SrvUav);
        if (srvUavTable.HasTable) {
            this.GetOrCreateDescriptorTable(set, srvUavTable, out _);
        }

        D3D12DescriptorTableBindingInfo samplerTable = D3D12ResourceSetBindingPlan.CreateLayoutDescriptorTableBindingInfo(elements, D3D12Pipeline.DescriptorTableKind.Sampler);
        if (samplerTable.HasTable) {
            this.GetOrCreateDescriptorTable(set, samplerTable, out _);
        }
    }

    /// <summary>
    /// Gets or creates the shader-visible descriptor table for a resource set and binding plan.
    /// </summary>
    /// <param name="set">The resource set whose descriptors are being bound.</param>
    /// <param name="tableInfo">The descriptor table metadata.</param>
    /// <param name="gpuHandle">The shader-visible table handle.</param>
    /// <returns>The number of descriptors copied into the shader-visible heap.</returns>
    internal uint GetOrCreateDescriptorTable(D3D12ResourceSet set, D3D12DescriptorTableBindingInfo tableInfo, out GpuDescriptorHandle gpuHandle) {
        D3D12Pipeline.DescriptorTableKind tableKind = tableInfo.TableKind;
        if (this.TryGetDescriptorTableHandle(set, tableKind, tableInfo.Signature, out gpuHandle)) {
            return 0;
        }

        DescriptorRangeRelease staleRangeRelease;
        uint copiedDescriptors;
        lock (this._allocationLock) {
            if (this.TryGetDescriptorTableHandle(set, tableKind, tableInfo.Signature, out gpuHandle)) {
                return 0;
            }

            DescriptorHeapType heapType;
            CpuDescriptorHandle cpuHandle;
            int descriptorSize;
            uint descriptorOffset;
            if (tableKind == D3D12Pipeline.DescriptorTableKind.Sampler) {
                heapType = DescriptorHeapType.Sampler;
                descriptorSize = this._samplerDescriptorSize;
                this.AllocateSamplerDescriptors(tableInfo.DescriptorCount, out descriptorOffset, out cpuHandle, out gpuHandle);
            }
            else {
                heapType = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView;
                descriptorSize = this._srvUavDescriptorSize;
                this.AllocateSrvUavDescriptors(tableInfo.DescriptorCount, out descriptorOffset, out cpuHandle, out gpuHandle);
            }

            this.EnsureDescriptorCopyCapacity(tableInfo.DescriptorCount);
            uint batchCount = 0;
            D3D12DescriptorTableBindingEntry[] entries = tableInfo.Entries;
            for (int i = 0; i < entries.Length; i++) {
                D3D12DescriptorTableBindingEntry entry = entries[i];
                this._descriptorCopyDests[batchCount] = cpuHandle + (int)(entry.DescriptorTableOffset * (uint)descriptorSize);
                this._descriptorCopySources[batchCount] = GetSourceDescriptor(set.ElementCaches[entry.ElementIndex], entry.Kind);
                batchCount++;
            }

            if (batchCount > 0) {
                this._gd.Device.CopyDescriptors(batchCount, this._descriptorCopyDests, this._descriptorCopyRangeSizes, batchCount, this._descriptorCopySources, this._descriptorCopyRangeSizes, heapType);
            }

            staleRangeRelease = this.CacheDescriptorTableHandle(set, tableKind, tableInfo.Signature, gpuHandle, descriptorOffset, tableInfo.DescriptorCount);
            copiedDescriptors = batchCount;
        }

        if (staleRangeRelease != null) {
            this._gd.ReleaseAfterLastSubmission(staleRangeRelease);
        }

        return copiedDescriptors;
    }

    /// <summary>
    /// Releases all shader-visible descriptor table ranges owned by a resource set after outstanding submissions complete.
    /// </summary>
    /// <param name="set">The resource set whose cached ranges should be released.</param>
    internal void ReleaseDescriptorTables(D3D12ResourceSet set) {
        DescriptorRange srvUavRange = default;
        DescriptorRange samplerRange = default;
        bool hasSrvUavRange = false;
        bool hasSamplerRange = false;

        lock (this._allocationLock) {
            if (set.HasCachedSrvUavHandle) {
                srvUavRange = new DescriptorRange(set.CachedSrvUavDescriptorOffset, set.CachedSrvUavDescriptorCount);
                hasSrvUavRange = srvUavRange.Count != 0;
                set.HasCachedSrvUavHandle = false;
                set.CachedSrvUavDescriptorCount = 0;
            }

            if (set.HasCachedSamplerHandle) {
                samplerRange = new DescriptorRange(set.CachedSamplerDescriptorOffset, set.CachedSamplerDescriptorCount);
                hasSamplerRange = samplerRange.Count != 0;
                set.HasCachedSamplerHandle = false;
                set.CachedSamplerDescriptorCount = 0;
            }
        }

        if (!hasSrvUavRange && !hasSamplerRange) {
            return;
        }

        this._gd.ReleaseAfterLastSubmission(new DescriptorRangeRelease(this, srvUavRange, hasSrvUavRange, samplerRange, hasSamplerRange));
    }

    /// <summary>
    /// Releases descriptor heap resources held by this instance.
    /// </summary>
    public void Dispose() {
        this._shaderVisibleSrvUavHeap?.Dispose();
        this._shaderVisibleSamplerHeap?.Dispose();
    }

    /// <summary>
    /// Allocates a contiguous range in the SRV/UAV shader-visible heap.
    /// </summary>
    /// <param name="count">The number of descriptors to allocate.</param>
    /// <param name="descriptorOffset">The first descriptor index in the allocation.</param>
    /// <param name="cpuHandle">The first CPU descriptor handle in the allocation.</param>
    /// <param name="gpuHandle">The first GPU descriptor handle in the allocation.</param>
    private void AllocateSrvUavDescriptors(uint count, out uint descriptorOffset, out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
        if (!TryRentDescriptorRange(this._freeSrvUavRanges, count, out descriptorOffset)) {
            if (this._nextSrvUavDescriptor + count > MaxSrvUavDescriptors) {
                throw new VeldridException("D3D12 global SRV/UAV descriptor heap exhausted. Create fewer unique ResourceSets or increase the persistent descriptor heap size.");
            }

            descriptorOffset = this._nextSrvUavDescriptor;
            this._nextSrvUavDescriptor += count;
        }

        cpuHandle = new CpuDescriptorHandle(this._srvUavHeapCpuStart, (int)descriptorOffset, (uint)this._srvUavDescriptorSize);
        gpuHandle = new GpuDescriptorHandle(this._srvUavHeapGpuStart, (int)descriptorOffset, (uint)this._srvUavDescriptorSize);
    }

    /// <summary>
    /// Allocates a contiguous range in the sampler shader-visible heap.
    /// </summary>
    /// <param name="count">The number of descriptors to allocate.</param>
    /// <param name="descriptorOffset">The first descriptor index in the allocation.</param>
    /// <param name="cpuHandle">The first CPU descriptor handle in the allocation.</param>
    /// <param name="gpuHandle">The first GPU descriptor handle in the allocation.</param>
    private void AllocateSamplerDescriptors(uint count, out uint descriptorOffset, out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
        if (!TryRentDescriptorRange(this._freeSamplerRanges, count, out descriptorOffset)) {
            if (this._nextSamplerDescriptor + count > MaxSamplerDescriptors) {
                throw new VeldridException("D3D12 global sampler descriptor heap exhausted. Create fewer unique ResourceSets or increase the persistent sampler descriptor heap size.");
            }

            descriptorOffset = this._nextSamplerDescriptor;
            this._nextSamplerDescriptor += count;
        }

        cpuHandle = new CpuDescriptorHandle(this._samplerHeapCpuStart, (int)descriptorOffset, (uint)this._samplerDescriptorSize);
        gpuHandle = new GpuDescriptorHandle(this._samplerHeapGpuStart, (int)descriptorOffset, (uint)this._samplerDescriptorSize);
    }

    /// <summary>
    /// Returns descriptor ranges to their free lists after their guarding fence has completed.
    /// </summary>
    /// <param name="srvUavRange">The SRV/UAV descriptor range.</param>
    /// <param name="hasSrvUavRange">Whether <paramref name="srvUavRange"/> is valid.</param>
    /// <param name="samplerRange">The sampler descriptor range.</param>
    /// <param name="hasSamplerRange">Whether <paramref name="samplerRange"/> is valid.</param>
    private void ReturnDescriptorRanges(DescriptorRange srvUavRange, bool hasSrvUavRange, DescriptorRange samplerRange, bool hasSamplerRange) {
        lock (this._allocationLock) {
            if (hasSrvUavRange) {
                AddFreeDescriptorRange(this._freeSrvUavRanges, srvUavRange);
            }

            if (hasSamplerRange) {
                AddFreeDescriptorRange(this._freeSamplerRanges, samplerRange);
            }
        }
    }

    /// <summary>
    /// Attempts to rent a descriptor range from a free list.
    /// </summary>
    /// <param name="freeRanges">The free-list to rent from.</param>
    /// <param name="count">The number of descriptors required.</param>
    /// <param name="descriptorOffset">The first descriptor index in the allocation.</param>
    /// <returns><see langword="true" /> when a free range was reused.</returns>
    private static bool TryRentDescriptorRange(List<DescriptorRange> freeRanges, uint count, out uint descriptorOffset) {
        for (int i = 0; i < freeRanges.Count; i++) {
            DescriptorRange range = freeRanges[i];
            if (range.Count < count) {
                continue;
            }

            descriptorOffset = range.Offset;
            if (range.Count == count) {
                freeRanges.RemoveAt(i);
            }
            else {
                freeRanges[i] = new DescriptorRange(range.Offset + count, range.Count - count);
            }

            return true;
        }

        descriptorOffset = 0;
        return false;
    }

    /// <summary>
    /// Adds a descriptor range back to a free-list and merges adjacent ranges.
    /// </summary>
    /// <param name="freeRanges">The target free-list.</param>
    /// <param name="range">The range to add.</param>
    private static void AddFreeDescriptorRange(List<DescriptorRange> freeRanges, DescriptorRange range) {
        if (range.Count == 0) {
            return;
        }

        int insertIndex = 0;
        while (insertIndex < freeRanges.Count && freeRanges[insertIndex].Offset < range.Offset) {
            insertIndex++;
        }

        freeRanges.Insert(insertIndex, range);
        int mergeIndex = Math.Max(0, insertIndex - 1);
        while (mergeIndex < freeRanges.Count - 1) {
            DescriptorRange current = freeRanges[mergeIndex];
            DescriptorRange next = freeRanges[mergeIndex + 1];
            uint currentEnd = current.Offset + current.Count;
            if (currentEnd < next.Offset) {
                mergeIndex++;
                continue;
            }

            uint nextEnd = Math.Max(currentEnd, next.Offset + next.Count);
            freeRanges[mergeIndex] = new DescriptorRange(current.Offset, nextEnd - current.Offset);
            freeRanges.RemoveAt(mergeIndex + 1);
        }
    }

    /// <summary>
    /// Gets the persistent CPU descriptor for a descriptor-table resource.
    /// </summary>
    /// <param name="elementCache">The pre-resolved resource-set element cache.</param>
    /// <param name="kind">The resource binding kind.</param>
    /// <returns>The CPU descriptor handle.</returns>
    private static CpuDescriptorHandle GetSourceDescriptor(D3D12ResourceSetElementCache elementCache, ResourceKind kind) {
        switch (kind) {
            case ResourceKind.TextureReadOnly: return elementCache.SrvDescriptor;
            case ResourceKind.TextureReadWrite: return elementCache.UavDescriptor;
            case ResourceKind.Sampler: return elementCache.SamplerDescriptor;
            default: throw new VeldridException("Invalid descriptor-table binding kind.");
        }
    }

    /// <summary>
    /// Ensures reusable descriptor-copy arrays can hold the requested descriptor count.
    /// </summary>
    /// <param name="count">The required descriptor count.</param>
    private void EnsureDescriptorCopyCapacity(uint count) {
        if (count <= (uint)this._descriptorCopySources.Length) {
            return;
        }

        uint newSize = (uint)this._descriptorCopySources.Length;
        while (newSize < count) {
            newSize *= 2;
        }

        Array.Resize(ref this._descriptorCopySources, (int)newSize);
        Array.Resize(ref this._descriptorCopyDests, (int)newSize);
        this._descriptorCopyRangeSizes = CreateDescriptorCopyRangeSizes((int)newSize);
    }

    /// <summary>
    /// Creates a descriptor-copy range-size array initialized to one descriptor per range.
    /// </summary>
    /// <param name="count">The number of range sizes to create.</param>
    /// <returns>The initialized range-size array.</returns>
    private static uint[] CreateDescriptorCopyRangeSizes(int count) {
        uint[] sizes = new uint[count];
        for (int i = 0; i < sizes.Length; i++) {
            sizes[i] = 1;
        }

        return sizes;
    }

    /// <summary>
    /// Attempts to reuse a cached shader-visible descriptor table handle.
    /// </summary>
    /// <param name="set">The resource set that owns the descriptor cache.</param>
    /// <param name="kind">The descriptor-table resource kind.</param>
    /// <param name="tableSignature">The binding-plan table signature.</param>
    /// <param name="handle">The cached GPU descriptor handle, when available.</param>
    /// <returns><see langword="true" /> when a cached handle was found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetDescriptorTableHandle(D3D12ResourceSet set, D3D12Pipeline.DescriptorTableKind kind, uint tableSignature, out GpuDescriptorHandle handle) {
        if (kind == D3D12Pipeline.DescriptorTableKind.Sampler) {
            if (set.HasCachedSamplerHandle && set.CachedSamplerHeapId == this._cacheId && set.CachedSamplerSignature == tableSignature) {
                handle = set.CachedSamplerHandle;
                return true;
            }
        }
        else {
            if (set.HasCachedSrvUavHandle && set.CachedSrvUavHeapId == this._cacheId && set.CachedSrvUavSignature == tableSignature) {
                handle = set.CachedSrvUavHandle;
                return true;
            }
        }

        handle = default;
        return false;
    }

    /// <summary>
    /// Stores a shader-visible descriptor table handle for reuse by later bindings.
    /// </summary>
    /// <param name="set">The resource set that owns the descriptor cache.</param>
    /// <param name="kind">The descriptor-table resource kind.</param>
    /// <param name="tableSignature">The binding-plan table signature.</param>
    /// <param name="handle">The GPU descriptor handle to cache.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DescriptorRangeRelease CacheDescriptorTableHandle(D3D12ResourceSet set, D3D12Pipeline.DescriptorTableKind kind, uint tableSignature, GpuDescriptorHandle handle, uint descriptorOffset, uint descriptorCount) {
        if (kind == D3D12Pipeline.DescriptorTableKind.Sampler) {
            DescriptorRangeRelease staleRangeRelease = set.HasCachedSamplerHandle && set.CachedSamplerDescriptorCount != 0
                ? new DescriptorRangeRelease(this, default, false, new DescriptorRange(set.CachedSamplerDescriptorOffset, set.CachedSamplerDescriptorCount), true)
                : null;
            set.CachedSamplerHandle = handle;
            set.CachedSamplerHeapId = this._cacheId;
            set.CachedSamplerSignature = tableSignature;
            set.CachedSamplerDescriptorOffset = descriptorOffset;
            set.CachedSamplerDescriptorCount = descriptorCount;
            set.HasCachedSamplerHandle = true;
            return staleRangeRelease;
        }
        else {
            DescriptorRangeRelease staleRangeRelease = set.HasCachedSrvUavHandle && set.CachedSrvUavDescriptorCount != 0
                ? new DescriptorRangeRelease(this, new DescriptorRange(set.CachedSrvUavDescriptorOffset, set.CachedSrvUavDescriptorCount), true, default, false)
                : null;
            set.CachedSrvUavHandle = handle;
            set.CachedSrvUavHeapId = this._cacheId;
            set.CachedSrvUavSignature = tableSignature;
            set.CachedSrvUavDescriptorOffset = descriptorOffset;
            set.CachedSrvUavDescriptorCount = descriptorCount;
            set.HasCachedSrvUavHandle = true;
            return staleRangeRelease;
        }
    }

    /// <summary>
    /// Sets descriptor heaps without going through the managed COM wrapper.
    /// </summary>
    /// <param name="commandList">The command list to update.</param>
    /// <param name="heaps">The descriptor heaps to bind.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void SetDescriptorHeapsNoAlloc(ID3D12GraphicsCommandList commandList, ID3D12DescriptorHeap[] heaps) {
        void** vtbl = *(void***)commandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, void> setDescriptorHeaps = (delegate* unmanaged[Stdcall]<void*, uint, void*, void>)vtbl[28];
        void* heap0 = (void*)heaps[0].NativePointer;
        void* heap1 = (void*)heaps[1].NativePointer;
        void** heapPtrs = stackalloc void*[2] { heap0, heap1 };
        setDescriptorHeaps((void*)commandList.NativePointer, 2u, heapPtrs);
    }

    /// <summary>
    /// Represents one contiguous range in a shader-visible descriptor heap.
    /// </summary>
    private readonly struct DescriptorRange {

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorRange" /> struct.
        /// </summary>
        /// <param name="offset">The first descriptor index.</param>
        /// <param name="count">The number of descriptors.</param>
        public DescriptorRange(uint offset, uint count) {
            this.Offset = offset;
            this.Count = count;
        }

        /// <summary>
        /// Gets the first descriptor index.
        /// </summary>
        public uint Offset { get; }

        /// <summary>
        /// Gets the number of descriptors.
        /// </summary>
        public uint Count { get; }
    }

    /// <summary>
    /// Returns descriptor ranges after deferred disposal reaches their fence.
    /// </summary>
    private sealed class DescriptorRangeRelease : IDisposable {

        /// <summary>
        /// Stores the descriptor heap state that owns the ranges.
        /// </summary>
        private readonly D3D12DescriptorHeapState _owner;

        /// <summary>
        /// Stores the SRV/UAV descriptor range.
        /// </summary>
        private readonly DescriptorRange _srvUavRange;

        /// <summary>
        /// Stores whether the SRV/UAV range is valid.
        /// </summary>
        private readonly bool _hasSrvUavRange;

        /// <summary>
        /// Stores the sampler descriptor range.
        /// </summary>
        private readonly DescriptorRange _samplerRange;

        /// <summary>
        /// Stores whether the sampler range is valid.
        /// </summary>
        private readonly bool _hasSamplerRange;

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorRangeRelease" /> class.
        /// </summary>
        /// <param name="owner">The descriptor heap state that owns the ranges.</param>
        /// <param name="srvUavRange">The SRV/UAV descriptor range.</param>
        /// <param name="hasSrvUavRange">Whether <paramref name="srvUavRange"/> is valid.</param>
        /// <param name="samplerRange">The sampler descriptor range.</param>
        /// <param name="hasSamplerRange">Whether <paramref name="samplerRange"/> is valid.</param>
        public DescriptorRangeRelease(D3D12DescriptorHeapState owner, DescriptorRange srvUavRange, bool hasSrvUavRange, DescriptorRange samplerRange, bool hasSamplerRange) {
            this._owner = owner;
            this._srvUavRange = srvUavRange;
            this._hasSrvUavRange = hasSrvUavRange;
            this._samplerRange = samplerRange;
            this._hasSamplerRange = hasSamplerRange;
        }

        /// <inheritdoc />
        public void Dispose() {
            this._owner.ReturnDescriptorRanges(this._srvUavRange, this._hasSrvUavRange, this._samplerRange, this._hasSamplerRange);
        }
    }
}
