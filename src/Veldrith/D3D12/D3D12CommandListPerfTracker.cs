using System;
using System.Collections.Generic;
using System.Diagnostics;

#if !VELDRID_D3D12_PERF
#pragma warning disable CS0162
#endif

namespace Veldrith.D3D12;

/// <summary>
/// Tracks optional D3D12 command-list recording performance metrics.
/// </summary>
internal sealed class D3D12CommandListPerfTracker {

    /// <summary>
    /// Stores the number of frames included in one console performance report.
    /// </summary>
    private const int ReportIntervalFrames = 240;

    /// <summary>
    /// Stores the command-list recording gap that triggers an immediate spike report.
    /// </summary>
    private const double RecordSpikeThresholdMs = 8.0;

    /// <summary>
    /// Gets whether D3D12 performance logging is enabled.
    /// </summary>
    #if VELDRID_D3D12_PERF
    internal static readonly bool Enabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PERF"), "1", StringComparison.Ordinal);
    #else
    internal const bool Enabled = false;
    #endif

    /// <summary>
    /// Gets whether performance gap spike reports should include managed stack traces.
    /// </summary>
    #if VELDRID_D3D12_PERF
    private static readonly bool StackLogEnabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PERF_STACK"), "1", StringComparison.Ordinal);
    #else
    private const bool StackLogEnabled = false;
    #endif

    /// <summary>
    /// Stores active debug group names for D3D12 performance gap attribution.
    /// </summary>
    private readonly List<string> _debugGroupStack = new();

    /// <summary>
    /// Stores the elapsed timer used for report-window durations.
    /// </summary>
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Stores the accumulated CPU time spent recording resource barriers during the current reporting window.
    /// </summary>
    private double _accumBarrierMs;

    /// <summary>
    /// Stores the accumulated number of begin-time fence waits during the current reporting window.
    /// </summary>
    private ulong _accumBeginWaitCount;

    /// <summary>
    /// Stores managed bytes allocated while recording command lists during the current reporting window.
    /// </summary>
    private ulong _accumAllocatedBytes;

    /// <summary>
    /// Stores accumulated begin-time fence wait duration during the current reporting window.
    /// </summary>
    private double _accumBeginWaitMs;

    /// <summary>
    /// Stores the accumulated CPU time spent recording descriptor copies during the current reporting window.
    /// </summary>
    private double _accumDescriptorCopyMs;

    /// <summary>
    /// Stores descriptor copies accumulated during the current reporting window.
    /// </summary>
    private ulong _accumDescriptorCopies;

    /// <summary>
    /// Stores the accumulated CPU time spent recording dispatch work during the current reporting window.
    /// </summary>
    private double _accumDispatchMs;

    /// <summary>
    /// Stores dispatch calls accumulated during the current reporting window.
    /// </summary>
    private ulong _accumDispatchCalls;

    /// <summary>
    /// Stores dynamic snapshot source-copy bytes accumulated during the current reporting window.
    /// </summary>
    private ulong _accumDynamicSnapshotCopyBytes;

    /// <summary>
    /// Stores dynamic snapshot prefix-copy bytes accumulated during the current reporting window.
    /// </summary>
    private ulong _accumDynamicSnapshotPrefixCopyBytes;

    /// <summary>
    /// Stores dynamic snapshot slot rotations accumulated during the current reporting window.
    /// </summary>
    private ulong _accumDynamicSnapshotRotations;

    /// <summary>
    /// Stores the accumulated CPU time spent recording draw work during the current reporting window.
    /// </summary>
    private double _accumDrawMs;

    /// <summary>
    /// Stores draw calls accumulated during the current reporting window.
    /// </summary>
    private ulong _accumDrawCalls;

    /// <summary>
    /// Stores index-buffer binds accumulated during the current reporting window.
    /// </summary>
    private ulong _accumIndexBufferBinds;

    /// <summary>
    /// Stores the accumulated CPU time spent binding pipeline state during the current reporting window.
    /// </summary>
    private double _accumPipelineSetMs;

    /// <summary>
    /// Stores pipeline changes accumulated during the current reporting window.
    /// </summary>
    private ulong _accumPipelineChanges;

    /// <summary>
    /// Stores the accumulated CPU time spent flushing changed resource sets during the current reporting window.
    /// </summary>
    private double _accumResourceSetFlushMs;

