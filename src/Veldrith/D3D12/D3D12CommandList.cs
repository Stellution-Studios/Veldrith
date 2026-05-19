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
/// Defines the behavior and responsibilities of the D3D12CommandList class.
/// </summary>
internal sealed class D3D12CommandList : CommandList {

    /// <summary>
    /// Stores the value associated with <c>FramesInFlight</c>.
    /// </summary>
    private const int FramesInFlight = 8;

    /// <summary>
    /// Stores the value associated with <c>PerfReportIntervalFrames</c>.
    /// </summary>
    private const int PerfReportIntervalFrames = 240;

    /// <summary>
    /// Executes the register operation.
    /// </summary>
    /// <param name="t0">Specifies the value of <paramref name="t0" />.</param>
    /// <returns>Returns the result produced by the register operation.</returns>
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
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="Ordinal">Specifies the value of <paramref name="Ordinal" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    private static readonly bool _perfLogEnabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PERF"), "1", StringComparison.Ordinal);

    /// <summary>
    /// Stores the value associated with <c>_activeScissorRects</c>.
    /// </summary>
    private readonly RawRect[] _activeScissorRects = new RawRect[16];

    /// <summary>
    /// Stores the value associated with <c>_activeViewports</c>.
    /// </summary>
    private readonly Vortice.Mathematics.Viewport[] _activeViewports = new Vortice.Mathematics.Viewport[16];

    /// <summary>
    /// Stores the value associated with <c>_beginEventMethod</c>.
    /// </summary>
    private readonly MethodInfo _beginEventMethod;

    /// <summary>
    /// Stores the value associated with <c>_boundDescriptorHeaps</c>.
    /// </summary>
    private readonly ID3D12DescriptorHeap[] _boundDescriptorHeaps = new ID3D12DescriptorHeap[2];

    /// <summary>
    /// Stores the value associated with <c>_boundVertexBufferOffsets</c>.
    /// </summary>
    private readonly uint[] _boundVertexBufferOffsets = new uint[16];

    /// <summary>
    /// Stores the value associated with <c>_boundVertexBuffers</c>.
    /// </summary>
    private readonly D3D12DeviceBuffer[] _boundVertexBuffers = new D3D12DeviceBuffer[16];

    /// <summary>
    /// Stores the value associated with <c>_boundVertexBufferVersions</c>.
    /// </summary>
    private readonly ulong[] _boundVertexBufferVersions = new ulong[16];

    /// <summary>
    /// Stores the value associated with <c>_commandAllocators</c>.
    /// </summary>
    private readonly ID3D12CommandAllocator[] _commandAllocators = new ID3D12CommandAllocator[FramesInFlight];

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="Instance">Specifies the value of <paramref name="Instance" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    private readonly Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlanEntry[]> _computeResourceSetBindingPlans = new(ResourceSetBindingPlanKeyComparer.Instance);

    /// <summary>
    /// Stores the value associated with <c>_descriptorTableCaches</c>.
    /// </summary>
    private readonly Dictionary<DescriptorCacheKey, GpuDescriptorHandle>[] _descriptorTableCaches = new Dictionary<DescriptorCacheKey, GpuDescriptorHandle>[FramesInFlight];

    /// <summary>
    /// Stores the value associated with <c>_endEventMethod</c>.
    /// </summary>
    private readonly MethodInfo _endEventMethod;

    /// <summary>
    /// Stores the value associated with <c>_frameSlotFenceValues</c>.
    /// </summary>
    private readonly ulong[] _frameSlotFenceValues = new ulong[FramesInFlight];

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="Instance">Specifies the value of <paramref name="Instance" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    private readonly Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlanEntry[]> _graphicsResourceSetBindingPlans = new(ResourceSetBindingPlanKeyComparer.Instance);

    /// <summary>
    /// Stores the value associated with <c>_maxSamplerDescriptors</c>.
    /// </summary>
    private readonly uint _maxSamplerDescriptors = 1024;

    /// <summary>
    /// Stores the value associated with <c>_maxSrvUavDescriptors</c>.
    /// </summary>
    private readonly uint _maxSrvUavDescriptors = 4096;

    /// <summary>
    /// Stores the value associated with <c>_nextSamplerDescriptorsPerFrameSlot</c>.
    /// </summary>
    private readonly uint[] _nextSamplerDescriptorsPerFrameSlot = new uint[FramesInFlight];

    /// <summary>
    /// Stores the value associated with <c>_nextSrvUavDescriptorsPerFrameSlot</c>.
    /// </summary>
    private readonly uint[] _nextSrvUavDescriptorsPerFrameSlot = new uint[FramesInFlight];

    /// <summary>
    /// Executes the StartNew operation.
    /// </summary>
    /// <returns>Returns the result produced by the StartNew operation.</returns>
    private readonly Stopwatch _perfStopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Stores the value associated with <c>_samplerDescriptorSize</c>.
    /// </summary>
    private readonly int _samplerDescriptorSize;

    /// <summary>
    /// Stores the value associated with <c>_setMarkerMethod</c>.
    /// </summary>
    private readonly MethodInfo _setMarkerMethod;

    /// <summary>
    /// Stores the value associated with <c>_shaderVisibleSamplerHeaps</c>.
    /// </summary>
    private readonly ID3D12DescriptorHeap[] _shaderVisibleSamplerHeaps = new ID3D12DescriptorHeap[FramesInFlight];

    /// <summary>
    /// Stores the value associated with <c>_shaderVisibleSrvUavHeaps</c>.
    /// </summary>
    private readonly ID3D12DescriptorHeap[] _shaderVisibleSrvUavHeaps = new ID3D12DescriptorHeap[FramesInFlight];

    /// <summary>
    /// Stores the value associated with <c>_singleBarrier</c>.
    /// </summary>
    private readonly ResourceBarrier[] _singleBarrier = new ResourceBarrier[1];

    /// <summary>
    /// Stores the value associated with <c>_srvUavDescriptorSize</c>.
    /// </summary>
    private readonly int _srvUavDescriptorSize;

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_activeScissorRectCount</c>.
    /// </summary>
    private uint _activeScissorRectCount;

    /// <summary>
    /// Stores the value associated with <c>_activeViewportCount</c>.
    /// </summary>
    private uint _activeViewportCount;

    /// <summary>
    /// Stores the value associated with <c>_begun</c>.
    /// </summary>
    private bool _begun;

    /// <summary>
    /// Stores the value associated with <c>_boundComputeResourceSets</c>.
    /// </summary>
    private BoundResourceSetInfo[] _boundComputeResourceSets = Array.Empty<BoundResourceSetInfo>();

    /// <summary>
    /// Stores the value associated with <c>_boundGraphicsResourceSets</c>.
    /// </summary>
    private BoundResourceSetInfo[] _boundGraphicsResourceSets = Array.Empty<BoundResourceSetInfo>();

    /// <summary>
    /// Stores the value associated with <c>_boundIndexBuffer</c>.
    /// </summary>
    private D3D12DeviceBuffer _boundIndexBuffer;

    /// <summary>
    /// Stores the value associated with <c>_boundIndexBufferOffset</c>.
    /// </summary>
    private uint _boundIndexBufferOffset;

    /// <summary>
    /// Stores the value associated with <c>_boundIndexBufferVersion</c>.
    /// </summary>
    private ulong _boundIndexBufferVersion;

    /// <summary>
    /// Stores the value associated with <c>_boundIndexFormat</c>.
    /// </summary>
    private IndexFormat _boundIndexFormat;

    /// <summary>
    /// Stores the value associated with <c>_computeRootBufferAddresses</c>.
    /// </summary>
    private ulong[] _computeRootBufferAddresses = Array.Empty<ulong>();

    /// <summary>
    /// Stores the value associated with <c>_computeRootBufferAddressValid</c>.
    /// </summary>
    private bool[] _computeRootBufferAddressValid = Array.Empty<bool>();

    /// <summary>
    /// Stores the value associated with <c>_computeRootTablePointers</c>.
    /// </summary>
    private ulong[] _computeRootTablePointers = Array.Empty<ulong>();

    /// <summary>
    /// Stores the value associated with <c>_computeRootTablePointerValid</c>.
    /// </summary>
    private bool[] _computeRootTablePointerValid = Array.Empty<bool>();

    /// <summary>
    /// Stores the value associated with <c>_currentComputePipeline</c>.
    /// </summary>
    private D3D12Pipeline _currentComputePipeline;

    /// <summary>
    /// Stores the value associated with <c>_currentFrameSlot</c>.
    /// </summary>
    private int _currentFrameSlot = -1;

    /// <summary>
    /// Stores the value associated with <c>_currentGraphicsPipeline</c>.
    /// </summary>
    private D3D12Pipeline _currentGraphicsPipeline;

