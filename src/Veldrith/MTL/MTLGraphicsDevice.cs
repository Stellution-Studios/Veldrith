using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlGraphicsDevice.
/// </summary>
internal unsafe class MtlGraphicsDevice : GraphicsDevice {

    /// <summary>
    /// Stores the unaligned buffer copy pipeline mac os name state used by this instance.
    /// </summary>
    private const string _unaligned_buffer_copy_pipeline_mac_os_name = "MTL_UnalignedBufferCopy_macOS";

    /// <summary>
    /// Stores the unaligned buffer copy pipelinei os name state used by this instance.
    /// </summary>
    private const string _unaligned_buffer_copy_pipelinei_os_name = "MTL_UnalignedBufferCopy_iOS";

    /// <summary>
    /// Stores the s is supported state used by this instance.
    /// </summary>

    private static readonly Lazy<bool> _s_is_supported = new(GetIsSupported);

    /// <summary>
    /// Synchronizes access to the s aot registered blocks state.
    /// </summary>
    private static readonly Dictionary<IntPtr, MtlGraphicsDevice> _s_aot_registered_blocks = new();

    /// <summary>
    /// Stores the command queue state used by this instance.
    /// </summary>
    private readonly MTLCommandQueue _commandQueue;

    /// <summary>
    /// Stores the completion block descriptor state used by this instance.
    /// </summary>
    private readonly IntPtr _completionBlockDescriptor;

    /// <summary>
    /// Stores the completion block literal state used by this instance.
    /// </summary>
    private readonly IntPtr _completionBlockLiteral;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable

    /// <summary>
    /// Stores the completion handler state used by this instance.
    /// </summary>
    private readonly MTLCommandBufferHandler _completionHandler;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable

    /// <summary>
    /// Stores the completion handler func ptr state used by this instance.
    /// </summary>
    private readonly IntPtr _completionHandlerFuncPtr;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable

    /// <summary>
    /// Stores the concrete global block state used by this instance.
    /// </summary>
    private readonly IntPtr _concreteGlobalBlock;

    /// <summary>
    /// Stores the device state used by this instance.
    /// </summary>
    private readonly MTLDevice _device;

    /// <summary>
    /// Stores the display link state used by this instance.
    /// </summary>
    private readonly IMtlDisplayLink _displayLink;

    /// <summary>
    /// Stores the frame ended event state used by this instance.
    /// </summary>

    private readonly EventWaitHandle _frameEndedEvent = new(true, EventResetMode.ManualReset);

    /// <summary>
    /// Stores the lib system state used by this instance.
    /// </summary>
    private readonly IntPtr _libSystem;

    /// <summary>
    /// Stores the main swapchain state used by this instance.
    /// </summary>
    private readonly MtlSwapchain _mainSwapchain;

    /// <summary>
    /// Stores the metal info state used by this instance.
    /// </summary>
    private readonly BackendInfoMetal _metalInfo;

    /// <summary>
    /// Stores the next frame ready event state used by this instance.
    /// </summary>
    private readonly AutoResetEvent _nextFrameReadyEvent;

    /// <summary>
    /// Stores the reset events collection used by this instance.
    /// </summary>
    private readonly List<ManualResetEvent[]> _resetEvents = new();

    /// <summary>
    /// Synchronizes access to the reset events lock state.
    /// </summary>
    private readonly object _resetEventsLock = new();

    /// <summary>
    /// Stores the submitted cls state used by this instance.
    /// </summary>
    private readonly CommandBufferUsageList<MtlCommandList> _submittedCLs = new();

    /// <summary>
    /// Stores reusable staging buffers used by immediate private-buffer uploads.
    /// </summary>
    private readonly List<MtlBuffer> _availableImmediateUploadBuffers = new();

    /// <summary>
    /// Stores staging buffers referenced by in-flight immediate upload command buffers.
    /// </summary>
    private readonly CommandBufferUsageList<MtlBuffer> _submittedImmediateUploadBuffers = new();

    /// <summary>
    /// Synchronizes access to the submitted commands lock state.
    /// </summary>
    private readonly object _submittedCommandsLock = new();

    /// <summary>
    /// Stores the supported sample counts value used during command execution.
    /// </summary>
    private readonly bool[] _supportedSampleCounts;

    /// <summary>
    /// Synchronizes access to the unaligned buffer copy pipeline lock state.
    /// </summary>
    private readonly object _unalignedBufferCopyPipelineLock = new();

