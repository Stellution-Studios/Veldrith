using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using D3D12Feature = Vortice.Direct3D12.Feature;
using VorticeD3D12 = Vortice.Direct3D12.D3D12;
using VorticeDXGI = Vortice.DXGI.DXGI;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12GraphicsDevice.
/// </summary>
internal sealed class D3D12GraphicsDevice : GraphicsDevice {

    /// <summary>
    /// Stores the largest upload buffer size that is retained for reuse.
    /// </summary>
    private const ulong MaxPooledUploadBufferSize = 16UL * 1024UL * 1024UL;

    /// <summary>
    /// Stores the size of each transient upload ring page.
    /// </summary>
    private const ulong UploadRingPageSize = 16UL * 1024UL * 1024UL;

    /// <summary>
    /// Stores the total upload-buffer pool budget.
    /// </summary>
    private const ulong MaxPooledUploadBufferBytes = 128UL * 1024UL * 1024UL;

    /// <summary>
    /// Stores the number of submissions between D3D12 device performance reports.
    /// </summary>
    private const int PerfReportIntervalSubmissions = 240;

    /// <summary>
    /// Stores the d3d12 features state used by this instance.
    /// </summary>



















    private static readonly GraphicsDeviceFeatures _d3d12Features = new(true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false);

    /// <summary>
    /// Tracks whether D3D12 performance logging is enabled for device-level upload and submit work.
    /// </summary>
    private static readonly bool _perfLogEnabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PERF"), "1", StringComparison.Ordinal);

    /// <summary>
    /// Gets whether D3D12 performance logging is enabled.
    /// </summary>
    internal static bool PerfLogEnabled => _perfLogEnabled;

    /// <summary>
    /// Stores the d3d12 info state used by this instance.
    /// </summary>
    private readonly BackendInfoD3D12 _d3d12Info;

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
    /// Protects native pipeline state cache access.
    /// </summary>
    private readonly object _pipelineStateCacheLock = new();

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
    /// Stores deferred disposals tracked by immediate copy fence values.
    /// </summary>
    private readonly Queue<DeferredDisposal> _immediateDeferredDisposals = new();

    /// <summary>
    /// Protects immediate deferred disposal queue access.
    /// </summary>
    private readonly object _immediateDeferredDisposalsLock = new();

    /// <summary>
    /// Stores reusable upload buffers after their GPU fence has completed.
    /// </summary>
    private readonly List<D3D12ResourceAllocation> _availableUploadBuffers = new();

    /// <summary>
    /// Stores transient upload pages used as a fence-recycled ring.
    /// </summary>
    private readonly List<UploadRingPage> _uploadRingPages = new();

    /// <summary>
    /// Protects reusable upload-buffer pool access.
    /// </summary>
    private readonly object _availableUploadBuffersLock = new();

    /// <summary>
    /// Tracks total bytes retained by the upload-buffer pool.
    /// </summary>
    private ulong _availableUploadBufferBytes;

    /// <summary>
    /// Stores the upload ring page index preferred for the next allocation.
    /// </summary>
    private int _currentUploadRingPageIndex = -1;

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
        try {
            if (adapter != null) {
                VorticeD3D12.D3D12CreateDevice(adapter, FeatureLevel.Level_11_0, out this._device).CheckError();
                AdapterDescription1 description = adapter.Description1;
                this.DeviceName = description.Description?.TrimEnd('\0');
                this.VendorName = $"0x{description.VendorId:X4}";
            }
            else {
                VorticeD3D12.D3D12CreateDevice(null, FeatureLevel.Level_11_0, out this._device).CheckError();
                this.DeviceName = "Direct3D 12 Device";
                this.VendorName = "Unknown";
            }
        }
        finally {
            adapter?.Dispose();
        }

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
        this._resourceFactory = new D3D12ResourceFactory(this, this.Features);

        if (swapchainDescription != null) {
            SwapchainDescription scDesc = swapchainDescription.Value;
            this.MainSwapchain = new D3D12Swapchain(this, ref scDesc);
        }

