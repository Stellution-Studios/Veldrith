using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using SharpGen.Runtime;
using Vortice;
using Vortice.Direct3D12;
using Vortice.Mathematics;

#if !VELDRID_D3D12_PERF
#pragma warning disable CS0162
#endif

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12CommandList.
/// </summary>
internal sealed class D3D12CommandList : CommandList {

    private const int DrawDirtyPendingBufferUploads = 1 << 0;
    private const int DrawDirtyGraphicsResourceSets = 1 << 1;
    private const int DrawDirtyUavBarrier = 1 << 2;
    private const int DrawDirtyResourceBarriers = 1 << 3;
    private const int DrawDirtyDynamicInputAssembler = 1 << 4;

    /// <summary>
    /// Controls the experimental stable ResourceSet buffer update bypass.
    /// </summary>
    // Dynamic ResourceSet buffers must use command-list snapshots. Reusing the stable
    // upload backing store can overwrite data still read by an in-flight frame.
    private const bool _stableResourceSetUpdateFastPathEnabled = false;

    /// <summary>
    /// Stores the begin event method state used by this instance.
    /// </summary>
    private readonly MethodInfo _beginEventMethod;

    /// <summary>
    /// Stores whether the BeginEvent metadata parameter uses a signed integer.
    /// </summary>
    private readonly bool _beginEventMetadataIsInt;

    /// <summary>
    /// Stores whether the BeginEvent size parameter uses a signed integer.
    /// </summary>
    private readonly bool _beginEventSizeIsInt;

    /// <summary>
    /// Tracks input-assembler bindings and small cached graphics state.
    /// </summary>
    private readonly D3D12InputAssemblerState _inputAssembler = new();

    /// <summary>
    /// Stores the end event method state used by this instance.
    /// </summary>
    private readonly MethodInfo _endEventMethod;

    /// <summary>
    /// Binds dirty ResourceSets through D3D12 root descriptors and descriptor tables.
    /// </summary>
    private readonly D3D12DescriptorSetBinder _descriptorSetBinder;

    /// <summary>
    /// Tracks optional D3D12 command-list recording performance metrics.
    /// </summary>
    private readonly D3D12CommandListPerfTracker _perf = new();

    /// <summary>
    /// Owns command allocator rotation and submission-fence waits for this command list.
    /// </summary>
    private readonly D3D12CommandListFrameState _frameState;

    /// <summary>
    /// Owns D3D12 command signatures used by indirect draw and dispatch commands.
    /// </summary>
    private readonly D3D12IndirectCommandSignatures _indirectCommandSignatures;

    /// <summary>
    /// Owns the optional compute path used to generate mipmaps on the GPU.
    /// </summary>
    private readonly D3D12GpuMipmapGenerator _gpuMipmapGenerator;

    /// <summary>
    /// Owns D3D12 graphics and compute pipeline binding state.
    /// </summary>
    private readonly D3D12PipelineStateBinder _pipelineStateBinder;

    /// <summary>
    /// Owns D3D12 render-target binding and render-target state transitions.
    /// </summary>
    private readonly D3D12RenderTargetBinder _renderTargetBinder;

    /// <summary>
    /// Plans and records D3D12 framebuffer clear operations.
    /// </summary>
    private readonly D3D12ClearPlanner _clearPlanner;

    /// <summary>
    /// Tracks native D3D12 render-pass lifetime for draw commands.
    /// </summary>
    private readonly D3D12RenderPassTracker _renderPassTracker;

    /// <summary>
    /// Plans and records D3D12 buffer update operations.
    /// </summary>
    private readonly D3D12BufferUpdatePlanner _bufferUpdatePlanner;

    /// <summary>
    /// Tracks dynamic viewport and scissor state to avoid redundant D3D12 calls.
    /// </summary>
    private readonly D3D12ViewportScissorState _viewportScissor = new();

    /// <summary>
    /// Tracks root descriptor state to skip redundant D3D12 root binding calls.
    /// </summary>
    private readonly D3D12RootBindingCache _rootBindingCache = new();

    /// <summary>
    /// Stores the set marker method state used by this instance.
    /// </summary>
    private readonly MethodInfo _setMarkerMethod;

    /// <summary>
    /// Stores whether the SetMarker metadata parameter uses a signed integer.
    /// </summary>
    private readonly bool _setMarkerMetadataIsInt;

    /// <summary>
    /// Stores whether the SetMarker size parameter uses a signed integer.
    /// </summary>
    private readonly bool _setMarkerSizeIsInt;

    /// <summary>
    /// Reuses reflection argument storage for debug marker calls.
    /// </summary>
    private readonly object[] _debugMarkerArgs = new object[3];

    /// <summary>
    /// Tracks pending D3D12 resource barriers and emits them in batches.
    /// </summary>
    private readonly D3D12ResourceBarrierTracker _barriers = new();

    /// <summary>
    /// Stores dynamic buffers whose command-list-local snapshot address changed and may need a deferred IA rebind.
    /// </summary>
    private D3D12DeviceBuffer[] _dynamicBindingRefreshBuffers = new D3D12DeviceBuffer[8];

    /// <summary>
    /// Stores the number of active dynamic IA refresh entries.
    /// </summary>
    private int _dynamicBindingRefreshBufferCount;

    /// <summary>
    /// Stores ResourceSet buffers that have already been used by a draw or dispatch in this command-list recording.
    /// </summary>
    private D3D12DeviceBuffer[] _usedResourceSetBuffers = new D3D12DeviceBuffer[16];

    /// <summary>
    /// Stores the number of active ResourceSet buffer usage entries.
    /// </summary>
    private int _usedResourceSetBufferCount;

    /// <summary>
    /// Stores the most recently tracked ResourceSet buffer.
    /// </summary>
    private D3D12DeviceBuffer _lastUsedResourceSetBuffer;

    /// <summary>
    /// Stores input-assembler buffers that have already been used by a draw in this command-list recording.
    /// </summary>
    private D3D12DeviceBuffer[] _usedInputAssemblerBuffers = new D3D12DeviceBuffer[16];

    /// <summary>
    /// Stores the number of active input-assembler usage entries.
    /// </summary>
    private int _usedInputAssemblerBufferCount;

    /// <summary>
    /// Stores the most recently tracked input-assembler buffer.
    /// </summary>
    private D3D12DeviceBuffer _lastUsedInputAssemblerBuffer;

    /// <summary>
    /// Tracks whether input-assembler buffer usage has been captured for the current dynamic IA binding version.
    /// </summary>
    private bool _inputAssemblerUsageCaptured;

    /// <summary>
    /// Stores the dynamic IA binding version that has already been captured.
    /// </summary>
    private uint _inputAssemblerUsageVersion;

    /// <summary>
    /// Tracks whether graphics ResourceSet buffer usage has been captured for the current state version.
    /// </summary>
    private bool _graphicsResourceSetUsageCaptured;

    /// <summary>
    /// Stores the graphics ResourceSet buffer-membership version that has already been captured.
    /// </summary>
    private uint _graphicsResourceSetUsageVersion;

    /// <summary>
    /// Stores the active graphics ResourceSet count that has already been captured.
    /// </summary>
    private uint _graphicsResourceSetUsageCount;

    /// <summary>
    /// Tracks whether compute ResourceSet buffer usage has been captured for the current state version.
    /// </summary>
    private bool _computeResourceSetUsageCaptured;

    /// <summary>
    /// Stores the compute ResourceSet buffer-membership version that has already been captured.
    /// </summary>
    private uint _computeResourceSetUsageVersion;

    /// <summary>
    /// Stores the active compute ResourceSet count that has already been captured.
    /// </summary>
    private uint _computeResourceSetUsageCount;

    /// <summary>
    /// Plans and records D3D12 texture copy/resolve operations.
    /// </summary>
    private readonly D3D12TextureCopyPlanner _textureCopyPlanner;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Stores the native command-list COM pointer for no-alloc hotpath calls.
    /// </summary>
    private readonly nint _nativeCommandListPointer;

    /// <summary>
    /// Stores the native command-list vtable pointer for no-alloc hotpath calls.
    /// </summary>
    private readonly nint _nativeCommandListVTable;

    private readonly unsafe delegate* unmanaged[Stdcall]<void*, int> _closeCommandList;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, void*, void*, int> _resetCommandList;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, uint, uint, uint, void> _drawInstanced;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, uint, uint, int, uint, void> _drawIndexedInstanced;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, uint, uint, void> _dispatch;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, void*, ulong, void*, ulong, ulong, void> _copyBufferRegion;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, void*, void> _setPipelineState;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, void*, void> _setComputeRootSignature;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, void*, void> _setGraphicsRootSignature;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, ulong, void> _setComputeRootConstantBufferView;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, ulong, void> _setGraphicsRootConstantBufferView;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, ulong, void> _setComputeRootShaderResourceView;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, ulong, void> _setGraphicsRootShaderResourceView;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, ulong, void> _setComputeRootUnorderedAccessView;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, ulong, void> _setGraphicsRootUnorderedAccessView;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, ulong, void> _setComputeRootDescriptorTable;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, ulong, void> _setGraphicsRootDescriptorTable;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, uint, void*, uint, void> _setComputeRoot32BitConstants;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, uint, void*, uint, void> _setGraphicsRoot32BitConstants;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, void*, void> _setIndexBuffer;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, uint, void*, void> _setVertexBuffers;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, int, void> _setPrimitiveTopology;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, void> _setStencilReference;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, uint, void*, int, void*, void> _setRenderTargets;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, CpuDescriptorHandle, uint, float, byte, uint, void*, void> _clearDepthStencilView;
    private readonly unsafe delegate* unmanaged[Stdcall]<void*, CpuDescriptorHandle, float*, uint, void*, void> _clearRenderTargetView;

    /// <summary>
    /// Stores upload resources recorded on this command list until submission assigns a fence value.
    /// </summary>
    private readonly List<D3D12ResourceAllocation> _pendingSubmissionUploadBuffers = new();

    /// <summary>
    /// Stores the begun state used by this instance.
    /// </summary>
    private bool _begun;

    /// <summary>
    /// Tracks currently bound compute resource sets and dirty slots.
    /// </summary>
    private readonly D3D12BoundResourceSetState _computeResourceSets = new();

    /// <summary>
    /// Tracks currently bound graphics resource sets and dirty slots.
    /// </summary>
    private readonly D3D12BoundResourceSetState _graphicsResourceSets = new();

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the ended state used by this instance.
    /// </summary>
    private bool _ended;

    /// <summary>
    /// Tracks draw pre-work that is known to be pending.
    /// </summary>
    private int _drawDirtyFlags;

