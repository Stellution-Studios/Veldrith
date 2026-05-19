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
/// Represents the D3D12CommandList class.
/// </summary>
internal sealed class D3D12CommandList : CommandList {

    /// <summary>
    /// Represents the FramesInFlight field.
    /// </summary>
    private const int FramesInFlight = 8;

    /// <summary>
    /// Represents the PerfReportIntervalFrames field.
    /// </summary>
    private const int PerfReportIntervalFrames = 240;

    /// <summary>
    /// Performs the register operation.
    /// </summary>
    /// <param name="t0">The value of t0.</param>
    /// <returns>The result of the register operation.</returns>
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
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="Ordinal">The value of Ordinal.</param>
    /// <returns>The result of the Equals operation.</returns>
    private static readonly bool _perfLogEnabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PERF"), "1", StringComparison.Ordinal);

    /// <summary>
    /// Represents the _activeScissorRects field.
    /// </summary>
    private readonly RawRect[] _activeScissorRects = new RawRect[16];

    /// <summary>
    /// Represents the _activeViewports field.
    /// </summary>
    private readonly Vortice.Mathematics.Viewport[] _activeViewports = new Vortice.Mathematics.Viewport[16];

    /// <summary>
    /// Represents the _beginEventMethod field.
    /// </summary>
    private readonly MethodInfo _beginEventMethod;

    /// <summary>
    /// Represents the _boundDescriptorHeaps field.
    /// </summary>
    private readonly ID3D12DescriptorHeap[] _boundDescriptorHeaps = new ID3D12DescriptorHeap[2];

    /// <summary>
    /// Represents the _boundVertexBufferOffsets field.
    /// </summary>
    private readonly uint[] _boundVertexBufferOffsets = new uint[16];

    /// <summary>
    /// Represents the _boundVertexBuffers field.
    /// </summary>
    private readonly D3D12DeviceBuffer[] _boundVertexBuffers = new D3D12DeviceBuffer[16];

    /// <summary>
    /// Represents the _boundVertexBufferVersions field.
    /// </summary>
    private readonly ulong[] _boundVertexBufferVersions = new ulong[16];

    /// <summary>
    /// Represents the _commandAllocators field.
    /// </summary>
    private readonly ID3D12CommandAllocator[] _commandAllocators = new ID3D12CommandAllocator[FramesInFlight];

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="Instance">The value of Instance.</param>
    /// <returns>The result of the new operation.</returns>
    private readonly Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlanEntry[]> _computeResourceSetBindingPlans = new(ResourceSetBindingPlanKeyComparer.Instance);

    /// <summary>
    /// Represents the _descriptorTableCaches field.
    /// </summary>
    private readonly Dictionary<DescriptorCacheKey, GpuDescriptorHandle>[] _descriptorTableCaches = new Dictionary<DescriptorCacheKey, GpuDescriptorHandle>[FramesInFlight];

    /// <summary>
    /// Represents the _endEventMethod field.
    /// </summary>
    private readonly MethodInfo _endEventMethod;

    /// <summary>
    /// Represents the _frameSlotFenceValues field.
    /// </summary>
    private readonly ulong[] _frameSlotFenceValues = new ulong[FramesInFlight];

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="Instance">The value of Instance.</param>
    /// <returns>The result of the new operation.</returns>
    private readonly Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlanEntry[]> _graphicsResourceSetBindingPlans = new(ResourceSetBindingPlanKeyComparer.Instance);

    /// <summary>
    /// Represents the _maxSamplerDescriptors field.
    /// </summary>
    private readonly uint _maxSamplerDescriptors = 1024;

    /// <summary>
    /// Represents the _maxSrvUavDescriptors field.
    /// </summary>
    private readonly uint _maxSrvUavDescriptors = 4096;

    /// <summary>
    /// Represents the _nextSamplerDescriptorsPerFrameSlot field.
    /// </summary>
    private readonly uint[] _nextSamplerDescriptorsPerFrameSlot = new uint[FramesInFlight];

    /// <summary>
    /// Represents the _nextSrvUavDescriptorsPerFrameSlot field.
    /// </summary>
    private readonly uint[] _nextSrvUavDescriptorsPerFrameSlot = new uint[FramesInFlight];

    /// <summary>
    /// Performs the StartNew operation.
    /// </summary>
    /// <returns>The result of the StartNew operation.</returns>
    private readonly Stopwatch _perfStopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Represents the _samplerDescriptorSize field.
    /// </summary>
    private readonly int _samplerDescriptorSize;

    /// <summary>
    /// Represents the _setMarkerMethod field.
    /// </summary>
    private readonly MethodInfo _setMarkerMethod;

    /// <summary>
    /// Represents the _shaderVisibleSamplerHeaps field.
    /// </summary>
    private readonly ID3D12DescriptorHeap[] _shaderVisibleSamplerHeaps = new ID3D12DescriptorHeap[FramesInFlight];

    /// <summary>
    /// Represents the _shaderVisibleSrvUavHeaps field.
    /// </summary>
    private readonly ID3D12DescriptorHeap[] _shaderVisibleSrvUavHeaps = new ID3D12DescriptorHeap[FramesInFlight];

