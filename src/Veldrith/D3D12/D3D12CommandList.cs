using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Vortice;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12CommandList.
/// </summary>
internal sealed class D3D12CommandList : CommandList {

    /// <summary>
    /// Stores the frames in flight state used by this instance.
    /// </summary>
    private const int FramesInFlight = 3;

    /// <summary>
    /// Stores the perf report interval frames state used by this instance.
    /// </summary>
    private const int PerfReportIntervalFrames = 240;

    /// <summary>
    /// Stores the command-list recording gap that triggers an immediate spike report.
    /// </summary>
    private const double PerfRecordSpikeThresholdMs = 8.0;

    /// <summary>
    /// Executes the register logic for this backend.
    /// </summary>
    private const string _mipmapComputeShaderCode = @"Texture2D<float4> SourceTexture : register(t0);

                                                      RWTexture2D<float4> DestinationTexture : register(u0);

                                                      SamplerState LinearSampler : register(s0);
                                                      
                                                      [numthreads(8, 8, 1)]
                                                      void cs_main(uint3 dispatchThreadID : SV_DispatchThreadID) {
                                                          uint width;
                                                          uint height;
                                                          DestinationTexture.GetDimensions(width, height);
                                                          
                                                          if (dispatchThreadID.x >= width || dispatchThreadID.y >= height) {
                                                              return;
                                                          }
                                                          
                                                          float2 uv = (float2(dispatchThreadID.xy) + 0.5f) / float2(width, height);
                                                          float4 value = SourceTexture.SampleLevel(LinearSampler, uv, 0.0f);
                                                          DestinationTexture[dispatchThreadID.xy] = value;
                                                      }";

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    private static readonly bool _perfLogEnabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PERF"), "1", StringComparison.Ordinal);

    /// <summary>
    /// Enables stack traces for D3D12 performance gap spikes.
    /// </summary>
    private static readonly bool _perfStackLogEnabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PERF_STACK"), "1", StringComparison.Ordinal);

    /// <summary>
    /// Stores the active scissor rects state used by this instance.
    /// </summary>
    private readonly RawRect[] _activeScissorRects = new RawRect[16];

    /// <summary>
    /// Stores the active viewports state used by this instance.
    /// </summary>
    private readonly Vortice.Mathematics.Viewport[] _activeViewports = new Vortice.Mathematics.Viewport[16];

    /// <summary>
    /// Stores the begin event method state used by this instance.
    /// </summary>
    private readonly MethodInfo _beginEventMethod;

    /// <summary>
    /// Stores the bound descriptor heaps state used by this instance.
    /// </summary>
    private readonly ID3D12DescriptorHeap[] _boundDescriptorHeaps = new ID3D12DescriptorHeap[2];

    /// <summary>
    /// Stores the bound vertex buffer offsets value used during command execution.
    /// </summary>
    private readonly uint[] _boundVertexBufferOffsets = new uint[16];

    /// <summary>
    /// Stores the vertex buffer strides currently recorded in D3D12 input-assembler state.
    /// </summary>
    private readonly uint[] _boundVertexBufferStrides = new uint[16];

    /// <summary>
    /// Stores the bound vertex buffers collection used by this instance.
    /// </summary>
    private readonly D3D12DeviceBuffer[] _boundVertexBuffers = new D3D12DeviceBuffer[16];

    /// <summary>
    /// Stores the bound vertex buffer versions state used by this instance.
    /// </summary>
    private readonly ulong[] _boundVertexBufferVersions = new ulong[16];

    /// <summary>
    /// Stores the command allocators state used by this instance.
    /// </summary>
    private readonly ID3D12CommandAllocator[] _commandAllocators = new ID3D12CommandAllocator[FramesInFlight];

    /// <summary>
    /// Stores the compute resource set binding plans collection used by this instance.
    /// </summary>
    private readonly Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlanEntry[]> _computeResourceSetBindingPlans = new(ResourceSetBindingPlanKeyComparer.Instance);

    /// <summary>
    /// Caches shader-visible descriptor table handles for resources already copied into the persistent heap.
    /// </summary>
    private readonly Dictionary<DescriptorCacheKey, GpuDescriptorHandle> _descriptorTableCache = new(DescriptorCacheKeyComparer.Instance);

    /// <summary>
    /// Stores the end event method state used by this instance.
    /// </summary>
    private readonly MethodInfo _endEventMethod;

    /// <summary>
    /// Stores the frame slot fence values state used by this instance.
    /// </summary>
    private readonly ulong[] _frameSlotFenceValues = new ulong[FramesInFlight];

    /// <summary>
    /// Stores the graphics resource set binding plans collection used by this instance.
    /// </summary>
    private readonly Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlanEntry[]> _graphicsResourceSetBindingPlans = new(ResourceSetBindingPlanKeyComparer.Instance);

    /// <summary>
    /// Stores active debug group names for D3D12 performance gap attribution.
    /// </summary>
    private readonly List<string> _perfDebugGroupStack = new();

    /// <summary>
    /// Stores the number of sampler descriptors retained by the persistent shader-visible heap.
    /// </summary>
    private readonly uint _maxSamplerDescriptors = 1024;

    /// <summary>
    /// Stores the number of SRV/UAV descriptors retained by the persistent shader-visible heap.
    /// </summary>
    private readonly uint _maxSrvUavDescriptors = 32768;

    /// <summary>
    /// Executes the start new logic for this backend.
    /// </summary>
    private readonly Stopwatch _perfStopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Stores the sampler descriptor size value used during command execution.
    /// </summary>
    private readonly int _samplerDescriptorSize;

    /// <summary>
    /// Stores the set marker method state used by this instance.
    /// </summary>
    private readonly MethodInfo _setMarkerMethod;

    /// <summary>
    /// Stores the persistent shader-visible sampler descriptor heap used by this command list.
    /// </summary>
    private readonly ID3D12DescriptorHeap _shaderVisibleSamplerHeap;

    /// <summary>
    /// Stores the persistent shader-visible SRV/UAV descriptor heap used by this command list.
    /// </summary>
    private readonly ID3D12DescriptorHeap _shaderVisibleSrvUavHeap;

    /// <summary>
    /// Stores the single barrier state used by this instance.
    /// </summary>
    private readonly ResourceBarrier[] _singleBarrier = new ResourceBarrier[1];

    /// <summary>
    /// Stores the srv uav descriptor size value used during command execution.
    /// </summary>
    private readonly int _srvUavDescriptorSize;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Stores upload resources recorded on this command list until submission assigns a fence value.
    /// </summary>
    private readonly List<D3D12ResourceAllocation> _pendingSubmissionUploadBuffers = new();

    /// <summary>
    /// Stores the active scissor rect count value used during command execution.
    /// </summary>
    private uint _activeScissorRectCount;

    /// <summary>
    /// Stores the active viewport count value used during command execution.
    /// </summary>
    private uint _activeViewportCount;

    /// <summary>
    /// Stores the begun state used by this instance.
    /// </summary>
    private bool _begun;

    /// <summary>
    /// Stores the bound compute resource sets collection used by this instance.
    /// </summary>
    private BoundResourceSetInfo[] _boundComputeResourceSets = Array.Empty<BoundResourceSetInfo>();

    /// <summary>
    /// Tracks compute resource sets that must be rebound before dispatch.
    /// </summary>
    private bool[] _computeResourceSetsChanged = Array.Empty<bool>();

    /// <summary>
    /// Tracks whether any compute resource set is dirty.
    /// </summary>
    private bool _computeResourceSetsDirty;

    /// <summary>
    /// Stores the bound graphics resource sets collection used by this instance.
    /// </summary>
    private BoundResourceSetInfo[] _boundGraphicsResourceSets = Array.Empty<BoundResourceSetInfo>();

    /// <summary>
    /// Tracks graphics resource sets that must be rebound before draw.
    /// </summary>
    private bool[] _graphicsResourceSetsChanged = Array.Empty<bool>();

    /// <summary>
    /// Tracks whether any graphics resource set is dirty.
    /// </summary>
    private bool _graphicsResourceSetsDirty;

    /// <summary>
    /// Stores the bound index buffer value used during command execution.
    /// </summary>
    private D3D12DeviceBuffer _boundIndexBuffer;

    /// <summary>
    /// Stores the bound index buffer offset value used during command execution.
    /// </summary>
    private uint _boundIndexBufferOffset;

    /// <summary>
    /// Stores the bound index buffer version value used during command execution.
    /// </summary>
    private ulong _boundIndexBufferVersion;

    /// <summary>
    /// Stores the bound index format value used during command execution.
    /// </summary>
    private IndexFormat _boundIndexFormat;

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private ulong[] _computeRootBufferAddresses = Array.Empty<ulong>();

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private bool[] _computeRootBufferAddressValid = Array.Empty<bool>();

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private ulong[] _computeRootTablePointers = Array.Empty<ulong>();

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private bool[] _computeRootTablePointerValid = Array.Empty<bool>();

    /// <summary>
    /// Stores the current compute pipeline state used by this instance.
    /// </summary>
    private D3D12Pipeline _currentComputePipeline;

    /// <summary>
    /// Stores the current frame slot state used by this instance.
    /// </summary>
    private int _currentFrameSlot = -1;

    /// <summary>
    /// Stores the current graphics pipeline state used by this instance.
    /// </summary>
    private D3D12Pipeline _currentGraphicsPipeline;

    /// <summary>
    /// Stores the stencil reference currently recorded in output-merger state.
    /// </summary>
    private uint _currentStencilReference;

    /// <summary>
    /// Tracks whether the cached stencil reference matches command-list state.
    /// </summary>
    private bool _currentStencilReferenceValid;

    /// <summary>
    /// Stores the primitive topology currently recorded in input-assembler state.
    /// </summary>
    private Vortice.Direct3D.PrimitiveTopology _currentPrimitiveTopology;

    /// <summary>
    /// Tracks whether the cached primitive topology matches command-list state.
    /// </summary>
    private bool _currentPrimitiveTopologyValid;

    /// <summary>
    /// Stores the descriptor heaps bound state used by this instance.
    /// </summary>
    private bool _descriptorHeapsBound;

    /// <summary>
    /// Stores the dispatch indirect signature state used by this instance.
    /// </summary>
    private ID3D12CommandSignature _dispatchIndirectSignature;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the draw indexed indirect signature value used during command execution.
    /// </summary>
    private ID3D12CommandSignature _drawIndexedIndirectSignature;

    /// <summary>
    /// Stores the draw indirect signature state used by this instance.
    /// </summary>
    private ID3D12CommandSignature _drawIndirectSignature;

    /// <summary>
    /// Stores the ended state used by this instance.
    /// </summary>
    private bool _ended;

    /// <summary>
    /// Stores the gpu mip pipeline state used by this instance.
    /// </summary>
    private D3D12Pipeline _gpuMipPipeline;

    /// <summary>
    /// Stores the gpu mip resource layout state used by this instance.
    /// </summary>
    private ResourceLayout _gpuMipResourceLayout;

    /// <summary>
    /// Stores the gpu mip resources available collection used by this instance.
    /// </summary>
    private bool _gpuMipResourcesAvailable;

    /// <summary>
    /// Stores the gpu mip resources initialized collection used by this instance.
    /// </summary>
    private bool _gpuMipResourcesInitialized;

    /// <summary>
    /// Stores the gpu mip sampler state used by this instance.
    /// </summary>
    private Sampler _gpuMipSampler;

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private ulong[] _graphicsRootBufferAddresses = Array.Empty<ulong>();

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private bool[] _graphicsRootBufferAddressValid = Array.Empty<bool>();

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private ulong[] _graphicsRootTablePointers = Array.Empty<ulong>();

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private bool[] _graphicsRootTablePointerValid = Array.Empty<bool>();

    /// <summary>
    /// Tracks whether has bound index buffer is currently enabled.
    /// </summary>
    private bool _hasBoundIndexBuffer;

    /// <summary>
    /// Stores the indirect signatures available state used by this instance.
    /// </summary>
    private bool _indirectSignaturesAvailable;

    /// <summary>
    /// Stores the indirect signatures initialized state used by this instance.
    /// </summary>
    private bool _indirectSignaturesInitialized;

    /// <summary>
    /// Stores the max bound vertex buffer slot collection used by this instance.
    /// </summary>
    private uint _maxBoundVertexBufferSlot;

    /// <summary>
    /// Stores the next sampler descriptor state used by this instance.
    /// </summary>
    private uint _nextSamplerDescriptor;

    /// <summary>
    /// Stores the next srv uav descriptor state used by this instance.
    /// </summary>
    private uint _nextSrvUavDescriptor;

    /// <summary>
    /// Stores the accumulated CPU time spent recording resource barriers during the current reporting window.
    /// </summary>
    private double _perfAccumBarrierMs;

    /// <summary>
    /// Stores the perf accum begin wait count value used during command execution.
    /// </summary>
    private ulong _perfAccumBeginWaitCount;

    /// <summary>
    /// Stores the perf accum begin wait ms state used by this instance.
    /// </summary>
    private double _perfAccumBeginWaitMs;

    /// <summary>
    /// Stores the accumulated CPU time spent recording descriptor copies during the current reporting window.
    /// </summary>
    private double _perfAccumDescriptorCopyMs;

    /// <summary>
    /// Stores the perf accum descriptor copies state used by this instance.
    /// </summary>
    private ulong _perfAccumDescriptorCopies;

    /// <summary>
    /// Stores the accumulated CPU time spent recording dispatch work during the current reporting window.
    /// </summary>
    private double _perfAccumDispatchMs;

    /// <summary>
    /// Stores the perf accum dispatch calls state used by this instance.
    /// </summary>
    private ulong _perfAccumDispatchCalls;

    /// <summary>
    /// Stores the accumulated CPU time spent recording draw work during the current reporting window.
    /// </summary>
    private double _perfAccumDrawMs;

    /// <summary>
    /// Stores the perf accum draw calls state used by this instance.
    /// </summary>
    private ulong _perfAccumDrawCalls;

    /// <summary>
    /// Stores the perf accum index buffer binds value used during command execution.
    /// </summary>
    private ulong _perfAccumIndexBufferBinds;

    /// <summary>
    /// Stores the accumulated CPU time spent binding pipeline state during the current reporting window.
    /// </summary>
    private double _perfAccumPipelineSetMs;

    /// <summary>
    /// Stores the perf accum pipeline changes state used by this instance.
    /// </summary>
    private ulong _perfAccumPipelineChanges;

    /// <summary>
    /// Stores the accumulated CPU time spent flushing changed resource sets during the current reporting window.
    /// </summary>
    private double _perfAccumResourceSetFlushMs;

    /// <summary>
    /// Stores the perf accum resource set changes collection used by this instance.
    /// </summary>
    private ulong _perfAccumResourceSetChanges;

    /// <summary>
    /// Stores the perf accum root table sets state used by this instance.
    /// </summary>
    private ulong _perfAccumRootTableSets;

    /// <summary>
    /// Stores the perf accum subresource transitions state used by this instance.
    /// </summary>
    private ulong _perfAccumSubresourceTransitions;

    /// <summary>
    /// Stores the perf accum transitions state used by this instance.
    /// </summary>
    private ulong _perfAccumTransitions;

    /// <summary>
    /// Stores the accumulated CPU time spent recording upload commands during the current reporting window.
    /// </summary>
    private double _perfAccumUploadRecordMs;

    /// <summary>
    /// Stores the perf accum uav barriers state used by this instance.
    /// </summary>
    private ulong _perfAccumUavBarriers;

    /// <summary>
    /// Stores the perf accum vertex buffer binds state used by this instance.
    /// </summary>
    private ulong _perfAccumVertexBufferBinds;

    /// <summary>
    /// Stores the CPU time spent recording resource barriers for the current command list.
    /// </summary>
    private double _perfBarrierMs;

    /// <summary>
    /// Stores the perf begin wait count value used during command execution.
    /// </summary>
    private ulong _perfBeginWaitCount;

    /// <summary>
    /// Stores the perf begin wait ms state used by this instance.
    /// </summary>
    private double _perfBeginWaitMs;

    /// <summary>
    /// Stores the CPU time spent recording descriptor copies for the current command list.
    /// </summary>
    private double _perfDescriptorCopyMs;

    /// <summary>
    /// Stores the perf descriptor copies state used by this instance.
    /// </summary>
    private ulong _perfDescriptorCopies;

    /// <summary>
    /// Stores the CPU time spent recording dispatch work for the current command list.
    /// </summary>
    private double _perfDispatchMs;

    /// <summary>
    /// Stores the perf dispatch calls state used by this instance.
    /// </summary>
    private ulong _perfDispatchCalls;

    /// <summary>
    /// Stores the CPU time spent recording draw work for the current command list.
    /// </summary>
    private double _perfDrawMs;

    /// <summary>
    /// Stores the timestamp captured at the beginning of the current command list recording.
    /// </summary>
    private long _perfFrameStartTicks;

    /// <summary>
    /// Stores the Gen0 collection count captured at the beginning of the current command list recording.
    /// </summary>
    private int _perfGc0Start;

