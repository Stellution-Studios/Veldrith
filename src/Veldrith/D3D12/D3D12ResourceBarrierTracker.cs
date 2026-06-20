using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Accumulates D3D12 resource barriers and emits them in batches.
/// </summary>
internal sealed class D3D12ResourceBarrierTracker {

    /// <summary>
    /// Stores a reusable single-barrier array for UAV barrier emission.
    /// </summary>
    private readonly ResourceBarrier[] _singleBarrier = new ResourceBarrier[1];

    /// <summary>
    /// Stores pending transition barriers before batched emission.
    /// </summary>
    private readonly ResourceBarrier[] _barrierBatch = new ResourceBarrier[32];

    /// <summary>
    /// Stores the resource associated with each pending transition barrier.
    /// </summary>
    private readonly ID3D12Resource[] _barrierResources = new ID3D12Resource[32];

    /// <summary>
    /// Stores the source state associated with each pending transition barrier.
    /// </summary>
    private readonly ResourceStates[] _barrierFromStates = new ResourceStates[32];

    /// <summary>
    /// Stores the destination state associated with each pending transition barrier.
    /// </summary>
    private readonly ResourceStates[] _barrierToStates = new ResourceStates[32];

    /// <summary>
    /// Stores the subresource index associated with each pending transition barrier.
    /// </summary>
    private readonly uint[] _barrierSubresources = new uint[32];

    /// <summary>
    /// Stores whether each pending transition barrier targets a specific subresource.
    /// </summary>
    private readonly bool[] _barrierUsesSubresource = new bool[32];

    /// <summary>
    /// Stores the number of pending transition barriers in <see cref="_barrierBatch" />.
    /// </summary>
    private uint _pendingBarrierCount;

    /// <summary>
    /// Gets the number of pending transitions folded into an earlier pending transition since the last reset.
    /// </summary>
    internal ulong CoalescedTransitions { get; private set; }

    /// <summary>
    /// Gets the number of pending transitions removed because later work returned the resource to its original state.
    /// </summary>
    internal ulong RemovedTransitions { get; private set; }

    /// <summary>
    /// Gets whether any transition barriers are queued.
    /// </summary>
    internal bool HasQueuedBarriers => this._pendingBarrierCount != 0;

    /// <summary>
    /// Gets whether the transition barrier batch has reached capacity.
    /// </summary>
    internal bool IsFull => this._pendingBarrierCount == (uint)this._barrierBatch.Length;

    /// <summary>
    /// Gets or sets whether a UAV barrier must be emitted before subsequent work.
    /// </summary>
    internal bool UavBarrierPending { get; set; }

    /// <summary>
    /// Clears pending transition and UAV barrier state.
    /// </summary>
    internal void Reset() {
        this.ClearQueuedBarrierMetadata();
        this._pendingBarrierCount = 0;
        this.CoalescedTransitions = 0;
        this.RemovedTransitions = 0;
        this.UavBarrierPending = false;
    }

    /// <summary>
    /// Queues a whole-resource transition barrier when the state changes.
    /// </summary>
    /// <param name="resource">The D3D12 resource to transition.</param>
    /// <param name="from">The previous resource state.</param>
    /// <param name="to">The target resource state.</param>
    /// <returns>The queue result describing how the transition was handled.</returns>
    internal D3D12BarrierQueueResult QueueTransition(ID3D12Resource resource, ResourceStates from, ResourceStates to) {
        if (from == to) {
            return D3D12BarrierQueueResult.Skipped;
        }

        D3D12BarrierQueueResult coalesceResult = this.TryCoalesceTransition(resource, from, to, 0, false);
        if (coalesceResult != D3D12BarrierQueueResult.Skipped) {
            return coalesceResult;
        }

        if (this.IsFull) {
            return D3D12BarrierQueueResult.Full;
        }

        uint index = this._pendingBarrierCount++;
        this._barrierBatch[index] = ResourceBarrier.BarrierTransition(resource, from, to);
        this.StoreBarrierMetadata(index, resource, from, to, 0, false);
        return D3D12BarrierQueueResult.Queued;
    }