    /// <summary>
    /// Represents the _singleBarrier field.
    /// </summary>
    private readonly ResourceBarrier[] _singleBarrier = new ResourceBarrier[1];

    /// <summary>
    /// Represents the _srvUavDescriptorSize field.
    /// </summary>
    private readonly int _srvUavDescriptorSize;

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Represents the _activeScissorRectCount field.
    /// </summary>
    private uint _activeScissorRectCount;

    /// <summary>
    /// Represents the _activeViewportCount field.
    /// </summary>
    private uint _activeViewportCount;

    /// <summary>
    /// Represents the _begun field.
    /// </summary>
    private bool _begun;

    /// <summary>
    /// Represents the _boundComputeResourceSets field.
    /// </summary>
    private BoundResourceSetInfo[] _boundComputeResourceSets = Array.Empty<BoundResourceSetInfo>();

    /// <summary>
    /// Represents the _boundGraphicsResourceSets field.
    /// </summary>
    private BoundResourceSetInfo[] _boundGraphicsResourceSets = Array.Empty<BoundResourceSetInfo>();

    /// <summary>
    /// Represents the _boundIndexBuffer field.
    /// </summary>
    private D3D12DeviceBuffer _boundIndexBuffer;

    /// <summary>
    /// Represents the _boundIndexBufferOffset field.
    /// </summary>
    private uint _boundIndexBufferOffset;

    /// <summary>
    /// Represents the _boundIndexBufferVersion field.
    /// </summary>
    private ulong _boundIndexBufferVersion;

    /// <summary>
    /// Represents the _boundIndexFormat field.
    /// </summary>
    private IndexFormat _boundIndexFormat;

    /// <summary>
    /// Represents the _computeRootBufferAddresses field.
    /// </summary>
    private ulong[] _computeRootBufferAddresses = Array.Empty<ulong>();

    /// <summary>
    /// Represents the _computeRootBufferAddressValid field.
    /// </summary>
    private bool[] _computeRootBufferAddressValid = Array.Empty<bool>();

    /// <summary>
    /// Represents the _computeRootTablePointers field.
    /// </summary>
    private ulong[] _computeRootTablePointers = Array.Empty<ulong>();

    /// <summary>
    /// Represents the _computeRootTablePointerValid field.
    /// </summary>
    private bool[] _computeRootTablePointerValid = Array.Empty<bool>();

    /// <summary>
    /// Represents the _currentComputePipeline field.
    /// </summary>
    private D3D12Pipeline _currentComputePipeline;

    /// <summary>
    /// Represents the _currentFrameSlot field.
    /// </summary>
    private int _currentFrameSlot = -1;

    /// <summary>
    /// Represents the _currentGraphicsPipeline field.
    /// </summary>
    private D3D12Pipeline _currentGraphicsPipeline;

    /// <summary>
    /// Represents the _descriptorHeapsBound field.
    /// </summary>
    private bool _descriptorHeapsBound;

    /// <summary>
    /// Represents the _dispatchIndirectSignature field.
    /// </summary>
    private ID3D12CommandSignature _dispatchIndirectSignature;

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Represents the _drawIndexedIndirectSignature field.
    /// </summary>
    private ID3D12CommandSignature _drawIndexedIndirectSignature;

    /// <summary>
    /// Represents the _drawIndirectSignature field.
    /// </summary>
    private ID3D12CommandSignature _drawIndirectSignature;

    /// <summary>
    /// Represents the _ended field.
    /// </summary>
    private bool _ended;

    /// <summary>
    /// Represents the _gpuMipPipeline field.
    /// </summary>
    private D3D12Pipeline _gpuMipPipeline;

    /// <summary>
    /// Represents the _gpuMipResourceLayout field.
    /// </summary>
    private ResourceLayout _gpuMipResourceLayout;

    /// <summary>
    /// Represents the _gpuMipResourcesAvailable field.
    /// </summary>
    private bool _gpuMipResourcesAvailable;

    /// <summary>
    /// Represents the _gpuMipResourcesInitialized field.
    /// </summary>
    private bool _gpuMipResourcesInitialized;

    /// <summary>
    /// Represents the _gpuMipSampler field.
    /// </summary>
    private Sampler _gpuMipSampler;

    /// <summary>
    /// Represents the _graphicsRootBufferAddresses field.
    /// </summary>
    private ulong[] _graphicsRootBufferAddresses = Array.Empty<ulong>();

    /// <summary>
    /// Represents the _graphicsRootBufferAddressValid field.
    /// </summary>
    private bool[] _graphicsRootBufferAddressValid = Array.Empty<bool>();

    /// <summary>
    /// Represents the _graphicsRootTablePointers field.
    /// </summary>
    private ulong[] _graphicsRootTablePointers = Array.Empty<ulong>();

    /// <summary>
    /// Represents the _graphicsRootTablePointerValid field.
    /// </summary>
    private bool[] _graphicsRootTablePointerValid = Array.Empty<bool>();

    /// <summary>
    /// Represents the _hasBoundIndexBuffer field.
    /// </summary>
    private bool _hasBoundIndexBuffer;

    /// <summary>
    /// Represents the _indirectSignaturesAvailable field.
    /// </summary>
    private bool _indirectSignaturesAvailable;