    /// <summary>
    /// Stores resource-set changes accumulated during the current reporting window.
    /// </summary>
    private ulong _accumResourceSetChanges;

    /// <summary>
    /// Stores resource set dirty slots scanned during the current reporting window.
    /// </summary>
    private ulong _accumResourceSetScanSlots;

    /// <summary>
    /// Stores resource sets rebound during the current reporting window.
    /// </summary>
    private ulong _accumResourceSetBinds;

    /// <summary>
    /// Stores root descriptor-table bindings accumulated during the current reporting window.
    /// </summary>
    private ulong _accumRootTableSets;

    /// <summary>
    /// Stores root buffer-view bindings accumulated during the current reporting window.
    /// </summary>
    private ulong _accumRootBufferSets;

    /// <summary>
    /// Stores render-target output-merger bindings accumulated during the current reporting window.
    /// </summary>
    private ulong _accumRenderTargetBinds;

    /// <summary>
    /// Stores redundant render-target output-merger bindings skipped during the current reporting window.
    /// </summary>
    private ulong _accumRenderTargetBindSkips;

    /// <summary>
    /// Stores subresource transitions accumulated during the current reporting window.
    /// </summary>
    private ulong _accumSubresourceTransitions;

    /// <summary>
    /// Stores resource transitions accumulated during the current reporting window.
    /// </summary>
    private ulong _accumTransitions;

    /// <summary>
    /// Stores the accumulated CPU time spent recording upload commands during the current reporting window.
    /// </summary>
    private double _accumUploadRecordMs;

    /// <summary>
    /// Stores UAV barriers accumulated during the current reporting window.
    /// </summary>
    private ulong _accumUavBarriers;

    /// <summary>
    /// Stores vertex-buffer binds accumulated during the current reporting window.
    /// </summary>
    private ulong _accumVertexBufferBinds;

    /// <summary>
    /// Stores the timestamp captured at the beginning of the current command list recording.
    /// </summary>
    private long _frameStartTicks;

    /// <summary>
    /// Stores the managed allocated byte counter captured at the beginning of the current command list recording.
    /// </summary>
    private long _frameStartAllocatedBytes;

    /// <summary>
    /// Stores the Gen0 collection count captured at the beginning of the current command list recording.
    /// </summary>
    private int _gc0Start;

    /// <summary>
    /// Stores the Gen1 collection count captured at the beginning of the current command list recording.
    /// </summary>
    private int _gc1Start;

    /// <summary>
    /// Stores the Gen2 collection count captured at the beginning of the current command list recording.
    /// </summary>
    private int _gc2Start;

    /// <summary>
    /// Stores accumulated Gen0 collections observed while command lists were recording.
    /// </summary>
    private ulong _accumGc0Collections;

    /// <summary>
    /// Stores accumulated Gen1 collections observed while command lists were recording.
    /// </summary>
    private ulong _accumGc1Collections;

    /// <summary>
    /// Stores accumulated Gen2 collections observed while command lists were recording.
    /// </summary>
    private ulong _accumGc2Collections;

    /// <summary>
    /// Stores the number of command lists included in the current report cadence.
    /// </summary>
    private ulong _frames;

    /// <summary>
    /// Stores the elapsed millisecond value at the previous report.
    /// </summary>
    private double _lastReportMs;

    /// <summary>
    /// Stores the command-list API name that preceded the largest external gap in the current command list.
    /// </summary>
    private string _maxExternalGapAfter;

    /// <summary>
    /// Stores the command-list API name that started the largest external gap in the current command list.
    /// </summary>
    private string _maxExternalGapBefore;

    /// <summary>
    /// Stores the largest gap between Veldrith command-list calls in the current command list.
    /// </summary>
    private double _maxExternalGapMs;

    /// <summary>
    /// Stores the debug scope active during the largest external gap in the current command list.
    /// </summary>
    private string _maxExternalGapScope;

    /// <summary>
    /// Stores the stack trace captured at the API entry after the largest external gap in the current command list.
    /// </summary>
    private string _maxExternalGapStack;

    /// <summary>
    /// Stores the command-list API timestamp captured at the previous D3D12 command-list entry point.
    /// </summary>
    private long _lastCommandApiTicks;

    /// <summary>
    /// Stores the previous D3D12 command-list entry point name for gap attribution.
    /// </summary>
    private string _lastCommandApiName;