    /// <summary>
    /// Queues a subresource transition barrier when the state changes.
    /// </summary>
    /// <param name="resource">The D3D12 resource to transition.</param>
    /// <param name="from">The previous resource state.</param>
    /// <param name="to">The target resource state.</param>
    /// <param name="subresource">The subresource index to transition.</param>
    /// <returns>The queue result describing how the transition was handled.</returns>
    internal D3D12BarrierQueueResult QueueSubresourceTransition(ID3D12Resource resource, ResourceStates from, ResourceStates to, uint subresource) {
        if (from == to) {
            return D3D12BarrierQueueResult.Skipped;
        }

        D3D12BarrierQueueResult coalesceResult = this.TryCoalesceTransition(resource, from, to, subresource, true);
        if (coalesceResult != D3D12BarrierQueueResult.Skipped) {
            return coalesceResult;
        }

        if (this.IsFull) {
            return D3D12BarrierQueueResult.Full;
        }

        uint index = this._pendingBarrierCount++;
        this._barrierBatch[index] = ResourceBarrier.BarrierTransition(resource, from, to, subresource);
        this.StoreBarrierMetadata(index, resource, from, to, subresource, true);
        return D3D12BarrierQueueResult.Queued;
    }

    /// <summary>
    /// Emits all pending transition barriers in one D3D12 call.
    /// </summary>
    /// <param name="commandList">The command list that receives the barriers.</param>
    /// <returns><see langword="true" /> when barriers were emitted.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool FlushPendingBarriers(ID3D12GraphicsCommandList commandList) {
        if (this._pendingBarrierCount == 0) {
            return false;
        }

