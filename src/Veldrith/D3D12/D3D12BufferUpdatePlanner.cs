using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

#if !VELDRID_D3D12_PERF
#pragma warning disable CS0162
#endif

namespace Veldrith.D3D12;

/// <summary>
/// Plans D3D12 buffer updates and records the required upload copies and binding refreshes.
/// </summary>
internal sealed class D3D12BufferUpdatePlanner {

    /// <summary>
    /// Stores the upload page size used for command-list-local batched buffer updates.
    /// </summary>
    private const uint BatchedUploadPageSize = 64 * 1024;

    /// <summary>
    /// Stores the largest single update that is queued into the command-list-local upload batch.
    /// </summary>
    private const uint MaxBatchedUpdateSize = 16 * 1024;

    /// <summary>
    /// Stores the command list that receives upload copy and transition commands.
    /// </summary>
    private readonly D3D12CommandList _commandList;

    /// <summary>
    /// Stores the graphics device that owns temporary upload allocations.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Stores optional performance counters updated while buffer updates are recorded.
    /// </summary>
    private readonly D3D12CommandListPerfTracker _perf;

    /// <summary>
    /// Records tiny aligned updates through D3D12 WriteBufferImmediate.
    /// </summary>
    private readonly D3D12ImmediateBufferWriter _immediateWriter;

    /// <summary>
    /// Stores queued GPU-local buffer updates that share the current upload allocation.
    /// </summary>
    private readonly List<PendingUpdate> _pendingUpdates = new(32);

    /// <summary>
    /// Stores unique destination buffers touched by the pending update batch.
    /// </summary>
    private readonly List<PendingBufferState> _pendingBuffers = new(8);

    /// <summary>
    /// Stores the upload allocation that backs the pending update batch.
    /// </summary>
    private D3D12ResourceAllocation _batchedUpload;

    /// <summary>
    /// Stores the next free byte inside the current batched upload allocation.
    /// </summary>
    private ulong _batchedUploadOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12BufferUpdatePlanner" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns upload allocations.</param>
    /// <param name="commandList">The command list that receives D3D12 commands.</param>
    /// <param name="perf">The optional performance tracker updated by this planner.</param>
    internal D3D12BufferUpdatePlanner(D3D12GraphicsDevice gd, D3D12CommandList commandList, D3D12CommandListPerfTracker perf) {
        this._gd = gd;
        this._commandList = commandList;
        this._perf = perf;
        this._immediateWriter = new D3D12ImmediateBufferWriter(commandList, perf);
    }

    /// <summary>
    /// Resets pending upload state at the start of a new command-list recording.
    /// </summary>
    internal void BeginRecording() {
        this.DiscardPendingUploads();
    }

    /// <summary>
    /// Discards queued updates that were not recorded into the native command list.
    /// </summary>
    internal void DiscardPendingUploads() {
        this._immediateWriter.Discard();
        this.ReturnPendingUpload();
        this._pendingUpdates.Clear();
        this._pendingBuffers.Clear();
        this._batchedUploadOffset = 0;
    }