    /// <summary>
    /// Stores the most recent debug marker name for performance gap attribution.
    /// </summary>
    private string _lastDebugMarker;

    /// <summary>
    /// Stores the largest per-command-list barrier recording time observed in the current report window.
    /// </summary>
    private double _maxBarrierMs;

    /// <summary>
    /// Stores the largest begin wait time observed in the current report window.
    /// </summary>
    private double _maxBeginWaitMs;

    /// <summary>
    /// Stores the largest per-command-list descriptor copy time observed in the current report window.
    /// </summary>
    private double _maxDescriptorCopyMs;

    /// <summary>
    /// Stores the largest per-command-list dispatch recording time observed in the current report window.
    /// </summary>
    private double _maxDispatchMs;

    /// <summary>
    /// Stores the largest per-command-list draw recording time observed in the current report window.
    /// </summary>
    private double _maxDrawMs;

    /// <summary>
    /// Stores the largest per-command-list pipeline binding time observed in the current report window.
    /// </summary>
    private double _maxPipelineSetMs;

    /// <summary>
    /// Stores the largest command-list recording time observed in the current report window.
    /// </summary>
    private double _maxRecordMs;

    /// <summary>
    /// Stores the largest command-list recording time not explained by tracked D3D12 work.
    /// </summary>
    private double _maxUntrackedRecordMs;

    /// <summary>
    /// Stores the largest per-command-list resource set flush time observed in the current report window.
    /// </summary>
    private double _maxResourceSetFlushMs;

    /// <summary>
    /// Stores the largest per-command-list upload recording time observed in the current report window.
    /// </summary>
    private double _maxUploadRecordMs;

    /// <summary>
    /// Stores the largest gap between Veldrith command-list calls observed in the current report window.
    /// </summary>
    private double _reportMaxExternalGapMs;

    /// <summary>
    /// Stores the API transition that produced the largest external gap in the current report window.
    /// </summary>
    private string _reportMaxExternalGapTransition;

    /// <summary>
    /// Stores the debug scope for the largest external gap in the current report window.
    /// </summary>
    private string _reportMaxExternalGapScope;

    /// <summary>
    /// Stores the stack trace captured for the largest external gap in the current report window.
    /// </summary>
    private string _reportMaxExternalGapStack;

    /// <summary>
    /// Tracks whether the current command list is recording API gaps for D3D12 performance logging.
    /// </summary>
    private bool _recordingCommandGaps;

    /// <summary>
    /// Stores managed bytes allocated while recording the current command list.
    /// </summary>
    internal ulong AllocatedBytes;

    /// <summary>
    /// Stores the CPU time spent recording resource barriers for the current command list.
    /// </summary>
    internal double BarrierMs;

    /// <summary>
    /// Stores begin-time fence waits for the current command list.
    /// </summary>
    internal ulong BeginWaitCount;

    /// <summary>
    /// Stores begin-time fence wait duration for the current command list.
    /// </summary>
    internal double BeginWaitMs;

    /// <summary>
    /// Stores the CPU time spent recording descriptor copies for the current command list.
    /// </summary>
    internal double DescriptorCopyMs;

    /// <summary>
    /// Stores descriptor copies for the current command list.
    /// </summary>
    internal ulong DescriptorCopies;

    /// <summary>
    /// Stores the CPU time spent recording dispatch work for the current command list.
    /// </summary>
    internal double DispatchMs;

    /// <summary>
    /// Stores dispatch calls for the current command list.
    /// </summary>
    internal ulong DispatchCalls;

    /// <summary>
    /// Stores the CPU time spent recording draw work for the current command list.
    /// </summary>
    internal double DrawMs;

    /// <summary>
    /// Stores dynamic snapshot source-copy bytes for the current command list.
    /// </summary>
    internal ulong DynamicSnapshotCopyBytes;

    /// <summary>
    /// Stores dynamic snapshot prefix-copy bytes for the current command list.
    /// </summary>
    internal ulong DynamicSnapshotPrefixCopyBytes;

    /// <summary>
    /// Stores dynamic snapshot slot rotations for the current command list.
    /// </summary>
    internal ulong DynamicSnapshotRotations;

    /// <summary>
    /// Stores draw calls for the current command list.
    /// </summary>
    internal ulong DrawCalls;

