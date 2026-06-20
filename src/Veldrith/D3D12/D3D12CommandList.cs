using System;
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

    /// <summary>
    /// Stores the begin event method state used by this instance.
    /// </summary>
    private readonly MethodInfo _beginEventMethod;

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
    private readonly object[] _debugMarkerArgs = new object[3];

    /// <summary>
    /// Tracks pending D3D12 resource barriers and emits them in batches.
    /// </summary>
    private readonly D3D12ResourceBarrierTracker _barriers = new();

    /// <summary>
    /// Plans and records D3D12 texture copy/resolve operations.
    /// </summary>
    private readonly D3D12TextureCopyPlanner _textureCopyPlanner;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

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
        this._graphicsResourceSets.Clear();
        this._computeResourceSets.Clear();
        this._graphicsResourceSets.ResetDirtyRange();
        this._computeResourceSets.ResetDirtyRange();
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
        this._swapchainBackBuffer.TransitionToPresent(this.Framebuffer as D3D12SwapchainFramebuffer, this.Transition);
        this.FlushPendingBarriers();
        this.CloseNoAlloc();
        this._ended = true;
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.BarrierCoalescedTransitions = this._barriers.CoalescedTransitions;
            this._perf.BarrierRemovedTransitions = this._barriers.RemovedTransitions;
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
        this.FlushPendingUavBarrier();
        this.FlushPendingBarriers();
        this.DispatchNoAlloc(groupCountX, groupCountY, groupCountZ);
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.DispatchCalls++;
            this._perf.DispatchMs += D3D12CommandListPerfTracker.TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
        }

        this._barriers.UavBarrierPending = true;
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

        this.EndRenderPassForInternalUse();
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.ResourceSetChanges++;
        }

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

        this.EndRenderPassForInternalUse();
        if (!this._computeResourceSets.TrySet(slot, set, dynamicOffsetsCount, ref dynamicOffsets)) {
            return;
        }

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
                this.NativeCommandList.DrawInstanced(arguments.VertexCount, arguments.InstanceCount, arguments.FirstVertex, arguments.FirstInstance);
            }
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
        ulong bindVersion = d3D12Buffer.BindVersion;
        if (!this._inputAssembler.TrySetVertexBuffer(index, d3D12Buffer, offset, bindVersion, isDynamicBuffer)) {
            return;
        }

        this.EndRenderPassForInternalUse();
        this.BindVertexBuffer(index, d3D12Buffer, offset);
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
        ulong bindVersion = d3D12Buffer.BindVersion;
        if (!this._inputAssembler.NeedsIndexBufferBind(d3D12Buffer, format, offset, bindVersion, isDynamicBuffer)) {
            return;
        }

        this.EndRenderPassForInternalUse();
        this.TransitionBuffer(d3D12Buffer, ResourceStates.IndexBuffer);
        uint viewSize = d3D12Buffer.GetBindableSize(offset);
        IndexBufferView indexView = new(d3D12Buffer.GetGpuVirtualAddress(offset), viewSize, D3D12Formats.ToDxgiFormat(format));
        this.SetIndexBufferNoAlloc(ref indexView);
        this._inputAssembler.SetIndexBuffer(d3D12Buffer, format, offset, bindVersion);
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
        this.FlushPendingBufferUploads();
        this.FlushGraphicsResourceSets();
        this.FlushPendingUavBarrier();
        this.FlushPendingBarriers();
        this.BeginRenderPassForDraw();
        this.DrawInstancedNoAlloc(vertexCount, instanceCount, vertexStart, instanceStart);
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
        this.FlushPendingBufferUploads();
        this.FlushGraphicsResourceSets();
        this.FlushPendingUavBarrier();
        this.FlushPendingBarriers();
        this.BeginRenderPassForDraw();
        this.DrawIndexedInstancedNoAlloc(indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
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
        this.EndRenderPassForInternalUse();
        this._bufferUpdatePlanner.Update(buffer, bufferOffsetInBytes, source, sizeInBytes);
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

        D3D12BarrierQueueResult result = this._barriers.QueueTransition(resource, from, to);
        if (result == D3D12BarrierQueueResult.Full) {
            this.FlushPendingBarriers();
            this._barriers.QueueTransition(resource, from, to);
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
        this.TransitionBuffer(buffer, ResourceStates.VertexAndConstantBuffer);

        uint stride = 0;
        D3D12Pipeline currentGraphicsPipeline = this.CurrentGraphicsPipeline;
        if (currentGraphicsPipeline != null && index < currentGraphicsPipeline.VertexStrides.Length) {
            stride = currentGraphicsPipeline.VertexStrides[index];
        }

        uint viewSize = buffer.GetBindableSize(offset);
        VertexBufferView view = new(buffer.GetGpuVirtualAddress(offset), viewSize, stride);
        this.SetVertexBufferNoAlloc(index, ref view);
        this._inputAssembler.SetVertexBufferStride(index, stride);
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

            this.BindVertexBuffer(index, buffer, this._inputAssembler.GetVertexBufferOffset(index));
        }
    }

    /// <summary>
    /// Refreshes input-assembler views that point at a dynamic buffer whose native snapshot offset changed.
    /// </summary>
    /// <param name="buffer">The dynamic buffer whose binding version changed.</param>
    private void RefreshDynamicBufferBindings(D3D12DeviceBuffer buffer) {
        ulong bindVersion = buffer.BindVersion;
        for (uint index = 0; index < this._inputAssembler.MaxBoundVertexBufferSlot; index++) {
            if (!ReferenceEquals(this._inputAssembler.GetVertexBuffer(index), buffer)) {
                continue;
            }

            this.BindVertexBuffer(index, buffer, this._inputAssembler.GetVertexBufferOffset(index));
            this._inputAssembler.SetVertexBufferVersion(index, bindVersion);
            if (D3D12CommandListPerfTracker.Enabled) {
                this._perf.VertexBufferBinds++;
            }
        }

        if (!this._inputAssembler.HasIndexBuffer || !ReferenceEquals(this._inputAssembler.IndexBuffer, buffer)) {
            return;
        }

        this.TransitionBuffer(buffer, ResourceStates.IndexBuffer);
        uint viewSize = buffer.GetBindableSize(this._inputAssembler.IndexBufferOffset);
        IndexBufferView indexView = new(buffer.GetGpuVirtualAddress(this._inputAssembler.IndexBufferOffset), viewSize, D3D12Formats.ToDxgiFormat(this._inputAssembler.IndexFormat));
        this.SetIndexBufferNoAlloc(ref indexView);
        this._inputAssembler.SetIndexBufferVersion(bindVersion);
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.IndexBufferBinds++;
        }
    }

    /// <summary>
    /// Refreshes input-assembler views for an internal D3D12 helper after a dynamic buffer native address change.
    /// </summary>
    /// <param name="buffer">The dynamic buffer whose binding version changed.</param>
    internal void RefreshDynamicBufferBindingsForInternalUse(D3D12DeviceBuffer buffer) {
        this.RefreshDynamicBufferBindings(buffer);
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

        D3D12BarrierQueueResult result = this._barriers.QueueSubresourceTransition(resource, from, to, subresource);
        if (result == D3D12BarrierQueueResult.Full) {
            this.FlushPendingBarriers();
            this._barriers.QueueSubresourceTransition(resource, from, to, subresource);
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.SubresourceTransitions++;
        }
    }

    /// <summary>
    /// Executes the flush pending uav barrier logic for this backend.
    /// </summary>
    private void FlushPendingUavBarrier() {
        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        bool flushed = this._barriers.FlushPendingUavBarrier(this.NativeCommandList);
        if (!flushed) {
            return;
        }

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
    }

    /// <summary>
    /// Flushes all accumulated pending resource-barrier transitions as a single batched D3D12 call.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushPendingBarriers() {
        long startTicks = D3D12CommandListPerfTracker.Enabled ? Stopwatch.GetTimestamp() : 0;
        bool flushed = this._barriers.FlushPendingBarriers(this.NativeCommandList);
        if (!flushed) {
            return;
        }

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
            this._debugMarkerArgs[0] = metadata;
            this._debugMarkerArgs[1] = dataPtr;
            this._debugMarkerArgs[2] = sizeValue;
            if (beginEvent) {
                markerMethod.Invoke(this.NativeCommandList, this._debugMarkerArgs);
            }
            else if (setMarker) {
                markerMethod.Invoke(this.NativeCommandList, this._debugMarkerArgs);
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
    /// Binds a graphics root constant buffer view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetGraphicsRootConstantBufferViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this.SetRootBufferViewNoAlloc(38, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a graphics root shader resource view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetGraphicsRootShaderResourceViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this.SetRootBufferViewNoAlloc(40, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a graphics root unordered access view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetGraphicsRootUnorderedAccessViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this.SetRootBufferViewNoAlloc(42, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a compute root constant buffer view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetComputeRootConstantBufferViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this.SetRootBufferViewNoAlloc(37, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a compute root shader resource view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetComputeRootShaderResourceViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
        this.SetRootBufferViewNoAlloc(39, rootParameterIndex, gpuAddress);
    }

    /// <summary>
    /// Binds a compute root unordered access view without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetComputeRootUnorderedAccessViewNoAlloc(uint rootParameterIndex, ulong gpuAddress) {
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
    /// Binds pipeline state for an internal D3D12 helper without going through the managed COM wrapper.
    /// </summary>
    /// <param name="pipelineState">The pipeline state to bind.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetPipelineStateNoAllocForInternalUse(ID3D12PipelineState pipelineState) {
        this.SetPipelineStateNoAlloc(pipelineState);
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
    internal unsafe void OMSetRenderTargetsNoAlloc(uint numRenderTargetDescriptors, CpuDescriptorHandle rtvHandle, bool hasDepthStencil, CpuDescriptorHandle dsvHandle) {
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, int, void*, void> omSetRenderTargets = (delegate* unmanaged[Stdcall]<void*, uint, void*, int, void*, void>)vtbl[46];
        omSetRenderTargets((void*)this.NativeCommandList.NativePointer, numRenderTargetDescriptors, Unsafe.AsPointer(ref rtvHandle), 1, hasDepthStencil ? Unsafe.AsPointer(ref dsvHandle) : null);
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
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, void*, void> setRootSig = (delegate* unmanaged[Stdcall]<void*, void*, void>)vtbl[29];
        setRootSig((void*)this.NativeCommandList.NativePointer, (void*)rootSignature.NativePointer);
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
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, void*, void> setRootSig = (delegate* unmanaged[Stdcall]<void*, void*, void>)vtbl[30];
        setRootSig((void*)this.NativeCommandList.NativePointer, (void*)rootSignature.NativePointer);
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
    internal unsafe void OMSetRenderTargetsArrayNoAlloc(CpuDescriptorHandle[] rtvs, bool hasDepthStencil, CpuDescriptorHandle dsv) {
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
        void** vtbl = *(void***)this.NativeCommandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, CpuDescriptorHandle, uint, float, byte, uint, void*, void> fn =
            (delegate* unmanaged[Stdcall]<void*, CpuDescriptorHandle, uint, float, byte, uint, void*, void>)vtbl[47];
        fn((void*)this.NativeCommandList.NativePointer, dsv, clearFlags, depth, stencil, 0u, null);
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
        this._descriptorSetBinder.FlushGraphicsResourceSets(this._graphicsResourceSets, this.CurrentGraphicsPipeline);
    }

    /// <summary>
    /// Flushes compute resource sets that were changed since the previous dispatch.
    /// </summary>
    private void FlushComputeResourceSets() {
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
        this._bufferUpdatePlanner.Flush();
    }

    /// <summary>
    /// Marks currently bound resource sets dirty when a dynamic buffer moves to a new native snapshot.
    /// </summary>
    /// <param name="buffer">The buffer whose native binding address changed.</param>
    private void MarkResourceSetsReferencingBufferDirty(D3D12DeviceBuffer buffer) {
        this._descriptorSetBinder.MarkResourceSetsReferencingBufferDirty(this._graphicsResourceSets, this.CurrentGraphicsPipeline, this._computeResourceSets, this.CurrentComputePipeline, buffer);
    }

    /// <summary>
    /// Marks currently bound resource sets dirty for an internal D3D12 helper.
    /// </summary>
    /// <param name="buffer">The buffer whose native binding address changed.</param>
    internal void MarkResourceSetsReferencingBufferDirtyForInternalUse(D3D12DeviceBuffer buffer) {
        this.MarkResourceSetsReferencingBufferDirty(buffer);
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
        this.SetRootDescriptorTableNoAlloc(31, rootParameterIndex, gpuHandle);
    }

    /// <summary>
    /// Binds a graphics descriptor table without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe void SetGraphicsRootDescriptorTableNoAlloc(uint rootParameterIndex, GpuDescriptorHandle gpuHandle) {
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