    /// <summary>
    /// Updates a buffer from CPU memory.
    /// </summary>
    /// <param name="buffer">The buffer to update.</param>
    /// <param name="bufferOffsetInBytes">The destination byte offset.</param>
    /// <param name="source">The source data pointer.</param>
    /// <param name="sizeInBytes">The number of bytes to update.</param>
    internal void Update(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        if (sizeInBytes == 0) {
            return;
        }

        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
        ulong previousBindVersion = d3D12Buffer.BindVersion;

        if (d3D12Buffer.CanTransitionState) {
            if (this._immediateWriter.CanWrite(d3D12Buffer, bufferOffsetInBytes, sizeInBytes)) {
                if (this._pendingUpdates.Count > 0) {
                    this.Flush();
                }

                this._immediateWriter.Queue(d3D12Buffer, bufferOffsetInBytes, source, sizeInBytes);
            }
            else if (ShouldBatchGpuBufferUpdate(sizeInBytes)) {
                this.QueueGpuBufferUpdate(d3D12Buffer, bufferOffsetInBytes, source, sizeInBytes);
            }
            else {
                this.Flush();
                this.UpdateGpuBuffer(d3D12Buffer, bufferOffsetInBytes, source, sizeInBytes);
            }
        }
        else {
            d3D12Buffer.Update(null, source, bufferOffsetInBytes, sizeInBytes);
        }

        this.RecordDynamicSnapshotMetrics(d3D12Buffer);
        this.RefreshBindingsIfNeeded(d3D12Buffer, previousBindVersion);

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.UploadRecordMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Flushes queued command-list-local buffer updates before another GPU command is recorded.
    /// </summary>
    internal void Flush() {
        this._immediateWriter.Flush();
        if (this._pendingUpdates.Count == 0) {
            return;
        }

        D3D12ResourceAllocation upload = this._batchedUpload;
        try {
            this._commandList.FlushPendingUavBarrierForInternalUse();
            this.CapturePendingBufferStates();

            for (int i = 0; i < this._pendingBuffers.Count; i++) {
                this._commandList.TransitionBufferForInternalUse(this._pendingBuffers[i].Buffer, ResourceStates.CopyDest);
            }

            this._commandList.FlushPendingBarriersForInternalUse();

            for (int i = 0; i < this._pendingUpdates.Count; i++) {
                PendingUpdate update = this._pendingUpdates[i];
                this._commandList.NativeCommandList.CopyBufferRegion(update.Buffer.NativeBuffer, update.DestinationOffset, upload.Resource, upload.Offset + update.UploadOffset, update.SizeInBytes);
            }

            for (int i = 0; i < this._pendingBuffers.Count; i++) {
                PendingBufferState state = this._pendingBuffers[i];
                this._commandList.TransitionBufferForInternalUse(state.Buffer, state.PreviousState);
            }

            this._commandList.TrackPendingSubmissionUploadBufferForInternalUse(upload);
            this._batchedUpload = null;
        }
        finally {
            if (this._batchedUpload != null) {
                this._gd.ReturnUploadBuffer(this._batchedUpload);
                this._batchedUpload = null;
            }

            this._pendingUpdates.Clear();
            this._pendingBuffers.Clear();
            this._batchedUploadOffset = 0;
        }
    }

    /// <summary>
    /// Records a GPU-local buffer update through an upload allocation and command-list copy.
    /// </summary>
    /// <param name="buffer">The GPU-local buffer to update.</param>
    /// <param name="bufferOffsetInBytes">The destination byte offset.</param>
    /// <param name="source">The source data pointer.</param>
    /// <param name="sizeInBytes">The number of bytes to update.</param>
    private void UpdateGpuBuffer(D3D12DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        this._commandList.FlushPendingUavBarrierForInternalUse();
        D3D12ResourceAllocation upload = buffer.CreateUploadBufferForCommandListUpdate(source, bufferOffsetInBytes, sizeInBytes);
        try {
            this.RecordGpuUploadMetrics(upload, sizeInBytes);
            ResourceStates previousState = buffer.CurrentState;
            this._commandList.TransitionBufferForInternalUse(buffer, ResourceStates.CopyDest);
            this._commandList.FlushPendingBarriersForInternalUse();
            this._commandList.NativeCommandList.CopyBufferRegion(buffer.NativeBuffer, bufferOffsetInBytes, upload.Resource, upload.Offset, sizeInBytes);
            this._commandList.TransitionBufferForInternalUse(buffer, previousState);
            this._commandList.TrackPendingSubmissionUploadBufferForInternalUse(upload);
            upload = null;
        }
        finally {
            if (upload != null) {
                this._gd.ReturnUploadBuffer(upload);
            }
        }
    }

    /// <summary>
    /// Queues a small GPU-local buffer update into the command-list-local upload batch.
    /// </summary>
    /// <param name="buffer">The GPU-local buffer to update.</param>
    /// <param name="bufferOffsetInBytes">The destination byte offset.</param>
    /// <param name="source">The source data pointer.</param>
    /// <param name="sizeInBytes">The number of bytes to copy.</param>
    private unsafe void QueueGpuBufferUpdate(D3D12DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        buffer.ValidateCommandListUpdateRange(bufferOffsetInBytes, sizeInBytes);
        this.EnsureBatchedUploadSpace(sizeInBytes);

        ulong uploadOffset = this._batchedUploadOffset;
        byte* destination = (byte*)this._batchedUpload.MappedPointer.ToPointer() + uploadOffset;
        Buffer.MemoryCopy(source.ToPointer(), destination, sizeInBytes, sizeInBytes);
        this.AppendOrMergePendingUpdate(buffer, bufferOffsetInBytes, uploadOffset, sizeInBytes);
        this._batchedUploadOffset = AlignUp(uploadOffset + sizeInBytes, 4);

        this.RecordBatchedGpuUploadMetrics(sizeInBytes);
    }

    /// <summary>
    /// Appends a pending update or merges it with the previous update when both source and destination ranges are contiguous.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="destinationOffset">The destination byte offset.</param>
    /// <param name="uploadOffset">The source byte offset inside the batched upload allocation.</param>
    /// <param name="sizeInBytes">The number of bytes to copy.</param>
    private void AppendOrMergePendingUpdate(D3D12DeviceBuffer buffer, uint destinationOffset, ulong uploadOffset, uint sizeInBytes) {
        int lastIndex = this._pendingUpdates.Count - 1;
        if (lastIndex >= 0) {
            PendingUpdate last = this._pendingUpdates[lastIndex];
            if (ReferenceEquals(last.Buffer, buffer)
                && last.DestinationOffset + last.SizeInBytes == destinationOffset
                && last.UploadOffset + last.SizeInBytes == uploadOffset) {
                this._pendingUpdates[lastIndex] = new PendingUpdate(buffer, last.DestinationOffset, last.UploadOffset, last.SizeInBytes + sizeInBytes);
                return;
            }
        }

        this._pendingUpdates.Add(new PendingUpdate(buffer, destinationOffset, uploadOffset, sizeInBytes));
    }

    /// <summary>
    /// Ensures that the current upload batch has enough space for a new update.
    /// </summary>
    /// <param name="sizeInBytes">The required update size.</param>
    private void EnsureBatchedUploadSpace(uint sizeInBytes) {
        if (this._batchedUpload != null && this._batchedUploadOffset + sizeInBytes <= this._batchedUpload.Size) {
            return;
        }

        this.Flush();
        this._batchedUpload = this._gd.RentUploadBuffer(BatchedUploadPageSize);
        this._batchedUploadOffset = 0;
        this.RecordBatchedUploadAllocation(this._batchedUpload);
    }

    /// <summary>
    /// Captures the current destination state for every unique buffer touched by the pending batch.
    /// </summary>
    private void CapturePendingBufferStates() {
        this._pendingBuffers.Clear();
        for (int i = 0; i < this._pendingUpdates.Count; i++) {
            D3D12DeviceBuffer buffer = this._pendingUpdates[i].Buffer;
            if (this.HasPendingBuffer(buffer)) {
                continue;
            }

            this._pendingBuffers.Add(new PendingBufferState(buffer, buffer.CurrentState));
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
    /// Records metrics for a GPU-local buffer upload allocation.
    /// </summary>
    /// <param name="upload">The upload allocation used by the copy.</param>
    /// <param name="sizeInBytes">The number of source bytes copied.</param>
    private void RecordGpuUploadMetrics(D3D12ResourceAllocation upload, uint sizeInBytes) {
        if (!D3D12CommandListPerfTracker.Enabled) {
            return;
        }

        this._perf.UploadBytes += sizeInBytes;
        this._perf.UploadCopies++;
        if (upload.IsTransient) {
            this._perf.UploadRingAllocations++;
        }
        else {
            this._perf.UploadDedicatedAllocations++;
        }
    }

    /// <summary>
    /// Records metrics for a queued GPU-local buffer update.
    /// </summary>
    /// <param name="sizeInBytes">The number of source bytes copied.</param>
    private void RecordBatchedGpuUploadMetrics(uint sizeInBytes) {
        if (!D3D12CommandListPerfTracker.Enabled) {
            return;
        }

        this._perf.UploadBytes += sizeInBytes;
        this._perf.UploadCopies++;
    }

    /// <summary>
    /// Records metrics for the upload allocation backing a queued update batch.
    /// </summary>
    /// <param name="upload">The upload allocation used by the batch.</param>
    private void RecordBatchedUploadAllocation(D3D12ResourceAllocation upload) {
        if (!D3D12CommandListPerfTracker.Enabled) {
            return;
        }

        if (upload.IsTransient) {
            this._perf.UploadRingAllocations++;
        }
        else {
            this._perf.UploadDedicatedAllocations++;
        }
    }

    /// <summary>
    /// Records metrics for dynamic snapshot updates.
    /// </summary>
    /// <param name="buffer">The updated buffer.</param>
    private void RecordDynamicSnapshotMetrics(D3D12DeviceBuffer buffer) {
        if (!D3D12CommandListPerfTracker.Enabled) {
            return;
        }

        this._perf.DynamicSnapshotCopyBytes += buffer.LastDynamicSnapshotCopyBytes;
        this._perf.DynamicSnapshotPrefixCopyBytes += buffer.LastDynamicSnapshotPrefixCopyBytes;
        if (buffer.LastDynamicSnapshotRotated) {
            this._perf.DynamicSnapshotRotations++;
        }
    }

    /// <summary>
    /// Refreshes currently bound views and resource sets when a dynamic buffer moved to a new native address.
    /// </summary>
    /// <param name="buffer">The updated buffer.</param>
    /// <param name="previousBindVersion">The bind version observed before the update.</param>
    private void RefreshBindingsIfNeeded(D3D12DeviceBuffer buffer, ulong previousBindVersion) {
        if (buffer.BindVersion == previousBindVersion) {
            return;
        }

        if (CanBeResourceSetBuffer(buffer)) {
            this._commandList.MarkResourceSetsReferencingBufferDirtyForInternalUse(buffer);
        }

        this._commandList.RefreshDynamicBufferBindingsForInternalUse(buffer);
    }

    /// <summary>
    /// Checks whether the buffer usage allows binding through a resource set.
    /// </summary>
    /// <param name="buffer">The buffer to inspect.</param>
    /// <returns><see langword="true" /> when a bound resource set may reference the buffer.</returns>
    private static bool CanBeResourceSetBuffer(D3D12DeviceBuffer buffer) {
        const BufferUsage resourceSetUsages = BufferUsage.UniformBuffer | BufferUsage.StructuredBufferReadOnly | BufferUsage.StructuredBufferReadWrite;
        return (buffer.Usage & resourceSetUsages) != 0;
    }

    /// <summary>
    /// Checks whether a GPU-local update should be staged into the command-list-local upload batch.
    /// </summary>
    /// <param name="sizeInBytes">The update size.</param>
    /// <returns><see langword="true" /> when batching is preferred.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldBatchGpuBufferUpdate(uint sizeInBytes) {
        return sizeInBytes <= MaxBatchedUpdateSize;
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
    /// Returns a pending upload allocation that has not been recorded into the command list.
    /// </summary>
    private void ReturnPendingUpload() {
        if (this._batchedUpload == null) {
            return;
        }

        this._gd.ReturnUploadBuffer(this._batchedUpload);
        this._batchedUpload = null;
    }

    /// <summary>
    /// Describes one queued GPU-local buffer update inside the batched upload allocation.
    /// </summary>
    private readonly struct PendingUpdate {

        /// <summary>
        /// Initializes a new instance of the <see cref="PendingUpdate" /> struct.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="destinationOffset">The destination byte offset.</param>
        /// <param name="uploadOffset">The source byte offset inside the batched upload allocation.</param>
        /// <param name="sizeInBytes">The number of bytes to copy.</param>
        public PendingUpdate(D3D12DeviceBuffer buffer, uint destinationOffset, ulong uploadOffset, uint sizeInBytes) {
            this.Buffer = buffer;
            this.DestinationOffset = destinationOffset;
            this.UploadOffset = uploadOffset;
            this.SizeInBytes = sizeInBytes;
        }

        /// <summary>
        /// Gets the destination buffer.
        /// </summary>
        public D3D12DeviceBuffer Buffer { get; }

        /// <summary>
        /// Gets the destination byte offset.
        /// </summary>
        public uint DestinationOffset { get; }

        /// <summary>
        /// Gets the source byte offset inside the batched upload allocation.
        /// </summary>
        public ulong UploadOffset { get; }

        /// <summary>
        /// Gets the number of bytes to copy.
        /// </summary>
        public uint SizeInBytes { get; }
    }

    /// <summary>
    /// Stores the pre-copy state for a buffer touched by a pending update batch.
    /// </summary>
    private readonly struct PendingBufferState {

        /// <summary>
        /// Initializes a new instance of the <see cref="PendingBufferState" /> struct.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="previousState">The state to restore after the copy batch.</param>
        public PendingBufferState(D3D12DeviceBuffer buffer, ResourceStates previousState) {
            this.Buffer = buffer;
            this.PreviousState = previousState;
        }

        /// <summary>
        /// Gets the destination buffer.
        /// </summary>
        public D3D12DeviceBuffer Buffer { get; }

        /// <summary>
        /// Gets the state to restore after the copy batch.
        /// </summary>
        public ResourceStates PreviousState { get; }
    }
}