    /// <summary>
    /// Stores index-buffer binds for the current command list.
    /// </summary>
    internal ulong IndexBufferBinds;

    /// <summary>
    /// Stores the CPU time spent binding pipeline state for the current command list.
    /// </summary>
    internal double PipelineSetMs;

    /// <summary>
    /// Stores pipeline changes for the current command list.
    /// </summary>
    internal ulong PipelineChanges;

    /// <summary>
    /// Stores the CPU time spent flushing changed resource sets for the current command list.
    /// </summary>
    internal double ResourceSetFlushMs;

    /// <summary>
    /// Stores resource set changes for the current command list.
    /// </summary>
    internal ulong ResourceSetChanges;

    /// <summary>
    /// Stores resource set dirty slots scanned for the current command list.
    /// </summary>
    internal ulong ResourceSetScanSlots;

    /// <summary>
    /// Stores resource sets rebound for the current command list.
    /// </summary>
    internal ulong ResourceSetBinds;

    /// <summary>
    /// Stores root descriptor-table bindings for the current command list.
    /// </summary>
    internal ulong RootTableSets;

    /// <summary>
    /// Stores root buffer-view bindings for the current command list.
    /// </summary>
    internal ulong RootBufferSets;

    /// <summary>
    /// Stores render-target output-merger bindings for the current command list.
    /// </summary>
    internal ulong RenderTargetBinds;

    /// <summary>
    /// Stores redundant render-target output-merger bindings skipped for the current command list.
    /// </summary>
    internal ulong RenderTargetBindSkips;

    /// <summary>
    /// Stores subresource transitions for the current command list.
    /// </summary>
    internal ulong SubresourceTransitions;

    /// <summary>
    /// Stores resource transitions for the current command list.
    /// </summary>
    internal ulong Transitions;

    /// <summary>
    /// Stores the CPU time spent recording upload commands for the current command list.
    /// </summary>
    internal double UploadRecordMs;

    /// <summary>
    /// Stores UAV barriers for the current command list.
    /// </summary>
    internal ulong UavBarriers;

    /// <summary>
    /// Stores vertex-buffer binds for the current command list.
    /// </summary>
    internal ulong VertexBufferBinds;

    /// <summary>
    /// Converts high-resolution stopwatch ticks to milliseconds for D3D12 performance logging.
    /// </summary>
    /// <param name="ticks">The elapsed stopwatch ticks.</param>
    /// <returns>The elapsed time in milliseconds.</returns>
    internal static double TicksToMilliseconds(long ticks) {
        return ticks * 1000.0 / Stopwatch.Frequency;
    }

    /// <summary>
    /// Begins recording metrics for a command-list recording.
    /// </summary>
    internal void BeginRecording() {
        if (!Enabled) {
            return;
        }

        this._frameStartTicks = Stopwatch.GetTimestamp();
        this._frameStartAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
        this._gc0Start = GC.CollectionCount(0);
        this._gc1Start = GC.CollectionCount(1);
        this._gc2Start = GC.CollectionCount(2);
        this.AllocatedBytes = 0;
        this.BarrierMs = 0;
        this.BeginWaitCount = 0;
        this.BeginWaitMs = 0;
        this.DescriptorCopyMs = 0;
        this.DispatchMs = 0;
        this.DrawMs = 0;
        this.PipelineSetMs = 0;
        this.ResourceSetFlushMs = 0;
        this.UploadRecordMs = 0;
        this.Transitions = 0;
        this.SubresourceTransitions = 0;
        this.UavBarriers = 0;
        this.PipelineChanges = 0;
        this.ResourceSetChanges = 0;
        this.ResourceSetScanSlots = 0;
        this.ResourceSetBinds = 0;
        this.DescriptorCopies = 0;
        this.RootTableSets = 0;
        this.RootBufferSets = 0;
        this.RenderTargetBinds = 0;
        this.RenderTargetBindSkips = 0;
        this.VertexBufferBinds = 0;
        this.IndexBufferBinds = 0;
        this.DynamicSnapshotCopyBytes = 0;
        this.DynamicSnapshotPrefixCopyBytes = 0;
        this.DynamicSnapshotRotations = 0;
        this.DrawCalls = 0;
        this.DispatchCalls = 0;
        this.BeginCommandGapTracking();
    }