    /// <summary>
    /// Represents the _indirectSignaturesInitialized field.
    /// </summary>
    private bool _indirectSignaturesInitialized;

    /// <summary>
    /// Represents the _maxBoundVertexBufferSlot field.
    /// </summary>
    private uint _maxBoundVertexBufferSlot;

    /// <summary>
    /// Represents the _nextSamplerDescriptor field.
    /// </summary>
    private uint _nextSamplerDescriptor;

    /// <summary>
    /// Represents the _nextSrvUavDescriptor field.
    /// </summary>
    private uint _nextSrvUavDescriptor;

    /// <summary>
    /// Represents the _perfAccumBeginWaitCount field.
    /// </summary>
    private ulong _perfAccumBeginWaitCount;

    /// <summary>
    /// Represents the _perfAccumBeginWaitMs field.
    /// </summary>
    private double _perfAccumBeginWaitMs;

    /// <summary>
    /// Represents the _perfAccumDescriptorCopies field.
    /// </summary>
    private ulong _perfAccumDescriptorCopies;

    /// <summary>
    /// Represents the _perfAccumDispatchCalls field.
    /// </summary>
    private ulong _perfAccumDispatchCalls;

    /// <summary>
    /// Represents the _perfAccumDrawCalls field.
    /// </summary>
    private ulong _perfAccumDrawCalls;

    /// <summary>
    /// Represents the _perfAccumIndexBufferBinds field.
    /// </summary>
    private ulong _perfAccumIndexBufferBinds;

    /// <summary>
    /// Represents the _perfAccumPipelineChanges field.
    /// </summary>
    private ulong _perfAccumPipelineChanges;

    /// <summary>
    /// Represents the _perfAccumResourceSetChanges field.
    /// </summary>
    private ulong _perfAccumResourceSetChanges;

    /// <summary>
    /// Represents the _perfAccumRootTableSets field.
    /// </summary>
    private ulong _perfAccumRootTableSets;

    /// <summary>
    /// Represents the _perfAccumSubresourceTransitions field.
    /// </summary>
    private ulong _perfAccumSubresourceTransitions;

    /// <summary>
    /// Represents the _perfAccumTransitions field.
    /// </summary>
    private ulong _perfAccumTransitions;

    /// <summary>
    /// Represents the _perfAccumUavBarriers field.
    /// </summary>
    private ulong _perfAccumUavBarriers;

    /// <summary>
    /// Represents the _perfAccumVertexBufferBinds field.
    /// </summary>
    private ulong _perfAccumVertexBufferBinds;

    /// <summary>
    /// Represents the _perfBeginWaitCount field.
    /// </summary>
    private ulong _perfBeginWaitCount;

    /// <summary>
    /// Represents the _perfBeginWaitMs field.
    /// </summary>
    private double _perfBeginWaitMs;

    /// <summary>
    /// Represents the _perfDescriptorCopies field.
    /// </summary>
    private ulong _perfDescriptorCopies;

    /// <summary>
    /// Represents the _perfDispatchCalls field.
    /// </summary>
    private ulong _perfDispatchCalls;

    /// <summary>
    /// Represents the _perfDrawCalls field.
    /// </summary>
    private ulong _perfDrawCalls;

    /// <summary>
    /// Represents the _perfFrames field.
    /// </summary>
    private ulong _perfFrames;

    /// <summary>
    /// Represents the _perfIndexBufferBinds field.
    /// </summary>
    private ulong _perfIndexBufferBinds;

    /// <summary>
    /// Represents the _perfLastReportMs field.
    /// </summary>
    private double _perfLastReportMs;

    /// <summary>
    /// Represents the _perfPipelineChanges field.
    /// </summary>
    private ulong _perfPipelineChanges;

    /// <summary>
    /// Represents the _perfResourceSetChanges field.
    /// </summary>
    private ulong _perfResourceSetChanges;

    /// <summary>
    /// Represents the _perfRootTableSets field.
    /// </summary>
    private ulong _perfRootTableSets;

    /// <summary>
    /// Represents the _perfSubresourceTransitions field.
    /// </summary>
    private ulong _perfSubresourceTransitions;

    /// <summary>
    /// Represents the _perfTransitions field.
    /// </summary>
    private ulong _perfTransitions;

    /// <summary>
    /// Represents the _perfUavBarriers field.
    /// </summary>
    private ulong _perfUavBarriers;

    /// <summary>
    /// Represents the _perfVertexBufferBinds field.
    /// </summary>
    private ulong _perfVertexBufferBinds;

    /// <summary>
    /// Represents the _transitionedBackBufferIndex field.
    /// </summary>
    private int _transitionedBackBufferIndex = -1;

