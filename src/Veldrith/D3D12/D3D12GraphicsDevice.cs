using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using D3D12Feature = Vortice.Direct3D12.Feature;
using VorticeD3D12 = Vortice.Direct3D12.D3D12;
using VorticeDXGI = Vortice.DXGI.DXGI;

#if !VELDRID_D3D12_PERF
#pragma warning disable CS0162
#endif

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12GraphicsDevice.
/// </summary>
internal sealed class D3D12GraphicsDevice : GraphicsDevice {

    /// <summary>
    /// Stores the number of submissions between D3D12 device performance reports.
    /// </summary>
    private const int _perfReportIntervalSubmissions = 240;

    /// <summary>
    /// Stores the default number of submits between deferred-disposal fence polls.
    /// </summary>
    private const int DefaultDeferredDisposalPumpInterval = 4;

    /// <summary>
    /// Stores the maximum number of pending deferred-disposal batches before throttling is bypassed.
    /// </summary>
    private const int DeferredDisposalPumpHighWatermark = 64;

    /// <summary>
    /// Stores the number of submits between deferred-disposal fence polls.
    /// </summary>
    private static readonly int _deferredDisposalPumpInterval = ReadDeferredDisposalPumpInterval();

    /// <summary>
    /// Gets whether persistent D3D12 pipeline library caching is enabled.
    /// </summary>
    private static readonly bool _persistentPipelineLibraryEnabled = !string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PIPELINE_LIBRARY"), "0", StringComparison.Ordinal);

