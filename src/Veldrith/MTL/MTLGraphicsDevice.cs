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
/// Represents the MtlGraphicsDevice class.
/// </summary>
internal unsafe class MtlGraphicsDevice : GraphicsDevice {

    /// <summary>
    /// Represents the _unaligned_buffer_copy_pipeline_mac_os_name field.
    /// </summary>
    private const string _unaligned_buffer_copy_pipeline_mac_os_name = "MTL_UnalignedBufferCopy_macOS";

    /// <summary>
    /// Represents the _unaligned_buffer_copy_pipelinei_os_name field.
    /// </summary>
    private const string _unaligned_buffer_copy_pipelinei_os_name = "MTL_UnalignedBufferCopy_iOS";

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="GetIsSupported">The value of GetIsSupported.</param>
    /// <returns>The result of the new operation.</returns>
    private static readonly Lazy<bool> _s_is_supported = new(GetIsSupported);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private static readonly Dictionary<IntPtr, MtlGraphicsDevice> _s_aot_registered_blocks = new();

    /// <summary>
    /// Represents the _commandQueue field.
    /// </summary>
    private readonly MTLCommandQueue _commandQueue;

    /// <summary>
    /// Represents the _completionBlockDescriptor field.
    /// </summary>
    private readonly IntPtr _completionBlockDescriptor;

    /// <summary>
    /// Represents the _completionBlockLiteral field.
    /// </summary>
    private readonly IntPtr _completionBlockLiteral;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable

    /// <summary>
    /// Represents the _completionHandler field.
    /// </summary>
    private readonly MTLCommandBufferHandler _completionHandler;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable

    /// <summary>
    /// Represents the _completionHandlerFuncPtr field.
    /// </summary>
    private readonly IntPtr _completionHandlerFuncPtr;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable

    /// <summary>
    /// Represents the _concreteGlobalBlock field.
    /// </summary>
    private readonly IntPtr _concreteGlobalBlock;

    /// <summary>
    /// Represents the _device field.
    /// </summary>
    private readonly MTLDevice _device;

    /// <summary>
    /// Represents the _displayLink field.
    /// </summary>
    private readonly IMtlDisplayLink _displayLink;

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="true">The value of true.</param>
    /// <param name="ManualReset">The value of ManualReset.</param>
    /// <returns>The result of the new operation.</returns>
    private readonly EventWaitHandle _frameEndedEvent = new(true, EventResetMode.ManualReset);

    /// <summary>
    /// Represents the _libSystem field.
    /// </summary>
    private readonly IntPtr _libSystem;

    /// <summary>
    /// Represents the _mainSwapchain field.
    /// </summary>
    private readonly MtlSwapchain _mainSwapchain;

    /// <summary>
    /// Represents the _metalInfo field.
    /// </summary>
    private readonly BackendInfoMetal _metalInfo;

    /// <summary>
    /// Represents the _nextFrameReadyEvent field.
    /// </summary>
    private readonly AutoResetEvent _nextFrameReadyEvent;

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly List<ManualResetEvent[]> _resetEvents = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly object _resetEventsLock = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly CommandBufferUsageList<MtlCommandList> _submittedCLs = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly object _submittedCommandsLock = new();

    /// <summary>
    /// Represents the _supportedSampleCounts field.
    /// </summary>
    private readonly bool[] _supportedSampleCounts;

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly object _unalignedBufferCopyPipelineLock = new();

    /// <summary>
    /// Represents the _latestSubmittedCb field.
    /// </summary>
    private MTLCommandBuffer _latestSubmittedCb;

    /// <summary>
    /// Represents the _unalignedBufferCopyPipeline field.
    /// </summary>
    private MTLComputePipelineState _unalignedBufferCopyPipeline;

