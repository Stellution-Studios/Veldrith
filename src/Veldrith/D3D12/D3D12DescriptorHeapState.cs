using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Owns the shader-visible descriptor heaps used while recording a D3D12 command list.
/// </summary>
internal sealed class D3D12DescriptorHeapState : IDisposable {

    /// <summary>
    /// Stores the next shader-visible descriptor heap cache id.
    /// </summary>
    private static int s_nextCacheId;

    /// <summary>
    /// Stores the default number of sampler descriptors retained by the command-list heap.
    /// </summary>
    private const uint MaxSamplerDescriptors = 1536;

    /// <summary>
    /// Stores the default number of SRV/UAV descriptors retained by the command-list heap.
    /// </summary>
    private const uint MaxSrvUavDescriptors = 49152;

    /// <summary>
    /// Stores the graphics device used to create descriptors and descriptor heaps.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

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
    /// Tracks whether the heaps have been bound for the current command-list recording.
    /// </summary>
    private bool _descriptorHeapsBound;

    /// <summary>
    /// Stores the next free sampler descriptor in the persistent heap.
    /// </summary>
    private uint _nextSamplerDescriptor;

    /// <summary>
    /// Stores the exclusive sampler descriptor limit.
    /// </summary>
    private uint _samplerDescriptorLimit;

    /// <summary>
    /// Stores the next free SRV/UAV descriptor in the persistent heap.
    /// </summary>
    private uint _nextSrvUavDescriptor;