    /// <summary>
    /// Ends recording metrics and emits reports when thresholds or report intervals are reached.
    /// </summary>
    internal void EndRecording() {
        if (!Enabled) {
            return;
        }

        double recordMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - this._frameStartTicks);
        long allocatedDelta = GC.GetAllocatedBytesForCurrentThread() - this._frameStartAllocatedBytes;
        this.AllocatedBytes = (ulong)Math.Max(allocatedDelta, 0);
        int gc0Delta = GC.CollectionCount(0) - this._gc0Start;
        int gc1Delta = GC.CollectionCount(1) - this._gc1Start;
        int gc2Delta = GC.CollectionCount(2) - this._gc2Start;
        double trackedMs = this.BeginWaitMs
                           + this.PipelineSetMs
                           + this.ResourceSetFlushMs
                           + this.BarrierMs
                           + this.DescriptorCopyMs
                           + this.UploadRecordMs
                           + this.DrawMs
                           + this.DispatchMs;
        double untrackedMs = Math.Max(0, recordMs - trackedMs);
        this._frames++;
        this._maxRecordMs = Math.Max(this._maxRecordMs, recordMs);
        this._maxUntrackedRecordMs = Math.Max(this._maxUntrackedRecordMs, untrackedMs);
        this._accumGc0Collections += (ulong)Math.Max(gc0Delta, 0);
        this._accumGc1Collections += (ulong)Math.Max(gc1Delta, 0);
        this._accumGc2Collections += (ulong)Math.Max(gc2Delta, 0);
        this._accumAllocatedBytes += this.AllocatedBytes;
        this._maxBeginWaitMs = Math.Max(this._maxBeginWaitMs, this.BeginWaitMs);
        this._maxPipelineSetMs = Math.Max(this._maxPipelineSetMs, this.PipelineSetMs);
        this._maxResourceSetFlushMs = Math.Max(this._maxResourceSetFlushMs, this.ResourceSetFlushMs);
        this._maxBarrierMs = Math.Max(this._maxBarrierMs, this.BarrierMs);
        this._maxDescriptorCopyMs = Math.Max(this._maxDescriptorCopyMs, this.DescriptorCopyMs);
        this._maxUploadRecordMs = Math.Max(this._maxUploadRecordMs, this.UploadRecordMs);
        this._maxDrawMs = Math.Max(this._maxDrawMs, this.DrawMs);
        this._maxDispatchMs = Math.Max(this._maxDispatchMs, this.DispatchMs);
        this._accumBarrierMs += this.BarrierMs;
        this._accumBeginWaitCount += this.BeginWaitCount;
        this._accumBeginWaitMs += this.BeginWaitMs;
        this._accumDescriptorCopyMs += this.DescriptorCopyMs;
        this._accumDispatchMs += this.DispatchMs;
        this._accumDrawMs += this.DrawMs;
        this._accumPipelineSetMs += this.PipelineSetMs;
        this._accumResourceSetFlushMs += this.ResourceSetFlushMs;
        this._accumUploadRecordMs += this.UploadRecordMs;
        this._accumTransitions += this.Transitions;
        this._accumSubresourceTransitions += this.SubresourceTransitions;
        this._accumUavBarriers += this.UavBarriers;
        this._accumPipelineChanges += this.PipelineChanges;
        this._accumResourceSetChanges += this.ResourceSetChanges;
        this._accumResourceSetScanSlots += this.ResourceSetScanSlots;
        this._accumResourceSetBinds += this.ResourceSetBinds;
        this._accumDescriptorCopies += this.DescriptorCopies;
        this._accumRootTableSets += this.RootTableSets;
        this._accumRootBufferSets += this.RootBufferSets;
        this._accumRenderTargetBinds += this.RenderTargetBinds;
        this._accumRenderTargetBindSkips += this.RenderTargetBindSkips;
        this._accumVertexBufferBinds += this.VertexBufferBinds;
        this._accumIndexBufferBinds += this.IndexBufferBinds;
        this._accumDynamicSnapshotCopyBytes += this.DynamicSnapshotCopyBytes;
        this._accumDynamicSnapshotPrefixCopyBytes += this.DynamicSnapshotPrefixCopyBytes;
        this._accumDynamicSnapshotRotations += this.DynamicSnapshotRotations;
        this._accumDrawCalls += this.DrawCalls;
        this._accumDispatchCalls += this.DispatchCalls;
        this.AccumulateCommandGapReport();