    /// <summary>
    /// Represents the _unalignedBufferCopyShader field.
    /// </summary>
    private MtlShader _unalignedBufferCopyShader;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlGraphicsDevice" /> type.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="swapchainDesc">The value of swapchainDesc.</param>
    public MtlGraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? swapchainDesc)
        : this(options, swapchainDesc, new MetalDeviceOptions()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlGraphicsDevice" /> type.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="swapchainDesc">The value of swapchainDesc.</param>
    /// <param name="metalOptions">The value of metalOptions.</param>
    public MtlGraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? swapchainDesc, MetalDeviceOptions metalOptions) {
        this._device = MTLDevice.MTLCreateSystemDefaultDevice();
        this.DeviceName = this._device.name;
        this.MetalFeatures = new MtlFeatureSupport(this._device);

        int major = (int)this.MetalFeatures.MaxFeatureSet / 10000;
        int minor = (int)this.MetalFeatures.MaxFeatureSet % 10000;
        this.ApiVersion = new GraphicsApiVersion(major, minor, 0, 0);

        // TODO: Should be macOS 10.11+ and iOS 11.0+.
        this.Features = new GraphicsDeviceFeatures(true, false, false, this.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3), false, this.MetalFeatures.IsDrawBaseVertexInstanceSupported(), this.MetalFeatures.IsDrawBaseVertexInstanceSupported(), true, true, true, true, true, true, true, true, true, true, true, false);
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
        descriptorPtr->reserved = 0;
        descriptorPtr->Block_size = (ulong)Unsafe.SizeOf<BlockDescriptor>();

        this._completionBlockLiteral = Marshal.AllocHGlobal(Unsafe.SizeOf<BlockLiteral>());
        BlockLiteral* blockPtr = (BlockLiteral*)this._completionBlockLiteral;
        blockPtr->isa = this._concreteGlobalBlock;
        blockPtr->flags = (1 << 28) | (1 << 29);
        blockPtr->invoke = this._completionHandlerFuncPtr;
        blockPtr->descriptor = descriptorPtr;

        if (!this.MetalFeatures.IsMacOS) {
            lock (_s_aot_registered_blocks) {
                _s_aot_registered_blocks.Add(this._completionBlockLiteral, this);
            }
        }

        this.ResourceFactory = new MtlResourceFactory(this);
        this._commandQueue = this._device.newCommandQueue();

        TextureSampleCount[] allSampleCounts = (TextureSampleCount[])Enum.GetValues(typeof(TextureSampleCount));
        this._supportedSampleCounts = new bool[allSampleCounts.Length];

        for (int i = 0; i < allSampleCounts.Length; i++) {
            TextureSampleCount count = allSampleCounts[i];
            uint uintValue = FormatHelpers.GetSampleCountUInt32(count);
            if (this._device.supportsTextureSampleCount(uintValue)) {
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
    /// Represents the Device field.
    /// </summary>
    public MTLDevice Device => this._device;

    /// <summary>
    /// Represents the CommandQueue field.
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
    /// Performs the UpdateActiveDisplay operation.
    /// </summary>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="w">The value of w.</param>
    /// <param name="h">The value of h.</param>
    public override void UpdateActiveDisplay(int x, int y, int w, int h) {
        this._displayLink?.UpdateActiveDisplay(x, y, w, h);
    }

    /// <summary>
    /// Performs the GetActualRefreshPeriod operation.
    /// </summary>
    /// <returns>The result of the GetActualRefreshPeriod operation.</returns>
    public override double GetActualRefreshPeriod() {
        if (this._displayLink != null) {
            return this._displayLink.GetActualOutputVideoRefreshPeriod();
        }

        return -1.0f;
    }

    /// <summary>
    /// Performs the GetSampleCountLimit operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="depthFormat">The value of depthFormat.</param>
    /// <returns>The result of the GetSampleCountLimit operation.</returns>
    public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat) {
        for (int i = this._supportedSampleCounts.Length - 1; i >= 0; i--) {
            if (this._supportedSampleCounts[i]) {
                return (TextureSampleCount)i;
            }
        }

        return TextureSampleCount.Count1;
    }

    /// <summary>
    /// Performs the GetMetalInfo operation.
    /// </summary>
    /// <param name="info">The value of info.</param>
    /// <returns>The result of the GetMetalInfo operation.</returns>
    public override bool GetMetalInfo(out BackendInfoMetal info) {
        info = this._metalInfo;
        return true;
    }

    /// <summary>
    /// Performs the WaitForFence operation.
    /// </summary>
    /// <param name="fence">The value of fence.</param>
    /// <param name="nanosecondTimeout">The value of nanosecondTimeout.</param>
    /// <returns>The result of the WaitForFence operation.</returns>
    public override bool WaitForFence(Fence fence, ulong nanosecondTimeout) {
        return Util.AssertSubtype<Fence, MtlFence>(fence).Wait(nanosecondTimeout);
    }

    /// <summary>
    /// Performs the WaitForFences operation.
    /// </summary>
    /// <param name="fences">The value of fences.</param>
    /// <param name="waitAll">The value of waitAll.</param>
    /// <param name="nanosecondTimeout">The value of nanosecondTimeout.</param>
    /// <returns>The result of the WaitForFences operation.</returns>
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
    /// Performs the ResetFence operation.
    /// </summary>
    /// <param name="fence">The value of fence.</param>
    public override void ResetFence(Fence fence) {
        Util.AssertSubtype<Fence, MtlFence>(fence).Reset();
    }

    /// <summary>
    /// Performs the IsSupported operation.
    /// </summary>
    /// <returns>The result of the IsSupported operation.</returns>
    internal static bool IsSupported() {
        return _s_is_supported.Value;
    }

    /// <summary>
    /// Performs the GetUnalignedBufferCopyPipeline operation.
    /// </summary>
    /// <returns>The result of the GetUnalignedBufferCopyPipeline operation.</returns>
    internal MTLComputePipelineState GetUnalignedBufferCopyPipeline() {
        lock (this._unalignedBufferCopyPipelineLock) {
            if (this._unalignedBufferCopyPipeline.IsNull) {
                MTLComputePipelineDescriptor descriptor = MTLUtil.AllocInit<MTLComputePipelineDescriptor>(nameof(MTLComputePipelineDescriptor));
                MTLPipelineBufferDescriptor buffer0 = descriptor.buffers[0];
                buffer0.mutability = MTLMutability.Mutable;
                MTLPipelineBufferDescriptor buffer1 = descriptor.buffers[1];
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

                descriptor.computeFunction = this._unalignedBufferCopyShader.Function;
                this._unalignedBufferCopyPipeline = this._device.newComputePipelineStateWithDescriptor(descriptor);
                ObjectiveCRuntime.release(descriptor.NativePtr);
            }

            return this._unalignedBufferCopyPipeline;
        }
    }

    /// <summary>
    /// Performs the GetUniformBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>The result of the GetUniformBufferMinOffsetAlignmentCore operation.</returns>
    internal override uint GetUniformBufferMinOffsetAlignmentCore() {
        return this.MetalFeatures.IsMacOS ? 16u : 256u;
    }

    /// <summary>
    /// Performs the GetStructuredBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>The result of the GetStructuredBufferMinOffsetAlignmentCore operation.</returns>
    internal override uint GetStructuredBufferMinOffsetAlignmentCore() {
        return 16u;
    }

    /// <summary>
    /// Performs the MapCore operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="mode">The value of mode.</param>
    /// <param name="subresource">The value of subresource.</param>
    /// <returns>The result of the MapCore operation.</returns>
    protected override MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource) {
        if (resource is MtlBuffer buffer) {
            return this.MapBuffer(buffer, mode);
        }

        MtlTexture texture = Util.AssertSubtype<IMappableResource, MtlTexture>(resource);
        return this.MapTexture(texture, mode, subresource);
    }

    /// <summary>
    /// Performs the PlatformDispose operation.
    /// </summary>
    protected override void PlatformDispose() {
        this.WaitForIdle();

        if (!this._unalignedBufferCopyPipeline.IsNull) {
            this._unalignedBufferCopyShader.Dispose();
            ObjectiveCRuntime.release(this._unalignedBufferCopyPipeline.NativePtr);
        }

        this._mainSwapchain?.Dispose();
        ObjectiveCRuntime.release(this._commandQueue.NativePtr);
        ObjectiveCRuntime.release(this._device.NativePtr);

        lock (_s_aot_registered_blocks) {
            _s_aot_registered_blocks.Remove(this._completionBlockLiteral);
        }

        NativeLibrary.Free(this._libSystem);

        Marshal.FreeHGlobal(this._completionBlockDescriptor);
        Marshal.FreeHGlobal(this._completionBlockLiteral);

        this._displayLink?.Dispose();
    }

    /// <summary>
    /// Performs the UnmapCore operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="subresource">The value of subresource.</param>
    protected override void UnmapCore(IMappableResource resource, uint subresource) { }

    // Xamarin AOT requires native callbacks be static.
    [MonoPInvokeCallback(typeof(MTLCommandBufferHandler))]

    /// <summary>
    /// Performs the OnCommandBufferCompleted_Static operation.
    /// </summary>
    /// <param name="block">The value of block.</param>
    /// <param name="cb">The value of cb.</param>
    private static void OnCommandBufferCompleted_Static(IntPtr block, MTLCommandBuffer cb) {
        lock (_s_aot_registered_blocks) {
            if (_s_aot_registered_blocks.TryGetValue(block, out MtlGraphicsDevice gd)) {
                gd.OnCommandBufferCompleted(block, cb);
            }
        }
    }

    /// <summary>
    /// Performs the GetIsSupported operation.
    /// </summary>
    /// <returns>The result of the GetIsSupported operation.</returns>
    private static bool GetIsSupported() {
        bool result = false;

        try {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                if (RuntimeInformation.OSDescription.Contains("Darwin")) {
                    NSArray allDevices = MTLDevice.MTLCopyAllDevices();
                    result |= (ulong)allDevices.count > 0;
                    ObjectiveCRuntime.release(allDevices.NativePtr);
                }
                else {
                    MTLDevice defaultDevice = MTLDevice.MTLCreateSystemDefaultDevice();

                    if (defaultDevice.NativePtr != IntPtr.Zero) {
                        result = true;
                        ObjectiveCRuntime.release(defaultDevice.NativePtr);
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
    /// Performs the OnCommandBufferCompleted operation.
    /// </summary>
    /// <param name="block">The value of block.</param>
    /// <param name="cb">The value of cb.</param>
    private void OnCommandBufferCompleted(IntPtr block, MTLCommandBuffer cb) {
        lock (this._submittedCommandsLock) {
            foreach (MtlCommandList cl in this._submittedCLs.EnumerateAndRemove(cb)) {
                cl.OnCompleted(cb);
            }

            if (this._latestSubmittedCb.NativePtr == cb.NativePtr) {
                this._latestSubmittedCb = default;
            }
        }

        ObjectiveCRuntime.release(cb.NativePtr);
    }

    /// <summary>
    /// Performs the OnDisplayLinkCallback operation.
    /// </summary>
    private void OnDisplayLinkCallback() {
        this._nextFrameReadyEvent.Set();
        this._frameEndedEvent.WaitOne();
    }

    /// <summary>
    /// Performs the MapBuffer operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="mode">The value of mode.</param>
    /// <returns>The result of the MapBuffer operation.</returns>
    private MappedResource MapBuffer(MtlBuffer buffer, MapMode mode) {
        return new MappedResource(buffer, mode, (IntPtr)buffer.Pointer, buffer.SizeInBytes, 0, buffer.SizeInBytes, buffer.SizeInBytes);
    }

    /// <summary>
    /// Performs the MapTexture operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="mode">The value of mode.</param>
    /// <param name="subresource">The value of subresource.</param>
    /// <returns>The result of the MapTexture operation.</returns>
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
    /// Performs the GetResetEventArray operation.
    /// </summary>
    /// <param name="length">The value of length.</param>
    /// <returns>The result of the GetResetEventArray operation.</returns>
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
    /// Performs the ReturnResetEventArray operation.
    /// </summary>
    /// <param name="array">The value of array.</param>
    private void ReturnResetEventArray(ManualResetEvent[] array) {
        lock (this._resetEventsLock) {
            this._resetEvents.Add(array);
        }
    }

    /// <summary>
    /// Performs the SubmitCommandsCore operation.
    /// </summary>
    /// <param name="commandList">The value of commandList.</param>
    /// <param name="fence">The value of fence.</param>
    private protected override void SubmitCommandsCore(CommandList commandList, Fence fence) {
        MtlCommandList mtlCl = Util.AssertSubtype<CommandList, MtlCommandList>(commandList);

        mtlCl.CommandBuffer.addCompletedHandler(this._completionBlockLiteral);

        lock (this._submittedCommandsLock) {
            if (fence != null) {
                mtlCl.SetCompletionFence(mtlCl.CommandBuffer, Util.AssertSubtype<Fence, MtlFence>(fence));
            }

            this._submittedCLs.Add(mtlCl.CommandBuffer, mtlCl);
            this._latestSubmittedCb = mtlCl.Commit();
        }
    }

    /// <summary>
    /// Performs the WaitForNextFrameReadyCore operation.
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
    /// Performs the GetPixelFormatSupportCore operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="type">The value of type.</param>
    /// <param name="usage">The value of usage.</param>
    /// <param name="properties">The value of properties.</param>
    /// <returns>The result of the GetPixelFormatSupportCore operation.</returns>
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
    /// Performs the SwapBuffersCore operation.
    /// </summary>
    /// <param name="swapchain">The value of swapchain.</param>
    private protected override void SwapBuffersCore(Swapchain swapchain) {
        MtlSwapchain mtlSc = Util.AssertSubtype<Swapchain, MtlSwapchain>(swapchain);
        IntPtr currentDrawablePtr = mtlSc.CurrentDrawable.NativePtr;

        if (currentDrawablePtr != IntPtr.Zero) {
            using (NSAutoreleasePool.Begin()) {
                MTLCommandBuffer submitCb = this._commandQueue.commandBuffer();
                submitCb.presentDrawable(currentDrawablePtr);
                submitCb.commit();
            }

            mtlSc.InvalidateDrawable();
        }

        this._frameEndedEvent.Set();
    }

    /// <summary>
    /// Performs the UpdateBufferCore operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="bufferOffsetInBytes">The value of bufferOffsetInBytes.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
        void* destPtr = mtlBuffer.Pointer;
        byte* destOffsetPtr = (byte*)destPtr + bufferOffsetInBytes;

        if (destPtr == null) {
            throw new VeldridException("Attempting to write to a MTLBuffer that is inaccessible from a CPU.");
        }

        Unsafe.CopyBlock(destOffsetPtr, source.ToPointer(), sizeInBytes);
    }

    /// <summary>
    /// Performs the UpdateTextureCore operation.
    /// </summary>
    /// <param name="texture">The value of texture.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    /// <param name="x">The value of x.</param>
    /// <param name="y">The value of y.</param>
    /// <param name="z">The value of z.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="mipLevel">The value of mipLevel.</param>
    /// <param name="arrayLayer">The value of arrayLayer.</param>
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
    /// Performs the WaitForIdleCore operation.
    /// </summary>
    private protected override void WaitForIdleCore() {
        MTLCommandBuffer lastCb;

        lock (this._submittedCommandsLock) {
            lastCb = this._latestSubmittedCb;
            ObjectiveCRuntime.retain(lastCb.NativePtr);
        }

        if (lastCb.NativePtr != IntPtr.Zero && lastCb.status != MTLCommandBufferStatus.Completed) {
            lastCb.waitUntilCompleted();
        }

        ObjectiveCRuntime.release(lastCb.NativePtr);
    }
}

/// <summary>
/// Represents the MonoPInvokeCallbackAttribute class.
/// </summary>
internal sealed class MonoPInvokeCallbackAttribute : Attribute {

    /// <summary>
    /// Initializes a new instance of the <see cref="MonoPInvokeCallbackAttribute" /> type.
    /// </summary>
    /// <param name="t">The value of t.</param>
    public MonoPInvokeCallbackAttribute(Type t) { }
}