        ResourceBarrierBatchNoAlloc(commandList, this._barrierBatch, this._pendingBarrierCount);
        this.ClearQueuedBarrierMetadata();
        this._pendingBarrierCount = 0;
        return true;
    }

    /// <summary>
    /// Attempts to fold a new transition into an already queued transition for the same resource range.
    /// </summary>
    /// <param name="resource">The D3D12 resource to transition.</param>
    /// <param name="from">The previous resource state.</param>
    /// <param name="to">The target resource state.</param>
    /// <param name="subresource">The subresource index, or zero for whole-resource transitions.</param>
    /// <param name="usesSubresource">Whether the transition targets one subresource.</param>
    /// <returns>The queue result when the transition was folded or removed; otherwise <see cref="D3D12BarrierQueueResult.Skipped" />.</returns>
    private D3D12BarrierQueueResult TryCoalesceTransition(ID3D12Resource resource, ResourceStates from, ResourceStates to, uint subresource, bool usesSubresource) {
        for (uint i = 0; i < this._pendingBarrierCount; i++) {
            if (!ReferenceEquals(this._barrierResources[i], resource)
                || this._barrierUsesSubresource[i] != usesSubresource
                || (usesSubresource && this._barrierSubresources[i] != subresource)
                || this._barrierToStates[i] != from) {
                continue;
            }

            ResourceStates originalFrom = this._barrierFromStates[i];
            if (originalFrom == to) {
                this.RemoveQueuedBarrier(i);
                this.RemovedTransitions++;
                return D3D12BarrierQueueResult.Removed;
            }

            this._barrierBatch[i] = usesSubresource
                ? ResourceBarrier.BarrierTransition(resource, originalFrom, to, subresource)
                : ResourceBarrier.BarrierTransition(resource, originalFrom, to);
            this._barrierToStates[i] = to;
            this.CoalescedTransitions++;
            return D3D12BarrierQueueResult.Coalesced;
        }

        return D3D12BarrierQueueResult.Skipped;
    }

    /// <summary>
    /// Stores metadata used to coalesce pending transitions before emission.
    /// </summary>
    /// <param name="index">The pending barrier index.</param>
    /// <param name="resource">The D3D12 resource to transition.</param>
    /// <param name="from">The previous resource state.</param>
    /// <param name="to">The target resource state.</param>
    /// <param name="subresource">The subresource index, or zero for whole-resource transitions.</param>
    /// <param name="usesSubresource">Whether the transition targets one subresource.</param>
    private void StoreBarrierMetadata(uint index, ID3D12Resource resource, ResourceStates from, ResourceStates to, uint subresource, bool usesSubresource) {
        this._barrierResources[index] = resource;
        this._barrierFromStates[index] = from;
        this._barrierToStates[index] = to;
        this._barrierSubresources[index] = subresource;
        this._barrierUsesSubresource[index] = usesSubresource;
    }

    /// <summary>
    /// Removes a queued transition barrier by compacting later pending barriers.
    /// </summary>
    /// <param name="index">The pending barrier index to remove.</param>
    private void RemoveQueuedBarrier(uint index) {
        uint lastIndex = this._pendingBarrierCount - 1;
        for (uint i = index; i < lastIndex; i++) {
            uint next = i + 1;
            this._barrierBatch[i] = this._barrierBatch[next];
            this._barrierResources[i] = this._barrierResources[next];
            this._barrierFromStates[i] = this._barrierFromStates[next];
            this._barrierToStates[i] = this._barrierToStates[next];
            this._barrierSubresources[i] = this._barrierSubresources[next];
            this._barrierUsesSubresource[i] = this._barrierUsesSubresource[next];
        }

        this._barrierBatch[lastIndex] = default;
        this._barrierResources[lastIndex] = null;
        this._pendingBarrierCount = lastIndex;
    }

    /// <summary>
    /// Clears queued transition barriers and releases resource references for the valid range.
    /// </summary>
    private void ClearQueuedBarrierMetadata() {
        for (uint i = 0; i < this._pendingBarrierCount; i++) {
            this._barrierBatch[i] = default;
            this._barrierResources[i] = null;
        }
    }

    /// <summary>
    /// Emits a pending UAV barrier if one was requested.
    /// </summary>
    /// <param name="commandList">The command list that receives the barrier.</param>
    /// <returns><see langword="true" /> when a UAV barrier was emitted.</returns>
    internal bool FlushPendingUavBarrier(ID3D12GraphicsCommandList commandList) {
        if (!this.UavBarrierPending) {
            return false;
        }

        this._singleBarrier[0] = ResourceBarrier.BarrierUnorderedAccessView(null);
        ResourceBarrierNoAlloc(commandList, ref this._singleBarrier[0]);
        this.UavBarrierPending = false;
        return true;
    }

    /// <summary>
    /// Emits one resource barrier without going through the managed COM wrapper.
    /// </summary>
    /// <param name="commandList">The command list that receives the barrier.</param>
    /// <param name="barrier">The barrier to emit.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ResourceBarrierNoAlloc(ID3D12GraphicsCommandList commandList, ref ResourceBarrier barrier) {
        fixed (ResourceBarrier* barrierPtr = &barrier) {
            void** vtbl = *(void***)commandList.NativePointer;
            delegate* unmanaged[Stdcall]<void*, uint, ResourceBarrier*, void> fn = (delegate* unmanaged[Stdcall]<void*, uint, ResourceBarrier*, void>)vtbl[26];
            fn((void*)commandList.NativePointer, 1, barrierPtr);
        }
    }

    /// <summary>
    /// Emits a resource barrier batch without going through the managed COM wrapper.
    /// </summary>
    /// <param name="commandList">The command list that receives the barriers.</param>
    /// <param name="barriers">The barrier batch to emit.</param>
    /// <param name="count">The number of valid barriers in the batch.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ResourceBarrierBatchNoAlloc(ID3D12GraphicsCommandList commandList, ResourceBarrier[] barriers, uint count) {
        fixed (ResourceBarrier* barriersPtr = barriers) {
            void** vtbl = *(void***)commandList.NativePointer;
            delegate* unmanaged[Stdcall]<void*, uint, ResourceBarrier*, void> fn = (delegate* unmanaged[Stdcall]<void*, uint, ResourceBarrier*, void>)vtbl[26];
            fn((void*)commandList.NativePointer, count, barriersPtr);
        }
    }
}

/// <summary>
/// Describes how a pending D3D12 resource transition was handled by the barrier tracker.
/// </summary>
internal enum D3D12BarrierQueueResult {

    /// <summary>
    /// Indicates that no transition was needed or no pending transition matched.
    /// </summary>
    Skipped,

    /// <summary>
    /// Indicates that a new transition barrier was appended to the pending batch.
    /// </summary>
    Queued,

    /// <summary>
    /// Indicates that the transition was folded into an existing pending transition.
    /// </summary>
    Coalesced,

    /// <summary>
    /// Indicates that an existing pending transition was removed because the final state matched the original state.
    /// </summary>
    Removed,

    /// <summary>
    /// Indicates that the pending batch is full and must be flushed before appending this transition.
    /// </summary>
    Full
}