    /// <summary>
    /// Stores the Gen1 collection count captured at the beginning of the current command list recording.
    /// </summary>
    private int _perfGc1Start;

    /// <summary>
    /// Stores the Gen2 collection count captured at the beginning of the current command list recording.
    /// </summary>
    private int _perfGc2Start;

    /// <summary>
    /// Stores accumulated Gen0 collections observed while command lists were recording.
    /// </summary>
    private ulong _perfAccumGc0Collections;

    /// <summary>
    /// Stores accumulated Gen1 collections observed while command lists were recording.
    /// </summary>
    private ulong _perfAccumGc1Collections;

    /// <summary>
    /// Stores accumulated Gen2 collections observed while command lists were recording.
    /// </summary>
    private ulong _perfAccumGc2Collections;

    /// <summary>
    /// Stores the perf draw calls state used by this instance.
    /// </summary>
    private ulong _perfDrawCalls;

    /// <summary>
    /// Stores the perf frames state used by this instance.
    /// </summary>
    private ulong _perfFrames;

    /// <summary>
    /// Stores the perf index buffer binds value used during command execution.
    /// </summary>
    private ulong _perfIndexBufferBinds;

    /// <summary>
    /// Stores the perf last report ms state used by this instance.
    /// </summary>
    private double _perfLastReportMs;

    /// <summary>
    /// Stores the command-list API name that preceded the largest external gap in the current command list.
    /// </summary>
    private string _perfMaxExternalGapAfter;

    /// <summary>
    /// Stores the command-list API name that started the largest external gap in the current command list.
    /// </summary>
    private string _perfMaxExternalGapBefore;

    /// <summary>
    /// Stores the largest gap between Veldrith command-list calls in the current reporting window.
    /// </summary>
    private double _perfMaxExternalGapMs;

    /// <summary>
    /// Stores the debug scope active during the largest external gap in the current command list.
    /// </summary>
    private string _perfMaxExternalGapScope;

    /// <summary>
    /// Stores the stack trace captured at the API entry after the largest external gap in the current command list.
    /// </summary>
    private string _perfMaxExternalGapStack;

    /// <summary>
    /// Stores the command-list API timestamp captured at the previous D3D12 command-list entry point.
    /// </summary>
    private long _perfLastCommandApiTicks;

    /// <summary>
    /// Stores the previous D3D12 command-list entry point name for gap attribution.
    /// </summary>
    private string _perfLastCommandApiName;

    /// <summary>
    /// Stores the most recent debug marker name for performance gap attribution.
    /// </summary>
    private string _perfLastDebugMarker;

    /// <summary>
    /// Stores the largest per-command-list barrier recording time observed in the current report window.
    /// </summary>
    private double _perfMaxBarrierMs;

    /// <summary>
    /// Stores the largest begin wait time observed in the current report window.
    /// </summary>
    private double _perfMaxBeginWaitMs;

    /// <summary>
    /// Stores the largest per-command-list descriptor copy time observed in the current report window.
    /// </summary>
    private double _perfMaxDescriptorCopyMs;

    /// <summary>
    /// Stores the largest per-command-list dispatch recording time observed in the current report window.
    /// </summary>
    private double _perfMaxDispatchMs;

    /// <summary>
    /// Stores the largest per-command-list draw recording time observed in the current report window.
    /// </summary>
    private double _perfMaxDrawMs;

    /// <summary>
    /// Stores the largest per-command-list pipeline binding time observed in the current report window.
    /// </summary>
    private double _perfMaxPipelineSetMs;

    /// <summary>
    /// Stores the largest command-list recording time observed in the current report window.
    /// </summary>
    private double _perfMaxRecordMs;

    /// <summary>
    /// Stores the largest command-list recording time not explained by tracked D3D12 work.
    /// </summary>
    private double _perfMaxUntrackedRecordMs;

    /// <summary>
    /// Stores the largest per-command-list resource set flush time observed in the current report window.
    /// </summary>
    private double _perfMaxResourceSetFlushMs;

    /// <summary>
    /// Stores the largest per-command-list upload recording time observed in the current report window.
    /// </summary>
    private double _perfMaxUploadRecordMs;

    /// <summary>
    /// Stores the largest gap between Veldrith command-list calls observed in the current report window.
    /// </summary>
    private double _perfReportMaxExternalGapMs;

    /// <summary>
    /// Stores the API transition that produced the largest external gap in the current report window.
    /// </summary>
    private string _perfReportMaxExternalGapTransition;

    /// <summary>
    /// Stores the debug scope for the largest external gap in the current report window.
    /// </summary>
    private string _perfReportMaxExternalGapScope;

    /// <summary>
    /// Stores the stack trace captured for the largest external gap in the current report window.
    /// </summary>
    private string _perfReportMaxExternalGapStack;

    /// <summary>
    /// Tracks whether the current command list is recording API gaps for D3D12 performance logging.
    /// </summary>
    private bool _perfRecordingCommandGaps;

    /// <summary>
    /// Stores the CPU time spent binding pipeline state for the current command list.
    /// </summary>
    private double _perfPipelineSetMs;

    /// <summary>
    /// Stores the perf pipeline changes state used by this instance.
    /// </summary>
    private ulong _perfPipelineChanges;

    /// <summary>
    /// Stores the CPU time spent flushing changed resource sets for the current command list.
    /// </summary>
    private double _perfResourceSetFlushMs;

    /// <summary>
    /// Stores the perf resource set changes collection used by this instance.
    /// </summary>
    private ulong _perfResourceSetChanges;

    /// <summary>
    /// Stores the perf root table sets state used by this instance.
    /// </summary>
    private ulong _perfRootTableSets;

    /// <summary>
    /// Stores the perf subresource transitions state used by this instance.
    /// </summary>
    private ulong _perfSubresourceTransitions;

    /// <summary>
    /// Stores the perf transitions state used by this instance.
    /// </summary>
    private ulong _perfTransitions;

    /// <summary>
    /// Stores the CPU time spent recording upload commands for the current command list.
    /// </summary>
    private double _perfUploadRecordMs;

    /// <summary>
    /// Stores the perf uav barriers state used by this instance.
    /// </summary>
    private ulong _perfUavBarriers;

    /// <summary>
    /// Stores the perf vertex buffer binds state used by this instance.
    /// </summary>
    private ulong _perfVertexBufferBinds;

    /// <summary>
    /// Stores the transitioned back buffer index value used during command execution.
    /// </summary>
    private int _transitionedBackBufferIndex = -1;

    /// <summary>
    /// Stores the uav barrier pending state used by this instance.
    /// </summary>
    private bool _uavBarrierPending;

    /// <summary>
    /// Converts high-resolution stopwatch ticks to milliseconds for D3D12 performance logging.
    /// </summary>
    /// <param name="ticks">The elapsed stopwatch ticks.</param>
    /// <returns>The elapsed time in milliseconds.</returns>
    private static double TicksToMilliseconds(long ticks) {
        return ticks * 1000.0 / Stopwatch.Frequency;
    }

    /// <summary>
    /// Starts per-command-list API gap tracking for D3D12 performance logging.
    /// </summary>
    private void BeginPerfCommandGapTracking() {
        this._perfDebugGroupStack.Clear();
        this._perfLastDebugMarker = null;
        this._perfMaxExternalGapMs = 0;
        this._perfMaxExternalGapBefore = null;
        this._perfMaxExternalGapAfter = null;
        this._perfMaxExternalGapScope = null;
        this._perfMaxExternalGapStack = null;
        this._perfLastCommandApiTicks = Stopwatch.GetTimestamp();
        this._perfLastCommandApiName = "Begin";
        this._perfRecordingCommandGaps = true;
    }

    /// <summary>
    /// Records the elapsed wall-clock gap since the previous D3D12 command-list entry point.
    /// </summary>
    /// <param name="apiName">The current command-list API name.</param>
    private PerfCommandApiScope TrackPerfCommandApi(string apiName) {
        if (!_perfLogEnabled || !this._perfRecordingCommandGaps) {
            return default;
        }

        long now = Stopwatch.GetTimestamp();
        if (this._perfLastCommandApiTicks != 0) {
            double gapMs = TicksToMilliseconds(now - this._perfLastCommandApiTicks);
            if (gapMs > this._perfMaxExternalGapMs) {
                this._perfMaxExternalGapMs = gapMs;
                this._perfMaxExternalGapBefore = this._perfLastCommandApiName;
                this._perfMaxExternalGapAfter = apiName;
                this._perfMaxExternalGapScope = this.GetPerfDebugScope();
                this._perfMaxExternalGapStack = _perfStackLogEnabled && gapMs >= PerfRecordSpikeThresholdMs
                    ? new StackTrace(1, true).ToString()
                    : null;
            }
        }

        return new PerfCommandApiScope(this, apiName);
    }

    /// <summary>
    /// Marks the current command-list API call as completed for exit-to-entry gap attribution.
    /// </summary>
    /// <param name="apiName">The command-list API name.</param>
    private void CompletePerfCommandApi(string apiName) {
        if (!_perfLogEnabled || !this._perfRecordingCommandGaps) {
            return;
        }

        this._perfLastCommandApiTicks = Stopwatch.GetTimestamp();
        this._perfLastCommandApiName = apiName;
    }

    /// <summary>
    /// Gets the active debug scope for D3D12 performance gap attribution.
    /// </summary>
    /// <returns>The active scope name.</returns>
    private string GetPerfDebugScope() {
        if (this._perfDebugGroupStack.Count > 0) {
            return this._perfDebugGroupStack[this._perfDebugGroupStack.Count - 1];
        }

        return string.IsNullOrEmpty(this._perfLastDebugMarker) ? "<none>" : this._perfLastDebugMarker;
    }