    /// <summary>
    /// Stores the directory used for persistent D3D12 pipeline library blobs.
    /// </summary>
    private static readonly string _persistentPipelineLibraryDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Veldrith",
        "D3D12PipelineLibrary");

    /// <summary>
    /// Stores the d3d12 features state used by this instance.
    /// </summary>
    private static readonly GraphicsDeviceFeatures _d3D12Features = new(true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false);

    /// <summary>
    /// Tracks whether D3D12 performance logging is enabled for device-level upload and submit work.
    /// </summary>
    #if VELDRID_D3D12_PERF
    private static readonly bool _perfLogEnabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PERF"), "1", StringComparison.Ordinal);
    #else
    private const bool _perfLogEnabled = false;
    #endif

    /// <summary>
    /// Gets whether D3D12 performance logging is enabled.
    /// </summary>
    #if VELDRID_D3D12_PERF
    internal static bool PerfLogEnabled => _perfLogEnabled;
    #else
    internal const bool PerfLogEnabled = false;
    #endif

    /// <summary>
    /// Reads the deferred-disposal pump interval from the environment.
    /// </summary>
    /// <returns>The configured pump interval clamped to the supported range.</returns>
    private static int ReadDeferredDisposalPumpInterval() {
        string value = Environment.GetEnvironmentVariable("VELDRID_D3D12_DEFERRED_DISPOSAL_PUMP_INTERVAL");

        if (int.TryParse(value, out int parsed)) {
            return Math.Clamp(parsed, 1, 64);
        }

        return DefaultDeferredDisposalPumpInterval;
    }

    /// <summary>
    /// Gets whether small immediate buffer updates should use the experimental shared-page batcher.
    /// </summary>
    private static readonly bool _immediateBufferUpdateBatcherEnabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_IMMEDIATE_BUFFER_BATCHER"), "1", StringComparison.Ordinal);

    /// <summary>
    /// Tracks submit calls for deferred-disposal pump throttling.
    /// </summary>
    private int _deferredDisposalPumpSubmitCounter;

    /// <summary>
    /// Stores the d3d12 info state used by this instance.
    /// </summary>
    private readonly BackendInfoD3D12 _d3D12Info;

    /// <summary>
    /// Stores the device state used by this instance.
    /// </summary>
    private readonly ID3D12Device _device;

    /// <summary>
    /// Caches format support cache to reduce repeated allocations and lookups.
    /// </summary>
    private readonly Dictionary<Format, CachedFormatSupport> _formatSupportCache = new();

    /// <summary>
    /// Caches format support cache lock to reduce repeated allocations and lookups.
    /// </summary>
    private readonly object _formatSupportCacheLock = new();

    /// <summary>
    /// Stores the resource factory state used by this instance.
    /// </summary>
    private readonly D3D12ResourceFactory _resourceFactory;

    /// <summary>
    /// Tracks whether direct command lists support D3D12 WriteBufferImmediate.
    /// </summary>
    private readonly bool _supportsDirectWriteBufferImmediate;

    /// <summary>
    /// Tracks whether direct command lists can use native D3D12 render passes.
    /// </summary>
    private readonly bool _supportsRenderPasses;

    /// <summary>
    /// Serializes direct command queue, presentation, and swapchain resize operations.
    /// </summary>
    private readonly object _commandQueueLock = new();

    /// <summary>
    /// Caches root signature cache to avoid repeated allocations and lookups.
    /// </summary>
    private readonly Dictionary<string, ID3D12RootSignature> _rootSignatureCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Caches root signature cache lock to reduce repeated allocations and lookups.
    /// </summary>
    private readonly object _rootSignatureCacheLock = new();

    /// <summary>
    /// Caches native D3D12 pipeline states to avoid repeated PSO creation for identical descriptions.
    /// </summary>
    private readonly Dictionary<string, ID3D12PipelineState> _pipelineStateCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Owns device-global shader-visible descriptor heaps used by D3D12 ResourceSets.
    /// </summary>
    private readonly D3D12DescriptorHeapState _descriptorHeapState;

    /// <summary>
    /// Protects native pipeline state cache access.
    /// </summary>
    private readonly object _pipelineStateCacheLock = new();

    /// <summary>
    /// Stores the optional device-level D3D12 pipeline library used to persist PSO data across runs.
    /// </summary>
    private ID3D12PipelineLibrary _pipelineLibrary;

    /// <summary>
    /// Stores the optional D3D12 device interface used to create pipeline libraries.
    /// </summary>
    private ID3D12Device1 _pipelineLibraryDevice;

    /// <summary>
    /// Stores the persistent pipeline library file path for this device, when enabled.
    /// </summary>
    private string _pipelineLibraryPath;

    /// <summary>
    /// Tracks whether the pipeline library changed and should be serialized on dispose.
    /// </summary>
    private bool _pipelineLibraryDirty;

    /// <summary>
    /// Tracks whether persistent pipeline library use failed and should stay disabled for this device.
    /// </summary>
    private bool _pipelineLibraryUnavailable;

    /// <summary>
    /// Keeps the loaded pipeline-library blob pinned for the lifetime of the native library object.
    /// </summary>
    private byte[] _pipelineLibraryBlob;

    /// <summary>
    /// Pins <see cref="_pipelineLibraryBlob"/> because D3D12 keeps the input blob pointer alive.
    /// </summary>
    private GCHandle _pipelineLibraryBlobHandle;

    /// <summary>
    /// Stores the submission fence state used by this instance.
    /// </summary>
    private readonly ID3D12Fence _submissionFence;

    /// <summary>
    /// Stores the submission fence event state used by this instance.
    /// </summary>
    private readonly AutoResetEvent _submissionFenceEvent;

    /// <summary>
    /// Stores the fence used to track completion of immediate copy submissions.
    /// </summary>
    private readonly ID3D12Fence _immediateCopyFence;

    /// <summary>
    /// Stores the event used while waiting for immediate copy fence completion.
    /// </summary>
    private readonly AutoResetEvent _immediateCopyFenceEvent;

    /// <summary>
    /// Caches reusable immediate copy command contexts to avoid allocator and command list churn.
    /// </summary>
    private readonly List<ImmediateCopyContext> _immediateCopyContexts = new();

    /// <summary>
    /// Stores upload buffers recorded into the batched immediate copy command list.
    /// </summary>
    private readonly List<D3D12ResourceAllocation> _batchedImmediateUploadBuffers = new();

    /// <summary>
    /// Batches small immediate buffer updates into shared upload pages.
    /// </summary>
    private readonly D3D12ImmediateBufferUpdateBatcher _immediateBufferUpdateBatcher;

    /// <summary>
    /// Pools standalone upload buffers and transient upload-ring pages.
    /// </summary>
    private readonly D3D12UploadBufferPool _uploadBufferPool;

    /// <summary>
    /// Stores resources retained by batched immediate copy commands until the copy fence completes.
    /// </summary>
    private readonly List<IDisposable> _batchedImmediateRetainedResources = new();

    /// <summary>
    /// Protects immediate copy command context pool access.
    /// </summary>
    private readonly object _immediateCopyContextsLock = new();

    /// <summary>
    /// Protects the batched immediate copy command list.
    /// </summary>
    private readonly object _batchedImmediateCopyLock = new();

    /// <summary>
    /// Stores deferred disposals tracked by submission fence values.
    /// </summary>
    private readonly Queue<DeferredDisposal> _submissionDeferredDisposals = new();

    /// <summary>
    /// Protects submission deferred disposal queue access.
    /// </summary>
    private readonly object _submissionDeferredDisposalsLock = new();

    /// <summary>
    /// Tracks the approximate number of pending submission deferred disposals for lock-free empty checks.
    /// </summary>
    private int _submissionDeferredDisposalCount;

    /// <summary>
    /// Stores the oldest submission fence value that can release deferred disposals.
    /// </summary>
    private ulong _submissionDeferredDisposalNextFenceValue;

    /// <summary>
    /// Stores deferred disposals tracked by immediate copy fence values.
    /// </summary>
    private readonly Queue<DeferredDisposal> _immediateDeferredDisposals = new();

    /// <summary>
    /// Protects immediate deferred disposal queue access.
    /// </summary>
    private readonly object _immediateDeferredDisposalsLock = new();

    /// <summary>
    /// Tracks the approximate number of pending immediate deferred disposals for lock-free empty checks.
    /// </summary>
    private int _immediateDeferredDisposalCount;

    /// <summary>
    /// Stores the oldest immediate-copy fence value that can release deferred disposals.
    /// </summary>
    private ulong _immediateDeferredDisposalNextFenceValue;

    /// <summary>
    /// Reusable batch buffer for deferred disposal drains — avoids per-pump List allocation.
    /// </summary>
    private IDisposable[] _pumpDisposalBatch = new IDisposable[64];

    /// <summary>
    /// Stores accumulated CPU time spent flushing batched immediate upload work.
    /// </summary>
    private double _perfAccumImmediateFlushMs;

    /// <summary>
    /// Stores accumulated CPU time spent executing standalone immediate command lists.
    /// </summary>
    private double _perfAccumImmediateExecuteMs;

    /// <summary>
    /// Stores accumulated CPU time spent recording batched immediate upload commands.
    /// </summary>
    private double _perfAccumImmediateRecordMs;

    /// <summary>
    /// Stores accumulated CPU time spent waiting for immediate upload fences.
    /// </summary>
    private double _perfAccumImmediateWaitMs;

    /// <summary>
    /// Stores accumulated CPU time spent submitting command lists.
    /// </summary>
    private double _perfAccumSubmitMs;

    /// <summary>
    /// Stores accumulated CPU time spent waiting to enter the D3D12 command queue lock.
    /// </summary>
    private double _perfAccumSubmitLockWaitMs;

    /// <summary>
    /// Stores accumulated CPU time spent pumping D3D12 deferred-disposal queues during submit.
    /// </summary>
    private double _perfAccumSubmitDisposalPumpMs;

    /// <summary>
    /// Stores accumulated CPU time spent executing D3D12 command lists on the queue.
    /// </summary>
    private double _perfAccumSubmitExecuteMs;

    /// <summary>
    /// Stores accumulated CPU time spent signaling D3D12 submit fences.
    /// </summary>
    private double _perfAccumSubmitSignalMs;

    /// <summary>
    /// Stores accumulated CPU time spent marking D3D12 command lists submitted.
    /// </summary>
    private double _perfAccumSubmitMarkMs;

    /// <summary>
    /// Stores accumulated CPU time spent presenting swapchains.
    /// </summary>
    private double _perfAccumPresentMs;

    /// <summary>
    /// Stores accumulated CPU time spent waiting for next frame readiness.
    /// </summary>
    private double _perfAccumFrameWaitMs;

    /// <summary>
    /// Stores accumulated CPU time spent creating D3D12 buffers.
    /// </summary>
    private double _perfAccumCreateBufferMs;

    /// <summary>
    /// Stores accumulated CPU time spent creating D3D12 pipelines.
    /// </summary>
    private double _perfAccumCreatePipelineMs;

    /// <summary>
    /// Stores accumulated CPU time spent creating D3D12 resource sets.
    /// </summary>
    private double _perfAccumCreateResourceSetMs;

    /// <summary>
    /// Stores accumulated CPU time spent creating D3D12 shaders.
    /// </summary>
    private double _perfAccumCreateShaderMs;

    /// <summary>
    /// Stores accumulated CPU time spent creating D3D12 textures.
    /// </summary>
    private double _perfAccumCreateTextureMs;

    /// <summary>
    /// Stores the number of batched immediate flushes recorded for the current performance window.
    /// </summary>
    private ulong _perfAccumImmediateFlushes;

    /// <summary>
    /// Stores the number of D3D12 buffers created in the current performance window.
    /// </summary>
    private ulong _perfAccumCreateBuffers;

    /// <summary>
    /// Stores the number of D3D12 pipelines created in the current performance window.
    /// </summary>
    private ulong _perfAccumCreatePipelines;

    /// <summary>
    /// Stores the number of D3D12 resource sets created in the current performance window.
    /// </summary>
    private ulong _perfAccumCreateResourceSets;

    /// <summary>
    /// Stores the number of D3D12 shaders created in the current performance window.
    /// </summary>
    private ulong _perfAccumCreateShaders;

    /// <summary>
    /// Stores the number of D3D12 textures created in the current performance window.
    /// </summary>
    private ulong _perfAccumCreateTextures;

    /// <summary>
    /// Stores the number of standalone immediate command lists recorded for the current performance window.
    /// </summary>
    private ulong _perfAccumImmediateExecutes;

    /// <summary>
    /// Stores the number of batched immediate record calls recorded for the current performance window.
    /// </summary>
    private ulong _perfAccumImmediateRecordCalls;

    /// <summary>
    /// Stores the number of upload buffers retained by batched immediate submissions in the current performance window.
    /// </summary>
    private ulong _perfAccumImmediateUploadBuffers;

    /// <summary>
    /// Stores the last elapsed millisecond value used for device-level performance reporting.
    /// </summary>
    private double _perfLastReportMs;

    /// <summary>
    /// Stores the largest immediate execute time observed in the current device performance window.
    /// </summary>
    private double _perfMaxImmediateExecuteMs;

    /// <summary>
    /// Stores the largest immediate flush time observed in the current device performance window.
    /// </summary>
    private double _perfMaxImmediateFlushMs;

    /// <summary>
    /// Stores the largest immediate record time observed in the current device performance window.
    /// </summary>
    private double _perfMaxImmediateRecordMs;

    /// <summary>
    /// Stores the largest immediate wait time observed in the current device performance window.
    /// </summary>
    private double _perfMaxImmediateWaitMs;

    /// <summary>
    /// Stores the largest submit time observed in the current device performance window.
    /// </summary>
    private double _perfMaxSubmitMs;

    /// <summary>
    /// Stores the largest command queue lock wait observed in the current device performance window.
    /// </summary>
    private double _perfMaxSubmitLockWaitMs;

    /// <summary>
    /// Stores the largest deferred-disposal pump time observed in the current device performance window.
    /// </summary>
    private double _perfMaxSubmitDisposalPumpMs;

    /// <summary>
    /// Stores the largest command-list execute time observed in the current device performance window.
    /// </summary>
    private double _perfMaxSubmitExecuteMs;

    /// <summary>
    /// Stores the largest submit fence signal time observed in the current device performance window.
    /// </summary>
    private double _perfMaxSubmitSignalMs;

    /// <summary>
    /// Stores the largest submitted command-list bookkeeping time observed in the current device performance window.
    /// </summary>
    private double _perfMaxSubmitMarkMs;

    /// <summary>
    /// Stores the largest present time observed in the current device performance window.
    /// </summary>
    private double _perfMaxPresentMs;

    /// <summary>
    /// Stores the largest next-frame wait time observed in the current device performance window.
    /// </summary>
    private double _perfMaxFrameWaitMs;

    /// <summary>
    /// Stores the largest buffer creation time observed in the current device performance window.
    /// </summary>
    private double _perfMaxCreateBufferMs;

    /// <summary>
    /// Stores the largest pipeline creation time observed in the current device performance window.
    /// </summary>
    private double _perfMaxCreatePipelineMs;

    /// <summary>
    /// Stores the largest resource set creation time observed in the current device performance window.
    /// </summary>
    private double _perfMaxCreateResourceSetMs;

    /// <summary>
    /// Stores the largest shader creation time observed in the current device performance window.
    /// </summary>
    private double _perfMaxCreateShaderMs;

    /// <summary>
    /// Stores the largest texture creation time observed in the current device performance window.
    /// </summary>
    private double _perfMaxCreateTextureMs;

    /// <summary>
    /// Stores the number of command-list submissions observed by device-level performance logging.
    /// </summary>
    private ulong _perfSubmissions;

    /// <summary>
    /// Measures wall-clock time for D3D12 device-level performance reports.
    /// </summary>
    private readonly Stopwatch _perfStopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Stores the next fence value for immediate copy submissions.
    /// </summary>
    private ulong _nextImmediateCopyFenceValue = 1;

    /// <summary>
    /// Stores the currently open batched immediate copy command context.
    /// </summary>
    private ImmediateCopyContext _batchedImmediateCopyContext;

    /// <summary>
    /// Tracks whether the batched immediate copy command list contains work.
    /// </summary>
    private bool _batchedImmediateCopyHasWork;

    /// <summary>
    /// Converts high-resolution stopwatch ticks to milliseconds for D3D12 performance logging.
    /// </summary>
    /// <param name="ticks">The elapsed stopwatch ticks.</param>
    /// <returns>The elapsed time in milliseconds.</returns>
    private static double TicksToMilliseconds(long ticks) {
        return ticks * 1000.0 / Stopwatch.Frequency;
    }

    /// <summary>
    /// Records CPU time spent creating a backend resource.
    /// </summary>
    /// <param name="kind">The kind of resource that was created.</param>
    /// <param name="elapsedMs">The elapsed creation time in milliseconds.</param>
    internal void RecordResourceCreationPerf(D3D12ResourceCreationKind kind, double elapsedMs) {
        if (!_perfLogEnabled) {
            return;
        }

        switch (kind) {
            case D3D12ResourceCreationKind.Buffer:
                this._perfAccumCreateBufferMs += elapsedMs;
                this._perfAccumCreateBuffers++;
                this._perfMaxCreateBufferMs = Math.Max(this._perfMaxCreateBufferMs, elapsedMs);
                break;
            case D3D12ResourceCreationKind.Pipeline:
                this._perfAccumCreatePipelineMs += elapsedMs;
                this._perfAccumCreatePipelines++;
                this._perfMaxCreatePipelineMs = Math.Max(this._perfMaxCreatePipelineMs, elapsedMs);
                break;
            case D3D12ResourceCreationKind.ResourceSet:
                this._perfAccumCreateResourceSetMs += elapsedMs;
                this._perfAccumCreateResourceSets++;
                this._perfMaxCreateResourceSetMs = Math.Max(this._perfMaxCreateResourceSetMs, elapsedMs);
                break;
            case D3D12ResourceCreationKind.Shader:
                this._perfAccumCreateShaderMs += elapsedMs;
                this._perfAccumCreateShaders++;
                this._perfMaxCreateShaderMs = Math.Max(this._perfMaxCreateShaderMs, elapsedMs);
                break;
            case D3D12ResourceCreationKind.Texture:
                this._perfAccumCreateTextureMs += elapsedMs;
                this._perfAccumCreateTextures++;
                this._perfMaxCreateTextureMs = Math.Max(this._perfMaxCreateTextureMs, elapsedMs);
                break;
        }
    }

    /// <summary>
    /// Stores the next submission fence value state used by this instance.
    /// </summary>
    private ulong _nextSubmissionFenceValue = 1;

    /// <summary>
    /// Stores the latest fence value signaled for user command-list submissions.
    /// </summary>
    private ulong _lastSubmissionFenceValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12GraphicsDevice" /> type.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="swapchainDescription">The swapchain description value used by this operation.</param>
    public D3D12GraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? swapchainDescription) {
        if (!IsSupported()) {
            throw new PlatformNotSupportedException("Direct3D 12 is only supported on Windows.");
        }

        this.DxgiFactory = VorticeDXGI.CreateDXGIFactory2<IDXGIFactory4>(false);
        IDXGIAdapter1 adapter = SelectAdapter(this.DxgiFactory);
        FeatureLevel maxFeatureLevel = FeatureLevel.Level_11_0;
        try {
            if (adapter != null) {
                maxFeatureLevel = GetMaxSupportedFeatureLevel(adapter);
                VorticeD3D12.D3D12CreateDevice(adapter, FeatureLevel.Level_11_0, out this._device).CheckError();
                AdapterDescription1 description = adapter.Description1;
                this.DeviceName = description.Description?.TrimEnd('\0');
                this.VendorName = $"0x{description.VendorId:X4}";
            }
            else {
                maxFeatureLevel = GetMaxSupportedFeatureLevel(null);
                VorticeD3D12.D3D12CreateDevice(null, FeatureLevel.Level_11_0, out this._device).CheckError();
                this.DeviceName = "Direct3D 12 Device";
                this.VendorName = "Unknown";
            }
        }
        finally {
            adapter?.Dispose();
        }

        this.ApiVersion = ToGraphicsApiVersion(maxFeatureLevel);
        this.CommandQueue = this._device.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));
        this._submissionFence = this._device.CreateFence();
        this._submissionFenceEvent = new AutoResetEvent(false);
        this._immediateCopyFence = this._device.CreateFence();
        this._immediateCopyFenceEvent = new AutoResetEvent(false);
        this.MemoryManager = new D3D12DeviceMemoryManager(this._device);
        this.SrvUavDescriptorAllocator = new D3D12CpuDescriptorAllocator(this._device, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, 4096);
        this.SamplerDescriptorAllocator = new D3D12CpuDescriptorAllocator(this._device, DescriptorHeapType.Sampler, 1024);
        this.RtvDescriptorAllocator = new D3D12CpuDescriptorAllocator(this._device, DescriptorHeapType.RenderTargetView, 1024);
        this.DsvDescriptorAllocator = new D3D12CpuDescriptorAllocator(this._device, DescriptorHeapType.DepthStencilView, 1024);
        this._descriptorHeapState = new D3D12DescriptorHeapState(this);
        this.InitializePipelineLibrary();
        this._supportsDirectWriteBufferImmediate = this.CheckDirectWriteBufferImmediateSupport();
        this._supportsRenderPasses = this.CheckRenderPassSupport();
        this._resourceFactory = new D3D12ResourceFactory(this, this.Features);
        this._uploadBufferPool = new D3D12UploadBufferPool(this);
        this._immediateBufferUpdateBatcher = new D3D12ImmediateBufferUpdateBatcher(this);

        if (swapchainDescription != null) {
            SwapchainDescription scDesc = swapchainDescription.Value;
            this.MainSwapchain = new D3D12Swapchain(this, ref scDesc);
        }

        this._d3D12Info = new BackendInfoD3D12(this._device.NativePointer);
        if (this.MainSwapchain != null) {
            this.SyncToVerticalBlank = options.SyncToVerticalBlank;
        }

        this.PostDeviceCreated();
    }

    /// <summary>
    /// Gets or sets DeviceName.
    /// </summary>
    public override string DeviceName { get; }

    /// <summary>
    /// Gets or sets VendorName.
    /// </summary>
    public override string VendorName { get; }

    /// <summary>
    /// Gets or sets ApiVersion.
    /// </summary>
    public override GraphicsApiVersion ApiVersion { get; }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

    /// <summary>
    /// Gets or sets IsUvOriginTopLeft.
    /// </summary>
    public override bool IsUvOriginTopLeft => true;

    /// <summary>
    /// Gets or sets IsDepthRangeZeroToOne.
    /// </summary>
    public override bool IsDepthRangeZeroToOne => true;

    /// <summary>
    /// Gets or sets IsClipSpaceYInverted.
    /// </summary>
    public override bool IsClipSpaceYInverted => true;

    /// <summary>
    /// Gets or sets ResourceFactory.
    /// </summary>
    public override ResourceFactory ResourceFactory => this._resourceFactory;

    /// <summary>
    /// Gets or sets MainSwapchain.
    /// </summary>
    public override Swapchain MainSwapchain { get; }

    /// <summary>
    /// Gets or sets Features.
    /// </summary>
    public override GraphicsDeviceFeatures Features => _d3D12Features;

    /// <summary>
    /// Gets whether direct command lists can use WriteBufferImmediate.
    /// </summary>
    internal bool SupportsDirectWriteBufferImmediate => this._supportsDirectWriteBufferImmediate;

    /// <summary>
    /// Gets whether direct command lists can use native D3D12 render passes.
    /// </summary>
    internal bool SupportsRenderPasses => this._supportsRenderPasses;

    /// <summary>
    /// Gets or sets AllowTearing.
    /// </summary>
    public override bool AllowTearing {
        get => this.MainSwapchain is D3D12Swapchain d3D12Swapchain && d3D12Swapchain.AllowTearing;
        set {
            if (this.MainSwapchain is D3D12Swapchain d3D12Swapchain) {
                d3D12Swapchain.AllowTearing = value;
            }
        }
    }

    /// <summary>
    /// Stores the device state used by this instance.
    /// </summary>
    internal ID3D12Device Device => this._device;

    /// <summary>
    /// Gets or sets CommandQueue.
    /// </summary>
    internal ID3D12CommandQueue CommandQueue { get; }

    /// <summary>
    /// Gets the lock used to serialize command queue and swapchain operations.
    /// </summary>
    internal object CommandQueueLock => this._commandQueueLock;

    /// <summary>
    /// Executes one command list without allocating a temporary submit array.
    /// </summary>
    /// <param name="commandList">The command list to execute.</param>
    internal unsafe void ExecuteCommandListNoAlloc(ID3D12GraphicsCommandList commandList) {
        IntPtr* commandLists = stackalloc IntPtr[1];
        commandLists[0] = commandList.NativePointer;
        void** vtbl = *(void***)this.CommandQueue.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, void> executeCommandLists = (delegate* unmanaged[Stdcall]<void*, uint, void*, void>)vtbl[10];
        executeCommandLists((void*)this.CommandQueue.NativePointer, 1u, commandLists);
    }

    /// <summary>
    /// Signals a queue fence without going through the managed COM wrapper.
    /// </summary>
    /// <param name="fence">The fence to signal.</param>
    /// <param name="value">The fence value to signal.</param>
    internal unsafe void SignalQueueFenceNoAlloc(ID3D12Fence fence, ulong value) {
        void** vtbl = *(void***)this.CommandQueue.NativePointer;
        delegate* unmanaged[Stdcall]<void*, void*, ulong, int> signal = (delegate* unmanaged[Stdcall]<void*, void*, ulong, int>)vtbl[14];
        Result result = new(signal((void*)this.CommandQueue.NativePointer, (void*)fence.NativePointer, value));
        result.CheckError();
    }

    /// <summary>
    /// Gets or sets DxgiFactory.
    /// </summary>
    internal IDXGIFactory4 DxgiFactory { get; }

    /// <summary>
    /// Gets the D3D12 default heap memory manager.
    /// </summary>
    internal D3D12DeviceMemoryManager MemoryManager { get; }

    /// <summary>
    /// Gets the CPU SRV/UAV descriptor allocator.
    /// </summary>
    internal D3D12CpuDescriptorAllocator SrvUavDescriptorAllocator { get; }

    /// <summary>
    /// Gets the CPU sampler descriptor allocator.
    /// </summary>
    internal D3D12CpuDescriptorAllocator SamplerDescriptorAllocator { get; }

    /// <summary>
    /// Gets the CPU RTV descriptor allocator.
    /// </summary>
    internal D3D12CpuDescriptorAllocator RtvDescriptorAllocator { get; }

    /// <summary>
    /// Gets the CPU DSV descriptor allocator.
    /// </summary>
    internal D3D12CpuDescriptorAllocator DsvDescriptorAllocator { get; }

    /// <summary>
    /// Gets the device-global shader-visible descriptor heap state.
    /// </summary>
    internal D3D12DescriptorHeapState DescriptorHeapState => this._descriptorHeapState;

    /// <summary>
    /// Emits a periodic device-level performance report when D3D12 performance logging is enabled.
    /// </summary>
    /// <param name="submitMs">The CPU time spent in the current submit path.</param>
    private void ReportDevicePerfIfNeeded(double submitMs) {
        if (!_perfLogEnabled) {
            return;
        }

        this._perfSubmissions++;
        this._perfAccumSubmitMs += submitMs;
        this._perfMaxSubmitMs = Math.Max(this._perfMaxSubmitMs, submitMs);
        if (this._perfSubmissions % _perfReportIntervalSubmissions != 0) {
            return;
        }

        double elapsedMs = this._perfStopwatch.Elapsed.TotalMilliseconds;
        double reportWindowMs = elapsedMs - this._perfLastReportMs;
        this._perfLastReportMs = elapsedMs;
        double invSubmissions = 1.0 / _perfReportIntervalSubmissions;
        Console.WriteLine($"[D3D12 PERF] device {_perfReportIntervalSubmissions} submits/{reportWindowMs:F0}ms avg: " + $"submitMs={this._perfAccumSubmitMs * invSubmissions:F3}, submitLockMs={this._perfAccumSubmitLockWaitMs * invSubmissions:F3}, submitPumpMs={this._perfAccumSubmitDisposalPumpMs * invSubmissions:F3}, submitExecMs={this._perfAccumSubmitExecuteMs * invSubmissions:F3}, submitSignalMs={this._perfAccumSubmitSignalMs * invSubmissions:F3}, submitMarkMs={this._perfAccumSubmitMarkMs * invSubmissions:F3}, presentMs={this._perfAccumPresentMs * invSubmissions:F3}, frameWaitMs={this._perfAccumFrameWaitMs * invSubmissions:F3}, " + $"immRecordMs={this._perfAccumImmediateRecordMs * invSubmissions:F3} ({this._perfAccumImmediateRecordCalls * invSubmissions:F2}x), " + $"immFlushMs={this._perfAccumImmediateFlushMs * invSubmissions:F3} ({this._perfAccumImmediateFlushes * invSubmissions:F2}x), " + $"immExecMs={this._perfAccumImmediateExecuteMs * invSubmissions:F3} ({this._perfAccumImmediateExecutes * invSubmissions:F2}x), " + $"immWaitMs={this._perfAccumImmediateWaitMs * invSubmissions:F3}, " + $"createBuf={this._perfAccumCreateBuffers * invSubmissions:F2}/{this._perfMaxCreateBufferMs:F3}ms, " + $"createTex={this._perfAccumCreateTextures * invSubmissions:F2}/{this._perfMaxCreateTextureMs:F3}ms, " + $"createPipe={this._perfAccumCreatePipelines * invSubmissions:F2}/{this._perfMaxCreatePipelineMs:F3}ms, " + $"createSet={this._perfAccumCreateResourceSets * invSubmissions:F2}/{this._perfMaxCreateResourceSetMs:F3}ms, " + $"createShader={this._perfAccumCreateShaders * invSubmissions:F2}/{this._perfMaxCreateShaderMs:F3}ms, " + $"maxSubmitMs={this._perfMaxSubmitMs:F3}, maxSubmitLockMs={this._perfMaxSubmitLockWaitMs:F3}, maxSubmitPumpMs={this._perfMaxSubmitDisposalPumpMs:F3}, maxSubmitExecMs={this._perfMaxSubmitExecuteMs:F3}, maxSubmitSignalMs={this._perfMaxSubmitSignalMs:F3}, maxSubmitMarkMs={this._perfMaxSubmitMarkMs:F3}, maxPresentMs={this._perfMaxPresentMs:F3}, maxFrameWaitMs={this._perfMaxFrameWaitMs:F3}, maxImmRecordMs={this._perfMaxImmediateRecordMs:F3}, maxImmFlushMs={this._perfMaxImmediateFlushMs:F3}, " + $"maxImmExecMs={this._perfMaxImmediateExecuteMs:F3}, maxImmWaitMs={this._perfMaxImmediateWaitMs:F3}, " + $"immUploadBuf={this._perfAccumImmediateUploadBuffers * invSubmissions:F2}, " + this.MemoryManager.GetStatsString());

        this._perfAccumImmediateFlushMs = 0;
        this._perfAccumImmediateExecuteMs = 0;
        this._perfAccumImmediateRecordMs = 0;
        this._perfAccumImmediateWaitMs = 0;
        this._perfAccumSubmitMs = 0;
        this._perfAccumSubmitLockWaitMs = 0;
        this._perfAccumSubmitDisposalPumpMs = 0;
        this._perfAccumSubmitExecuteMs = 0;
        this._perfAccumSubmitSignalMs = 0;
        this._perfAccumSubmitMarkMs = 0;
        this._perfAccumPresentMs = 0;
        this._perfAccumFrameWaitMs = 0;
        this._perfAccumCreateBufferMs = 0;
        this._perfAccumCreatePipelineMs = 0;
        this._perfAccumCreateResourceSetMs = 0;
        this._perfAccumCreateShaderMs = 0;
        this._perfAccumCreateTextureMs = 0;
        this._perfAccumImmediateFlushes = 0;
        this._perfAccumCreateBuffers = 0;
        this._perfAccumCreatePipelines = 0;
        this._perfAccumCreateResourceSets = 0;
        this._perfAccumCreateShaders = 0;
        this._perfAccumCreateTextures = 0;
        this._perfAccumImmediateExecutes = 0;
        this._perfAccumImmediateRecordCalls = 0;
        this._perfAccumImmediateUploadBuffers = 0;
        this._perfMaxCreateBufferMs = 0;
        this._perfMaxCreatePipelineMs = 0;
        this._perfMaxCreateResourceSetMs = 0;
        this._perfMaxCreateShaderMs = 0;
        this._perfMaxCreateTextureMs = 0;
        this._perfMaxImmediateExecuteMs = 0;
        this._perfMaxImmediateFlushMs = 0;
        this._perfMaxImmediateRecordMs = 0;
        this._perfMaxImmediateWaitMs = 0;
        this._perfMaxSubmitMs = 0;
        this._perfMaxSubmitLockWaitMs = 0;
        this._perfMaxSubmitDisposalPumpMs = 0;
        this._perfMaxSubmitExecuteMs = 0;
        this._perfMaxSubmitSignalMs = 0;
        this._perfMaxSubmitMarkMs = 0;
        this._perfMaxPresentMs = 0;
        this._perfMaxFrameWaitMs = 0;
    }

    /// <summary>
    /// Executes the is supported logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public static bool IsSupported() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            return false;
        }

        return VorticeD3D12.D3D12CreateDevice(null, FeatureLevel.Level_11_0, out ID3D12Device _).Success;
    }

    /// <summary>
    /// Gets the highest Direct3D feature level supported by the adapter.
    /// </summary>
    /// <param name="adapter">The adapter to query, or null for the default adapter.</param>
    /// <returns>The value produced by this operation.</returns>
    private static FeatureLevel GetMaxSupportedFeatureLevel(IDXGIAdapter adapter) {
        try {
            return adapter != null
                ? VorticeD3D12.GetMaxSupportedFeatureLevel(adapter, FeatureLevel.Level_11_0)
                : VorticeD3D12.GetMaxSupportedFeatureLevel(FeatureLevel.Level_11_0);
        }
        catch (SharpGenException) {
            return FeatureLevel.Level_11_0;
        }
    }

    /// <summary>
    /// Converts a Direct3D feature level into the generic graphics API version value.
    /// </summary>
    /// <param name="featureLevel">The feature level to convert.</param>
    /// <returns>The value produced by this operation.</returns>
    private static GraphicsApiVersion ToGraphicsApiVersion(FeatureLevel featureLevel) {
        switch (featureLevel) {
            case FeatureLevel.Level_12_2: return new GraphicsApiVersion(12, 2, 0, 0);
            case FeatureLevel.Level_12_1: return new GraphicsApiVersion(12, 1, 0, 0);
            case FeatureLevel.Level_12_0: return new GraphicsApiVersion(12, 0, 0, 0);
            case FeatureLevel.Level_11_1: return new GraphicsApiVersion(11, 1, 0, 0);
            case FeatureLevel.Level_11_0: return new GraphicsApiVersion(11, 0, 0, 0);
            default: return GraphicsApiVersion.Unknown;
        }
    }

    /// <summary>
    /// Executes the is submission fence complete logic for this backend.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool IsSubmissionFenceComplete(ulong value) {
        return this.GetCompletedValueNoAlloc(this._submissionFence) >= value;
    }

    /// <summary>
    /// Gets the completed fence value without going through the managed COM wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe ulong GetCompletedValueNoAlloc(ID3D12Fence fence) {
        void** vtbl = *(void***)fence.NativePointer;
        delegate* unmanaged[Stdcall]<void*, ulong> getCompletedValue =
            (delegate* unmanaged[Stdcall]<void*, ulong>)vtbl[8];
        return getCompletedValue((void*)fence.NativePointer);
    }

    /// <summary>
    /// Executes the wait for submission fence logic for this backend.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    internal void WaitForSubmissionFence(ulong value) {
        if (this.GetCompletedValueNoAlloc(this._submissionFence) >= value) {
            return;
        }

        this._submissionFence
            .SetEventOnCompletion(value, this._submissionFenceEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();
        this._submissionFenceEvent.WaitOne();
    }

    /// <summary>
    /// Waits for the latest user command-list submission, without draining unrelated immediate-copy work.
    /// </summary>
    internal void WaitForLastSubmission() {
        ulong fenceValue = this._lastSubmissionFenceValue;
        if (fenceValue == 0) {
            return;
        }

        this.WaitForSubmissionFence(fenceValue);
        this.PumpSubmissionDeferredDisposals();
    }

    /// <summary>
    /// Executes a short immediate command list using a reusable allocator and command list context.
    /// </summary>
    /// <param name="recordCommands">Records commands into the provided command list.</param>
    /// <param name="waitForCompletion">When true, waits until the submitted work is completed on the GPU.</param>
    /// <returns>The fence value signaled for this immediate submission.</returns>
    internal ulong ExecuteImmediateCommand(Action<ID3D12GraphicsCommandList> recordCommands, bool waitForCompletion = false) {
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        ulong signalValue = 0;

        lock (this._commandQueueLock) {
            this.FlushBatchedImmediateCommandsQueueLocked();

            ImmediateCopyContext context = this.AcquireImmediateCopyContext();
            bool removeContext = false;
            try {
                PrepareImmediateCopyContext(context);

                recordCommands(context.CommandList);
                context.CommandList.Close();
                this.CommandQueue.ExecuteCommandList(context.CommandList);
                signalValue = this._nextImmediateCopyFenceValue++;
                this.SignalQueueFenceNoAlloc(this._immediateCopyFence, signalValue);
                context.FenceValue = signalValue;

                if (waitForCompletion) {
                    long waitStartTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
                    this.WaitForImmediateCopyFence(signalValue);
                    if (_perfLogEnabled) {
                        double waitMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - waitStartTicks);
                        this._perfAccumImmediateWaitMs += waitMs;
                        this._perfMaxImmediateWaitMs = Math.Max(this._perfMaxImmediateWaitMs, waitMs);
                    }
                }

                this.PumpImmediateDeferredDisposals();
            }
            catch {
                removeContext = true;
                throw;
            }
            finally {
                if (removeContext) {
                    this.ReleaseImmediateCopyContext(context, remove: true);
                }
                else {
                    this.ReleaseImmediateCopyContext(context, remove: false);
                }
            }
        }

        if (_perfLogEnabled) {
            double executeMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            this._perfAccumImmediateExecuteMs += executeMs;
            this._perfMaxImmediateExecuteMs = Math.Max(this._perfMaxImmediateExecuteMs, executeMs);
            this._perfAccumImmediateExecutes++;
        }

        return signalValue;
    }

    /// <summary>
    /// Records immediate upload work into a command list that is flushed before the next user submission.
    /// </summary>
    /// <param name="recordCommands">Records commands into the batched immediate command list.</param>
    internal void RecordBatchedImmediateCommand(Action<ID3D12GraphicsCommandList> recordCommands) {
        this.RecordBatchedImmediateCommand(recordCommands, null);
    }

    /// <summary>
    /// Records immediate upload work and keeps an upload buffer alive with the recorded batch.
    /// </summary>
    /// <param name="recordCommands">Records commands into the batched immediate command list.</param>
    /// <param name="uploadBuffer">The upload buffer to retain until the batch completes.</param>
    internal void RecordBatchedImmediateCommand(Action<ID3D12GraphicsCommandList> recordCommands, D3D12ResourceAllocation uploadBuffer) {
        this.RecordBatchedImmediateCommand(recordCommands, uploadBuffer, null);
    }

    /// <summary>
    /// Records immediate upload work and keeps related resources alive with the recorded batch.
    /// </summary>
    /// <param name="recordCommands">Records commands into the batched immediate command list.</param>
    /// <param name="uploadBuffer">The upload buffer to retain until the batch completes.</param>
    /// <param name="retainedResource">An additional disposable resource reference to retain until the batch completes.</param>
    internal void RecordBatchedImmediateCommand(Action<ID3D12GraphicsCommandList> recordCommands, D3D12ResourceAllocation uploadBuffer, IDisposable retainedResource) {
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        lock (this._batchedImmediateCopyLock) {
            ImmediateCopyContext context = this.EnsureBatchedImmediateCopyContextLocked();
            this._immediateBufferUpdateBatcher.FlushLocked(context.CommandList);
            recordCommands(context.CommandList);
            if (uploadBuffer != null) {
                this._batchedImmediateUploadBuffers.Add(uploadBuffer);
            }

            if (retainedResource != null) {
                this._batchedImmediateRetainedResources.Add(retainedResource);
            }

            this._batchedImmediateCopyHasWork = true;
        }

        if (_perfLogEnabled) {
            double recordMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            this._perfAccumImmediateRecordMs += recordMs;
            this._perfMaxImmediateRecordMs = Math.Max(this._perfMaxImmediateRecordMs, recordMs);
            this._perfAccumImmediateRecordCalls++;
        }
    }

    /// <summary>
    /// Keeps an upload buffer alive until the batched immediate upload work has completed.
    /// </summary>
    /// <param name="buffer">The upload buffer to retain.</param>
    internal void EnqueueBatchedImmediateUploadBuffer(D3D12ResourceAllocation buffer) {
        if (buffer == null) {
            return;
        }

        lock (this._batchedImmediateCopyLock) {
            this._batchedImmediateUploadBuffers.Add(buffer);
        }
    }

    /// <summary>
    /// Gets a prepared batched immediate command context while the batched immediate lock is held.
    /// </summary>
    /// <returns>The prepared immediate copy context.</returns>
    private ImmediateCopyContext EnsureBatchedImmediateCopyContextLocked() {
        if (this._batchedImmediateCopyContext == null) {
            this._batchedImmediateCopyContext = this.AcquireImmediateCopyContext();
            PrepareImmediateCopyContext(this._batchedImmediateCopyContext);
        }

        return this._batchedImmediateCopyContext;
    }

    /// <summary>
    /// Records a small immediate buffer update using a shared upload allocation in the batched immediate command list.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="source">The source data pointer.</param>
    /// <param name="bufferOffsetInBytes">The destination byte offset.</param>
    /// <param name="sizeInBytes">The number of bytes to copy.</param>
    private unsafe void RecordBatchedImmediateBufferUpdate(D3D12DeviceBuffer buffer, IntPtr source, uint bufferOffsetInBytes, uint sizeInBytes) {
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        lock (this._batchedImmediateCopyLock) {
            ImmediateCopyContext context = this.EnsureBatchedImmediateCopyContextLocked();
            D3D12ResourceAllocation uploadToRetain = this._immediateBufferUpdateBatcher.QueueLocked(context.CommandList, buffer, source, bufferOffsetInBytes, sizeInBytes);
            if (uploadToRetain != null) {
                this._batchedImmediateUploadBuffers.Add(uploadToRetain);
            }

            this._batchedImmediateCopyHasWork = true;
        }

        if (_perfLogEnabled) {
            double recordMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            this._perfAccumImmediateRecordMs += recordMs;
            this._perfMaxImmediateRecordMs = Math.Max(this._perfMaxImmediateRecordMs, recordMs);
            this._perfAccumImmediateRecordCalls++;
        }
    }

    /// <summary>
    /// Submits any batched immediate upload work recorded since the last flush.
    /// </summary>
    /// <param name="waitForCompletion">When true, waits until the submitted upload work has completed.</param>
    /// <returns>The signaled fence value, or zero when there was no batched work.</returns>
    internal ulong FlushBatchedImmediateCommands(bool waitForCompletion = false) {
        if (!Volatile.Read(ref this._batchedImmediateCopyHasWork)) {
            return 0;
        }

        lock (this._commandQueueLock) {
            return this.FlushBatchedImmediateCommandsQueueLocked(waitForCompletion);
        }
    }

    /// <summary>
    /// Submits pending batched immediate upload work while the queue lock is already held.
    /// </summary>
    /// <param name="waitForCompletion">When true, waits until the submitted upload work has completed.</param>
    /// <returns>The signaled fence value, or zero when there was no batched work.</returns>
    private ulong FlushBatchedImmediateCommandsQueueLocked(bool waitForCompletion = false) {
        if (!Volatile.Read(ref this._batchedImmediateCopyHasWork)) {
            return 0;
        }

        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        ImmediateCopyContext context;
        ulong signalValue;
        int uploadBufferCount;

        lock (this._batchedImmediateCopyLock) {
            if (!this._batchedImmediateCopyHasWork || this._batchedImmediateCopyContext == null) {
                return 0;
            }

            context = this._batchedImmediateCopyContext;
            this._batchedImmediateCopyContext = null;
            this._batchedImmediateCopyHasWork = false;

            this._immediateBufferUpdateBatcher.FlushLocked(context.CommandList);
            context.CommandList.Close();
            this.CommandQueue.ExecuteCommandList(context.CommandList);
            signalValue = this._nextImmediateCopyFenceValue++;
            this.SignalQueueFenceNoAlloc(this._immediateCopyFence, signalValue);
            context.FenceValue = signalValue;

            uploadBufferCount = this._batchedImmediateUploadBuffers.Count;
            this.EnqueueImmediateUploadBuffers(this._batchedImmediateUploadBuffers, signalValue);
            this.EnqueueImmediateDisposals(this._batchedImmediateRetainedResources, signalValue);

            this._batchedImmediateUploadBuffers.Clear();
            this._batchedImmediateRetainedResources.Clear();
            this._immediateBufferUpdateBatcher.ResetAfterFlush();
        }

        if (waitForCompletion) {
            long waitStartTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
            this.WaitForImmediateCopyFence(signalValue);
            if (_perfLogEnabled) {
                double waitMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - waitStartTicks);
                this._perfAccumImmediateWaitMs += waitMs;
                this._perfMaxImmediateWaitMs = Math.Max(this._perfMaxImmediateWaitMs, waitMs);
            }
        }

        this.ReleaseImmediateCopyContext(context, remove: false);
        this.PumpImmediateDeferredDisposals();
        if (_perfLogEnabled) {
            double flushMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
            this._perfAccumImmediateFlushMs += flushMs;
            this._perfMaxImmediateFlushMs = Math.Max(this._perfMaxImmediateFlushMs, flushMs);
            this._perfAccumImmediateFlushes++;
            this._perfAccumImmediateUploadBuffers += (ulong)uploadBufferCount;
        }

        return signalValue;
    }

    /// <summary>
    /// Enqueues a disposable resource to be released after the specified submission fence value has completed.
    /// </summary>
    /// <param name="disposable">The resource to dispose.</param>
    /// <param name="fenceValue">The submission fence value that guards the resource lifetime.</param>
    internal void EnqueueSubmissionDisposal(IDisposable disposable, ulong fenceValue) {
        if (disposable == null) {
            return;
        }

        lock (this._submissionDeferredDisposalsLock) {
            if (this._submissionDeferredDisposals.Count == 0) {
                Volatile.Write(ref this._submissionDeferredDisposalNextFenceValue, fenceValue);
            }

            this._submissionDeferredDisposals.Enqueue(new DeferredDisposal(disposable, fenceValue));
        }

        Interlocked.Increment(ref this._submissionDeferredDisposalCount);
    }

    /// <summary>
    /// Enqueues disposable resources to be released after the specified submission fence value has completed.
    /// </summary>
    /// <param name="disposables">The resources to dispose.</param>
    /// <param name="fenceValue">The submission fence value that guards the resource lifetime.</param>
    internal void EnqueueSubmissionDisposals(List<IDisposable> disposables, ulong fenceValue) {
        this.EnqueueDeferredDisposals(this._submissionDeferredDisposalsLock, this._submissionDeferredDisposals, ref this._submissionDeferredDisposalCount, ref this._submissionDeferredDisposalNextFenceValue, disposables, fenceValue);
    }

    /// <summary>
    /// Releases a resource after all currently submitted user command lists have completed.
    /// </summary>
    /// <param name="disposable">The resource to release.</param>
    internal void ReleaseAfterLastSubmission(IDisposable disposable) {
        if (disposable == null) {
            return;
        }

        ulong fenceValue = this._lastSubmissionFenceValue;
        if (fenceValue == 0 || this.GetCompletedValueNoAlloc(this._submissionFence) >= fenceValue) {
            disposable.Dispose();
            return;
        }

        this.EnqueueSubmissionDisposal(disposable, fenceValue);
    }

    /// <summary>
    /// Enqueues a disposable resource to be released after the specified immediate fence value has completed.
    /// </summary>
    /// <param name="disposable">The resource to dispose.</param>
    /// <param name="fenceValue">The immediate fence value that guards the resource lifetime.</param>
    internal void EnqueueImmediateDisposal(IDisposable disposable, ulong fenceValue) {
        if (disposable == null) {
            return;
        }

        lock (this._immediateDeferredDisposalsLock) {
            if (this._immediateDeferredDisposals.Count == 0) {
                Volatile.Write(ref this._immediateDeferredDisposalNextFenceValue, fenceValue);
            }

            this._immediateDeferredDisposals.Enqueue(new DeferredDisposal(disposable, fenceValue));
        }

        Interlocked.Increment(ref this._immediateDeferredDisposalCount);
    }

    /// <summary>
    /// Enqueues disposable resources to be released after the specified immediate fence value has completed.
    /// </summary>
    /// <param name="disposables">The resources to dispose.</param>
    /// <param name="fenceValue">The immediate fence value that guards the resource lifetime.</param>
    internal void EnqueueImmediateDisposals(List<IDisposable> disposables, ulong fenceValue) {
        this.EnqueueDeferredDisposals(this._immediateDeferredDisposalsLock, this._immediateDeferredDisposals, ref this._immediateDeferredDisposalCount, ref this._immediateDeferredDisposalNextFenceValue, disposables, fenceValue);
    }

    /// <summary>
    /// Rents an upload buffer that is at least the requested size.
    /// </summary>
    /// <param name="sizeInBytes">The required upload-buffer size in bytes.</param>
    /// <returns>An upload heap resource ready for CPU writes.</returns>
    internal D3D12ResourceAllocation RentUploadBuffer(ulong sizeInBytes) {
        return this._uploadBufferPool.Rent(sizeInBytes);
    }

    /// <summary>
    /// Rents an upload buffer with an offset aligned for the target D3D12 copy operation.
    /// </summary>
    /// <param name="sizeInBytes">The required upload-buffer size in bytes.</param>
    /// <param name="alignment">The required byte alignment for the returned allocation offset.</param>
    /// <returns>An upload heap resource ready for CPU writes.</returns>
    internal D3D12ResourceAllocation RentUploadBuffer(ulong sizeInBytes, ulong alignment) {
        return this._uploadBufferPool.Rent(sizeInBytes, alignment);
    }

    /// <summary>
    /// Returns an upload buffer to the reusable pool or disposes it when it is too large.
    /// </summary>
    /// <param name="buffer">The upload buffer to return.</param>
    internal void ReturnUploadBuffer(D3D12ResourceAllocation buffer) {
        this._uploadBufferPool.Return(buffer);
    }

    /// <summary>
    /// Returns an upload buffer to the pool after a submission fence has completed.
    /// </summary>
    /// <param name="buffer">The upload buffer to return.</param>
    /// <param name="fenceValue">The submission fence value guarding the buffer.</param>
    internal void EnqueueSubmissionUploadBuffer(D3D12ResourceAllocation buffer, ulong fenceValue) {
        this.EnqueueSubmissionDisposal(new PooledUploadBuffer(this, buffer), fenceValue);
    }

    /// <summary>
    /// Returns upload buffers to the pool after a submission fence has completed.
    /// </summary>
    /// <param name="buffers">The upload buffers to return.</param>
    /// <param name="fenceValue">The submission fence value guarding the buffers.</param>
    internal void EnqueueSubmissionUploadBuffers(List<D3D12ResourceAllocation> buffers, ulong fenceValue) {
        this.EnqueueDeferredUploadBuffers(this._submissionDeferredDisposalsLock, this._submissionDeferredDisposals, ref this._submissionDeferredDisposalCount, ref this._submissionDeferredDisposalNextFenceValue, buffers, fenceValue);
    }

    /// <summary>
    /// Returns an upload buffer to the pool after an immediate-copy fence has completed.
    /// </summary>
    /// <param name="buffer">The upload buffer to return.</param>
    /// <param name="fenceValue">The immediate-copy fence value guarding the buffer.</param>
    internal void EnqueueImmediateUploadBuffer(D3D12ResourceAllocation buffer, ulong fenceValue) {
        this.EnqueueImmediateDisposal(new PooledUploadBuffer(this, buffer), fenceValue);
    }

    /// <summary>
    /// Returns upload buffers to the pool after an immediate-copy fence has completed.
    /// </summary>
    /// <param name="buffers">The upload buffers to return.</param>
    /// <param name="fenceValue">The immediate-copy fence value guarding the buffers.</param>
    internal void EnqueueImmediateUploadBuffers(List<D3D12ResourceAllocation> buffers, ulong fenceValue) {
        this.EnqueueDeferredUploadBuffers(this._immediateDeferredDisposalsLock, this._immediateDeferredDisposals, ref this._immediateDeferredDisposalCount, ref this._immediateDeferredDisposalNextFenceValue, buffers, fenceValue);
    }

    /// <summary>
    /// Gets the or create root signature value.
    /// </summary>
    /// <param name="cacheKey">The cache key value used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal ID3D12RootSignature GetOrCreateRootSignature(string cacheKey, in RootSignatureDescription description) {
        lock (this._rootSignatureCacheLock) {
            if (this._rootSignatureCache.TryGetValue(cacheKey, out ID3D12RootSignature cached)) {
                return cached;
            }

            ID3D12RootSignature created = this._device.CreateRootSignature(in description, RootSignatureVersion.Version1);
            this._rootSignatureCache.Add(cacheKey, created);
            return created;
        }
    }

    /// <summary>
    /// Gets a cached D3D12 pipeline state or creates and stores one when it is first requested.
    /// </summary>
    /// <param name="cacheKey">The stable pipeline-state key.</param>
    /// <param name="createPipelineState">Creates the native pipeline state on a cache miss.</param>
    /// <returns>The cached or newly-created pipeline state.</returns>
    internal ID3D12PipelineState GetOrCreatePipelineState(string cacheKey, Func<ID3D12PipelineState> createPipelineState) {
        lock (this._pipelineStateCacheLock) {
            if (this._pipelineStateCache.TryGetValue(cacheKey, out ID3D12PipelineState cached)) {
                return cached;
            }

            ID3D12PipelineState created = createPipelineState();
            this._pipelineStateCache.Add(cacheKey, created);
            return created;
        }
    }

    /// <summary>
    /// Gets a cached D3D12 graphics pipeline state or creates and stores one when it is first requested.
    /// </summary>
    /// <param name="cacheKey">The stable pipeline-state key.</param>
    /// <param name="description">The D3D12 graphics pipeline description.</param>
    /// <returns>The cached or newly-created pipeline state.</returns>
    internal ID3D12PipelineState GetOrCreateGraphicsPipelineState(string cacheKey, in GraphicsPipelineStateDescription description) {
        lock (this._pipelineStateCacheLock) {
            if (this._pipelineStateCache.TryGetValue(cacheKey, out ID3D12PipelineState cached)) {
                return cached;
            }

            string libraryName = BuildPipelineLibraryEntryName(cacheKey);
            ID3D12PipelineState created = this.TryLoadGraphicsPipelineStateFromLibrary(libraryName, in description)
                                          ?? this._device.CreateGraphicsPipelineState(description);
            this.TryStorePipelineStateInLibrary(libraryName, created);
            this._pipelineStateCache.Add(cacheKey, created);
            return created;
        }
    }

    /// <summary>
    /// Gets a cached D3D12 compute pipeline state or creates and stores one when it is first requested.
    /// </summary>
    /// <param name="cacheKey">The stable pipeline-state key.</param>
    /// <param name="description">The D3D12 compute pipeline description.</param>
    /// <returns>The cached or newly-created pipeline state.</returns>
    internal ID3D12PipelineState GetOrCreateComputePipelineState(string cacheKey, in ComputePipelineStateDescription description) {
        lock (this._pipelineStateCacheLock) {
            if (this._pipelineStateCache.TryGetValue(cacheKey, out ID3D12PipelineState cached)) {
                return cached;
            }

            string libraryName = BuildPipelineLibraryEntryName(cacheKey);
            ID3D12PipelineState created = this.TryLoadComputePipelineStateFromLibrary(libraryName, in description)
                                          ?? this._device.CreateComputePipelineState(description);
            this.TryStorePipelineStateInLibrary(libraryName, created);
            this._pipelineStateCache.Add(cacheKey, created);
            return created;
        }
    }

    /// <summary>
    /// Initializes the optional persistent pipeline library for this device.
    /// </summary>
    private unsafe void InitializePipelineLibrary() {
        if (!_persistentPipelineLibraryEnabled) {
            this._pipelineLibraryUnavailable = true;
            return;
        }

        ID3D12Device1 device1 = null;
        try {
            device1 = this._device.QueryInterface<ID3D12Device1>();
            byte[] blob = this.ReadPipelineLibraryBlob();
            this._pipelineLibraryPath = this.GetPipelineLibraryPath();
            if (blob.Length != 0) {
                this._pipelineLibraryBlob = blob;
                this._pipelineLibraryBlobHandle = GCHandle.Alloc(this._pipelineLibraryBlob, GCHandleType.Pinned);
            }

            Span<byte> libraryBlob = blob.Length == 0
                ? Span<byte>.Empty
                : this._pipelineLibraryBlob.AsSpan();
            device1.CreatePipelineLibrary(libraryBlob, out this._pipelineLibrary);
            this._pipelineLibraryDevice = device1;
            device1 = null;
        }
        catch {
            this._pipelineLibraryUnavailable = true;
            this._pipelineLibrary?.Dispose();
            this._pipelineLibrary = null;
            this.ReleasePipelineLibraryBlob();
        }
        finally {
            device1?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to load a graphics PSO from the persistent pipeline library.
    /// </summary>
    /// <param name="libraryName">The library entry name.</param>
    /// <param name="description">The graphics pipeline description.</param>
    /// <returns>The loaded pipeline state, or null when the library cannot satisfy the request.</returns>
    private ID3D12PipelineState TryLoadGraphicsPipelineStateFromLibrary(string libraryName, in GraphicsPipelineStateDescription description) {
        if (this._pipelineLibrary == null || this._pipelineLibraryUnavailable) {
            return null;
        }

        try {
            return this._pipelineLibrary.LoadGraphicsPipeline(libraryName, description);
        }
        catch {
            return null;
        }
    }

    /// <summary>
    /// Attempts to load a compute PSO from the persistent pipeline library.
    /// </summary>
    /// <param name="libraryName">The library entry name.</param>
    /// <param name="description">The compute pipeline description.</param>
    /// <returns>The loaded pipeline state, or null when the library cannot satisfy the request.</returns>
    private ID3D12PipelineState TryLoadComputePipelineStateFromLibrary(string libraryName, in ComputePipelineStateDescription description) {
        if (this._pipelineLibrary == null || this._pipelineLibraryUnavailable) {
            return null;
        }

        try {
            return this._pipelineLibrary.LoadComputePipeline(libraryName, description);
        }
        catch {
            return null;
        }
    }

    /// <summary>
    /// Stores a newly created PSO in the persistent pipeline library.
    /// </summary>
    /// <param name="libraryName">The library entry name.</param>
    /// <param name="pipelineState">The pipeline state to store.</param>
    private void TryStorePipelineStateInLibrary(string libraryName, ID3D12PipelineState pipelineState) {
        if (this._pipelineLibrary == null || this._pipelineLibraryUnavailable || pipelineState == null) {
            return;
        }

        try {
            this._pipelineLibrary.StorePipeline(libraryName, pipelineState);
            this._pipelineLibraryDirty = true;
        }
        catch {
            // Duplicate names and unsupported libraries are non-fatal. The in-memory PSO cache remains authoritative.
        }
    }

    /// <summary>
    /// Serializes the persistent pipeline library to disk if it changed.
    /// </summary>
    private unsafe void StoreAndDestroyPipelineLibrary() {
        ID3D12PipelineLibrary library = this._pipelineLibrary;
        this._pipelineLibrary = null;
        try {
            if (library != null && this._pipelineLibraryDirty && !this._pipelineLibraryUnavailable && !string.IsNullOrEmpty(this._pipelineLibraryPath)) {
                nuint serializedSize = (nuint)library.SerializedSize;
                if (serializedSize != 0 && serializedSize <= int.MaxValue) {
                    byte[] blob = new byte[(int)serializedSize];
                    fixed (byte* blobPointer = blob) {
                        library.Serialize((IntPtr)blobPointer, serializedSize);
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(this._pipelineLibraryPath)!);
                    File.WriteAllBytes(this._pipelineLibraryPath, blob);
                }
            }
        }
        catch {
            // Pipeline library cache persistence must never make device disposal fail.
        }
        finally {
            library?.Dispose();
            this._pipelineLibraryDevice?.Dispose();
            this._pipelineLibraryDevice = null;
            this.ReleasePipelineLibraryBlob();
        }
    }

    /// <summary>
    /// Reads the persistent pipeline library blob from disk.
    /// </summary>
    /// <returns>The loaded blob, or an empty array when none is available.</returns>
    private byte[] ReadPipelineLibraryBlob() {
        string path = this.GetPipelineLibraryPath();
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
            return Array.Empty<byte>();
        }

        try {
            return File.ReadAllBytes(path);
        }
        catch {
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Gets the persistent pipeline library file path for this device.
    /// </summary>
    /// <returns>The cache path.</returns>
    private string GetPipelineLibraryPath() {
        string deviceKey = $"{this.DeviceName}|{this.VendorName}|{this.ApiVersion}";
        return Path.Combine(_persistentPipelineLibraryDirectory, $"{ComputeStableFileHash(deviceKey)}.bin");
    }

    /// <summary>
    /// Creates a stable short file-name-safe hash for cache data.
    /// </summary>
    /// <param name="text">The text to hash.</param>
    /// <returns>The stable hash.</returns>
    private static string ComputeStableFileHash(string text) {
        byte[] bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash, 0, 16);
    }

    /// <summary>
    /// Builds a short stable name for a pipeline library entry.
    /// </summary>
    /// <param name="cacheKey">The full pipeline cache key.</param>
    /// <returns>The library entry name.</returns>
    private static string BuildPipelineLibraryEntryName(string cacheKey) {
        return ComputeStableFileHash(cacheKey);
    }

    /// <summary>
    /// Releases the pinned pipeline library input blob.
    /// </summary>
    private void ReleasePipelineLibraryBlob() {
        if (this._pipelineLibraryBlobHandle.IsAllocated) {
            this._pipelineLibraryBlobHandle.Free();
        }

        this._pipelineLibraryBlob = null;
    }

    /// <summary>
    /// Gets a detailed device-removed diagnostic string for Direct3D 12.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal string GetDeviceRemovedReasonDescription() {
        if (this._device == null) {
            return "Device unavailable.";
        }

        Result reason = this._device.DeviceRemovedReason;
        if (reason.Success) {
            return "S_OK";
        }

        StringBuilder sb = new(96);
        sb.Append("HRESULT=0x");
        sb.Append(reason.Code.ToString("X8"));

        string text = reason.Description;
        if (!string.IsNullOrWhiteSpace(text)) {
            sb.Append(" (");
            sb.Append(text.Trim());
            sb.Append(')');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Executes the wait for fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool WaitForFence(Fence fence, ulong nanosecondTimeout) {
        D3D12Fence d3d12Fence = Util.AssertSubtype<Fence, D3D12Fence>(fence);
        return d3d12Fence.Wait(nanosecondTimeout);
    }

    /// <summary>
    /// Executes the wait for fences logic for this backend.
    /// </summary>
    /// <param name="fences">The synchronization fence used by this operation.</param>
    /// <param name="waitAll">The wait all value used by this operation.</param>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout) {
        if (fences == null || fences.Length == 0) {
            return true;
        }

        if (waitAll) {
            foreach (Fence fence in fences) {
                if (!this.WaitForFence(fence, nanosecondTimeout)) {
                    return false;
                }
            }

            return true;
        }

        if (nanosecondTimeout == ulong.MaxValue) {
            while (true) {
                foreach (Fence fence in fences) {
                    if (fence.Signaled) {
                        return true;
                    }
                }

                Thread.Sleep(0);
            }
        }

        DateTime deadline = DateTime.UtcNow + TimeSpan.FromTicks((long)(nanosecondTimeout / 100));
        while (DateTime.UtcNow <= deadline) {
            foreach (Fence fence in fences) {
                if (fence.Signaled) {
                    return true;
                }
            }

            Thread.Sleep(0);
        }

        return false;
    }

    /// <summary>
    /// Executes the reset fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    public override void ResetFence(Fence fence) {
        fence.Reset();
    }

    /// <summary>
    /// Gets the sample count limit value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="depthFormat">The depth format value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat) {
        Format dxgiFormat;
        try {
            dxgiFormat = depthFormat
                ? D3D12Formats.ToDepthFormat(format)
                : D3D12Formats.ToDxgiFormat(format);
        }
        catch (VeldridException) {
            return TextureSampleCount.Count1;
        }

        if (!this.TryGetFormatSupport(dxgiFormat, out FeatureDataFormatSupport formatSupport)) {
            return TextureSampleCount.Count1;
        }

        FormatSupport1 support1 = formatSupport.Support1;
        if (depthFormat) {
            if ((support1 & FormatSupport1.DepthStencil) == 0) {
                return TextureSampleCount.Count1;
            }
        }
        else if ((support1 & (FormatSupport1.RenderTarget | FormatSupport1.MultisampleRendertarget)) == 0) {
            return TextureSampleCount.Count1;
        }

        uint sampleMask = this.GetSupportedSampleFlags(dxgiFormat);
        if ((sampleMask & (1u << (int)TextureSampleCount.Count32)) != 0) {
            return TextureSampleCount.Count32;
        }

        if ((sampleMask & (1u << (int)TextureSampleCount.Count16)) != 0) {
            return TextureSampleCount.Count16;
        }

        if ((sampleMask & (1u << (int)TextureSampleCount.Count8)) != 0) {
            return TextureSampleCount.Count8;
        }

        if ((sampleMask & (1u << (int)TextureSampleCount.Count4)) != 0) {
            return TextureSampleCount.Count4;
        }

        if ((sampleMask & (1u << (int)TextureSampleCount.Count2)) != 0) {
            return TextureSampleCount.Count2;
        }

        return TextureSampleCount.Count1;
    }

    /// <summary>
    /// Gets the uniform buffer min offset alignment core value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal override uint GetUniformBufferMinOffsetAlignmentCore() {
        return 256;
    }

    /// <summary>
    /// Gets the structured buffer min offset alignment core value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal override uint GetStructuredBufferMinOffsetAlignmentCore() {
        return 16;
    }

    /// <summary>
    /// Maps the core resource for CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource) {
        if (resource is D3D12DeviceBuffer buffer) {
            return buffer.Map(mode);
        }

        if (resource is D3D12Texture texture) {
            return texture.Map(mode, subresource);
        }

        throw new VeldridException("Resource belongs to a different backend.");
    }

    /// <summary>
    /// Unmaps the core resource from CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    protected override void UnmapCore(IMappableResource resource, uint subresource) {
        if (resource is D3D12DeviceBuffer buffer) {
            buffer.Unmap();
            return;
        }

        if (resource is D3D12Texture texture) {
            texture.Unmap();
            return;
        }

        throw new VeldridException("Resource belongs to a different backend.");
    }

    /// <summary>
    /// Executes the platform dispose logic for this backend.
    /// </summary>
    protected override void PlatformDispose() {
        this.WaitForQueueIdle();
        this.PumpSubmissionDeferredDisposals();
        this.PumpImmediateDeferredDisposals();
        this.DisposeAllDeferredDisposals();
        this._uploadBufferPool?.Dispose();

        lock (this._immediateCopyContextsLock) {
            foreach (ImmediateCopyContext context in this._immediateCopyContexts) {
                context.CommandList?.Dispose();
                context.Allocator?.Dispose();
            }

            this._immediateCopyContexts.Clear();
        }

        lock (this._rootSignatureCacheLock) {
            foreach (ID3D12RootSignature rootSignature in this._rootSignatureCache.Values) {
                rootSignature?.Dispose();
            }

            this._rootSignatureCache.Clear();
        }

        this.StoreAndDestroyPipelineLibrary();

        lock (this._pipelineStateCacheLock) {
            foreach (ID3D12PipelineState pipelineState in this._pipelineStateCache.Values) {
                pipelineState?.Dispose();
            }

            this._pipelineStateCache.Clear();
        }

        this.MainSwapchain?.Dispose();
        this._descriptorHeapState?.Dispose();
        this.SrvUavDescriptorAllocator?.Dispose();
        this.SamplerDescriptorAllocator?.Dispose();
        this.RtvDescriptorAllocator?.Dispose();
        this.DsvDescriptorAllocator?.Dispose();
        this.MemoryManager?.Dispose();
        this._submissionFenceEvent?.Dispose();
        this._submissionFence?.Dispose();
        this._immediateCopyFenceEvent?.Dispose();
        this._immediateCopyFence?.Dispose();
        this.CommandQueue?.Dispose();
        this._device?.Dispose();
        this.DxgiFactory?.Dispose();
    }

    /// <summary>
    /// Executes the submit commands core logic for this backend.
    /// </summary>
    /// <param name="commandList">The command list used by this operation.</param>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    private protected override void SubmitCommandsCore(CommandList commandList, Fence fence) {
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;

        long pumpStartTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        bool pumpedDeferredDisposals = this.TryPumpDeferredDisposalsForSubmit();
        if (_perfLogEnabled) {
            double pumpMs = pumpedDeferredDisposals ? TicksToMilliseconds(Stopwatch.GetTimestamp() - pumpStartTicks) : 0;
            if (pumpedDeferredDisposals) {
                this._perfAccumSubmitDisposalPumpMs += pumpMs;
                this._perfMaxSubmitDisposalPumpMs = Math.Max(this._perfMaxSubmitDisposalPumpMs, pumpMs);
            }
        }

        long lockWaitStartTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        lock (this._commandQueueLock) {
            if (_perfLogEnabled) {
                double lockWaitMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - lockWaitStartTicks);
                this._perfAccumSubmitLockWaitMs += lockWaitMs;
                this._perfMaxSubmitLockWaitMs = Math.Max(this._perfMaxSubmitLockWaitMs, lockWaitMs);
            }

            this.FlushBatchedImmediateCommandsQueueLocked();

            try {
                if (commandList is D3D12CommandList d3d12CommandList) {
                    long executeStartTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
                    d3d12CommandList.ExecuteNoSignal();
                    if (_perfLogEnabled) {
                        double executeMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - executeStartTicks);
                        this._perfAccumSubmitExecuteMs += executeMs;
                        this._perfMaxSubmitExecuteMs = Math.Max(this._perfMaxSubmitExecuteMs, executeMs);
                    }

                    long signalStartTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
                    ulong signalValue = this._nextSubmissionFenceValue++;
                    this.SignalQueueFenceNoAlloc(this._submissionFence, signalValue);
                    this._lastSubmissionFenceValue = signalValue;
                    if (_perfLogEnabled) {
                        double signalMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - signalStartTicks);
                        this._perfAccumSubmitSignalMs += signalMs;
                        this._perfMaxSubmitSignalMs = Math.Max(this._perfMaxSubmitSignalMs, signalMs);
                    }

                    long markStartTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
                    d3d12CommandList.MarkSubmitted(signalValue);
                    d3d12CommandList.ClearCachedState();
                    if (_perfLogEnabled) {
                        double markMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - markStartTicks);
                        this._perfAccumSubmitMarkMs += markMs;
                        this._perfMaxSubmitMarkMs = Math.Max(this._perfMaxSubmitMarkMs, markMs);
                    }
                }

                if (fence is D3D12Fence d3d12Fence) {
                    d3d12Fence.Signal(this.CommandQueue);
                }
            }
            catch (SharpGenException ex) {
                string reason = this.GetDeviceRemovedReasonDescription();
                throw new VeldridException($"D3D12 command submission failed. DeviceRemovedReason={reason}.", ex);
            }
        }

        if (_perfLogEnabled) {
            this.ReportDevicePerfIfNeeded(TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks));
        }
    }

    /// <summary>
    /// Pumps deferred-disposal queues from the submit hot path only when enough work has accumulated.
    /// </summary>
    /// <returns><see langword="true" /> when a fence poll was performed.</returns>
    private bool TryPumpDeferredDisposalsForSubmit() {
        int pendingSubmissionDisposals = Volatile.Read(ref this._submissionDeferredDisposalCount);
        int pendingImmediateDisposals = Volatile.Read(ref this._immediateDeferredDisposalCount);
        if (pendingSubmissionDisposals == 0 && pendingImmediateDisposals == 0) {
            this._deferredDisposalPumpSubmitCounter = 0;
            return false;
        }

        int submitCounter = this._deferredDisposalPumpSubmitCounter + 1;
        bool highWatermark = pendingSubmissionDisposals >= DeferredDisposalPumpHighWatermark
                             || pendingImmediateDisposals >= DeferredDisposalPumpHighWatermark;
        if (!highWatermark && submitCounter < _deferredDisposalPumpInterval) {
            this._deferredDisposalPumpSubmitCounter = submitCounter;
            return false;
        }

        this._deferredDisposalPumpSubmitCounter = 0;
        this.PumpSubmissionDeferredDisposals();
        this.PumpImmediateDeferredDisposals();
        return true;
    }

    /// <summary>
    /// Executes the swap buffers core logic for this backend.
    /// </summary>
    /// <param name="swapchain">The swapchain used by this operation.</param>
    private protected override void SwapBuffersCore(Swapchain swapchain) {
        if (swapchain is D3D12Swapchain d3d12Swapchain) {
            long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
            d3d12Swapchain.Present();
            if (_perfLogEnabled) {
                double presentMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
                this._perfAccumPresentMs += presentMs;
                this._perfMaxPresentMs = Math.Max(this._perfMaxPresentMs, presentMs);
            }

            return;
        }

        throw new VeldridException("Swapchain belongs to a different backend.");
    }

    /// <summary>
    /// Executes the wait for idle core logic for this backend.
    /// </summary>
    private protected override void WaitForIdleCore() {
        this.WaitForQueueIdle();
        this.PumpSubmissionDeferredDisposals();
        this.PumpImmediateDeferredDisposals();
    }

    /// <summary>
    /// Executes the wait for next frame ready core logic for this backend.
    /// </summary>
    private protected override void WaitForNextFrameReadyCore() {
        if (!D3D12Swapchain.FrameLatencyWaitsEnabled) {
            return;
        }

        if (this.MainSwapchain is D3D12Swapchain swapchain) {
            long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
            swapchain.WaitForNextFrameReady();
            if (_perfLogEnabled) {
                double waitMs = TicksToMilliseconds(Stopwatch.GetTimestamp() - startTicks);
                this._perfAccumFrameWaitMs += waitMs;
                this._perfMaxFrameWaitMs = Math.Max(this._perfMaxFrameWaitMs, waitMs);
            }
        }
    }

    /// <summary>
    /// Updates the texture core state for this command sequence.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    private protected override void UpdateTextureCore(Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
        if (texture is not D3D12Texture d3d12Texture) {
            throw new VeldridException("Texture belongs to a different backend.");
        }

        if (d3d12Texture.NativeTexture != null) {
            // Use the validated staging->native upload path in D3D12Texture to avoid
            // partial CopyTextureRegion edge-cases that can trigger device removal.
            d3d12Texture.UpdateNativeSubresource(source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
            return;
        }

        d3d12Texture.Update(source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    /// <summary>
    /// Updates the buffer core state for this command sequence.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        if (buffer is not D3D12DeviceBuffer d3d12Buffer) {
            throw new VeldridException("Buffer belongs to a different backend.");
        }

        if (sizeInBytes == 0) {
            return;
        }

        if (!d3d12Buffer.CanTransitionState) {
            d3d12Buffer.Update(null, source, bufferOffsetInBytes, sizeInBytes);
            return;
        }

        if (_immediateBufferUpdateBatcherEnabled && this._immediateBufferUpdateBatcher.ShouldBatch(sizeInBytes)) {
            this.RecordBatchedImmediateBufferUpdate(d3d12Buffer, source, bufferOffsetInBytes, sizeInBytes);
            return;
        }

        D3D12ResourceAllocation temporaryUpload = null;
        try {
            this.RecordBatchedImmediateCommand(commandList => {
                temporaryUpload = d3d12Buffer.Update(commandList, source, bufferOffsetInBytes, sizeInBytes);
            });

            if (temporaryUpload != null) {
                this.EnqueueBatchedImmediateUploadBuffer(temporaryUpload);
                temporaryUpload = null;
            }
        }
        finally {
            if (temporaryUpload != null) {
                this.ReturnUploadBuffer(temporaryUpload);
            }
        }
    }

    /// <summary>
    /// Gets the pixel format support core value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="properties">The properties value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private protected override bool GetPixelFormatSupportCore(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties) {
        if ((usage & TextureUsage.Cubemap) != 0 && type != TextureType.Texture2D) {
            properties = default;
            return false;
        }

        if ((usage & TextureUsage.DepthStencil) != 0 && (usage & (TextureUsage.RenderTarget | TextureUsage.Storage)) != 0) {
            properties = default;
            return false;
        }

        if ((usage & TextureUsage.DepthStencil) != 0 && type == TextureType.Texture3D) {
            properties = default;
            return false;
        }

        bool depthUsage = (usage & TextureUsage.DepthStencil) != 0;
        Format resourceFormat;
        Format depthStencilFormat = Format.Unknown;
        Format sampledViewFormat = Format.Unknown;
        try {
            if (depthUsage) {
                resourceFormat = D3D12Formats.ToDxgiFormat(format, true);
                depthStencilFormat = D3D12Formats.ToDepthFormat(format);
                sampledViewFormat = D3D12Formats.GetViewFormat(resourceFormat);
            }
            else {
                resourceFormat = D3D12Formats.ToDxgiFormat(format);
                sampledViewFormat = resourceFormat;
            }
        }
        catch (VeldridException) {
            properties = default;
            return false;
        }

        if (!this.TryGetFormatSupport(resourceFormat, out FeatureDataFormatSupport formatSupport)) {
            properties = default;
            return false;
        }

        FormatSupport1 support1 = formatSupport.Support1;
        FormatSupport2 support2 = formatSupport.Support2;
        if (!IsTypeSupported(type, support1)) {
            properties = default;
            return false;
        }

        if ((usage & TextureUsage.Cubemap) != 0 && (support1 & FormatSupport1.TextureCube) == 0) {
            properties = default;
            return false;
        }

        if ((usage & TextureUsage.Sampled) != 0) {
            if (depthUsage) {
                if (!this.TryGetFormatSupport(sampledViewFormat, out FeatureDataFormatSupport sampledSupport)) {
                    properties = default;
                    return false;
                }

                FormatSupport1 sampledSupport1 = sampledSupport.Support1;
                if ((sampledSupport1 & (FormatSupport1.ShaderSample | FormatSupport1.ShaderSampleComparison)) == 0) {
                    properties = default;
                    return false;
                }
            }
            else if ((support1 & FormatSupport1.ShaderSample) == 0) {
                properties = default;
                return false;
            }
        }

        if ((usage & TextureUsage.RenderTarget) != 0 && (support1 & FormatSupport1.RenderTarget) == 0) {
            properties = default;
            return false;
        }

        if (depthUsage && (support1 & FormatSupport1.DepthStencil) == 0) {
            if (!this.TryGetFormatSupport(depthStencilFormat, out FeatureDataFormatSupport depthSupport)
                || (depthSupport.Support1 & FormatSupport1.DepthStencil) == 0) {
                properties = default;
                return false;
            }
        }

        if ((usage & TextureUsage.Storage) != 0
            && (support1 & FormatSupport1.TypedUnorderedAccessView) == 0) {
            properties = default;
            return false;
        }

        if ((usage & TextureUsage.Storage) != 0 && (FormatHelpers.IsCompressedFormat(format) || IsSrgbFormat(format))) {
            properties = default;
            return false;
        }

        if ((usage & TextureUsage.Storage) != 0
            && ((support2 & FormatSupport2.UnorderedAccessViewTypedLoad) == 0
                || (support2 & FormatSupport2.UnorderedAccessViewTypedStore) == 0)) {
            properties = default;
            return false;
        }

        if ((usage & TextureUsage.GenerateMipmaps) != 0 && (support1 & FormatSupport1.Mip) == 0) {
            properties = default;
            return false;
        }

        if ((usage & TextureUsage.GenerateMipmaps) != 0
            && !IsRuntimeMipmapGenerationSupported(format, type, usage, depthUsage)) {
            properties = default;
            return false;
        }

        Format sampleCountFormat = depthUsage ? depthStencilFormat : resourceFormat;
        uint sampleFlags = this.GetSupportedSampleFlags(sampleCountFormat);
        if (sampleFlags == 0) {
            sampleFlags = 1u << (int)TextureSampleCount.Count1;
        }

        bool supportsMsaa = (support1 & FormatSupport1.MultisampleRendertarget) != 0;
        bool allowsMsaaUsage = (usage & (TextureUsage.RenderTarget | TextureUsage.DepthStencil)) != 0;
        if (!supportsMsaa || !allowsMsaaUsage) {
            sampleFlags = 1u << (int)TextureSampleCount.Count1;
        }

        GetTextureTypeLimits(type, out uint maxWidth, out uint maxHeight, out uint maxDepth, out uint maxArrayLayers);
        uint maxMipLevels = GetMaxMipLevels(maxWidth, maxHeight, maxDepth);
        if (type != TextureType.Texture2D
            || (usage & (TextureUsage.Storage | TextureUsage.Staging | TextureUsage.GenerateMipmaps |
                         TextureUsage.Cubemap)) != 0) {
            sampleFlags = 1u << (int)TextureSampleCount.Count1;
        }

        properties = new PixelFormatProperties(maxWidth, maxHeight, maxDepth, maxMipLevels, maxArrayLayers, sampleFlags);
        return true;
    }

    /// <summary>
    /// Gets the d3 d12 info value.
    /// </summary>
    /// <param name="info">The info value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool GetD3D12Info(out BackendInfoD3D12 info) {
        info = this._d3D12Info;
        return true;
    }

    /// <summary>
    /// Executes the select adapter logic for this backend.
    /// </summary>
    /// <param name="factory">The factory value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static IDXGIAdapter1 SelectAdapter(IDXGIFactory4 factory) {
        // Prefer the high-performance adapter when DXGI 1.6 is available.
        using (IDXGIFactory6 factory6 = factory.QueryInterfaceOrNull<IDXGIFactory6>()) {
            if (factory6 != null) {
                uint hpIndex = 0;
                while (factory6.EnumAdapterByGpuPreference(hpIndex, GpuPreference.HighPerformance, out IDXGIAdapter1 hpAdapter).Success) {
                    AdapterDescription1 hpDescription = hpAdapter.Description1;
                    bool softwareHp = (hpDescription.Flags & AdapterFlags.Software) != 0;
                    if (!softwareHp && VorticeD3D12.D3D12CreateDevice(hpAdapter, FeatureLevel.Level_11_0, out ID3D12Device hpProbeDevice).Success) {
                        hpProbeDevice.Dispose();
                        return hpAdapter;
                    }

                    hpAdapter.Dispose();
                    hpIndex++;
                }
            }
        }

        // Fallback: choose the supported hardware adapter with the largest dedicated VRAM.
        IDXGIAdapter1 bestAdapter = null;
        long bestDedicatedMemory = -1;
        uint index = 0;
        while (factory.EnumAdapters1(index, out IDXGIAdapter1 adapter).Success) {
            AdapterDescription1 description = adapter.Description1;
            bool software = (description.Flags & AdapterFlags.Software) != 0;
            if (!software && VorticeD3D12.D3D12CreateDevice(adapter, FeatureLevel.Level_11_0, out ID3D12Device probeDevice).Success) {
                probeDevice.Dispose();
                long dedicatedMemory = description.DedicatedVideoMemory;
                if (dedicatedMemory > bestDedicatedMemory) {
                    bestAdapter?.Dispose();
                    bestAdapter = adapter;
                    bestDedicatedMemory = dedicatedMemory;
                }
                else {
                    adapter.Dispose();
                }
            }
            else {
                adapter.Dispose();
            }

            index++;
        }

        return bestAdapter;
    }

    /// <summary>
    /// Gets the supported sample flags value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private uint GetSupportedSampleFlags(Format format) {
        uint sampleFlags = 1u << (int)TextureSampleCount.Count1;
        sampleFlags |= this.QuerySampleSupportFlag(format, 2, TextureSampleCount.Count2);
        sampleFlags |= this.QuerySampleSupportFlag(format, 4, TextureSampleCount.Count4);
        sampleFlags |= this.QuerySampleSupportFlag(format, 8, TextureSampleCount.Count8);
        sampleFlags |= this.QuerySampleSupportFlag(format, 16, TextureSampleCount.Count16);
        sampleFlags |= this.QuerySampleSupportFlag(format, 32, TextureSampleCount.Count32);
        return sampleFlags;
    }

    /// <summary>
    /// Executes the query sample support flag logic for this backend.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="sampleCount">The sample count value used by this operation.</param>
    /// <param name="textureSampleCount">The texture sample count value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private uint QuerySampleSupportFlag(Format format, uint sampleCount, TextureSampleCount textureSampleCount) {
        FeatureDataMultisampleQualityLevels msaa = new() {
            Format = format,
            SampleCount = sampleCount,
            Flags = MultisampleQualityLevelFlags.None
        };

        if (!this.TryCheckFeatureSupport(D3D12Feature.MultisampleQualityLevels, ref msaa) || msaa.NumQualityLevels == 0) {
            return 0;
        }

        return 1u << (int)textureSampleCount;
    }

    /// <summary>
    /// Gets the texture type limits value.
    /// </summary>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="maxWidth">The max width value used by this operation.</param>
    /// <param name="maxHeight">The max height value used by this operation.</param>
    /// <param name="maxDepth">The max depth value used by this operation.</param>
    /// <param name="maxArrayLayers">The max array layers value used by this operation.</param>
    private static void GetTextureTypeLimits(TextureType type, out uint maxWidth, out uint maxHeight, out uint maxDepth, out uint maxArrayLayers) {
        switch (type) {
            case TextureType.Texture1D:
                maxWidth = 16384;
                maxHeight = 1;
                maxDepth = 1;
                maxArrayLayers = 2048;
                break;
            case TextureType.Texture2D:
                maxWidth = 16384;
                maxHeight = 16384;
                maxDepth = 1;
                maxArrayLayers = 2048;
                break;
            case TextureType.Texture3D:
                maxWidth = 2048;
                maxHeight = 2048;
                maxDepth = 2048;
                maxArrayLayers = 1;
                break;
            default: throw Illegal.Value<TextureType>();
        }
    }

    /// <summary>
    /// Gets the max mip levels value.
    /// </summary>
    /// <param name="maxWidth">The max width value used by this operation.</param>
    /// <param name="maxHeight">The max height value used by this operation.</param>
    /// <param name="maxDepth">The max depth value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static uint GetMaxMipLevels(uint maxWidth, uint maxHeight, uint maxDepth) {
        uint maxDimension = Math.Max(maxWidth, Math.Max(maxHeight, maxDepth));
        uint mipLevels = 1;
        while (maxDimension > 1) {
            maxDimension >>= 1;
            mipLevels++;
        }

        return mipLevels;
    }

    /// <summary>
    /// Executes the is srgb format logic for this backend.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool IsSrgbFormat(PixelFormat format) {
        switch (format) {
            case PixelFormat.R8G8B8A8UNormSRgb: case PixelFormat.B8G8R8A8UNormSRgb: case PixelFormat.Bc1RgbUNormSRgb: case PixelFormat.Bc1RgbaUNormSRgb: case PixelFormat.Bc2UNormSRgb: case PixelFormat.Bc3UNormSRgb: case PixelFormat.Bc7UNormSRgb: return true;
            default: return false;
        }
    }

    /// <summary>
    /// Executes the is runtime mipmap generation supported logic for this backend.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="depthUsage">The depth usage value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool IsRuntimeMipmapGenerationSupported(PixelFormat format, TextureType type, TextureUsage usage, bool depthUsage) {
        if (depthUsage) {
            return false;
        }

        if (FormatHelpers.IsCompressedFormat(format)) {
            return false;
        }

        if ((usage & TextureUsage.Cubemap) != 0) {
            return false;
        }

        if (type != TextureType.Texture1D
            && type != TextureType.Texture2D
            && type != TextureType.Texture3D) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to get format support and reports whether it succeeded.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="formatSupport">The format support value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool TryGetFormatSupport(Format format, out FeatureDataFormatSupport formatSupport) {
        lock (this._formatSupportCacheLock) {
            if (this._formatSupportCache.TryGetValue(format, out CachedFormatSupport cached)) {
                formatSupport = cached.Support;
                return cached.IsSupported;
            }

            formatSupport = new FeatureDataFormatSupport {
                Format = format
            };

            bool isSupported = this.TryCheckFeatureSupport(D3D12Feature.FormatSupport, ref formatSupport);
            this._formatSupportCache[format] = new CachedFormatSupport(isSupported, formatSupport);
            return isSupported;
        }
    }

    /// <summary>
    /// Executes the is type supported logic for this backend.
    /// </summary>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="support">The support value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool IsTypeSupported(TextureType type, FormatSupport1 support) {
        switch (type) {
            case TextureType.Texture1D: return (support & FormatSupport1.Texture1D) != 0;
            case TextureType.Texture2D: return (support & FormatSupport1.Texture2D) != 0;
            case TextureType.Texture3D: return (support & FormatSupport1.Texture3D) != 0;
            default: return false;
        }
    }

    private bool TryCheckFeatureSupport<T>(D3D12Feature feature, ref T data) where T : unmanaged {
        return this._device.CheckFeatureSupport(feature, ref data);
    }

    /// <summary>
    /// Checks whether D3D12 WriteBufferImmediate can be used on direct command lists.
    /// </summary>
    /// <returns><see langword="true" /> when the fast path is supported and enabled.</returns>
    private bool CheckDirectWriteBufferImmediateSupport() {
        if (string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_WRITEBUFFERIMMEDIATE"), "0", StringComparison.Ordinal)) {
            return false;
        }

        FeatureDataD3D12Options3 options = default;
        if (!this.TryCheckFeatureSupport(D3D12Feature.Options3, ref options)) {
            return false;
        }

        return (options.WriteBufferImmediateSupportFlags & CommandListSupportFlags.Direct) != 0;
    }

    /// <summary>
    /// Checks whether native D3D12 render passes can be used.
    /// </summary>
    /// <returns><see langword="true" /> when render passes are supported and enabled.</returns>
    private bool CheckRenderPassSupport() {
        if (!string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_RENDERPASS"), "1", StringComparison.Ordinal)) {
            return false;
        }

        FeatureDataD3D12Options5 options = default;
        if (!this.TryCheckFeatureSupport(D3D12Feature.Options5, ref options)) {
            return false;
        }

        return options.RenderPassesTier != RenderPassTier.Tier0;
    }

    /// <summary>
    /// Executes the wait for queue idle logic for this backend.
    /// </summary>
    private void WaitForQueueIdle() {
        lock (this._commandQueueLock) {
            this.FlushBatchedImmediateCommandsQueueLocked();
            try {
                ulong signalValue = this._nextSubmissionFenceValue++;
                this.SignalQueueFenceNoAlloc(this._submissionFence, signalValue);
                if (this.GetCompletedValueNoAlloc(this._submissionFence) < signalValue) {
                    this._submissionFence.SetEventOnCompletion(signalValue, this._submissionFenceEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();
                    this._submissionFenceEvent.WaitOne();
                }
            }
            catch (SharpGenException ex) when (ex.ResultCode.Code == unchecked((int)0x887A0005)) {
                // Device already lost. During shutdown this should not escalate into a second crash.
            }
        }
    }

    /// <summary>
    /// Waits until an immediate copy fence value has completed.
    /// </summary>
    /// <param name="value">The fence value to wait for.</param>
    private void WaitForImmediateCopyFence(ulong value) {
        if (this.GetCompletedValueNoAlloc(this._immediateCopyFence) >= value) {
            return;
        }

        this._immediateCopyFence.SetEventOnCompletion(value, this._immediateCopyFenceEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();
        this._immediateCopyFenceEvent.WaitOne();
    }

    /// <summary>
    /// Acquires a reusable immediate copy command context from the pool.
    /// </summary>
    /// <returns>The acquired immediate copy command context.</returns>
    private ImmediateCopyContext AcquireImmediateCopyContext() {
        lock (this._immediateCopyContextsLock) {
            ulong completedValue = this.GetCompletedValueNoAlloc(this._immediateCopyFence);
            for (int i = 0; i < this._immediateCopyContexts.Count; i++) {
                ImmediateCopyContext context = this._immediateCopyContexts[i];
                if (!context.InUse && completedValue >= context.FenceValue) {
                    context.InUse = true;
                    return context;
                }
            }

            ID3D12CommandAllocator allocator = this._device.CreateCommandAllocator(CommandListType.Direct);
            ID3D12GraphicsCommandList commandList = this._device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, allocator);
            ImmediateCopyContext created = new(allocator, commandList) {
                InUse = true
            };
            this._immediateCopyContexts.Add(created);
            return created;
        }
    }

    /// <summary>
    /// Resets a pooled immediate copy context so it can record a new command list.
    /// </summary>
    /// <param name="context">The immediate copy context to prepare.</param>
    private static void PrepareImmediateCopyContext(ImmediateCopyContext context) {
        if (context.Initialized) {
            context.Allocator.Reset();
            context.CommandList.Reset(context.Allocator);
            return;
        }

        context.Initialized = true;
    }

    /// <summary>
    /// Releases an immediate copy command context back to the pool.
    /// </summary>
    /// <param name="context">The context to release.</param>
    /// <param name="remove">When true, removes and disposes the context from the pool.</param>
    private void ReleaseImmediateCopyContext(ImmediateCopyContext context, bool remove) {
        lock (this._immediateCopyContextsLock) {
            context.InUse = false;
            if (!remove) {
                return;
            }

            this._immediateCopyContexts.Remove(context);
            context.CommandList.Dispose();
            context.Allocator.Dispose();
        }
    }

    /// <summary>
    /// Releases resources whose submission-fence lifetime has already completed.
    /// </summary>
    private void PumpSubmissionDeferredDisposals() {
        if (Volatile.Read(ref this._submissionDeferredDisposalCount) == 0) {
            return;
        }

        ulong completedFenceValue = this.GetCompletedValueNoAlloc(this._submissionFence);
        ulong nextFenceValue = Volatile.Read(ref this._submissionDeferredDisposalNextFenceValue);
        if (nextFenceValue != 0 && completedFenceValue < nextFenceValue) {
            return;
        }

        this.PumpDeferredDisposals(this._submissionDeferredDisposalsLock, this._submissionDeferredDisposals, ref this._submissionDeferredDisposalCount, ref this._submissionDeferredDisposalNextFenceValue, completedFenceValue);
    }

    /// <summary>
    /// Releases resources whose immediate-copy-fence lifetime has already completed.
    /// </summary>
    private void PumpImmediateDeferredDisposals() {
        if (Volatile.Read(ref this._immediateDeferredDisposalCount) == 0) {
            return;
        }

        ulong completedFenceValue = this.GetCompletedValueNoAlloc(this._immediateCopyFence);
        ulong nextFenceValue = Volatile.Read(ref this._immediateDeferredDisposalNextFenceValue);
        if (nextFenceValue != 0 && completedFenceValue < nextFenceValue) {
            return;
        }

        this.PumpDeferredDisposals(this._immediateDeferredDisposalsLock, this._immediateDeferredDisposals, ref this._immediateDeferredDisposalCount, ref this._immediateDeferredDisposalNextFenceValue, completedFenceValue);
    }

    /// <summary>
    /// Adds a batch of upload buffers to a deferred-disposal queue under one lock.
    /// </summary>
    /// <param name="lockObject">The queue lock object.</param>
    /// <param name="queue">The deferred disposal queue to append to.</param>
    /// <param name="pendingCount">The approximate pending item count.</param>
    /// <param name="nextFenceValue">The cached oldest fence value still pending in the queue.</param>
    /// <param name="buffers">The upload buffers to return after the fence completes.</param>
    /// <param name="fenceValue">The fence value that guards the batch.</param>
    private void EnqueueDeferredUploadBuffers(object lockObject, Queue<DeferredDisposal> queue, ref int pendingCount, ref ulong nextFenceValue, List<D3D12ResourceAllocation> buffers, ulong fenceValue) {
        if (buffers == null || buffers.Count == 0) {
            return;
        }

        IDisposable disposableBatch = this.CreatePooledUploadBufferBatch(buffers);
        if (disposableBatch == null) {
            return;
        }

        lock (lockObject) {
            if (queue.Count == 0) {
                Volatile.Write(ref nextFenceValue, fenceValue);
            }

            queue.Enqueue(new DeferredDisposal(disposableBatch, fenceValue));
        }

        Interlocked.Increment(ref pendingCount);
    }

    /// <summary>
    /// Adds a batch of disposable resources to a deferred-disposal queue under one lock.
    /// </summary>
    /// <param name="lockObject">The queue lock object.</param>
    /// <param name="queue">The deferred disposal queue to append to.</param>
    /// <param name="pendingCount">The approximate pending item count.</param>
    /// <param name="nextFenceValue">The cached oldest fence value still pending in the queue.</param>
    /// <param name="disposables">The disposable resources to release after the fence completes.</param>
    /// <param name="fenceValue">The fence value that guards the batch.</param>
    private void EnqueueDeferredDisposals(object lockObject, Queue<DeferredDisposal> queue, ref int pendingCount, ref ulong nextFenceValue, List<IDisposable> disposables, ulong fenceValue) {
        if (disposables == null || disposables.Count == 0) {
            return;
        }

        IDisposable disposableBatch = CreateDisposableBatch(disposables);
        if (disposableBatch == null) {
            return;
        }

        lock (lockObject) {
            if (queue.Count == 0) {
                Volatile.Write(ref nextFenceValue, fenceValue);
            }

            queue.Enqueue(new DeferredDisposal(disposableBatch, fenceValue));
        }

        Interlocked.Increment(ref pendingCount);
    }

    /// <summary>
    /// Creates the smallest disposable wrapper for a batch of upload buffers.
    /// </summary>
    /// <param name="buffers">The upload buffers to return after a fence completes.</param>
    /// <returns>A disposable upload-buffer wrapper, or null when the list contains no buffers.</returns>
    private IDisposable CreatePooledUploadBufferBatch(List<D3D12ResourceAllocation> buffers) {
        D3D12ResourceAllocation first = null;
        D3D12ResourceAllocation[] batch = null;
        int count = 0;
        for (int i = 0; i < buffers.Count; i++) {
            D3D12ResourceAllocation buffer = buffers[i];
            if (buffer == null) {
                continue;
            }

            if (first == null) {
                first = buffer;
                count = 1;
                continue;
            }

            if (batch == null) {
                batch = new D3D12ResourceAllocation[buffers.Count];
                batch[0] = first;
            }

            batch[count++] = buffer;
        }

        if (first == null) {
            return null;
        }

        return batch == null ? new PooledUploadBuffer(this, first) : new PooledUploadBufferBatch(this, batch, count);
    }

    /// <summary>
    /// Creates the smallest disposable wrapper for a batch of disposable resources.
    /// </summary>
    /// <param name="disposables">The disposable resources to release after a fence completes.</param>
    /// <returns>A disposable wrapper, or null when the list contains no resources.</returns>
    private static IDisposable CreateDisposableBatch(List<IDisposable> disposables) {
        IDisposable first = null;
        IDisposable[] batch = null;
        int count = 0;
        for (int i = 0; i < disposables.Count; i++) {
            IDisposable disposable = disposables[i];
            if (disposable == null) {
                continue;
            }

            if (first == null) {
                first = disposable;
                count = 1;
                continue;
            }

            if (batch == null) {
                batch = new IDisposable[disposables.Count];
                batch[0] = first;
            }

            batch[count++] = disposable;
        }

        if (first == null) {
            return null;
        }

        return batch == null ? first : new DisposableBatch(batch, count);
    }

    /// <summary>
    /// Disposes all deferred resources, regardless of fence value.
    /// </summary>
    private void DisposeAllDeferredDisposals() {
        this.DisposeDeferredQueue(this._submissionDeferredDisposalsLock, this._submissionDeferredDisposals, ref this._submissionDeferredDisposalCount, ref this._submissionDeferredDisposalNextFenceValue);
        this.DisposeDeferredQueue(this._immediateDeferredDisposalsLock, this._immediateDeferredDisposals, ref this._immediateDeferredDisposalCount, ref this._immediateDeferredDisposalNextFenceValue);
    }

    /// <summary>
    /// Disposes queue entries whose fence values are already completed.
    /// </summary>
    /// <param name="lockObject">The queue lock object.</param>
    /// <param name="queue">The deferred disposal queue to process.</param>
    /// <param name="pendingCount">The approximate pending item count.</param>
    /// <param name="nextFenceValue">The cached oldest fence value still pending in the queue.</param>
    /// <param name="completedFenceValue">The latest completed fence value.</param>
    private void PumpDeferredDisposals(object lockObject, Queue<DeferredDisposal> queue, ref int pendingCount, ref ulong nextFenceValue, ulong completedFenceValue) {
        int count = 0;
        lock (lockObject) {
            while (queue.Count > 0 && queue.Peek().FenceValue <= completedFenceValue) {
                if (count == this._pumpDisposalBatch.Length) {
                    Array.Resize(ref this._pumpDisposalBatch, this._pumpDisposalBatch.Length * 2);
                }

                this._pumpDisposalBatch[count++] = queue.Dequeue().Disposable;
            }

            Volatile.Write(ref nextFenceValue, queue.Count > 0 ? queue.Peek().FenceValue : 0);
        }

        if (count == 0) {
            return;
        }

        Interlocked.Add(ref pendingCount, -count);
        for (int i = 0; i < count; i++) {
            this._pumpDisposalBatch[i].Dispose();
            this._pumpDisposalBatch[i] = null;
        }
    }

    /// <summary>
    /// Disposes all entries in the specified deferred disposal queue.
    /// </summary>
    /// <param name="lockObject">The queue lock object.</param>
    /// <param name="queue">The deferred disposal queue to drain.</param>
    /// <param name="pendingCount">The approximate pending item count.</param>
    /// <param name="nextFenceValue">The cached oldest fence value still pending in the queue.</param>
    private void DisposeDeferredQueue(object lockObject, Queue<DeferredDisposal> queue, ref int pendingCount, ref ulong nextFenceValue) {
        int count = 0;
        lock (lockObject) {
            while (queue.Count > 0) {
                if (count == this._pumpDisposalBatch.Length) {
                    Array.Resize(ref this._pumpDisposalBatch, this._pumpDisposalBatch.Length * 2);
                }

                this._pumpDisposalBatch[count++] = queue.Dequeue().Disposable;
            }
        }

        Volatile.Write(ref pendingCount, 0);
        Volatile.Write(ref nextFenceValue, 0);
        for (int i = 0; i < count; i++) {
            this._pumpDisposalBatch[i].Dispose();
            this._pumpDisposalBatch[i] = null;
        }
    }

    /// <summary>
    /// Represents the CachedFormatSupport data structure used by the graphics runtime.
    /// </summary>
    private readonly struct CachedFormatSupport {

        /// <summary>
        /// Stores the is supported state used by this instance.
        /// </summary>
        public readonly bool IsSupported;

        /// <summary>
        /// Stores the support state used by this instance.
        /// </summary>
        public readonly FeatureDataFormatSupport Support;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedFormatSupport" /> type.
        /// </summary>
        /// <param name="isSupported">The is supported value used by this operation.</param>
        /// <param name="support">The support value used by this operation.</param>
        public CachedFormatSupport(bool isSupported, FeatureDataFormatSupport support) {
            this.IsSupported = isSupported;
            this.Support = support;
        }
    }

    /// <summary>
    /// Represents a disposable resource that must stay alive until a specific fence value completes.
    /// </summary>
    private readonly struct DeferredDisposal {

        /// <summary>
        /// Gets the disposable resource reference.
        /// </summary>
        public IDisposable Disposable { get; }

        /// <summary>
        /// Gets the fence value that guards resource disposal.
        /// </summary>
        public ulong FenceValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeferredDisposal"/> struct.
        /// </summary>
        /// <param name="disposable">The resource to dispose when the fence completes.</param>
        /// <param name="fenceValue">The fence value that marks safe disposal.</param>
        public DeferredDisposal(IDisposable disposable, ulong fenceValue) {
            this.Disposable = disposable;
            this.FenceValue = fenceValue;
        }
    }

    /// <summary>
    /// Returns an upload buffer to its owning D3D12 device when a deferred fence completes.
    /// </summary>
    private sealed class PooledUploadBuffer : IDisposable {

        /// <summary>
        /// Stores the owning graphics device.
        /// </summary>
        private readonly D3D12GraphicsDevice _gd;

        /// <summary>
        /// Stores the upload buffer being returned.
        /// </summary>
        private D3D12ResourceAllocation _buffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledUploadBuffer"/> class.
        /// </summary>
        /// <param name="gd">The owning graphics device.</param>
        /// <param name="buffer">The upload buffer to return.</param>
        public PooledUploadBuffer(D3D12GraphicsDevice gd, D3D12ResourceAllocation buffer) {
            this._gd = gd;
            this._buffer = buffer;
        }

        /// <summary>
        /// Returns the upload buffer to the graphics device pool.
        /// </summary>
        public void Dispose() {
            D3D12ResourceAllocation resource = this._buffer;
            this._buffer = null;
            if (resource != null) {
                this._gd.ReturnUploadBuffer(resource);
            }
        }
    }

    /// <summary>
    /// Returns a batch of upload buffers to its owning D3D12 device when a deferred fence completes.
    /// </summary>
    private sealed class PooledUploadBufferBatch : IDisposable {

        /// <summary>
        /// Stores the owning graphics device.
        /// </summary>
        private readonly D3D12GraphicsDevice _gd;

        /// <summary>
        /// Stores upload buffers retained by this batch.
        /// </summary>
        private D3D12ResourceAllocation[] _buffers;

        /// <summary>
        /// Stores the number of valid entries in <see cref="_buffers" />.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledUploadBufferBatch" /> class.
        /// </summary>
        /// <param name="gd">The owning graphics device.</param>
        /// <param name="buffers">The upload buffers to return.</param>
        /// <param name="count">The number of valid buffer entries.</param>
        public PooledUploadBufferBatch(D3D12GraphicsDevice gd, D3D12ResourceAllocation[] buffers, int count) {
            this._gd = gd;
            this._buffers = buffers;
            this._count = count;
        }

        /// <summary>
        /// Returns every upload buffer in the batch to the graphics device pool.
        /// </summary>
        public void Dispose() {
            D3D12ResourceAllocation[] buffers = this._buffers;
            this._buffers = null;
            if (buffers == null) {
                return;
            }

            for (int i = 0; i < this._count; i++) {
                D3D12ResourceAllocation resource = buffers[i];
                buffers[i] = null;
                if (resource != null) {
                    this._gd.ReturnUploadBuffer(resource);
                }
            }
        }
    }

    /// <summary>
    /// Disposes a batch of resources when a deferred fence completes.
    /// </summary>
    private sealed class DisposableBatch : IDisposable {

        /// <summary>
        /// Stores disposable resources retained by this batch.
        /// </summary>
        private IDisposable[] _disposables;

        /// <summary>
        /// Stores the number of valid entries in <see cref="_disposables" />.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableBatch" /> class.
        /// </summary>
        /// <param name="disposables">The resources to dispose.</param>
        /// <param name="count">The number of valid disposable entries.</param>
        public DisposableBatch(IDisposable[] disposables, int count) {
            this._disposables = disposables;
            this._count = count;
        }

        /// <summary>
        /// Disposes every retained resource.
        /// </summary>
        public void Dispose() {
            IDisposable[] disposables = this._disposables;
            this._disposables = null;
            if (disposables == null) {
                return;
            }

            for (int i = 0; i < this._count; i++) {
                IDisposable disposable = disposables[i];
                disposables[i] = null;
                disposable?.Dispose();
            }
        }
    }

    /// <summary>
    /// Represents a reusable command allocator and command list pair for immediate copy workloads.
    /// </summary>
    private sealed class ImmediateCopyContext {

        /// <summary>
        /// Gets the command allocator owned by this context.
        /// </summary>
        public ID3D12CommandAllocator Allocator { get; }

        /// <summary>
        /// Gets the command list owned by this context.
        /// </summary>
        public ID3D12GraphicsCommandList CommandList { get; }

        /// <summary>
        /// Gets or sets the last fence value submitted by this context.
        /// </summary>
        public ulong FenceValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this context has already been used at least once.
        /// </summary>
        public bool Initialized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this context is currently checked out.
        /// </summary>
        public bool InUse { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmediateCopyContext"/> class.
        /// </summary>
        /// <param name="allocator">The command allocator used by this context.</param>
        /// <param name="commandList">The command list used by this context.</param>
        public ImmediateCopyContext(ID3D12CommandAllocator allocator, ID3D12GraphicsCommandList commandList) {
            this.Allocator = allocator;
            this.CommandList = commandList;
        }
    }
}

/// <summary>
/// Identifies a D3D12 resource creation category for performance logging.
/// </summary>
internal enum D3D12ResourceCreationKind {

    /// <summary>
    /// A device buffer was created.
    /// </summary>
    Buffer,

    /// <summary>
    /// A pipeline state wrapper was created.
    /// </summary>
    Pipeline,

    /// <summary>
    /// A resource set was created.
    /// </summary>
    ResourceSet,

    /// <summary>
    /// A shader wrapper was created.
    /// </summary>
    Shader,

    /// <summary>
    /// A texture was created.
    /// </summary>
    Texture
}