    /// <summary>
    /// Represents the _uavBarrierPending field.
    /// </summary>
    private bool _uavBarrierPending;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12CommandList" /> type.
    /// </summary>
    /// <param name="gd">The value of gd.</param>
    /// <param name="description">The value of description.</param>
    /// <param name="features">The value of features.</param>
    /// <param name="uniformAlignment">The value of uniformAlignment.</param>
    /// <param name="structuredAlignment">The value of structuredAlignment.</param>
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
    /// Performs the Dispose operation.
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
    /// Performs the Begin operation.
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
    /// Performs the End operation.
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
    /// Performs the SetViewport operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="viewport">The value of viewport.</param>
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
    /// Performs the SetScissorRect operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
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
    /// Performs the Dispatch operation.
    /// </summary>
    /// <param name="groupCountX">The value of groupCountX.</param>
    /// <param name="groupCountY">The value of groupCountY.</param>
    /// <param name="groupCountZ">The value of groupCountZ.</param>
    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ) {
        this.FlushPendingUavBarrier();
        this.NativeCommandList.Dispatch(groupCountX, groupCountY, groupCountZ);
        if (_perfLogEnabled) {
            this._perfDispatchCalls++;
        }

        this._uavBarrierPending = true;
    }

    /// <summary>
    /// Performs the ExecuteNoSignal operation.
    /// </summary>
    internal void ExecuteNoSignal() {
        if (!this._ended) {
            throw new VeldridException("CommandList must be ended before submit.");
        }

        this.gd.CommandQueue.ExecuteCommandList(this.NativeCommandList);
    }

    /// <summary>
    /// Performs the MarkSubmitted operation.
    /// </summary>
    /// <param name="signalValue">The value of signalValue.</param>
    internal void MarkSubmitted(ulong signalValue) {
        if (this._currentFrameSlot >= 0) {
            this._frameSlotFenceValues[this._currentFrameSlot] = signalValue;
        }
    }

    /// <summary>
    /// Performs the SetGraphicsResourceSetCore operation.
    /// </summary>
    /// <param name="slot">The value of slot.</param>
    /// <param name="rs">The value of rs.</param>
    /// <param name="dynamicOffsetsCount">The value of dynamicOffsetsCount.</param>
    /// <param name="dynamicOffsets">The value of dynamicOffsets.</param>
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
    /// Performs the SetComputeResourceSetCore operation.
    /// </summary>
    /// <param name="slot">The value of slot.</param>
    /// <param name="set">The value of set.</param>
    /// <param name="dynamicOffsetsCount">The value of dynamicOffsetsCount.</param>
    /// <param name="dynamicOffsets">The value of dynamicOffsets.</param>
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
    /// Performs the SetFramebufferCore operation.
    /// </summary>
    /// <param name="fb">The value of fb.</param>
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
    /// Performs the DrawIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="offset">The value of offset.</param>
    /// <param name="drawCount">The value of drawCount.</param>
    /// <param name="stride">The value of stride.</param>
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
    /// Performs the DrawIndexedIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="offset">The value of offset.</param>
    /// <param name="drawCount">The value of drawCount.</param>
    /// <param name="stride">The value of stride.</param>
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
    /// Performs the DispatchIndirectCore operation.
    /// </summary>
    /// <param name="indirectBuffer">The value of indirectBuffer.</param>
    /// <param name="offset">The value of offset.</param>
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
    /// Performs the ResolveTextureCore operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="destination">The value of destination.</param>
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
    /// Performs the CopyBufferCore operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="sourceOffset">The value of sourceOffset.</param>
    /// <param name="destination">The value of destination.</param>
    /// <param name="destinationOffset">The value of destinationOffset.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes) {
        this.FlushPendingUavBarrier();
        D3D12DeviceBuffer src = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(source);
        D3D12DeviceBuffer dst = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(destination);
        src.CopyTo(this.NativeCommandList, dst, sourceOffset, destinationOffset, sizeInBytes);
    }

    /// <summary>
    /// Performs the CopyTextureCore operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="srcX">The value of srcX.</param>
    /// <param name="srcY">The value of srcY.</param>
    /// <param name="srcZ">The value of srcZ.</param>
    /// <param name="srcMipLevel">The value of srcMipLevel.</param>
    /// <param name="srcBaseArrayLayer">The value of srcBaseArrayLayer.</param>
    /// <param name="destination">The value of destination.</param>
    /// <param name="dstX">The value of dstX.</param>
    /// <param name="dstY">The value of dstY.</param>
    /// <param name="dstZ">The value of dstZ.</param>
    /// <param name="dstMipLevel">The value of dstMipLevel.</param>
    /// <param name="dstBaseArrayLayer">The value of dstBaseArrayLayer.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="layerCount">The value of layerCount.</param>
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
    /// Performs the SetPipelineCore operation.
    /// </summary>
    /// <param name="pipeline">The value of pipeline.</param>
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
    /// Performs the SetVertexBufferCore operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="offset">The value of offset.</param>
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
    /// Performs the SetIndexBufferCore operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="format">The value of format.</param>
    /// <param name="offset">The value of offset.</param>
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
    /// Performs the ClearColorTargetCore operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="clearColor">The value of clearColor.</param>
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
    /// Performs the ClearDepthStencilCore operation.
    /// </summary>
    /// <param name="depth">The value of depth.</param>
    /// <param name="stencil">The value of stencil.</param>
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
    /// Performs the DrawCore operation.
    /// </summary>
    /// <param name="vertexCount">The value of vertexCount.</param>
    /// <param name="instanceCount">The value of instanceCount.</param>
    /// <param name="vertexStart">The value of vertexStart.</param>
    /// <param name="instanceStart">The value of instanceStart.</param>
    private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart) {
        this.FlushPendingUavBarrier();
        this.NativeCommandList.DrawInstanced(vertexCount, instanceCount, vertexStart, instanceStart);
        if (_perfLogEnabled) {
            this._perfDrawCalls++;
        }
    }

    /// <summary>
    /// Performs the DrawIndexedCore operation.
    /// </summary>
    /// <param name="indexCount">The value of indexCount.</param>
    /// <param name="instanceCount">The value of instanceCount.</param>
    /// <param name="indexStart">The value of indexStart.</param>
    /// <param name="vertexOffset">The value of vertexOffset.</param>
    /// <param name="instanceStart">The value of instanceStart.</param>
    private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart) {
        this.FlushPendingUavBarrier();
        this.NativeCommandList.DrawIndexedInstanced(indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
        if (_perfLogEnabled) {
            this._perfDrawCalls++;
        }
    }

    /// <summary>
    /// Performs the UpdateBufferCore operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="bufferOffsetInBytes">The value of bufferOffsetInBytes.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
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
    /// Performs the GenerateMipmapsCore operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
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
    /// Performs the PushDebugGroupCore operation.
    /// </summary>
    /// <param name="name">The value of name.</param>
    private protected override void PushDebugGroupCore(string name) {
        this.WriteDebugMarker(name, true, false);
    }

    /// <summary>
    /// Performs the PopDebugGroupCore operation.
    /// </summary>
    private protected override void PopDebugGroupCore() {
        this._endEventMethod?.Invoke(this.NativeCommandList, null);
    }

    /// <summary>
    /// Performs the InsertDebugMarkerCore operation.
    /// </summary>
    /// <param name="name">The value of name.</param>
    private protected override void InsertDebugMarkerCore(string name) {
        this.WriteDebugMarker(name, false, true);
    }

    /// <summary>
    /// Performs the Transition operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="from">The value of from.</param>
    /// <param name="to">The value of to.</param>
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
    /// Performs the BindVertexBuffer operation.
    /// </summary>
    /// <param name="index">The value of index.</param>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="offset">The value of offset.</param>
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
    /// Performs the RebindVertexBuffersForCurrentPipeline operation.
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
    /// Performs the TransitionSubresource operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="from">The value of from.</param>
    /// <param name="to">The value of to.</param>
    /// <param name="subresource">The value of subresource.</param>
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
    /// Performs the FlushPendingUavBarrier operation.
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
    /// Performs the WaitForFrameSlot operation.
    /// </summary>
    /// <param name="frameSlot">The value of frameSlot.</param>
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
    /// Performs the ExecuteIndirect operation.
    /// </summary>
    /// <param name="argumentBuffer">The value of argumentBuffer.</param>
    /// <param name="offset">The value of offset.</param>
    /// <param name="drawCount">The value of drawCount.</param>
    /// <param name="stride">The value of stride.</param>
    /// <param name="argumentSize">The value of argumentSize.</param>
    /// <param name="signature">The value of signature.</param>
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
    /// Performs the EnsureIndirectCommandSignatures operation.
    /// </summary>
    /// <returns>The result of the EnsureIndirectCommandSignatures operation.</returns>
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
    /// Performs the CreateCommandSignature operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the CreateCommandSignature operation.</returns>
    private ID3D12CommandSignature CreateCommandSignature(CommandSignatureDescription description) {
        ID3D12CommandSignature signature = this.gd.Device.CreateCommandSignature<ID3D12CommandSignature>(description, null);

        if (signature == null) {
            throw new VeldridException("Unable to create D3D12 command signature.");
        }

        return signature;
    }

    /// <summary>
    /// Performs the CanUseGpuMipmapPath operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <returns>The result of the CanUseGpuMipmapPath operation.</returns>
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
    /// Performs the GenerateMipmapsGpu operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
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
    /// Performs the EnsureGpuMipmapResources operation.
    /// </summary>
    /// <returns>The result of the EnsureGpuMipmapResources operation.</returns>
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
    /// Performs the CompileComputeShader operation.
    /// </summary>
    /// <param name="sourceCode">The value of sourceCode.</param>
    /// <param name="entryPoint">The value of entryPoint.</param>
    /// <param name="target">The value of target.</param>
    /// <returns>The result of the CompileComputeShader operation.</returns>
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
    /// Performs the TransitionTexture operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="toState">The value of toState.</param>
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
    /// Performs the TransitionTextureView operation.
    /// </summary>
    /// <param name="textureView">The value of textureView.</param>
    /// <param name="toState">The value of toState.</param>
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
    /// Performs the TransitionBuffer operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="toState">The value of toState.</param>
    private void TransitionBuffer(D3D12DeviceBuffer buffer, ResourceStates toState) {
        if (!buffer.CanTransitionState || buffer.CurrentState == toState) {
            return;
        }

        this.Transition(buffer.NativeBuffer, buffer.CurrentState, toState);
        buffer.CurrentState = toState;
    }

    /// <summary>
    /// Performs the CaptureTextureStates operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <returns>The result of the CaptureTextureStates operation.</returns>
    private static ResourceStates[] CaptureTextureStates(D3D12Texture texture) {
        uint subresourceCount = texture.SubresourceCount;
        ResourceStates[] states = new ResourceStates[subresourceCount];
        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            states[subresource] = texture.GetSubresourceState(subresource);
        }

        return states;
    }

    /// <summary>
    /// Performs the RestoreTextureStates operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="previousStates">The value of previousStates.</param>
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
    /// Performs the WriteDebugMarker operation.
    /// </summary>
    /// <param name="name">The value of name.</param>
    /// <param name="beginEvent">The value of beginEvent.</param>
    /// <param name="setMarker">The value of setMarker.</param>
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
    /// Performs the GetDebugMarkerMethod operation.
    /// </summary>
    /// <param name="methodName">The value of methodName.</param>
    /// <returns>The result of the GetDebugMarkerMethod operation.</returns>
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
    /// Performs the D3DCompile operation.
    /// </summary>
    /// <param name="srcData">The value of srcData.</param>
    /// <param name="srcDataSize">The value of srcDataSize.</param>
    /// <param name="sourceName">The value of sourceName.</param>
    /// <param name="defines">The value of defines.</param>
    /// <param name="include">The value of include.</param>
    /// <param name="entryPoint">The value of entryPoint.</param>
    /// <param name="target">The value of target.</param>
    /// <param name="flags1">The value of flags1.</param>
    /// <param name="flags2">The value of flags2.</param>
    /// <param name="code">The value of code.</param>
    /// <param name="errorMsgs">The value of errorMsgs.</param>
    /// <returns>The result of the D3DCompile operation.</returns>
    private static extern int D3DCompile(byte[] srcData, nuint srcDataSize, string sourceName, IntPtr defines, IntPtr include, string entryPoint, string target, uint flags1, uint flags2, out IntPtr code, out IntPtr errorMsgs);

    /// <summary>
    /// Performs the BindGraphicsResource operation.
    /// </summary>
    /// <param name="bindingInfo">The value of bindingInfo.</param>
    /// <param name="resource">The value of resource.</param>
    /// <param name="dynamicOffset">The value of dynamicOffset.</param>
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
    /// Performs the BindComputeResource operation.
    /// </summary>
    /// <param name="bindingInfo">The value of bindingInfo.</param>
    /// <param name="resource">The value of resource.</param>
    /// <param name="dynamicOffset">The value of dynamicOffset.</param>
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
    /// Performs the BindDescriptorTableResource operation.
    /// </summary>
    /// <param name="bindingInfo">The value of bindingInfo.</param>
    /// <param name="resource">The value of resource.</param>
    /// <param name="compute">The value of compute.</param>
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
    /// Performs the ValidateTextureViewBindingSupport operation.
    /// </summary>
    /// <param name="textureView">The value of textureView.</param>
    /// <param name="requestedUsage">The value of requestedUsage.</param>
    /// <param name="bindingKind">The value of bindingKind.</param>
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
    /// Performs the BindDescriptorHeaps operation.
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
    /// Performs the AllocateSrvUavDescriptor operation.
    /// </summary>
    /// <param name="cpuHandle">The value of cpuHandle.</param>
    /// <param name="gpuHandle">The value of gpuHandle.</param>
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
    /// Performs the AllocateSamplerDescriptor operation.
    /// </summary>
    /// <param name="cpuHandle">The value of cpuHandle.</param>
    /// <param name="gpuHandle">The value of gpuHandle.</param>
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
    /// Performs the GetOrCreateDescriptorTableHandle operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="kind">The value of kind.</param>
    /// <param name="createHandle">The value of createHandle.</param>
    /// <returns>The result of the GetOrCreateDescriptorTableHandle operation.</returns>
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
    /// Performs the GetGraphicsResourceSetBindingPlan operation.
    /// </summary>
    /// <param name="pipeline">The value of pipeline.</param>
    /// <param name="slot">The value of slot.</param>
    /// <param name="layout">The value of layout.</param>
    /// <returns>The result of the GetGraphicsResourceSetBindingPlan operation.</returns>
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
    /// Performs the GetComputeResourceSetBindingPlan operation.
    /// </summary>
    /// <param name="pipeline">The value of pipeline.</param>
    /// <param name="slot">The value of slot.</param>
    /// <param name="layout">The value of layout.</param>
    /// <returns>The result of the GetComputeResourceSetBindingPlan operation.</returns>
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
    /// Performs the CreateGraphicsResourceSetBindingPlan operation.
    /// </summary>
    /// <param name="pipeline">The value of pipeline.</param>
    /// <param name="slot">The value of slot.</param>
    /// <param name="elements">The value of elements.</param>
    /// <returns>The result of the CreateGraphicsResourceSetBindingPlan operation.</returns>
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
    /// Performs the CreateComputeResourceSetBindingPlan operation.
    /// </summary>
    /// <param name="pipeline">The value of pipeline.</param>
    /// <param name="slot">The value of slot.</param>
    /// <param name="elements">The value of elements.</param>
    /// <returns>The result of the CreateComputeResourceSetBindingPlan operation.</returns>
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
    /// Performs the GetGraphicsBufferState operation.
    /// </summary>
    /// <param name="kind">The value of kind.</param>
    /// <returns>The result of the GetGraphicsBufferState operation.</returns>
    private static ResourceStates GetGraphicsBufferState(ResourceKind kind) {
        switch (kind) {
            case ResourceKind.UniformBuffer: return ResourceStates.VertexAndConstantBuffer;
            case ResourceKind.StructuredBufferReadOnly: return ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource;
            case ResourceKind.StructuredBufferReadWrite: return ResourceStates.UnorderedAccess;
            default: return ResourceStates.Common;
        }
    }

    /// <summary>
    /// Performs the GetComputeBufferState operation.
    /// </summary>
    /// <param name="kind">The value of kind.</param>
    /// <returns>The result of the GetComputeBufferState operation.</returns>
    private static ResourceStates GetComputeBufferState(ResourceKind kind) {
        switch (kind) {
            case ResourceKind.UniformBuffer: return ResourceStates.VertexAndConstantBuffer;
            case ResourceKind.StructuredBufferReadOnly: return ResourceStates.NonPixelShaderResource;
            case ResourceKind.StructuredBufferReadWrite: return ResourceStates.UnorderedAccess;
            default: return ResourceStates.Common;
        }
    }

    /// <summary>
    /// Performs the IsSameGraphicsRootBuffer operation.
    /// </summary>
    /// <param name="rootParameterIndex">The value of rootParameterIndex.</param>
    /// <param name="gpuAddress">The value of gpuAddress.</param>
    /// <returns>The result of the IsSameGraphicsRootBuffer operation.</returns>
    private bool IsSameGraphicsRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddresses, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddressValid, rootParameterIndex + 1);
        return this._graphicsRootBufferAddressValid[index] && this._graphicsRootBufferAddresses[index] == gpuAddress;
    }

    /// <summary>
    /// Performs the SetGraphicsRootBufferCache operation.
    /// </summary>
    /// <param name="rootParameterIndex">The value of rootParameterIndex.</param>
    /// <param name="gpuAddress">The value of gpuAddress.</param>
    private void SetGraphicsRootBufferCache(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddresses, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddressValid, rootParameterIndex + 1);
        this._graphicsRootBufferAddresses[index] = gpuAddress;
        this._graphicsRootBufferAddressValid[index] = true;
    }

    /// <summary>
    /// Performs the IsSameComputeRootBuffer operation.
    /// </summary>
    /// <param name="rootParameterIndex">The value of rootParameterIndex.</param>
    /// <param name="gpuAddress">The value of gpuAddress.</param>
    /// <returns>The result of the IsSameComputeRootBuffer operation.</returns>
    private bool IsSameComputeRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddresses, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddressValid, rootParameterIndex + 1);
        return this._computeRootBufferAddressValid[index] && this._computeRootBufferAddresses[index] == gpuAddress;
    }

    /// <summary>
    /// Performs the SetComputeRootBufferCache operation.
    /// </summary>
    /// <param name="rootParameterIndex">The value of rootParameterIndex.</param>
    /// <param name="gpuAddress">The value of gpuAddress.</param>
    private void SetComputeRootBufferCache(uint rootParameterIndex, ulong gpuAddress) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddresses, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddressValid, rootParameterIndex + 1);
        this._computeRootBufferAddresses[index] = gpuAddress;
        this._computeRootBufferAddressValid[index] = true;
    }

    /// <summary>
    /// Performs the IsSameGraphicsRootTable operation.
    /// </summary>
    /// <param name="rootParameterIndex">The value of rootParameterIndex.</param>
    /// <param name="tablePtr">The value of tablePtr.</param>
    /// <returns>The result of the IsSameGraphicsRootTable operation.</returns>
    private bool IsSameGraphicsRootTable(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointers, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointerValid, rootParameterIndex + 1);
        return this._graphicsRootTablePointerValid[index] && this._graphicsRootTablePointers[index] == tablePtr;
    }

    /// <summary>
    /// Performs the SetGraphicsRootTableCache operation.
    /// </summary>
    /// <param name="rootParameterIndex">The value of rootParameterIndex.</param>
    /// <param name="tablePtr">The value of tablePtr.</param>
    private void SetGraphicsRootTableCache(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointers, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointerValid, rootParameterIndex + 1);
        this._graphicsRootTablePointers[index] = tablePtr;
        this._graphicsRootTablePointerValid[index] = true;
    }

    /// <summary>
    /// Performs the IsSameComputeRootTable operation.
    /// </summary>
    /// <param name="rootParameterIndex">The value of rootParameterIndex.</param>
    /// <param name="tablePtr">The value of tablePtr.</param>
    /// <returns>The result of the IsSameComputeRootTable operation.</returns>
    private bool IsSameComputeRootTable(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._computeRootTablePointers, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._computeRootTablePointerValid, rootParameterIndex + 1);
        return this._computeRootTablePointerValid[index] && this._computeRootTablePointers[index] == tablePtr;
    }

    /// <summary>
    /// Performs the SetComputeRootTableCache operation.
    /// </summary>
    /// <param name="rootParameterIndex">The value of rootParameterIndex.</param>
    /// <param name="tablePtr">The value of tablePtr.</param>
    private void SetComputeRootTableCache(uint rootParameterIndex, ulong tablePtr) {
        int index = (int)rootParameterIndex;
        Util.EnsureArrayMinimumSize(ref this._computeRootTablePointers, rootParameterIndex + 1);
        Util.EnsureArrayMinimumSize(ref this._computeRootTablePointerValid, rootParameterIndex + 1);
        this._computeRootTablePointers[index] = tablePtr;
        this._computeRootTablePointerValid[index] = true;
    }

    /// <summary>
    /// Performs the InvalidateGraphicsRootCaches operation.
    /// </summary>
    private void InvalidateGraphicsRootCaches() {
        Array.Clear(this._graphicsRootBufferAddressValid, 0, this._graphicsRootBufferAddressValid.Length);
        Array.Clear(this._graphicsRootTablePointerValid, 0, this._graphicsRootTablePointerValid.Length);
    }

    /// <summary>
    /// Performs the InvalidateComputeRootCaches operation.
    /// </summary>
    private void InvalidateComputeRootCaches() {
        Array.Clear(this._computeRootBufferAddressValid, 0, this._computeRootBufferAddressValid.Length);
        Array.Clear(this._computeRootTablePointerValid, 0, this._computeRootTablePointerValid.Length);
    }

    /// <summary>
    /// Performs the ClearBoundResourceSets operation.
    /// </summary>
    /// <param name="infos">The value of infos.</param>
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
    /// Performs the TransitionSwapchainBackBuffersToPresent operation.
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
        /// Performs the GetBufferPointer operation.
        /// </summary>
        /// <returns>The result of the GetBufferPointer operation.</returns>
        IntPtr GetBufferPointer();

        [PreserveSig]

        /// <summary>
        /// Performs the GetBufferSize operation.
        /// </summary>
        /// <returns>The result of the GetBufferSize operation.</returns>
        nuint GetBufferSize();
    }

    /// <summary>
    /// Represents the DescriptorCacheKey struct.
    /// </summary>
    private readonly struct DescriptorCacheKey {

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorCacheKey" /> type.
        /// </summary>
        /// <param name="resource">The value of resource.</param>
        /// <param name="kind">The value of kind.</param>
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
    /// Represents the ResourceSetBindingPlanKey struct.
    /// </summary>
    private readonly struct ResourceSetBindingPlanKey {

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSetBindingPlanKey" /> type.
        /// </summary>
        /// <param name="pipeline">The value of pipeline.</param>
        /// <param name="layout">The value of layout.</param>
        /// <param name="slot">The value of slot.</param>
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
    /// Represents the ResourceSetBindingPlanEntry struct.
    /// </summary>
    private readonly struct ResourceSetBindingPlanEntry {

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSetBindingPlanEntry" /> type.
        /// </summary>
        /// <param name="elementIndex">The value of elementIndex.</param>
        /// <param name="bindingInfo">The value of bindingInfo.</param>
        /// <param name="isDynamicBinding">The value of isDynamicBinding.</param>
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
    /// Represents the DescriptorCacheKeyComparer class.
    /// </summary>
    private sealed class DescriptorCacheKeyComparer : IEqualityComparer<DescriptorCacheKey> {

        /// <summary>
        /// Performs the new operation.
        /// </summary>
        /// <returns>The result of the new operation.</returns>
        public static readonly DescriptorCacheKeyComparer Instance = new();

        /// <summary>
        /// Performs the Equals operation.
        /// </summary>
        /// <param name="x">The value of x.</param>
        /// <param name="y">The value of y.</param>
        /// <returns>The result of the Equals operation.</returns>
        public bool Equals(DescriptorCacheKey x, DescriptorCacheKey y) {
            return x.Kind == y.Kind && ReferenceEquals(x.Resource, y.Resource);
        }

        /// <summary>
        /// Performs the GetHashCode operation.
        /// </summary>
        /// <param name="obj">The value of obj.</param>
        /// <returns>The result of the GetHashCode operation.</returns>
        public int GetHashCode(DescriptorCacheKey obj) {
            return HashCode.Combine((int)obj.Kind, RuntimeHelpers.GetHashCode(obj.Resource));
        }
    }

    /// <summary>
    /// Represents the ResourceSetBindingPlanKeyComparer class.
    /// </summary>
    private sealed class ResourceSetBindingPlanKeyComparer : IEqualityComparer<ResourceSetBindingPlanKey> {

        /// <summary>
        /// Performs the new operation.
        /// </summary>
        /// <returns>The result of the new operation.</returns>
        public static readonly ResourceSetBindingPlanKeyComparer Instance = new();

        /// <summary>
        /// Performs the Equals operation.
        /// </summary>
        /// <param name="x">The value of x.</param>
        /// <param name="y">The value of y.</param>
        /// <returns>The result of the Equals operation.</returns>
        public bool Equals(ResourceSetBindingPlanKey x, ResourceSetBindingPlanKey y) {
            return x.Slot == y.Slot && ReferenceEquals(x.Pipeline, y.Pipeline) && ReferenceEquals(x.Layout, y.Layout);
        }

        /// <summary>
        /// Performs the GetHashCode operation.
        /// </summary>
        /// <param name="obj">The value of obj.</param>
        /// <returns>The result of the GetHashCode operation.</returns>
        public int GetHashCode(ResourceSetBindingPlanKey obj) {
            return HashCode.Combine((int)obj.Slot, RuntimeHelpers.GetHashCode(obj.Pipeline), RuntimeHelpers.GetHashCode(obj.Layout));
        }
    }
}