    /// <summary>
    /// Caches swapchain back-buffer state for the current command-list recording.
    /// </summary>
    private readonly D3D12SwapchainBackBufferTracker _swapchainBackBuffer = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12CommandList" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="features">The features value used by this operation.</param>
    /// <param name="uniformAlignment">The uniform alignment value used by this operation.</param>
    /// <param name="structuredAlignment">The structured alignment value used by this operation.</param>
    public D3D12CommandList(D3D12GraphicsDevice gd, ref CommandListDescription description, GraphicsDeviceFeatures features, uint uniformAlignment, uint structuredAlignment) : base(ref description, features, uniformAlignment, structuredAlignment) {
        this._gd = gd;

        this._frameState = new D3D12CommandListFrameState(gd);
        this._descriptorSetBinder = new D3D12DescriptorSetBinder(gd, this, this._rootBindingCache, this._perf);
        this._indirectCommandSignatures = new D3D12IndirectCommandSignatures(gd);
        this._gpuMipmapGenerator = new D3D12GpuMipmapGenerator(gd, this);
        this._textureCopyPlanner = new D3D12TextureCopyPlanner(this);
        this._pipelineStateBinder = new D3D12PipelineStateBinder(this, this._graphicsResourceSets, this._computeResourceSets, this._rootBindingCache, this._perf);
        this._renderTargetBinder = new D3D12RenderTargetBinder(this, this._swapchainBackBuffer, this._perf);
        this._clearPlanner = new D3D12ClearPlanner(this, this._swapchainBackBuffer);
        this._renderPassTracker = new D3D12RenderPassTracker(this, this._swapchainBackBuffer);
        this._bufferUpdatePlanner = new D3D12BufferUpdatePlanner(gd, this, this._perf);
        this.NativeCommandList = gd.Device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, this._frameState.InitialAllocator);
        unsafe {
            this._nativeCommandListPointer = this.NativeCommandList.NativePointer;
            this._nativeCommandListVTable = (nint)(*(void***)this._nativeCommandListPointer);
            void** vtbl = (void**)this._nativeCommandListVTable;
            this._closeCommandList = (delegate* unmanaged[Stdcall]<void*, int>)vtbl[9];
            this._resetCommandList = (delegate* unmanaged[Stdcall]<void*, void*, void*, int>)vtbl[10];
            this._drawInstanced = (delegate* unmanaged[Stdcall]<void*, uint, uint, uint, uint, void>)vtbl[12];
            this._drawIndexedInstanced = (delegate* unmanaged[Stdcall]<void*, uint, uint, uint, int, uint, void>)vtbl[13];
            this._dispatch = (delegate* unmanaged[Stdcall]<void*, uint, uint, uint, void>)vtbl[14];
            this._copyBufferRegion = (delegate* unmanaged[Stdcall]<void*, void*, ulong, void*, ulong, ulong, void>)vtbl[15];
            this._setPrimitiveTopology = (delegate* unmanaged[Stdcall]<void*, int, void>)vtbl[20];
            this._setStencilReference = (delegate* unmanaged[Stdcall]<void*, uint, void>)vtbl[24];
            this._setPipelineState = (delegate* unmanaged[Stdcall]<void*, void*, void>)vtbl[25];
            this._setComputeRootSignature = (delegate* unmanaged[Stdcall]<void*, void*, void>)vtbl[29];
            this._setGraphicsRootSignature = (delegate* unmanaged[Stdcall]<void*, void*, void>)vtbl[30];
            this._setComputeRootDescriptorTable = (delegate* unmanaged[Stdcall]<void*, uint, ulong, void>)vtbl[31];
            this._setGraphicsRootDescriptorTable = (delegate* unmanaged[Stdcall]<void*, uint, ulong, void>)vtbl[32];
            this._setComputeRoot32BitConstants = (delegate* unmanaged[Stdcall]<void*, uint, uint, void*, uint, void>)vtbl[35];
            this._setGraphicsRoot32BitConstants = (delegate* unmanaged[Stdcall]<void*, uint, uint, void*, uint, void>)vtbl[36];
            this._setComputeRootConstantBufferView = (delegate* unmanaged[Stdcall]<void*, uint, ulong, void>)vtbl[37];
            this._setGraphicsRootConstantBufferView = (delegate* unmanaged[Stdcall]<void*, uint, ulong, void>)vtbl[38];
            this._setComputeRootShaderResourceView = (delegate* unmanaged[Stdcall]<void*, uint, ulong, void>)vtbl[39];
            this._setGraphicsRootShaderResourceView = (delegate* unmanaged[Stdcall]<void*, uint, ulong, void>)vtbl[40];
            this._setComputeRootUnorderedAccessView = (delegate* unmanaged[Stdcall]<void*, uint, ulong, void>)vtbl[41];
            this._setGraphicsRootUnorderedAccessView = (delegate* unmanaged[Stdcall]<void*, uint, ulong, void>)vtbl[42];
            this._setIndexBuffer = (delegate* unmanaged[Stdcall]<void*, void*, void>)vtbl[43];
            this._setVertexBuffers = (delegate* unmanaged[Stdcall]<void*, uint, uint, void*, void>)vtbl[44];
            this._setRenderTargets = (delegate* unmanaged[Stdcall]<void*, uint, void*, int, void*, void>)vtbl[46];
            this._clearDepthStencilView = (delegate* unmanaged[Stdcall]<void*, CpuDescriptorHandle, uint, float, byte, uint, void*, void>)vtbl[47];
            this._clearRenderTargetView = (delegate* unmanaged[Stdcall]<void*, CpuDescriptorHandle, float*, uint, void*, void>)vtbl[48];
        }

        if (gd.SupportsDirectWriteBufferImmediate) {
            try {
                this.NativeCommandList2 = this.NativeCommandList.QueryInterface<ID3D12GraphicsCommandList2>();
            }
            catch (SharpGenException) {
                this.NativeCommandList2 = null;
            }
        }

        if (gd.SupportsRenderPasses) {
            try {
                this.NativeCommandList4 = this.NativeCommandList.QueryInterface<ID3D12GraphicsCommandList4>();
            }
            catch (SharpGenException) {
                this.NativeCommandList4 = null;
            }
        }

