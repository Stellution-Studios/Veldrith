using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Batches small immediate D3D12 buffer updates into shared upload pages.
/// </summary>
internal sealed class D3D12ImmediateBufferUpdateBatcher {

    /// <summary>
    /// Stores the upload page size used by batched immediate buffer updates.
    /// </summary>
    private const uint BatchedUploadPageSize = 64 * 1024;

    /// <summary>
    /// Stores the largest immediate buffer update that should share a batched upload page.
    /// </summary>
    private const uint MaxBatchedUpdateSize = 16 * 1024;

    /// <summary>
    /// Stores the graphics device that owns upload allocations.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Stores queued immediate buffer updates that share the batched immediate command list.
    /// </summary>
    private readonly List<PendingUpdate> _pendingUpdates = new(32);

    /// <summary>
    /// Stores unique destination buffers touched by the queued immediate update batch.
    /// </summary>
    private readonly List<PendingBufferState> _pendingBuffers = new(8);

    /// <summary>
    /// Stores transition barriers before they are emitted into the immediate command list.
    /// </summary>
    private ResourceBarrier[] _barrierBatch = new ResourceBarrier[32];

    /// <summary>
    /// Stores the number of queued transition barriers in <see cref="_barrierBatch" />.
    /// </summary>
    private uint _pendingBarrierCount;

    /// <summary>
    /// Stores the upload allocation currently used for small immediate buffer updates.
    /// </summary>
    private D3D12ResourceAllocation _batchedUpload;

    /// <summary>
    /// Stores the next free byte in the current batched upload allocation.
    /// </summary>
    private ulong _batchedUploadOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ImmediateBufferUpdateBatcher" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns upload allocations.</param>
    internal D3D12ImmediateBufferUpdateBatcher(D3D12GraphicsDevice gd) {
        this._gd = gd;
    }

    /// <summary>
    /// Checks whether an immediate buffer update should share the batched upload page.
    /// </summary>
    /// <param name="sizeInBytes">The update size.</param>
    /// <returns><see langword="true" /> when the update is small enough for the shared page.</returns>
    internal bool ShouldBatch(uint sizeInBytes) {
        return sizeInBytes <= MaxBatchedUpdateSize;
    }

    /// <summary>
    /// Queues a small immediate buffer update into the current batched upload page.
    /// </summary>
    /// <param name="commandList">The immediate command list that receives copy commands.</param>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="source">The source data pointer.</param>
    /// <param name="bufferOffsetInBytes">The destination byte offset.</param>
    /// <param name="sizeInBytes">The number of bytes to copy.</param>
    /// <returns>A newly rented upload allocation that must be retained until the batch completes, or <see langword="null" />.</returns>
    internal unsafe D3D12ResourceAllocation QueueLocked(ID3D12GraphicsCommandList commandList, D3D12DeviceBuffer buffer, IntPtr source, uint bufferOffsetInBytes, uint sizeInBytes) {
        buffer.ValidateCommandListUpdateRange(bufferOffsetInBytes, sizeInBytes);

        D3D12ResourceAllocation uploadToRetain = this.EnsureBatchedUploadSpace(commandList, sizeInBytes);
        ulong uploadOffset = this._batchedUploadOffset;
        byte* destination = (byte*)this._batchedUpload.MappedPointer.ToPointer() + uploadOffset;
        Buffer.MemoryCopy(source.ToPointer(), destination, sizeInBytes, sizeInBytes);
        this.AppendOrMergePendingUpdate(buffer, this._batchedUpload, bufferOffsetInBytes, uploadOffset, sizeInBytes);
        this._batchedUploadOffset = AlignUp(uploadOffset + sizeInBytes, 4);
        return uploadToRetain;
    }