    /// <summary>
    /// Updates report-window max gap state from the current command list.
    /// </summary>
    private void AccumulatePerfCommandGapReport() {
        if (this._perfMaxExternalGapMs <= this._perfReportMaxExternalGapMs) {
            return;
        }

        this._perfReportMaxExternalGapMs = this._perfMaxExternalGapMs;
        this._perfReportMaxExternalGapTransition = $"{this._perfMaxExternalGapBefore}->{this._perfMaxExternalGapAfter}";
        this._perfReportMaxExternalGapScope = this._perfMaxExternalGapScope;
        this._perfReportMaxExternalGapStack = this._perfMaxExternalGapStack;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12CommandList" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="features">The features value used by this operation.</param>
    /// <param name="uniformAlignment">The uniform alignment value used by this operation.</param>
    /// <param name="structuredAlignment">The structured alignment value used by this operation.</param>
    public D3D12CommandList(D3D12GraphicsDevice gd, ref CommandListDescription description, GraphicsDeviceFeatures features, uint uniformAlignment, uint structuredAlignment) : base(ref description, features, uniformAlignment, structuredAlignment) {
        this.gd = gd;

        for (int i = 0; i < FramesInFlight; i++) {
            this._commandAllocators[i] = gd.Device.CreateCommandAllocator(CommandListType.Direct);
        }

        this._shaderVisibleSrvUavHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, this._maxSrvUavDescriptors, DescriptorHeapFlags.ShaderVisible));
        this._shaderVisibleSamplerHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.Sampler, this._maxSamplerDescriptors, DescriptorHeapFlags.ShaderVisible));
        this.NativeCommandList = gd.Device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, this._commandAllocators[0]);
        this._srvUavDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        this._samplerDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler);
        this._beginEventMethod = this.GetDebugMarkerMethod("BeginEvent");
        this._setMarkerMethod = this.GetDebugMarkerMethod("SetMarker");
        this._endEventMethod = this.NativeCommandList.GetType().GetMethod("EndEvent", Type.EmptyTypes);
        this.NativeCommandList.Close();
    }

    /// <summary>
    /// Gets or sets NativeCommandList.
    /// </summary>
    public ID3D12GraphicsCommandList NativeCommandList { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this.DisposePendingSubmissionDisposals();
        this._gpuMipPipeline?.Dispose();
        this._gpuMipResourceLayout?.Dispose();
        this._gpuMipSampler?.Dispose();
        this._drawIndirectSignature?.Dispose();
        this._drawIndexedIndirectSignature?.Dispose();
        this._dispatchIndirectSignature?.Dispose();
        ClearBoundResourceSets(this._boundGraphicsResourceSets);
        ClearBoundResourceSets(this._boundComputeResourceSets);
        this.NativeCommandList.Dispose();
        for (int i = 0; i < FramesInFlight; i++) {
            this._commandAllocators[i]?.Dispose();
        }

        this._shaderVisibleSrvUavHeap?.Dispose();
        this._shaderVisibleSamplerHeap?.Dispose();
        this._disposed = true;
    }

    /// <summary>
    /// Begins the value operation.
    /// </summary>
    public override void Begin() {
        this.DisposePendingSubmissionDisposals();

        if (_perfLogEnabled) {
            this._perfFrameStartTicks = Stopwatch.GetTimestamp();
            this._perfGc0Start = GC.CollectionCount(0);
            this._perfGc1Start = GC.CollectionCount(1);
            this._perfGc2Start = GC.CollectionCount(2);
            this._perfBarrierMs = 0;
            this._perfBeginWaitCount = 0;
            this._perfBeginWaitMs = 0;
            this._perfDescriptorCopyMs = 0;
            this._perfDispatchMs = 0;
            this._perfDrawMs = 0;
            this._perfPipelineSetMs = 0;
            this._perfResourceSetFlushMs = 0;
            this._perfUploadRecordMs = 0;
            this._perfTransitions = 0;
            this._perfSubresourceTransitions = 0;
            this._perfUavBarriers = 0;
            this._perfPipelineChanges = 0;
            this._perfResourceSetChanges = 0;
            this._perfDescriptorCopies = 0;
            this._perfRootTableSets = 0;
            this._perfVertexBufferBinds = 0;
            this._perfIndexBufferBinds = 0;
            this._perfDrawCalls = 0;
            this._perfDispatchCalls = 0;
        }

        this._currentFrameSlot = (this._currentFrameSlot + 1) % FramesInFlight;
        this.WaitForFrameSlot(this._currentFrameSlot);
        this._commandAllocators[this._currentFrameSlot].Reset();
        this.NativeCommandList.Reset(this._commandAllocators[this._currentFrameSlot]);
        this._begun = true;
        this._ended = false;
        this._transitionedBackBufferIndex = -1;
        this._descriptorHeapsBound = false;
        this._activeViewportCount = 0;
        this._activeScissorRectCount = 0;
        this._uavBarrierPending = false;
        Array.Clear(this._boundVertexBuffers, 0, this._boundVertexBuffers.Length);
        Array.Clear(this._boundVertexBufferOffsets, 0, this._boundVertexBufferOffsets.Length);
        Array.Clear(this._boundVertexBufferStrides, 0, this._boundVertexBufferStrides.Length);
        Array.Clear(this._boundVertexBufferVersions, 0, this._boundVertexBufferVersions.Length);
        this._boundIndexBuffer = null;
        this._boundIndexBufferOffset = 0;
        this._boundIndexBufferVersion = 0;
        this._boundIndexFormat = IndexFormat.UInt16;
        this._hasBoundIndexBuffer = false;
        ClearBoundResourceSets(this._boundGraphicsResourceSets);
        ClearBoundResourceSets(this._boundComputeResourceSets);
        ClearChangedResourceSets(this._graphicsResourceSetsChanged);
        ClearChangedResourceSets(this._computeResourceSetsChanged);
        this._graphicsResourceSetsDirty = false;
        this._computeResourceSetsDirty = false;
        this.InvalidateGraphicsRootCaches();
        this.InvalidateComputeRootCaches();
        this._maxBoundVertexBufferSlot = 0;
        this._currentPrimitiveTopologyValid = false;
        this._currentStencilReferenceValid = false;
        this.ClearCachedState();
        this._currentGraphicsPipeline = null;
        this._currentComputePipeline = null;

        if (_perfLogEnabled) {
            this.BeginPerfCommandGapTracking();
        }
    }

    /// <summary>
    /// Ends the value operation.
    /// </summary>
    public override void End() {
        if (!this._begun) {
            throw new VeldridException("CommandList.End cannot be called before Begin.");
        }

        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.End));
        this.FlushPendingUavBarrier();
        this.TransitionSwapchainBackBuffersToPresent();
        this.NativeCommandList.Close();
        this._ended = true;

        if (_perfLogEnabled) {
            double recordMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - this._perfFrameStartTicks);
            int gc0Delta = GC.CollectionCount(0) - this._perfGc0Start;
            int gc1Delta = GC.CollectionCount(1) - this._perfGc1Start;
            int gc2Delta = GC.CollectionCount(2) - this._perfGc2Start;
            double trackedMs = this._perfBeginWaitMs
                               + this._perfPipelineSetMs
                               + this._perfResourceSetFlushMs
                               + this._perfBarrierMs
                               + this._perfDescriptorCopyMs
                               + this._perfUploadRecordMs
                               + this._perfDrawMs
                               + this._perfDispatchMs;
            double untrackedMs = Math.Max(0, recordMs - trackedMs);
            this._perfFrames++;
            this._perfMaxRecordMs = Math.Max(this._perfMaxRecordMs, recordMs);
            this._perfMaxUntrackedRecordMs = Math.Max(this._perfMaxUntrackedRecordMs, untrackedMs);
            this._perfAccumGc0Collections += (ulong)Math.Max(gc0Delta, 0);
            this._perfAccumGc1Collections += (ulong)Math.Max(gc1Delta, 0);
            this._perfAccumGc2Collections += (ulong)Math.Max(gc2Delta, 0);
            this._perfMaxBeginWaitMs = Math.Max(this._perfMaxBeginWaitMs, this._perfBeginWaitMs);
            this._perfMaxPipelineSetMs = Math.Max(this._perfMaxPipelineSetMs, this._perfPipelineSetMs);
            this._perfMaxResourceSetFlushMs = Math.Max(this._perfMaxResourceSetFlushMs, this._perfResourceSetFlushMs);
            this._perfMaxBarrierMs = Math.Max(this._perfMaxBarrierMs, this._perfBarrierMs);
            this._perfMaxDescriptorCopyMs = Math.Max(this._perfMaxDescriptorCopyMs, this._perfDescriptorCopyMs);
            this._perfMaxUploadRecordMs = Math.Max(this._perfMaxUploadRecordMs, this._perfUploadRecordMs);
            this._perfMaxDrawMs = Math.Max(this._perfMaxDrawMs, this._perfDrawMs);
            this._perfMaxDispatchMs = Math.Max(this._perfMaxDispatchMs, this._perfDispatchMs);
            this._perfAccumBarrierMs += this._perfBarrierMs;
            this._perfAccumBeginWaitCount += this._perfBeginWaitCount;
            this._perfAccumBeginWaitMs += this._perfBeginWaitMs;
            this._perfAccumDescriptorCopyMs += this._perfDescriptorCopyMs;
            this._perfAccumDispatchMs += this._perfDispatchMs;
            this._perfAccumDrawMs += this._perfDrawMs;
            this._perfAccumPipelineSetMs += this._perfPipelineSetMs;
            this._perfAccumResourceSetFlushMs += this._perfResourceSetFlushMs;
            this._perfAccumUploadRecordMs += this._perfUploadRecordMs;
            this._perfAccumTransitions += this._perfTransitions;
            this._perfAccumSubresourceTransitions += this._perfSubresourceTransitions;
            this._perfAccumUavBarriers += this._perfUavBarriers;
            this._perfAccumPipelineChanges += this._perfPipelineChanges;
            this._perfAccumResourceSetChanges += this._perfResourceSetChanges;
            this._perfAccumDescriptorCopies += this._perfDescriptorCopies;
            this._perfAccumRootTableSets += this._perfRootTableSets;
            this._perfAccumVertexBufferBinds += this._perfVertexBufferBinds;
            this._perfAccumIndexBufferBinds += this._perfIndexBufferBinds;
            this._perfAccumDrawCalls += this._perfDrawCalls;
            this._perfAccumDispatchCalls += this._perfDispatchCalls;
            this.AccumulatePerfCommandGapReport();

            if (untrackedMs >= PerfRecordSpikeThresholdMs) {
                Console.WriteLine($"[D3D12 PERF SPIKE] recordMs={recordMs:F3}, trackedMs={trackedMs:F3}, untrackedMs={untrackedMs:F3}, " + $"wait={this._perfBeginWaitMs:F3}, pso={this._perfPipelineSetMs:F3}, rs={this._perfResourceSetFlushMs:F3}, barrier={this._perfBarrierMs:F3}, " + $"upload={this._perfUploadRecordMs:F3}, draw={this._perfDrawMs:F3}, dispatch={this._perfDispatchMs:F3}, " + $"gc={Math.Max(gc0Delta, 0)}/{Math.Max(gc1Delta, 0)}/{Math.Max(gc2Delta, 0)}, psoCount={this._perfPipelineChanges}, rsCount={this._perfResourceSetChanges}, drawCount={this._perfDrawCalls}");
                Console.WriteLine($"[D3D12 PERF GAP] maxGapMs={this._perfMaxExternalGapMs:F3}, transition={this._perfMaxExternalGapBefore}->{this._perfMaxExternalGapAfter}, scope={this._perfMaxExternalGapScope}");
                if (!string.IsNullOrEmpty(this._perfMaxExternalGapStack)) {
                    Console.WriteLine($"[D3D12 PERF GAP STACK]\n{this._perfMaxExternalGapStack}");
                }
            }

            if (this._perfFrames % PerfReportIntervalFrames == 0) {
                double elapsedMs = this._perfStopwatch.Elapsed.TotalMilliseconds;
                double reportWindowMs = elapsedMs - this._perfLastReportMs;
                this._perfLastReportMs = elapsedMs;
                double invFrames = 1.0 / PerfReportIntervalFrames;
                Console.WriteLine($"[D3D12 PERF] {PerfReportIntervalFrames}f/{reportWindowMs:F0}ms avg: " + $"wait={this._perfAccumBeginWaitMs * invFrames:F3}ms ({this._perfAccumBeginWaitCount * invFrames:F2}x), " + $"psoMs={this._perfAccumPipelineSetMs * invFrames:F3}, rsMs={this._perfAccumResourceSetFlushMs * invFrames:F3}, " + $"barrierMs={this._perfAccumBarrierMs * invFrames:F3}, descCopyMs={this._perfAccumDescriptorCopyMs * invFrames:F3}, uploadMs={this._perfAccumUploadRecordMs * invFrames:F3}, " + $"drawMs={this._perfAccumDrawMs * invFrames:F3}, dispatchMs={this._perfAccumDispatchMs * invFrames:F3}, " + $"maxRecordMs={this._perfMaxRecordMs:F3}, maxUntrackedMs={this._perfMaxUntrackedRecordMs:F3}, maxWaitMs={this._perfMaxBeginWaitMs:F3}, maxPsoMs={this._perfMaxPipelineSetMs:F3}, maxRsMs={this._perfMaxResourceSetFlushMs:F3}, " + $"maxBarrierMs={this._perfMaxBarrierMs:F3}, maxUploadMs={this._perfMaxUploadRecordMs:F3}, maxDrawMs={this._perfMaxDrawMs:F3}, " + $"gc={this._perfAccumGc0Collections}/{this._perfAccumGc1Collections}/{this._perfAccumGc2Collections}, " + $"trans={this._perfAccumTransitions * invFrames:F1}, subTrans={this._perfAccumSubresourceTransitions * invFrames:F1}, uavB={this._perfAccumUavBarriers * invFrames:F1}, " + $"pso={this._perfAccumPipelineChanges * invFrames:F1}, rs={this._perfAccumResourceSetChanges * invFrames:F1}, " + $"descCopy={this._perfAccumDescriptorCopies * invFrames:F1}, rootTbl={this._perfAccumRootTableSets * invFrames:F1}, " + $"vb={this._perfAccumVertexBufferBinds * invFrames:F1}, ib={this._perfAccumIndexBufferBinds * invFrames:F1}, " + $"draw={this._perfAccumDrawCalls * invFrames:F1}, dispatch={this._perfAccumDispatchCalls * invFrames:F1}");
                Console.WriteLine($"[D3D12 PERF GAP] windowMaxGapMs={this._perfReportMaxExternalGapMs:F3}, transition={this._perfReportMaxExternalGapTransition}, scope={this._perfReportMaxExternalGapScope}");

                this._perfAccumBarrierMs = 0;
                this._perfAccumBeginWaitCount = 0;
                this._perfAccumBeginWaitMs = 0;
                this._perfAccumDescriptorCopyMs = 0;
                this._perfAccumDispatchMs = 0;
                this._perfAccumDrawMs = 0;
                this._perfAccumPipelineSetMs = 0;
                this._perfAccumResourceSetFlushMs = 0;
                this._perfAccumUploadRecordMs = 0;
                this._perfAccumTransitions = 0;
                this._perfAccumSubresourceTransitions = 0;
                this._perfAccumUavBarriers = 0;
                this._perfAccumPipelineChanges = 0;
                this._perfAccumResourceSetChanges = 0;
                this._perfAccumDescriptorCopies = 0;
                this._perfAccumRootTableSets = 0;
                this._perfAccumVertexBufferBinds = 0;
                this._perfAccumIndexBufferBinds = 0;
                this._perfAccumDrawCalls = 0;
                this._perfAccumDispatchCalls = 0;
                this._perfAccumGc0Collections = 0;
                this._perfAccumGc1Collections = 0;
                this._perfAccumGc2Collections = 0;
                this._perfMaxBarrierMs = 0;
                this._perfMaxBeginWaitMs = 0;
                this._perfMaxDescriptorCopyMs = 0;
                this._perfMaxDispatchMs = 0;
                this._perfMaxDrawMs = 0;
                this._perfMaxPipelineSetMs = 0;
                this._perfMaxRecordMs = 0;
                this._perfMaxUntrackedRecordMs = 0;
                this._perfMaxResourceSetFlushMs = 0;
                this._perfMaxUploadRecordMs = 0;
                this._perfReportMaxExternalGapMs = 0;
                this._perfReportMaxExternalGapTransition = null;
                this._perfReportMaxExternalGapScope = null;
                this._perfReportMaxExternalGapStack = null;
            }
        }

        this._perfRecordingCommandGaps = false;
    }

    /// <summary>
    /// Sets the viewport value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="viewport">The viewport value used by this operation.</param>
    public override void SetViewport(uint index, ref Viewport viewport) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.SetViewport));
        if (index >= this._activeViewports.Length) {
            return;
        }

        this._activeViewports[index] = new Vortice.Mathematics.Viewport(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);

        if (index + 1 > this._activeViewportCount) {
            this._activeViewportCount = index + 1;
        }

        this.NativeCommandList.RSSetViewports(this._activeViewportCount, this._activeViewports);
    }

    /// <summary>
    /// Sets the scissor rect value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.SetScissorRect));
        if (index >= this._activeScissorRects.Length) {
            return;
        }

        this._activeScissorRects[index] = new RawRect((int)x, (int)y, (int)(x + width), (int)(y + height));

        if (index + 1 > this._activeScissorRectCount) {
            this._activeScissorRectCount = index + 1;
        }

        this.NativeCommandList.RSSetScissorRects(this._activeScissorRectCount, this._activeScissorRects);
    }

    /// <summary>
    /// Executes the dispatch logic for this backend.
    /// </summary>
    /// <param name="groupCountX">The group count x value used by this operation.</param>
    /// <param name="groupCountY">The group count y value used by this operation.</param>
    /// <param name="groupCountZ">The group count z value used by this operation.</param>
    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.Dispatch));
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushComputeResourceSets();
        this.FlushPendingUavBarrier();
        this.NativeCommandList.Dispatch(groupCountX, groupCountY, groupCountZ);
        if (_perfLogEnabled) {
            this._perfDispatchCalls++;
            this._perfDispatchMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }

        this._uavBarrierPending = true;
    }

    /// <summary>
    /// Executes the execute no signal logic for this backend.
    /// </summary>
    internal void ExecuteNoSignal() {
        if (!this._ended) {
            throw new VeldridException("CommandList must be ended before submit.");
        }

        this.gd.CommandQueue.ExecuteCommandList(this.NativeCommandList);
    }

    /// <summary>
    /// Executes the mark submitted logic for this backend.
    /// </summary>
    /// <param name="signalValue">The signal value value used by this operation.</param>
    internal void MarkSubmitted(ulong signalValue) {
        if (this._currentFrameSlot >= 0) {
            this._frameSlotFenceValues[this._currentFrameSlot] = signalValue;
        }

        if (this._pendingSubmissionUploadBuffers.Count == 0) {
            return;
        }

        for (int i = 0; i < this._pendingSubmissionUploadBuffers.Count; i++) {
            this.gd.EnqueueSubmissionUploadBuffer(this._pendingSubmissionUploadBuffers[i], signalValue);
        }

        this._pendingSubmissionUploadBuffers.Clear();
    }

    /// <summary>
    /// Sets the graphics resource set core value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="rs">The rs value used by this operation.</param>
    /// <param name="dynamicOffsetsCount">The dynamic offsets count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.SetGraphicsResourceSet));
        if (this._currentGraphicsPipeline == null) {
            return;
        }

        if (slot >= this._boundGraphicsResourceSets.Length) {
            Util.EnsureArrayMinimumSize(ref this._boundGraphicsResourceSets, slot + 1);
            Util.EnsureArrayMinimumSize(ref this._graphicsResourceSetsChanged, slot + 1);
        }

        BoundResourceSetInfo previousBinding = this._boundGraphicsResourceSets[slot];
        if (previousBinding.Equals(rs, dynamicOffsetsCount, ref dynamicOffsets)) {
            return;
        }

        this._boundGraphicsResourceSets[slot].Offsets.Dispose();
        this._boundGraphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
        if (_perfLogEnabled) {
            this._perfResourceSetChanges++;
        }

        Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(rs);
        this._graphicsResourceSetsChanged[slot] = true;
        this._graphicsResourceSetsDirty = true;
    }

    /// <summary>
    /// Sets the compute resource set core value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="dynamicOffsetsCount">The dynamic offsets count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.SetComputeResourceSet));
        if (this._currentComputePipeline == null) {
            return;
        }

        if (slot >= this._boundComputeResourceSets.Length) {
            Util.EnsureArrayMinimumSize(ref this._boundComputeResourceSets, slot + 1);
            Util.EnsureArrayMinimumSize(ref this._computeResourceSetsChanged, slot + 1);
        }

        BoundResourceSetInfo previousBinding = this._boundComputeResourceSets[slot];
        if (previousBinding.Equals(set, dynamicOffsetsCount, ref dynamicOffsets)) {
            return;
        }

        this._boundComputeResourceSets[slot].Offsets.Dispose();
        this._boundComputeResourceSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetsCount, ref dynamicOffsets);
        if (_perfLogEnabled) {
            this._perfResourceSetChanges++;
        }

        Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(set);
        this._computeResourceSetsChanged[slot] = true;
        this._computeResourceSetsDirty = true;
    }

    /// <summary>
    /// Sets the framebuffer core value.
    /// </summary>
    /// <param name="fb">The fb value used by this operation.</param>
    protected override void SetFramebufferCore(Framebuffer fb) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.SetFramebuffer));
        if (fb is D3D12SwapchainFramebuffer swapchainFramebuffer && swapchainFramebuffer.Swapchain.TryGetCurrentBackBuffer(out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int backBufferIndex, out ResourceStates currentState)) {
            this.Transition(backBuffer, currentState, ResourceStates.RenderTarget);
            swapchainFramebuffer.Swapchain.SetBackBufferState(backBufferIndex, ResourceStates.RenderTarget);
            this._transitionedBackBufferIndex = backBufferIndex;
            if (swapchainFramebuffer.DepthTargetTexture != null) {
                this.TransitionTexture(swapchainFramebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
            }

            if (swapchainFramebuffer.TryGetDepthStencilView(out CpuDescriptorHandle swapchainDsv)) {
                this.NativeCommandList.OMSetRenderTargets(rtv, swapchainDsv);
            }
            else {
                this.NativeCommandList.OMSetRenderTargets(rtv);
            }

            return;
        }

        D3D12Framebuffer d3D12Framebuffer = Util.AssertSubtype<Framebuffer, D3D12Framebuffer>(fb);
        foreach (D3D12Texture colorTexture in d3D12Framebuffer.ColorTargetTextures) {
            if (colorTexture != null) {
                this.TransitionTexture(colorTexture, ResourceStates.RenderTarget);
            }
        }

        if (d3D12Framebuffer.DepthTargetTexture != null) {
            this.TransitionTexture(d3D12Framebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
        }

        if (!d3D12Framebuffer.TryGetColorTargetViews(out CpuDescriptorHandle[] rtvs)) {
            if (d3D12Framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle depthOnlyDsv)) {
                this.NativeCommandList.OMSetRenderTargets(Array.Empty<CpuDescriptorHandle>(), depthOnlyDsv);
            }

            return;
        }

        if (d3D12Framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv)) {
            this.NativeCommandList.OMSetRenderTargets(rtvs, dsv);
        }
        else {
            this.NativeCommandList.OMSetRenderTargets(rtvs);
        }
    }

    /// <summary>
    /// Executes the draw indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="drawCount">The draw count value used by this operation.</param>
    /// <param name="stride">The stride value used by this operation.</param>
    protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.DrawIndirect));
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushGraphicsResourceSets();
        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(indirectBuffer);
        uint argumentSize = (uint)Unsafe.SizeOf<IndirectDrawArguments>();
        if (drawCount > 0) {
            ulong requiredSize = offset + (drawCount - 1UL) * stride + argumentSize;
            if (requiredSize > d3D12Buffer.SizeInBytes) {
                throw new VeldridException("Indirect draw argument range exceeds buffer bounds.");
            }
        }

        if (this.EnsureIndirectCommandSignatures()) {
            this.ExecuteIndirect(d3D12Buffer, offset, drawCount, stride, argumentSize, this._drawIndirectSignature);
            if (_perfLogEnabled) {
                this._perfDrawCalls += drawCount;
                this._perfDrawMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            }

            return;
        }

        // Fallback path if command signatures are unavailable.
        unsafe {
            if (!d3D12Buffer.TryGetCpuReadPointer(out IntPtr mappedPointer)) {
                throw new PlatformNotSupportedException("D3D12 indirect fallback requires a CPU-readable argument buffer.");
            }

            byte* basePtr = (byte*)mappedPointer + offset;
            for (uint i = 0; i < drawCount; i++) {
                IndirectDrawArguments arguments = *(IndirectDrawArguments*)(basePtr + i * stride);
                this.NativeCommandList.DrawInstanced(arguments.VertexCount, arguments.InstanceCount, arguments.FirstVertex, arguments.FirstInstance);
            }
        }

        if (_perfLogEnabled) {
            this._perfDrawCalls += drawCount;
            this._perfDrawMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Executes the draw indexed indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="drawCount">The draw count value used by this operation.</param>
    /// <param name="stride">The stride value used by this operation.</param>
    protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.DrawIndexedIndirect));
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushGraphicsResourceSets();
        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(indirectBuffer);
        uint argumentSize = (uint)Unsafe.SizeOf<IndirectDrawIndexedArguments>();
        if (drawCount > 0) {
            ulong requiredSize = offset + (drawCount - 1UL) * stride + argumentSize;
            if (requiredSize > d3D12Buffer.SizeInBytes) {
                throw new VeldridException("Indirect indexed draw argument range exceeds buffer bounds.");
            }
        }

        if (this.EnsureIndirectCommandSignatures()) {
            this.ExecuteIndirect(d3D12Buffer, offset, drawCount, stride, argumentSize, this._drawIndexedIndirectSignature);
            if (_perfLogEnabled) {
                this._perfDrawCalls += drawCount;
                this._perfDrawMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            }

            return;
        }

        // Fallback path if command signatures are unavailable.
        unsafe {
            if (!d3D12Buffer.TryGetCpuReadPointer(out IntPtr mappedPointer)) {
                throw new PlatformNotSupportedException("D3D12 indirect fallback requires a CPU-readable argument buffer.");
            }

            byte* basePtr = (byte*)mappedPointer + offset;
            for (uint i = 0; i < drawCount; i++) {
                IndirectDrawIndexedArguments arguments = *(IndirectDrawIndexedArguments*)(basePtr + i * stride);
                this.NativeCommandList.DrawIndexedInstanced(arguments.IndexCount, arguments.InstanceCount, arguments.FirstIndex, arguments.VertexOffset, arguments.FirstInstance);
            }
        }

        if (_perfLogEnabled) {
            this._perfDrawCalls += drawCount;
            this._perfDrawMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Executes the dispatch indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.DispatchIndirect));
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushComputeResourceSets();
        D3D12DeviceBuffer d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(indirectBuffer);
        uint argumentSize = (uint)Unsafe.SizeOf<IndirectDispatchArguments>();
        ulong requiredSize = (ulong)offset + argumentSize;
        if (requiredSize > d3d12Buffer.SizeInBytes) {
            throw new VeldridException("Indirect dispatch argument range exceeds buffer bounds.");
        }

        if (this.EnsureIndirectCommandSignatures()) {
            this.ExecuteIndirect(d3d12Buffer, offset, 1, argumentSize, argumentSize, this._dispatchIndirectSignature);
            if (_perfLogEnabled) {
                this._perfDispatchCalls++;
                this._perfDispatchMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            }

            return;
        }

        // Fallback path if command signatures are unavailable.
        unsafe {
            if (!d3d12Buffer.TryGetCpuReadPointer(out IntPtr mappedPointer)) {
                throw new PlatformNotSupportedException("D3D12 indirect fallback requires a CPU-readable argument buffer.");
            }

            IndirectDispatchArguments arguments = *(IndirectDispatchArguments*)((byte*)mappedPointer + offset);
            this.NativeCommandList.Dispatch(arguments.GroupCountX, arguments.GroupCountY, arguments.GroupCountZ);
        }

        if (_perfLogEnabled) {
            this._perfDispatchCalls++;
            this._perfDispatchMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Executes the resolve texture core logic for this backend.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="destination">The destination value or resource.</param>
    protected override void ResolveTextureCore(Texture source, Texture destination) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.ResolveTexture));
        this.FlushPendingUavBarrier();
        D3D12Texture src = Util.AssertSubtype<Texture, D3D12Texture>(source);
        D3D12Texture dst = Util.AssertSubtype<Texture, D3D12Texture>(destination);

        if (src.NativeTexture == null || dst.NativeTexture == null) {
            src.CopyRegionTo(dst, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, source.Width, source.Height, source.Depth, source.ArrayLayers);
            return;
        }

        ResourceStates[] srcPreviousStates = CaptureTextureStates(src);
        ResourceStates[] dstPreviousStates = CaptureTextureStates(dst);
        this.TransitionTexture(src, ResourceStates.ResolveSource);
        this.TransitionTexture(dst, ResourceStates.ResolveDest);

        Format resolveFormat = D3D12Formats.ToDxgiFormat(source.Format);
        uint mipLevels = Math.Min(source.MipLevels, destination.MipLevels);
        uint arrayLayers = Math.Min(source.ArrayLayers, destination.ArrayLayers);
        for (uint arrayLayer = 0; arrayLayer < arrayLayers; arrayLayer++) {
            for (uint mipLevel = 0; mipLevel < mipLevels; mipLevel++) {
                uint srcSubresource = source.CalculateSubresource(mipLevel, arrayLayer);
                uint dstSubresource = destination.CalculateSubresource(mipLevel, arrayLayer);
                this.NativeCommandList.ResolveSubresource(dst.NativeTexture, dstSubresource, src.NativeTexture, srcSubresource, resolveFormat);
            }
        }

        this.RestoreTextureStates(src, srcPreviousStates);
        this.RestoreTextureStates(dst, dstPreviousStates);
    }

    /// <summary>
    /// Copies buffer core data between resources.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sourceOffset">The source offset value used by this operation.</param>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="destinationOffset">The destination offset value used by this operation.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.CopyBuffer));
        this.FlushPendingUavBarrier();
        D3D12DeviceBuffer src = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(source);
        D3D12DeviceBuffer dst = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(destination);
        src.CopyTo(this.NativeCommandList, dst, sourceOffset, destinationOffset, sizeInBytes);
    }

    /// <summary>
    /// Copies texture core data between resources.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="srcX">The src x value used by this operation.</param>
    /// <param name="srcY">The src y value used by this operation.</param>
    /// <param name="srcZ">The src z value used by this operation.</param>
    /// <param name="srcMipLevel">The src mip level value used by this operation.</param>
    /// <param name="srcBaseArrayLayer">The src base array layer value used by this operation.</param>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="dstX">The dst x value used by this operation.</param>
    /// <param name="dstY">The dst y value used by this operation.</param>
    /// <param name="dstZ">The dst z value used by this operation.</param>
    /// <param name="dstMipLevel">The dst mip level value used by this operation.</param>
    /// <param name="dstBaseArrayLayer">The dst base array layer value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="layerCount">The layer count value used by this operation.</param>
    protected override void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.CopyTexture));
        this.FlushPendingUavBarrier();
        D3D12Texture src = Util.AssertSubtype<Texture, D3D12Texture>(source);
        D3D12Texture dst = Util.AssertSubtype<Texture, D3D12Texture>(destination);

        if (src.NativeTexture != null && dst.NativeTexture != null) {
            ResourceStates[] srcPreviousStates = CaptureTextureStates(src);
            ResourceStates[] dstPreviousStates = CaptureTextureStates(dst);
            this.TransitionTexture(src, ResourceStates.CopySource);
            this.TransitionTexture(dst, ResourceStates.CopyDest);

            for (uint layer = 0; layer < layerCount; layer++) {
                uint srcSubresource = source.CalculateSubresource(srcMipLevel, srcBaseArrayLayer + layer);
                uint dstSubresource = destination.CalculateSubresource(dstMipLevel, dstBaseArrayLayer + layer);
                TextureCopyLocation srcLocation = new(src.NativeTexture, srcSubresource);
                TextureCopyLocation dstLocation = new(dst.NativeTexture, dstSubresource);
                Box srcBox = new((int)srcX, (int)srcY, (int)srcZ, (int)(srcX + width), (int)(srcY + height), (int)(srcZ + depth));
                this.NativeCommandList.CopyTextureRegion(dstLocation, dstX, dstY, dstZ, srcLocation, srcBox);
            }

            this.RestoreTextureStates(src, srcPreviousStates);
            this.RestoreTextureStates(dst, dstPreviousStates);
            return;
        }

        src.CopyRegionTo(dst, srcX, srcY, srcZ, srcMipLevel, srcBaseArrayLayer, dstX, dstY, dstZ, dstMipLevel, dstBaseArrayLayer, width, height, depth, layerCount);
    }

    /// <summary>
    /// Sets the pipeline core value.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    private protected override void SetPipelineCore(Pipeline pipeline) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.SetPipeline));
        if (pipeline.IsComputePipeline) {
            D3D12Pipeline d3D12ComputePipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline);
            if (ReferenceEquals(this._currentComputePipeline, d3D12ComputePipeline)) {
                return;
            }

            long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
            bool computeRootSignatureChanged = !ReferenceEquals(this._currentComputePipeline?.RootSignature, d3D12ComputePipeline.RootSignature);
            this._currentComputePipeline = d3D12ComputePipeline;
            this._currentGraphicsPipeline = null;

            if (_perfLogEnabled) {
                this._perfPipelineChanges++;
            }

            this.NativeCommandList.SetPipelineState(d3D12ComputePipeline.PipelineState);
            if (computeRootSignatureChanged) {
                ClearBoundResourceSets(this._boundComputeResourceSets);
                ClearChangedResourceSets(this._computeResourceSetsChanged);
                this._computeResourceSetsDirty = false;
                this.InvalidateComputeRootCaches();
                this.NativeCommandList.SetComputeRootSignature(d3D12ComputePipeline.RootSignature);
            }

            if (_perfLogEnabled) {
                this._perfPipelineSetMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            }

            return;
        }

        D3D12Pipeline d3D12Pipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline);
        if (ReferenceEquals(this._currentGraphicsPipeline, d3D12Pipeline)) {
            return;
        }

        long graphicsStartTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        bool rootSignatureChanged = !ReferenceEquals(this._currentGraphicsPipeline?.RootSignature, d3D12Pipeline.RootSignature);
        this._currentGraphicsPipeline = d3D12Pipeline;
        this._currentComputePipeline = null;
        if (_perfLogEnabled) {
            this._perfPipelineChanges++;
        }

        this.NativeCommandList.SetPipelineState(d3D12Pipeline.PipelineState);
        this.SetStencilReference(d3D12Pipeline.StencilReference);
        if (rootSignatureChanged) {
            ClearBoundResourceSets(this._boundGraphicsResourceSets);
            ClearChangedResourceSets(this._graphicsResourceSetsChanged);
            this._graphicsResourceSetsDirty = false;
            this.InvalidateGraphicsRootCaches();
            this.NativeCommandList.SetGraphicsRootSignature(d3D12Pipeline.RootSignature);
        }

        this.SetPrimitiveTopology(d3D12Pipeline.PrimitiveTopology);
        this.RebindVertexBuffersForCurrentPipeline();
        if (_perfLogEnabled) {
            this._perfPipelineSetMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - graphicsStartTicks);
        }
    }

    /// <summary>
    /// Sets the vertex buffer core value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.SetVertexBuffer));
        if (index >= this._boundVertexBuffers.Length) {
            return;
        }

        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
        bool isDynamicBuffer = (d3D12Buffer.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
        ulong bindVersion = d3D12Buffer.BindVersion;
        if (ReferenceEquals(this._boundVertexBuffers[index], d3D12Buffer)
            && this._boundVertexBufferOffsets[index] == offset
            && (!isDynamicBuffer || this._boundVertexBufferVersions[index] == bindVersion)) {
            return;
        }

        this._boundVertexBuffers[index] = d3D12Buffer;
        this._boundVertexBufferOffsets[index] = offset;
        this._boundVertexBufferVersions[index] = bindVersion;
        if (index + 1 > this._maxBoundVertexBufferSlot) {
            this._maxBoundVertexBufferSlot = index + 1;
        }

        this.BindVertexBuffer(index, d3D12Buffer, offset);
        if (_perfLogEnabled) {
            this._perfVertexBufferBinds++;
        }
    }

    /// <summary>
    /// Sets the index buffer core value.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.SetIndexBuffer));
        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
        bool isDynamicBuffer = (d3D12Buffer.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
        ulong bindVersion = d3D12Buffer.BindVersion;
        if (this._hasBoundIndexBuffer
            && ReferenceEquals(this._boundIndexBuffer, d3D12Buffer)
            && this._boundIndexBufferOffset == offset
            && this._boundIndexFormat == format
            && (!isDynamicBuffer || this._boundIndexBufferVersion == bindVersion)) {
            return;
        }

        this.TransitionBuffer(d3D12Buffer, ResourceStates.IndexBuffer);
        uint viewSize = d3D12Buffer.GetBindableSize(offset);
        IndexBufferView indexView = new(d3D12Buffer.GetGpuVirtualAddress(offset), viewSize, D3D12Formats.ToDxgiFormat(format));
        this.NativeCommandList.IASetIndexBuffer(indexView);
        this._boundIndexBuffer = d3D12Buffer;
        this._boundIndexBufferOffset = offset;
        this._boundIndexBufferVersion = bindVersion;
        this._boundIndexFormat = format;
        this._hasBoundIndexBuffer = true;
        if (_perfLogEnabled) {
            this._perfIndexBufferBinds++;
        }
    }

    /// <summary>
    /// Executes the clear color target core logic for this backend.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="clearColor">The clear color value used by this operation.</param>
    private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.ClearColorTarget));
        this.FlushPendingUavBarrier();
        if (this.Framebuffer is D3D12SwapchainFramebuffer swapchainFramebuffer && swapchainFramebuffer.Swapchain.TryGetCurrentBackBuffer(out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int backBufferIndex, out ResourceStates currentState)) {
            this.Transition(backBuffer, currentState, ResourceStates.RenderTarget);
            swapchainFramebuffer.Swapchain.SetBackBufferState(backBufferIndex, ResourceStates.RenderTarget);
            this._transitionedBackBufferIndex = backBufferIndex;
            if (swapchainFramebuffer.TryGetDepthStencilView(out CpuDescriptorHandle swapchainDsv)) {
                this.NativeCommandList.OMSetRenderTargets(rtv, swapchainDsv);
            }
            else {
                this.NativeCommandList.OMSetRenderTargets(rtv);
            }

            this.NativeCommandList.ClearRenderTargetView(rtv, new Color4(clearColor.R, clearColor.G, clearColor.B, clearColor.A), 0, null);
            return;
        }

        if (this.Framebuffer is D3D12Framebuffer d3D12Framebuffer
            && d3D12Framebuffer.TryGetColorTargetView(index, out CpuDescriptorHandle offscreenRtv)) {
            if (index < d3D12Framebuffer.ColorTargetTextures.Length) {
                D3D12Texture colorTexture = d3D12Framebuffer.ColorTargetTextures[(int)index];
                if (colorTexture != null) {
                    this.TransitionTexture(colorTexture, ResourceStates.RenderTarget);
                }
            }

            this.NativeCommandList.ClearRenderTargetView(offscreenRtv, new Color4(clearColor.R, clearColor.G, clearColor.B, clearColor.A), 0, null);
        }
    }

    /// <summary>
    /// Executes the clear depth stencil core logic for this backend.
    /// </summary>
    /// <param name="depth">The depth value.</param>
    /// <param name="stencil">The stencil value used by this operation.</param>
    private protected override void ClearDepthStencilCore(float depth, byte stencil) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.ClearDepthStencil));
        this.FlushPendingUavBarrier();
        if (this.Framebuffer is D3D12SwapchainFramebuffer swapchainFramebuffer) {
            if (swapchainFramebuffer.DepthTargetTexture == null) {
                return;
            }

            this.TransitionTexture(swapchainFramebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
            if (swapchainFramebuffer.TryGetDepthStencilView(out CpuDescriptorHandle swapchainDsv)) {
                ClearFlags swapchainClearFlags = ClearFlags.Depth;
                if (FormatHelpers.IsStencilFormat(swapchainFramebuffer.DepthTargetTexture.Format)) {
                    swapchainClearFlags |= ClearFlags.Stencil;
                }

                this.NativeCommandList.ClearDepthStencilView(swapchainDsv, swapchainClearFlags, depth, stencil, 0, null!);
            }

            return;
        }

        if (this.Framebuffer is not D3D12Framebuffer d3D12Framebuffer) {
            return;
        }

        if (d3D12Framebuffer.DepthTargetTexture == null) {
            return;
        }

        this.TransitionTexture(d3D12Framebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
        if (d3D12Framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv)) {
            ClearFlags clearFlags = ClearFlags.Depth;
            if (FormatHelpers.IsStencilFormat(d3D12Framebuffer.DepthTargetTexture.Format)) {
                clearFlags |= ClearFlags.Stencil;
            }

            this.NativeCommandList.ClearDepthStencilView(dsv, clearFlags, depth, stencil, 0, null!);
        }
    }

    /// <summary>
    /// Executes the draw core logic for this backend.
    /// </summary>
    /// <param name="vertexCount">The vertex count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="vertexStart">The vertex start value used by this operation.</param>
    /// <param name="instanceStart">The instance start value used by this operation.</param>
    private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.Draw));
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushGraphicsResourceSets();
        this.FlushPendingUavBarrier();
        this.NativeCommandList.DrawInstanced(vertexCount, instanceCount, vertexStart, instanceStart);
        if (_perfLogEnabled) {
            this._perfDrawCalls++;
            this._perfDrawMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Executes the draw indexed core logic for this backend.
    /// </summary>
    /// <param name="indexCount">The index count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="indexStart">The index start value used by this operation.</param>
    /// <param name="vertexOffset">The vertex offset value used by this operation.</param>
    /// <param name="instanceStart">The instance start value used by this operation.</param>
    private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.DrawIndexed));
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushGraphicsResourceSets();
        this.FlushPendingUavBarrier();
        this.NativeCommandList.DrawIndexedInstanced(indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
        if (_perfLogEnabled) {
            this._perfDrawCalls++;
            this._perfDrawMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Updates the buffer core state for this command sequence.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.UpdateBuffer));
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushPendingUavBarrier();
        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
        ulong previousBindVersion = d3D12Buffer.BindVersion;
        D3D12ResourceAllocation temporaryUpload = d3D12Buffer.Update(this.NativeCommandList, source, bufferOffsetInBytes, sizeInBytes);
        if (d3D12Buffer.BindVersion != previousBindVersion) {
            this.MarkResourceSetsReferencingBufferDirty(d3D12Buffer);
        }

        if (temporaryUpload != null) {
            this._pendingSubmissionUploadBuffers.Add(temporaryUpload);
        }

        if (_perfLogEnabled) {
            this._perfUploadRecordMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Executes the generate mipmaps core logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    private protected override void GenerateMipmapsCore(Texture texture) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.GenerateMipmaps));
        D3D12Texture d3D12Texture = Util.AssertSubtype<Texture, D3D12Texture>(texture);
        if (texture.MipLevels <= 1 || d3D12Texture.NativeTexture == null) {
            return;
        }

        if (!this.gd.GetPixelFormatSupport(texture.Format, texture.Type, texture.Usage)) {
            throw new PlatformNotSupportedException("GenerateMipmaps is not supported for this D3D12 texture format/type/usage combination.");
        }

        if (this.CanUseGpuMipmapPath(texture) && this.EnsureGpuMipmapResources()) {
            this.GenerateMipmapsGpu(d3D12Texture);
            return;
        }

        if (!d3D12Texture.GenerateMipmapsCpu()) {
            throw new PlatformNotSupportedException("D3D12 mip generation currently supports only uncompressed color textures.");
        }

        d3D12Texture.UploadGeneratedMipmaps();
    }

    /// <summary>
    /// Executes the push debug group core logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private protected override void PushDebugGroupCore(string name) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.PushDebugGroup));
        if (_perfLogEnabled && this._perfRecordingCommandGaps) {
            this._perfDebugGroupStack.Add(name);
            this._perfLastDebugMarker = name;
        }

        this.WriteDebugMarker(name, true, false);
    }

    /// <summary>
    /// Executes the pop debug group core logic for this backend.
    /// </summary>
    private protected override void PopDebugGroupCore() {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.PopDebugGroup));
        this._endEventMethod?.Invoke(this.NativeCommandList, null);
        if (_perfLogEnabled && this._perfRecordingCommandGaps && this._perfDebugGroupStack.Count > 0) {
            this._perfDebugGroupStack.RemoveAt(this._perfDebugGroupStack.Count - 1);
        }
    }

    /// <summary>
    /// Executes the insert debug marker core logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private protected override void InsertDebugMarkerCore(string name) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.InsertDebugMarker));
        if (_perfLogEnabled && this._perfRecordingCommandGaps) {
            this._perfLastDebugMarker = name;
        }

        this.WriteDebugMarker(name, false, true);
    }

    /// <summary>
    /// Uploads backend-specific push-constant data to the active pipeline.
    /// </summary>
    /// <param name="offset">The byte offset inside the push-constant range.</param>
    /// <param name="data">A pointer to source data.</param>
    /// <param name="sizeInBytes">The number of bytes to upload.</param>
    private protected override unsafe void PushConstantsCore(uint offset, IntPtr data, uint sizeInBytes) {
        using PerfCommandApiScope perfCommandApiScope = this.TrackPerfCommandApi(nameof(this.PushConstants));
        D3D12Pipeline pipeline = this._currentComputePipeline ?? this._currentGraphicsPipeline;
        if (pipeline == null) {
            throw new VeldridException("A Direct3D12 pipeline must be bound before push constants can be set.");
        }

        if ((offset % 4) != 0 || (sizeInBytes % 4) != 0) {
            throw new VeldridException("Direct3D12 push constants require 4-byte aligned offsets and sizes.");
        }

        if (offset + sizeInBytes > pipeline.MaxPushConstantSizeInBytes) {
            throw new VeldridException($"Push constants exceed the backend limit of {pipeline.MaxPushConstantSizeInBytes} bytes.");
        }

        uint dwordOffset = offset / 4;
        uint dwordCount = sizeInBytes / 4;
        if (this._currentComputePipeline != null) {
            this.NativeCommandList.SetComputeRoot32BitConstants(pipeline.PushConstantRootParameterIndex, dwordCount, (void*)data, dwordOffset);
        }
        else {
            this.NativeCommandList.SetGraphicsRoot32BitConstants(pipeline.PushConstantRootParameterIndex, dwordCount, (void*)data, dwordOffset);
        }
    }

    /// <summary>
    /// Executes the transition logic for this backend.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="from">The from value used by this operation.</param>
    /// <param name="to">The to value used by this operation.</param>
    private void Transition(ID3D12Resource resource, ResourceStates from, ResourceStates to) {
        if (from == to) {
            return;
        }

        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        ResourceBarrier barrier = ResourceBarrier.BarrierTransition(resource, from, to);
        this._singleBarrier[0] = barrier;
        this.NativeCommandList.ResourceBarrier(this._singleBarrier);
        if (_perfLogEnabled) {
            this._perfTransitions++;
            this._perfBarrierMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Binds the vertex buffer resources for subsequent commands.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    private void BindVertexBuffer(uint index, D3D12DeviceBuffer buffer, uint offset) {
        this.TransitionBuffer(buffer, ResourceStates.VertexAndConstantBuffer);

        uint stride = 0;
        if (this._currentGraphicsPipeline != null && index < this._currentGraphicsPipeline.VertexStrides.Length) {
            stride = this._currentGraphicsPipeline.VertexStrides[index];
        }

        uint viewSize = buffer.GetBindableSize(offset);
        VertexBufferView view = new(buffer.GetGpuVirtualAddress(offset), viewSize, stride);
        this.NativeCommandList.IASetVertexBuffers(index, view);
        this._boundVertexBufferStrides[index] = stride;
    }

    /// <summary>
    /// Executes the rebind vertex buffers for current pipeline logic for this backend.
    /// </summary>
    private void RebindVertexBuffersForCurrentPipeline() {
        if (this._currentGraphicsPipeline == null) {
            return;
        }

        for (uint index = 0; index < this._maxBoundVertexBufferSlot; index++) {
            D3D12DeviceBuffer buffer = this._boundVertexBuffers[index];
            if (buffer == null) {
                continue;
            }

            uint stride = index < this._currentGraphicsPipeline.VertexStrides.Length ? this._currentGraphicsPipeline.VertexStrides[index] : 0;
            if (this._boundVertexBufferStrides[index] == stride) {
                continue;
            }

            this.BindVertexBuffer(index, buffer, this._boundVertexBufferOffsets[index]);
        }
    }

    /// <summary>
    /// Records primitive topology only when the requested topology differs from command-list state.
    /// </summary>
    /// <param name="topology">The primitive topology required by the active graphics pipeline.</param>
    private void SetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology topology) {
        if (this._currentPrimitiveTopologyValid && this._currentPrimitiveTopology == topology) {
            return;
        }

        this.NativeCommandList.IASetPrimitiveTopology(topology);
        this._currentPrimitiveTopology = topology;
        this._currentPrimitiveTopologyValid = true;
    }

    /// <summary>
    /// Sets the output-merger stencil reference when needed.
    /// </summary>
    /// <param name="stencilReference">The stencil reference value.</param>
    private void SetStencilReference(uint stencilReference) {
        if (this._currentStencilReferenceValid && this._currentStencilReference == stencilReference) {
            return;
        }

        this.NativeCommandList.OMSetStencilRef(stencilReference);
        this._currentStencilReference = stencilReference;
        this._currentStencilReferenceValid = true;
    }

    /// <summary>
    /// Executes the transition subresource logic for this backend.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="from">The from value used by this operation.</param>
    /// <param name="to">The to value used by this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    private void TransitionSubresource(ID3D12Resource resource, ResourceStates from, ResourceStates to, uint subresource) {
        if (from == to) {
            return;
        }

        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        ResourceBarrier barrier = ResourceBarrier.BarrierTransition(resource, from, to, subresource);
        this._singleBarrier[0] = barrier;
        this.NativeCommandList.ResourceBarrier(this._singleBarrier);
        if (_perfLogEnabled) {
            this._perfSubresourceTransitions++;
            this._perfBarrierMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Executes the flush pending uav barrier logic for this backend.
    /// </summary>
    private void FlushPendingUavBarrier() {
        if (!this._uavBarrierPending) {
            return;
        }

        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        ResourceBarrier barrier = ResourceBarrier.BarrierUnorderedAccessView(null);
        this._singleBarrier[0] = barrier;
        this.NativeCommandList.ResourceBarrier(this._singleBarrier);
        if (_perfLogEnabled) {
            this._perfUavBarriers++;
            this._perfBarrierMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }

        this._uavBarrierPending = false;
    }

    /// <summary>
    /// Executes the wait for frame slot logic for this backend.
    /// </summary>
    /// <param name="frameSlot">The frame slot value used by this operation.</param>
    private void WaitForFrameSlot(int frameSlot) {
        ulong fenceValue = this._frameSlotFenceValues[frameSlot];
        if (fenceValue == 0) {
            return;
        }

        if (this.gd.IsSubmissionFenceComplete(fenceValue)) {
            return;
        }

        long startTicks = 0;
        if (_perfLogEnabled) {
            startTicks = Stopwatch.GetTimestamp();
        }

        this.gd.WaitForSubmissionFence(fenceValue);
        if (_perfLogEnabled) {
            long elapsedTicks = Stopwatch.GetTimestamp() - startTicks;
            this._perfBeginWaitMs += TicksToMilliseconds(elapsedTicks);
            this._perfBeginWaitCount++;
        }
    }

    /// <summary>
    /// Executes the execute indirect logic for this backend.
    /// </summary>
    /// <param name="argumentBuffer">The argument buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="drawCount">The draw count value used by this operation.</param>
    /// <param name="stride">The stride value used by this operation.</param>
    /// <param name="argumentSize">The argument size value used by this operation.</param>
    /// <param name="signature">The signature value used by this operation.</param>
    private void ExecuteIndirect(D3D12DeviceBuffer argumentBuffer, uint offset, uint drawCount, uint stride, uint argumentSize, ID3D12CommandSignature signature) {
        if (drawCount == 0) {
            return;
        }

        this.FlushPendingUavBarrier();
        ResourceStates previousState = argumentBuffer.CurrentState;
        this.TransitionBuffer(argumentBuffer, ResourceStates.IndirectArgument);

        if (stride == argumentSize) {
            this.NativeCommandList.ExecuteIndirect(signature, drawCount, argumentBuffer.NativeBuffer, offset, null, 0);
            this.TransitionBuffer(argumentBuffer, previousState);
            return;
        }

        for (uint i = 0; i < drawCount; i++) {
            ulong commandOffset = offset + (ulong)i * stride;
            this.NativeCommandList.ExecuteIndirect(signature, 1, argumentBuffer.NativeBuffer, commandOffset, null, 0);
        }

        this.TransitionBuffer(argumentBuffer, previousState);
        this._uavBarrierPending = true;
    }

    /// <summary>
    /// Executes the ensure indirect command signatures logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool EnsureIndirectCommandSignatures() {
        if (this._indirectSignaturesInitialized) {
            return this._indirectSignaturesAvailable;
        }

        this._indirectSignaturesInitialized = true;
        try {
            IndirectArgumentDescription drawArgument = default;
            drawArgument.Type = IndirectArgumentType.Draw;
            CommandSignatureDescription drawDescription = default;
            drawDescription.ByteStride = Unsafe.SizeOf<IndirectDrawArguments>();
            drawDescription.IndirectArguments = [drawArgument];
            this._drawIndirectSignature = this.CreateCommandSignature(drawDescription);

            IndirectArgumentDescription drawIndexedArgument = default;
            drawIndexedArgument.Type = IndirectArgumentType.DrawIndexed;
            CommandSignatureDescription drawIndexedDescription = default;
            drawIndexedDescription.ByteStride = Unsafe.SizeOf<IndirectDrawIndexedArguments>();
            drawIndexedDescription.IndirectArguments = [drawIndexedArgument];
            this._drawIndexedIndirectSignature = this.CreateCommandSignature(drawIndexedDescription);

            IndirectArgumentDescription dispatchArgument = default;
            dispatchArgument.Type = IndirectArgumentType.Dispatch;
            CommandSignatureDescription dispatchDescription = default;
            dispatchDescription.ByteStride = Unsafe.SizeOf<IndirectDispatchArguments>();
            dispatchDescription.IndirectArguments = [dispatchArgument];
            this._dispatchIndirectSignature = this.CreateCommandSignature(dispatchDescription);

            this._indirectSignaturesAvailable = this._drawIndirectSignature != null
                                                && this._drawIndexedIndirectSignature != null
                                                && this._dispatchIndirectSignature != null;
        }
        catch {
            this._indirectSignaturesAvailable = false;
        }

        return this._indirectSignaturesAvailable;
    }

    /// <summary>
    /// Creates the command signature instance used by this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private ID3D12CommandSignature CreateCommandSignature(CommandSignatureDescription description) {
        ID3D12CommandSignature signature = this.gd.Device.CreateCommandSignature<ID3D12CommandSignature>(description, null);

        if (signature == null) {
            throw new VeldridException("Unable to create D3D12 command signature.");
        }

        return signature;
    }

    /// <summary>
    /// Executes the can use gpu mipmap path logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool CanUseGpuMipmapPath(Texture texture) {
        return texture.Type == TextureType.Texture2D
               && texture.SampleCount == TextureSampleCount.Count1
               && (texture.Usage & TextureUsage.Cubemap) == 0
               && (texture.Usage & (TextureUsage.Sampled | TextureUsage.Storage)) == (TextureUsage.Sampled | TextureUsage.Storage)
               && this.gd.GetPixelFormatSupport(texture.Format, texture.Type, TextureUsage.Sampled | TextureUsage.Storage)
               && !FormatHelpers.IsCompressedFormat(texture.Format)
               && (texture.Usage & TextureUsage.DepthStencil) == 0;
    }

    /// <summary>
    /// Executes the generate mipmaps gpu logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    private void GenerateMipmapsGpu(D3D12Texture texture) {
        D3D12Pipeline previousGraphics = this._currentGraphicsPipeline;
        D3D12Pipeline previousCompute = this._currentComputePipeline;

        this.NativeCommandList.SetPipelineState(this._gpuMipPipeline.PipelineState);
        this.NativeCommandList.SetComputeRootSignature(this._gpuMipPipeline.RootSignature);
        this._currentGraphicsPipeline = null;
        this._currentComputePipeline = this._gpuMipPipeline;

        uint layerCount = texture.ArrayLayers;
        uint subresourceCount = texture.MipLevels * layerCount;
        ResourceStates[] previousStates = CaptureTextureStates(texture);
        ResourceStates[] subresourceStates = new ResourceStates[subresourceCount];
        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            subresourceStates[subresource] = previousStates[subresource];
        }

        try {
            for (uint layer = 0; layer < layerCount; layer++) {
                for (uint mipLevel = 1; mipLevel < texture.MipLevels; mipLevel++) {
                    uint srcSubresource = texture.CalculateSubresource(mipLevel - 1, layer);
                    uint dstSubresource = texture.CalculateSubresource(mipLevel, layer);

                    if (subresourceStates[srcSubresource] != ResourceStates.NonPixelShaderResource) {
                        this.TransitionSubresource(texture.NativeTexture, subresourceStates[srcSubresource], ResourceStates.NonPixelShaderResource, srcSubresource);
                        subresourceStates[srcSubresource] = ResourceStates.NonPixelShaderResource;
                    }

                    if (subresourceStates[dstSubresource] != ResourceStates.UnorderedAccess) {
                        this.TransitionSubresource(texture.NativeTexture, subresourceStates[dstSubresource], ResourceStates.UnorderedAccess, dstSubresource);
                        subresourceStates[dstSubresource] = ResourceStates.UnorderedAccess;
                    }

                    using TextureView srcView = this.gd.ResourceFactory.CreateTextureView(new TextureViewDescription(texture, mipLevel - 1, 1, layer, 1));
                    using TextureView dstView = this.gd.ResourceFactory.CreateTextureView(new TextureViewDescription(texture, mipLevel, 1, layer, 1));
                    using ResourceSet mipResourceSet = this.gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this._gpuMipResourceLayout, srcView, dstView, this._gpuMipSampler));

                    this.SetComputeResourceSet(0, mipResourceSet);
                    Util.GetMipDimensions(texture, mipLevel, out uint mipWidth, out uint mipHeight, out _);
                    uint groupCountX = (mipWidth + 7) / 8;
                    uint groupCountY = (mipHeight + 7) / 8;
                    this.FlushComputeResourceSets();
                    this.NativeCommandList.Dispatch(groupCountX, groupCountY, 1);
                }
            }

            for (uint subresource = 0; subresource < subresourceCount; subresource++) {
                if (subresourceStates[subresource] != previousStates[subresource]) {
                    this.TransitionSubresource(texture.NativeTexture, subresourceStates[subresource], previousStates[subresource], subresource);
                    subresourceStates[subresource] = previousStates[subresource];
                }
            }

            for (uint subresource = 0; subresource < subresourceCount; subresource++) {
                texture.SetSubresourceState(subresource, subresourceStates[subresource]);
            }
        }
        finally {
            if (previousCompute != null) {
                this.NativeCommandList.SetPipelineState(previousCompute.PipelineState);
                this.NativeCommandList.SetComputeRootSignature(previousCompute.RootSignature);
                this._currentComputePipeline = previousCompute;
                this._currentGraphicsPipeline = null;
                this.InvalidateComputeRootCaches();
                MarkChangedResourceSets(this._computeResourceSetsChanged);
                this._computeResourceSetsDirty = this._computeResourceSetsChanged.Length > 0;
            }
            else if (previousGraphics != null) {
                this.NativeCommandList.SetPipelineState(previousGraphics.PipelineState);
                this.NativeCommandList.SetGraphicsRootSignature(previousGraphics.RootSignature);
                this.SetPrimitiveTopology(previousGraphics.PrimitiveTopology);
                this.SetStencilReference(previousGraphics.StencilReference);
                this._currentGraphicsPipeline = previousGraphics;
                this._currentComputePipeline = null;
                this.InvalidateGraphicsRootCaches();
                MarkChangedResourceSets(this._graphicsResourceSetsChanged);
                this._graphicsResourceSetsDirty = this._graphicsResourceSetsChanged.Length > 0;
            }
            else {
                this._currentComputePipeline = null;
                this._currentGraphicsPipeline = null;
            }
        }
    }

    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Executes the ensure gpu mipmap resources logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool EnsureGpuMipmapResources() {
        if (this._gpuMipResourcesInitialized) {
            return this._gpuMipResourcesAvailable;
        }

        this._gpuMipResourcesInitialized = true;
        try {
            byte[] shaderBytes = CompileComputeShader(_mipmapComputeShaderCode, "cs_main", "cs_5_0");
            using Shader mipShader = this.gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Compute, shaderBytes, "cs_main"));

            ResourceLayoutDescription resourceLayoutDescription = new(new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Compute), new ResourceLayoutElementDescription("DestinationTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute), new ResourceLayoutElementDescription("LinearSampler", ResourceKind.Sampler, ShaderStages.Compute));

            this._gpuMipResourceLayout = this.gd.ResourceFactory.CreateResourceLayout(resourceLayoutDescription);
            SamplerDescription samplerDescription = SamplerDescription.LINEAR;
            samplerDescription.AddressModeU = SamplerAddressMode.Clamp;
            samplerDescription.AddressModeV = SamplerAddressMode.Clamp;
            samplerDescription.AddressModeW = SamplerAddressMode.Clamp;
            this._gpuMipSampler = this.gd.ResourceFactory.CreateSampler(samplerDescription);

            ComputePipelineDescription computePipelineDescription = new(mipShader, [this._gpuMipResourceLayout], 8, 8, 1);

            this._gpuMipPipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(this.gd.ResourceFactory.CreateComputePipeline(computePipelineDescription));
            this._gpuMipResourcesAvailable = true;
        }
        catch {
            this._gpuMipResourcesAvailable = false;
        }

        return this._gpuMipResourcesAvailable;
    }

    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Executes the compile compute shader logic for this backend.
    /// </summary>
    /// <param name="sourceCode">The source code value used by this operation.</param>
    /// <param name="entryPoint">The entry point value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static byte[] CompileComputeShader(string sourceCode, string entryPoint, string target) {
        byte[] sourceBytes = Encoding.UTF8.GetBytes(sourceCode);

        int result = D3DCompile(sourceBytes, (nuint)sourceBytes.Length, null, IntPtr.Zero, IntPtr.Zero, entryPoint, target, 0, 0, out IntPtr codeBlobPtr, out IntPtr errorBlobPtr);

        string errorMessage = null;

        if (errorBlobPtr != IntPtr.Zero) {
            try {
                ID3DBlob errorBlob = (ID3DBlob)Marshal.GetObjectForIUnknown(errorBlobPtr);
                IntPtr errorPtr = errorBlob.GetBufferPointer();
                int errorSize = checked((int)errorBlob.GetBufferSize());
                if (errorSize > 0) {
                    byte[] errorBytes = new byte[errorSize];
                    Marshal.Copy(errorPtr, errorBytes, 0, errorSize);
                    errorMessage = Encoding.UTF8.GetString(errorBytes).TrimEnd('\0', '\r', '\n');
                }
            }
            finally {
                Marshal.Release(errorBlobPtr);
            }
        }

        if (result < 0 || codeBlobPtr == IntPtr.Zero) {
            throw new VeldridException($"Failed to compile D3D12 mipmap compute shader. {errorMessage}");
        }

        try {
            ID3DBlob codeBlob = (ID3DBlob)Marshal.GetObjectForIUnknown(codeBlobPtr);
            IntPtr codePtr = codeBlob.GetBufferPointer();
            int codeSize = checked((int)codeBlob.GetBufferSize());
            byte[] shaderBytes = new byte[codeSize];
            Marshal.Copy(codePtr, shaderBytes, 0, codeSize);
            return shaderBytes;
        }
        finally {
            Marshal.Release(codeBlobPtr);
        }
    }

    /// <summary>
    /// Executes the transition texture logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="toState">The to state value used by this operation.</param>
    private void TransitionTexture(D3D12Texture texture, ResourceStates toState) {
        if (texture.NativeTexture == null) {
            return;
        }

        if (texture.TryGetCommonState(out ResourceStates commonState)) {
            if (commonState == toState) {
                return;
            }

            this.Transition(texture.NativeTexture, commonState, toState);
            texture.SetAllSubresourceStates(toState);
            return;
        }

        uint subresourceCount = texture.SubresourceCount;
        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            ResourceStates fromState = texture.GetSubresourceState(subresource);
            if (fromState == toState) {
                continue;
            }

            this.TransitionSubresource(texture.NativeTexture, fromState, toState, subresource);
            texture.SetSubresourceState(subresource, toState);
        }
    }

    /// <summary>
    /// Executes the transition texture view logic for this backend.
    /// </summary>
    /// <param name="textureView">The texture view value used by this operation.</param>
    /// <param name="toState">The to state value used by this operation.</param>
    private void TransitionTextureView(D3D12TextureView textureView, ResourceStates toState) {
        D3D12Texture texture = textureView.TargetTexture;
        if (texture.NativeTexture == null) {
            return;
        }

        uint mipStart = textureView.BaseMipLevel;
        uint mipEnd = mipStart + textureView.MipLevels;
        uint layerStart = textureView.BaseArrayLayer;
        uint layerEnd = layerStart + textureView.ArrayLayers;
        if ((texture.Usage & TextureUsage.Cubemap) != 0) {
            layerStart *= 6;
            layerEnd *= 6;
        }

        bool fullResourceView = mipStart == 0
            && mipEnd == texture.MipLevels
            && layerStart == 0
            && layerEnd == texture.EffectiveArrayLayers;

        if (fullResourceView) {
            this.TransitionTexture(texture, toState);
            return;
        }

        for (uint layer = layerStart; layer < layerEnd; layer++) {
            for (uint mip = mipStart; mip < mipEnd; mip++) {
                uint subresource = texture.CalculateSubresource(mip, layer);
                ResourceStates fromState = texture.GetSubresourceState(subresource);
                if (fromState == toState) {
                    continue;
                }

                this.TransitionSubresource(texture.NativeTexture, fromState, toState, subresource);
                texture.SetSubresourceState(subresource, toState);
            }
        }
    }

    /// <summary>
    /// Executes the transition buffer logic for this backend.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="toState">The to state value used by this operation.</param>
    private void TransitionBuffer(D3D12DeviceBuffer buffer, ResourceStates toState) {
        if (!buffer.CanTransitionState || buffer.CurrentState == toState) {
            return;
        }

        this.Transition(buffer.NativeBuffer, buffer.CurrentState, toState);
        buffer.CurrentState = toState;
    }

    /// <summary>
    /// Executes the capture texture states logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static ResourceStates[] CaptureTextureStates(D3D12Texture texture) {
        uint subresourceCount = texture.SubresourceCount;
        ResourceStates[] states = new ResourceStates[subresourceCount];
        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            states[subresource] = texture.GetSubresourceState(subresource);
        }

        return states;
    }

    /// <summary>
    /// Executes the restore texture states logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="previousStates">The previous states value used by this operation.</param>
    private void RestoreTextureStates(D3D12Texture texture, ResourceStates[] previousStates) {
        if (texture.NativeTexture == null || previousStates == null || previousStates.Length == 0) {
            return;
        }

        uint subresourceCount = Math.Min(texture.SubresourceCount, (uint)previousStates.Length);
        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            ResourceStates current = texture.GetSubresourceState(subresource);
            ResourceStates previous = previousStates[subresource];
            if (current == previous) {
                continue;
            }

            this.TransitionSubresource(texture.NativeTexture, current, previous, subresource);
            texture.SetSubresourceState(subresource, previous);
        }
    }

    /// <summary>
    /// Executes the write debug marker logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    /// <param name="beginEvent">The begin event value used by this operation.</param>
    /// <param name="setMarker">The set marker value used by this operation.</param>
    private unsafe void WriteDebugMarker(string name, bool beginEvent, bool setMarker) {
        if (string.IsNullOrEmpty(name)) {
            return;
        }

        byte[] utf8Bytes = Encoding.UTF8.GetBytes(name);
        fixed (byte* bytesPtr = utf8Bytes) {
            IntPtr dataPtr = (IntPtr)bytesPtr;
            int size = utf8Bytes.Length;

            MethodInfo markerMethod = beginEvent ? this._beginEventMethod : this._setMarkerMethod;
            if (markerMethod == null) {
                return;
            }

            ParameterInfo[] parameters = markerMethod.GetParameters();
            object metadata = parameters[0].ParameterType == typeof(int) ? 0 : 0u;
            object sizeValue = parameters[2].ParameterType == typeof(int) ? size : (uint)size;
            if (beginEvent) {
                markerMethod.Invoke(this.NativeCommandList, new[] { metadata, dataPtr, sizeValue });
            }
            else if (setMarker) {
                markerMethod.Invoke(this.NativeCommandList, new[] { metadata, dataPtr, sizeValue });
            }
        }
    }

    /// <summary>
    /// Gets the debug marker method value.
    /// </summary>
    /// <param name="methodName">The method name value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private MethodInfo GetDebugMarkerMethod(string methodName) {
        MethodInfo[] methods = this.NativeCommandList.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
        for (int i = 0; i < methods.Length; i++) {
            MethodInfo method = methods[i];
            if (method.Name != methodName) {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 3
                && parameters[1].ParameterType == typeof(IntPtr)
                && (parameters[0].ParameterType == typeof(uint) || parameters[0].ParameterType == typeof(int))
                && (parameters[2].ParameterType == typeof(uint) || parameters[2].ParameterType == typeof(int))) {
                return method;
            }
        }

        return null;
    }

    [DllImport("d3dcompiler_47.dll", CharSet = CharSet.Ansi)]

    /// <summary>
    /// Executes the d3 dcompile logic for this backend.
    /// </summary>
    /// <param name="srcData">The src data value used by this operation.</param>
    /// <param name="srcDataSize">The src data size value used by this operation.</param>
    /// <param name="sourceName">The source name value used by this operation.</param>
    /// <param name="defines">The defines value used by this operation.</param>
    /// <param name="include">The include value used by this operation.</param>
    /// <param name="entryPoint">The entry point value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="flags1">The flags1 value used by this operation.</param>
    /// <param name="flags2">The flags2 value used by this operation.</param>
    /// <param name="code">The code value used by this operation.</param>
    /// <param name="errorMsgs">The error msgs value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static extern int D3DCompile(byte[] srcData, nuint srcDataSize, string sourceName, IntPtr defines, IntPtr include, string entryPoint, string target, uint flags1, uint flags2, out IntPtr code, out IntPtr errorMsgs);

    /// <summary>
    /// Binds the graphics resource resources for subsequent commands.
    /// </summary>
    /// <param name="bindingInfo">The binding info value used by this operation.</param>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="dynamicOffset">The dynamic offset value used by this operation.</param>
    private void BindGraphicsResource(D3D12Pipeline.RootBindingInfo bindingInfo, IBindableResource resource, uint dynamicOffset) {

        if (bindingInfo.DescriptorTable) {
            return;
        }

        if (!Util.GetDeviceBuffer(resource, out DeviceBuffer _)) {
            throw new PlatformNotSupportedException("D3D12 ResourceSet currently supports buffer resources only for non-table root bindings.");
        }

        DeviceBufferRange range = Util.GetBufferRange(resource, dynamicOffset);
        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(range.Buffer);
        this.TransitionBuffer(d3D12Buffer, GetGraphicsBufferState(bindingInfo.Kind));
        ulong gpuAddress = d3D12Buffer.GetGpuVirtualAddress(range.Offset);
        if (this.IsSameGraphicsRootBuffer(bindingInfo.RootParameterIndex, gpuAddress)) {
            return;
        }

        switch (bindingInfo.Kind) {
            case ResourceKind.UniformBuffer:
                this.NativeCommandList.SetGraphicsRootConstantBufferView(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadOnly:
                this.NativeCommandList.SetGraphicsRootShaderResourceView(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadWrite:
                this.NativeCommandList.SetGraphicsRootUnorderedAccessView(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.TextureReadOnly: case ResourceKind.TextureReadWrite: case ResourceKind.Sampler: throw new VeldridException("Texture and sampler root bindings must use descriptor tables.");
            default: throw Illegal.Value<ResourceKind>();
        }

        this.SetGraphicsRootBufferCache(bindingInfo.RootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds the compute resource resources for subsequent commands.
    /// </summary>
    /// <param name="bindingInfo">The binding info value used by this operation.</param>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="dynamicOffset">The dynamic offset value used by this operation.</param>
    private void BindComputeResource(D3D12Pipeline.RootBindingInfo bindingInfo, IBindableResource resource, uint dynamicOffset) {
        if (bindingInfo.DescriptorTable) {
            return;
        }

        if (!Util.GetDeviceBuffer(resource, out DeviceBuffer _)) {
            throw new PlatformNotSupportedException("D3D12 ResourceSet currently supports buffer resources only for non-table root bindings.");
        }

        DeviceBufferRange range = Util.GetBufferRange(resource, dynamicOffset);
        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(range.Buffer);
        this.TransitionBuffer(d3D12Buffer, GetComputeBufferState(bindingInfo.Kind));
        ulong gpuAddress = d3D12Buffer.GetGpuVirtualAddress(range.Offset);

        if (this.IsSameComputeRootBuffer(bindingInfo.RootParameterIndex, gpuAddress)) {
            return;
        }

        switch (bindingInfo.Kind) {
            case ResourceKind.UniformBuffer:
                this.NativeCommandList.SetComputeRootConstantBufferView(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadOnly:
                this.NativeCommandList.SetComputeRootShaderResourceView(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadWrite:
                this.NativeCommandList.SetComputeRootUnorderedAccessView(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.TextureReadOnly: case ResourceKind.TextureReadWrite: case ResourceKind.Sampler: throw new VeldridException("Texture and sampler root bindings must use descriptor tables.");
            default: throw Illegal.Value<ResourceKind>();
        }

        this.SetComputeRootBufferCache(bindingInfo.RootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Flushes graphics resource sets that were changed since the previous draw.
    /// </summary>
    private void FlushGraphicsResourceSets() {
        if (!this._graphicsResourceSetsDirty || this._currentGraphicsPipeline == null) {
            return;
        }

        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        int count = Math.Min(this._boundGraphicsResourceSets.Length, this._graphicsResourceSetsChanged.Length);
        for (int slot = 0; slot < count; slot++) {
            if (!this._graphicsResourceSetsChanged[slot]) {
                continue;
            }

            this._graphicsResourceSetsChanged[slot] = false;
            this.BindResourceSet(this._currentGraphicsPipeline, (uint)slot, ref this._boundGraphicsResourceSets[slot], false);
        }

        this._graphicsResourceSetsDirty = false;
        if (_perfLogEnabled) {
            this._perfResourceSetFlushMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Flushes compute resource sets that were changed since the previous dispatch.
    /// </summary>
    private void FlushComputeResourceSets() {
        if (!this._computeResourceSetsDirty || this._currentComputePipeline == null) {
            return;
        }

        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        int count = Math.Min(this._boundComputeResourceSets.Length, this._computeResourceSetsChanged.Length);
        for (int slot = 0; slot < count; slot++) {
            if (!this._computeResourceSetsChanged[slot]) {
                continue;
            }

            this._computeResourceSetsChanged[slot] = false;
            this.BindResourceSet(this._currentComputePipeline, (uint)slot, ref this._boundComputeResourceSets[slot], true);
        }

        this._computeResourceSetsDirty = false;
        if (_perfLogEnabled) {
            this._perfResourceSetFlushMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Binds a dirty resource set to the active root signature.
    /// </summary>
    /// <param name="pipeline">The active pipeline.</param>
    /// <param name="slot">The resource set slot.</param>
    /// <param name="boundSet">The bound resource set information.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    private void BindResourceSet(D3D12Pipeline pipeline, uint slot, ref BoundResourceSetInfo boundSet, bool compute) {
        if (boundSet.Set == null) {
            return;
        }

        D3D12ResourceSet d3d12Set = Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(boundSet.Set);
        IBindableResource[] resources = d3d12Set.BoundResources;
        ResourceSetBindingPlanEntry[] bindingPlan = compute
            ? this.GetComputeResourceSetBindingPlan(pipeline, slot, d3d12Set.ResourceLayoutInfo)
            : this.GetGraphicsResourceSetBindingPlan(pipeline, slot, d3d12Set.ResourceLayoutInfo);
        uint dynamicOffsetIndex = 0;
        bool descriptorTablesChanged = false;

        for (int i = 0; i < bindingPlan.Length; i++) {
            ref readonly ResourceSetBindingPlanEntry bindingEntry = ref bindingPlan[i];
            uint dynamicOffset = 0;
            if (bindingEntry.IsDynamicBinding) {
                if (dynamicOffsetIndex >= boundSet.Offsets.Count) {
                    throw new VeldridException("Insufficient dynamic offsets provided for ResourceSet binding.");
                }

                dynamicOffset = boundSet.Offsets.Get(dynamicOffsetIndex);
                dynamicOffsetIndex++;
            }

            IBindableResource resource = resources[bindingEntry.ElementIndex];
            if (bindingEntry.BindingInfo.DescriptorTable) {
                this.PrepareDescriptorTableResource(bindingEntry.BindingInfo.Kind, resource, compute);
                descriptorTablesChanged = true;
                continue;
            }

            if (compute) {
                this.BindComputeResource(bindingEntry.BindingInfo, resource, dynamicOffset);
            }
            else {
                this.BindGraphicsResource(bindingEntry.BindingInfo, resource, dynamicOffset);
            }
        }

        if (descriptorTablesChanged) {
            this.BindResourceSetDescriptorTables(d3d12Set, bindingPlan, compute);
        }
    }

    /// <summary>
    /// Marks currently bound resource sets dirty when a dynamic buffer moves to a new native snapshot.
    /// </summary>
    /// <param name="buffer">The buffer whose native binding address changed.</param>
    private void MarkResourceSetsReferencingBufferDirty(D3D12DeviceBuffer buffer) {
        bool graphicsChanged = this.MarkResourceSetsReferencingBufferDirty(this._boundGraphicsResourceSets, this._graphicsResourceSetsChanged, buffer);
        bool computeChanged = this.MarkResourceSetsReferencingBufferDirty(this._boundComputeResourceSets, this._computeResourceSetsChanged, buffer);
        if (graphicsChanged) {
            this._graphicsResourceSetsDirty = true;
            this.InvalidateGraphicsRootCaches();
        }

        if (computeChanged) {
            this._computeResourceSetsDirty = true;
            this.InvalidateComputeRootCaches();
        }
    }

    /// <summary>
    /// Marks the resource sets that reference a specific buffer as dirty.
    /// </summary>
    /// <param name="resourceSets">The bound resource set collection.</param>
    /// <param name="changed">The dirty flag collection.</param>
    /// <param name="buffer">The buffer whose native binding address changed.</param>
    /// <returns><see langword="true" /> when at least one resource set was marked dirty.</returns>
    private bool MarkResourceSetsReferencingBufferDirty(BoundResourceSetInfo[] resourceSets, bool[] changed, D3D12DeviceBuffer buffer) {
        int count = Math.Min(resourceSets.Length, changed.Length);
        bool anyChanged = false;
        for (int slot = 0; slot < count; slot++) {
            if (resourceSets[slot].Set is not D3D12ResourceSet resourceSet) {
                continue;
            }

            if (!ResourceSetReferencesBuffer(resourceSet, buffer)) {
                continue;
            }

            changed[slot] = true;
            anyChanged = true;
        }

        return anyChanged;
    }

    /// <summary>
    /// Checks whether a resource set references a specific buffer.
    /// </summary>
    /// <param name="resourceSet">The resource set to inspect.</param>
    /// <param name="buffer">The buffer to find.</param>
    /// <returns><see langword="true" /> when the resource set references the buffer.</returns>
    private static bool ResourceSetReferencesBuffer(D3D12ResourceSet resourceSet, D3D12DeviceBuffer buffer) {
        IBindableResource[] resources = resourceSet.BoundResources;
        for (int i = 0; i < resources.Length; i++) {
            if (!Util.GetDeviceBuffer(resources[i], out DeviceBuffer boundBuffer)) {
                continue;
            }

            if (ReferenceEquals(boundBuffer, buffer)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Prepares a descriptor-table resource for binding by validating and transitioning it.
    /// </summary>
    /// <param name="kind">The resource kind.</param>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="compute">Whether the resource is being used by a compute pipeline.</param>
    private void PrepareDescriptorTableResource(ResourceKind kind, IBindableResource resource, bool compute) {
        switch (kind) {
            case ResourceKind.TextureReadOnly: {
                    TextureView textureView = Util.GetTextureView(this.gd, resource);
                    D3D12TextureView d3d12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                    d3d12TextureView.EnsureBindingSupport(TextureUsage.Sampled, "sampled");
                    ResourceStates readState = compute ? ResourceStates.NonPixelShaderResource : ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource;
                    this.TransitionTextureView(d3d12TextureView, readState);
                    break;
                }
            case ResourceKind.TextureReadWrite: {
                    TextureView textureView = Util.GetTextureView(this.gd, resource);
                    D3D12TextureView d3D12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                    d3D12TextureView.EnsureBindingSupport(TextureUsage.Storage, "storage");
                    this.TransitionTextureView(d3D12TextureView, ResourceStates.UnorderedAccess);
                    break;
                }
            case ResourceKind.Sampler: break;
            default: throw new VeldridException("Invalid descriptor-table binding kind.");
        }
    }

    /// <summary>
    /// Binds grouped descriptor tables for a resource set.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="bindingPlan">The binding plan for the active pipeline and set slot.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    private void BindResourceSetDescriptorTables(D3D12ResourceSet set, ResourceSetBindingPlanEntry[] bindingPlan, bool compute) {
        this.BindDescriptorHeaps();
        this.BindResourceSetDescriptorTable(set, bindingPlan, compute, D3D12Pipeline.DescriptorTableKind.SrvUav);
        this.BindResourceSetDescriptorTable(set, bindingPlan, compute, D3D12Pipeline.DescriptorTableKind.Sampler);
    }

    /// <summary>
    /// Binds one grouped descriptor table for a resource set.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="bindingPlan">The binding plan for the active pipeline and set slot.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    /// <param name="tableKind">The table kind to bind.</param>
    private void BindResourceSetDescriptorTable(D3D12ResourceSet set, ResourceSetBindingPlanEntry[] bindingPlan, bool compute, D3D12Pipeline.DescriptorTableKind tableKind) {
        uint descriptorCount = 0;
        uint rootParameterIndex = 0;
        bool hasTable = false;
        for (int i = 0; i < bindingPlan.Length; i++) {
            D3D12Pipeline.RootBindingInfo bindingInfo = bindingPlan[i].BindingInfo;
            if (!bindingInfo.DescriptorTable || bindingInfo.DescriptorTableKind != tableKind) {
                continue;
            }

            hasTable = true;
            rootParameterIndex = bindingInfo.RootParameterIndex;
            descriptorCount = Math.Max(descriptorCount, bindingInfo.DescriptorTableOffset + 1);
        }

        if (!hasTable) {
            return;
        }

        if (!this.TryGetDescriptorTableHandle(set, tableKind, out GpuDescriptorHandle gpuHandle)) {
            DescriptorHeapType heapType;
            CpuDescriptorHandle cpuHandle;
            if (tableKind == D3D12Pipeline.DescriptorTableKind.Sampler) {
                heapType = DescriptorHeapType.Sampler;
                this.AllocateSamplerDescriptors(descriptorCount, out cpuHandle, out gpuHandle);
            }
            else {
                heapType = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView;
                this.AllocateSrvUavDescriptors(descriptorCount, out cpuHandle, out gpuHandle);
            }

            long descriptorCopyStartTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
            for (int i = 0; i < bindingPlan.Length; i++) {
                ResourceSetBindingPlanEntry entry = bindingPlan[i];
                D3D12Pipeline.RootBindingInfo bindingInfo = entry.BindingInfo;
                if (!bindingInfo.DescriptorTable || bindingInfo.DescriptorTableKind != tableKind) {
                    continue;
                }

                CpuDescriptorHandle destination = cpuHandle + (int)(bindingInfo.DescriptorTableOffset * (uint)(tableKind == D3D12Pipeline.DescriptorTableKind.Sampler ? this._samplerDescriptorSize : this._srvUavDescriptorSize));
                CpuDescriptorHandle source = this.GetSourceDescriptor(set.BoundResources[entry.ElementIndex], bindingInfo.Kind);
                this.gd.Device.CopyDescriptorsSimple(1u, destination, source, heapType);
            }

            if (_perfLogEnabled) {
                this._perfDescriptorCopyMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - descriptorCopyStartTicks);
            }

            this.CacheDescriptorTableHandle(set, tableKind, gpuHandle);
        }

        if ((compute && this.IsSameComputeRootTable(rootParameterIndex, gpuHandle.Ptr))
            || (!compute && this.IsSameGraphicsRootTable(rootParameterIndex, gpuHandle.Ptr))) {
            return;
        }

        if (compute) {
            this.NativeCommandList.SetComputeRootDescriptorTable(rootParameterIndex, gpuHandle);
            this.SetComputeRootTableCache(rootParameterIndex, gpuHandle.Ptr);
        }
        else {
            this.NativeCommandList.SetGraphicsRootDescriptorTable(rootParameterIndex, gpuHandle);
            this.SetGraphicsRootTableCache(rootParameterIndex, gpuHandle.Ptr);
        }

        if (_perfLogEnabled) {
            this._perfRootTableSets++;
        }
    }

    /// <summary>
    /// Gets the persistent CPU descriptor for a resource set member.
    /// </summary>
    /// <param name="resource">The resource to resolve.</param>
    /// <param name="kind">The resource binding kind.</param>
    /// <returns>The CPU descriptor handle.</returns>
    private CpuDescriptorHandle GetSourceDescriptor(IBindableResource resource, ResourceKind kind) {
        switch (kind) {
            case ResourceKind.TextureReadOnly: {
                    TextureView textureView = Util.GetTextureView(this.gd, resource);
                    D3D12TextureView d3d12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                    return d3d12TextureView.GetOrCreateShaderResourceViewDescriptor();
                }
            case ResourceKind.TextureReadWrite: {
                    TextureView textureView = Util.GetTextureView(this.gd, resource);
                    D3D12TextureView d3d12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                    return d3d12TextureView.GetOrCreateUnorderedAccessViewDescriptor();
                }
            case ResourceKind.Sampler: {
                    D3D12Sampler d3d12Sampler = Util.AssertSubtype<IBindableResource, D3D12Sampler>(resource);
                    return d3d12Sampler.GetOrCreateDescriptor();
                }
            default: throw new VeldridException("Invalid descriptor-table binding kind.");
        }
    }

    /// <summary>
    /// Binds the descriptor heaps resources for subsequent commands.
    /// </summary>
    private void BindDescriptorHeaps() {
        if (this._descriptorHeapsBound) {
            return;
        }

        this._boundDescriptorHeaps[0] = this._shaderVisibleSrvUavHeap;
        this._boundDescriptorHeaps[1] = this._shaderVisibleSamplerHeap;
        this.NativeCommandList.SetDescriptorHeaps(this._boundDescriptorHeaps);
        this._descriptorHeapsBound = true;
    }

    /// <summary>
    /// Executes the allocate srv uav descriptor logic for this backend.
    /// </summary>
    /// <param name="cpuHandle">The cpu handle value used by this operation.</param>
    /// <param name="gpuHandle">The gpu handle value used by this operation.</param>
    private void AllocateSrvUavDescriptors(uint count, out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
        if (this._nextSrvUavDescriptor + count > this._maxSrvUavDescriptors) {
            throw new VeldridException("D3D12 SRV/UAV descriptor heap exhausted for this CommandList recording.");
        }

        CpuDescriptorHandle cpuStart = this._shaderVisibleSrvUavHeap.GetCPUDescriptorHandleForHeapStart();
        GpuDescriptorHandle gpuStart = this._shaderVisibleSrvUavHeap.GetGPUDescriptorHandleForHeapStart();
        cpuHandle = new CpuDescriptorHandle(cpuStart, (int)this._nextSrvUavDescriptor, (uint)this._srvUavDescriptorSize);
        gpuHandle = new GpuDescriptorHandle(gpuStart, (int)this._nextSrvUavDescriptor, (uint)this._srvUavDescriptorSize);
        this._nextSrvUavDescriptor += count;
    }

    /// <summary>
    /// Executes the allocate sampler descriptor logic for this backend.
    /// </summary>
    /// <param name="cpuHandle">The cpu handle value used by this operation.</param>
    /// <param name="gpuHandle">The gpu handle value used by this operation.</param>
    private void AllocateSamplerDescriptors(uint count, out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
        if (this._nextSamplerDescriptor + count > this._maxSamplerDescriptors) {
            throw new VeldridException("D3D12 sampler descriptor heap exhausted for this CommandList recording.");
        }

        CpuDescriptorHandle cpuStart = this._shaderVisibleSamplerHeap.GetCPUDescriptorHandleForHeapStart();
        GpuDescriptorHandle gpuStart = this._shaderVisibleSamplerHeap.GetGPUDescriptorHandleForHeapStart();
        cpuHandle = new CpuDescriptorHandle(cpuStart, (int)this._nextSamplerDescriptor, (uint)this._samplerDescriptorSize);
        gpuHandle = new GpuDescriptorHandle(gpuStart, (int)this._nextSamplerDescriptor, (uint)this._samplerDescriptorSize);
        this._nextSamplerDescriptor += count;
    }

    /// <summary>
    /// Attempts to reuse a shader-visible descriptor table handle for the current frame slot.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="kind">The descriptor-table resource kind.</param>
    /// <param name="handle">The cached GPU descriptor handle, when available.</param>
    /// <returns><see langword="true" /> when a cached handle was found.</returns>
    private bool TryGetDescriptorTableHandle(D3D12ResourceSet set, D3D12Pipeline.DescriptorTableKind kind, out GpuDescriptorHandle handle) {
        DescriptorCacheKey key = new(set, kind);
        return this._descriptorTableCache.TryGetValue(key, out handle);
    }

    /// <summary>
    /// Stores a shader-visible descriptor table handle for reuse on the current frame slot.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="kind">The descriptor-table resource kind.</param>
    /// <param name="handle">The GPU descriptor handle to cache.</param>
    private void CacheDescriptorTableHandle(D3D12ResourceSet set, D3D12Pipeline.DescriptorTableKind kind, GpuDescriptorHandle handle) {
        DescriptorCacheKey key = new(set, kind);
        this._descriptorTableCache.Add(key, handle);
        if (_perfLogEnabled) {
            this._perfDescriptorCopies++;
        }
    }

    /// <summary>
    /// Gets the graphics resource set binding plan value.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="layout">The resource layout used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private ResourceSetBindingPlanEntry[] GetGraphicsResourceSetBindingPlan(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout) {
        ResourceSetBindingPlanKey key = new(pipeline, layout, slot);
        if (this._graphicsResourceSetBindingPlans.TryGetValue(key, out ResourceSetBindingPlanEntry[] existingPlan)) {
            return existingPlan;
        }

        ResourceSetBindingPlanEntry[] createdPlan = CreateGraphicsResourceSetBindingPlan(pipeline, slot, layout.Elements);
        this._graphicsResourceSetBindingPlans.Add(key, createdPlan);
        return createdPlan;
    }

    /// <summary>
    /// Gets the compute resource set binding plan value.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="layout">The resource layout used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private ResourceSetBindingPlanEntry[] GetComputeResourceSetBindingPlan(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout) {
        ResourceSetBindingPlanKey key = new(pipeline, layout, slot);
        if (this._computeResourceSetBindingPlans.TryGetValue(key, out ResourceSetBindingPlanEntry[] existingPlan)) {
            return existingPlan;
        }

        ResourceSetBindingPlanEntry[]
            createdPlan = CreateComputeResourceSetBindingPlan(pipeline, slot, layout.Elements);
        this._computeResourceSetBindingPlans.Add(key, createdPlan);
        return createdPlan;
    }

    /// <summary>
    /// Creates the graphics resource set binding plan instance used by this backend.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="elements">The elements value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static ResourceSetBindingPlanEntry[] CreateGraphicsResourceSetBindingPlan(D3D12Pipeline pipeline, uint slot, ResourceLayoutElementDescription[] elements) {
        List<ResourceSetBindingPlanEntry> plan = new(elements.Length);
        for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
            if (!pipeline.TryGetGraphicsRootBinding(slot, elementIndex, out D3D12Pipeline.RootBindingInfo bindingInfo)) {
                continue;
            }

            bool isDynamicBinding = (elements[elementIndex].Options & ResourceLayoutElementOptions.DynamicBinding) != 0;
            plan.Add(new ResourceSetBindingPlanEntry(elementIndex, bindingInfo, isDynamicBinding));
        }

        return plan.ToArray();
    }

    /// <summary>
    /// Creates the compute resource set binding plan instance used by this backend.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="elements">The elements value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static ResourceSetBindingPlanEntry[] CreateComputeResourceSetBindingPlan(D3D12Pipeline pipeline, uint slot, ResourceLayoutElementDescription[] elements) {
        List<ResourceSetBindingPlanEntry> plan = new(elements.Length);

        for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
            if (!pipeline.TryGetComputeRootBinding(slot, elementIndex, out D3D12Pipeline.RootBindingInfo bindingInfo)) {
                continue;
            }

            bool isDynamicBinding = (elements[elementIndex].Options & ResourceLayoutElementOptions.DynamicBinding) != 0;
            plan.Add(new ResourceSetBindingPlanEntry(elementIndex, bindingInfo, isDynamicBinding));
        }

        return plan.ToArray();
    }

    /// <summary>
    /// Gets the graphics buffer state value.
    /// </summary>
    /// <param name="kind">The kind value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static ResourceStates GetGraphicsBufferState(ResourceKind kind) {
        switch (kind) {
            case ResourceKind.UniformBuffer: return ResourceStates.VertexAndConstantBuffer;
            case ResourceKind.StructuredBufferReadOnly: return ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource;
            case ResourceKind.StructuredBufferReadWrite: return ResourceStates.UnorderedAccess;
            default: return ResourceStates.Common;
        }
    }

    /// <summary>
    /// Gets the compute buffer state value.
    /// </summary>
    /// <param name="kind">The kind value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static ResourceStates GetComputeBufferState(ResourceKind kind) {
        switch (kind) {
            case ResourceKind.UniformBuffer: return ResourceStates.VertexAndConstantBuffer;
            case ResourceKind.StructuredBufferReadOnly: return ResourceStates.NonPixelShaderResource;
            case ResourceKind.StructuredBufferReadWrite: return ResourceStates.UnorderedAccess;
            default: return ResourceStates.Common;
        }
    }

    /// <summary>
    /// Executes the is same graphics root buffer logic for this backend.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index value used by this operation.</param>
    /// <param name="gpuAddress">The gpu address value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool IsSameGraphicsRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        if (index >= this._graphicsRootBufferAddresses.Length) {
            Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddresses, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddressValid, rootParameterIndex + 1);
        }

        return this._graphicsRootBufferAddressValid[index] && this._graphicsRootBufferAddresses[index] == gpuAddress;
    }

    /// <summary>
    /// Sets the graphics root buffer cache value.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index value used by this operation.</param>
    /// <param name="gpuAddress">The gpu address value used by this operation.</param>
    private void SetGraphicsRootBufferCache(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        if (index >= this._graphicsRootBufferAddresses.Length) {
            Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddresses, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddressValid, rootParameterIndex + 1);
        }

        this._graphicsRootBufferAddresses[index] = gpuAddress;
        this._graphicsRootBufferAddressValid[index] = true;
    }

    /// <summary>
    /// Executes the is same compute root buffer logic for this backend.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index value used by this operation.</param>
    /// <param name="gpuAddress">The gpu address value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool IsSameComputeRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        if (index >= this._computeRootBufferAddresses.Length) {
            Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddresses, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddressValid, rootParameterIndex + 1);
        }

        return this._computeRootBufferAddressValid[index] && this._computeRootBufferAddresses[index] == gpuAddress;
    }

    /// <summary>
    /// Sets the compute root buffer cache value.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index value used by this operation.</param>
    /// <param name="gpuAddress">The gpu address value used by this operation.</param>
    private void SetComputeRootBufferCache(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        if (index >= this._computeRootBufferAddresses.Length) {
            Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddresses, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddressValid, rootParameterIndex + 1);
        }

        this._computeRootBufferAddresses[index] = gpuAddress;
        this._computeRootBufferAddressValid[index] = true;
    }

    /// <summary>
    /// Executes the is same graphics root table logic for this backend.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index value used by this operation.</param>
    /// <param name="tablePtr">The table ptr value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool IsSameGraphicsRootTable(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        if (index >= this._graphicsRootTablePointers.Length) {
            Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointers, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointerValid, rootParameterIndex + 1);
        }

        return this._graphicsRootTablePointerValid[index] && this._graphicsRootTablePointers[index] == tablePtr;
    }

    /// <summary>
    /// Sets the graphics root table cache value.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index value used by this operation.</param>
    /// <param name="tablePtr">The table ptr value used by this operation.</param>
    private void SetGraphicsRootTableCache(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        if (index >= this._graphicsRootTablePointers.Length) {
            Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointers, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointerValid, rootParameterIndex + 1);
        }

        this._graphicsRootTablePointers[index] = tablePtr;
        this._graphicsRootTablePointerValid[index] = true;
    }

    /// <summary>
    /// Executes the is same compute root table logic for this backend.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index value used by this operation.</param>
    /// <param name="tablePtr">The table ptr value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool IsSameComputeRootTable(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        if (index >= this._computeRootTablePointers.Length) {
            Util.EnsureArrayMinimumSize(ref this._computeRootTablePointers, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._computeRootTablePointerValid, rootParameterIndex + 1);
        }

        return this._computeRootTablePointerValid[index] && this._computeRootTablePointers[index] == tablePtr;
    }

    /// <summary>
    /// Sets the compute root table cache value.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index value used by this operation.</param>
    /// <param name="tablePtr">The table ptr value used by this operation.</param>
    private void SetComputeRootTableCache(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        if (index >= this._computeRootTablePointers.Length) {
            Util.EnsureArrayMinimumSize(ref this._computeRootTablePointers, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._computeRootTablePointerValid, rootParameterIndex + 1);
        }

        this._computeRootTablePointers[index] = tablePtr;
        this._computeRootTablePointerValid[index] = true;
    }

    /// <summary>
    /// Executes the invalidate graphics root caches logic for this backend.
    /// </summary>
    private void InvalidateGraphicsRootCaches() {
        Array.Clear(this._graphicsRootBufferAddressValid, 0, this._graphicsRootBufferAddressValid.Length);
        Array.Clear(this._graphicsRootTablePointerValid, 0, this._graphicsRootTablePointerValid.Length);
    }

    /// <summary>
    /// Executes the invalidate compute root caches logic for this backend.
    /// </summary>
    private void InvalidateComputeRootCaches() {
        Array.Clear(this._computeRootBufferAddressValid, 0, this._computeRootBufferAddressValid.Length);
        Array.Clear(this._computeRootTablePointerValid, 0, this._computeRootTablePointerValid.Length);
    }

    /// <summary>
    /// Executes the clear bound resource sets logic for this backend.
    /// </summary>
    /// <param name="infos">The infos value used by this operation.</param>
    private static void ClearBoundResourceSets(BoundResourceSetInfo[] infos) {
        if (infos == null) {
            return;
        }

        for (int i = 0; i < infos.Length; i++) {
            infos[i].Offsets.Dispose();
        }

        Util.ClearArray(infos);
    }

    /// <summary>
    /// Clears resource set dirty flags.
    /// </summary>
    /// <param name="changed">The dirty flag array to clear.</param>
    private static void ClearChangedResourceSets(bool[] changed) {
        if (changed == null || changed.Length == 0) {
            return;
        }

        Array.Clear(changed, 0, changed.Length);
    }

    /// <summary>
    /// Marks currently tracked resource sets as dirty after a compatible pipeline change.
    /// </summary>
    /// <param name="changed">The dirty flag array to update.</param>
    private static void MarkChangedResourceSets(bool[] changed) {
        if (changed == null) {
            return;
        }

        for (int i = 0; i < changed.Length; i++) {
            changed[i] = true;
        }
    }

    /// <summary>
    /// Executes the transition swapchain back buffers to present logic for this backend.
    /// </summary>
    private void TransitionSwapchainBackBuffersToPresent() {
        if (this.Framebuffer is not D3D12SwapchainFramebuffer swapchainFramebuffer) {
            return;
        }

        if (this._transitionedBackBufferIndex < 0) {
            return;
        }

        if (swapchainFramebuffer.Swapchain.TryGetCurrentBackBuffer(out ID3D12Resource backBuffer, out _, out int currentIndex, out ResourceStates state) && currentIndex == this._transitionedBackBufferIndex) {
            this.Transition(backBuffer, state, ResourceStates.Present);
            swapchainFramebuffer.Swapchain.SetBackBufferState(currentIndex, ResourceStates.Present);
        }
    }

    /// <summary>
    /// Disposes upload resources that were recorded but not submitted.
    /// </summary>
    private void DisposePendingSubmissionDisposals() {
        if (this._pendingSubmissionUploadBuffers.Count == 0) {
            return;
        }

        for (int i = 0; i < this._pendingSubmissionUploadBuffers.Count; i++) {
            this.gd.ReturnUploadBuffer(this._pendingSubmissionUploadBuffers[i]);
        }

        this._pendingSubmissionUploadBuffers.Clear();
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("8BA5FB08-5195-40E2-AC58-0D989C3A0102")]

    /// <summary>
    /// Defines the ID3DBlob interface.
    /// </summary>
    private interface ID3DBlob {
        [PreserveSig]

        /// <summary>
        /// Gets the buffer pointer value.
        /// </summary>
        /// <returns>The value produced by this operation.</returns>
        IntPtr GetBufferPointer();

        [PreserveSig]

        /// <summary>
        /// Gets the buffer size value.
        /// </summary>
        /// <returns>The value produced by this operation.</returns>
        nuint GetBufferSize();
    }

    /// <summary>
    /// Represents the DescriptorCacheKey data structure used by the graphics runtime.
    /// </summary>
    private readonly struct DescriptorCacheKey {

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorCacheKey" /> type.
        /// </summary>
        /// <param name="set">The resource set that owns the grouped descriptor table.</param>
        /// <param name="kind">The kind value used by this operation.</param>
        public DescriptorCacheKey(D3D12ResourceSet set, D3D12Pipeline.DescriptorTableKind kind) {
            this.Set = set;
            this.Kind = kind;
        }

        /// <summary>
        /// Gets or sets Set.
        /// </summary>
        public D3D12ResourceSet Set { get; }

        /// <summary>
        /// Gets or sets Kind.
        /// </summary>
        public D3D12Pipeline.DescriptorTableKind Kind { get; }
    }

    /// <summary>
    /// Represents the ResourceSetBindingPlanKey data structure used by the graphics runtime.
    /// </summary>
    private readonly struct ResourceSetBindingPlanKey {

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSetBindingPlanKey" /> type.
        /// </summary>
        /// <param name="pipeline">The pipeline value used by this operation.</param>
        /// <param name="layout">The resource layout used by this operation.</param>
        /// <param name="slot">The slot value used by this operation.</param>
        public ResourceSetBindingPlanKey(D3D12Pipeline pipeline, D3D12ResourceLayout layout, uint slot) {
            this.Pipeline = pipeline;
            this.Layout = layout;
            this.Slot = slot;
        }

        /// <summary>
        /// Gets or sets Pipeline.
        /// </summary>
        public D3D12Pipeline Pipeline { get; }

        /// <summary>
        /// Gets or sets Layout.
        /// </summary>
        public D3D12ResourceLayout Layout { get; }

        /// <summary>
        /// Gets or sets Slot.
        /// </summary>
        public uint Slot { get; }
    }

    /// <summary>
    /// Represents the ResourceSetBindingPlanEntry data structure used by the graphics runtime.
    /// </summary>
    private readonly struct ResourceSetBindingPlanEntry {

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSetBindingPlanEntry" /> type.
        /// </summary>
        /// <param name="elementIndex">The element index value used by this operation.</param>
        /// <param name="bindingInfo">The binding info value used by this operation.</param>
        /// <param name="isDynamicBinding">The is dynamic binding value used by this operation.</param>
        public ResourceSetBindingPlanEntry(uint elementIndex, D3D12Pipeline.RootBindingInfo bindingInfo, bool isDynamicBinding) {
            this.ElementIndex = elementIndex;
            this.BindingInfo = bindingInfo;
            this.IsDynamicBinding = isDynamicBinding;
        }

        /// <summary>
        /// Gets or sets ElementIndex.
        /// </summary>
        public uint ElementIndex { get; }

        /// <summary>
        /// Gets or sets BindingInfo.
        /// </summary>
        public D3D12Pipeline.RootBindingInfo BindingInfo { get; }

        /// <summary>
        /// Gets or sets IsDynamicBinding.
        /// </summary>
        public bool IsDynamicBinding { get; }
    }

    /// <summary>
    /// Represents the DescriptorCacheKeyComparer type used by the graphics runtime.
    /// </summary>
    private sealed class DescriptorCacheKeyComparer : IEqualityComparer<DescriptorCacheKey> {

        /// <summary>
        /// Stores the instance state used by this instance.
        /// </summary>
        public static readonly DescriptorCacheKeyComparer Instance = new();

        /// <summary>
        /// Determines whether this instance is equal to the specified value.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
        public bool Equals(DescriptorCacheKey x, DescriptorCacheKey y) {
            return x.Kind == y.Kind && ReferenceEquals(x.Set, y.Set);
        }

        /// <summary>
        /// Computes a hash code for this instance.
        /// </summary>
        /// <param name="obj">The object instance to evaluate.</param>
        /// <returns>The value produced by this operation.</returns>
        public int GetHashCode(DescriptorCacheKey obj) {
            return HashCode.Combine((int)obj.Kind, RuntimeHelpers.GetHashCode(obj.Set));
        }
    }

    /// <summary>
    /// Completes command-list API gap tracking when an instrumented API returns.
    /// </summary>
    private readonly struct PerfCommandApiScope : IDisposable {

        /// <summary>
        /// Stores the owning command list.
        /// </summary>
        private readonly D3D12CommandList commandList;

        /// <summary>
        /// Stores the command-list API name.
        /// </summary>
        private readonly string apiName;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerfCommandApiScope"/> struct.
        /// </summary>
        /// <param name="commandList">The owning command list.</param>
        /// <param name="apiName">The command-list API name.</param>
        public PerfCommandApiScope(D3D12CommandList commandList, string apiName) {
            this.commandList = commandList;
            this.apiName = apiName;
        }

        /// <summary>
        /// Completes command-list API gap tracking.
        /// </summary>
        public void Dispose() {
            this.commandList?.CompletePerfCommandApi(this.apiName);
        }
    }

    /// <summary>
    /// Represents the ResourceSetBindingPlanKeyComparer type used by the graphics runtime.
    /// </summary>
    private sealed class ResourceSetBindingPlanKeyComparer : IEqualityComparer<ResourceSetBindingPlanKey> {

        /// <summary>
        /// Stores the instance state used by this instance.
        /// </summary>
        public static readonly ResourceSetBindingPlanKeyComparer Instance = new();

        /// <summary>
        /// Determines whether this instance is equal to the specified value.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
        public bool Equals(ResourceSetBindingPlanKey x, ResourceSetBindingPlanKey y) {
            return x.Slot == y.Slot && ReferenceEquals(x.Pipeline, y.Pipeline) && ReferenceEquals(x.Layout, y.Layout);
        }

        /// <summary>
        /// Computes a hash code for this instance.
        /// </summary>
        /// <param name="obj">The object instance to evaluate.</param>
        /// <returns>The value produced by this operation.</returns>
        public int GetHashCode(ResourceSetBindingPlanKey obj) {
            return HashCode.Combine((int)obj.Slot, RuntimeHelpers.GetHashCode(obj.Pipeline), RuntimeHelpers.GetHashCode(obj.Layout));
        }
    }
}