        if (untrackedMs >= RecordSpikeThresholdMs) {
            Console.WriteLine($"[D3D12 PERF SPIKE] recordMs={recordMs:F3}, trackedMs={trackedMs:F3}, untrackedMs={untrackedMs:F3}, " + $"wait={this.BeginWaitMs:F3}, pso={this.PipelineSetMs:F3}, rs={this.ResourceSetFlushMs:F3}, barrier={this.BarrierMs:F3}, " + $"upload={this.UploadRecordMs:F3}, draw={this.DrawMs:F3}, dispatch={this.DispatchMs:F3}, " + $"allocKB={this.AllocatedBytes / 1024.0:F1}, gc={Math.Max(gc0Delta, 0)}/{Math.Max(gc1Delta, 0)}/{Math.Max(gc2Delta, 0)}, psoCount={this.PipelineChanges}, rsCount={this.ResourceSetChanges}, drawCount={this.DrawCalls}");
            Console.WriteLine($"[D3D12 PERF UPLOAD] dynCopyKB={this.DynamicSnapshotCopyBytes / 1024.0:F1}, dynPrefixKB={this.DynamicSnapshotPrefixCopyBytes / 1024.0:F1}, dynRot={this.DynamicSnapshotRotations}, vb={this.VertexBufferBinds}, ib={this.IndexBufferBinds}, rtBind={this.RenderTargetBinds}, rtSkip={this.RenderTargetBindSkips}");
            Console.WriteLine($"[D3D12 PERF GAP] maxGapMs={this._maxExternalGapMs:F3}, transition={this._maxExternalGapBefore}->{this._maxExternalGapAfter}, scope={this._maxExternalGapScope}");
            if (!string.IsNullOrEmpty(this._maxExternalGapStack)) {
                Console.WriteLine($"[D3D12 PERF GAP STACK]\n{this._maxExternalGapStack}");
            }
        }

        if (this._frames % ReportIntervalFrames == 0) {
            this.WriteReport();
            this.ResetReportWindow();
        }

