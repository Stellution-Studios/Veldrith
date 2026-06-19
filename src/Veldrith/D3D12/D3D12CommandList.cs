using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SharpGen.Runtime;
using Vortice;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

#if !VELDRID_D3D12_PERF
#pragma warning disable CS0162
#endif

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
    #if VELDRID_D3D12_PERF
    private static readonly bool _perfLogEnabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PERF"), "1", StringComparison.Ordinal);
    #else
    private const bool _perfLogEnabled = false;
    #endif

    /// <summary>
    /// Enables stack traces for D3D12 performance gap spikes.
    /// </summary>
    #if VELDRID_D3D12_PERF
    private static readonly bool _perfStackLogEnabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PERF_STACK"), "1", StringComparison.Ordinal);
    #else
    private const bool _perfStackLogEnabled = false;
    #endif

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
    private readonly Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlan> _computeResourceSetBindingPlans = new(ResourceSetBindingPlanKeyComparer.Instance);

    /// <summary>
    /// Per-slot array cache for compute binding plans — avoids dictionary lookup when the pipeline is unchanged.
    /// </summary>
    private ResourceSetBindingPlan[] _computeBindingPlanCache = new ResourceSetBindingPlan[8];

    /// <summary>
    /// The pipeline whose binding plans are stored in <see cref="_computeBindingPlanCache"/>.
    /// </summary>
    private D3D12Pipeline _computeBindingPlanCachePipeline;

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
    private readonly Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlan> _graphicsResourceSetBindingPlans = new(ResourceSetBindingPlanKeyComparer.Instance);

    /// <summary>
    /// Per-slot array cache for graphics binding plans — avoids dictionary lookup when the pipeline is unchanged.
    /// </summary>
    private ResourceSetBindingPlan[] _graphicsBindingPlanCache = new ResourceSetBindingPlan[8];

    /// <summary>
    /// The pipeline whose binding plans are stored in <see cref="_graphicsBindingPlanCache"/>.
    /// </summary>
    private D3D12Pipeline _graphicsBindingPlanCachePipeline;

    /// <summary>
    /// Stores active debug group names for D3D12 performance gap attribution.
    /// </summary>
    private readonly List<string> _perfDebugGroupStack = new();

    /// <summary>
    /// Stores the number of sampler descriptors retained by the persistent shader-visible heap.
    /// </summary>
    private readonly uint _maxSamplerDescriptors = 1536;

    /// <summary>
    /// Stores the number of SRV/UAV descriptors retained by the persistent shader-visible heap.
    /// </summary>
    private readonly uint _maxSrvUavDescriptors = 49152;

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
    /// Pre-cached CPU start handle for the shader-visible SRV/UAV heap.
    /// </summary>
    private CpuDescriptorHandle _srvUavHeapCpuStart;

    /// <summary>
    /// Pre-cached GPU start handle for the shader-visible SRV/UAV heap.
    /// </summary>
    private GpuDescriptorHandle _srvUavHeapGpuStart;

    /// <summary>
    /// Pre-cached CPU start handle for the shader-visible sampler heap.
    /// </summary>
    private CpuDescriptorHandle _samplerHeapCpuStart;

    /// <summary>
    /// Pre-cached GPU start handle for the shader-visible sampler heap.
    /// </summary>
    private GpuDescriptorHandle _samplerHeapGpuStart;

    /// <summary>
    /// Stores the single barrier state used by this instance.
    /// </summary>
    private readonly ResourceBarrier[] _singleBarrier = new ResourceBarrier[1];

    /// <summary>
    /// Reusable barrier batch buffer — barriers are accumulated here and flushed in one batch call.
    /// </summary>
    private readonly ResourceBarrier[] _barrierBatch = new ResourceBarrier[32];

    /// <summary>
    /// Number of barriers currently accumulated in <see cref="_barrierBatch"/> awaiting a batch flush.
    /// </summary>
    private uint _pendingBarrierCount;

    /// <summary>
    /// Pre-allocated state capture buffer for src texture transitions in copy/resolve operations.
    /// </summary>
    private readonly ResourceStates[] _srcCaptureStates = new ResourceStates[128];

    /// <summary>
    /// Pre-allocated state capture buffer for dst texture transitions in copy/resolve operations.
    /// </summary>
    private readonly ResourceStates[] _dstCaptureStates = new ResourceStates[128];

    /// <summary>
    /// Reusable source descriptor handle batch for batched descriptor copies.
    /// </summary>
    private readonly CpuDescriptorHandle[] _descriptorCopySources = new CpuDescriptorHandle[16];

    /// <summary>
    /// Reusable destination descriptor handle batch for batched descriptor copies.
    /// </summary>
    private readonly CpuDescriptorHandle[] _descriptorCopyDests = new CpuDescriptorHandle[16];

    /// <summary>
    /// Reusable per-range size array (all 1s) for batched descriptor copies.
    /// </summary>
    private static readonly uint[] _descriptorCopyRangeSizes = new uint[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

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
    /// Stores the first dirty compute resource set slot, or -1 when none are dirty.
    /// </summary>
    private int _computeResourceSetsChangedStart = -1;

    /// <summary>
    /// Stores the last dirty compute resource set slot, or -1 when none are dirty.
    /// </summary>
    private int _computeResourceSetsChangedEnd = -1;

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
    /// Stores the first dirty graphics resource set slot, or -1 when none are dirty.
    /// </summary>
    private int _graphicsResourceSetsChangedStart = -1;

    /// <summary>
    /// Stores the last dirty graphics resource set slot, or -1 when none are dirty.
    /// </summary>
    private int _graphicsResourceSetsChangedEnd = -1;

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
    private ulong[] _computeRootBufferAddresses = new ulong[32];

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private bool[] _computeRootBufferAddressValid = new bool[32];

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private ulong[] _computeRootTablePointers = new ulong[32];

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private bool[] _computeRootTablePointerValid = new bool[32];

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
    private ulong[] _graphicsRootBufferAddresses = new ulong[32];

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private bool[] _graphicsRootBufferAddressValid = new bool[32];

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private ulong[] _graphicsRootTablePointers = new ulong[32];

    /// <summary>
    /// Executes the empty logic for this backend.
    /// </summary>
    private bool[] _graphicsRootTablePointerValid = new bool[32];

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
    /// Stores the exclusive sampler descriptor limit for the active frame slot.
    /// </summary>
    private uint _samplerDescriptorLimit;

    /// <summary>
    /// Stores the next srv uav descriptor state used by this instance.
    /// </summary>
    private uint _nextSrvUavDescriptor;

    /// <summary>
    /// Stores the exclusive SRV/UAV descriptor limit for the active frame slot.
    /// </summary>
    private uint _srvUavDescriptorLimit;

    /// <summary>
    /// Stores the accumulated CPU time spent recording resource barriers during the current reporting window.
    /// </summary>
    private double _perfAccumBarrierMs;

    /// <summary>
    /// Stores the perf accum begin wait count value used during command execution.
    /// </summary>
    private ulong _perfAccumBeginWaitCount;

    /// <summary>
    /// Stores managed bytes allocated while recording command lists during the current reporting window.
    /// </summary>
    private ulong _perfAccumAllocatedBytes;

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
    /// Stores dynamic snapshot source-copy bytes accumulated during the current reporting window.
    /// </summary>
    private ulong _perfAccumDynamicSnapshotCopyBytes;

    /// <summary>
    /// Stores dynamic snapshot prefix-copy bytes accumulated during the current reporting window.
    /// </summary>
    private ulong _perfAccumDynamicSnapshotPrefixCopyBytes;

    /// <summary>
    /// Stores dynamic snapshot slot rotations accumulated during the current reporting window.
    /// </summary>
    private ulong _perfAccumDynamicSnapshotRotations;

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
    /// Stores resource set dirty slots scanned during the current reporting window.
    /// </summary>
    private ulong _perfAccumResourceSetScanSlots;

    /// <summary>
    /// Stores resource sets rebound during the current reporting window.
    /// </summary>
    private ulong _perfAccumResourceSetBinds;

    /// <summary>
    /// Stores the perf accum root table sets state used by this instance.
    /// </summary>
    private ulong _perfAccumRootTableSets;

    /// <summary>
    /// Stores root buffer view bindings accumulated during the current reporting window.
    /// </summary>
    private ulong _perfAccumRootBufferSets;

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
    /// Stores dynamic snapshot source-copy bytes for the current command list.
    /// </summary>
    private ulong _perfDynamicSnapshotCopyBytes;

    /// <summary>
    /// Stores dynamic snapshot prefix-copy bytes for the current command list.
    /// </summary>
    private ulong _perfDynamicSnapshotPrefixCopyBytes;

    /// <summary>
    /// Stores dynamic snapshot slot rotations for the current command list.
    /// </summary>
    private ulong _perfDynamicSnapshotRotations;

    /// <summary>
    /// Stores the timestamp captured at the beginning of the current command list recording.
    /// </summary>
    private long _perfFrameStartTicks;

    /// <summary>
    /// Stores the managed allocated byte counter captured at the beginning of the current command list recording.
    /// </summary>
    private long _perfFrameStartAllocatedBytes;

    /// <summary>
    /// Stores managed bytes allocated while recording the current command list.
    /// </summary>
    private ulong _perfAllocatedBytes;

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
    /// Stores resource set dirty slots scanned for the current command list.
    /// </summary>
    private ulong _perfResourceSetScanSlots;

    /// <summary>
    /// Stores resource sets rebound for the current command list.
    /// </summary>
    private ulong _perfResourceSetBinds;

    /// <summary>
    /// Stores the perf root table sets state used by this instance.
    /// </summary>
    private ulong _perfRootTableSets;

    /// <summary>
    /// Stores root buffer view bindings for the current command list.
    /// </summary>
    private ulong _perfRootBufferSets;

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
    /// Stores the swapchain framebuffer currently cached for this command list.
    /// </summary>
    private D3D12SwapchainFramebuffer _cachedSwapchainFramebuffer;

    /// <summary>
    /// Stores the cached current swapchain back buffer resource.
    /// </summary>
    private ID3D12Resource _cachedSwapchainBackBuffer;

    /// <summary>
    /// Stores the cached current swapchain render-target view.
    /// </summary>
    private CpuDescriptorHandle _cachedSwapchainRtv;

    /// <summary>
    /// Stores the cached current swapchain back-buffer index.
    /// </summary>
    private int _cachedSwapchainBackBufferIndex = -1;

    /// <summary>
    /// Stores the cached current swapchain back-buffer state.
    /// </summary>
    private ResourceStates _cachedSwapchainBackBufferState;

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
        this._srvUavDescriptorLimit = this._maxSrvUavDescriptors;
        this._samplerDescriptorLimit = this._maxSamplerDescriptors;
        this._srvUavHeapCpuStart = this._shaderVisibleSrvUavHeap.GetCPUDescriptorHandleForHeapStart();
        this._srvUavHeapGpuStart = this._shaderVisibleSrvUavHeap.GetGPUDescriptorHandleForHeapStart();
        this._samplerHeapCpuStart = this._shaderVisibleSamplerHeap.GetCPUDescriptorHandleForHeapStart();
        this._samplerHeapGpuStart = this._shaderVisibleSamplerHeap.GetGPUDescriptorHandleForHeapStart();
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
        if (this._disposed) {
            return;
        }

        this.WaitForSubmittedFrameSlots();
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
            this._perfFrameStartAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
            this._perfGc0Start = GC.CollectionCount(0);
            this._perfGc1Start = GC.CollectionCount(1);
            this._perfGc2Start = GC.CollectionCount(2);
            this._perfAllocatedBytes = 0;
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
            this._perfResourceSetScanSlots = 0;
            this._perfResourceSetBinds = 0;
            this._perfDescriptorCopies = 0;
            this._perfRootTableSets = 0;
            this._perfRootBufferSets = 0;
            this._perfVertexBufferBinds = 0;
            this._perfIndexBufferBinds = 0;
            this._perfDynamicSnapshotCopyBytes = 0;
            this._perfDynamicSnapshotPrefixCopyBytes = 0;
            this._perfDynamicSnapshotRotations = 0;
            this._perfDrawCalls = 0;
            this._perfDispatchCalls = 0;
        }

        this._currentFrameSlot = (this._currentFrameSlot + 1) % FramesInFlight;
        this.WaitForFrameSlot(this._currentFrameSlot);
        ResetCommandAllocatorNoAlloc(this._commandAllocators[this._currentFrameSlot]);
        this.ResetCommandListNoAlloc(this._commandAllocators[this._currentFrameSlot]);
        this.UpdateDescriptorAllocatorLimits();
        this._pendingBarrierCount = 0;
        this._begun = true;
        this._ended = false;
        this._transitionedBackBufferIndex = -1;
        this.ClearCachedSwapchainBackBuffer();
        this._descriptorHeapsBound = false;
        this._activeViewportCount = 0;
        this._activeScissorRectCount = 0;
        this._uavBarrierPending = false;
        this.ClearBoundVertexBuffers();
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
        this.ResetGraphicsResourceSetChangeRange();
        this.ResetComputeResourceSetChangeRange();
        this.InvalidateGraphicsRootCaches();
        this.InvalidateComputeRootCaches();
        this._maxBoundVertexBufferSlot = 0;
        this._currentPrimitiveTopologyValid = false;
        this._currentStencilReferenceValid = false;
        this.ClearCachedState();
        this._currentGraphicsPipeline = null;
        this._currentComputePipeline = null;
        this._graphicsBindingPlanCachePipeline = null;
        this._computeBindingPlanCachePipeline = null;

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

        this.FlushPendingUavBarrier();
        this.TransitionSwapchainBackBuffersToPresent();
        this.FlushPendingBarriers();
        this.CloseNoAlloc();
        this._ended = true;

        if (_perfLogEnabled) {
            double recordMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - this._perfFrameStartTicks);
            long allocatedDelta = GC.GetAllocatedBytesForCurrentThread() - this._perfFrameStartAllocatedBytes;
            this._perfAllocatedBytes = (ulong)Math.Max(allocatedDelta, 0);
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
            this._perfAccumAllocatedBytes += this._perfAllocatedBytes;
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
            this._perfAccumResourceSetScanSlots += this._perfResourceSetScanSlots;
            this._perfAccumResourceSetBinds += this._perfResourceSetBinds;
            this._perfAccumDescriptorCopies += this._perfDescriptorCopies;
            this._perfAccumRootTableSets += this._perfRootTableSets;
            this._perfAccumRootBufferSets += this._perfRootBufferSets;
            this._perfAccumVertexBufferBinds += this._perfVertexBufferBinds;
            this._perfAccumIndexBufferBinds += this._perfIndexBufferBinds;
            this._perfAccumDynamicSnapshotCopyBytes += this._perfDynamicSnapshotCopyBytes;
            this._perfAccumDynamicSnapshotPrefixCopyBytes += this._perfDynamicSnapshotPrefixCopyBytes;
            this._perfAccumDynamicSnapshotRotations += this._perfDynamicSnapshotRotations;
            this._perfAccumDrawCalls += this._perfDrawCalls;
            this._perfAccumDispatchCalls += this._perfDispatchCalls;
            this.AccumulatePerfCommandGapReport();

            if (untrackedMs >= PerfRecordSpikeThresholdMs) {
                Console.WriteLine($"[D3D12 PERF SPIKE] recordMs={recordMs:F3}, trackedMs={trackedMs:F3}, untrackedMs={untrackedMs:F3}, " + $"wait={this._perfBeginWaitMs:F3}, pso={this._perfPipelineSetMs:F3}, rs={this._perfResourceSetFlushMs:F3}, barrier={this._perfBarrierMs:F3}, " + $"upload={this._perfUploadRecordMs:F3}, draw={this._perfDrawMs:F3}, dispatch={this._perfDispatchMs:F3}, " + $"allocKB={this._perfAllocatedBytes / 1024.0:F1}, gc={Math.Max(gc0Delta, 0)}/{Math.Max(gc1Delta, 0)}/{Math.Max(gc2Delta, 0)}, psoCount={this._perfPipelineChanges}, rsCount={this._perfResourceSetChanges}, drawCount={this._perfDrawCalls}");
                Console.WriteLine($"[D3D12 PERF UPLOAD] dynCopyKB={this._perfDynamicSnapshotCopyBytes / 1024.0:F1}, dynPrefixKB={this._perfDynamicSnapshotPrefixCopyBytes / 1024.0:F1}, dynRot={this._perfDynamicSnapshotRotations}, vb={this._perfVertexBufferBinds}, ib={this._perfIndexBufferBinds}");
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
                Console.WriteLine($"[D3D12 PERF] {PerfReportIntervalFrames}f/{reportWindowMs:F0}ms avg: " + $"wait={this._perfAccumBeginWaitMs * invFrames:F3}ms ({this._perfAccumBeginWaitCount * invFrames:F2}x), " + $"psoMs={this._perfAccumPipelineSetMs * invFrames:F3}, rsMs={this._perfAccumResourceSetFlushMs * invFrames:F3}, " + $"barrierMs={this._perfAccumBarrierMs * invFrames:F3}, descCopyMs={this._perfAccumDescriptorCopyMs * invFrames:F3}, uploadMs={this._perfAccumUploadRecordMs * invFrames:F3}, " + $"drawMs={this._perfAccumDrawMs * invFrames:F3}, dispatchMs={this._perfAccumDispatchMs * invFrames:F3}, " + $"maxRecordMs={this._perfMaxRecordMs:F3}, maxUntrackedMs={this._perfMaxUntrackedRecordMs:F3}, maxWaitMs={this._perfMaxBeginWaitMs:F3}, maxPsoMs={this._perfMaxPipelineSetMs:F3}, maxRsMs={this._perfMaxResourceSetFlushMs:F3}, " + $"maxBarrierMs={this._perfMaxBarrierMs:F3}, maxUploadMs={this._perfMaxUploadRecordMs:F3}, maxDrawMs={this._perfMaxDrawMs:F3}, " + $"allocKB={this._perfAccumAllocatedBytes * invFrames / 1024.0:F1}, gc={this._perfAccumGc0Collections}/{this._perfAccumGc1Collections}/{this._perfAccumGc2Collections}, " + $"trans={this._perfAccumTransitions * invFrames:F1}, subTrans={this._perfAccumSubresourceTransitions * invFrames:F1}, uavB={this._perfAccumUavBarriers * invFrames:F1}, " + $"pso={this._perfAccumPipelineChanges * invFrames:F1}, rs={this._perfAccumResourceSetChanges * invFrames:F1}, rsScan={this._perfAccumResourceSetScanSlots * invFrames:F1}, rsBind={this._perfAccumResourceSetBinds * invFrames:F1}, " + $"descCopy={this._perfAccumDescriptorCopies * invFrames:F1}, rootTbl={this._perfAccumRootTableSets * invFrames:F1}, rootBuf={this._perfAccumRootBufferSets * invFrames:F1}, " + $"vb={this._perfAccumVertexBufferBinds * invFrames:F1}, ib={this._perfAccumIndexBufferBinds * invFrames:F1}, " + $"dynCopyKB={this._perfAccumDynamicSnapshotCopyBytes * invFrames / 1024.0:F1}, dynPrefixKB={this._perfAccumDynamicSnapshotPrefixCopyBytes * invFrames / 1024.0:F1}, dynRot={this._perfAccumDynamicSnapshotRotations * invFrames:F1}, " + $"draw={this._perfAccumDrawCalls * invFrames:F1}, dispatch={this._perfAccumDispatchCalls * invFrames:F1}");
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
                this._perfAccumResourceSetScanSlots = 0;
                this._perfAccumResourceSetBinds = 0;
                this._perfAccumDescriptorCopies = 0;
                this._perfAccumRootTableSets = 0;
                this._perfAccumRootBufferSets = 0;
                this._perfAccumVertexBufferBinds = 0;
                this._perfAccumIndexBufferBinds = 0;
                this._perfAccumDynamicSnapshotCopyBytes = 0;
                this._perfAccumDynamicSnapshotPrefixCopyBytes = 0;
                this._perfAccumDynamicSnapshotRotations = 0;
                this._perfAccumDrawCalls = 0;
                this._perfAccumDispatchCalls = 0;
                this._perfAccumGc0Collections = 0;
                this._perfAccumGc1Collections = 0;
                this._perfAccumGc2Collections = 0;
                this._perfAccumAllocatedBytes = 0;
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
        if (index >= this._activeViewports.Length) {
            return;
        }

        Vortice.Mathematics.Viewport d3D12Viewport = new(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
        if (index + 1 <= this._activeViewportCount && this._activeViewports[index] == d3D12Viewport) {
            return;
        }

        this._activeViewports[index] = d3D12Viewport;

        if (index + 1 > this._activeViewportCount) {
            this._activeViewportCount = index + 1;
        }

        this.RSSetViewportsNoAlloc(this._activeViewportCount, this._activeViewports);
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
        if (index >= this._activeScissorRects.Length) {
            return;
        }

        RawRect scissorRect = new((int)x, (int)y, (int)(x + width), (int)(y + height));
        if (index + 1 <= this._activeScissorRectCount && this._activeScissorRects[index] == scissorRect) {
            return;
        }

        this._activeScissorRects[index] = scissorRect;

        if (index + 1 > this._activeScissorRectCount) {
            this._activeScissorRectCount = index + 1;
        }

        this.RSSetScissorRectsNoAlloc(this._activeScissorRectCount, this._activeScissorRects);
    }

    /// <summary>
    /// Executes the dispatch logic for this backend.
    /// </summary>
    /// <param name="groupCountX">The group count x value used by this operation.</param>
    /// <param name="groupCountY">The group count y value used by this operation.</param>
    /// <param name="groupCountZ">The group count z value used by this operation.</param>
    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ) {
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushComputeResourceSets();
        this.FlushPendingUavBarrier();
        this.FlushPendingBarriers();
        this.DispatchNoAlloc(groupCountX, groupCountY, groupCountZ);
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

        this.gd.ExecuteCommandListNoAlloc(this.NativeCommandList);
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
        if (this._currentGraphicsPipeline == null) {
            return;
        }

        this.EnsureGraphicsResourceSetCapacity(slot + 1);

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
        this.MarkGraphicsResourceSetChanged(slot);
    }

    /// <summary>
    /// Sets the compute resource set core value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="dynamicOffsetsCount">The dynamic offsets count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
        if (this._currentComputePipeline == null) {
            return;
        }

        this.EnsureComputeResourceSetCapacity(slot + 1);

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
        this.MarkComputeResourceSetChanged(slot);
    }

    /// <summary>
    /// Sets the framebuffer core value.
    /// </summary>
    /// <param name="fb">The fb value used by this operation.</param>
    protected override void SetFramebufferCore(Framebuffer fb) {
        if (fb is D3D12SwapchainFramebuffer swapchainFramebuffer && this.TryGetSwapchainBackBuffer(swapchainFramebuffer, out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int backBufferIndex, out ResourceStates currentState)) {
            this.Transition(backBuffer, currentState, ResourceStates.RenderTarget);
            swapchainFramebuffer.Swapchain.SetBackBufferState(backBufferIndex, ResourceStates.RenderTarget);
            this._cachedSwapchainBackBufferState = ResourceStates.RenderTarget;
            this._transitionedBackBufferIndex = backBufferIndex;
            if (swapchainFramebuffer.DepthTargetTexture != null) {
                this.TransitionTexture(swapchainFramebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
            }

            if (swapchainFramebuffer.TryGetDepthStencilView(out CpuDescriptorHandle swapchainDsv)) {
                this.OMSetRenderTargetsNoAlloc(1, rtv, true, swapchainDsv);
            }
            else {
                this.OMSetRenderTargetsNoAlloc(1, rtv, false, default);
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
                this.OMSetRenderTargetsNoAlloc(0, default, true, depthOnlyDsv);
            }

            return;
        }

        if (d3D12Framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv)) {
            this.OMSetRenderTargetsArrayNoAlloc(rtvs, true, dsv);
        }
        else {
            this.OMSetRenderTargetsArrayNoAlloc(rtvs, false, default);
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
                            this.DrawIndexedInstancedNoAlloc(arguments.IndexCount, arguments.InstanceCount, arguments.FirstIndex, arguments.VertexOffset, arguments.FirstInstance);
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
            this.DispatchNoAlloc(arguments.GroupCountX, arguments.GroupCountY, arguments.GroupCountZ);
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
        this.FlushPendingUavBarrier();
        D3D12Texture src = Util.AssertSubtype<Texture, D3D12Texture>(source);
        D3D12Texture dst = Util.AssertSubtype<Texture, D3D12Texture>(destination);

        if (src.NativeTexture == null || dst.NativeTexture == null) {
            src.CopyRegionTo(dst, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, source.Width, source.Height, source.Depth, source.ArrayLayers);
            return;
        }

        CaptureTextureStatesInto(src, this._srcCaptureStates);
        CaptureTextureStatesInto(dst, this._dstCaptureStates);
        this.TransitionTexture(src, ResourceStates.ResolveSource);
        this.TransitionTexture(dst, ResourceStates.ResolveDest);
        this.FlushPendingBarriers();

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

        this.RestoreTextureStates(src, this._srcCaptureStates);
        this.RestoreTextureStates(dst, this._dstCaptureStates);
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
        this.FlushPendingUavBarrier();
        D3D12Texture src = Util.AssertSubtype<Texture, D3D12Texture>(source);
        D3D12Texture dst = Util.AssertSubtype<Texture, D3D12Texture>(destination);

        if (src.NativeTexture != null && dst.NativeTexture != null) {
            CaptureTextureStatesInto(src, this._srcCaptureStates);
            CaptureTextureStatesInto(dst, this._dstCaptureStates);
            this.TransitionTexture(src, ResourceStates.CopySource);
            this.TransitionTexture(dst, ResourceStates.CopyDest);
            this.FlushPendingBarriers();

            for (uint layer = 0; layer < layerCount; layer++) {
                uint srcSubresource = source.CalculateSubresource(srcMipLevel, srcBaseArrayLayer + layer);
                uint dstSubresource = destination.CalculateSubresource(dstMipLevel, dstBaseArrayLayer + layer);
                TextureCopyLocation srcLocation = new(src.NativeTexture, srcSubresource);
                TextureCopyLocation dstLocation = new(dst.NativeTexture, dstSubresource);
                Box srcBox = new((int)srcX, (int)srcY, (int)srcZ, (int)(srcX + width), (int)(srcY + height), (int)(srcZ + depth));
                this.NativeCommandList.CopyTextureRegion(dstLocation, dstX, dstY, dstZ, srcLocation, srcBox);
            }

            this.RestoreTextureStates(src, this._srcCaptureStates);
            this.RestoreTextureStates(dst, this._dstCaptureStates);
            return;
        }

        src.CopyRegionTo(dst, srcX, srcY, srcZ, srcMipLevel, srcBaseArrayLayer, dstX, dstY, dstZ, dstMipLevel, dstBaseArrayLayer, width, height, depth, layerCount);
    }

    /// <summary>
    /// Sets the pipeline core value.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    private protected override void SetPipelineCore(Pipeline pipeline) {
        if (pipeline.IsComputePipeline) {
            D3D12Pipeline d3D12ComputePipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline);
            if (ReferenceEquals(this._currentComputePipeline, d3D12ComputePipeline)) {
                return;
            }

            long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
            this.EnsureComputeResourceSetCapacity(d3D12ComputePipeline.ResourceSetCount);
            bool computeRootSignatureChanged = !ReferenceEquals(this._currentComputePipeline?.RootSignature, d3D12ComputePipeline.RootSignature);
            this._currentComputePipeline = d3D12ComputePipeline;
            this._currentGraphicsPipeline = null;

            if (_perfLogEnabled) {
                this._perfPipelineChanges++;
            }

            this.SetPipelineStateNoAlloc(d3D12ComputePipeline.PipelineState);
            if (computeRootSignatureChanged) {
                ClearBoundResourceSets(this._boundComputeResourceSets);
                ClearChangedResourceSets(this._computeResourceSetsChanged);
                this._computeResourceSetsDirty = false;
                this.ResetComputeResourceSetChangeRange();
                this.InvalidateComputeRootCaches();
                this.SetComputeRootSignatureNoAlloc(d3D12ComputePipeline.RootSignature);
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
        this.EnsureGraphicsResourceSetCapacity(d3D12Pipeline.ResourceSetCount);
        bool rootSignatureChanged = !ReferenceEquals(this._currentGraphicsPipeline?.RootSignature, d3D12Pipeline.RootSignature);
        this._currentGraphicsPipeline = d3D12Pipeline;
        this._currentComputePipeline = null;
        if (_perfLogEnabled) {
            this._perfPipelineChanges++;
        }

        this.SetPipelineStateNoAlloc(d3D12Pipeline.PipelineState);
        if (d3D12Pipeline.UsesStencilReference) {
            this.SetStencilReference(d3D12Pipeline.StencilReference);
        }

        if (rootSignatureChanged) {
            ClearBoundResourceSets(this._boundGraphicsResourceSets);
            ClearChangedResourceSets(this._graphicsResourceSetsChanged);
            this._graphicsResourceSetsDirty = false;
            this.ResetGraphicsResourceSetChangeRange();
            this.InvalidateGraphicsRootCaches();
            this.SetGraphicsRootSignatureNoAlloc(d3D12Pipeline.RootSignature);
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
        this.SetIndexBufferNoAlloc(ref indexView);
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
        this.FlushPendingUavBarrier();
        if (this.Framebuffer is D3D12SwapchainFramebuffer swapchainFramebuffer && this.TryGetSwapchainBackBuffer(swapchainFramebuffer, out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int backBufferIndex, out ResourceStates currentState)) {
            this.Transition(backBuffer, currentState, ResourceStates.RenderTarget);
            swapchainFramebuffer.Swapchain.SetBackBufferState(backBufferIndex, ResourceStates.RenderTarget);
            this._cachedSwapchainBackBufferState = ResourceStates.RenderTarget;
            this._transitionedBackBufferIndex = backBufferIndex;
            this.FlushPendingBarriers();
            this.ClearRenderTargetViewNoAlloc(rtv, clearColor.R, clearColor.G, clearColor.B, clearColor.A);
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

            this.FlushPendingBarriers();
            this.ClearRenderTargetViewNoAlloc(offscreenRtv, clearColor.R, clearColor.G, clearColor.B, clearColor.A);
        }
    }

    /// <summary>
    /// Executes the clear depth stencil core logic for this backend.
    /// </summary>
    /// <param name="depth">The depth value.</param>
    /// <param name="stencil">The stencil value used by this operation.</param>
    private protected override void ClearDepthStencilCore(float depth, byte stencil) {
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

                this.FlushPendingBarriers();
                this.ClearDepthStencilViewNoAlloc(swapchainDsv, (uint)swapchainClearFlags, depth, stencil);
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

            this.FlushPendingBarriers();
            this.ClearDepthStencilViewNoAlloc(dsv, (uint)clearFlags, depth, stencil);
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
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushGraphicsResourceSets();
        this.FlushPendingUavBarrier();
        this.FlushPendingBarriers();
        this.DrawInstancedNoAlloc(vertexCount, instanceCount, vertexStart, instanceStart);
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
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushGraphicsResourceSets();
        this.FlushPendingUavBarrier();
        this.FlushPendingBarriers();
        this.DrawIndexedInstancedNoAlloc(indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
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
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.FlushPendingUavBarrier();
        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
        ulong previousBindVersion = d3D12Buffer.BindVersion;
        D3D12ResourceAllocation temporaryUpload = d3D12Buffer.Update(this.NativeCommandList, source, bufferOffsetInBytes, sizeInBytes);
        if (_perfLogEnabled) {
            this._perfDynamicSnapshotCopyBytes += d3D12Buffer.LastDynamicSnapshotCopyBytes;
            this._perfDynamicSnapshotPrefixCopyBytes += d3D12Buffer.LastDynamicSnapshotPrefixCopyBytes;
            if (d3D12Buffer.LastDynamicSnapshotRotated) {
                this._perfDynamicSnapshotRotations++;
            }
        }

        if (d3D12Buffer.BindVersion != previousBindVersion) {
            if (CanBeResourceSetBuffer(d3D12Buffer)) {
                this.MarkResourceSetsReferencingBufferDirty(d3D12Buffer);
            }

            this.RefreshDynamicBufferBindings(d3D12Buffer);
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
            this.SetComputeRoot32BitConstantsNoAlloc(pipeline.PushConstantRootParameterIndex, dwordCount, (void*)data, dwordOffset);
        }
        else {
            this.SetGraphicsRoot32BitConstantsNoAlloc(pipeline.PushConstantRootParameterIndex, dwordCount, (void*)data, dwordOffset);
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

        if (this._pendingBarrierCount == (uint)this._barrierBatch.Length) {
            this.FlushPendingBarriers();
        }

        this._barrierBatch[this._pendingBarrierCount++] = ResourceBarrier.BarrierTransition(resource, from, to);
        if (_perfLogEnabled) {
            this._perfTransitions++;
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
        this.SetVertexBufferNoAlloc(index, ref view);
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
    /// Refreshes input-assembler views that point at a dynamic buffer whose native snapshot offset changed.
    /// </summary>
    /// <param name="buffer">The dynamic buffer whose binding version changed.</param>
    private void RefreshDynamicBufferBindings(D3D12DeviceBuffer buffer) {
        ulong bindVersion = buffer.BindVersion;
        for (uint index = 0; index < this._maxBoundVertexBufferSlot; index++) {
            if (!ReferenceEquals(this._boundVertexBuffers[index], buffer)) {
                continue;
            }

            this.BindVertexBuffer(index, buffer, this._boundVertexBufferOffsets[index]);
            this._boundVertexBufferVersions[index] = bindVersion;
            if (_perfLogEnabled) {
                this._perfVertexBufferBinds++;
            }
        }

        if (!this._hasBoundIndexBuffer || !ReferenceEquals(this._boundIndexBuffer, buffer)) {
            return;
        }

        this.TransitionBuffer(buffer, ResourceStates.IndexBuffer);
        uint viewSize = buffer.GetBindableSize(this._boundIndexBufferOffset);
        IndexBufferView indexView = new(buffer.GetGpuVirtualAddress(this._boundIndexBufferOffset), viewSize, D3D12Formats.ToDxgiFormat(this._boundIndexFormat));
        this.SetIndexBufferNoAlloc(ref indexView);
        this._boundIndexBufferVersion = bindVersion;
        if (_perfLogEnabled) {
            this._perfIndexBufferBinds++;
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

        this.IASetPrimitiveTopologyNoAlloc(topology);
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

        this.OMSetStencilRefNoAlloc(stencilReference);
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

        if (this._pendingBarrierCount == (uint)this._barrierBatch.Length) {
            this.FlushPendingBarriers();
        }

        this._barrierBatch[this._pendingBarrierCount++] = ResourceBarrier.BarrierTransition(resource, from, to, subresource);
        if (_perfLogEnabled) {
            this._perfSubresourceTransitions++;
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
        this.ResourceBarrierNoAlloc(ref this._singleBarrier[0]);
        if (_perfLogEnabled) {
            this._perfUavBarriers++;
            this._perfBarrierMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }

        this._uavBarrierPending = false;
    }

    /// <summary>
    /// Flushes all accumulated pending resource-barrier transitions as a single batched D3D12 call.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushPendingBarriers() {
        if (this._pendingBarrierCount == 0) {
            return;
        }

        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        this.ResourceBarrierBatchNoAlloc(this._barrierBatch, this._pendingBarrierCount);
        this._pendingBarrierCount = 0;
        if (_perfLogEnabled) {
            this._perfBarrierMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
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
            this._frameSlotFenceValues[frameSlot] = 0;
            return;
        }

        long startTicks = 0;
        if (_perfLogEnabled) {
            startTicks = Stopwatch.GetTimestamp();
        }

        this.gd.WaitForSubmissionFence(fenceValue);
        this._frameSlotFenceValues[frameSlot] = 0;
        if (_perfLogEnabled) {
            long elapsedTicks = Stopwatch.GetTimestamp() - startTicks;
            this._perfBeginWaitMs += TicksToMilliseconds(elapsedTicks);
            this._perfBeginWaitCount++;
        }
    }

    /// <summary>
    /// Updates shader-visible descriptor allocator limits for the persistent command-list heaps.
    /// </summary>
    private void UpdateDescriptorAllocatorLimits() {
        this._srvUavDescriptorLimit = this._maxSrvUavDescriptors;
        this._samplerDescriptorLimit = this._maxSamplerDescriptors;
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
        this.FlushPendingBarriers();

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
        this.SetComputeRootSignatureNoAlloc(this._gpuMipPipeline.RootSignature);
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
                    this.DispatchNoAlloc(groupCountX, groupCountY, 1);
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
                this.SetComputeRootSignatureNoAlloc(previousCompute.RootSignature);
                this._currentComputePipeline = previousCompute;
                this._currentGraphicsPipeline = null;
                this.InvalidateComputeRootCaches();
                this.EnsureComputeResourceSetCapacity(previousCompute.ResourceSetCount);
                this.MarkBoundComputeResourceSetsChanged(previousCompute.ResourceSetCount);
            }
            else if (previousGraphics != null) {
                this.NativeCommandList.SetPipelineState(previousGraphics.PipelineState);
                this.SetGraphicsRootSignatureNoAlloc(previousGraphics.RootSignature);
                this.SetPrimitiveTopology(previousGraphics.PrimitiveTopology);
                this.SetStencilReference(previousGraphics.StencilReference);
                this._currentGraphicsPipeline = previousGraphics;
                this._currentComputePipeline = null;
                this.InvalidateGraphicsRootCaches();
                this.EnsureGraphicsResourceSetCapacity(previousGraphics.ResourceSetCount);
                this.MarkBoundGraphicsResourceSetsChanged(previousGraphics.ResourceSetCount);
            }
            else {
                this._currentComputePipeline = null;
                this._currentGraphicsPipeline = null;
            }
        }
    }
    
    /// <summary>
    /// Executes the ensure gpu mipmap resources logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    [SupportedOSPlatform("windows")]
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
    
    /// <summary>
    /// Executes the compile compute shader logic for this backend.
    /// </summary>
    /// <param name="sourceCode">The source code value used by this operation.</param>
    /// <param name="entryPoint">The entry point value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [SupportedOSPlatform("windows")]
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
    /// Captures current per-subresource states into a pre-allocated buffer without heap allocation.
    /// </summary>
    private static void CaptureTextureStatesInto(D3D12Texture texture, ResourceStates[] buffer) {
        uint subresourceCount = texture.SubresourceCount;
        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            buffer[subresource] = texture.GetSubresourceState(subresource);
        }
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
    [DllImport("d3dcompiler_47.dll", CharSet = CharSet.Ansi)]
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

        D3D12DeviceBuffer d3D12Buffer = GetD3D12BufferRange(resource, dynamicOffset, out uint rangeOffset);
        this.TransitionBuffer(d3D12Buffer, GetGraphicsBufferState(bindingInfo.Kind));
        ulong gpuAddress = d3D12Buffer.GetGpuVirtualAddress(rangeOffset);
        if (this.IsSameGraphicsRootBuffer(bindingInfo.RootParameterIndex, gpuAddress)) {
            return;
        }

        switch (bindingInfo.Kind) {
            case ResourceKind.UniformBuffer:
                this.SetGraphicsRootConstantBufferViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadOnly:
                this.SetGraphicsRootShaderResourceViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadWrite:
                this.SetGraphicsRootUnorderedAccessViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.TextureReadOnly: case ResourceKind.TextureReadWrite: case ResourceKind.Sampler: throw new VeldridException("Texture and sampler root bindings must use descriptor tables.");
            default: throw Illegal.Value<ResourceKind>();
        }

        this.SetGraphicsRootBufferCache(bindingInfo.RootParameterIndex, gpuAddress);
        if (_perfLogEnabled) {
            this._perfRootBufferSets++;
        }
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

        D3D12DeviceBuffer d3D12Buffer = GetD3D12BufferRange(resource, dynamicOffset, out uint rangeOffset);
        this.TransitionBuffer(d3D12Buffer, GetComputeBufferState(bindingInfo.Kind));
        ulong gpuAddress = d3D12Buffer.GetGpuVirtualAddress(rangeOffset);

        if (this.IsSameComputeRootBuffer(bindingInfo.RootParameterIndex, gpuAddress)) {
            return;
        }

        switch (bindingInfo.Kind) {
            case ResourceKind.UniformBuffer:
                this.SetComputeRootConstantBufferViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadOnly:
                this.SetComputeRootShaderResourceViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.StructuredBufferReadWrite:
                this.SetComputeRootUnorderedAccessViewNoAlloc(bindingInfo.RootParameterIndex, gpuAddress);
                break;
            case ResourceKind.TextureReadOnly: case ResourceKind.TextureReadWrite: case ResourceKind.Sampler: throw new VeldridException("Texture and sampler root bindings must use descriptor tables.");
            default: throw Illegal.Value<ResourceKind>();
        }

        this.SetComputeRootBufferCache(bindingInfo.RootParameterIndex, gpuAddress);
        if (_perfLogEnabled) {
            this._perfRootBufferSets++;
        }
    }

    /// <summary>
    /// Binds a graphics root constant buffer view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetGraphicsRootConstantBufferViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this.SetRootBufferViewNoAlloc(38, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a graphics root shader resource view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetGraphicsRootShaderResourceViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this.SetRootBufferViewNoAlloc(40, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a graphics root unordered access view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetGraphicsRootUnorderedAccessViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this.SetRootBufferViewNoAlloc(42, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a compute root constant buffer view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetComputeRootConstantBufferViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this.SetRootBufferViewNoAlloc(37, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a compute root shader resource view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetComputeRootShaderResourceViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this.SetRootBufferViewNoAlloc(39, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a compute root unordered access view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetComputeRootUnorderedAccessViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this.SetRootBufferViewNoAlloc(41, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Invokes an ID3D12GraphicsCommandList root buffer binding method directly.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetRootBufferViewNoAlloc(int vtableIndex, uint rootParameterIndex, ulong gpuAddress) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, ulong, void> setRootBufferView = (delegate* unmanaged[Stdcall]<void*, uint, ulong, void>)vtbl[vtableIndex];
        setRootBufferView((void*)this.NativeCommandList.NativePointer, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Sets viewports without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void RSSetViewportsNoAlloc(uint count, Vortice.Mathematics.Viewport[] viewports) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, void> rsSetViewports = (delegate* unmanaged[Stdcall]<void*, uint, void*, void>)vtbl[21];
        fixed (Vortice.Mathematics.Viewport* pViewports = viewports) {
            rsSetViewports((void*)this.NativeCommandList.NativePointer, count, pViewports);
        }
    }

    /// <summary>
    /// Sets scissor rects without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void RSSetScissorRectsNoAlloc(uint count, RawRect[] rects) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, void> rsSetScissorRects = (delegate* unmanaged[Stdcall]<void*, uint, void*, void>)vtbl[22];
        fixed (RawRect* pRects = rects) {
            rsSetScissorRects((void*)this.NativeCommandList.NativePointer, count, pRects);
        }
    }

    /// <summary>
    /// Closes the command list without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void CloseNoAlloc() {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, int> close = (delegate* unmanaged[Stdcall]<void*, int>)vtbl[9];
        Result result = new(close((void*)this.NativeCommandList.NativePointer));
        result.CheckError();
    }

    /// <summary>
    /// Resets the command list without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ResetCommandListNoAlloc(ID3D12CommandAllocator allocator) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, void*, void*, int> reset = (delegate* unmanaged[Stdcall]<void*, void*, void*, int>)vtbl[10];
        Result result = new(reset((void*)this.NativeCommandList.NativePointer, (void*)allocator.NativePointer, null));
        result.CheckError();
    }

    /// <summary>
    /// Resets a command allocator without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ResetCommandAllocatorNoAlloc(ID3D12CommandAllocator allocator) {
        void** vtbl = *(void***)allocator.NativePointer;
        delegate* unmanaged[Stdcall]<void*, int> reset = (delegate* unmanaged[Stdcall]<void*, int>)vtbl[8];
        Result result = new(reset((void*)allocator.NativePointer));
        result.CheckError();
    }

    /// <summary>
    /// Records DrawInstanced without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void DrawInstancedNoAlloc(uint vertexCount, uint instanceCount, uint startVertexLocation, uint startInstanceLocation) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, uint, uint, uint, void> drawInstanced = (delegate* unmanaged[Stdcall]<void*, uint, uint, uint, uint, void>)vtbl[12];
        drawInstanced((void*)this.NativeCommandList.NativePointer, vertexCount, instanceCount, startVertexLocation, startInstanceLocation);
    }

    /// <summary>
    /// Records DrawIndexedInstanced without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void DrawIndexedInstancedNoAlloc(uint indexCount, uint instanceCount, uint startIndexLocation, int baseVertexLocation, uint startInstanceLocation) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, uint, uint, int, uint, void> drawIndexedInstanced = (delegate* unmanaged[Stdcall]<void*, uint, uint, uint, int, uint, void>)vtbl[13];
        drawIndexedInstanced((void*)this.NativeCommandList.NativePointer, indexCount, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
    }

    /// <summary>
    /// Binds pipeline state without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetPipelineStateNoAlloc(ID3D12PipelineState pipelineState) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, void*, void> setPipelineState = (delegate* unmanaged[Stdcall]<void*, void*, void>)vtbl[25];
        setPipelineState((void*)this.NativeCommandList.NativePointer, (void*)pipelineState.NativePointer);
    }

    /// <summary>
    /// Records a resource barrier without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ResourceBarrierNoAlloc(ref ResourceBarrier barrier) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, void> resourceBarrier = (delegate* unmanaged[Stdcall]<void*, uint, void*, void>)vtbl[26];
        resourceBarrier((void*)this.NativeCommandList.NativePointer, 1u, Unsafe.AsPointer(ref barrier));
    }

    /// <summary>
    /// Records a batch of resource barriers without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ResourceBarrierBatchNoAlloc(ResourceBarrier[] barriers, uint count) {
        if (count == 0) {
            return;
        }

        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, void> resourceBarrier = (delegate* unmanaged[Stdcall]<void*, uint, void*, void>)vtbl[26];
        resourceBarrier((void*)this.NativeCommandList.NativePointer, count, Unsafe.AsPointer(ref barriers[0]));
    }

    /// <summary>
    /// Binds one index-buffer view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetIndexBufferNoAlloc(ref IndexBufferView view) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, void*, void> setIndexBuffer = (delegate* unmanaged[Stdcall]<void*, void*, void>)vtbl[43];
        setIndexBuffer((void*)this.NativeCommandList.NativePointer, Unsafe.AsPointer(ref view));
    }

    /// <summary>
    /// Sets render targets without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void OMSetRenderTargetsNoAlloc(uint numRenderTargetDescriptors, CpuDescriptorHandle rtvHandle, bool hasDepthStencil, CpuDescriptorHandle dsvHandle) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, int, void*, void> omSetRenderTargets = (delegate* unmanaged[Stdcall]<void*, uint, void*, int, void*, void>)vtbl[46];
        omSetRenderTargets((void*)this.NativeCommandList.NativePointer, numRenderTargetDescriptors, Unsafe.AsPointer(ref rtvHandle), 1, hasDepthStencil ? Unsafe.AsPointer(ref dsvHandle) : null);
    }

    /// <summary>
    /// Sets descriptor heaps without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetDescriptorHeapsNoAlloc(ID3D12DescriptorHeap[] heaps) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, void> setDescriptorHeaps = (delegate* unmanaged[Stdcall]<void*, uint, void*, void>)vtbl[28];
        void* heap0 = (void*)heaps[0].NativePointer;
        void* heap1 = (void*)heaps[1].NativePointer;
        void** heapPtrs = stackalloc void*[2] { heap0, heap1 };
        setDescriptorHeaps((void*)this.NativeCommandList.NativePointer, 2u, heapPtrs);
    }

    /// <summary>
    /// Sets stencil reference without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void OMSetStencilRefNoAlloc(uint stencilRef) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void> setStencilRef = (delegate* unmanaged[Stdcall]<void*, uint, void>)vtbl[24];
        setStencilRef((void*)this.NativeCommandList.NativePointer, stencilRef);
    }

    /// <summary>
    /// Sets primitive topology without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void IASetPrimitiveTopologyNoAlloc(Vortice.Direct3D.PrimitiveTopology topology) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, int, void> setPrimitiveTopology = (delegate* unmanaged[Stdcall]<void*, int, void>)vtbl[20];
        setPrimitiveTopology((void*)this.NativeCommandList.NativePointer, (int)topology);
    }

    /// <summary>
    /// Binds one vertex-buffer view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetVertexBufferNoAlloc(uint startSlot, ref VertexBufferView view) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, uint, void*, void> setVertexBuffers = (delegate* unmanaged[Stdcall]<void*, uint, uint, void*, void>)vtbl[44];
        setVertexBuffers((void*)this.NativeCommandList.NativePointer, startSlot, 1u, Unsafe.AsPointer(ref view));
    }

    /// <summary>
    /// Dispatches a compute shader without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void DispatchNoAlloc(uint groupCountX, uint groupCountY, uint groupCountZ) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, uint, uint, void> dispatch = (delegate* unmanaged[Stdcall]<void*, uint, uint, uint, void>)vtbl[14];
        dispatch((void*)this.NativeCommandList.NativePointer, groupCountX, groupCountY, groupCountZ);
    }

    /// <summary>
    /// Sets the compute root signature without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetComputeRootSignatureNoAlloc(ID3D12RootSignature rootSignature) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, void*, void> setRootSig = (delegate* unmanaged[Stdcall]<void*, void*, void>)vtbl[29];
        setRootSig((void*)this.NativeCommandList.NativePointer, (void*)rootSignature.NativePointer);
    }

    /// <summary>
    /// Sets the graphics root signature without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetGraphicsRootSignatureNoAlloc(ID3D12RootSignature rootSignature) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, void*, void> setRootSig = (delegate* unmanaged[Stdcall]<void*, void*, void>)vtbl[30];
        setRootSig((void*)this.NativeCommandList.NativePointer, (void*)rootSignature.NativePointer);
    }

    /// <summary>
    /// Sets compute root 32-bit constants without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetComputeRoot32BitConstantsNoAlloc(uint rootParameterIndex, uint num32BitValues, void* srcData, uint destOffset) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, uint, void*, uint, void> setConstants = (delegate* unmanaged[Stdcall]<void*, uint, uint, void*, uint, void>)vtbl[35];
        setConstants((void*)this.NativeCommandList.NativePointer, rootParameterIndex, num32BitValues, srcData, destOffset);
    }

    /// <summary>
    /// Sets graphics root 32-bit constants without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetGraphicsRoot32BitConstantsNoAlloc(uint rootParameterIndex, uint num32BitValues, void* srcData, uint destOffset) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, uint, void*, uint, void> setConstants = (delegate* unmanaged[Stdcall]<void*, uint, uint, void*, uint, void>)vtbl[36];
        setConstants((void*)this.NativeCommandList.NativePointer, rootParameterIndex, num32BitValues, srcData, destOffset);
    }

    /// <summary>
    /// Sets multiple render targets without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void OMSetRenderTargetsArrayNoAlloc(CpuDescriptorHandle[] rtvs, bool hasDepthStencil, CpuDescriptorHandle dsv) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, int, void*, void> omSetRenderTargets = (delegate* unmanaged[Stdcall]<void*, uint, void*, int, void*, void>)vtbl[46];
        fixed (CpuDescriptorHandle* rtvPtr = rtvs) {
            omSetRenderTargets((void*)this.NativeCommandList.NativePointer, (uint)rtvs.Length, rtvPtr, 0, hasDepthStencil ? Unsafe.AsPointer(ref dsv) : null);
        }
    }

    /// <summary>
    /// Clears a render target view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ClearRenderTargetViewNoAlloc(CpuDescriptorHandle rtv, float r, float g, float b, float a) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, CpuDescriptorHandle, float*, uint, void*, void> fn =
            (delegate* unmanaged[Stdcall]<void*, CpuDescriptorHandle, float*, uint, void*, void>)vtbl[48];
        float* color = stackalloc float[4] { r, g, b, a };
        fn((void*)this.NativeCommandList.NativePointer, rtv, color, 0u, null);
    }

    /// <summary>
    /// Clears a depth/stencil view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ClearDepthStencilViewNoAlloc(CpuDescriptorHandle dsv, uint clearFlags, float depth, byte stencil) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, CpuDescriptorHandle, uint, float, byte, uint, void*, void> fn =
            (delegate* unmanaged[Stdcall]<void*, CpuDescriptorHandle, uint, float, byte, uint, void*, void>)vtbl[47];
        fn((void*)this.NativeCommandList.NativePointer, dsv, clearFlags, depth, stencil, 0u, null);
    }

    /// <summary>
    /// Resolves a bindable buffer resource and its dynamic offset for a D3D12 root-buffer binding.
    /// </summary>
    private static D3D12DeviceBuffer GetD3D12BufferRange(IBindableResource resource, uint dynamicOffset, out uint offset) {
        if (resource is DeviceBufferRange range) {
            offset = range.Offset + dynamicOffset;
            return Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(range.Buffer);
        }

        if (resource is DeviceBuffer buffer) {
            offset = dynamicOffset;
            return Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
        }

        throw new PlatformNotSupportedException("D3D12 ResourceSet currently supports buffer resources only for non-table root bindings.");
    }

    /// <summary>
    /// Flushes graphics resource sets that were changed since the previous draw.
    /// </summary>
    private void FlushGraphicsResourceSets() {
        if (!this._graphicsResourceSetsDirty || this._currentGraphicsPipeline == null) {
            return;
        }

        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        int start = this._graphicsResourceSetsChangedStart;
        int end = Math.Min(this._graphicsResourceSetsChangedEnd, this.GetGraphicsResourceSetFlushEnd());
        if (start < 0 || end < start) {
            this._graphicsResourceSetsDirty = false;
            this.ResetGraphicsResourceSetChangeRange();
            return;
        }

        if (_perfLogEnabled) {
            this._perfResourceSetScanSlots += (ulong)(end - start + 1);
        }

        for (int slot = start; slot <= end; slot++) {
            if (!this._graphicsResourceSetsChanged[slot]) {
                continue;
            }

            this._graphicsResourceSetsChanged[slot] = false;
            this.BindResourceSet(this._currentGraphicsPipeline, (uint)slot, ref this._boundGraphicsResourceSets[slot], false);
        }

        this._graphicsResourceSetsDirty = false;
        this.ResetGraphicsResourceSetChangeRange();
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
        int start = this._computeResourceSetsChangedStart;
        int end = Math.Min(this._computeResourceSetsChangedEnd, this.GetComputeResourceSetFlushEnd());
        if (start < 0 || end < start) {
            this._computeResourceSetsDirty = false;
            this.ResetComputeResourceSetChangeRange();
            return;
        }

        if (_perfLogEnabled) {
            this._perfResourceSetScanSlots += (ulong)(end - start + 1);
        }

        for (int slot = start; slot <= end; slot++) {
            if (!this._computeResourceSetsChanged[slot]) {
                continue;
            }

            this._computeResourceSetsChanged[slot] = false;
            this.BindResourceSet(this._currentComputePipeline, (uint)slot, ref this._boundComputeResourceSets[slot], true);
        }

        this._computeResourceSetsDirty = false;
        this.ResetComputeResourceSetChangeRange();
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
        if (slot >= pipeline.ResourceSetCount || boundSet.Set == null) {
            return;
        }

        if (_perfLogEnabled) {
            this._perfResourceSetBinds++;
        }

        D3D12ResourceSet d3d12Set = Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(boundSet.Set);
        IBindableResource[] resources = d3d12Set.BoundResources;
        ResourceSetBindingPlan bindingPlan = compute ? this.GetComputeResourceSetBindingPlan(pipeline, slot, d3d12Set.ResourceLayoutInfo) : this.GetGraphicsResourceSetBindingPlan(pipeline, slot, d3d12Set.ResourceLayoutInfo);
        uint dynamicOffsetIndex = 0;
        bool descriptorTablesChanged = false;
        ResourceSetBindingPlanEntry[] entries = bindingPlan.Entries;

        for (int i = 0; i < entries.Length; i++) {
            ref readonly ResourceSetBindingPlanEntry bindingEntry = ref entries[i];
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
        bool graphicsChanged = this.MarkResourceSetsReferencingBufferDirty(this._boundGraphicsResourceSets, this._graphicsResourceSetsChanged, this._currentGraphicsPipeline?.ResourceSetCount ?? 0u, buffer, true);
        bool computeChanged = this.MarkResourceSetsReferencingBufferDirty(this._boundComputeResourceSets, this._computeResourceSetsChanged, this._currentComputePipeline?.ResourceSetCount ?? 0u, buffer, false);
        if (graphicsChanged) {
            this._graphicsResourceSetsDirty = true;
        }

        if (computeChanged) {
            this._computeResourceSetsDirty = true;
        }
    }

    /// <summary>
    /// Marks the resource sets that reference a specific buffer as dirty.
    /// </summary>
    /// <param name="resourceSets">The bound resource set collection.</param>
    /// <param name="changed">The dirty flag collection.</param>
    /// <param name="resourceSetCount">The number of slots used by the active pipeline.</param>
    /// <param name="buffer">The buffer whose native binding address changed.</param>
    /// <param name="graphics">Whether the graphics or compute range should be updated.</param>
    /// <returns><see langword="true" /> when at least one resource set was marked dirty.</returns>
    private bool MarkResourceSetsReferencingBufferDirty(BoundResourceSetInfo[] resourceSets, bool[] changed, uint resourceSetCount, D3D12DeviceBuffer buffer, bool graphics) {
        int count = Math.Min(Math.Min(resourceSets.Length, changed.Length), GetClampedResourceSetCount(resourceSetCount));
        bool anyChanged = false;
        for (int slot = 0; slot < count; slot++) {
            if (resourceSets[slot].Set is not D3D12ResourceSet resourceSet) {
                continue;
            }

            if (!ResourceSetReferencesBuffer(resourceSet, buffer)) {
                continue;
            }

            if (graphics) {
                this.MarkGraphicsResourceSetChanged((uint)slot);
            }
            else {
                this.MarkComputeResourceSetChanged((uint)slot);
            }

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
        D3D12DeviceBuffer[] referencedBuffers = resourceSet.ReferencedBuffers;
        for (int i = 0; i < referencedBuffers.Length; i++) {
            if (ReferenceEquals(referencedBuffers[i], buffer)) {
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
    private void BindResourceSetDescriptorTables(D3D12ResourceSet set, ResourceSetBindingPlan bindingPlan, bool compute) {
        this.BindDescriptorHeaps();
        this.BindResourceSetDescriptorTable(set, bindingPlan.Entries, bindingPlan.SrvUavTable, compute);
        this.BindResourceSetDescriptorTable(set, bindingPlan.Entries, bindingPlan.SamplerTable, compute);
    }

    /// <summary>
    /// Binds one grouped descriptor table for a resource set.
    /// </summary>
    /// <param name="set">The resource set being bound.</param>
    /// <param name="bindingPlan">The binding plan for the active pipeline and set slot.</param>
    /// <param name="compute">Whether the active pipeline is a compute pipeline.</param>
    /// <param name="tableInfo">The descriptor table metadata.</param>
    private void BindResourceSetDescriptorTable(D3D12ResourceSet set, ResourceSetBindingPlanEntry[] bindingPlan, DescriptorTableBindingInfo tableInfo, bool compute) {
        if (!tableInfo.HasTable) {
            return;
        }

        D3D12Pipeline.DescriptorTableKind tableKind = tableInfo.TableKind;
        if (!TryGetDescriptorTableHandle(set, tableKind, tableInfo.Signature, out GpuDescriptorHandle gpuHandle)) {
            DescriptorHeapType heapType;
            CpuDescriptorHandle cpuHandle;
            if (tableKind == D3D12Pipeline.DescriptorTableKind.Sampler) {
                heapType = DescriptorHeapType.Sampler;
                this.AllocateSamplerDescriptors(tableInfo.DescriptorCount, out cpuHandle, out gpuHandle);
            }
            else {
                heapType = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView;
                this.AllocateSrvUavDescriptors(tableInfo.DescriptorCount, out cpuHandle, out gpuHandle);
            }

            long descriptorCopyStartTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
            int descriptorSize = tableKind == D3D12Pipeline.DescriptorTableKind.Sampler ? this._samplerDescriptorSize : this._srvUavDescriptorSize;
            uint batchCount = 0;
            for (int i = 0; i < bindingPlan.Length; i++) {
                ResourceSetBindingPlanEntry entry = bindingPlan[i];
                D3D12Pipeline.RootBindingInfo bindingInfo = entry.BindingInfo;
                if (!bindingInfo.DescriptorTable || bindingInfo.DescriptorTableKind != tableKind) {
                    continue;
                }

                this._descriptorCopyDests[batchCount] = cpuHandle + (int)(bindingInfo.DescriptorTableOffset * (uint)descriptorSize);
                this._descriptorCopySources[batchCount] = this.GetSourceDescriptor(set.BoundResources[entry.ElementIndex], bindingInfo.Kind);
                batchCount++;
            }

            if (batchCount > 0) {
                this.gd.Device.CopyDescriptors(batchCount, this._descriptorCopyDests, _descriptorCopyRangeSizes, batchCount, this._descriptorCopySources, _descriptorCopyRangeSizes, heapType);
            }

            if (_perfLogEnabled) {
                this._perfDescriptorCopyMs += TicksToMilliseconds(Stopwatch.GetTimestamp() - descriptorCopyStartTicks);
            }

            CacheDescriptorTableHandle(set, tableKind, tableInfo.Signature, gpuHandle);
        }

        if ((compute && this.IsSameComputeRootTable(tableInfo.RootParameterIndex, gpuHandle.Ptr))
            || (!compute && this.IsSameGraphicsRootTable(tableInfo.RootParameterIndex, gpuHandle.Ptr))) {
            return;
        }

        if (compute) {
            this.SetComputeRootDescriptorTableNoAlloc(tableInfo.RootParameterIndex, gpuHandle);
            this.SetComputeRootTableCache(tableInfo.RootParameterIndex, gpuHandle.Ptr);
        }
        else {
            this.SetGraphicsRootDescriptorTableNoAlloc(tableInfo.RootParameterIndex, gpuHandle);
            this.SetGraphicsRootTableCache(tableInfo.RootParameterIndex, gpuHandle.Ptr);
        }

        if (_perfLogEnabled) {
            this._perfRootTableSets++;
        }
    }

    /// <summary>
    /// Binds a compute descriptor table without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetComputeRootDescriptorTableNoAlloc(uint rootParameterIndex, GpuDescriptorHandle gpuHandle) {
        this.SetRootDescriptorTableNoAlloc(31, rootParameterIndex, gpuHandle);
    }

    /// <summary>
    /// Binds a graphics descriptor table without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetGraphicsRootDescriptorTableNoAlloc(uint rootParameterIndex, GpuDescriptorHandle gpuHandle) {
        this.SetRootDescriptorTableNoAlloc(32, rootParameterIndex, gpuHandle);
    }

    /// <summary>
    /// Invokes an ID3D12GraphicsCommandList root descriptor-table binding method directly.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetRootDescriptorTableNoAlloc(int vtableIndex, uint rootParameterIndex, GpuDescriptorHandle gpuHandle) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, ulong, void> setRootDescriptorTable = (delegate* unmanaged[Stdcall]<void*, uint, ulong, void>)vtbl[vtableIndex];
        setRootDescriptorTable((void*)this.NativeCommandList.NativePointer, rootParameterIndex, gpuHandle.Ptr);
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
        this.SetDescriptorHeapsNoAlloc(this._boundDescriptorHeaps);
        this._descriptorHeapsBound = true;
    }

    /// <summary>
    /// Executes the allocate srv uav descriptor logic for this backend.
    /// </summary>
    /// <param name="cpuHandle">The cpu handle value used by this operation.</param>
    /// <param name="gpuHandle">The gpu handle value used by this operation.</param>
    private void AllocateSrvUavDescriptors(uint count, out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
        if (this._nextSrvUavDescriptor + count > this._srvUavDescriptorLimit) {
            throw new VeldridException("D3D12 SRV/UAV descriptor heap exhausted for this CommandList. Create fewer unique ResourceSets or increase the persistent descriptor heap size.");
        }

        cpuHandle = new CpuDescriptorHandle(this._srvUavHeapCpuStart, (int)this._nextSrvUavDescriptor, (uint)this._srvUavDescriptorSize);
        gpuHandle = new GpuDescriptorHandle(this._srvUavHeapGpuStart, (int)this._nextSrvUavDescriptor, (uint)this._srvUavDescriptorSize);
        this._nextSrvUavDescriptor += count;
    }

    /// <summary>
    /// Executes the allocate sampler descriptor logic for this backend.
    /// </summary>
    /// <param name="cpuHandle">The cpu handle value used by this operation.</param>
    /// <param name="gpuHandle">The gpu handle value used by this operation.</param>
    private void AllocateSamplerDescriptors(uint count, out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
        if (this._nextSamplerDescriptor + count > this._samplerDescriptorLimit) {
            throw new VeldridException("D3D12 sampler descriptor heap exhausted for this CommandList. Create fewer unique ResourceSets or increase the persistent sampler descriptor heap size.");
        }

        cpuHandle = new CpuDescriptorHandle(this._samplerHeapCpuStart, (int)this._nextSamplerDescriptor, (uint)this._samplerDescriptorSize);
        gpuHandle = new GpuDescriptorHandle(this._samplerHeapGpuStart, (int)this._nextSamplerDescriptor, (uint)this._samplerDescriptorSize);
        this._nextSamplerDescriptor += count;
    }

    /// <summary>
    /// Attempts to reuse a shader-visible descriptor table handle for the current frame slot.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="kind">The descriptor-table resource kind.</param>
    /// <param name="handle">The cached GPU descriptor handle, when available.</param>
    /// <returns><see langword="true" /> when a cached handle was found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetDescriptorTableHandle(D3D12ResourceSet set, D3D12Pipeline.DescriptorTableKind kind, uint tableSignature, out GpuDescriptorHandle handle) {
        if (kind == D3D12Pipeline.DescriptorTableKind.Sampler) {
            if (set.HasCachedSamplerHandle && set.CachedSamplerSignature == tableSignature) {
                handle = set.CachedSamplerHandle;
                return true;
            }
        }
        else {
            if (set.HasCachedSrvUavHandle && set.CachedSrvUavSignature == tableSignature) {
                handle = set.CachedSrvUavHandle;
                return true;
            }
        }

        handle = default;
        return false;
    }

    /// <summary>
    /// Stores a shader-visible descriptor table handle for reuse on the current frame slot.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="kind">The descriptor-table resource kind.</param>
    /// <param name="handle">The GPU descriptor handle to cache.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CacheDescriptorTableHandle(D3D12ResourceSet set, D3D12Pipeline.DescriptorTableKind kind, uint tableSignature, GpuDescriptorHandle handle) {
        if (kind == D3D12Pipeline.DescriptorTableKind.Sampler) {
            set.CachedSamplerHandle = handle;
            set.CachedSamplerSignature = tableSignature;
            set.HasCachedSamplerHandle = true;
        }
        else {
            set.CachedSrvUavHandle = handle;
            set.CachedSrvUavSignature = tableSignature;
            set.HasCachedSrvUavHandle = true;
        }
    }

    /// <summary>
    /// Gets the graphics resource set binding plan value.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="layout">The resource layout used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ResourceSetBindingPlan GetGraphicsResourceSetBindingPlan(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout) {
        if (ReferenceEquals(this._graphicsBindingPlanCachePipeline, pipeline)
            && slot < (uint)this._graphicsBindingPlanCache.Length
            && this._graphicsBindingPlanCache[slot].Entries != null) {
            return this._graphicsBindingPlanCache[slot];
        }

        return this.GetGraphicsResourceSetBindingPlanSlow(pipeline, slot, layout);
    }

    private ResourceSetBindingPlan GetGraphicsResourceSetBindingPlanSlow(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout) {
        ResourceSetBindingPlanKey key = new(pipeline, layout, slot);
        if (!this._graphicsResourceSetBindingPlans.TryGetValue(key, out ResourceSetBindingPlan existingPlan)) {
            existingPlan = CreateGraphicsResourceSetBindingPlan(pipeline, slot, layout.Elements);
            this._graphicsResourceSetBindingPlans.Add(key, existingPlan);
        }

        if (!ReferenceEquals(this._graphicsBindingPlanCachePipeline, pipeline)) {
            this._graphicsBindingPlanCachePipeline = pipeline;
            Array.Clear(this._graphicsBindingPlanCache, 0, this._graphicsBindingPlanCache.Length);
        }

        Util.EnsureArrayMinimumSize(ref this._graphicsBindingPlanCache, slot + 1);
        this._graphicsBindingPlanCache[slot] = existingPlan;
        return existingPlan;
    }

    /// <summary>
    /// Gets the compute resource set binding plan value.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="layout">The resource layout used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ResourceSetBindingPlan GetComputeResourceSetBindingPlan(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout) {
        if (ReferenceEquals(this._computeBindingPlanCachePipeline, pipeline)
            && slot < (uint)this._computeBindingPlanCache.Length
            && this._computeBindingPlanCache[slot].Entries != null) {
            return this._computeBindingPlanCache[slot];
        }

        return this.GetComputeResourceSetBindingPlanSlow(pipeline, slot, layout);
    }

    private ResourceSetBindingPlan GetComputeResourceSetBindingPlanSlow(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout) {
        ResourceSetBindingPlanKey key = new(pipeline, layout, slot);
        if (!this._computeResourceSetBindingPlans.TryGetValue(key, out ResourceSetBindingPlan existingPlan)) {
            existingPlan = CreateComputeResourceSetBindingPlan(pipeline, slot, layout.Elements);
            this._computeResourceSetBindingPlans.Add(key, existingPlan);
        }

        if (!ReferenceEquals(this._computeBindingPlanCachePipeline, pipeline)) {
            this._computeBindingPlanCachePipeline = pipeline;
            Array.Clear(this._computeBindingPlanCache, 0, this._computeBindingPlanCache.Length);
        }

        Util.EnsureArrayMinimumSize(ref this._computeBindingPlanCache, slot + 1);
        this._computeBindingPlanCache[slot] = existingPlan;
        return existingPlan;
    }

    /// <summary>
    /// Creates the graphics resource set binding plan instance used by this backend.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="elements">The elements value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static ResourceSetBindingPlan CreateGraphicsResourceSetBindingPlan(D3D12Pipeline pipeline, uint slot, ResourceLayoutElementDescription[] elements) {
        List<ResourceSetBindingPlanEntry> plan = new(elements.Length);
        for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
            if (!pipeline.TryGetGraphicsRootBinding(slot, elementIndex, out D3D12Pipeline.RootBindingInfo bindingInfo)) {
                continue;
            }

            bool isDynamicBinding = (elements[elementIndex].Options & ResourceLayoutElementOptions.DynamicBinding) != 0;
            plan.Add(new ResourceSetBindingPlanEntry(elementIndex, bindingInfo, isDynamicBinding));
        }

        return new ResourceSetBindingPlan(plan.ToArray());
    }

    /// <summary>
    /// Creates the compute resource set binding plan instance used by this backend.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="elements">The elements value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static ResourceSetBindingPlan CreateComputeResourceSetBindingPlan(D3D12Pipeline pipeline, uint slot, ResourceLayoutElementDescription[] elements) {
        List<ResourceSetBindingPlanEntry> plan = new(elements.Length);

        for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++) {
            if (!pipeline.TryGetComputeRootBinding(slot, elementIndex, out D3D12Pipeline.RootBindingInfo bindingInfo)) {
                continue;
            }

            bool isDynamicBinding = (elements[elementIndex].Options & ResourceLayoutElementOptions.DynamicBinding) != 0;
            plan.Add(new ResourceSetBindingPlanEntry(elementIndex, bindingInfo, isDynamicBinding));
        }

        return new ResourceSetBindingPlan(plan.ToArray());
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
    /// Builds descriptor-table metadata for a cached binding plan.
    /// </summary>
    /// <param name="bindingPlan">The binding plan to inspect.</param>
    /// <param name="tableKind">The descriptor table kind.</param>
    /// <returns>The descriptor table metadata.</returns>
    private static DescriptorTableBindingInfo CreateDescriptorTableBindingInfo(ResourceSetBindingPlanEntry[] bindingPlan, D3D12Pipeline.DescriptorTableKind tableKind) {
        uint descriptorCount = 0;
        uint rootParameterIndex = 0;
        bool hasTable = false;
        uint hash = 2166136261u;

        for (int i = 0; i < bindingPlan.Length; i++) {
            D3D12Pipeline.RootBindingInfo bindingInfo = bindingPlan[i].BindingInfo;
            if (!bindingInfo.DescriptorTable || bindingInfo.DescriptorTableKind != tableKind) {
                continue;
            }

            hasTable = true;
            rootParameterIndex = bindingInfo.RootParameterIndex;
            descriptorCount = Math.Max(descriptorCount, bindingInfo.DescriptorTableOffset + 1);
            hash = (hash ^ bindingPlan[i].ElementIndex) * 16777619u;
            hash = (hash ^ bindingInfo.DescriptorTableOffset) * 16777619u;
            hash = (hash ^ (uint)bindingInfo.Kind) * 16777619u;
        }

        if (!hasTable) {
            return default;
        }

        hash = (hash ^ descriptorCount) * 16777619u;
        return new DescriptorTableBindingInfo(tableKind, descriptorCount, rootParameterIndex, hash);
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
    /// Marks a graphics resource set slot dirty.
    /// </summary>
    /// <param name="slot">The slot to mark.</param>
    private void MarkGraphicsResourceSetChanged(uint slot) {
        this.MarkResourceSetChanged(this._graphicsResourceSetsChanged, slot, ref this._graphicsResourceSetsDirty, ref this._graphicsResourceSetsChangedStart, ref this._graphicsResourceSetsChangedEnd);
    }

    /// <summary>
    /// Marks a compute resource set slot dirty.
    /// </summary>
    /// <param name="slot">The slot to mark.</param>
    private void MarkComputeResourceSetChanged(uint slot) {
        this.MarkResourceSetChanged(this._computeResourceSetsChanged, slot, ref this._computeResourceSetsDirty, ref this._computeResourceSetsChangedStart, ref this._computeResourceSetsChangedEnd);
    }

    /// <summary>
    /// Marks currently bound graphics resource sets dirty.
    /// </summary>
    /// <param name="resourceSetCount">The number of slots used by the active graphics pipeline.</param>
    private void MarkBoundGraphicsResourceSetsChanged(uint resourceSetCount) {
        this.MarkBoundResourceSetsChanged(this._boundGraphicsResourceSets, resourceSetCount, true);
    }

    /// <summary>
    /// Marks currently bound compute resource sets dirty.
    /// </summary>
    /// <param name="resourceSetCount">The number of slots used by the active compute pipeline.</param>
    private void MarkBoundComputeResourceSetsChanged(uint resourceSetCount) {
        this.MarkBoundResourceSetsChanged(this._boundComputeResourceSets, resourceSetCount, false);
    }

    /// <summary>
    /// Marks currently bound resource sets dirty.
    /// </summary>
    /// <param name="resourceSets">The bound resource set collection.</param>
    /// <param name="resourceSetCount">The number of slots used by the active pipeline.</param>
    /// <param name="graphics">Whether the graphics or compute range should be updated.</param>
    private void MarkBoundResourceSetsChanged(BoundResourceSetInfo[] resourceSets, uint resourceSetCount, bool graphics) {
        int changedLength = graphics ? this._graphicsResourceSetsChanged.Length : this._computeResourceSetsChanged.Length;
        int count = Math.Min(Math.Min(resourceSets.Length, changedLength), GetClampedResourceSetCount(resourceSetCount));
        for (uint slot = 0; slot < count; slot++) {
            if (resourceSets[slot].Set == null) {
                continue;
            }

            if (graphics) {
                this.MarkGraphicsResourceSetChanged(slot);
            }
            else {
                this.MarkComputeResourceSetChanged(slot);
            }
        }
    }

    /// <summary>
    /// Ensures graphics resource set arrays can hold the requested slot count.
    /// </summary>
    /// <param name="count">The minimum slot count.</param>
    private void EnsureGraphicsResourceSetCapacity(uint count) {
        Util.EnsureArrayMinimumSize(ref this._boundGraphicsResourceSets, count);
        Util.EnsureArrayMinimumSize(ref this._graphicsResourceSetsChanged, count);
    }

    /// <summary>
    /// Ensures compute resource set arrays can hold the requested slot count.
    /// </summary>
    /// <param name="count">The minimum slot count.</param>
    private void EnsureComputeResourceSetCapacity(uint count) {
        Util.EnsureArrayMinimumSize(ref this._boundComputeResourceSets, count);
        Util.EnsureArrayMinimumSize(ref this._computeResourceSetsChanged, count);
    }

    /// <summary>
    /// Gets the final graphics resource set slot that can be flushed for the active pipeline.
    /// </summary>
    /// <returns>The final slot index, or -1 when no slot can be flushed.</returns>
    private int GetGraphicsResourceSetFlushEnd() {
        if (this._currentGraphicsPipeline == null) {
            return -1;
        }

        int count = Math.Min(Math.Min(this._boundGraphicsResourceSets.Length, this._graphicsResourceSetsChanged.Length), GetClampedResourceSetCount(this._currentGraphicsPipeline.ResourceSetCount));
        return count - 1;
    }

    /// <summary>
    /// Gets the final compute resource set slot that can be flushed for the active pipeline.
    /// </summary>
    /// <returns>The final slot index, or -1 when no slot can be flushed.</returns>
    private int GetComputeResourceSetFlushEnd() {
        if (this._currentComputePipeline == null) {
            return -1;
        }

        int count = Math.Min(Math.Min(this._boundComputeResourceSets.Length, this._computeResourceSetsChanged.Length), GetClampedResourceSetCount(this._currentComputePipeline.ResourceSetCount));
        return count - 1;
    }

    /// <summary>
    /// Converts a pipeline resource set count to an array indexable count.
    /// </summary>
    /// <param name="count">The pipeline resource set count.</param>
    /// <returns>The count clamped to the maximum managed array length.</returns>
    private static int GetClampedResourceSetCount(uint count) {
        return count > int.MaxValue ? int.MaxValue : (int)count;
    }

    /// <summary>
    /// Marks a resource set slot dirty and expands the dirty range.
    /// </summary>
    /// <param name="changed">The dirty flag collection.</param>
    /// <param name="slot">The slot to mark.</param>
    /// <param name="dirty">The dirty state to update.</param>
    /// <param name="start">The first dirty slot.</param>
    /// <param name="end">The last dirty slot.</param>
    private void MarkResourceSetChanged(bool[] changed, uint slot, ref bool dirty, ref int start, ref int end) {
        int index = (int)slot;
        changed[index] = true;
        dirty = true;
        if (start < 0 || index < start) {
            start = index;
        }

        if (index > end) {
            end = index;
        }
    }

    /// <summary>
    /// Resets the graphics resource set dirty range.
    /// </summary>
    private void ResetGraphicsResourceSetChangeRange() {
        this._graphicsResourceSetsChangedStart = -1;
        this._graphicsResourceSetsChangedEnd = -1;
    }

    /// <summary>
    /// Resets the compute resource set dirty range.
    /// </summary>
    private void ResetComputeResourceSetChangeRange() {
        this._computeResourceSetsChangedStart = -1;
        this._computeResourceSetsChangedEnd = -1;
    }

    /// <summary>
    /// Clears cached vertex-buffer bindings from the previous recording.
    /// </summary>
    private void ClearBoundVertexBuffers() {
        int count = Math.Min((int)this._maxBoundVertexBufferSlot, this._boundVertexBuffers.Length);
        if (count <= 0) {
            return;
        }

        Array.Clear(this._boundVertexBuffers, 0, count);
        Array.Clear(this._boundVertexBufferOffsets, 0, count);
        Array.Clear(this._boundVertexBufferStrides, 0, count);
        Array.Clear(this._boundVertexBufferVersions, 0, count);
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
    /// Executes the transition swapchain back buffers to present logic for this backend.
    /// </summary>
    private void TransitionSwapchainBackBuffersToPresent() {
        if (this.Framebuffer is not D3D12SwapchainFramebuffer swapchainFramebuffer) {
            return;
        }

        if (this._transitionedBackBufferIndex < 0) {
            return;
        }

        if (ReferenceEquals(this._cachedSwapchainFramebuffer, swapchainFramebuffer)
            && this._cachedSwapchainBackBuffer != null
            && this._cachedSwapchainBackBufferIndex == this._transitionedBackBufferIndex) {
            this.Transition(this._cachedSwapchainBackBuffer, this._cachedSwapchainBackBufferState, ResourceStates.Present);
            swapchainFramebuffer.Swapchain.SetBackBufferState(this._cachedSwapchainBackBufferIndex, ResourceStates.Present);
            this._cachedSwapchainBackBufferState = ResourceStates.Present;
            return;
        }

        if (swapchainFramebuffer.Swapchain.TryGetCurrentBackBuffer(out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int currentIndex, out ResourceStates state) && currentIndex == this._transitionedBackBufferIndex) {
            this.CacheSwapchainBackBuffer(swapchainFramebuffer, backBuffer, rtv, currentIndex, state);
            this.Transition(backBuffer, state, ResourceStates.Present);
            swapchainFramebuffer.Swapchain.SetBackBufferState(currentIndex, ResourceStates.Present);
            this._cachedSwapchainBackBufferState = ResourceStates.Present;
        }
    }

    /// <summary>
    /// Gets the current swapchain back buffer, reusing the command-list cache when possible.
    /// </summary>
    private bool TryGetSwapchainBackBuffer(D3D12SwapchainFramebuffer swapchainFramebuffer, out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int index, out ResourceStates state) {
        if (ReferenceEquals(this._cachedSwapchainFramebuffer, swapchainFramebuffer)
            && this._cachedSwapchainBackBuffer != null
            && this._cachedSwapchainBackBufferIndex >= 0) {
            backBuffer = this._cachedSwapchainBackBuffer;
            rtv = this._cachedSwapchainRtv;
            index = this._cachedSwapchainBackBufferIndex;
            state = this._cachedSwapchainBackBufferState;
            return true;
        }

        if (!swapchainFramebuffer.Swapchain.TryGetCurrentBackBuffer(out backBuffer, out rtv, out index, out state)) {
            return false;
        }

        this.CacheSwapchainBackBuffer(swapchainFramebuffer, backBuffer, rtv, index, state);
        return true;
    }

    /// <summary>
    /// Stores the current swapchain back buffer for the active command-list recording.
    /// </summary>
    private void CacheSwapchainBackBuffer(D3D12SwapchainFramebuffer swapchainFramebuffer, ID3D12Resource backBuffer, CpuDescriptorHandle rtv, int index, ResourceStates state) {
        this._cachedSwapchainFramebuffer = swapchainFramebuffer;
        this._cachedSwapchainBackBuffer = backBuffer;
        this._cachedSwapchainRtv = rtv;
        this._cachedSwapchainBackBufferIndex = index;
        this._cachedSwapchainBackBufferState = state;
    }

    /// <summary>
    /// Clears cached swapchain back-buffer state for a new command-list recording.
    /// </summary>
    private void ClearCachedSwapchainBackBuffer() {
        this._cachedSwapchainFramebuffer = null;
        this._cachedSwapchainBackBuffer = null;
        this._cachedSwapchainRtv = default;
        this._cachedSwapchainBackBufferIndex = -1;
        this._cachedSwapchainBackBufferState = ResourceStates.Common;
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

    /// <summary>
    /// Waits for all GPU submissions that can still reference this command list's allocators and descriptor heaps.
    /// </summary>
    private void WaitForSubmittedFrameSlots() {
        for (int i = 0; i < this._frameSlotFenceValues.Length; i++) {
            ulong fenceValue = this._frameSlotFenceValues[i];
            if (fenceValue != 0) {
                this.gd.WaitForSubmissionFence(fenceValue);
                this._frameSlotFenceValues[i] = 0;
            }
        }
    }

    /// <summary>
    /// Defines the ID3DBlob interface.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("8BA5FB08-5195-40E2-AC58-0D989C3A0102")]
    private interface ID3DBlob {

        /// <summary>
        /// Gets the buffer pointer value.
        /// </summary>
        /// <returns>The value produced by this operation.</returns>
        [PreserveSig]
        IntPtr GetBufferPointer();
        
        /// <summary>
        /// Gets the buffer size value.
        /// </summary>
        /// <returns>The value produced by this operation.</returns>
        [PreserveSig]
        nuint GetBufferSize();
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
    /// Represents a cached resource set binding plan and its descriptor-table metadata.
    /// </summary>
    private readonly struct ResourceSetBindingPlan {

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSetBindingPlan" /> type.
        /// </summary>
        /// <param name="entries">The binding entries used by this plan.</param>
        public ResourceSetBindingPlan(ResourceSetBindingPlanEntry[] entries) {
            this.Entries = entries;
            this.SrvUavTable = CreateDescriptorTableBindingInfo(entries, D3D12Pipeline.DescriptorTableKind.SrvUav);
            this.SamplerTable = CreateDescriptorTableBindingInfo(entries, D3D12Pipeline.DescriptorTableKind.Sampler);
        }

        /// <summary>
        /// Gets or sets Entries.
        /// </summary>
        public ResourceSetBindingPlanEntry[] Entries { get; }

        /// <summary>
        /// Gets or sets SrvUavTable.
        /// </summary>
        public DescriptorTableBindingInfo SrvUavTable { get; }

        /// <summary>
        /// Gets or sets SamplerTable.
        /// </summary>
        public DescriptorTableBindingInfo SamplerTable { get; }
    }

    /// <summary>
    /// Represents cached metadata for one descriptor table in a resource set binding plan.
    /// </summary>
    private readonly struct DescriptorTableBindingInfo {

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorTableBindingInfo" /> type.
        /// </summary>
        /// <param name="tableKind">The descriptor table kind.</param>
        /// <param name="descriptorCount">The descriptor count required by the table.</param>
        /// <param name="rootParameterIndex">The root parameter index that owns the table.</param>
        /// <param name="signature">The descriptor table shape identifier.</param>
        public DescriptorTableBindingInfo(D3D12Pipeline.DescriptorTableKind tableKind, uint descriptorCount, uint rootParameterIndex, uint signature) {
            this.HasTable = true;
            this.TableKind = tableKind;
            this.DescriptorCount = descriptorCount;
            this.RootParameterIndex = rootParameterIndex;
            this.Signature = signature;
        }

        /// <summary>
        /// Gets or sets HasTable.
        /// </summary>
        public bool HasTable { get; }

        /// <summary>
        /// Gets or sets TableKind.
        /// </summary>
        public D3D12Pipeline.DescriptorTableKind TableKind { get; }

        /// <summary>
        /// Gets or sets DescriptorCount.
        /// </summary>
        public uint DescriptorCount { get; }

        /// <summary>
        /// Gets or sets RootParameterIndex.
        /// </summary>
        public uint RootParameterIndex { get; }

        /// <summary>
        /// Gets or sets Signature.
        /// </summary>
        public uint Signature { get; }
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
            unchecked {
                int hash = RuntimeHelpers.GetHashCode(obj.Pipeline);
                hash = (hash * 397) ^ RuntimeHelpers.GetHashCode(obj.Layout);
                hash = (hash * 397) ^ (int)obj.Slot;
                return hash;
            }
        }
    }
}