    /// <summary>
    /// Stores the value associated with <c>_descriptorHeapsBound</c>.
    /// </summary>
    private bool _descriptorHeapsBound;

    /// <summary>
    /// Stores the value associated with <c>_dispatchIndirectSignature</c>.
    /// </summary>
    private ID3D12CommandSignature _dispatchIndirectSignature;

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the value associated with <c>_drawIndexedIndirectSignature</c>.
    /// </summary>
    private ID3D12CommandSignature _drawIndexedIndirectSignature;

    /// <summary>
    /// Stores the value associated with <c>_drawIndirectSignature</c>.
    /// </summary>
    private ID3D12CommandSignature _drawIndirectSignature;

    /// <summary>
    /// Stores the value associated with <c>_ended</c>.
    /// </summary>
    private bool _ended;

    /// <summary>
    /// Stores the value associated with <c>_gpuMipPipeline</c>.
    /// </summary>
    private D3D12Pipeline _gpuMipPipeline;

    /// <summary>
    /// Stores the value associated with <c>_gpuMipResourceLayout</c>.
    /// </summary>
    private ResourceLayout _gpuMipResourceLayout;

    /// <summary>
    /// Stores the value associated with <c>_gpuMipResourcesAvailable</c>.
    /// </summary>
    private bool _gpuMipResourcesAvailable;

    /// <summary>
    /// Stores the value associated with <c>_gpuMipResourcesInitialized</c>.
    /// </summary>
    private bool _gpuMipResourcesInitialized;

    /// <summary>
    /// Stores the value associated with <c>_gpuMipSampler</c>.
    /// </summary>
    private Sampler _gpuMipSampler;

    /// <summary>
    /// Stores the value associated with <c>_graphicsRootBufferAddresses</c>.
    /// </summary>
    private ulong[] _graphicsRootBufferAddresses = Array.Empty<ulong>();

    /// <summary>
    /// Stores the value associated with <c>_graphicsRootBufferAddressValid</c>.
    /// </summary>
    private bool[] _graphicsRootBufferAddressValid = Array.Empty<bool>();

    /// <summary>
    /// Stores the value associated with <c>_graphicsRootTablePointers</c>.
    /// </summary>
    private ulong[] _graphicsRootTablePointers = Array.Empty<ulong>();

    /// <summary>
    /// Stores the value associated with <c>_graphicsRootTablePointerValid</c>.
    /// </summary>
    private bool[] _graphicsRootTablePointerValid = Array.Empty<bool>();

    /// <summary>
    /// Stores the value associated with <c>_hasBoundIndexBuffer</c>.
    /// </summary>
    private bool _hasBoundIndexBuffer;

    /// <summary>
    /// Stores the value associated with <c>_indirectSignaturesAvailable</c>.
    /// </summary>
    private bool _indirectSignaturesAvailable;

    /// <summary>
    /// Stores the value associated with <c>_indirectSignaturesInitialized</c>.
    /// </summary>
    private bool _indirectSignaturesInitialized;

    /// <summary>
    /// Stores the value associated with <c>_maxBoundVertexBufferSlot</c>.
    /// </summary>
    private uint _maxBoundVertexBufferSlot;

    /// <summary>
    /// Stores the value associated with <c>_nextSamplerDescriptor</c>.
    /// </summary>
    private uint _nextSamplerDescriptor;

    /// <summary>
    /// Stores the value associated with <c>_nextSrvUavDescriptor</c>.
    /// </summary>
    private uint _nextSrvUavDescriptor;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumBeginWaitCount</c>.
    /// </summary>
    private ulong _perfAccumBeginWaitCount;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumBeginWaitMs</c>.
    /// </summary>
    private double _perfAccumBeginWaitMs;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumDescriptorCopies</c>.
    /// </summary>
    private ulong _perfAccumDescriptorCopies;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumDispatchCalls</c>.
    /// </summary>
    private ulong _perfAccumDispatchCalls;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumDrawCalls</c>.
    /// </summary>
    private ulong _perfAccumDrawCalls;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumIndexBufferBinds</c>.
    /// </summary>
    private ulong _perfAccumIndexBufferBinds;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumPipelineChanges</c>.
    /// </summary>
    private ulong _perfAccumPipelineChanges;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumResourceSetChanges</c>.
    /// </summary>
    private ulong _perfAccumResourceSetChanges;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumRootTableSets</c>.
    /// </summary>
    private ulong _perfAccumRootTableSets;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumSubresourceTransitions</c>.
    /// </summary>
    private ulong _perfAccumSubresourceTransitions;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumTransitions</c>.
    /// </summary>
    private ulong _perfAccumTransitions;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumUavBarriers</c>.
    /// </summary>
    private ulong _perfAccumUavBarriers;

    /// <summary>
    /// Stores the value associated with <c>_perfAccumVertexBufferBinds</c>.
    /// </summary>
    private ulong _perfAccumVertexBufferBinds;

    /// <summary>
    /// Stores the value associated with <c>_perfBeginWaitCount</c>.
    /// </summary>
    private ulong _perfBeginWaitCount;

    /// <summary>
    /// Stores the value associated with <c>_perfBeginWaitMs</c>.
    /// </summary>
    private double _perfBeginWaitMs;

    /// <summary>
    /// Stores the value associated with <c>_perfDescriptorCopies</c>.
    /// </summary>
    private ulong _perfDescriptorCopies;

    /// <summary>
    /// Stores the value associated with <c>_perfDispatchCalls</c>.
    /// </summary>
    private ulong _perfDispatchCalls;

    /// <summary>
    /// Stores the value associated with <c>_perfDrawCalls</c>.
    /// </summary>
    private ulong _perfDrawCalls;

    /// <summary>
    /// Stores the value associated with <c>_perfFrames</c>.
    /// </summary>
    private ulong _perfFrames;

    /// <summary>
    /// Stores the value associated with <c>_perfIndexBufferBinds</c>.
    /// </summary>
    private ulong _perfIndexBufferBinds;

    /// <summary>
    /// Stores the value associated with <c>_perfLastReportMs</c>.
    /// </summary>
    private double _perfLastReportMs;

    /// <summary>
    /// Stores the value associated with <c>_perfPipelineChanges</c>.
    /// </summary>
    private ulong _perfPipelineChanges;

    /// <summary>
    /// Stores the value associated with <c>_perfResourceSetChanges</c>.
    /// </summary>
    private ulong _perfResourceSetChanges;

    /// <summary>
    /// Stores the value associated with <c>_perfRootTableSets</c>.
    /// </summary>
    private ulong _perfRootTableSets;

    /// <summary>
    /// Stores the value associated with <c>_perfSubresourceTransitions</c>.
    /// </summary>
    private ulong _perfSubresourceTransitions;

    /// <summary>
    /// Stores the value associated with <c>_perfTransitions</c>.
    /// </summary>
    private ulong _perfTransitions;

    /// <summary>
    /// Stores the value associated with <c>_perfUavBarriers</c>.
    /// </summary>
    private ulong _perfUavBarriers;

    /// <summary>
    /// Stores the value associated with <c>_perfVertexBufferBinds</c>.
    /// </summary>
    private ulong _perfVertexBufferBinds;

    /// <summary>
    /// Stores the value associated with <c>_transitionedBackBufferIndex</c>.
    /// </summary>
    private int _transitionedBackBufferIndex = -1;