    /// <summary>
    /// Stores the exclusive SRV/UAV descriptor limit.
    /// </summary>
    private uint _srvUavDescriptorLimit;

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
        this._srvUavDescriptorLimit = MaxSrvUavDescriptors;
        this._samplerDescriptorLimit = MaxSamplerDescriptors;
        this._srvUavHeapCpuStart = this._shaderVisibleSrvUavHeap.GetCPUDescriptorHandleForHeapStart();
        this._srvUavHeapGpuStart = this._shaderVisibleSrvUavHeap.GetGPUDescriptorHandleForHeapStart();
        this._samplerHeapCpuStart = this._shaderVisibleSamplerHeap.GetCPUDescriptorHandleForHeapStart();
        this._samplerHeapGpuStart = this._shaderVisibleSamplerHeap.GetGPUDescriptorHandleForHeapStart();
    }

    /// <summary>
    /// Resets per-recording heap binding state.
    /// </summary>
    internal void BeginRecording() {
        this._descriptorHeapsBound = false;
        this._srvUavDescriptorLimit = MaxSrvUavDescriptors;
        this._samplerDescriptorLimit = MaxSamplerDescriptors;
    }

    /// <summary>
    /// Ensures the shader-visible descriptor heaps are bound on the command list.
    /// </summary>
    /// <param name="commandList">The native command list to bind heaps on.</param>
    internal void BindDescriptorHeaps(ID3D12GraphicsCommandList commandList) {
        if (this._descriptorHeapsBound) {
            return;
        }

        this._boundDescriptorHeaps[0] = this._shaderVisibleSrvUavHeap;
        this._boundDescriptorHeaps[1] = this._shaderVisibleSamplerHeap;
        SetDescriptorHeapsNoAlloc(commandList, this._boundDescriptorHeaps);
        this._descriptorHeapsBound = true;
    }

    /// <summary>
    /// Gets or creates the shader-visible descriptor table for a resource set and binding plan.
    /// </summary>
    /// <param name="set">The resource set whose descriptors are being bound.</param>
    /// <param name="bindingPlan">The binding plan entries for the resource set slot.</param>
    /// <param name="tableInfo">The descriptor table metadata.</param>
    /// <param name="gpuHandle">The shader-visible table handle.</param>
    /// <returns>The number of descriptors copied into the shader-visible heap.</returns>
    internal uint GetOrCreateDescriptorTable(D3D12ResourceSet set, D3D12ResourceSetBindingPlanEntry[] bindingPlan, D3D12DescriptorTableBindingInfo tableInfo, out GpuDescriptorHandle gpuHandle) {
        D3D12Pipeline.DescriptorTableKind tableKind = tableInfo.TableKind;
        if (this.TryGetDescriptorTableHandle(set, tableKind, tableInfo.Signature, out gpuHandle)) {
            return 0;
        }

        DescriptorHeapType heapType;
        CpuDescriptorHandle cpuHandle;
        int descriptorSize;
        if (tableKind == D3D12Pipeline.DescriptorTableKind.Sampler) {
            heapType = DescriptorHeapType.Sampler;
            descriptorSize = this._samplerDescriptorSize;
            this.AllocateSamplerDescriptors(tableInfo.DescriptorCount, out cpuHandle, out gpuHandle);
        }
        else {
            heapType = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView;
            descriptorSize = this._srvUavDescriptorSize;
            this.AllocateSrvUavDescriptors(tableInfo.DescriptorCount, out cpuHandle, out gpuHandle);
        }

        this.EnsureDescriptorCopyCapacity(tableInfo.DescriptorCount);
        uint batchCount = 0;
        for (int i = 0; i < bindingPlan.Length; i++) {
            D3D12ResourceSetBindingPlanEntry entry = bindingPlan[i];
            D3D12Pipeline.RootBindingInfo bindingInfo = entry.BindingInfo;
            if (!bindingInfo.DescriptorTable || bindingInfo.DescriptorTableKind != tableKind) {
                continue;
            }

            this._descriptorCopyDests[batchCount] = cpuHandle + (int)(bindingInfo.DescriptorTableOffset * (uint)descriptorSize);
            this._descriptorCopySources[batchCount] = GetSourceDescriptor(set.ElementCaches[entry.ElementIndex], bindingInfo.Kind);
            batchCount++;
        }

        if (batchCount > 0) {
            this._gd.Device.CopyDescriptors(batchCount, this._descriptorCopyDests, this._descriptorCopyRangeSizes, batchCount, this._descriptorCopySources, this._descriptorCopyRangeSizes, heapType);
        }

        this.CacheDescriptorTableHandle(set, tableKind, tableInfo.Signature, gpuHandle);
        return batchCount;
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
    /// <param name="cpuHandle">The first CPU descriptor handle in the allocation.</param>
    /// <param name="gpuHandle">The first GPU descriptor handle in the allocation.</param>
    private void AllocateSrvUavDescriptors(uint count, out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
        if (this._nextSrvUavDescriptor + count > this._srvUavDescriptorLimit) {
            throw new VeldridException("D3D12 SRV/UAV descriptor heap exhausted for this CommandList. Create fewer unique ResourceSets or increase the persistent descriptor heap size.");
        }

        cpuHandle = new CpuDescriptorHandle(this._srvUavHeapCpuStart, (int)this._nextSrvUavDescriptor, (uint)this._srvUavDescriptorSize);
        gpuHandle = new GpuDescriptorHandle(this._srvUavHeapGpuStart, (int)this._nextSrvUavDescriptor, (uint)this._srvUavDescriptorSize);
        this._nextSrvUavDescriptor += count;
    }

    /// <summary>
    /// Allocates a contiguous range in the sampler shader-visible heap.
    /// </summary>
    /// <param name="count">The number of descriptors to allocate.</param>
    /// <param name="cpuHandle">The first CPU descriptor handle in the allocation.</param>
    /// <param name="gpuHandle">The first GPU descriptor handle in the allocation.</param>
    private void AllocateSamplerDescriptors(uint count, out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
        if (this._nextSamplerDescriptor + count > this._samplerDescriptorLimit) {
            throw new VeldridException("D3D12 sampler descriptor heap exhausted for this CommandList. Create fewer unique ResourceSets or increase the persistent sampler descriptor heap size.");
        }

        cpuHandle = new CpuDescriptorHandle(this._samplerHeapCpuStart, (int)this._nextSamplerDescriptor, (uint)this._samplerDescriptorSize);
        gpuHandle = new GpuDescriptorHandle(this._samplerHeapGpuStart, (int)this._nextSamplerDescriptor, (uint)this._samplerDescriptorSize);
        this._nextSamplerDescriptor += count;
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
    private void CacheDescriptorTableHandle(D3D12ResourceSet set, D3D12Pipeline.DescriptorTableKind kind, uint tableSignature, GpuDescriptorHandle handle) {
        if (kind == D3D12Pipeline.DescriptorTableKind.Sampler) {
            set.CachedSamplerHandle = handle;
            set.CachedSamplerHeapId = this._cacheId;
            set.CachedSamplerSignature = tableSignature;
            set.HasCachedSamplerHandle = true;
        }
        else {
            set.CachedSrvUavHandle = handle;
            set.CachedSrvUavHeapId = this._cacheId;
            set.CachedSrvUavSignature = tableSignature;
            set.HasCachedSrvUavHandle = true;
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
}