    /// <summary>
    /// Records queued immediate buffer updates into the native command list.
    /// </summary>
    /// <param name="commandList">The immediate command list that receives copy commands.</param>
    internal void FlushLocked(ID3D12GraphicsCommandList commandList) {
        if (this._pendingUpdates.Count == 0) {
            return;
        }

        this.CapturePendingBufferStates();
        for (int i = 0; i < this._pendingBuffers.Count; i++) {
            this.QueueTransitionBuffer(this._pendingBuffers[i].Buffer, this._pendingBuffers[i].PreviousState, ResourceStates.CopyDest);
        }

        this.FlushTransitionBarriers(commandList);
        for (int i = 0; i < this._pendingUpdates.Count; i++) {
            PendingUpdate update = this._pendingUpdates[i];
            commandList.CopyBufferRegion(update.Buffer.NativeBuffer, update.DestinationOffset, update.Upload.Resource, update.Upload.Offset + update.UploadOffset, update.SizeInBytes);
        }

        for (int i = 0; i < this._pendingBuffers.Count; i++) {
            PendingBufferState state = this._pendingBuffers[i];
            this.QueueTransitionBuffer(state.Buffer, ResourceStates.CopyDest, state.PreviousState);
        }

        this.FlushTransitionBarriers(commandList);
        this._pendingUpdates.Clear();
        this._pendingBuffers.Clear();
    }

    /// <summary>
    /// Clears transient page state after the owning immediate command batch has been submitted.
    /// </summary>
    internal void ResetAfterFlush() {
        this._pendingUpdates.Clear();
        this._pendingBuffers.Clear();
        this._pendingBarrierCount = 0;
        this._batchedUpload = null;
        this._batchedUploadOffset = 0;
    }

    /// <summary>
    /// Ensures that the current immediate upload page can hold another update.
    /// </summary>
    /// <param name="commandList">The immediate command list that receives flushed copy commands when a page rolls over.</param>
    /// <param name="sizeInBytes">The required update size.</param>
    /// <returns>A newly rented upload allocation that must be retained, or <see langword="null" /> when the existing page is reused.</returns>
    private D3D12ResourceAllocation EnsureBatchedUploadSpace(ID3D12GraphicsCommandList commandList, uint sizeInBytes) {
        if (this._batchedUpload != null && this._batchedUploadOffset + sizeInBytes <= this._batchedUpload.Size) {
            return null;
        }

        this.FlushLocked(commandList);
        this._batchedUpload = this._gd.RentUploadBuffer(BatchedUploadPageSize);
        this._batchedUploadOffset = 0;
        return this._batchedUpload;
    }

    /// <summary>
    /// Appends a pending update or merges it with the previous update when source and destination ranges are contiguous.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="upload">The upload allocation containing source data.</param>
    /// <param name="destinationOffset">The destination byte offset.</param>
    /// <param name="uploadOffset">The source byte offset inside the upload allocation.</param>
    /// <param name="sizeInBytes">The number of bytes to copy.</param>
    private void AppendOrMergePendingUpdate(D3D12DeviceBuffer buffer, D3D12ResourceAllocation upload, uint destinationOffset, ulong uploadOffset, uint sizeInBytes) {
        int lastIndex = this._pendingUpdates.Count - 1;
        if (lastIndex >= 0) {
            PendingUpdate last = this._pendingUpdates[lastIndex];
            if (ReferenceEquals(last.Buffer, buffer)
                && ReferenceEquals(last.Upload, upload)
                && last.DestinationOffset + last.SizeInBytes == destinationOffset
                && last.UploadOffset + last.SizeInBytes == uploadOffset) {
                this._pendingUpdates[lastIndex] = new PendingUpdate(buffer, upload, last.DestinationOffset, last.UploadOffset, last.SizeInBytes + sizeInBytes);
                return;
            }
        }

        this._pendingUpdates.Add(new PendingUpdate(buffer, upload, destinationOffset, uploadOffset, sizeInBytes));
    }

    /// <summary>
    /// Captures the current destination state for every unique buffer touched by queued updates.
    /// </summary>
    private void CapturePendingBufferStates() {
        this._pendingBuffers.Clear();
        D3D12DeviceBuffer lastPendingBuffer = null;
        for (int i = 0; i < this._pendingUpdates.Count; i++) {
            D3D12DeviceBuffer buffer = this._pendingUpdates[i].Buffer;
            if (ReferenceEquals(buffer, lastPendingBuffer) || this.HasPendingBuffer(buffer)) {
                continue;
            }

            this._pendingBuffers.Add(new PendingBufferState(buffer, buffer.CurrentState));
            lastPendingBuffer = buffer;
        }
    }