        this._beginEventMethod = this.GetDebugMarkerMethod("BeginEvent");
        this._setMarkerMethod = this.GetDebugMarkerMethod("SetMarker");
        this._endEventMethod = this.NativeCommandList.GetType().GetMethod("EndEvent", Type.EmptyTypes);
        this._beginEventMetadataIsInt = IsDebugMarkerParameterInt(this._beginEventMethod, 0);
        this._beginEventSizeIsInt = IsDebugMarkerParameterInt(this._beginEventMethod, 2);
        this._setMarkerMetadataIsInt = IsDebugMarkerParameterInt(this._setMarkerMethod, 0);
        this._setMarkerSizeIsInt = IsDebugMarkerParameterInt(this._setMarkerMethod, 2);
        this.NativeCommandList.Close();
    }

    /// <summary>
    /// Gets or sets NativeCommandList.
    /// </summary>
    public ID3D12GraphicsCommandList NativeCommandList { get; }

    /// <summary>
    /// Gets the optional command-list interface used for WriteBufferImmediate.
    /// </summary>
    internal ID3D12GraphicsCommandList2 NativeCommandList2 { get; }

    /// <summary>
    /// Gets the optional command-list interface used for native D3D12 render passes.
    /// </summary>
    internal ID3D12GraphicsCommandList4 NativeCommandList4 { get; }

    /// <summary>
    /// Gets or sets the currently recorded graphics pipeline for internal D3D12 helpers.
    /// </summary>
    internal D3D12Pipeline CurrentGraphicsPipeline {
        get => this._pipelineStateBinder.CurrentGraphicsPipeline;
        set => this._pipelineStateBinder.CurrentGraphicsPipeline = value;
    }

    /// <summary>
    /// Gets or sets the currently recorded compute pipeline for internal D3D12 helpers.
    /// </summary>
    internal D3D12Pipeline CurrentComputePipeline {
        get => this._pipelineStateBinder.CurrentComputePipeline;
        set => this._pipelineStateBinder.CurrentComputePipeline = value;
    }

    /// <summary>
    /// Gets the compute resource-set state for internal D3D12 helpers that temporarily alter compute bindings.
    /// </summary>
    internal D3D12BoundResourceSetState ComputeResourceSets => this._computeResourceSets;

    /// <summary>
    /// Gets the graphics resource-set state for internal D3D12 helpers that temporarily alter graphics bindings.
    /// </summary>
    internal D3D12BoundResourceSetState GraphicsResourceSets => this._graphicsResourceSets;

    /// <summary>
    /// Gets whether stable ResourceSet buffer updates may bypass command-list snapshots.
    /// </summary>
    internal static bool StableResourceSetUpdateFastPathEnabled => _stableResourceSetUpdateFastPathEnabled;

    /// <summary>
    /// Gets the root binding cache for internal D3D12 helpers that temporarily bind root signatures.
    /// </summary>
    internal D3D12RootBindingCache RootBindingCache => this._rootBindingCache;

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

        this._frameState.WaitForSubmittedFrameSlots();
        this.DisposePendingSubmissionDisposals();
        this._gpuMipmapGenerator.Dispose();
        this._indirectCommandSignatures.Dispose();
        this._bufferUpdatePlanner.DiscardPendingUploads();
        this._graphicsResourceSets.Clear();
        this._computeResourceSets.Clear();
        this.NativeCommandList4?.Dispose();
        this.NativeCommandList2?.Dispose();
        this.NativeCommandList.Dispose();
        this._frameState.Dispose();
        this._descriptorSetBinder.Dispose();
        this._disposed = true;
    }

    /// <summary>
    /// Begins the value operation.
    /// </summary>
    public override void Begin() {
        this.DisposePendingSubmissionDisposals();
        this._perf.BeginRecording();

        ID3D12CommandAllocator allocator = this._frameState.BeginRecording(this._perf);
        this.ResetCommandListNoAlloc(allocator);
        this._descriptorSetBinder.BeginRecording();
        this._barriers.Reset();
        this._bufferUpdatePlanner.BeginRecording();
        this._begun = true;
        this._ended = false;
        this._swapchainBackBuffer.Reset();
        this._renderTargetBinder.Reset();
        this._renderPassTracker.Reset();
        this._viewportScissor.Reset();
        this._inputAssembler.Reset();
        this.ClearDynamicBindingRefreshBuffers();
        this.ClearUsedResourceSetBuffers();
        this.ClearUsedInputAssemblerBuffers();
        this._drawDirtyFlags = 0;
        this._graphicsResourceSets.Clear();
        this._computeResourceSets.Clear();
        this._rootBindingCache.InvalidateGraphics();
        this._rootBindingCache.InvalidateCompute();
        this.ClearCachedState();
        this._pipelineStateBinder.BeginRecording();

    }

    /// <summary>
    /// Ends the value operation.
    /// </summary>
    public override void End() {
        if (!this._begun) {
            throw new VeldridException("CommandList.End cannot be called before Begin.");
        }

        this.FlushPendingBufferUploads();
        this.FlushPendingUavBarrier();
        this.EndRenderPassForInternalUse();
        this.FlushQueuedRenderPassClears();
        this._swapchainBackBuffer.TransitionToPresent(this);
        this.FlushPendingBarriers();
        this.CloseNoAlloc();
        this._ended = true;
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.BarrierCoalescedTransitions = this._barriers.CoalescedTransitions;
            this._perf.BarrierRemovedTransitions = this._barriers.RemovedTransitions;
            this._perf.AllocatorSlots = this._frameState.AllocatorSlotCount;
        }

        this._perf.EndRecording();
    }

    /// <summary>
    /// Sets the viewport value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="viewport">The viewport value used by this operation.</param>
    public override void SetViewport(uint index, ref Viewport viewport) {
        this._viewportScissor.SetViewport(this.NativeCommandList, index, ref viewport);
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
        this._viewportScissor.SetScissorRect(this.NativeCommandList, index, x, y, width, height);
    }

    /// <summary>
    /// Executes the dispatch logic for this backend.
    /// </summary>
    /// <param name="groupCountX">The group count x value used by this operation.</param>
    /// <param name="groupCountY">The group count y value used by this operation.</param>
    /// <param name="groupCountZ">The group count z value used by this operation.</param>
    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ) {
        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        this.EndRenderPassForInternalUse();
        this.FlushQueuedRenderPassClears();
        this.FlushPendingBufferUploads();
        this.FlushComputeResourceSets();
        if (_stableResourceSetUpdateFastPathEnabled) {
            this.MarkComputeResourceSetBuffersUsed();
        }

        this.FlushPendingUavBarrier();
        this.FlushPendingBarriers();
        this.DispatchNoAlloc(groupCountX, groupCountY, groupCountZ);
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.DispatchCalls++;
            this._perf.DispatchMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }

        this._barriers.UavBarrierPending = true;
        this.MarkDrawDirty(DrawDirtyUavBarrier);
    }

    /// <summary>
    /// Executes the execute no signal logic for this backend.
    /// </summary>
    internal void ExecuteNoSignal() {
        if (!this._ended) {
            throw new VeldridException("CommandList must be ended before submit.");
        }

        this._gd.ExecuteCommandListNoAlloc(this.NativeCommandList);
    }

    /// <summary>
    /// Executes the mark submitted logic for this backend.
    /// </summary>
    /// <param name="signalValue">The signal value value used by this operation.</param>
    internal void MarkSubmitted(ulong signalValue) {
        this._frameState.MarkSubmitted(signalValue);

        if (this._pendingSubmissionUploadBuffers.Count == 0) {
            return;
        }

        this._gd.EnqueueSubmissionUploadBuffers(this._pendingSubmissionUploadBuffers, signalValue);
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
        if (this.CurrentGraphicsPipeline == null) {
            return;
        }

        if (!this._graphicsResourceSets.TrySet(slot, rs, dynamicOffsetsCount, ref dynamicOffsets)) {
            return;
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.ResourceSetChanges++;
        }

        this.MarkDrawDirty(DrawDirtyGraphicsResourceSets);
        Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(rs);
    }

    /// <summary>
    /// Sets the compute resource set core value.
    /// </summary>
    /// <param name="slot">The slot value used by this operation.</param>
    /// <param name="set">The set value used by this operation.</param>
    /// <param name="dynamicOffsetsCount">The dynamic offsets count value used by this operation.</param>
    /// <param name="dynamicOffsets">The dynamic offsets value used by this operation.</param>
    protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets) {
        if (this.CurrentComputePipeline == null) {
            return;
        }

        if (!this._computeResourceSets.TrySet(slot, set, dynamicOffsetsCount, ref dynamicOffsets)) {
            return;
        }

        this.EndRenderPassForInternalUse();
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.ResourceSetChanges++;
        }

        Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(set);
    }

    /// <summary>
    /// Sets the framebuffer core value.
    /// </summary>
    /// <param name="fb">The fb value used by this operation.</param>
    protected override void SetFramebufferCore(Framebuffer fb) {
        this.EndRenderPassForInternalUse();
        this.FlushQueuedRenderPassClears();
        this._renderTargetBinder.SetFramebuffer(fb);
    }

    /// <summary>
    /// Executes the draw indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="drawCount">The draw count value used by this operation.</param>
    /// <param name="stride">The stride value used by this operation.</param>
    protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride) {
        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        this.EndRenderPassForInternalUse();
        this.FlushPendingBufferUploads();
        this.FlushGraphicsResourceSets();
        if (_stableResourceSetUpdateFastPathEnabled && drawCount != 0) {
            this.MarkGraphicsResourceSetBuffersUsed();
        }

        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(indirectBuffer);
        uint argumentSize = (uint)Unsafe.SizeOf<IndirectDrawArguments>();
        if (drawCount > 0) {
            ulong requiredSize = offset + (drawCount - 1UL) * stride + argumentSize;
            if (requiredSize > d3D12Buffer.SizeInBytes) {
                throw new VeldridException("Indirect draw argument range exceeds buffer bounds.");
            }
        }

        if (this._indirectCommandSignatures.EnsureAvailable()) {
            this.ExecuteIndirect(d3D12Buffer, offset, drawCount, stride, argumentSize, this._indirectCommandSignatures.Draw, true);
            if (drawCount != 0) {
                this.MarkInputAssemblerBuffersUsed();
            }

            if (D3D12CommandListPerfTracker.Enabled) {
                this._perf.DrawCalls += drawCount;
                this._perf.DrawMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            }

            return;
        }

        // Fallback path if command signatures are unavailable.
        unsafe {
            if (!d3D12Buffer.TryGetCpuReadPointer(out IntPtr mappedPointer)) {
                throw new PlatformNotSupportedException("D3D12 indirect fallback requires a CPU-readable argument buffer.");
            }

            this.BeginRenderPassForDraw();
            byte* basePtr = (byte*)mappedPointer + offset;
            for (uint i = 0; i < drawCount; i++) {
                IndirectDrawArguments arguments = *(IndirectDrawArguments*)(basePtr + i * stride);
                this.DrawInstancedNoAlloc(arguments.VertexCount, arguments.InstanceCount, arguments.FirstVertex, arguments.FirstInstance);
            }
        }

        if (drawCount != 0) {
            this.MarkInputAssemblerBuffersUsed();
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.DrawCalls += drawCount;
            this._perf.DrawMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
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
        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        this.EndRenderPassForInternalUse();
        this.FlushPendingBufferUploads();
        this.FlushGraphicsResourceSets();
        if (_stableResourceSetUpdateFastPathEnabled && drawCount != 0) {
            this.MarkGraphicsResourceSetBuffersUsed();
        }

        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(indirectBuffer);
        uint argumentSize = (uint)Unsafe.SizeOf<IndirectDrawIndexedArguments>();
        if (drawCount > 0) {
            ulong requiredSize = offset + (drawCount - 1UL) * stride + argumentSize;
            if (requiredSize > d3D12Buffer.SizeInBytes) {
                throw new VeldridException("Indirect indexed draw argument range exceeds buffer bounds.");
            }
        }

        if (this._indirectCommandSignatures.EnsureAvailable()) {
            this.ExecuteIndirect(d3D12Buffer, offset, drawCount, stride, argumentSize, this._indirectCommandSignatures.DrawIndexed, true);
            if (drawCount != 0) {
                this.MarkInputAssemblerBuffersUsed();
            }

            if (D3D12CommandListPerfTracker.Enabled) {
                this._perf.DrawCalls += drawCount;
                this._perf.DrawMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            }

            return;
        }

        // Fallback path if command signatures are unavailable.
        unsafe {
            if (!d3D12Buffer.TryGetCpuReadPointer(out IntPtr mappedPointer)) {
                throw new PlatformNotSupportedException("D3D12 indirect fallback requires a CPU-readable argument buffer.");
            }

            this.BeginRenderPassForDraw();
            byte* basePtr = (byte*)mappedPointer + offset;
            for (uint i = 0; i < drawCount; i++) {
                IndirectDrawIndexedArguments arguments = *(IndirectDrawIndexedArguments*)(basePtr + i * stride);
                this.DrawIndexedInstancedNoAlloc(arguments.IndexCount, arguments.InstanceCount, arguments.FirstIndex, arguments.VertexOffset, arguments.FirstInstance);
            }
        }

        if (drawCount != 0) {
            this.MarkInputAssemblerBuffersUsed();
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.DrawCalls += drawCount;
            this._perf.DrawMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Executes the dispatch indirect core logic for this backend.
    /// </summary>
    /// <param name="indirectBuffer">The indirect buffer value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset) {
        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        this.EndRenderPassForInternalUse();
        this.FlushQueuedRenderPassClears();
        this.FlushPendingBufferUploads();
        this.FlushComputeResourceSets();
        if (_stableResourceSetUpdateFastPathEnabled) {
            this.MarkComputeResourceSetBuffersUsed();
        }

        D3D12DeviceBuffer d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(indirectBuffer);
        uint argumentSize = (uint)Unsafe.SizeOf<IndirectDispatchArguments>();
        ulong requiredSize = (ulong)offset + argumentSize;
        if (requiredSize > d3d12Buffer.SizeInBytes) {
            throw new VeldridException("Indirect dispatch argument range exceeds buffer bounds.");
        }

        if (this._indirectCommandSignatures.EnsureAvailable()) {
            this.ExecuteIndirect(d3d12Buffer, offset, 1, argumentSize, argumentSize, this._indirectCommandSignatures.Dispatch, false);
            if (D3D12CommandListPerfTracker.Enabled) {
                this._perf.DispatchCalls++;
                this._perf.DispatchMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
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

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.DispatchCalls++;
            this._perf.DispatchMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Executes the resolve texture core logic for this backend.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="destination">The destination value or resource.</param>
    protected override void ResolveTextureCore(Texture source, Texture destination) {
        this.EndRenderPassForInternalUse();
        this.FlushQueuedRenderPassClears();
        this.FlushPendingBufferUploads();
        this._textureCopyPlanner.Resolve(source, destination);
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
        this.EndRenderPassForInternalUse();
        this.FlushPendingBufferUploads();
        this.FlushPendingUavBarrier();
        this.FlushPendingBarriers();
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
        this.EndRenderPassForInternalUse();
        this.FlushQueuedRenderPassClears();
        this.FlushPendingBufferUploads();
        this._textureCopyPlanner.Copy(source, srcX, srcY, srcZ, srcMipLevel, srcBaseArrayLayer, destination, dstX, dstY, dstZ, dstMipLevel, dstBaseArrayLayer, width, height, depth, layerCount);
    }

    /// <summary>
    /// Sets the pipeline core value.
    /// </summary>
    /// <param name="pipeline">The pipeline value used by this operation.</param>
    private protected override void SetPipelineCore(Pipeline pipeline) {
        if (pipeline.IsComputePipeline) {
            this.EndRenderPassForInternalUse();
        }

        this._pipelineStateBinder.SetPipeline(pipeline);
        if (!pipeline.IsComputePipeline && this._graphicsResourceSets.Dirty) {
            this.MarkDrawDirty(DrawDirtyGraphicsResourceSets);
        }
    }

    /// <summary>
    /// Sets the vertex buffer core value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset) {
        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
        bool isDynamicBuffer = (d3D12Buffer.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
        D3D12BufferBindingInfo bindingInfo = this.GetBufferBindingInfo(d3D12Buffer, offset);
        if (!this._inputAssembler.TrySetVertexBuffer(index, d3D12Buffer, offset, bindingInfo.BindVersion, isDynamicBuffer)) {
            return;
        }

        if (RequiresBufferTransition(d3D12Buffer, ResourceStates.VertexAndConstantBuffer)) {
            this.EndRenderPassForInternalUse();
        }

        this.BindVertexBuffer(index, d3D12Buffer, offset, in bindingInfo);
        this.ClearDynamicBindingRefreshIfCurrent(d3D12Buffer);
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.VertexBufferBinds++;
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
        D3D12BufferBindingInfo bindingInfo = this.GetBufferBindingInfo(d3D12Buffer, offset);
        if (!this._inputAssembler.NeedsIndexBufferBind(d3D12Buffer, format, offset, bindingInfo.BindVersion, isDynamicBuffer)) {
            return;
        }

        if (RequiresBufferTransition(d3D12Buffer, ResourceStates.IndexBuffer)) {
            this.EndRenderPassForInternalUse();
        }

        this.TransitionBuffer(d3D12Buffer, ResourceStates.IndexBuffer);
        IndexBufferView indexView = new(bindingInfo.GpuVirtualAddress, bindingInfo.BindableSize, D3D12Formats.ToDxgiFormat(format));
        this.SetIndexBufferNoAlloc(ref indexView);
        this._inputAssembler.SetIndexBuffer(d3D12Buffer, format, offset, bindingInfo.BindVersion, isDynamicBuffer);
        this.ClearDynamicBindingRefreshIfCurrent(d3D12Buffer);
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.IndexBufferBinds++;
        }
    }

    /// <summary>
    /// Executes the clear color target core logic for this backend.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="clearColor">The clear color value used by this operation.</param>
    private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor) {
        this.EndRenderPassForInternalUse();
        this.FlushPendingBufferUploads();
        if (this._renderPassTracker.TryQueueColorClear(this.Framebuffer, index, clearColor)) {
            return;
        }

        this._clearPlanner.ClearColorTarget(this.Framebuffer, index, clearColor);
    }

    /// <summary>
    /// Executes the clear depth stencil core logic for this backend.
    /// </summary>
    /// <param name="depth">The depth value.</param>
    /// <param name="stencil">The stencil value used by this operation.</param>
    private protected override void ClearDepthStencilCore(float depth, byte stencil) {
        this.EndRenderPassForInternalUse();
        this.FlushPendingBufferUploads();
        if (this._renderPassTracker.TryQueueDepthStencilClear(this.Framebuffer, depth, stencil)) {
            return;
        }

        this._clearPlanner.ClearDepthStencil(this.Framebuffer, depth, stencil);
    }

    /// <summary>
    /// Executes the draw core logic for this backend.
    /// </summary>
    /// <param name="vertexCount">The vertex count value used by this operation.</param>
    /// <param name="instanceCount">The instance count value used by this operation.</param>
    /// <param name="vertexStart">The vertex start value used by this operation.</param>
    /// <param name="instanceStart">The instance start value used by this operation.</param>
    private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart) {
        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        this.PreDrawCommand();
        this.BindInputAssemblerForDraw(ref vertexStart);
        if (_stableResourceSetUpdateFastPathEnabled && vertexCount != 0 && instanceCount != 0) {
            this.MarkGraphicsResourceSetBuffersUsed();
        }

        this.DrawInstancedNoAlloc(vertexCount, instanceCount, vertexStart, instanceStart);
        if (vertexCount != 0 && instanceCount != 0) {
            this.MarkInputAssemblerBuffersUsed();
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.DrawCalls++;
            this._perf.DrawMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
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
        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        this.PreDrawCommand();
        this.BindInputAssemblerForIndexedDraw(ref indexStart, ref vertexOffset);
        if (_stableResourceSetUpdateFastPathEnabled && indexCount != 0 && instanceCount != 0) {
            this.MarkGraphicsResourceSetBuffersUsed();
        }

        this.DrawIndexedInstancedNoAlloc(indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
        if (indexCount != 0 && instanceCount != 0) {
            this.MarkInputAssemblerBuffersUsed();
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.DrawCalls++;
            this._perf.DrawMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
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
        D3D12DeviceBuffer d3D12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
        if (d3D12Buffer.CanTransitionState) {
            this.EndRenderPassForInternalUse();
        }

        if (this._bufferUpdatePlanner.Update(d3D12Buffer, bufferOffsetInBytes, source, sizeInBytes)) {
            this.UpdatePendingBufferUploadDirtyFlag();
        }
    }

    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Executes the generate mipmaps core logic for this backend.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    private protected override void GenerateMipmapsCore(Texture texture) {
        this.EndRenderPassForInternalUse();
        this.FlushQueuedRenderPassClears();
        this.FlushPendingBufferUploads();
        D3D12Texture d3D12Texture = Util.AssertSubtype<Texture, D3D12Texture>(texture);
        if (texture.MipLevels <= 1 || d3D12Texture.NativeTexture == null) {
            return;
        }

        if (!this._gd.GetPixelFormatSupport(texture.Format, texture.Type, texture.Usage)) {
            throw new PlatformNotSupportedException("GenerateMipmaps is not supported for this D3D12 texture format/type/usage combination.");
        }

        if (this._gpuMipmapGenerator.TryGenerate(d3D12Texture)) {
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
        this._perf.PushDebugGroup(name);
        this.WriteDebugMarker(name, true, false);
    }

    /// <summary>
    /// Executes the pop debug group core logic for this backend.
    /// </summary>
    private protected override void PopDebugGroupCore() {
        this._endEventMethod?.Invoke(this.NativeCommandList, null);
        this._perf.PopDebugGroup();
    }

    /// <summary>
    /// Executes the insert debug marker core logic for this backend.
    /// </summary>
    /// <param name="name">The name used by this operation.</param>
    private protected override void InsertDebugMarkerCore(string name) {
        this._perf.InsertDebugMarker(name);
        this.WriteDebugMarker(name, false, true);
    }

    /// <summary>
    /// Uploads backend-specific push-constant data to the active pipeline.
    /// </summary>
    /// <param name="offset">The byte offset inside the push-constant range.</param>
    /// <param name="data">A pointer to source data.</param>
    /// <param name="sizeInBytes">The number of bytes to upload.</param>
    private protected override unsafe void PushConstantsCore(uint offset, IntPtr data, uint sizeInBytes) {
        D3D12Pipeline pipeline = this.CurrentComputePipeline ?? this.CurrentGraphicsPipeline;
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
        if (this.CurrentComputePipeline != null) {
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

        this.EndRenderPassForInternalUse();
        D3D12BarrierQueueResult result = this._barriers.QueueTransition(resource, from, to);
        this.MarkDrawDirty(DrawDirtyResourceBarriers);
        if (result == D3D12BarrierQueueResult.Full) {
            this.FlushPendingBarriers();
            this._barriers.QueueTransition(resource, from, to);
            this.MarkDrawDirty(DrawDirtyResourceBarriers);
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.Transitions++;
        }
    }

    /// <summary>
    /// Queues a full-resource transition for an internal D3D12 helper.
    /// </summary>
    /// <param name="resource">The native resource to transition.</param>
    /// <param name="from">The current resource state.</param>
    /// <param name="to">The destination resource state.</param>
    internal void TransitionForInternalUse(ID3D12Resource resource, ResourceStates from, ResourceStates to) {
        this.Transition(resource, from, to);
    }

    /// <summary>
    /// Queues a subresource transition for an internal D3D12 helper.
    /// </summary>
    /// <param name="resource">The native resource to transition.</param>
    /// <param name="from">The current resource state.</param>
    /// <param name="to">The destination resource state.</param>
    /// <param name="subresource">The subresource index.</param>
    internal void TransitionSubresourceForInternalUse(ID3D12Resource resource, ResourceStates from, ResourceStates to, uint subresource) {
        this.TransitionSubresource(resource, from, to, subresource);
    }

    /// <summary>
    /// Binds the vertex buffer resources for subsequent commands.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    private void BindVertexBuffer(uint index, D3D12DeviceBuffer buffer, uint offset) {
        D3D12BufferBindingInfo bindingInfo = this.GetBufferBindingInfo(buffer, offset);
        this.BindVertexBuffer(index, buffer, offset, in bindingInfo);
    }

    /// <summary>
    /// Binds the vertex buffer resources for subsequent commands.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="bindingInfo">The resolved command-list-local binding information.</param>
    private void BindVertexBuffer(uint index, D3D12DeviceBuffer buffer, uint offset, in D3D12BufferBindingInfo bindingInfo) {
        this.TransitionBuffer(buffer, ResourceStates.VertexAndConstantBuffer);

        uint stride = 0;
        D3D12Pipeline currentGraphicsPipeline = this.CurrentGraphicsPipeline;
        if (currentGraphicsPipeline != null && index < currentGraphicsPipeline.VertexStrides.Length) {
            stride = currentGraphicsPipeline.VertexStrides[index];
        }

        VertexBufferView view = new(bindingInfo.GpuVirtualAddress, bindingInfo.BindableSize, stride);
        this.SetVertexBufferNoAlloc(index, ref view);
        this._inputAssembler.SetVertexBufferStride(index, stride);
    }

    /// <summary>
    /// Rebinds input-assembler vertex views at the draw start for dynamic buffers updated after binding.
    /// </summary>
    /// <param name="vertexStart">The draw vertex start, rewritten when views are shifted.</param>
    private void BindInputAssemblerForDraw(ref uint vertexStart) {
        if (vertexStart == 0 || !this._inputAssembler.HasDynamicInputAssemblerBuffer) {
            return;
        }

        this.BindVertexBuffersForDrawStart(vertexStart);
        vertexStart = 0;
    }

    /// <summary>
    /// Rebinds input-assembler views at the draw start for dynamic buffers updated after binding.
    /// </summary>
    /// <param name="indexStart">The draw index start, rewritten when the index view is shifted.</param>
    /// <param name="vertexOffset">The draw vertex offset, rewritten when vertex views are shifted.</param>
    private void BindInputAssemblerForIndexedDraw(ref uint indexStart, ref int vertexOffset) {
        if (!this._inputAssembler.HasDynamicInputAssemblerBuffer) {
            return;
        }

        if (vertexOffset > 0) {
            this.BindVertexBuffersForDrawStart((uint)vertexOffset);
            vertexOffset = 0;
        }

        if (indexStart == 0 || !this._inputAssembler.HasIndexBuffer) {
            return;
        }

        D3D12DeviceBuffer indexBuffer = this._inputAssembler.IndexBuffer;
        if (indexBuffer == null) {
            return;
        }

        uint shiftedOffset = this._inputAssembler.IndexBufferOffset + indexStart * GetIndexFormatSizeInBytes(this._inputAssembler.IndexFormat);
        D3D12BufferBindingInfo bindingInfo = this.GetBufferBindingInfo(indexBuffer, shiftedOffset);
        this.TransitionBuffer(indexBuffer, ResourceStates.IndexBuffer);
        IndexBufferView indexView = new(bindingInfo.GpuVirtualAddress, bindingInfo.BindableSize, D3D12Formats.ToDxgiFormat(this._inputAssembler.IndexFormat));
        this.SetIndexBufferNoAlloc(ref indexView);
        indexStart = 0;
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.IndexBufferBinds++;
        }
    }

    /// <summary>
    /// Binds vertex buffer views at a draw-relative vertex start and leaves cached logical bindings unchanged.
    /// </summary>
    /// <param name="vertexStart">The vertex start used by the draw call.</param>
    private void BindVertexBuffersForDrawStart(uint vertexStart) {
        for (uint index = 0; index < this._inputAssembler.MaxBoundVertexBufferSlot; index++) {
            D3D12DeviceBuffer buffer = this._inputAssembler.GetVertexBuffer(index);
            if (buffer == null) {
                continue;
            }

            uint stride = this._inputAssembler.GetVertexBufferStride(index);
            uint shiftedOffset = this._inputAssembler.GetVertexBufferOffset(index) + vertexStart * stride;
            D3D12BufferBindingInfo bindingInfo = this.GetBufferBindingInfo(buffer, shiftedOffset);
            this.TransitionBuffer(buffer, ResourceStates.VertexAndConstantBuffer);
            VertexBufferView view = new(bindingInfo.GpuVirtualAddress, bindingInfo.BindableSize, stride);
            this.SetVertexBufferNoAlloc(index, ref view);
            if (D3D12CommandListPerfTracker.Enabled) {
                this._perf.VertexBufferBinds++;
            }
        }
    }

    /// <summary>
    /// Gets the byte width of an index format.
    /// </summary>
    /// <param name="format">The index format.</param>
    /// <returns>The index width in bytes.</returns>
    private static uint GetIndexFormatSizeInBytes(IndexFormat format) {
        return format == IndexFormat.UInt32 ? 4u : 2u;
    }

    /// <summary>
    /// Executes the rebind vertex buffers for current pipeline logic for this backend.
    /// </summary>
    internal void RebindVertexBuffersForCurrentPipeline() {
        D3D12Pipeline currentGraphicsPipeline = this.CurrentGraphicsPipeline;
        if (currentGraphicsPipeline == null) {
            return;
        }

        for (uint index = 0; index < this._inputAssembler.MaxBoundVertexBufferSlot; index++) {
            D3D12DeviceBuffer buffer = this._inputAssembler.GetVertexBuffer(index);
            if (buffer == null) {
                continue;
            }

            uint stride = index < currentGraphicsPipeline.VertexStrides.Length ? currentGraphicsPipeline.VertexStrides[index] : 0;
            if (this._inputAssembler.GetVertexBufferStride(index) == stride) {
                continue;
            }

            uint offset = this._inputAssembler.GetVertexBufferOffset(index);
            this.BindVertexBuffer(index, buffer, offset);
        }
    }

    /// <summary>
    /// Refreshes input-assembler views that point at a dynamic buffer whose native snapshot offset changed.
    /// </summary>
    /// <param name="buffer">The dynamic buffer whose binding version changed.</param>
    private void RefreshDynamicBufferBindings(D3D12DeviceBuffer buffer) {
        for (uint index = 0; index < this._inputAssembler.MaxBoundVertexBufferSlot; index++) {
            if (!ReferenceEquals(this._inputAssembler.GetVertexBuffer(index), buffer)) {
                continue;
            }

            uint offset = this._inputAssembler.GetVertexBufferOffset(index);
            ulong bindVersion = this.GetBufferBindVersion(buffer, offset);
            if (this._inputAssembler.GetVertexBufferVersion(index) == bindVersion) {
                continue;
            }

            D3D12BufferBindingInfo bindingInfo = this.GetBufferBindingInfo(buffer, offset);
            this.BindVertexBuffer(index, buffer, offset, in bindingInfo);
            this._inputAssembler.SetVertexBufferVersion(index, bindingInfo.BindVersion);
            if (D3D12CommandListPerfTracker.Enabled) {
                this._perf.VertexBufferBinds++;
            }
        }

        if (!this._inputAssembler.HasIndexBuffer || !ReferenceEquals(this._inputAssembler.IndexBuffer, buffer)) {
            return;
        }

        ulong indexBindVersion = this.GetBufferBindVersion(buffer, this._inputAssembler.IndexBufferOffset);
        if (this._inputAssembler.IndexBufferVersion == indexBindVersion) {
            return;
        }

        this.TransitionBuffer(buffer, ResourceStates.IndexBuffer);
        D3D12BufferBindingInfo indexBindingInfo = this.GetBufferBindingInfo(buffer, this._inputAssembler.IndexBufferOffset);
        IndexBufferView indexView = new(indexBindingInfo.GpuVirtualAddress, indexBindingInfo.BindableSize, D3D12Formats.ToDxgiFormat(this._inputAssembler.IndexFormat));
        this.SetIndexBufferNoAlloc(ref indexView);
        this._inputAssembler.SetIndexBufferVersion(indexBindingInfo.BindVersion);
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.IndexBufferBinds++;
        }
    }

    /// <summary>
    /// Refreshes input-assembler views for an internal D3D12 helper after a dynamic buffer native address change.
    /// </summary>
    /// <param name="buffer">The dynamic buffer whose binding version changed.</param>
    internal void RefreshDynamicBufferBindingsForInternalUse(D3D12DeviceBuffer buffer) {
        this.MarkDynamicBufferBindingsDirty(buffer);
    }

    /// <summary>
    /// Marks input-assembler bindings dirty after a dynamic buffer snapshot address changed.
    /// </summary>
    /// <param name="buffer">The dynamic buffer whose command-list-local address changed.</param>
    private void MarkDynamicBufferBindingsDirty(D3D12DeviceBuffer buffer) {
        for (int i = 0; i < this._dynamicBindingRefreshBufferCount; i++) {
            if (ReferenceEquals(this._dynamicBindingRefreshBuffers[i], buffer)) {
                return;
            }
        }

        if (!this.HasStaleDynamicInputAssemblerBinding(buffer)) {
            return;
        }

        this.AddDynamicBindingRefreshBuffer(buffer);
        this.MarkDrawDirty(DrawDirtyDynamicInputAssembler);
    }

    /// <summary>
    /// Removes a dynamic buffer from the deferred refresh list when all recorded input-assembler bindings are current.
    /// </summary>
    /// <param name="buffer">The dynamic buffer to inspect.</param>
    private void ClearDynamicBindingRefreshIfCurrent(D3D12DeviceBuffer buffer) {
        if (this._dynamicBindingRefreshBufferCount == 0 || this.HasStaleDynamicInputAssemblerBinding(buffer)) {
            return;
        }

        for (int i = 0; i < this._dynamicBindingRefreshBufferCount; i++) {
            if (!ReferenceEquals(this._dynamicBindingRefreshBuffers[i], buffer)) {
                continue;
            }

            int lastIndex = this._dynamicBindingRefreshBufferCount - 1;
            this._dynamicBindingRefreshBuffers[i] = this._dynamicBindingRefreshBuffers[lastIndex];
            this._dynamicBindingRefreshBuffers[lastIndex] = null;
            this._dynamicBindingRefreshBufferCount = lastIndex;
            if (this._dynamicBindingRefreshBufferCount == 0) {
                this.ClearDrawDirty(DrawDirtyDynamicInputAssembler);
            }

            return;
        }
    }

    /// <summary>
    /// Checks whether any recorded input-assembler binding still points at an older dynamic snapshot.
    /// </summary>
    /// <param name="buffer">The dynamic buffer to inspect.</param>
    /// <returns><see langword="true" /> when a deferred refresh is still required.</returns>
    private bool HasStaleDynamicInputAssemblerBinding(D3D12DeviceBuffer buffer) {
        for (uint index = 0; index < this._inputAssembler.MaxBoundVertexBufferSlot; index++) {
            if (!ReferenceEquals(this._inputAssembler.GetVertexBuffer(index), buffer)) {
                continue;
            }

            uint offset = this._inputAssembler.GetVertexBufferOffset(index);
            ulong bindVersion = this.GetBufferBindVersion(buffer, offset);
            if (this._inputAssembler.GetVertexBufferVersion(index) != bindVersion) {
                return true;
            }
        }

        return this._inputAssembler.HasIndexBuffer
               && ReferenceEquals(this._inputAssembler.IndexBuffer, buffer)
               && this._inputAssembler.IndexBufferVersion != this.GetBufferBindVersion(buffer, this._inputAssembler.IndexBufferOffset);
    }

    /// <summary>
    /// Refreshes stale input-assembler bindings after dynamic buffer snapshot address changes.
    /// </summary>
    private void RefreshDirtyDynamicBufferBindings() {
        if (this._dynamicBindingRefreshBufferCount == 0) {
            this.ClearDrawDirty(DrawDirtyDynamicInputAssembler);
            return;
        }

        for (int i = 0; i < this._dynamicBindingRefreshBufferCount; i++) {
            this.RefreshDynamicBufferBindings(this._dynamicBindingRefreshBuffers[i]);
        }

        this.ClearDynamicBindingRefreshBuffers();
        this.ClearDrawDirty(DrawDirtyDynamicInputAssembler);
    }

    /// <summary>
    /// Adds a dynamic buffer to the deferred IA refresh list.
    /// </summary>
    /// <param name="buffer">The dynamic buffer to refresh before the next draw.</param>
    private void AddDynamicBindingRefreshBuffer(D3D12DeviceBuffer buffer) {
        if (this._dynamicBindingRefreshBufferCount == this._dynamicBindingRefreshBuffers.Length) {
            Array.Resize(ref this._dynamicBindingRefreshBuffers, this._dynamicBindingRefreshBuffers.Length * 2);
        }

        this._dynamicBindingRefreshBuffers[this._dynamicBindingRefreshBufferCount++] = buffer;
    }

    /// <summary>
    /// Clears deferred dynamic IA refresh entries and releases managed references.
    /// </summary>
    private void ClearDynamicBindingRefreshBuffers() {
        if (this._dynamicBindingRefreshBufferCount == 0) {
            return;
        }

        Array.Clear(this._dynamicBindingRefreshBuffers, 0, this._dynamicBindingRefreshBufferCount);
        this._dynamicBindingRefreshBufferCount = 0;
    }

    /// <summary>
    /// Checks whether a ResourceSet buffer has already been consumed by this command-list recording.
    /// </summary>
    /// <param name="buffer">The buffer to inspect.</param>
    /// <returns><see langword="true" /> when a previous draw or dispatch may still read the stable buffer address.</returns>
    internal bool HasResourceSetBufferBeenUsedForInternalUse(D3D12DeviceBuffer buffer) {
        for (int i = 0; i < this._usedResourceSetBufferCount; i++) {
            if (ReferenceEquals(this._usedResourceSetBuffers[i], buffer)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether an input-assembler buffer has already been consumed by this command-list recording.
    /// </summary>
    /// <param name="buffer">The buffer to inspect.</param>
    /// <returns><see langword="true" /> when a previous draw may still read the stable buffer address.</returns>
    internal bool HasInputAssemblerBufferBeenUsedForInternalUse(D3D12DeviceBuffer buffer) {
        for (int i = 0; i < this._usedInputAssemblerBufferCount; i++) {
            if (ReferenceEquals(this._usedInputAssemblerBuffers[i], buffer)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Marks snapshot-capable input-assembler buffers as used by a recorded draw.
    /// </summary>
    private void MarkInputAssemblerBuffersUsed() {
        if (!this._inputAssembler.HasDynamicInputAssemblerBuffer) {
            return;
        }

        uint dynamicBindingVersion = this._inputAssembler.DynamicBindingVersion;
        if (this._inputAssemblerUsageCaptured && this._inputAssemblerUsageVersion == dynamicBindingVersion) {
            return;
        }

        for (uint index = 0; index < this._inputAssembler.MaxBoundVertexBufferSlot; index++) {
            D3D12DeviceBuffer buffer = this._inputAssembler.GetVertexBuffer(index);
            if (buffer?.UsesCommandListSnapshots == true) {
                this.TrackUsedInputAssemblerBuffer(buffer);
            }
        }

        D3D12DeviceBuffer indexBuffer = this._inputAssembler.HasIndexBuffer ? this._inputAssembler.IndexBuffer : null;
        if (indexBuffer?.UsesCommandListSnapshots == true) {
            this.TrackUsedInputAssemblerBuffer(indexBuffer);
        }

        this._inputAssemblerUsageVersion = dynamicBindingVersion;
        this._inputAssemblerUsageCaptured = true;
    }

    /// <summary>
    /// Tracks a single input-assembler buffer as used by a recorded draw.
    /// </summary>
    /// <param name="buffer">The buffer to track.</param>
    private void TrackUsedInputAssemblerBuffer(D3D12DeviceBuffer buffer) {
        if (ReferenceEquals(this._lastUsedInputAssemblerBuffer, buffer)) {
            return;
        }

        for (int i = 0; i < this._usedInputAssemblerBufferCount; i++) {
            if (ReferenceEquals(this._usedInputAssemblerBuffers[i], buffer)) {
                this._lastUsedInputAssemblerBuffer = buffer;
                return;
            }
        }

        Util.EnsureArrayMinimumSize(ref this._usedInputAssemblerBuffers, (uint)this._usedInputAssemblerBufferCount + 1);
        this._usedInputAssemblerBuffers[this._usedInputAssemblerBufferCount++] = buffer;
        this._lastUsedInputAssemblerBuffer = buffer;
    }

    /// <summary>
    /// Marks buffers referenced by bound graphics resource sets as used by a draw.
    /// </summary>
    private void MarkGraphicsResourceSetBuffersUsed() {
        if (!_stableResourceSetUpdateFastPathEnabled) {
            return;
        }

        uint resourceSetCount = this.CurrentGraphicsPipeline?.ResourceSetCount ?? 0u;
        if (this._graphicsResourceSetUsageCaptured
            && this._graphicsResourceSetUsageVersion == this._graphicsResourceSets.BufferSetVersion
            && this._graphicsResourceSetUsageCount == resourceSetCount) {
            return;
        }

        this.MarkResourceSetBuffersUsed(this._graphicsResourceSets, resourceSetCount);
        this._graphicsResourceSetUsageVersion = this._graphicsResourceSets.BufferSetVersion;
        this._graphicsResourceSetUsageCount = resourceSetCount;
        this._graphicsResourceSetUsageCaptured = true;
    }

    /// <summary>
    /// Marks buffers referenced by bound compute resource sets as used by a dispatch.
    /// </summary>
    private void MarkComputeResourceSetBuffersUsed() {
        if (!_stableResourceSetUpdateFastPathEnabled) {
            return;
        }

        uint resourceSetCount = this.CurrentComputePipeline?.ResourceSetCount ?? 0u;
        if (this._computeResourceSetUsageCaptured
            && this._computeResourceSetUsageVersion == this._computeResourceSets.BufferSetVersion
            && this._computeResourceSetUsageCount == resourceSetCount) {
            return;
        }

        this.MarkResourceSetBuffersUsed(this._computeResourceSets, resourceSetCount);
        this._computeResourceSetUsageVersion = this._computeResourceSets.BufferSetVersion;
        this._computeResourceSetUsageCount = resourceSetCount;
        this._computeResourceSetUsageCaptured = true;
    }

    /// <summary>
    /// Marks buffers referenced by one bind point's active resource sets as used.
    /// </summary>
    /// <param name="resourceSets">The resource set state to scan.</param>
    /// <param name="resourceSetCount">The active pipeline resource set count.</param>
    private void MarkResourceSetBuffersUsed(D3D12BoundResourceSetState resourceSets, uint resourceSetCount) {
        int count = Math.Min(resourceSets.BoundSets.Length, resourceSetCount > int.MaxValue ? int.MaxValue : (int)resourceSetCount);
        int bufferSlotCount = resourceSets.BufferSetSlotCount;
        for (int bufferSlotIndex = 0; bufferSlotIndex < bufferSlotCount; bufferSlotIndex++) {
            int slot = resourceSets.GetBufferSetSlot(bufferSlotIndex);
            if (slot >= count) {
                continue;
            }

            if (resourceSets.BoundSets[slot].Set is not D3D12ResourceSet set) {
                continue;
            }

            D3D12DeviceBuffer singleBuffer = set.SingleReferencedBuffer;
            if (singleBuffer != null) {
                this.TrackUsedResourceSetBuffer(singleBuffer);
                continue;
            }

            D3D12DeviceBuffer[] referencedBuffers = set.ReferencedBuffers;
            for (int i = 0; i < referencedBuffers.Length; i++) {
                this.TrackUsedResourceSetBuffer(referencedBuffers[i]);
            }
        }
    }

    /// <summary>
    /// Tracks a single ResourceSet buffer as used by a recorded draw or dispatch.
    /// </summary>
    /// <param name="buffer">The buffer to track.</param>
    private void TrackUsedResourceSetBuffer(D3D12DeviceBuffer buffer) {
        if (ReferenceEquals(this._lastUsedResourceSetBuffer, buffer)) {
            return;
        }

        for (int i = 0; i < this._usedResourceSetBufferCount; i++) {
            if (ReferenceEquals(this._usedResourceSetBuffers[i], buffer)) {
                this._lastUsedResourceSetBuffer = buffer;
                return;
            }
        }

        Util.EnsureArrayMinimumSize(ref this._usedResourceSetBuffers, (uint)this._usedResourceSetBufferCount + 1);
        this._usedResourceSetBuffers[this._usedResourceSetBufferCount++] = buffer;
        this._lastUsedResourceSetBuffer = buffer;
    }

    /// <summary>
    /// Clears the ResourceSet buffer usage list for a new command-list recording.
    /// </summary>
    private void ClearUsedResourceSetBuffers() {
        if (this._usedResourceSetBufferCount != 0) {
            Array.Clear(this._usedResourceSetBuffers, 0, this._usedResourceSetBufferCount);
            this._usedResourceSetBufferCount = 0;
        }

        this._lastUsedResourceSetBuffer = null;
        this._graphicsResourceSetUsageCaptured = false;
        this._graphicsResourceSetUsageVersion = 0;
        this._graphicsResourceSetUsageCount = 0;
        this._computeResourceSetUsageCaptured = false;
        this._computeResourceSetUsageVersion = 0;
        this._computeResourceSetUsageCount = 0;
    }

    /// <summary>
    /// Clears the input-assembler buffer usage list for a new command-list recording.
    /// </summary>
    private void ClearUsedInputAssemblerBuffers() {
        if (this._usedInputAssemblerBufferCount != 0) {
            Array.Clear(this._usedInputAssemblerBuffers, 0, this._usedInputAssemblerBufferCount);
            this._usedInputAssemblerBufferCount = 0;
        }

        this._lastUsedInputAssemblerBuffer = null;
        this._inputAssemblerUsageCaptured = false;
        this._inputAssemblerUsageVersion = 0;
    }

    /// <summary>
    /// Gets the command-list-local GPU virtual address for a buffer range.
    /// </summary>
    /// <param name="buffer">The buffer to bind.</param>
    /// <param name="offset">The byte offset into the buffer.</param>
    /// <returns>The GPU virtual address visible to this command list.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ulong GetBufferGpuVirtualAddressForInternalUse(D3D12DeviceBuffer buffer, uint offset) {
        return this.GetBufferGpuVirtualAddress(buffer, offset);
    }

    /// <summary>
    /// Gets the command-list-local bind version for a buffer.
    /// </summary>
    /// <param name="buffer">The buffer to inspect.</param>
    /// <returns>The bind version visible to this command list.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong GetBufferBindVersion(D3D12DeviceBuffer buffer) {
        return this._bufferUpdatePlanner.GetBindVersion(buffer);
    }

    /// <summary>
    /// Gets the command-list-local bind version for a buffer range.
    /// </summary>
    /// <param name="buffer">The buffer to inspect.</param>
    /// <param name="offset">The byte offset into the buffer.</param>
    /// <returns>The bind version visible to this command list for the range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong GetBufferBindVersion(D3D12DeviceBuffer buffer, uint offset) {
        return this._bufferUpdatePlanner.GetBindVersion(buffer, offset);
    }

    /// <summary>
    /// Gets the command-list-local GPU virtual address for a buffer range.
    /// </summary>
    /// <param name="buffer">The buffer to bind.</param>
    /// <param name="offset">The byte offset into the buffer.</param>
    /// <returns>The GPU virtual address visible to this command list.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong GetBufferGpuVirtualAddress(D3D12DeviceBuffer buffer, uint offset) {
        return this._bufferUpdatePlanner.GetGpuVirtualAddress(buffer, offset);
    }

    /// <summary>
    /// Gets the command-list-local bindable size for a buffer range.
    /// </summary>
    /// <param name="buffer">The buffer to bind.</param>
    /// <param name="offset">The byte offset into the buffer.</param>
    /// <returns>The bindable size visible to this command list.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetBufferBindableSize(D3D12DeviceBuffer buffer, uint offset) {
        return this._bufferUpdatePlanner.GetBindableSize(buffer, offset);
    }

    /// <summary>
    /// Gets the command-list-local binding information for a buffer range.
    /// </summary>
    /// <param name="buffer">The buffer to bind.</param>
    /// <param name="offset">The byte offset into the buffer.</param>
    /// <returns>The resolved binding information.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private D3D12BufferBindingInfo GetBufferBindingInfo(D3D12DeviceBuffer buffer, uint offset) {
        return this._bufferUpdatePlanner.GetBindingInfo(buffer, offset);
    }

    /// <summary>
    /// Records primitive topology only when the requested topology differs from command-list state.
    /// </summary>
    /// <param name="topology">The primitive topology required by the active graphics pipeline.</param>
    private void SetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology topology) {
        if (!this._inputAssembler.TrySetPrimitiveTopology(topology)) {
            return;
        }

        this.IASetPrimitiveTopologyNoAlloc(topology);
    }

    /// <summary>
    /// Sets primitive topology for an internal D3D12 helper restoring graphics state.
    /// </summary>
    /// <param name="topology">The topology to record.</param>
    internal void SetPrimitiveTopologyForInternalUse(Vortice.Direct3D.PrimitiveTopology topology) {
        this.SetPrimitiveTopology(topology);
    }

    /// <summary>
    /// Sets the output-merger stencil reference when needed.
    /// </summary>
    /// <param name="stencilReference">The stencil reference value.</param>
    private void SetStencilReference(uint stencilReference) {
        if (!this._inputAssembler.TrySetStencilReference(stencilReference)) {
            return;
        }

        this.OMSetStencilRefNoAlloc(stencilReference);
    }

    /// <summary>
    /// Sets stencil reference for an internal D3D12 helper restoring graphics state.
    /// </summary>
    /// <param name="stencilReference">The stencil reference to record.</param>
    internal void SetStencilReferenceForInternalUse(uint stencilReference) {
        this.SetStencilReference(stencilReference);
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

        this.EndRenderPassForInternalUse();
        D3D12BarrierQueueResult result = this._barriers.QueueSubresourceTransition(resource, from, to, subresource);
        this.MarkDrawDirty(DrawDirtyResourceBarriers);
        if (result == D3D12BarrierQueueResult.Full) {
            this.FlushPendingBarriers();
            this._barriers.QueueSubresourceTransition(resource, from, to, subresource);
            this.MarkDrawDirty(DrawDirtyResourceBarriers);
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.SubresourceTransitions++;
        }
    }

    /// <summary>
    /// Executes the flush pending uav barrier logic for this backend.
    /// </summary>
    private void FlushPendingUavBarrier() {
        if (!this._barriers.UavBarrierPending) {
            this.ClearDrawDirty(DrawDirtyUavBarrier);
            return;
        }

        this.EndRenderPassForInternalUse();
        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        bool flushed = this._barriers.FlushPendingUavBarrier(this.NativeCommandList);
        if (!flushed) {
            this.UpdateUavBarrierDrawDirtyFlag();
            return;
        }

        this.ClearDrawDirty(DrawDirtyUavBarrier);

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.UavBarriers++;
            this._perf.BarrierMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Flushes a pending UAV barrier for an internal D3D12 helper.
    /// </summary>
    internal void FlushPendingUavBarrierForInternalUse() {
        this.FlushPendingUavBarrier();
    }

    /// <summary>
    /// Marks that an internal D3D12 helper recorded unordered-access writes.
    /// </summary>
    internal void MarkUavBarrierPendingForInternalUse() {
        this._barriers.UavBarrierPending = true;
        this.MarkDrawDirty(DrawDirtyUavBarrier);
    }

    /// <summary>
    /// Flushes all accumulated pending resource-barrier transitions as a single batched D3D12 call.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushPendingBarriers() {
        if (!this._barriers.HasQueuedBarriers) {
            this.ClearDrawDirty(DrawDirtyResourceBarriers);
            return;
        }

        this.EndRenderPassForInternalUse();
        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        bool flushed = this._barriers.FlushPendingBarriers(this.NativeCommandList);
        if (!flushed) {
            this.UpdateResourceBarrierDrawDirtyFlag();
            return;
        }

        this.ClearDrawDirty(DrawDirtyResourceBarriers);

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.BarrierMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }
    }

    /// <summary>
    /// Flushes pending resource barriers for an internal D3D12 helper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void FlushPendingBarriersForInternalUse() {
        this.FlushPendingBarriers();
    }

    /// <summary>
    /// Flushes dirty graphics state required before a direct draw command.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PreDrawCommand() {
        if (this._drawDirtyFlags == 0) {
            if (this.NativeCommandList4 == null || this._renderPassTracker.Active) {
                return;
            }
        }

        this.PreDrawCommandSlow();
    }

    /// <summary>
    /// Flushes dirty graphics state before a draw when at least one dirty bit is set.
    /// </summary>
    private void PreDrawCommandSlow() {
        if ((this._drawDirtyFlags & DrawDirtyPendingBufferUploads) != 0) {
            this.FlushPendingBufferUploads();
        }

        if ((this._drawDirtyFlags & DrawDirtyDynamicInputAssembler) != 0) {
            this.RefreshDirtyDynamicBufferBindings();
        }

        if ((this._drawDirtyFlags & DrawDirtyGraphicsResourceSets) != 0) {
            this.FlushGraphicsResourceSets();
        }

        if ((this._drawDirtyFlags & DrawDirtyUavBarrier) != 0) {
            this.FlushPendingUavBarrier();
        }

        if ((this._drawDirtyFlags & DrawDirtyResourceBarriers) != 0) {
            this.FlushPendingBarriers();
        }

        if (this.NativeCommandList4 != null) {
            this._renderPassTracker.BeginDrawPass(this.Framebuffer);
        }
    }

    /// <summary>
    /// Begins a native D3D12 render pass before a draw command when the device supports it.
    /// </summary>
    private void BeginRenderPassForDraw() {
        this._renderPassTracker.BeginDrawPass(this.Framebuffer);
    }

    /// <summary>
    /// Ends the native D3D12 render pass for internal helpers that record non-render-pass commands.
    /// </summary>
    internal void EndRenderPassForInternalUse() {
        this._renderPassTracker.EndPass();
    }

    /// <summary>
    /// Emits queued render-pass clears through the immediate clear path when no draw consumes them.
    /// </summary>
    private void FlushQueuedRenderPassClears() {
        this._renderPassTracker.FlushQueuedClears(this._clearPlanner);
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
    /// <param name="beginRenderPass">Whether to begin a render pass before recording the indirect command.</param>
    private void ExecuteIndirect(D3D12DeviceBuffer argumentBuffer, uint offset, uint drawCount, uint stride, uint argumentSize, ID3D12CommandSignature signature, bool beginRenderPass) {
        if (drawCount == 0) {
            return;
        }

        this.FlushPendingUavBarrier();
        ResourceStates previousState = argumentBuffer.CurrentState;
        this.TransitionBuffer(argumentBuffer, ResourceStates.IndirectArgument);
        this.FlushPendingBarriers();
        if (beginRenderPass) {
            this.BeginRenderPassForDraw();
        }

        if (stride == argumentSize) {
            this.NativeCommandList.ExecuteIndirect(signature, drawCount, argumentBuffer.NativeBuffer, offset, null, 0);
            if (beginRenderPass) {
                this.EndRenderPassForInternalUse();
            }

            this.TransitionBuffer(argumentBuffer, previousState);
            return;
        }

        for (uint i = 0; i < drawCount; i++) {
            ulong commandOffset = offset + (ulong)i * stride;
            this.NativeCommandList.ExecuteIndirect(signature, 1, argumentBuffer.NativeBuffer, commandOffset, null, 0);
        }

        if (beginRenderPass) {
            this.EndRenderPassForInternalUse();
        }

        this.TransitionBuffer(argumentBuffer, previousState);
        this._barriers.UavBarrierPending = true;
        this.MarkDrawDirty(DrawDirtyUavBarrier);
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
    /// Transitions a texture for an internal D3D12 helper.
    /// </summary>
    /// <param name="texture">The texture to transition.</param>
    /// <param name="toState">The required D3D12 state.</param>
    internal void TransitionTextureForInternalUse(D3D12Texture texture, ResourceStates toState) {
        this.TransitionTexture(texture, toState);
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

        if (textureView.IsKnownInState(toState)) {
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
            textureView.MarkKnownState(toState);
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

        textureView.MarkKnownState(toState);
    }

    /// <summary>
    /// Transitions a texture view for an internal D3D12 helper.
    /// </summary>
    /// <param name="textureView">The texture view to transition.</param>
    /// <param name="toState">The required D3D12 state.</param>
    internal void TransitionTextureViewForInternalUse(D3D12TextureView textureView, ResourceStates toState) {
        this.TransitionTextureView(textureView, toState);
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
    /// Checks whether binding a buffer would require recording a D3D12 resource barrier.
    /// </summary>
    /// <param name="buffer">The buffer to inspect.</param>
    /// <param name="toState">The required D3D12 state.</param>
    /// <returns><see langword="true" /> when the buffer needs a transition command.</returns>
    private static bool RequiresBufferTransition(D3D12DeviceBuffer buffer, ResourceStates toState) {
        return buffer.CanTransitionState && buffer.CurrentState != toState;
    }

    /// <summary>
    /// Transitions a buffer for an internal D3D12 helper.
    /// </summary>
    /// <param name="buffer">The buffer to transition.</param>
    /// <param name="toState">The required D3D12 state.</param>
    internal void TransitionBufferForInternalUse(D3D12DeviceBuffer buffer, ResourceStates toState) {
        this.TransitionBuffer(buffer, toState);
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

        MethodInfo markerMethod = beginEvent ? this._beginEventMethod : this._setMarkerMethod;
        if (markerMethod == null) {
            return;
        }

        int size = Encoding.UTF8.GetByteCount(name);
        byte[] rentedBytes = null;
        Span<byte> utf8Bytes = size <= 512
            ? stackalloc byte[size]
            : (rentedBytes = ArrayPool<byte>.Shared.Rent(size));
        utf8Bytes = utf8Bytes[..size];

        try {
            Encoding.UTF8.GetBytes(name.AsSpan(), utf8Bytes);
            fixed (byte* bytesPtr = utf8Bytes) {
                IntPtr dataPtr = (IntPtr)bytesPtr;
                bool metadataIsInt = beginEvent ? this._beginEventMetadataIsInt : this._setMarkerMetadataIsInt;
                bool sizeIsInt = beginEvent ? this._beginEventSizeIsInt : this._setMarkerSizeIsInt;
                this._debugMarkerArgs[0] = metadataIsInt ? 0 : 0u;
                this._debugMarkerArgs[1] = dataPtr;
                this._debugMarkerArgs[2] = sizeIsInt ? size : (uint)size;
                if (beginEvent) {
                    markerMethod.Invoke(this.NativeCommandList, this._debugMarkerArgs);
                }
                else if (setMarker) {
                    markerMethod.Invoke(this.NativeCommandList, this._debugMarkerArgs);
                }
            }
        }
        finally {
            if (rentedBytes != null) {
                ArrayPool<byte>.Shared.Return(rentedBytes);
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
    /// Checks whether a cached debug marker method uses a signed integer parameter at an index.
    /// </summary>
    /// <param name="method">The reflected marker method.</param>
    /// <param name="index">The parameter index to inspect.</param>
    /// <returns><see langword="true" /> when the parameter type is <see cref="int" />.</returns>
    private static bool IsDebugMarkerParameterInt(MethodInfo method, int index) {
        return method?.GetParameters()[index].ParameterType == typeof(int);
    }

    /// <summary>
    /// Binds a graphics root constant buffer view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void SetGraphicsRootConstantBufferViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this._setGraphicsRootConstantBufferView((void*)this._nativeCommandListPointer, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a graphics root shader resource view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void SetGraphicsRootShaderResourceViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this._setGraphicsRootShaderResourceView((void*)this._nativeCommandListPointer, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a graphics root unordered access view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void SetGraphicsRootUnorderedAccessViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this._setGraphicsRootUnorderedAccessView((void*)this._nativeCommandListPointer, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a compute root constant buffer view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void SetComputeRootConstantBufferViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this._setComputeRootConstantBufferView((void*)this._nativeCommandListPointer, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a compute root shader resource view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void SetComputeRootShaderResourceViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this._setComputeRootShaderResourceView((void*)this._nativeCommandListPointer, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a compute root unordered access view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void SetComputeRootUnorderedAccessViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this._setComputeRootUnorderedAccessView((void*)this._nativeCommandListPointer, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Closes the command list without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void CloseNoAlloc() {
        Result result = new(this._closeCommandList((void*)this._nativeCommandListPointer));
        result.CheckError();
    }

    /// <summary>
    /// Resets the command list without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ResetCommandListNoAlloc(ID3D12CommandAllocator allocator) {
        Result result = new(this._resetCommandList((void*)this._nativeCommandListPointer, (void*)allocator.NativePointer, null));
        result.CheckError();
    }

    /// <summary>
    /// Records DrawInstanced without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void DrawInstancedNoAlloc(uint vertexCount, uint instanceCount, uint startVertexLocation, uint startInstanceLocation) {
        this._drawInstanced((void*)this._nativeCommandListPointer, vertexCount, instanceCount, startVertexLocation, startInstanceLocation);
    }

    /// <summary>
    /// Records DrawIndexedInstanced without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void DrawIndexedInstancedNoAlloc(uint indexCount, uint instanceCount, uint startIndexLocation, int baseVertexLocation, uint startInstanceLocation) {
        this._drawIndexedInstanced((void*)this._nativeCommandListPointer, indexCount, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
    }

    /// <summary>
    /// Binds pipeline state without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetPipelineStateNoAlloc(ID3D12PipelineState pipelineState) {
        this._setPipelineState((void*)this._nativeCommandListPointer, (void*)pipelineState.NativePointer);
    }

    /// <summary>
    /// Binds pipeline state for an internal D3D12 helper without going through the managed COM wrapper.
    /// </summary>
    /// <param name="pipelineState">The pipeline state to bind.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetPipelineStateNoAllocForInternalUse(ID3D12PipelineState pipelineState) {
        this.SetPipelineStateNoAlloc(pipelineState);
    }

    /// <summary>
    /// Copies a buffer range without going through the managed COM wrapper.
    /// </summary>
    /// <param name="destinationBuffer">The destination buffer resource.</param>
    /// <param name="destinationOffset">The destination byte offset.</param>
    /// <param name="sourceBuffer">The source buffer resource.</param>
    /// <param name="sourceOffset">The source byte offset.</param>
    /// <param name="sizeInBytes">The number of bytes to copy.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void CopyBufferRegionNoAllocForInternalUse(ID3D12Resource destinationBuffer, ulong destinationOffset, ID3D12Resource sourceBuffer, ulong sourceOffset, ulong sizeInBytes) {
        this._copyBufferRegion((void*)this._nativeCommandListPointer, (void*)destinationBuffer.NativePointer, destinationOffset, (void*)sourceBuffer.NativePointer, sourceOffset, sizeInBytes);
    }

    /// <summary>
    /// Binds one index-buffer view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetIndexBufferNoAlloc(ref IndexBufferView view) {
        this._setIndexBuffer((void*)this._nativeCommandListPointer, Unsafe.AsPointer(ref view));
    }

    /// <summary>
    /// Sets render targets without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void OMSetRenderTargetsNoAlloc(uint numRenderTargetDescriptors, CpuDescriptorHandle rtvHandle, bool hasDepthStencil, CpuDescriptorHandle dsvHandle) {
        this._setRenderTargets((void*)this._nativeCommandListPointer, numRenderTargetDescriptors, Unsafe.AsPointer(ref rtvHandle), 1, hasDepthStencil ? Unsafe.AsPointer(ref dsvHandle) : null);
    }

    /// <summary>
    /// Sets stencil reference without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void OMSetStencilRefNoAlloc(uint stencilRef) {
        this._setStencilReference((void*)this._nativeCommandListPointer, stencilRef);
    }

    /// <summary>
    /// Sets primitive topology without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void IASetPrimitiveTopologyNoAlloc(Vortice.Direct3D.PrimitiveTopology topology) {
        this._setPrimitiveTopology((void*)this._nativeCommandListPointer, (int)topology);
    }

    /// <summary>
    /// Binds one vertex-buffer view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetVertexBufferNoAlloc(uint startSlot, ref VertexBufferView view) {
        this._setVertexBuffers((void*)this._nativeCommandListPointer, startSlot, 1u, Unsafe.AsPointer(ref view));
    }

    /// <summary>
    /// Dispatches a compute shader without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void DispatchNoAlloc(uint groupCountX, uint groupCountY, uint groupCountZ) {
        this._dispatch((void*)this._nativeCommandListPointer, groupCountX, groupCountY, groupCountZ);
    }

    /// <summary>
    /// Dispatches compute work for an internal D3D12 helper without going through the managed COM wrapper.
    /// </summary>
    /// <param name="groupCountX">The X dimension of the dispatch.</param>
    /// <param name="groupCountY">The Y dimension of the dispatch.</param>
    /// <param name="groupCountZ">The Z dimension of the dispatch.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DispatchNoAllocForInternalUse(uint groupCountX, uint groupCountY, uint groupCountZ) {
        this.DispatchNoAlloc(groupCountX, groupCountY, groupCountZ);
    }

    /// <summary>
    /// Sets the compute root signature without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetComputeRootSignatureNoAlloc(ID3D12RootSignature rootSignature) {
        this._setComputeRootSignature((void*)this._nativeCommandListPointer, (void*)rootSignature.NativePointer);
    }

    /// <summary>
    /// Sets the compute root signature for an internal D3D12 helper without going through the managed COM wrapper.
    /// </summary>
    /// <param name="rootSignature">The root signature to bind.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetComputeRootSignatureNoAllocForInternalUse(ID3D12RootSignature rootSignature) {
        this.SetComputeRootSignatureNoAlloc(rootSignature);
    }

    /// <summary>
    /// Sets the graphics root signature without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetGraphicsRootSignatureNoAlloc(ID3D12RootSignature rootSignature) {
        this._setGraphicsRootSignature((void*)this._nativeCommandListPointer, (void*)rootSignature.NativePointer);
    }

    /// <summary>
    /// Sets the graphics root signature for an internal D3D12 helper without going through the managed COM wrapper.
    /// </summary>
    /// <param name="rootSignature">The root signature to bind.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetGraphicsRootSignatureNoAllocForInternalUse(ID3D12RootSignature rootSignature) {
        this.SetGraphicsRootSignatureNoAlloc(rootSignature);
    }

    /// <summary>
    /// Sets compute root 32-bit constants without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetComputeRoot32BitConstantsNoAlloc(uint rootParameterIndex, uint num32BitValues, void* srcData, uint destOffset) {
        this._setComputeRoot32BitConstants((void*)this._nativeCommandListPointer, rootParameterIndex, num32BitValues, srcData, destOffset);
    }

    /// <summary>
    /// Sets graphics root 32-bit constants without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void SetGraphicsRoot32BitConstantsNoAlloc(uint rootParameterIndex, uint num32BitValues, void* srcData, uint destOffset) {
        this._setGraphicsRoot32BitConstants((void*)this._nativeCommandListPointer, rootParameterIndex, num32BitValues, srcData, destOffset);
    }

    /// <summary>
    /// Sets multiple render targets without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void OMSetRenderTargetsArrayNoAlloc(CpuDescriptorHandle[] rtvs, bool hasDepthStencil, CpuDescriptorHandle dsv) {
        fixed (CpuDescriptorHandle* rtvPtr = rtvs) {
            this._setRenderTargets((void*)this._nativeCommandListPointer, (uint)rtvs.Length, rtvPtr, 0, hasDepthStencil ? Unsafe.AsPointer(ref dsv) : null);
        }
    }

    /// <summary>
    /// Clears a render target view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ClearRenderTargetViewNoAlloc(CpuDescriptorHandle rtv, float r, float g, float b, float a) {
        float* color = stackalloc float[4] { r, g, b, a };
        this._clearRenderTargetView((void*)this._nativeCommandListPointer, rtv, color, 0u, null);
    }

    /// <summary>
    /// Clears a render target view for an internal D3D12 helper without going through the managed COM wrapper.
    /// </summary>
    /// <param name="rtv">The render-target view to clear.</param>
    /// <param name="r">The red clear value.</param>
    /// <param name="g">The green clear value.</param>
    /// <param name="b">The blue clear value.</param>
    /// <param name="a">The alpha clear value.</param>
    internal void ClearRenderTargetViewNoAllocForInternalUse(CpuDescriptorHandle rtv, float r, float g, float b, float a) {
        this.ClearRenderTargetViewNoAlloc(rtv, r, g, b, a);
    }

    /// <summary>
    /// Clears a depth/stencil view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ClearDepthStencilViewNoAlloc(CpuDescriptorHandle dsv, uint clearFlags, float depth, byte stencil) {
        this._clearDepthStencilView((void*)this._nativeCommandListPointer, dsv, clearFlags, depth, stencil, 0u, null);
    }

    /// <summary>
    /// Clears a depth/stencil view for an internal D3D12 helper without going through the managed COM wrapper.
    /// </summary>
    /// <param name="dsv">The depth/stencil view to clear.</param>
    /// <param name="clearFlags">The native D3D12 clear flags.</param>
    /// <param name="depth">The depth clear value.</param>
    /// <param name="stencil">The stencil clear value.</param>
    internal void ClearDepthStencilViewNoAllocForInternalUse(CpuDescriptorHandle dsv, uint clearFlags, float depth, byte stencil) {
        this.ClearDepthStencilViewNoAlloc(dsv, clearFlags, depth, stencil);
    }

    /// <summary>
    /// Flushes graphics resource sets that were changed since the previous draw.
    /// </summary>
    private void FlushGraphicsResourceSets() {
        if (!this._graphicsResourceSets.Dirty) {
            this.ClearDrawDirty(DrawDirtyGraphicsResourceSets);
            return;
        }

        if (this.CurrentGraphicsPipeline == null) {
            return;
        }

        this._descriptorSetBinder.FlushGraphicsResourceSets(this._graphicsResourceSets, this.CurrentGraphicsPipeline);
        if (!this._graphicsResourceSets.Dirty) {
            this.ClearDrawDirty(DrawDirtyGraphicsResourceSets);
        }
    }

    /// <summary>
    /// Flushes compute resource sets that were changed since the previous dispatch.
    /// </summary>
    private void FlushComputeResourceSets() {
        if (!this._computeResourceSets.Dirty || this.CurrentComputePipeline == null) {
            return;
        }

        this._descriptorSetBinder.FlushComputeResourceSets(this._computeResourceSets, this.CurrentComputePipeline);
    }

    /// <summary>
    /// Flushes dirty compute resource sets for an internal D3D12 helper.
    /// </summary>
    internal void FlushComputeResourceSetsForInternalUse() {
        this.FlushComputeResourceSets();
    }

    /// <summary>
    /// Flushes command-list-local batched buffer updates before recording a GPU command.
    /// </summary>
    private void FlushPendingBufferUploads() {
        if (!this._bufferUpdatePlanner.HasPendingUploads) {
            this.ClearDrawDirty(DrawDirtyPendingBufferUploads);
            return;
        }

        this._bufferUpdatePlanner.Flush();
        this.UpdatePendingBufferUploadDirtyFlag();
    }

    /// <summary>
    /// Marks currently bound resource sets dirty when a dynamic buffer moves to a new native snapshot.
    /// </summary>
    /// <param name="buffer">The buffer whose native binding address changed.</param>
    private void MarkResourceSetsReferencingBufferDirty(D3D12DeviceBuffer buffer) {
        this._descriptorSetBinder.MarkResourceSetsReferencingBufferDirty(this._graphicsResourceSets, this.CurrentGraphicsPipeline, this._computeResourceSets, this.CurrentComputePipeline, buffer);
        if (this._graphicsResourceSets.Dirty) {
            this.MarkDrawDirty(DrawDirtyGraphicsResourceSets);
        }
    }

    /// <summary>
    /// Marks currently bound resource sets dirty for an internal D3D12 helper.
    /// </summary>
    /// <param name="buffer">The buffer whose native binding address changed.</param>
    internal void MarkResourceSetsReferencingBufferDirtyForInternalUse(D3D12DeviceBuffer buffer) {
        this.MarkResourceSetsReferencingBufferDirty(buffer);
    }

    /// <summary>
    /// Marks graphics resource sets dirty after an internal helper restored graphics bindings.
    /// </summary>
    internal void MarkGraphicsResourceSetsDirtyForInternalUse() {
        if (this._graphicsResourceSets.Dirty) {
            this.MarkDrawDirty(DrawDirtyGraphicsResourceSets);
        }
    }

    /// <summary>
    /// Marks draw pre-work as pending.
    /// </summary>
    /// <param name="flags">The flags to mark.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MarkDrawDirty(int flags) {
        this._drawDirtyFlags |= flags;
    }

    /// <summary>
    /// Clears draw pre-work flags that are no longer pending.
    /// </summary>
    /// <param name="flags">The flags to clear.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearDrawDirty(int flags) {
        this._drawDirtyFlags &= ~flags;
    }

    /// <summary>
    /// Syncs the draw dirty bit for pending buffer uploads.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdatePendingBufferUploadDirtyFlag() {
        if (this._bufferUpdatePlanner.HasPendingUploads) {
            this.MarkDrawDirty(DrawDirtyPendingBufferUploads);
        }
        else {
            this.ClearDrawDirty(DrawDirtyPendingBufferUploads);
        }
    }

    /// <summary>
    /// Syncs the draw dirty bit for queued resource barriers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateResourceBarrierDrawDirtyFlag() {
        if (this._barriers.HasQueuedBarriers) {
            this.MarkDrawDirty(DrawDirtyResourceBarriers);
        }
        else {
            this.ClearDrawDirty(DrawDirtyResourceBarriers);
        }
    }

    /// <summary>
    /// Syncs the draw dirty bit for a pending UAV barrier.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateUavBarrierDrawDirtyFlag() {
        if (this._barriers.UavBarrierPending) {
            this.MarkDrawDirty(DrawDirtyUavBarrier);
        }
        else {
            this.ClearDrawDirty(DrawDirtyUavBarrier);
        }
    }

    /// <summary>
    /// Keeps an upload allocation alive until the current command-list submission fence completes.
    /// </summary>
    /// <param name="uploadBuffer">The upload allocation referenced by recorded copy commands.</param>
    internal void TrackPendingSubmissionUploadBufferForInternalUse(D3D12ResourceAllocation uploadBuffer) {
        if (uploadBuffer != null) {
            this._pendingSubmissionUploadBuffers.Add(uploadBuffer);
        }
    }

    /// <summary>
    /// Binds a compute descriptor table without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void SetComputeRootDescriptorTableNoAlloc(uint rootParameterIndex, GpuDescriptorHandle gpuHandle) {
        this._setComputeRootDescriptorTable((void*)this._nativeCommandListPointer, rootParameterIndex, gpuHandle.Ptr);
    }

    /// <summary>
    /// Binds a graphics descriptor table without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void SetGraphicsRootDescriptorTableNoAlloc(uint rootParameterIndex, GpuDescriptorHandle gpuHandle) {
        this._setGraphicsRootDescriptorTable((void*)this._nativeCommandListPointer, rootParameterIndex, gpuHandle.Ptr);
    }

    /// Disposes upload resources that were recorded but not submitted.
    /// </summary>
    private void DisposePendingSubmissionDisposals() {
        if (this._pendingSubmissionUploadBuffers.Count == 0) {
            return;
        }

        for (int i = 0; i < this._pendingSubmissionUploadBuffers.Count; i++) {
            this._gd.ReturnUploadBuffer(this._pendingSubmissionUploadBuffers[i]);
        }

        this._pendingSubmissionUploadBuffers.Clear();
    }

}