        this._d3d12Info = new BackendInfoD3D12(this._device.NativePointer);
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
    public override GraphicsApiVersion ApiVersion => GraphicsApiVersion.Unknown;

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
    public override GraphicsDeviceFeatures Features => _d3d12Features;

    /// <summary>
    /// Gets or sets AllowTearing.
    /// </summary>
    public override bool AllowTearing {
        get => this.MainSwapchain is D3D12Swapchain d3d12Swapchain && d3d12Swapchain.AllowTearing;
        set {
            if (this.MainSwapchain is D3D12Swapchain d3d12Swapchain) {
                d3d12Swapchain.AllowTearing = value;
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
        if (this._perfSubmissions % PerfReportIntervalSubmissions != 0) {
            return;
        }

        double elapsedMs = this._perfStopwatch.Elapsed.TotalMilliseconds;
        double reportWindowMs = elapsedMs - this._perfLastReportMs;
        this._perfLastReportMs = elapsedMs;
        double invSubmissions = 1.0 / PerfReportIntervalSubmissions;
        Console.WriteLine($"[D3D12 PERF] device {PerfReportIntervalSubmissions} submits/{reportWindowMs:F0}ms avg: " + $"submitMs={this._perfAccumSubmitMs * invSubmissions:F3}, " + $"immRecordMs={this._perfAccumImmediateRecordMs * invSubmissions:F3} ({this._perfAccumImmediateRecordCalls * invSubmissions:F2}x), " + $"immFlushMs={this._perfAccumImmediateFlushMs * invSubmissions:F3} ({this._perfAccumImmediateFlushes * invSubmissions:F2}x), " + $"immExecMs={this._perfAccumImmediateExecuteMs * invSubmissions:F3} ({this._perfAccumImmediateExecutes * invSubmissions:F2}x), " + $"immWaitMs={this._perfAccumImmediateWaitMs * invSubmissions:F3}, " + $"createBuf={this._perfAccumCreateBuffers * invSubmissions:F2}/{this._perfMaxCreateBufferMs:F3}ms, " + $"createTex={this._perfAccumCreateTextures * invSubmissions:F2}/{this._perfMaxCreateTextureMs:F3}ms, " + $"createPipe={this._perfAccumCreatePipelines * invSubmissions:F2}/{this._perfMaxCreatePipelineMs:F3}ms, " + $"createSet={this._perfAccumCreateResourceSets * invSubmissions:F2}/{this._perfMaxCreateResourceSetMs:F3}ms, " + $"createShader={this._perfAccumCreateShaders * invSubmissions:F2}/{this._perfMaxCreateShaderMs:F3}ms, " + $"maxSubmitMs={this._perfMaxSubmitMs:F3}, maxImmRecordMs={this._perfMaxImmediateRecordMs:F3}, maxImmFlushMs={this._perfMaxImmediateFlushMs:F3}, " + $"maxImmExecMs={this._perfMaxImmediateExecuteMs:F3}, maxImmWaitMs={this._perfMaxImmediateWaitMs:F3}, " + $"immUploadBuf={this._perfAccumImmediateUploadBuffers * invSubmissions:F2}, " + this.MemoryManager.GetStatsString());

        this._perfAccumImmediateFlushMs = 0;
        this._perfAccumImmediateExecuteMs = 0;
        this._perfAccumImmediateRecordMs = 0;
        this._perfAccumImmediateWaitMs = 0;
        this._perfAccumSubmitMs = 0;
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
    /// Executes the is submission fence complete logic for this backend.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool IsSubmissionFenceComplete(ulong value) {
        return this._submissionFence.CompletedValue >= value;
    }

    /// <summary>
    /// Executes the wait for submission fence logic for this backend.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    internal void WaitForSubmissionFence(ulong value) {
        if (this._submissionFence.CompletedValue >= value) {
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
            this.FlushBatchedImmediateCommands();

            ImmediateCopyContext context = this.AcquireImmediateCopyContext();
            bool removeContext = false;
            try {
                PrepareImmediateCopyContext(context);

                recordCommands(context.CommandList);
                context.CommandList.Close();
                this.CommandQueue.ExecuteCommandList(context.CommandList);
                signalValue = this._nextImmediateCopyFenceValue++;
                this.CommandQueue.Signal(this._immediateCopyFence, signalValue).CheckError();
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
        lock (this._commandQueueLock) {
            lock (this._batchedImmediateCopyLock) {
                if (this._batchedImmediateCopyContext == null) {
                    this._batchedImmediateCopyContext = this.AcquireImmediateCopyContext();
                    PrepareImmediateCopyContext(this._batchedImmediateCopyContext);
                }

                recordCommands(this._batchedImmediateCopyContext.CommandList);
                if (uploadBuffer != null) {
                    this._batchedImmediateUploadBuffers.Add(uploadBuffer);
                }

                if (retainedResource != null) {
                    this._batchedImmediateRetainedResources.Add(retainedResource);
                }

                this._batchedImmediateCopyHasWork = true;
            }
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
    /// Submits any batched immediate upload work recorded since the last flush.
    /// </summary>
    /// <param name="waitForCompletion">When true, waits until the submitted upload work has completed.</param>
    /// <returns>The signaled fence value, or zero when there was no batched work.</returns>
    internal ulong FlushBatchedImmediateCommands(bool waitForCompletion = false) {
        long startTicks = _perfLogEnabled ? Stopwatch.GetTimestamp() : 0;
        ImmediateCopyContext context;
        ulong signalValue;
        int uploadBufferCount;

        lock (this._commandQueueLock) {
            lock (this._batchedImmediateCopyLock) {
                if (!this._batchedImmediateCopyHasWork || this._batchedImmediateCopyContext == null) {
                    return 0;
                }

                context = this._batchedImmediateCopyContext;
                this._batchedImmediateCopyContext = null;
                this._batchedImmediateCopyHasWork = false;

                context.CommandList.Close();
                this.CommandQueue.ExecuteCommandList(context.CommandList);
                signalValue = this._nextImmediateCopyFenceValue++;
                this.CommandQueue.Signal(this._immediateCopyFence, signalValue).CheckError();
                context.FenceValue = signalValue;

                uploadBufferCount = this._batchedImmediateUploadBuffers.Count;
                for (int i = 0; i < this._batchedImmediateUploadBuffers.Count; i++) {
                    this.EnqueueImmediateUploadBuffer(this._batchedImmediateUploadBuffers[i], signalValue);
                }

                for (int i = 0; i < this._batchedImmediateRetainedResources.Count; i++) {
                    this.EnqueueImmediateDisposal(this._batchedImmediateRetainedResources[i], signalValue);
                }

                this._batchedImmediateUploadBuffers.Clear();
                this._batchedImmediateRetainedResources.Clear();
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
            this._submissionDeferredDisposals.Enqueue(new DeferredDisposal(disposable, fenceValue));
        }
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
        if (fenceValue == 0 || this._submissionFence.CompletedValue >= fenceValue) {
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
            this._immediateDeferredDisposals.Enqueue(new DeferredDisposal(disposable, fenceValue));
        }
    }

    /// <summary>
    /// Rents an upload buffer that is at least the requested size.
    /// </summary>
    /// <param name="sizeInBytes">The required upload-buffer size in bytes.</param>
    /// <returns>An upload heap resource ready for CPU writes.</returns>
    internal D3D12ResourceAllocation RentUploadBuffer(ulong sizeInBytes) {
        if (sizeInBytes == 0) {
            sizeInBytes = 1;
        }

        ulong allocationSize = AlignUp(sizeInBytes, 256UL);
        if (allocationSize <= UploadRingPageSize) {
            lock (this._availableUploadBuffersLock) {
                D3D12ResourceAllocation ringAllocation = this.TryRentUploadRingAllocation(allocationSize);
                if (ringAllocation != null) {
                    return ringAllocation;
                }
            }
        }

        lock (this._availableUploadBuffersLock) {
            int bestIndex = -1;
            ulong bestSize = ulong.MaxValue;
            for (int i = 0; i < this._availableUploadBuffers.Count; i++) {
                D3D12ResourceAllocation candidate = this._availableUploadBuffers[i];
                ulong candidateSize = candidate.Resource.Description.Width;
                if (candidateSize >= sizeInBytes && candidateSize < bestSize) {
                    bestIndex = i;
                    bestSize = candidateSize;
                }
            }

            if (bestIndex >= 0) {
                D3D12ResourceAllocation buffer = this._availableUploadBuffers[bestIndex];
                this._availableUploadBuffers.RemoveAt(bestIndex);
                this._availableUploadBufferBytes -= buffer.Resource.Description.Width;
                return buffer;
            }
        }

        ResourceDescription description = ResourceDescription.Buffer(allocationSize);
        return this.MemoryManager.CreateResource(ref description, ResourceStates.GenericRead, HeapType.Upload, HeapFlags.AllowOnlyBuffers);
    }

    /// <summary>
    /// Returns an upload buffer to the reusable pool or disposes it when it is too large.
    /// </summary>
    /// <param name="buffer">The upload buffer to return.</param>
    internal void ReturnUploadBuffer(D3D12ResourceAllocation buffer) {
        if (buffer == null) {
            return;
        }

        if (buffer.IsTransient) {
            buffer.Dispose();
            return;
        }

        ulong size = buffer.Resource.Description.Width;
        if (size > MaxPooledUploadBufferSize) {
            buffer.Dispose();
            return;
        }

        lock (this._availableUploadBuffersLock) {
            if (this._availableUploadBufferBytes + size > MaxPooledUploadBufferBytes) {
                buffer.Dispose();
                return;
            }

            this._availableUploadBuffers.Add(buffer);
            this._availableUploadBufferBytes += size;
        }
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
    /// Returns an upload buffer to the pool after an immediate-copy fence has completed.
    /// </summary>
    /// <param name="buffer">The upload buffer to return.</param>
    /// <param name="fenceValue">The immediate-copy fence value guarding the buffer.</param>
    internal void EnqueueImmediateUploadBuffer(D3D12ResourceAllocation buffer, ulong fenceValue) {
        this.EnqueueImmediateDisposal(new PooledUploadBuffer(this, buffer), fenceValue);
    }

    /// <summary>
    /// Attempts to rent a suballocation from the transient upload ring.
    /// </summary>
    /// <param name="sizeInBytes">The aligned allocation size.</param>
    /// <returns>The transient allocation, or null when a dedicated upload buffer should be used.</returns>
    private D3D12ResourceAllocation TryRentUploadRingAllocation(ulong sizeInBytes) {
        if (this._currentUploadRingPageIndex >= 0 && this._currentUploadRingPageIndex < this._uploadRingPages.Count) {
            D3D12ResourceAllocation allocation = this._uploadRingPages[this._currentUploadRingPageIndex].TryAllocate(sizeInBytes);
            if (allocation != null) {
                return allocation;
            }
        }

        for (int i = 0; i < this._uploadRingPages.Count; i++) {
            D3D12ResourceAllocation allocation = this._uploadRingPages[i].TryAllocate(sizeInBytes);
            if (allocation != null) {
                this._currentUploadRingPageIndex = i;
                return allocation;
            }
        }

        ResourceDescription description = ResourceDescription.Buffer(UploadRingPageSize);
        D3D12ResourceAllocation pageAllocation = this.MemoryManager.CreateResource(ref description, ResourceStates.GenericRead, HeapType.Upload, HeapFlags.AllowOnlyBuffers);
        UploadRingPage page = new(pageAllocation);
        this._uploadRingPages.Add(page);
        this._currentUploadRingPageIndex = this._uploadRingPages.Count - 1;
        return page.TryAllocate(sizeInBytes);
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
        this.DisposeAvailableUploadBuffers();

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

        lock (this._pipelineStateCacheLock) {
            foreach (ID3D12PipelineState pipelineState in this._pipelineStateCache.Values) {
                pipelineState?.Dispose();
            }

            this._pipelineStateCache.Clear();
        }

        this.MainSwapchain?.Dispose();
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
        lock (this._commandQueueLock) {
            this.FlushBatchedImmediateCommands();
            this.PumpSubmissionDeferredDisposals();
            this.PumpImmediateDeferredDisposals();

            try {
                if (commandList is D3D12CommandList d3d12CommandList) {
                    d3d12CommandList.ExecuteNoSignal();
                    ulong signalValue = this._nextSubmissionFenceValue++;
                    this.CommandQueue.Signal(this._submissionFence, signalValue).CheckError();
                    this._lastSubmissionFenceValue = signalValue;
                    d3d12CommandList.MarkSubmitted(signalValue);
                    d3d12CommandList.ClearCachedState();
                }

                if (fence is D3D12Fence d3d12Fence) {
                    d3d12Fence.Signal(this.CommandQueue);
                }

                this.PumpSubmissionDeferredDisposals();
                this.PumpImmediateDeferredDisposals();
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
    /// Executes the swap buffers core logic for this backend.
    /// </summary>
    /// <param name="swapchain">The swapchain used by this operation.</param>
    private protected override void SwapBuffersCore(Swapchain swapchain) {
        if (swapchain is D3D12Swapchain d3d12Swapchain) {
            d3d12Swapchain.Present();
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
        if (this.MainSwapchain is D3D12Swapchain swapchain) {
            swapchain.WaitForNextFrameReady();
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
            this.UpdateNativeTexture(d3d12Texture, source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
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
        info = this._d3d12Info;
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
                    if (!softwareHp
                        && VorticeD3D12
                            .D3D12CreateDevice(hpAdapter, FeatureLevel.Level_11_0, out ID3D12Device hpProbeDevice)
                            .Success) {
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
            if (!software
                && VorticeD3D12.D3D12CreateDevice(adapter, FeatureLevel.Level_11_0, out ID3D12Device probeDevice)
                    .Success) {
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

    private bool TryCheckFeatureSupport<T>(D3D12Feature feature, ref T data)
        where T : unmanaged {
        return this._device.CheckFeatureSupport(feature, ref data);
    }

    /// <summary>
    /// Updates the native texture state for this command sequence.
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
    private void UpdateNativeTexture(D3D12Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
        // Use the validated staging->native upload path in D3D12Texture to avoid
        // partial CopyTextureRegion edge-cases that can trigger device removal.
        texture.UpdateNativeSubresource(source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    /// <summary>
    /// Copies texture data to upload buffer data between resources.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="copyWidth">The copy width value used by this operation.</param>
    /// <param name="copyHeight">The copy height value used by this operation.</param>
    /// <param name="copyDepth">The copy depth value used by this operation.</param>
    /// <param name="uploadMappedPtr">The upload mapped ptr value used by this operation.</param>
    /// <param name="placedFootprint">The placed footprint value used by this operation.</param>
    /// <param name="numRows">The num rows value used by this operation.</param>
    /// <param name="rowSizeInBytes">The size, in bytes, used by this operation.</param>
    private unsafe void CopyTextureDataToUploadBuffer(IntPtr source, uint sizeInBytes, PixelFormat format, uint copyWidth, uint copyHeight, uint copyDepth, void* uploadMappedPtr, PlacedSubresourceFootPrint placedFootprint, uint numRows, ulong rowSizeInBytes) {
        uint srcRowPitch = FormatHelpers.GetRowPitch(copyWidth, format);
        uint srcNumRows = FormatHelpers.GetNumRows(copyHeight, format);
        uint srcDepthPitch = srcRowPitch * srcNumRows;
        ulong requiredBytes = (ulong)srcDepthPitch * copyDepth;
        if (sizeInBytes < requiredBytes) {
            throw new VeldridException("Texture update source size is smaller than required for the destination texture.");
        }

        if (numRows < srcNumRows) {
            throw new VeldridException("Unexpected row count when uploading native D3D12 texture data.");
        }

        if (rowSizeInBytes < srcRowPitch) {
            throw new VeldridException("Unexpected row size when uploading native D3D12 texture data.");
        }

        byte* srcBase = (byte*)source.ToPointer();
        byte* dstBase = (byte*)uploadMappedPtr + placedFootprint.Offset;
        uint dstRowPitch = placedFootprint.Footprint.RowPitch;
        uint dstSlicePitch = dstRowPitch * numRows;
        uint copyRowSize = srcRowPitch;

        for (uint slice = 0; slice < copyDepth; slice++) {
            for (uint row = 0; row < srcNumRows; row++) {
                byte* srcRow = srcBase + slice * srcDepthPitch + row * srcRowPitch;
                byte* dstRow = dstBase + slice * dstSlicePitch + row * dstRowPitch;
                Unsafe.CopyBlock(dstRow, srcRow, copyRowSize);
            }
        }
    }

    /// <summary>
    /// Executes the wait for queue idle logic for this backend.
    /// </summary>
    private void WaitForQueueIdle() {
        lock (this._commandQueueLock) {
            this.FlushBatchedImmediateCommands();
            try {
                ulong signalValue = this._nextSubmissionFenceValue++;
                this.CommandQueue.Signal(this._submissionFence, signalValue).CheckError();
                if (this._submissionFence.CompletedValue < signalValue) {
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
        if (this._immediateCopyFence.CompletedValue >= value) {
            return;
        }

        this._immediateCopyFence
            .SetEventOnCompletion(value, this._immediateCopyFenceEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();
        this._immediateCopyFenceEvent.WaitOne();
    }

    /// <summary>
    /// Acquires a reusable immediate copy command context from the pool.
    /// </summary>
    /// <returns>The acquired immediate copy command context.</returns>
    private ImmediateCopyContext AcquireImmediateCopyContext() {
        lock (this._immediateCopyContextsLock) {
            ulong completedValue = this._immediateCopyFence.CompletedValue;
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
        this.PumpDeferredDisposals(this._submissionDeferredDisposalsLock, this._submissionDeferredDisposals, this._submissionFence.CompletedValue);
    }

    /// <summary>
    /// Releases resources whose immediate-copy-fence lifetime has already completed.
    /// </summary>
    private void PumpImmediateDeferredDisposals() {
        this.PumpDeferredDisposals(this._immediateDeferredDisposalsLock, this._immediateDeferredDisposals, this._immediateCopyFence.CompletedValue);
    }

    /// <summary>
    /// Disposes all deferred resources, regardless of fence value.
    /// </summary>
    private void DisposeAllDeferredDisposals() {
        this.DisposeDeferredQueue(this._submissionDeferredDisposalsLock, this._submissionDeferredDisposals);
        this.DisposeDeferredQueue(this._immediateDeferredDisposalsLock, this._immediateDeferredDisposals);
    }

    /// <summary>
    /// Disposes queue entries whose fence values are already completed.
    /// </summary>
    /// <param name="lockObject">The queue lock object.</param>
    /// <param name="queue">The deferred disposal queue to process.</param>
    /// <param name="completedFenceValue">The latest completed fence value.</param>
    private void PumpDeferredDisposals(object lockObject, Queue<DeferredDisposal> queue, ulong completedFenceValue) {
        List<IDisposable> disposables = null;
        lock (lockObject) {
            while (queue.Count > 0 && queue.Peek().FenceValue <= completedFenceValue) {
                (disposables ??= new List<IDisposable>(4)).Add(queue.Dequeue().Disposable);
            }
        }

        if (disposables == null) {
            return;
        }

        for (int i = 0; i < disposables.Count; i++) {
            disposables[i].Dispose();
        }
    }

    /// <summary>
    /// Disposes all entries in the specified deferred disposal queue.
    /// </summary>
    /// <param name="lockObject">The queue lock object.</param>
    /// <param name="queue">The deferred disposal queue to drain.</param>
    private void DisposeDeferredQueue(object lockObject, Queue<DeferredDisposal> queue) {
        List<IDisposable> disposables = null;
        lock (lockObject) {
            while (queue.Count > 0) {
                (disposables ??= new List<IDisposable>(queue.Count)).Add(queue.Dequeue().Disposable);
            }
        }

        if (disposables == null) {
            return;
        }

        for (int i = 0; i < disposables.Count; i++) {
            disposables[i].Dispose();
        }
    }

    /// <summary>
    /// Disposes all upload buffers currently retained by the pool.
    /// </summary>
    private void DisposeAvailableUploadBuffers() {
        List<D3D12ResourceAllocation> buffers = null;
        List<UploadRingPage> ringPages = null;
        lock (this._availableUploadBuffersLock) {
            if (this._availableUploadBuffers.Count > 0) {
                buffers = new List<D3D12ResourceAllocation>(this._availableUploadBuffers);
                this._availableUploadBuffers.Clear();
                this._availableUploadBufferBytes = 0;
            }

            if (this._uploadRingPages.Count > 0) {
                ringPages = new List<UploadRingPage>(this._uploadRingPages);
                this._uploadRingPages.Clear();
                this._currentUploadRingPageIndex = -1;
            }
        }

        if (buffers != null) {
            for (int i = 0; i < buffers.Count; i++) {
                buffers[i].Dispose();
            }
        }

        if (ringPages == null) {
            return;
        }

        for (int i = 0; i < ringPages.Count; i++) {
            ringPages[i].Dispose();
        }
    }

    /// <summary>
    /// Aligns a value upward to the specified alignment.
    /// </summary>
    /// <param name="value">The value to align.</param>
    /// <param name="alignment">The alignment boundary.</param>
    /// <returns>The aligned value.</returns>
    private static ulong AlignUp(ulong value, ulong alignment) {
        return (value + alignment - 1) / alignment * alignment;
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
    /// Represents a persistently mapped transient upload page.
    /// </summary>
    private sealed class UploadRingPage : IDisposable {

        /// <summary>
        /// Stores the backing upload resource allocation.
        /// </summary>
        private readonly D3D12ResourceAllocation _allocation;

        /// <summary>
        /// Protects page suballocation state.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// Stores the current write offset.
        /// </summary>
        private ulong _offset;

        /// <summary>
        /// Stores the number of live transient allocations on this page.
        /// </summary>
        private int _activeAllocations;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadRingPage"/> type.
        /// </summary>
        /// <param name="allocation">The backing allocation.</param>
        public UploadRingPage(D3D12ResourceAllocation allocation) {
            this._allocation = allocation;
        }

        /// <summary>
        /// Attempts to allocate a transient region from this page.
        /// </summary>
        /// <param name="sizeInBytes">The allocation size in bytes.</param>
        /// <returns>The allocation, or null when this page has no room.</returns>
        public D3D12ResourceAllocation TryAllocate(ulong sizeInBytes) {
            lock (this._lock) {
                if (this._offset + sizeInBytes > UploadRingPageSize) {
                    if (this._activeAllocations == 0) {
                        this._offset = 0;
                    }

                    if (this._offset + sizeInBytes > UploadRingPageSize) {
                        return null;
                    }
                }

                ulong allocationOffset = this._offset;
                this._offset += sizeInBytes;
                this._activeAllocations++;
                IntPtr mappedPointer = IntPtr.Add(this._allocation.MappedPointer, checked((int)allocationOffset));
                return new D3D12ResourceAllocation(this._allocation.Resource, null, mappedPointer, allocationOffset, sizeInBytes, this.ReturnAllocation);
            }
        }

        /// <summary>
        /// Returns a transient allocation to this page.
        /// </summary>
        /// <param name="returnedAllocation">The returned allocation.</param>
        private void ReturnAllocation(D3D12ResourceAllocation returnedAllocation) {
            lock (this._lock) {
                this._activeAllocations--;
                if (this._activeAllocations == 0 && this._offset >= UploadRingPageSize) {
                    this._offset = 0;
                }
            }
        }

        /// <summary>
        /// Releases the backing allocation.
        /// </summary>
        public void Dispose() {
            this._allocation.Dispose();
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