    /// <summary>
    /// Checks whether the pending-buffer list already contains a buffer.
    /// </summary>
    /// <param name="buffer">The buffer to find.</param>
    /// <returns><see langword="true" /> when the buffer is already tracked.</returns>
    private bool HasPendingBuffer(D3D12DeviceBuffer buffer) {
        for (int i = 0; i < this._pendingBuffers.Count; i++) {
            if (ReferenceEquals(this._pendingBuffers[i].Buffer, buffer)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Queues a buffer transition and mirrors the resulting state on the buffer.
    /// </summary>
    /// <param name="buffer">The buffer to transition.</param>
    /// <param name="from">The previous state.</param>
    /// <param name="to">The target state.</param>
    private void QueueTransitionBuffer(D3D12DeviceBuffer buffer, ResourceStates from, ResourceStates to) {
        if (from != to) {
            this.EnsureBarrierCapacity(this._pendingBarrierCount + 1);
            this._barrierBatch[this._pendingBarrierCount++] = ResourceBarrier.BarrierTransition(buffer.NativeBuffer, from, to);
        }

        buffer.CurrentState = to;
    }

    /// <summary>
    /// Emits queued transition barriers in one native D3D12 call.
    /// </summary>
    /// <param name="commandList">The immediate command list that receives the barriers.</param>
    private void FlushTransitionBarriers(ID3D12GraphicsCommandList commandList) {
        if (this._pendingBarrierCount == 0) {
            return;
        }

        ResourceBarrierBatchNoAlloc(commandList, this._barrierBatch, this._pendingBarrierCount);
        Array.Clear(this._barrierBatch, 0, checked((int)this._pendingBarrierCount));
        this._pendingBarrierCount = 0;
    }

    /// <summary>
    /// Ensures that the reusable barrier array is large enough.
    /// </summary>
    /// <param name="requiredCount">The required barrier count.</param>
    private void EnsureBarrierCapacity(uint requiredCount) {
        if (requiredCount <= (uint)this._barrierBatch.Length) {
            return;
        }

        uint newSize = (uint)this._barrierBatch.Length;
        while (newSize < requiredCount) {
            newSize *= 2;
        }

        Array.Resize(ref this._barrierBatch, checked((int)newSize));
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

    /// <summary>
    /// Aligns a value upward to a power-of-two alignment.
    /// </summary>
    /// <param name="value">The value to align.</param>
    /// <param name="alignment">The required alignment.</param>
    /// <returns>The aligned value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong AlignUp(ulong value, ulong alignment) {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    /// <summary>
    /// Represents a queued immediate buffer update.
    /// </summary>
    private readonly struct PendingUpdate {

        /// <summary>
        /// Stores the destination buffer.
        /// </summary>
        public readonly D3D12DeviceBuffer Buffer;

        /// <summary>
        /// Stores the upload allocation containing source data.
        /// </summary>
        public readonly D3D12ResourceAllocation Upload;

        /// <summary>
        /// Stores the destination byte offset.
        /// </summary>
        public readonly uint DestinationOffset;

        /// <summary>
        /// Stores the source byte offset inside the upload allocation.
        /// </summary>
        public readonly ulong UploadOffset;

        /// <summary>
        /// Stores the number of bytes to copy.
        /// </summary>
        public readonly uint SizeInBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="PendingUpdate" /> struct.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="upload">The upload allocation containing source data.</param>
        /// <param name="destinationOffset">The destination byte offset.</param>
        /// <param name="uploadOffset">The source byte offset inside the upload allocation.</param>
        /// <param name="sizeInBytes">The number of bytes to copy.</param>
        public PendingUpdate(D3D12DeviceBuffer buffer, D3D12ResourceAllocation upload, uint destinationOffset, ulong uploadOffset, uint sizeInBytes) {
            this.Buffer = buffer;
            this.Upload = upload;
            this.DestinationOffset = destinationOffset;
            this.UploadOffset = uploadOffset;
            this.SizeInBytes = sizeInBytes;
        }
    }

    /// <summary>
    /// Represents the previous state of a buffer touched by queued immediate updates.
    /// </summary>
    private readonly struct PendingBufferState {

        /// <summary>
        /// Stores the buffer.
        /// </summary>
        public readonly D3D12DeviceBuffer Buffer;

        /// <summary>
        /// Stores the state to restore after queued copies.
        /// </summary>
        public readonly ResourceStates PreviousState;

        /// <summary>
        /// Initializes a new instance of the <see cref="PendingBufferState" /> struct.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="previousState">The state to restore after queued copies.</param>
        public PendingBufferState(D3D12DeviceBuffer buffer, ResourceStates previousState) {
            this.Buffer = buffer;
            this.PreviousState = previousState;
        }
    }
}