    /// <summary>
    /// Stores the latest submitted cb state used by this instance.
    /// </summary>
    private MTLCommandBuffer _latestSubmittedCb;

    /// <summary>
    /// Stores the command buffer currently collecting immediate private-buffer uploads.
    /// </summary>
    private MTLCommandBuffer _immediateUploadCb;

    /// <summary>
    /// Stores the blit encoder currently collecting immediate private-buffer uploads.
    /// </summary>
    private MTLBlitCommandEncoder _immediateUploadBce;

    /// <summary>
    /// Stores the unaligned buffer copy pipeline state used by this instance.
    /// </summary>
    private MTLComputePipelineState _unalignedBufferCopyPipeline;

    /// <summary>
    /// Stores the unaligned buffer copy shader state used by this instance.
    /// </summary>
    private MtlShader _unalignedBufferCopyShader;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlGraphicsDevice" /> type.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="swapchainDesc">The swapchain desc value used by this operation.</param>
    public MtlGraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? swapchainDesc)
        : this(options, swapchainDesc, new MetalDeviceOptions()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlGraphicsDevice" /> type.
    /// </summary>
    /// <param name="options">The options used to configure this operation.</param>
    /// <param name="swapchainDesc">The swapchain desc value used by this operation.</param>
    /// <param name="metalOptions">The metal options value used by this operation.</param>
    public MtlGraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? swapchainDesc, MetalDeviceOptions metalOptions) {
        this._device = MTLDevice.MTLCreateSystemDefaultDevice();
        this.DeviceName = this._device.name;
        this.MetalFeatures = new MtlFeatureSupport(this._device);

        int major = (int)this.MetalFeatures.MaxFeatureSet / 10000;
        int minor = (int)this.MetalFeatures.MaxFeatureSet % 10000;
        this.ApiVersion = new GraphicsApiVersion(major, minor, 0, 0);

        // TODO: Should be macOS 10.11+ and iOS 11.0+.
        this.Features = new GraphicsDeviceFeatures(true, false, false, this.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3), false, this.MetalFeatures.IsDrawBaseVertexInstanceSupported(), this.MetalFeatures.IsDrawBaseVertexInstanceSupported(), true, true, true, true, true, true, true, true, true, true, true, true, false);
        this.ResourceBindingModel = options.ResourceBindingModel;
        this.PreferMemorylessDepthTargets = metalOptions.PreferMemorylessDepthTargets;

        if (this.MetalFeatures.IsMacOS) {
            this._libSystem = NativeLibrary.Load("libSystem.dylib");
            this._concreteGlobalBlock = NativeLibrary.GetExport(this._libSystem, "_NSConcreteGlobalBlock");
            this._completionHandler = this.OnCommandBufferCompleted;
            this._displayLink = new MtlcvDisplayLink();
        }
        else {
            this._concreteGlobalBlock = IntPtr.Zero;
            this._completionHandler = OnCommandBufferCompleted_Static;
        }

        if (this._displayLink != null) {
            this._nextFrameReadyEvent = new AutoResetEvent(true);
            this._displayLink.Callback += this.OnDisplayLinkCallback;
        }

        this._completionHandlerFuncPtr = Marshal.GetFunctionPointerForDelegate(this._completionHandler);
        this._completionBlockDescriptor = Marshal.AllocHGlobal(Unsafe.SizeOf<BlockDescriptor>());
        BlockDescriptor* descriptorPtr = (BlockDescriptor*)this._completionBlockDescriptor;
        descriptorPtr->Reserved = 0;
        descriptorPtr->BlockSize = (ulong)Unsafe.SizeOf<BlockDescriptor>();

        this._completionBlockLiteral = Marshal.AllocHGlobal(Unsafe.SizeOf<BlockLiteral>());
        BlockLiteral* blockPtr = (BlockLiteral*)this._completionBlockLiteral;
        blockPtr->Isa = this._concreteGlobalBlock;
        blockPtr->Flags = (1 << 28) | (1 << 29);
        blockPtr->Invoke = this._completionHandlerFuncPtr;
        blockPtr->Descriptor = descriptorPtr;

        if (!this.MetalFeatures.IsMacOS) {
            lock (_s_aot_registered_blocks) {
                _s_aot_registered_blocks.Add(this._completionBlockLiteral, this);
            }
        }

        this.ResourceFactory = new MtlResourceFactory(this);
        this._commandQueue = this._device.NewCommandQueue();

        TextureSampleCount[] allSampleCounts = (TextureSampleCount[])Enum.GetValues(typeof(TextureSampleCount));
        this._supportedSampleCounts = new bool[allSampleCounts.Length];

        for (int i = 0; i < allSampleCounts.Length; i++) {
            TextureSampleCount count = allSampleCounts[i];
            uint uintValue = FormatHelpers.GetSampleCountUInt32(count);
            if (this._device.SupportsTextureSampleCount(uintValue)) {
                this._supportedSampleCounts[i] = true;
            }
        }

        if (swapchainDesc != null) {
            SwapchainDescription desc = swapchainDesc.Value;
            this._mainSwapchain = new MtlSwapchain(this, ref desc);
        }

        this._metalInfo = new BackendInfoMetal(this);

        this.PostDeviceCreated();
    }

    /// <summary>
    /// Stores the device state used by this instance.
    /// </summary>
    public MTLDevice Device => this._device;

    /// <summary>
    /// Stores the command queue state used by this instance.
    /// </summary>
    public MTLCommandQueue CommandQueue => this._commandQueue;

    /// <summary>
    /// Gets or sets MetalFeatures.
    /// </summary>
    public MtlFeatureSupport MetalFeatures { get; }

    /// <summary>
    /// Gets or sets ResourceBindingModel.
    /// </summary>
    public ResourceBindingModel ResourceBindingModel { get; }

    /// <summary>
    /// Gets or sets PreferMemorylessDepthTargets.
    /// </summary>
    public bool PreferMemorylessDepthTargets { get; }

    /// <summary>
    /// Gets or sets DeviceName.
    /// </summary>
    public override string DeviceName { get; }

    /// <summary>
    /// Gets or sets VendorName.
    /// </summary>
    public override string VendorName => "Apple";

    /// <summary>
    /// Gets or sets ApiVersion.
    /// </summary>
    public override GraphicsApiVersion ApiVersion { get; }

    /// <summary>
    /// Gets or sets BackendType.
    /// </summary>
    public override GraphicsBackend BackendType => GraphicsBackend.Metal;

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
    public override bool IsClipSpaceYInverted => false;

    /// <summary>
    /// Gets or sets ResourceFactory.
    /// </summary>
    public override ResourceFactory ResourceFactory { get; }

    /// <summary>
    /// Gets or sets MainSwapchain.
    /// </summary>
    public override Swapchain MainSwapchain => this._mainSwapchain;

    /// <summary>
    /// Gets or sets Features.
    /// </summary>
    public override GraphicsDeviceFeatures Features { get; }

    /// <summary>
    /// Updates the active display state for this command sequence.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="w">The w value used by this operation.</param>
    /// <param name="h">The h value used by this operation.</param>
    public override void UpdateActiveDisplay(int x, int y, int w, int h) {
        this._displayLink?.UpdateActiveDisplay(x, y, w, h);
    }

    /// <summary>
    /// Gets the actual refresh period value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override double GetActualRefreshPeriod() {
        if (this._displayLink != null) {
            return this._displayLink.GetActualOutputVideoRefreshPeriod();
        }

        return -1.0f;
    }

    /// <summary>
    /// Gets the sample count limit value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="depthFormat">The depth format value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat) {
        for (int i = this._supportedSampleCounts.Length - 1; i >= 0; i--) {
            if (this._supportedSampleCounts[i]) {
                return (TextureSampleCount)i;
            }
        }

        return TextureSampleCount.Count1;
    }

    /// <summary>
    /// Gets the metal info value.
    /// </summary>
    /// <param name="info">The info value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool GetMetalInfo(out BackendInfoMetal info) {
        info = this._metalInfo;
        return true;
    }

    /// <summary>
    /// Executes the wait for fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool WaitForFence(Fence fence, ulong nanosecondTimeout) {
        return Util.AssertSubtype<Fence, MtlFence>(fence).Wait(nanosecondTimeout);
    }

    /// <summary>
    /// Executes the wait for fences logic for this backend.
    /// </summary>
    /// <param name="fences">The synchronization fence used by this operation.</param>
    /// <param name="waitAll">The wait all value used by this operation.</param>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public override bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout) {
        int msTimeout;
        if (nanosecondTimeout == ulong.MaxValue) {
            msTimeout = -1;
        }
        else {
            msTimeout = (int)Math.Min(nanosecondTimeout / 1_000_000, int.MaxValue);
        }

        ManualResetEvent[] events = this.GetResetEventArray(fences.Length);
        for (int i = 0; i < fences.Length; i++) {
            events[i] = Util.AssertSubtype<Fence, MtlFence>(fences[i]).ResetEvent;
        }

        bool result;

        if (waitAll) {
            result = WaitHandle.WaitAll(events.Cast<WaitHandle>().ToArray(), msTimeout);
        }
        else {
            int index = WaitHandle.WaitAny(events.Cast<WaitHandle>().ToArray(), msTimeout);
            result = index != WaitHandle.WaitTimeout;
        }

        this.ReturnResetEventArray(events);

        return result;
    }

    /// <summary>
    /// Executes the reset fence logic for this backend.
    /// </summary>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    public override void ResetFence(Fence fence) {
        Util.AssertSubtype<Fence, MtlFence>(fence).Reset();
    }

    /// <summary>
    /// Executes the is supported logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal static bool IsSupported() {
        return _s_is_supported.Value;
    }

    /// <summary>
    /// Gets the unaligned buffer copy pipeline value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal MTLComputePipelineState GetUnalignedBufferCopyPipeline() {
        lock (this._unalignedBufferCopyPipelineLock) {
            if (this._unalignedBufferCopyPipeline.IsNull) {
                MTLComputePipelineDescriptor descriptor = MTLUtil.AllocInit<MTLComputePipelineDescriptor>(nameof(MTLComputePipelineDescriptor));
                MTLPipelineBufferDescriptor buffer0 = descriptor.Buffers[0];
                buffer0.mutability = MTLMutability.Mutable;
                MTLPipelineBufferDescriptor buffer1 = descriptor.Buffers[1];
                buffer1.mutability = MTLMutability.Mutable;

                Debug.Assert(this._unalignedBufferCopyShader == null);
                string name = this.MetalFeatures.IsMacOS
                    ? _unaligned_buffer_copy_pipeline_mac_os_name
                    : _unaligned_buffer_copy_pipelinei_os_name;

                using (Stream resourceStream = typeof(MtlGraphicsDevice).Assembly.GetManifestResourceStream(name)!) {
                    byte[] data = new byte[resourceStream.Length];

                    using (MemoryStream ms = new(data)) {
                        resourceStream.CopyTo(ms);
                        ShaderDescription shaderDesc = new(ShaderStages.Compute, data, "copy_bytes");
                        this._unalignedBufferCopyShader = new MtlShader(ref shaderDesc, this);
                    }
                }

                descriptor.ComputeFunction = this._unalignedBufferCopyShader.Function;
                this._unalignedBufferCopyPipeline = this._device.NewComputePipelineStateWithDescriptor(descriptor);
                ObjectiveCRuntime.Release(descriptor.NativePtr);
            }

            return this._unalignedBufferCopyPipeline;
        }
    }

    /// <summary>
    /// Gets the uniform buffer min offset alignment core value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal override uint GetUniformBufferMinOffsetAlignmentCore() {
        return this.MetalFeatures.IsMacOS ? 16u : 256u;
    }

    /// <summary>
    /// Gets the structured buffer min offset alignment core value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal override uint GetStructuredBufferMinOffsetAlignmentCore() {
        return 16u;
    }

    /// <summary>
    /// Maps the core resource for CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    protected override MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource) {
        if (resource is MtlBuffer buffer) {
            return this.MapBuffer(buffer, mode);
        }

        MtlTexture texture = Util.AssertSubtype<IMappableResource, MtlTexture>(resource);
        return this.MapTexture(texture, mode, subresource);
    }

    /// <summary>
    /// Executes the platform dispose logic for this backend.
    /// </summary>
    protected override void PlatformDispose() {
        this.WaitForIdle();

        if (!this._unalignedBufferCopyPipeline.IsNull) {
            this._unalignedBufferCopyShader.Dispose();
            ObjectiveCRuntime.Release(this._unalignedBufferCopyPipeline.NativePtr);
        }

        this._mainSwapchain?.Dispose();

        lock (this._submittedCommandsLock) {
            foreach (MtlBuffer buffer in this._availableImmediateUploadBuffers) {
                buffer.Dispose();
            }
            this._availableImmediateUploadBuffers.Clear();

            foreach (MtlBuffer buffer in this._submittedImmediateUploadBuffers.EnumerateItems()) {
                buffer.Dispose();
            }
            this._submittedImmediateUploadBuffers.Clear();
        }

        ObjectiveCRuntime.Release(this._commandQueue.NativePtr);
        ObjectiveCRuntime.Release(this._device.NativePtr);

        lock (_s_aot_registered_blocks) {
            _s_aot_registered_blocks.Remove(this._completionBlockLiteral);
        }

        NativeLibrary.Free(this._libSystem);

        Marshal.FreeHGlobal(this._completionBlockDescriptor);
        Marshal.FreeHGlobal(this._completionBlockLiteral);

        this._displayLink?.Dispose();
    }

    /// <summary>
    /// Unmaps the core resource from CPU access.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    protected override void UnmapCore(IMappableResource resource, uint subresource) { }

    // Xamarin AOT requires native callbacks be static.
    [MonoPInvokeCallback(typeof(MTLCommandBufferHandler))]

    /// <summary>
    /// Executes the on command buffer completed static logic for this backend.
    /// </summary>
    /// <param name="block">The block value used by this operation.</param>
    /// <param name="cb">The cb value used by this operation.</param>
    private static void OnCommandBufferCompleted_Static(IntPtr block, MTLCommandBuffer cb) {
        lock (_s_aot_registered_blocks) {
            if (_s_aot_registered_blocks.TryGetValue(block, out MtlGraphicsDevice gd)) {
                gd.OnCommandBufferCompleted(block, cb);
            }
        }
    }

    /// <summary>
    /// Gets the is supported value.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private static bool GetIsSupported() {
        bool result = false;

        try {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                if (RuntimeInformation.OSDescription.Contains("Darwin")) {
                    NSArray allDevices = MTLDevice.MTLCopyAllDevices();
                    result |= (ulong)allDevices.Count > 0;
                    ObjectiveCRuntime.Release(allDevices.NativePtr);
                }
                else {
                    MTLDevice defaultDevice = MTLDevice.MTLCreateSystemDefaultDevice();

                    if (defaultDevice.NativePtr != IntPtr.Zero) {
                        result = true;
                        ObjectiveCRuntime.Release(defaultDevice.NativePtr);
                    }
                }
            }
        }
        catch {
            result = false;
        }

        return result;
    }

    /// <summary>
    /// Executes the on command buffer completed logic for this backend.
    /// </summary>
    /// <param name="block">The block value used by this operation.</param>
    /// <param name="cb">The cb value used by this operation.</param>
    private void OnCommandBufferCompleted(IntPtr block, MTLCommandBuffer cb) {
        lock (this._submittedCommandsLock) {
            foreach (MtlCommandList cl in this._submittedCLs.EnumerateAndRemove(cb)) {
                cl.OnCompleted(cb);
            }

            foreach (MtlBuffer stagingBuffer in this._submittedImmediateUploadBuffers.EnumerateAndRemove(cb)) {
                this._availableImmediateUploadBuffers.Add(stagingBuffer);
            }

            if (this._latestSubmittedCb.NativePtr == cb.NativePtr) {
                this._latestSubmittedCb = default;
            }
        }

        ObjectiveCRuntime.Release(cb.NativePtr);
    }

    /// <summary>
    /// Executes the on display link callback logic for this backend.
    /// </summary>
    private void OnDisplayLinkCallback() {
        this._nextFrameReadyEvent.Set();
        this._frameEndedEvent.WaitOne();
    }

    /// <summary>
    /// Maps the buffer resource for CPU access.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private MappedResource MapBuffer(MtlBuffer buffer, MapMode mode) {
        if (mode == MapMode.Write && (buffer.Usage & BufferUsage.Staging) == 0) {
            this.WaitForPendingGpuWorkBeforeSharedBufferWrite();
        }

        return new MappedResource(buffer, mode, (IntPtr)buffer.Pointer, buffer.SizeInBytes, 0, buffer.SizeInBytes, buffer.SizeInBytes);
    }

    /// <summary>
    /// Maps the texture resource for CPU access.
    /// </summary>
    /// <param name="texture">The texture resource involved in this operation.</param>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private MappedResource MapTexture(MtlTexture texture, MapMode mode, uint subresource) {
        Debug.Assert(!texture.StagingBuffer.IsNull);
        void* data = texture.StagingBufferPointer;
        Util.GetMipLevelAndArrayLayer(texture, subresource, out uint mipLevel, out uint arrayLayer);
        Util.GetMipDimensions(texture, mipLevel, out uint _, out uint _, out uint _);
        uint subresourceSize = texture.GetSubresourceSize(mipLevel, arrayLayer);
        texture.GetSubresourceLayout(mipLevel, arrayLayer, out uint rowPitch, out uint depthPitch);
        ulong offset = Util.ComputeSubresourceOffset(texture, mipLevel, arrayLayer);
        byte* offsetPtr = (byte*)data + offset;
        return new MappedResource(texture, mode, (IntPtr)offsetPtr, subresourceSize, subresource, rowPitch, depthPitch);
    }

    /// <summary>
    /// Gets the reset event array value.
    /// </summary>
    /// <param name="length">The number of items involved in this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private ManualResetEvent[] GetResetEventArray(int length) {
        lock (this._resetEventsLock) {
            for (int i = this._resetEvents.Count - 1; i > 0; i--) {
                ManualResetEvent[] array = this._resetEvents[i];

                if (array.Length == length) {
                    this._resetEvents.RemoveAt(i);
                    return array;
                }
            }
        }

        ManualResetEvent[] newArray = new ManualResetEvent[length];
        return newArray;
    }

    /// <summary>
    /// Executes the return reset event array logic for this backend.
    /// </summary>
    /// <param name="array">The array value used by this operation.</param>
    private void ReturnResetEventArray(ManualResetEvent[] array) {
        lock (this._resetEventsLock) {
            this._resetEvents.Add(array);
        }
    }

    /// <summary>
    /// Executes the submit commands core logic for this backend.
    /// </summary>
    /// <param name="commandList">The command list used by this operation.</param>
    /// <param name="fence">The synchronization fence used by this operation.</param>
    private protected override void SubmitCommandsCore(CommandList commandList, Fence fence) {
        MtlCommandList mtlCl = Util.AssertSubtype<CommandList, MtlCommandList>(commandList);

        mtlCl.CommandBuffer.AddCompletedHandler(this._completionBlockLiteral);

        lock (this._submittedCommandsLock) {
            this.FlushPendingImmediateBufferUploads_NoLock();

            if (fence != null) {
                mtlCl.SetCompletionFence(mtlCl.CommandBuffer, Util.AssertSubtype<Fence, MtlFence>(fence));
            }

            this._submittedCLs.Add(mtlCl.CommandBuffer, mtlCl);
            this._latestSubmittedCb = mtlCl.Commit();
        }
    }

    /// <summary>
    /// Executes the wait for next frame ready core logic for this backend.
    /// </summary>
    private protected override void WaitForNextFrameReadyCore() {
        this._frameEndedEvent.Reset();
        this._nextFrameReadyEvent?.WaitOne(TimeSpan.FromSeconds(1)); // Should never time out.

        // in iOS, if one frame takes longer than the next V-Sync request, the next frame will be processed immediately rather than being delayed to a subsequent V-Sync request,
        // therefore we will request the next drawable here as a method of waiting until we're ready to draw the next frame.
        if (!this.MetalFeatures.IsMacOS) {
            MtlSwapchainFramebuffer mtlSwapchainFramebuffer = Util.AssertSubtype<Framebuffer, MtlSwapchainFramebuffer>(this._mainSwapchain.Framebuffer);
            mtlSwapchainFramebuffer.EnsureDrawableAvailable();
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
        if (!MtlFormats.IsFormatSupported(format, usage, this.MetalFeatures)) {
            properties = default;
            return false;
        }

        uint sampleCounts = 0;

        for (int i = 0; i < this._supportedSampleCounts.Length; i++) {
            if (this._supportedSampleCounts[i]) {
                sampleCounts |= (uint)(1 << i);
            }
        }

        MTLFeatureSet maxFeatureSet = this.MetalFeatures.MaxFeatureSet;
        uint maxArrayLayer = MtlFormats.GetMaxTextureVolume(maxFeatureSet);
        uint maxWidth;
        uint maxHeight;
        uint maxDepth;

        if (type == TextureType.Texture1D) {
            maxWidth = MtlFormats.GetMaxTexture1DWidth(maxFeatureSet);
            maxHeight = 1;
            maxDepth = 1;
        }
        else if (type == TextureType.Texture2D) {
            uint maxDimensions = (usage & TextureUsage.Cubemap) != 0
                ? MtlFormats.GetMaxTextureCubeDimensions(maxFeatureSet)
                : MtlFormats.GetMaxTexture2DDimensions(maxFeatureSet);

            maxWidth = maxDimensions;
            maxHeight = maxDimensions;
            maxDepth = 1;
        }
        else if (type == TextureType.Texture3D) {
            maxWidth = maxArrayLayer;
            maxHeight = maxArrayLayer;
            maxDepth = maxArrayLayer;
            maxArrayLayer = 1;
        }
        else {
            throw Illegal.Value<TextureType>();
        }

        properties = new PixelFormatProperties(maxWidth, maxHeight, maxDepth, uint.MaxValue, maxArrayLayer, sampleCounts);
        return true;
    }

    /// <summary>
    /// Executes the swap buffers core logic for this backend.
    /// </summary>
    /// <param name="swapchain">The swapchain used by this operation.</param>
    private protected override void SwapBuffersCore(Swapchain swapchain) {
        this.FlushPendingImmediateBufferUploads();

        MtlSwapchain mtlSc = Util.AssertSubtype<Swapchain, MtlSwapchain>(swapchain);
        IntPtr currentDrawablePtr = mtlSc.CurrentDrawable.NativePtr;

        if (currentDrawablePtr != IntPtr.Zero) {
            using (NSAutoreleasePool.Begin()) {
                MTLCommandBuffer submitCb = this._commandQueue.CommandBuffer();
                submitCb.PresentDrawable(currentDrawablePtr);
                submitCb.Commit();
            }

            mtlSc.InvalidateDrawable();
        }

        this._frameEndedEvent.Set();
    }

    /// <summary>
    /// Updates the buffer core state for this command sequence.
    /// </summary>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <param name="bufferOffsetInBytes">The byte offset used by this operation.</param>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
        void* destPtr = mtlBuffer.Pointer;

        if (destPtr != null) {
            if ((buffer.Usage & BufferUsage.Staging) == 0) {
                this.WaitForPendingGpuWorkBeforeSharedBufferWrite();
            }

            byte* destOffsetPtr = (byte*)destPtr + bufferOffsetInBytes;
            Unsafe.CopyBlock(destOffsetPtr, source.ToPointer(), sizeInBytes);
            return;
        }

        bool isBlitAligned = (bufferOffsetInBytes & 3) == 0;
        if (!isBlitAligned) {
            this.UploadPrivateBufferViaOneShotCommandList(buffer, bufferOffsetInBytes, source, sizeInBytes);
            return;
        }

        lock (this._submittedCommandsLock) {
            this.EnsureImmediateUploadEncoder_NoLock();
            MtlBuffer staging = this.GetFreeImmediateUploadBuffer_NoLock(sizeInBytes);
            this.UpdateBuffer(staging, 0, source, sizeInBytes);
            uint copySize = sizeInBytes + ((4 - sizeInBytes % 4) % 4);
            this._immediateUploadBce.Copy(staging.DeviceBuffer, UIntPtr.Zero, mtlBuffer.DeviceBuffer, bufferOffsetInBytes, copySize);
            this._submittedImmediateUploadBuffers.Add(this._immediateUploadCb, staging);
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
        MtlTexture mtlTex = Util.AssertSubtype<Texture, MtlTexture>(texture);

        if (mtlTex.StagingBuffer.IsNull) {
            Texture stagingTex = this.ResourceFactory.CreateTexture(new TextureDescription(width, height, depth, 1, 1, texture.Format, TextureUsage.Staging, texture.Type));
            this.UpdateTexture(stagingTex, source, sizeInBytes, 0, 0, 0, width, height, depth, 0, 0);
            CommandList cl = this.ResourceFactory.CreateCommandList();
            cl.Begin();
            cl.CopyTexture(stagingTex, 0, 0, 0, 0, 0, texture, x, y, z, mipLevel, arrayLayer, width, height, depth, 1);
            cl.End();
            this.SubmitCommands(cl);

            cl.Dispose();
            stagingTex.Dispose();
        }
        else {
            mtlTex.GetSubresourceLayout(mipLevel, arrayLayer, out uint dstRowPitch, out uint dstDepthPitch);
            ulong dstOffset = Util.ComputeSubresourceOffset(mtlTex, mipLevel, arrayLayer);
            uint srcRowPitch = FormatHelpers.GetRowPitch(width, texture.Format);
            uint srcDepthPitch = FormatHelpers.GetDepthPitch(srcRowPitch, height, texture.Format);
            Util.CopyTextureRegion(source.ToPointer(), 0, 0, 0, srcRowPitch, srcDepthPitch, (byte*)mtlTex.StagingBufferPointer + dstOffset, x, y, z, dstRowPitch, dstDepthPitch, width, height, depth, texture.Format);
        }
    }

    /// <summary>
    /// Executes the wait for idle core logic for this backend.
    /// </summary>
    private protected override void WaitForIdleCore() {
        MTLCommandBuffer lastCb;

        lock (this._submittedCommandsLock) {
            this.FlushPendingImmediateBufferUploads_NoLock();
            lastCb = this._latestSubmittedCb;
            ObjectiveCRuntime.Retain(lastCb.NativePtr);
        }

        if (lastCb.NativePtr != IntPtr.Zero && lastCb.Status != MTLCommandBufferStatus.Completed) {
            lastCb.WaitUntilCompleted();
        }

        ObjectiveCRuntime.Release(lastCb.NativePtr);
    }

    /// <summary>
    /// Flushes pending immediate private-buffer uploads.
    /// </summary>
    private void FlushPendingImmediateBufferUploads() {
        lock (this._submittedCommandsLock) {
            this.FlushPendingImmediateBufferUploads_NoLock();
        }
    }

    /// <summary>
    /// Flushes pending immediate private-buffer uploads.
    /// </summary>
    private void FlushPendingImmediateBufferUploads_NoLock() {
        if (this._immediateUploadCb.NativePtr == IntPtr.Zero) {
            return;
        }

        if (this._immediateUploadBce.NativePtr != IntPtr.Zero) {
            this._immediateUploadBce.EndEncoding();
            this._immediateUploadBce = default;
        }

        this._immediateUploadCb.AddCompletedHandler(this._completionBlockLiteral);
        this._immediateUploadCb.Commit();
        this._latestSubmittedCb = this._immediateUploadCb;
        this._immediateUploadCb = default;
    }

    /// <summary>
    /// Waits until in-flight GPU work can no longer read from CPU-writable shared buffers.
    /// </summary>
    private void WaitForPendingGpuWorkBeforeSharedBufferWrite() {
        MTLCommandBuffer lastCb;

        lock (this._submittedCommandsLock) {
            this.FlushPendingImmediateBufferUploads_NoLock();
            lastCb = this._latestSubmittedCb;

            if (lastCb.NativePtr != IntPtr.Zero) {
                ObjectiveCRuntime.Retain(lastCb.NativePtr);
            }
        }

        if (lastCb.NativePtr == IntPtr.Zero) {
            return;
        }

        try {
            if (lastCb.Status != MTLCommandBufferStatus.Completed) {
                lastCb.WaitUntilCompleted();
            }
        }
        finally {
            ObjectiveCRuntime.Release(lastCb.NativePtr);
        }
    }

    /// <summary>
    /// Ensures the immediate private-buffer upload encoder is available.
    /// </summary>
    private void EnsureImmediateUploadEncoder_NoLock() {
        if (this._immediateUploadCb.NativePtr == IntPtr.Zero) {
            using (NSAutoreleasePool.Begin()) {
                this._immediateUploadCb = this._commandQueue.CommandBuffer();
                ObjectiveCRuntime.Retain(this._immediateUploadCb.NativePtr);
            }
        }

        if (this._immediateUploadBce.IsNull) {
            this._immediateUploadBce = this._immediateUploadCb.BlitCommandEncoder();
        }
    }

    /// <summary>
    /// Gets a reusable shared staging buffer for immediate private-buffer uploads.
    /// </summary>
    private MtlBuffer GetFreeImmediateUploadBuffer_NoLock(uint sizeInBytes) {
        for (int i = 0; i < this._availableImmediateUploadBuffers.Count; i++) {
            MtlBuffer buffer = this._availableImmediateUploadBuffers[i];
            if (buffer.SizeInBytes >= sizeInBytes) {
                this._availableImmediateUploadBuffers.RemoveAt(i);
                return buffer;
            }
        }

        DeviceBuffer staging = this.ResourceFactory.CreateBuffer(new BufferDescription(sizeInBytes, BufferUsage.Staging));
        return Util.AssertSubtype<DeviceBuffer, MtlBuffer>(staging);
    }

    /// <summary>
    /// Uploads a private Metal buffer update through a one-shot command list.
    /// </summary>
    private void UploadPrivateBufferViaOneShotCommandList(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        CommandList cl = this.ResourceFactory.CreateCommandList();
        cl.Begin();
        cl.UpdateBuffer(buffer, bufferOffsetInBytes, source, sizeInBytes);
        cl.End();
        this.SubmitCommands(cl);
        cl.Dispose();
    }
}

/// <summary>
/// Represents the MonoPInvokeCallbackAttribute type used by the graphics runtime.
/// </summary>
internal sealed class MonoPInvokeCallbackAttribute : Attribute {

    /// <summary>
    /// Initializes a new instance of the <see cref="MonoPInvokeCallbackAttribute" /> type.
    /// </summary>
    /// <param name="t">The t value used by this operation.</param>
    public MonoPInvokeCallbackAttribute(Type t) { }
}
