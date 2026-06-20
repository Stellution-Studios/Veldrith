using System;
using System.Diagnostics;
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
    /// Initializes a new instance of the <see cref="D3D12BufferUpdatePlanner" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns upload allocations.</param>
    /// <param name="commandList">The command list that receives D3D12 commands.</param>
    /// <param name="perf">The optional performance tracker updated by this planner.</param>
    internal D3D12BufferUpdatePlanner(D3D12GraphicsDevice gd, D3D12CommandList commandList, D3D12CommandListPerfTracker perf) {
        this._gd = gd;
        this._commandList = commandList;
        this._perf = perf;
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
            this.UpdateGpuBuffer(d3D12Buffer, bufferOffsetInBytes, source, sizeInBytes);
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
}