    /// <summary>
    /// Stores the value associated with <c>_uavBarrierPending</c>.
    /// </summary>
    private bool _uavBarrierPending;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12CommandList" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <param name="features">Specifies the value of <paramref name="features" />.</param>
    /// <param name="uniformAlignment">Specifies the value of <paramref name="uniformAlignment" />.</param>
    /// <param name="structuredAlignment">Specifies the value of <paramref name="structuredAlignment" />.</param>
    public D3D12CommandList(D3D12GraphicsDevice gd, ref CommandListDescription description, GraphicsDeviceFeatures features, uint uniformAlignment, uint structuredAlignment) : base(ref description, features, uniformAlignment, structuredAlignment) {
        this.gd = gd;

        for (int i = 0; i < FramesInFlight; i++) {
            this._commandAllocators[i] = gd.Device.CreateCommandAllocator(CommandListType.Direct);
            this._shaderVisibleSrvUavHeaps[i] = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, this._maxSrvUavDescriptors, DescriptorHeapFlags.ShaderVisible));
            this._shaderVisibleSamplerHeaps[i] = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.Sampler, this._maxSamplerDescriptors, DescriptorHeapFlags.ShaderVisible));
            this._descriptorTableCaches[i] = new Dictionary<DescriptorCacheKey, GpuDescriptorHandle>(DescriptorCacheKeyComparer.Instance);
        }

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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
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
            this._shaderVisibleSrvUavHeaps[i]?.Dispose();
            this._shaderVisibleSamplerHeaps[i]?.Dispose();
        }

        this._disposed = true;
    }

    /// <summary>
    /// Executes the Begin operation.
    /// </summary>
    public override void Begin() {
        if (_perfLogEnabled) {
            this._perfBeginWaitCount = 0;
            this._perfBeginWaitMs = 0;
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
        this._nextSrvUavDescriptor = this._nextSrvUavDescriptorsPerFrameSlot[this._currentFrameSlot];
        this._nextSamplerDescriptor = this._nextSamplerDescriptorsPerFrameSlot[this._currentFrameSlot];
        this._descriptorHeapsBound = false;
        this._activeViewportCount = 0;
        this._activeScissorRectCount = 0;
        this._uavBarrierPending = false;
        Array.Clear(this._boundVertexBuffers, 0, this._boundVertexBuffers.Length);
        Array.Clear(this._boundVertexBufferOffsets, 0, this._boundVertexBufferOffsets.Length);
        Array.Clear(this._boundVertexBufferVersions, 0, this._boundVertexBufferVersions.Length);
        this._boundIndexBuffer = null;
        this._boundIndexBufferOffset = 0;
        this._boundIndexBufferVersion = 0;
        this._boundIndexFormat = IndexFormat.UInt16;
        this._hasBoundIndexBuffer = false;
        ClearBoundResourceSets(this._boundGraphicsResourceSets);
        ClearBoundResourceSets(this._boundComputeResourceSets);
        this.InvalidateGraphicsRootCaches();
        this.InvalidateComputeRootCaches();
        this._maxBoundVertexBufferSlot = 0;
        this.ClearCachedState();
        this._currentGraphicsPipeline = null;
        this._currentComputePipeline = null;
    }

    /// <summary>
    /// Executes the End operation.
    /// </summary>
    public override void End() {
        if (!this._begun) {
            throw new VeldridException("CommandList.End cannot be called before Begin.");
        }

        this.FlushPendingUavBarrier();
        this.TransitionSwapchainBackBuffersToPresent();
        this.NativeCommandList.Close();
        this._ended = true;

        if (_perfLogEnabled) {
            this._perfFrames++;
            this._perfAccumBeginWaitCount += this._perfBeginWaitCount;
            this._perfAccumBeginWaitMs += this._perfBeginWaitMs;
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

            if (this._perfFrames % PerfReportIntervalFrames == 0) {
                double elapsedMs = this._perfStopwatch.Elapsed.TotalMilliseconds;
                double reportWindowMs = elapsedMs - this._perfLastReportMs;
                this._perfLastReportMs = elapsedMs;
                double invFrames = 1.0 / PerfReportIntervalFrames;
                Console.WriteLine($"[D3D12 PERF] {PerfReportIntervalFrames}f/{reportWindowMs:F0}ms avg: " + $"wait={this._perfAccumBeginWaitMs * invFrames:F3}ms ({this._perfAccumBeginWaitCount * invFrames:F2}x), " + $"trans={this._perfAccumTransitions * invFrames:F1}, subTrans={this._perfAccumSubresourceTransitions * invFrames:F1}, uavB={this._perfAccumUavBarriers * invFrames:F1}, " + $"pso={this._perfAccumPipelineChanges * invFrames:F1}, rs={this._perfAccumResourceSetChanges * invFrames:F1}, " + $"descCopy={this._perfAccumDescriptorCopies * invFrames:F1}, rootTbl={this._perfAccumRootTableSets * invFrames:F1}, " + $"vb={this._perfAccumVertexBufferBinds * invFrames:F1}, ib={this._perfAccumIndexBufferBinds * invFrames:F1}, " + $"draw={this._perfAccumDrawCalls * invFrames:F1}, dispatch={this._perfAccumDispatchCalls * invFrames:F1}");

                this._perfAccumBeginWaitCount = 0;
                this._perfAccumBeginWaitMs = 0;
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
            }
        }
    }

    /// <summary>
    /// Executes the SetViewport operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="viewport">Specifies the value of <paramref name="viewport" />.</param>
    public override void SetViewport(uint index, ref Viewport viewport) {
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
    /// Executes the SetScissorRect operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height) {
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
    /// Executes the Dispatch operation.
    /// </summary>
    /// <param name="groupCountX">Specifies the value of <paramref name="groupCountX" />.</param>
    /// <param name="groupCountY">Specifies the value of <paramref name="groupCountY" />.</param>
    /// <param name="groupCountZ">Specifies the value of <paramref name="groupCountZ" />.</param>
    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ) {
        this.FlushPendingUavBarrier();
        this.NativeCommandList.Dispatch(groupCountX, groupCountY, groupCountZ);
        if (_perfLogEnabled) {
            this._perfDispatchCalls++;
        }

        this._uavBarrierPending = true;
    }

    /// <summary>
    /// Executes the ExecuteNoSignal operation.
    /// </summary>
    internal void ExecuteNoSignal() {
        if (!this._ended) {
            throw new VeldridException("CommandList must be ended before submit.");
        }

        this.gd.CommandQueue.ExecuteCommandList(this.NativeCommandList);
    }

    /// <summary>
    /// Executes the MarkSubmitted operation.
    /// </summary>
    /// <param name="signalValue">Specifies the value of <paramref name="signalValue" />.</param>
    internal void MarkSubmitted(ulong signalValue) {
        if (this._currentFrameSlot >= 0) {
            this._frameSlotFenceValues[this._currentFrameSlot] = signalValue;
        }
    }

    /// <summary>
    /// Executes the SetGraphicsResourceSetCore operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="rs">Specifies the value of <paramref name="rs" />.</param>
    /// <param name="dynamicOffsetsCount">Specifies the value of <paramref name="dynamicOffsetsCount" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
    protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
        if (this._currentGraphicsPipeline == null) {
            return;
        }

        Util.EnsureArrayMinimumSize(ref this._boundGraphicsResourceSets, slot + 1);
        BoundResourceSetInfo previousBinding = this._boundGraphicsResourceSets[slot];
        if (previousBinding.Equals(rs, dynamicOffsetsCount, ref dynamicOffsets)) {
            return;
        }

        this._boundGraphicsResourceSets[slot].Offsets.Dispose();
        this._boundGraphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
        if (_perfLogEnabled) {
            this._perfResourceSetChanges++;
        }

        D3D12ResourceSet d3d12Set = Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(rs);
        IBindableResource[] resources = d3d12Set.BoundResources;
        ResourceSetBindingPlanEntry[] bindingPlan = this.GetGraphicsResourceSetBindingPlan(this._currentGraphicsPipeline, slot, d3d12Set.ResourceLayoutInfo);
        uint dynamicOffsetIndex = 0;
        bool bindOnlyDynamicResources = ReferenceEquals(previousBinding.Set, rs);

        for (int i = 0; i < bindingPlan.Length; i++) {
            ref readonly ResourceSetBindingPlanEntry bindingEntry = ref bindingPlan[i];
            if (bindOnlyDynamicResources && !bindingEntry.IsDynamicBinding) {
                continue;
            }

            uint dynamicOffset = 0;
            if (bindingEntry.IsDynamicBinding) {
                if (dynamicOffsetIndex >= dynamicOffsetsCount) {
                    throw new VeldridException("Insufficient dynamic offsets provided for ResourceSet binding.");
                }

                dynamicOffset = Unsafe.Add(ref dynamicOffsets, (int)dynamicOffsetIndex);
                dynamicOffsetIndex++;
            }

            IBindableResource resource = resources[bindingEntry.ElementIndex];
            this.BindGraphicsResource(bindingEntry.BindingInfo, resource, dynamicOffset);
        }
    }

    /// <summary>
    /// Executes the SetComputeResourceSetCore operation.
    /// </summary>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="set">Specifies the value of <paramref name="set" />.</param>
    /// <param name="dynamicOffsetsCount">Specifies the value of <paramref name="dynamicOffsetsCount" />.</param>
    /// <param name="dynamicOffsets">Specifies the value of <paramref name="dynamicOffsets" />.</param>
    protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
        if (this._currentComputePipeline == null) {
            return;
        }

        Util.EnsureArrayMinimumSize(ref this._boundComputeResourceSets, slot + 1);
        BoundResourceSetInfo previousBinding = this._boundComputeResourceSets[slot];
        if (previousBinding.Equals(set, dynamicOffsetsCount, ref dynamicOffsets)) {
            return;
        }

        this._boundComputeResourceSets[slot].Offsets.Dispose();
        this._boundComputeResourceSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetsCount, ref dynamicOffsets);
        if (_perfLogEnabled) {
            this._perfResourceSetChanges++;
        }

        D3D12ResourceSet d3d12Set = Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(set);
        IBindableResource[] resources = d3d12Set.BoundResources;
        ResourceSetBindingPlanEntry[] bindingPlan = this.GetComputeResourceSetBindingPlan(this._currentComputePipeline, slot, d3d12Set.ResourceLayoutInfo);
        uint dynamicOffsetIndex = 0;
        bool bindOnlyDynamicResources = ReferenceEquals(previousBinding.Set, set);

        for (int i = 0; i < bindingPlan.Length; i++) {
            ref readonly ResourceSetBindingPlanEntry bindingEntry = ref bindingPlan[i];
            if (bindOnlyDynamicResources && !bindingEntry.IsDynamicBinding) {
                continue;
            }

            uint dynamicOffset = 0;
            if (bindingEntry.IsDynamicBinding) {
                if (dynamicOffsetIndex >= dynamicOffsetsCount) {
                    throw new VeldridException("Insufficient dynamic offsets provided for ResourceSet binding.");
                }

                dynamicOffset = Unsafe.Add(ref dynamicOffsets, (int)dynamicOffsetIndex);
                dynamicOffsetIndex++;
            }

            IBindableResource resource = resources[bindingEntry.ElementIndex];
            this.BindComputeResource(bindingEntry.BindingInfo, resource, dynamicOffset);
        }
    }

    /// <summary>
    /// Executes the SetFramebufferCore operation.
    /// </summary>
    /// <param name="fb">Specifies the value of <paramref name="fb" />.</param>
    protected override void SetFramebufferCore(Framebuffer fb) {
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
    /// Executes the DrawIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="drawCount">Specifies the value of <paramref name="drawCount" />.</param>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
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
    }

    /// <summary>
    /// Executes the DrawIndexedIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="drawCount">Specifies the value of <paramref name="drawCount" />.</param>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
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
    }

    /// <summary>
    /// Executes the DispatchIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">Specifies the value of <paramref name="indirectBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset) {
        D3D12DeviceBuffer d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(indirectBuffer);
        uint argumentSize = (uint)Unsafe.SizeOf<IndirectDispatchArguments>();
        ulong requiredSize = (ulong)offset + argumentSize;
        if (requiredSize > d3d12Buffer.SizeInBytes) {
            throw new VeldridException("Indirect dispatch argument range exceeds buffer bounds.");
        }

        if (this.EnsureIndirectCommandSignatures()) {
            this.ExecuteIndirect(d3d12Buffer, offset, 1, argumentSize, argumentSize, this._dispatchIndirectSignature);
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
    }

    /// <summary>
    /// Executes the ResolveTextureCore operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    protected override void ResolveTextureCore(Texture source, Texture destination) {
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
    /// Executes the CopyBufferCore operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sourceOffset">Specifies the value of <paramref name="sourceOffset" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    /// <param name="destinationOffset">Specifies the value of <paramref name="destinationOffset" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes) {
        this.FlushPendingUavBarrier();
        D3D12DeviceBuffer src = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(source);
        D3D12DeviceBuffer dst = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(destination);
        src.CopyTo(this.NativeCommandList, dst, sourceOffset, destinationOffset, sizeInBytes);
    }

    /// <summary>
    /// Executes the CopyTextureCore operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="srcX">Specifies the value of <paramref name="srcX" />.</param>
    /// <param name="srcY">Specifies the value of <paramref name="srcY" />.</param>
    /// <param name="srcZ">Specifies the value of <paramref name="srcZ" />.</param>
    /// <param name="srcMipLevel">Specifies the value of <paramref name="srcMipLevel" />.</param>
    /// <param name="srcBaseArrayLayer">Specifies the value of <paramref name="srcBaseArrayLayer" />.</param>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    /// <param name="dstX">Specifies the value of <paramref name="dstX" />.</param>
    /// <param name="dstY">Specifies the value of <paramref name="dstY" />.</param>
    /// <param name="dstZ">Specifies the value of <paramref name="dstZ" />.</param>
    /// <param name="dstMipLevel">Specifies the value of <paramref name="dstMipLevel" />.</param>
    /// <param name="dstBaseArrayLayer">Specifies the value of <paramref name="dstBaseArrayLayer" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="layerCount">Specifies the value of <paramref name="layerCount" />.</param>
    protected override void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount) {
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
    /// Executes the SetPipelineCore operation.
    /// </summary>
    /// <param name="pipeline">Specifies the value of <paramref name="pipeline" />.</param>
    private protected override void SetPipelineCore(Pipeline pipeline) {
        if (pipeline.IsComputePipeline) {
            D3D12Pipeline d3D12ComputePipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline);
            if (ReferenceEquals(this._currentComputePipeline, d3D12ComputePipeline)) {
                return;
            }

            bool computeRootSignatureChanged = !ReferenceEquals(this._currentComputePipeline?.RootSignature, d3D12ComputePipeline.RootSignature);
            this._currentComputePipeline = d3D12ComputePipeline;
            this._currentGraphicsPipeline = null;

            if (_perfLogEnabled) {
                this._perfPipelineChanges++;
            }

            this.NativeCommandList.SetPipelineState(d3D12ComputePipeline.PipelineState);
            if (computeRootSignatureChanged) {
                ClearBoundResourceSets(this._boundComputeResourceSets);
                this.InvalidateComputeRootCaches();
                this.NativeCommandList.SetComputeRootSignature(d3D12ComputePipeline.RootSignature);
            }

            return;
        }

        D3D12Pipeline d3D12Pipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline);
        if (ReferenceEquals(this._currentGraphicsPipeline, d3D12Pipeline)) {
            return;
        }

        bool rootSignatureChanged = !ReferenceEquals(this._currentGraphicsPipeline?.RootSignature, d3D12Pipeline.RootSignature);
        this._currentGraphicsPipeline = d3D12Pipeline;
        this._currentComputePipeline = null;
        if (_perfLogEnabled) {
            this._perfPipelineChanges++;
        }

        this.NativeCommandList.SetPipelineState(d3D12Pipeline.PipelineState);
        if (rootSignatureChanged) {
            ClearBoundResourceSets(this._boundGraphicsResourceSets);
            this.InvalidateGraphicsRootCaches();
            this.NativeCommandList.SetGraphicsRootSignature(d3D12Pipeline.RootSignature);
        }

        this.NativeCommandList.IASetPrimitiveTopology(d3D12Pipeline.PrimitiveTopology);
        this.RebindVertexBuffersForCurrentPipeline();
    }

    /// <summary>
    /// Executes the SetVertexBufferCore operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
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
    /// Executes the SetIndexBufferCore operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
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
    /// Executes the ClearColorTargetCore operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="clearColor">Specifies the value of <paramref name="clearColor" />.</param>
    private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor) {
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
    /// Executes the ClearDepthStencilCore operation.
    /// </summary>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="stencil">Specifies the value of <paramref name="stencil" />.</param>
    private protected override void ClearDepthStencilCore(float depth, byte stencil) {
        this.FlushPendingUavBarrier();
        if (this.Framebuffer is not D3D12Framebuffer d3D12Framebuffer) {
            return;
        }

        if (d3D12Framebuffer.DepthTargetTexture == null) {
            return;
        }

        this.TransitionTexture(d3D12Framebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
        if (d3D12Framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv)) {
            this.NativeCommandList.ClearDepthStencilView(dsv, ClearFlags.Depth | ClearFlags.Stencil, depth, stencil, 0, null!);
        }
    }

    /// <summary>
    /// Executes the DrawCore operation.
    /// </summary>
    /// <param name="vertexCount">Specifies the value of <paramref name="vertexCount" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    /// <param name="vertexStart">Specifies the value of <paramref name="vertexStart" />.</param>
    /// <param name="instanceStart">Specifies the value of <paramref name="instanceStart" />.</param>
    private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart) {
        this.FlushPendingUavBarrier();
        this.NativeCommandList.DrawInstanced(vertexCount, instanceCount, vertexStart, instanceStart);
        if (_perfLogEnabled) {
            this._perfDrawCalls++;
        }
    }

    /// <summary>
    /// Executes the DrawIndexedCore operation.
    /// </summary>
    /// <param name="indexCount">Specifies the value of <paramref name="indexCount" />.</param>
    /// <param name="instanceCount">Specifies the value of <paramref name="instanceCount" />.</param>
    /// <param name="indexStart">Specifies the value of <paramref name="indexStart" />.</param>
    /// <param name="vertexOffset">Specifies the value of <paramref name="vertexOffset" />.</param>
    /// <param name="instanceStart">Specifies the value of <paramref name="instanceStart" />.</param>
    private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart) {
        this.FlushPendingUavBarrier();
        this.NativeCommandList.DrawIndexedInstanced(indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
        if (_perfLogEnabled) {
            this._perfDrawCalls++;
        }
    }

    /// <summary>
    /// Executes the UpdateBufferCore operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="bufferOffsetInBytes">Specifies the value of <paramref name="bufferOffsetInBytes" />.</param>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        this.FlushPendingUavBarrier();
        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
        ID3D12Resource temporaryUpload = d3D12Buffer.Update(this.NativeCommandList, source, bufferOffsetInBytes, sizeInBytes);
        if (temporaryUpload != null) {
            this.gd.DisposeWhenIdle(temporaryUpload);
        }
    }

    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Executes the GenerateMipmapsCore operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
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
    /// Executes the PushDebugGroupCore operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    private protected override void PushDebugGroupCore(string name) {
        this.WriteDebugMarker(name, true, false);
    }

    /// <summary>
    /// Executes the PopDebugGroupCore operation.
    /// </summary>
    private protected override void PopDebugGroupCore() {
        this._endEventMethod?.Invoke(this.NativeCommandList, null);
    }

    /// <summary>
    /// Executes the InsertDebugMarkerCore operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    private protected override void InsertDebugMarkerCore(string name) {
        this.WriteDebugMarker(name, false, true);
    }

    /// <summary>
    /// Executes the Transition operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="from">Specifies the value of <paramref name="from" />.</param>
    /// <param name="to">Specifies the value of <paramref name="to" />.</param>
    private void Transition(ID3D12Resource resource, ResourceStates from, ResourceStates to) {
        if (from == to) {
            return;
        }

        ResourceBarrier barrier = ResourceBarrier.BarrierTransition(resource, from, to);
        this._singleBarrier[0] = barrier;
        this.NativeCommandList.ResourceBarrier(this._singleBarrier);
        if (_perfLogEnabled) {
            this._perfTransitions++;
        }
    }

    /// <summary>
    /// Executes the BindVertexBuffer operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    private void BindVertexBuffer(uint index, D3D12DeviceBuffer buffer, uint offset) {
        this.TransitionBuffer(buffer, ResourceStates.VertexAndConstantBuffer);

        uint stride = 0;
        if (this._currentGraphicsPipeline != null && index < this._currentGraphicsPipeline.VertexStrides.Length) {
            stride = this._currentGraphicsPipeline.VertexStrides[index];
        }

        uint viewSize = buffer.GetBindableSize(offset);
        VertexBufferView view = new(buffer.GetGpuVirtualAddress(offset), viewSize, stride);
        this.NativeCommandList.IASetVertexBuffers(index, view);
    }

    /// <summary>
    /// Executes the RebindVertexBuffersForCurrentPipeline operation.
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

            this.BindVertexBuffer(index, buffer, this._boundVertexBufferOffsets[index]);
        }
    }

    /// <summary>
    /// Executes the TransitionSubresource operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="from">Specifies the value of <paramref name="from" />.</param>
    /// <param name="to">Specifies the value of <paramref name="to" />.</param>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    private void TransitionSubresource(ID3D12Resource resource, ResourceStates from, ResourceStates to, uint subresource) {
        if (from == to) {
            return;
        }

        ResourceBarrier barrier = ResourceBarrier.BarrierTransition(resource, from, to, subresource);
        this._singleBarrier[0] = barrier;
        this.NativeCommandList.ResourceBarrier(this._singleBarrier);
        if (_perfLogEnabled) {
            this._perfSubresourceTransitions++;
        }
    }

    /// <summary>
    /// Executes the FlushPendingUavBarrier operation.
    /// </summary>
    private void FlushPendingUavBarrier() {
        if (!this._uavBarrierPending) {
            return;
        }

        ResourceBarrier barrier = ResourceBarrier.BarrierUnorderedAccessView(null);
        this._singleBarrier[0] = barrier;
        this.NativeCommandList.ResourceBarrier(this._singleBarrier);
        if (_perfLogEnabled) {
            this._perfUavBarriers++;
        }

        this._uavBarrierPending = false;
    }

    /// <summary>
    /// Executes the WaitForFrameSlot operation.
    /// </summary>
    /// <param name="frameSlot">Specifies the value of <paramref name="frameSlot" />.</param>
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
            this._perfBeginWaitMs += elapsedTicks * 1000.0 / Stopwatch.Frequency;
            this._perfBeginWaitCount++;
        }
    }

    /// <summary>
    /// Executes the ExecuteIndirect operation.
    /// </summary>
    /// <param name="argumentBuffer">Specifies the value of <paramref name="argumentBuffer" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="drawCount">Specifies the value of <paramref name="drawCount" />.</param>
    /// <param name="stride">Specifies the value of <paramref name="stride" />.</param>
    /// <param name="argumentSize">Specifies the value of <paramref name="argumentSize" />.</param>
    /// <param name="signature">Specifies the value of <paramref name="signature" />.</param>
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
    /// Executes the EnsureIndirectCommandSignatures operation.
    /// </summary>
    /// <returns>Returns the result produced by the EnsureIndirectCommandSignatures operation.</returns>
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
    /// Executes the CreateCommandSignature operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateCommandSignature operation.</returns>
    private ID3D12CommandSignature CreateCommandSignature(CommandSignatureDescription description) {
        ID3D12CommandSignature signature = this.gd.Device.CreateCommandSignature<ID3D12CommandSignature>(description, null);

        if (signature == null) {
            throw new VeldridException("Unable to create D3D12 command signature.");
        }

        return signature;
    }

    /// <summary>
    /// Executes the CanUseGpuMipmapPath operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <returns>Returns the result produced by the CanUseGpuMipmapPath operation.</returns>
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
    /// Executes the GenerateMipmapsGpu operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
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
            }
            else if (previousGraphics != null) {
                this.NativeCommandList.SetPipelineState(previousGraphics.PipelineState);
                this.NativeCommandList.SetGraphicsRootSignature(previousGraphics.RootSignature);
                this.NativeCommandList.IASetPrimitiveTopology(previousGraphics.PrimitiveTopology);
                this._currentGraphicsPipeline = previousGraphics;
                this._currentComputePipeline = null;
            }
            else {
                this._currentComputePipeline = null;
                this._currentGraphicsPipeline = null;
            }
        }
    }

    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Executes the EnsureGpuMipmapResources operation.
    /// </summary>
    /// <returns>Returns the result produced by the EnsureGpuMipmapResources operation.</returns>
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
    /// Executes the CompileComputeShader operation.
    /// </summary>
    /// <param name="sourceCode">Specifies the value of <paramref name="sourceCode" />.</param>
    /// <param name="entryPoint">Specifies the value of <paramref name="entryPoint" />.</param>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <returns>Returns the result produced by the CompileComputeShader operation.</returns>
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
    /// Executes the TransitionTexture operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <param name="toState">Specifies the value of <paramref name="toState" />.</param>
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
    /// Executes the TransitionTextureView operation.
    /// </summary>
    /// <param name="textureView">Specifies the value of <paramref name="textureView" />.</param>
    /// <param name="toState">Specifies the value of <paramref name="toState" />.</param>
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
    /// Executes the TransitionBuffer operation.
    /// </summary>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <param name="toState">Specifies the value of <paramref name="toState" />.</param>
    private void TransitionBuffer(D3D12DeviceBuffer buffer, ResourceStates toState) {
        if (!buffer.CanTransitionState || buffer.CurrentState == toState) {
            return;
        }

        this.Transition(buffer.NativeBuffer, buffer.CurrentState, toState);
        buffer.CurrentState = toState;
    }

    /// <summary>
    /// Executes the CaptureTextureStates operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <returns>Returns the result produced by the CaptureTextureStates operation.</returns>
    private static ResourceStates[] CaptureTextureStates(D3D12Texture texture) {
        uint subresourceCount = texture.SubresourceCount;
        ResourceStates[] states = new ResourceStates[subresourceCount];
        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            states[subresource] = texture.GetSubresourceState(subresource);
        }

        return states;
    }

    /// <summary>
    /// Executes the RestoreTextureStates operation.
    /// </summary>
    /// <param name="texture">Specifies the value of <paramref name="texture" />.</param>
    /// <param name="previousStates">Specifies the value of <paramref name="previousStates" />.</param>
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
    /// Executes the WriteDebugMarker operation.
    /// </summary>
    /// <param name="name">Specifies the value of <paramref name="name" />.</param>
    /// <param name="beginEvent">Specifies the value of <paramref name="beginEvent" />.</param>
    /// <param name="setMarker">Specifies the value of <paramref name="setMarker" />.</param>
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
    /// Executes the GetDebugMarkerMethod operation.
    /// </summary>
    /// <param name="methodName">Specifies the value of <paramref name="methodName" />.</param>
    /// <returns>Returns the result produced by the GetDebugMarkerMethod operation.</returns>
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
    /// Executes the D3DCompile operation.
    /// </summary>
    /// <param name="srcData">Specifies the value of <paramref name="srcData" />.</param>
    /// <param name="srcDataSize">Specifies the value of <paramref name="srcDataSize" />.</param>
    /// <param name="sourceName">Specifies the value of <paramref name="sourceName" />.</param>
    /// <param name="defines">Specifies the value of <paramref name="defines" />.</param>
    /// <param name="include">Specifies the value of <paramref name="include" />.</param>
    /// <param name="entryPoint">Specifies the value of <paramref name="entryPoint" />.</param>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <param name="flags1">Specifies the value of <paramref name="flags1" />.</param>
    /// <param name="flags2">Specifies the value of <paramref name="flags2" />.</param>
    /// <param name="code">Specifies the value of <paramref name="code" />.</param>
    /// <param name="errorMsgs">Specifies the value of <paramref name="errorMsgs" />.</param>
    /// <returns>Returns the result produced by the D3DCompile operation.</returns>
    private static extern int D3DCompile(byte[] srcData, nuint srcDataSize, string sourceName, IntPtr defines, IntPtr include, string entryPoint, string target, uint flags1, uint flags2, out IntPtr code, out IntPtr errorMsgs);

    /// <summary>
    /// Executes the BindGraphicsResource operation.
    /// </summary>
    /// <param name="bindingInfo">Specifies the value of <paramref name="bindingInfo" />.</param>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="dynamicOffset">Specifies the value of <paramref name="dynamicOffset" />.</param>
    private void BindGraphicsResource(D3D12Pipeline.RootBindingInfo bindingInfo, IBindableResource resource, uint dynamicOffset) {

        if (bindingInfo.DescriptorTable) {
            this.BindDescriptorTableResource(bindingInfo, resource, false);
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
    /// Executes the BindComputeResource operation.
    /// </summary>
    /// <param name="bindingInfo">Specifies the value of <paramref name="bindingInfo" />.</param>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="dynamicOffset">Specifies the value of <paramref name="dynamicOffset" />.</param>
    private void BindComputeResource(D3D12Pipeline.RootBindingInfo bindingInfo, IBindableResource resource, uint dynamicOffset) {
        if (bindingInfo.DescriptorTable) {
            this.BindDescriptorTableResource(bindingInfo, resource, true);
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
    /// Executes the BindDescriptorTableResource operation.
    /// </summary>
    /// <param name="bindingInfo">Specifies the value of <paramref name="bindingInfo" />.</param>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="compute">Specifies the value of <paramref name="compute" />.</param>
    private void BindDescriptorTableResource(D3D12Pipeline.RootBindingInfo bindingInfo, IBindableResource resource, bool compute) {
        this.BindDescriptorHeaps();

        switch (bindingInfo.Kind) {
            case ResourceKind.TextureReadOnly: {
                    TextureView textureView = Util.GetTextureView(this.gd, resource);
                    D3D12TextureView d3d12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                    this.ValidateTextureViewBindingSupport(d3d12TextureView, TextureUsage.Sampled, "sampled");
                    ResourceStates readState = compute ? ResourceStates.NonPixelShaderResource : ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource;
                    this.TransitionTextureView(d3d12TextureView, readState);
                    ID3D12Resource nativeTexture = d3d12TextureView.TargetTexture.NativeTexture ?? throw new PlatformNotSupportedException("Texture has no native D3D12 resource.");
                    GpuDescriptorHandle gpuHandle = this.GetOrCreateDescriptorTableHandle(resource, bindingInfo.Kind, () => {
                        this.AllocateSrvUavDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle allocatedGpuHandle);
                        CpuDescriptorHandle sourceSrv = d3d12TextureView.GetOrCreateShaderResourceViewDescriptor();
                        this.gd.Device.CopyDescriptorsSimple(1u, cpuHandle, sourceSrv, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
                        return allocatedGpuHandle;
                    });
                    if ((compute && this.IsSameComputeRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr))
                        || (!compute && this.IsSameGraphicsRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr))) {
                        break;
                    }

                    if (compute) {
                        this.NativeCommandList.SetComputeRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        this.SetComputeRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }
                    else {
                        this.NativeCommandList.SetGraphicsRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        this.SetGraphicsRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }

                    if (_perfLogEnabled) {
                        this._perfRootTableSets++;
                    }

                    break;
                }
            case ResourceKind.TextureReadWrite: {
                    TextureView textureView = Util.GetTextureView(this.gd, resource);
                    D3D12TextureView d3D12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                    this.ValidateTextureViewBindingSupport(d3D12TextureView, TextureUsage.Storage, "storage");
                    this.TransitionTextureView(d3D12TextureView, ResourceStates.UnorderedAccess);
                    ID3D12Resource nativeTexture = d3D12TextureView.TargetTexture.NativeTexture ?? throw new PlatformNotSupportedException("Texture has no native D3D12 resource.");
                    GpuDescriptorHandle gpuHandle = this.GetOrCreateDescriptorTableHandle(resource, bindingInfo.Kind, () => {
                        this.AllocateSrvUavDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle allocatedGpuHandle);

                        CpuDescriptorHandle sourceUav = d3D12TextureView.GetOrCreateUnorderedAccessViewDescriptor();
                        this.gd.Device.CopyDescriptorsSimple(1u, cpuHandle, sourceUav, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
                        return allocatedGpuHandle;
                    });
                    if ((compute && this.IsSameComputeRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr)) || (!compute && this.IsSameGraphicsRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr))) {
                        break;
                    }

                    if (compute) {
                        this.NativeCommandList.SetComputeRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        this.SetComputeRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }
                    else {
                        this.NativeCommandList.SetGraphicsRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        this.SetGraphicsRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }

                    if (_perfLogEnabled) {
                        this._perfRootTableSets++;
                    }

                    break;
                }
            case ResourceKind.Sampler: {
                    D3D12Sampler d3D12Sampler = Util.AssertSubtype<IBindableResource, D3D12Sampler>(resource);
                    GpuDescriptorHandle gpuHandle = this.GetOrCreateDescriptorTableHandle(resource, bindingInfo.Kind, () => {
                        this.AllocateSamplerDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle allocatedGpuHandle);
                        CpuDescriptorHandle sourceSampler = d3D12Sampler.GetOrCreateDescriptor();

                        this.gd.Device.CopyDescriptorsSimple(1u, cpuHandle, sourceSampler, DescriptorHeapType.Sampler);
                        return allocatedGpuHandle;
                    });
                    if ((compute && this.IsSameComputeRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr)) || (!compute && this.IsSameGraphicsRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr))) {
                        break;
                    }

                    if (compute) {
                        this.NativeCommandList.SetComputeRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        this.SetComputeRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }
                    else {
                        this.NativeCommandList.SetGraphicsRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        this.SetGraphicsRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }

                    if (_perfLogEnabled) {
                        this._perfRootTableSets++;
                    }

                    break;
                }
            default: throw new VeldridException("Invalid descriptor-table binding kind.");
        }
    }

    /// <summary>
    /// Executes the ValidateTextureViewBindingSupport operation.
    /// </summary>
    /// <param name="textureView">Specifies the value of <paramref name="textureView" />.</param>
    /// <param name="requestedUsage">Specifies the value of <paramref name="requestedUsage" />.</param>
    /// <param name="bindingKind">Specifies the value of <paramref name="bindingKind" />.</param>
    private void ValidateTextureViewBindingSupport(D3D12TextureView textureView, TextureUsage requestedUsage, string bindingKind) {
        D3D12Texture texture = textureView.TargetTexture;
        TextureUsage usage = requestedUsage;

        if ((requestedUsage & TextureUsage.Sampled) != 0) {
            if ((texture.Usage & TextureUsage.Cubemap) != 0) {
                usage |= TextureUsage.Cubemap;
            }

            if ((texture.Usage & TextureUsage.DepthStencil) != 0) {
                usage |= TextureUsage.DepthStencil;
            }
        }

        if (!this.gd.GetPixelFormatSupport(textureView.Format, texture.Type, usage)) {
            throw new PlatformNotSupportedException($"D3D12 {bindingKind} texture view binding is not supported for format {textureView.Format}, type {texture.Type}, usage {usage}.");
        }
    }

    /// <summary>
    /// Executes the BindDescriptorHeaps operation.
    /// </summary>
    private void BindDescriptorHeaps() {
        if (this._descriptorHeapsBound) {
            return;
        }

        this._boundDescriptorHeaps[0] = this._shaderVisibleSrvUavHeaps[this._currentFrameSlot];
        this._boundDescriptorHeaps[1] = this._shaderVisibleSamplerHeaps[this._currentFrameSlot];
        this.NativeCommandList.SetDescriptorHeaps(this._boundDescriptorHeaps);
        this._descriptorHeapsBound = true;
    }

    /// <summary>
    /// Executes the AllocateSrvUavDescriptor operation.
    /// </summary>
    /// <param name="cpuHandle">Specifies the value of <paramref name="cpuHandle" />.</param>
    /// <param name="gpuHandle">Specifies the value of <paramref name="gpuHandle" />.</param>
    private void AllocateSrvUavDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
        if (this._nextSrvUavDescriptor >= this._maxSrvUavDescriptors) {
            throw new VeldridException("D3D12 SRV/UAV descriptor heap exhausted for this CommandList recording.");
        }

        CpuDescriptorHandle cpuStart = this._shaderVisibleSrvUavHeaps[this._currentFrameSlot].GetCPUDescriptorHandleForHeapStart();
        GpuDescriptorHandle gpuStart = this._shaderVisibleSrvUavHeaps[this._currentFrameSlot].GetGPUDescriptorHandleForHeapStart();
        cpuHandle = new CpuDescriptorHandle(cpuStart, (int)this._nextSrvUavDescriptor, (uint)this._srvUavDescriptorSize);
        gpuHandle = new GpuDescriptorHandle(gpuStart, (int)this._nextSrvUavDescriptor, (uint)this._srvUavDescriptorSize);
        this._nextSrvUavDescriptor++;
        this._nextSrvUavDescriptorsPerFrameSlot[this._currentFrameSlot] = this._nextSrvUavDescriptor;
    }

    /// <summary>
    /// Executes the AllocateSamplerDescriptor operation.
    /// </summary>
    /// <param name="cpuHandle">Specifies the value of <paramref name="cpuHandle" />.</param>
    /// <param name="gpuHandle">Specifies the value of <paramref name="gpuHandle" />.</param>
    private void AllocateSamplerDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
        if (this._nextSamplerDescriptor >= this._maxSamplerDescriptors) {
            throw new VeldridException("D3D12 sampler descriptor heap exhausted for this CommandList recording.");
        }

        CpuDescriptorHandle cpuStart = this._shaderVisibleSamplerHeaps[this._currentFrameSlot].GetCPUDescriptorHandleForHeapStart();
        GpuDescriptorHandle gpuStart = this._shaderVisibleSamplerHeaps[this._currentFrameSlot].GetGPUDescriptorHandleForHeapStart();
        cpuHandle = new CpuDescriptorHandle(cpuStart, (int)this._nextSamplerDescriptor, (uint)this._samplerDescriptorSize);
        gpuHandle = new GpuDescriptorHandle(gpuStart, (int)this._nextSamplerDescriptor, (uint)this._samplerDescriptorSize);
        this._nextSamplerDescriptor++;
        this._nextSamplerDescriptorsPerFrameSlot[this._currentFrameSlot] = this._nextSamplerDescriptor;
    }

    /// <summary>
    /// Executes the GetOrCreateDescriptorTableHandle operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="kind">Specifies the value of <paramref name="kind" />.</param>
    /// <param name="createHandle">Specifies the value of <paramref name="createHandle" />.</param>
    /// <returns>Returns the result produced by the GetOrCreateDescriptorTableHandle operation.</returns>
    private GpuDescriptorHandle GetOrCreateDescriptorTableHandle(IBindableResource resource, ResourceKind kind, Func<GpuDescriptorHandle> createHandle) {
        Dictionary<DescriptorCacheKey, GpuDescriptorHandle> descriptorTableCache = this._descriptorTableCaches[this._currentFrameSlot];
        DescriptorCacheKey key = new(resource, kind);
        if (descriptorTableCache.TryGetValue(key, out GpuDescriptorHandle cachedHandle)) {
            return cachedHandle;
        }

        GpuDescriptorHandle newHandle = createHandle();
        if (_perfLogEnabled) {
            this._perfDescriptorCopies++;
        }

        descriptorTableCache.Add(key, newHandle);
        return newHandle;
    }

    /// <summary>
    /// Executes the GetGraphicsResourceSetBindingPlan operation.
    /// </summary>
    /// <param name="pipeline">Specifies the value of <paramref name="pipeline" />.</param>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="layout">Specifies the value of <paramref name="layout" />.</param>
    /// <returns>Returns the result produced by the GetGraphicsResourceSetBindingPlan operation.</returns>
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
    /// Executes the GetComputeResourceSetBindingPlan operation.
    /// </summary>
    /// <param name="pipeline">Specifies the value of <paramref name="pipeline" />.</param>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="layout">Specifies the value of <paramref name="layout" />.</param>
    /// <returns>Returns the result produced by the GetComputeResourceSetBindingPlan operation.</returns>
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
    /// Executes the CreateGraphicsResourceSetBindingPlan operation.
    /// </summary>
    /// <param name="pipeline">Specifies the value of <paramref name="pipeline" />.</param>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="elements">Specifies the value of <paramref name="elements" />.</param>
    /// <returns>Returns the result produced by the CreateGraphicsResourceSetBindingPlan operation.</returns>
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
    /// Executes the CreateComputeResourceSetBindingPlan operation.
    /// </summary>
    /// <param name="pipeline">Specifies the value of <paramref name="pipeline" />.</param>
    /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
    /// <param name="elements">Specifies the value of <paramref name="elements" />.</param>
    /// <returns>Returns the result produced by the CreateComputeResourceSetBindingPlan operation.</returns>
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
    /// Executes the GetGraphicsBufferState operation.
    /// </summary>
    /// <param name="kind">Specifies the value of <paramref name="kind" />.</param>
    /// <returns>Returns the result produced by the GetGraphicsBufferState operation.</returns>
    private static ResourceStates GetGraphicsBufferState(ResourceKind kind) {
        switch (kind) {
            case ResourceKind.UniformBuffer: return ResourceStates.VertexAndConstantBuffer;
            case ResourceKind.StructuredBufferReadOnly: return ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource;
            case ResourceKind.StructuredBufferReadWrite: return ResourceStates.UnorderedAccess;
            default: return ResourceStates.Common;
        }
    }

    /// <summary>
    /// Executes the GetComputeBufferState operation.
    /// </summary>
    /// <param name="kind">Specifies the value of <paramref name="kind" />.</param>
    /// <returns>Returns the result produced by the GetComputeBufferState operation.</returns>
    private static ResourceStates GetComputeBufferState(ResourceKind kind) {
        switch (kind) {
            case ResourceKind.UniformBuffer: return ResourceStates.VertexAndConstantBuffer;
            case ResourceKind.StructuredBufferReadOnly: return ResourceStates.NonPixelShaderResource;
            case ResourceKind.StructuredBufferReadWrite: return ResourceStates.UnorderedAccess;
            default: return ResourceStates.Common;
        }
    }

    /// <summary>
    /// Executes the IsSameGraphicsRootBuffer operation.
    /// </summary>
    /// <param name="rootParameterIndex">Specifies the value of <paramref name="rootParameterIndex" />.</param>
    /// <param name="gpuAddress">Specifies the value of <paramref name="gpuAddress" />.</param>
    /// <returns>Returns the result produced by the IsSameGraphicsRootBuffer operation.</returns>
    private bool IsSameGraphicsRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddresses, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddressValid, rootParameterIndex + 1);
        return this._graphicsRootBufferAddressValid[index] && this._graphicsRootBufferAddresses[index] == gpuAddress;
    }

    /// <summary>
    /// Executes the SetGraphicsRootBufferCache operation.
    /// </summary>
    /// <param name="rootParameterIndex">Specifies the value of <paramref name="rootParameterIndex" />.</param>
    /// <param name="gpuAddress">Specifies the value of <paramref name="gpuAddress" />.</param>
    private void SetGraphicsRootBufferCache(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddresses, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddressValid, rootParameterIndex + 1);
        this._graphicsRootBufferAddresses[index] = gpuAddress;
        this._graphicsRootBufferAddressValid[index] = true;
    }

    /// <summary>
    /// Executes the IsSameComputeRootBuffer operation.
    /// </summary>
    /// <param name="rootParameterIndex">Specifies the value of <paramref name="rootParameterIndex" />.</param>
    /// <param name="gpuAddress">Specifies the value of <paramref name="gpuAddress" />.</param>
    /// <returns>Returns the result produced by the IsSameComputeRootBuffer operation.</returns>
    private bool IsSameComputeRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddresses, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddressValid, rootParameterIndex + 1);
        return this._computeRootBufferAddressValid[index] && this._computeRootBufferAddresses[index] == gpuAddress;
    }

    /// <summary>
    /// Executes the SetComputeRootBufferCache operation.
    /// </summary>
    /// <param name="rootParameterIndex">Specifies the value of <paramref name="rootParameterIndex" />.</param>
    /// <param name="gpuAddress">Specifies the value of <paramref name="gpuAddress" />.</param>
    private void SetComputeRootBufferCache(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddresses, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddressValid, rootParameterIndex + 1);
        this._computeRootBufferAddresses[index] = gpuAddress;
        this._computeRootBufferAddressValid[index] = true;
    }

    /// <summary>
    /// Executes the IsSameGraphicsRootTable operation.
    /// </summary>
    /// <param name="rootParameterIndex">Specifies the value of <paramref name="rootParameterIndex" />.</param>
    /// <param name="tablePtr">Specifies the value of <paramref name="tablePtr" />.</param>
    /// <returns>Returns the result produced by the IsSameGraphicsRootTable operation.</returns>
    private bool IsSameGraphicsRootTable(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointers, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointerValid, rootParameterIndex + 1);
        return this._graphicsRootTablePointerValid[index] && this._graphicsRootTablePointers[index] == tablePtr;
    }

    /// <summary>
    /// Executes the SetGraphicsRootTableCache operation.
    /// </summary>
    /// <param name="rootParameterIndex">Specifies the value of <paramref name="rootParameterIndex" />.</param>
    /// <param name="tablePtr">Specifies the value of <paramref name="tablePtr" />.</param>
    private void SetGraphicsRootTableCache(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointers, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointerValid, rootParameterIndex + 1);
        this._graphicsRootTablePointers[index] = tablePtr;
        this._graphicsRootTablePointerValid[index] = true;
    }

    /// <summary>
    /// Executes the IsSameComputeRootTable operation.
    /// </summary>
    /// <param name="rootParameterIndex">Specifies the value of <paramref name="rootParameterIndex" />.</param>
    /// <param name="tablePtr">Specifies the value of <paramref name="tablePtr" />.</param>
    /// <returns>Returns the result produced by the IsSameComputeRootTable operation.</returns>
    private bool IsSameComputeRootTable(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._computeRootTablePointers, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._computeRootTablePointerValid, rootParameterIndex + 1);
        return this._computeRootTablePointerValid[index] && this._computeRootTablePointers[index] == tablePtr;
    }

    /// <summary>
    /// Executes the SetComputeRootTableCache operation.
    /// </summary>
    /// <param name="rootParameterIndex">Specifies the value of <paramref name="rootParameterIndex" />.</param>
    /// <param name="tablePtr">Specifies the value of <paramref name="tablePtr" />.</param>
    private void SetComputeRootTableCache(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._computeRootTablePointers, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._computeRootTablePointerValid, rootParameterIndex + 1);
        this._computeRootTablePointers[index] = tablePtr;
        this._computeRootTablePointerValid[index] = true;
    }

    /// <summary>
    /// Executes the InvalidateGraphicsRootCaches operation.
    /// </summary>
    private void InvalidateGraphicsRootCaches() {
        Array.Clear(this._graphicsRootBufferAddressValid, 0, this._graphicsRootBufferAddressValid.Length);
        Array.Clear(this._graphicsRootTablePointerValid, 0, this._graphicsRootTablePointerValid.Length);
    }

    /// <summary>
    /// Executes the InvalidateComputeRootCaches operation.
    /// </summary>
    private void InvalidateComputeRootCaches() {
        Array.Clear(this._computeRootBufferAddressValid, 0, this._computeRootBufferAddressValid.Length);
        Array.Clear(this._computeRootTablePointerValid, 0, this._computeRootTablePointerValid.Length);
    }

    /// <summary>
    /// Executes the ClearBoundResourceSets operation.
    /// </summary>
    /// <param name="infos">Specifies the value of <paramref name="infos" />.</param>
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
    /// Executes the TransitionSwapchainBackBuffersToPresent operation.
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

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("8BA5FB08-5195-40E2-AC58-0D989C3A0102")]

    /// <summary>
    /// Defines the ID3DBlob interface.
    /// </summary>
    private interface ID3DBlob {
        [PreserveSig]

        /// <summary>
        /// Executes the GetBufferPointer operation.
        /// </summary>
        /// <returns>Returns the result produced by the GetBufferPointer operation.</returns>
        IntPtr GetBufferPointer();

        [PreserveSig]

        /// <summary>
        /// Executes the GetBufferSize operation.
        /// </summary>
        /// <returns>Returns the result produced by the GetBufferSize operation.</returns>
        nuint GetBufferSize();
    }

    /// <summary>
    /// Defines the data layout and behavior of the DescriptorCacheKey struct.
    /// </summary>
    private readonly struct DescriptorCacheKey {

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorCacheKey" /> type.
        /// </summary>
        /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
        /// <param name="kind">Specifies the value of <paramref name="kind" />.</param>
        public DescriptorCacheKey(IBindableResource resource, ResourceKind kind) {
            this.Resource = resource;
            this.Kind = kind;
        }

        /// <summary>
        /// Gets or sets Resource.
        /// </summary>
        public IBindableResource Resource { get; }

        /// <summary>
        /// Gets or sets Kind.
        /// </summary>
        public ResourceKind Kind { get; }
    }

    /// <summary>
    /// Defines the data layout and behavior of the ResourceSetBindingPlanKey struct.
    /// </summary>
    private readonly struct ResourceSetBindingPlanKey {

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSetBindingPlanKey" /> type.
        /// </summary>
        /// <param name="pipeline">Specifies the value of <paramref name="pipeline" />.</param>
        /// <param name="layout">Specifies the value of <paramref name="layout" />.</param>
        /// <param name="slot">Specifies the value of <paramref name="slot" />.</param>
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
    /// Defines the data layout and behavior of the ResourceSetBindingPlanEntry struct.
    /// </summary>
    private readonly struct ResourceSetBindingPlanEntry {

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSetBindingPlanEntry" /> type.
        /// </summary>
        /// <param name="elementIndex">Specifies the value of <paramref name="elementIndex" />.</param>
        /// <param name="bindingInfo">Specifies the value of <paramref name="bindingInfo" />.</param>
        /// <param name="isDynamicBinding">Specifies the value of <paramref name="isDynamicBinding" />.</param>
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
    /// Defines the behavior and responsibilities of the DescriptorCacheKeyComparer class.
    /// </summary>
    private sealed class DescriptorCacheKeyComparer : IEqualityComparer<DescriptorCacheKey> {

        /// <summary>
        /// Stores the value associated with <c>Instance</c>.
        /// </summary>
        public static readonly DescriptorCacheKeyComparer Instance = new();

        /// <summary>
        /// Executes the Equals operation.
        /// </summary>
        /// <param name="x">Specifies the value of <paramref name="x" />.</param>
        /// <param name="y">Specifies the value of <paramref name="y" />.</param>
        /// <returns>Returns the result produced by the Equals operation.</returns>
        public bool Equals(DescriptorCacheKey x, DescriptorCacheKey y) {
            return x.Kind == y.Kind && ReferenceEquals(x.Resource, y.Resource);
        }

        /// <summary>
        /// Executes the GetHashCode operation.
        /// </summary>
        /// <param name="obj">Specifies the value of <paramref name="obj" />.</param>
        /// <returns>Returns the result produced by the GetHashCode operation.</returns>
        public int GetHashCode(DescriptorCacheKey obj) {
            return HashCode.Combine((int)obj.Kind, RuntimeHelpers.GetHashCode(obj.Resource));
        }
    }

    /// <summary>
    /// Defines the behavior and responsibilities of the ResourceSetBindingPlanKeyComparer class.
    /// </summary>
    private sealed class ResourceSetBindingPlanKeyComparer : IEqualityComparer<ResourceSetBindingPlanKey> {

        /// <summary>
        /// Stores the value associated with <c>Instance</c>.
        /// </summary>
        public static readonly ResourceSetBindingPlanKeyComparer Instance = new();

        /// <summary>
        /// Executes the Equals operation.
        /// </summary>
        /// <param name="x">Specifies the value of <paramref name="x" />.</param>
        /// <param name="y">Specifies the value of <paramref name="y" />.</param>
        /// <returns>Returns the result produced by the Equals operation.</returns>
        public bool Equals(ResourceSetBindingPlanKey x, ResourceSetBindingPlanKey y) {
            return x.Slot == y.Slot && ReferenceEquals(x.Pipeline, y.Pipeline) && ReferenceEquals(x.Layout, y.Layout);
        }

        /// <summary>
        /// Executes the GetHashCode operation.
        /// </summary>
        /// <param name="obj">Specifies the value of <paramref name="obj" />.</param>
        /// <returns>Returns the result produced by the GetHashCode operation.</returns>
        public int GetHashCode(ResourceSetBindingPlanKey obj) {
            return HashCode.Combine((int)obj.Slot, RuntimeHelpers.GetHashCode(obj.Pipeline), RuntimeHelpers.GetHashCode(obj.Layout));
        }
    }
}