        this._recordingCommandGaps = false;
    }

    /// <summary>
    /// Records a pushed debug group for performance gap attribution.
    /// </summary>
    /// <param name="name">The debug group name.</param>
    internal void PushDebugGroup(string name) {
        if (Enabled && this._recordingCommandGaps) {
            this._debugGroupStack.Add(name);
            this._lastDebugMarker = name;
        }
    }

    /// <summary>
    /// Records a popped debug group for performance gap attribution.
    /// </summary>
    internal void PopDebugGroup() {
        if (Enabled && this._recordingCommandGaps && this._debugGroupStack.Count > 0) {
            this._debugGroupStack.RemoveAt(this._debugGroupStack.Count - 1);
        }
    }

    /// <summary>
    /// Records a debug marker for performance gap attribution.
    /// </summary>
    /// <param name="name">The debug marker name.</param>
    internal void InsertDebugMarker(string name) {
        if (Enabled && this._recordingCommandGaps) {
            this._lastDebugMarker = name;
        }
    }

    /// <summary>
    /// Records the elapsed wall-clock gap since the previous D3D12 command-list entry point.
    /// </summary>
    /// <param name="apiName">The current command-list API name.</param>
    /// <returns>A scope that completes gap tracking when disposed.</returns>
    internal CommandApiScope TrackCommandApi(string apiName) {
        if (!Enabled || !this._recordingCommandGaps) {
            return default;
        }

        long now = Stopwatch.GetTimestamp();
        if (this._lastCommandApiTicks != 0) {
            double gapMs = TicksToMilliseconds(now - this._lastCommandApiTicks);
            if (gapMs > this._maxExternalGapMs) {
                this._maxExternalGapMs = gapMs;
                this._maxExternalGapBefore = this._lastCommandApiName;
                this._maxExternalGapAfter = apiName;
                this._maxExternalGapScope = this.GetDebugScope();
                this._maxExternalGapStack = StackLogEnabled && gapMs >= RecordSpikeThresholdMs
                    ? new StackTrace(1, true).ToString()
                    : null;
            }
        }

        return new CommandApiScope(this, apiName);
    }

    /// <summary>
    /// Marks the current command-list API call as completed for exit-to-entry gap attribution.
    /// </summary>
    /// <param name="apiName">The command-list API name.</param>
    private void CompleteCommandApi(string apiName) {
        if (!Enabled || !this._recordingCommandGaps) {
            return;
        }

        this._lastCommandApiTicks = Stopwatch.GetTimestamp();
        this._lastCommandApiName = apiName;
    }

    /// <summary>
    /// Starts per-command-list API gap tracking for D3D12 performance logging.
    /// </summary>
    private void BeginCommandGapTracking() {
        this._debugGroupStack.Clear();
        this._lastDebugMarker = null;
        this._maxExternalGapMs = 0;
        this._maxExternalGapBefore = null;
        this._maxExternalGapAfter = null;
        this._maxExternalGapScope = null;
        this._maxExternalGapStack = null;
        this._lastCommandApiTicks = Stopwatch.GetTimestamp();
        this._lastCommandApiName = "Begin";
        this._recordingCommandGaps = true;
    }

    /// <summary>
    /// Updates report-window max gap state from the current command list.
    /// </summary>
    private void AccumulateCommandGapReport() {
        if (this._maxExternalGapMs <= this._reportMaxExternalGapMs) {
            return;
        }

        this._reportMaxExternalGapMs = this._maxExternalGapMs;
        this._reportMaxExternalGapTransition = $"{this._maxExternalGapBefore}->{this._maxExternalGapAfter}";
        this._reportMaxExternalGapScope = this._maxExternalGapScope;
        this._reportMaxExternalGapStack = this._maxExternalGapStack;
    }

    /// <summary>
    /// Gets the active debug scope for D3D12 performance gap attribution.
    /// </summary>
    /// <returns>The active scope name.</returns>
    private string GetDebugScope() {
        if (this._debugGroupStack.Count > 0) {
            return this._debugGroupStack[this._debugGroupStack.Count - 1];
        }

        return string.IsNullOrEmpty(this._lastDebugMarker) ? "<none>" : this._lastDebugMarker;
    }

    /// <summary>
    /// Emits the current report-window summary to the console.
    /// </summary>
    private void WriteReport() {
        double elapsedMs = this._stopwatch.Elapsed.TotalMilliseconds;
        double reportWindowMs = elapsedMs - this._lastReportMs;
        this._lastReportMs = elapsedMs;
        double invFrames = 1.0 / ReportIntervalFrames;
        Console.WriteLine($"[D3D12 PERF] {ReportIntervalFrames}f/{reportWindowMs:F0}ms avg: " + $"wait={this._accumBeginWaitMs * invFrames:F3}ms ({this._accumBeginWaitCount * invFrames:F2}x), " + $"psoMs={this._accumPipelineSetMs * invFrames:F3}, rsMs={this._accumResourceSetFlushMs * invFrames:F3}, " + $"barrierMs={this._accumBarrierMs * invFrames:F3}, descCopyMs={this._accumDescriptorCopyMs * invFrames:F3}, uploadMs={this._accumUploadRecordMs * invFrames:F3}, " + $"drawMs={this._accumDrawMs * invFrames:F3}, dispatchMs={this._accumDispatchMs * invFrames:F3}, " + $"maxRecordMs={this._maxRecordMs:F3}, maxUntrackedMs={this._maxUntrackedRecordMs:F3}, maxWaitMs={this._maxBeginWaitMs:F3}, maxPsoMs={this._maxPipelineSetMs:F3}, maxRsMs={this._maxResourceSetFlushMs:F3}, " + $"maxBarrierMs={this._maxBarrierMs:F3}, maxUploadMs={this._maxUploadRecordMs:F3}, maxDrawMs={this._maxDrawMs:F3}, " + $"allocKB={this._accumAllocatedBytes * invFrames / 1024.0:F1}, gc={this._accumGc0Collections}/{this._accumGc1Collections}/{this._accumGc2Collections}, " + $"trans={this._accumTransitions * invFrames:F1}, subTrans={this._accumSubresourceTransitions * invFrames:F1}, uavB={this._accumUavBarriers * invFrames:F1}, " + $"pso={this._accumPipelineChanges * invFrames:F1}, rs={this._accumResourceSetChanges * invFrames:F1}, rsScan={this._accumResourceSetScanSlots * invFrames:F1}, rsBind={this._accumResourceSetBinds * invFrames:F1}, " + $"descCopy={this._accumDescriptorCopies * invFrames:F1}, rootTbl={this._accumRootTableSets * invFrames:F1}, rootBuf={this._accumRootBufferSets * invFrames:F1}, rtBind={this._accumRenderTargetBinds * invFrames:F1}, rtSkip={this._accumRenderTargetBindSkips * invFrames:F1}, " + $"vb={this._accumVertexBufferBinds * invFrames:F1}, ib={this._accumIndexBufferBinds * invFrames:F1}, " + $"dynCopyKB={this._accumDynamicSnapshotCopyBytes * invFrames / 1024.0:F1}, dynPrefixKB={this._accumDynamicSnapshotPrefixCopyBytes * invFrames / 1024.0:F1}, dynRot={this._accumDynamicSnapshotRotations * invFrames:F1}, " + $"draw={this._accumDrawCalls * invFrames:F1}, dispatch={this._accumDispatchCalls * invFrames:F1}");
        Console.WriteLine($"[D3D12 PERF GAP] windowMaxGapMs={this._reportMaxExternalGapMs:F3}, transition={this._reportMaxExternalGapTransition}, scope={this._reportMaxExternalGapScope}");
    }

    /// <summary>
    /// Clears accumulated report-window values after a report is emitted.
    /// </summary>
    private void ResetReportWindow() {
        this._accumBarrierMs = 0;
        this._accumBeginWaitCount = 0;
        this._accumBeginWaitMs = 0;
        this._accumDescriptorCopyMs = 0;
        this._accumDispatchMs = 0;
        this._accumDrawMs = 0;
        this._accumPipelineSetMs = 0;
        this._accumResourceSetFlushMs = 0;
        this._accumUploadRecordMs = 0;
        this._accumTransitions = 0;
        this._accumSubresourceTransitions = 0;
        this._accumUavBarriers = 0;
        this._accumPipelineChanges = 0;
        this._accumResourceSetChanges = 0;
        this._accumResourceSetScanSlots = 0;
        this._accumResourceSetBinds = 0;
        this._accumDescriptorCopies = 0;
        this._accumRootTableSets = 0;
        this._accumRootBufferSets = 0;
        this._accumRenderTargetBinds = 0;
        this._accumRenderTargetBindSkips = 0;
        this._accumVertexBufferBinds = 0;
        this._accumIndexBufferBinds = 0;
        this._accumDynamicSnapshotCopyBytes = 0;
        this._accumDynamicSnapshotPrefixCopyBytes = 0;
        this._accumDynamicSnapshotRotations = 0;
        this._accumDrawCalls = 0;
        this._accumDispatchCalls = 0;
        this._accumGc0Collections = 0;
        this._accumGc1Collections = 0;
        this._accumGc2Collections = 0;
        this._accumAllocatedBytes = 0;
        this._maxBarrierMs = 0;
        this._maxBeginWaitMs = 0;
        this._maxDescriptorCopyMs = 0;
        this._maxDispatchMs = 0;
        this._maxDrawMs = 0;
        this._maxPipelineSetMs = 0;
        this._maxRecordMs = 0;
        this._maxUntrackedRecordMs = 0;
        this._maxResourceSetFlushMs = 0;
        this._maxUploadRecordMs = 0;
        this._reportMaxExternalGapMs = 0;
        this._reportMaxExternalGapTransition = null;
        this._reportMaxExternalGapScope = null;
        this._reportMaxExternalGapStack = null;
    }

    /// <summary>
    /// Completes command-list API gap tracking when an instrumented API returns.
    /// </summary>
    internal readonly struct CommandApiScope : IDisposable {

        /// <summary>
        /// Stores the owning performance tracker.
        /// </summary>
        private readonly D3D12CommandListPerfTracker _tracker;

        /// <summary>
        /// Stores the command-list API name.
        /// </summary>
        private readonly string _apiName;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandApiScope" /> struct.
        /// </summary>
        /// <param name="tracker">The owning performance tracker.</param>
        /// <param name="apiName">The command-list API name.</param>
        internal CommandApiScope(D3D12CommandListPerfTracker tracker, string apiName) {
            this._tracker = tracker;
            this._apiName = apiName;
        }

        /// <summary>
        /// Completes command-list API gap tracking.
        /// </summary>
        public void Dispose() {
            this._tracker?.CompleteCommandApi(this._apiName);
        }
    }
}
