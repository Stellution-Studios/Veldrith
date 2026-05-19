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

internal unsafe class MtlGraphicsDevice : GraphicsDevice {
    private const string _unaligned_buffer_copy_pipeline_mac_os_name = "MTL_UnalignedBufferCopy_macOS";
    private const string _unaligned_buffer_copy_pipelinei_os_name = "MTL_UnalignedBufferCopy_iOS";
    private static readonly Lazy<bool> _s_is_supported = new(GetIsSupported);

    private static readonly Dictionary<IntPtr, MtlGraphicsDevice> _s_aot_registered_blocks = new();

    private readonly MTLCommandQueue _commandQueue;

    private readonly IntPtr _completionBlockDescriptor;
    private readonly IntPtr _completionBlockLiteral;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly MTLCommandBufferHandler _completionHandler;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly IntPtr _completionHandlerFuncPtr;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly IntPtr _concreteGlobalBlock;

    private readonly MTLDevice _device;

    private readonly IMtlDisplayLink _displayLink;
    private readonly EventWaitHandle _frameEndedEvent = new(true, EventResetMode.ManualReset);
    private readonly IntPtr _libSystem;
    private readonly MtlSwapchain _mainSwapchain;
    private readonly BackendInfoMetal _metalInfo;
    private readonly AutoResetEvent _nextFrameReadyEvent;
    private readonly List<ManualResetEvent[]> _resetEvents = new();

    private readonly object _resetEventsLock = new();
    private readonly CommandBufferUsageList<MtlCommandList> _submittedCLs = new();

    private readonly object _submittedCommandsLock = new();
    private readonly bool[] _supportedSampleCounts;
    private readonly object _unalignedBufferCopyPipelineLock = new();
    private MTLCommandBuffer _latestSubmittedCb;
    private MTLComputePipelineState _unalignedBufferCopyPipeline;
    private MtlShader _unalignedBufferCopyShader;

    public MtlGraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? swapchainDesc)
        : this(options, swapchainDesc, new MetalDeviceOptions()) { }

    public MtlGraphicsDevice(
        GraphicsDeviceOptions options,
        SwapchainDescription? swapchainDesc,
        MetalDeviceOptions metalOptions) {
        this._device = MTLDevice.MTLCreateSystemDefaultDevice();
        this.DeviceName = this._device.name;
        this.MetalFeatures = new MtlFeatureSupport(this._device);

        int major = (int)this.MetalFeatures.MaxFeatureSet / 10000;
        int minor = (int)this.MetalFeatures.MaxFeatureSet % 10000;
        this.ApiVersion = new GraphicsApiVersion(major, minor, 0, 0);

        this.Features = new GraphicsDeviceFeatures(
            true,
            false,
            false, this.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3),
            false, this.MetalFeatures.IsDrawBaseVertexInstanceSupported(),
            this.MetalFeatures.IsDrawBaseVertexInstanceSupported(),
            true,
            true,
            true,
            true,
            true,
            true, // TODO: Should be macOS 10.11+ and iOS 11.0+.
            true,
            true,
            true,
            true,
            true,
            false);
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

    public MTLDevice Device => this._device;
    public MTLCommandQueue CommandQueue => this._commandQueue;
    public MtlFeatureSupport MetalFeatures { get; }
    public ResourceBindingModel ResourceBindingModel { get; }
    public bool PreferMemorylessDepthTargets { get; }

    public override string DeviceName { get; }

    public override string VendorName => "Apple";

    public override GraphicsApiVersion ApiVersion { get; }

    public override GraphicsBackend BackendType => GraphicsBackend.Metal;

    public override bool IsUvOriginTopLeft => true;

    public override bool IsDepthRangeZeroToOne => true;

    public override bool IsClipSpaceYInverted => false;

    public override ResourceFactory ResourceFactory { get; }

    public override Swapchain MainSwapchain => this._mainSwapchain;

    public override GraphicsDeviceFeatures Features { get; }

    public override void UpdateActiveDisplay(int x, int y, int w, int h) {
        this._displayLink?.UpdateActiveDisplay(x, y, w, h);
    }

    public override double GetActualRefreshPeriod() {
        if (this._displayLink != null) {
            return this._displayLink.GetActualOutputVideoRefreshPeriod();
        }

        return -1.0f;
    }

    public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat) {
        for (int i = this._supportedSampleCounts.Length - 1; i >= 0; i--) {
            if (this._supportedSampleCounts[i]) {
                return (TextureSampleCount)i;
            }
        }

        return TextureSampleCount.Count1;
    }

    public override bool GetMetalInfo(out BackendInfoMetal info) {
        info = this._metalInfo;
        return true;
    }

    public override bool WaitForFence(Fence fence, ulong nanosecondTimeout) {
        return Util.AssertSubtype<Fence, MtlFence>(fence).Wait(nanosecondTimeout);
    }

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

    public override void ResetFence(Fence fence) {
        Util.AssertSubtype<Fence, MtlFence>(fence).Reset();
    }

    internal static bool IsSupported() {
        return _s_is_supported.Value;
    }

    internal MTLComputePipelineState GetUnalignedBufferCopyPipeline() {
        lock (this._unalignedBufferCopyPipelineLock) {
            if (this._unalignedBufferCopyPipeline.IsNull) {
                MTLComputePipelineDescriptor descriptor = MTLUtil.AllocInit<MTLComputePipelineDescriptor>(
                    nameof(MTLComputePipelineDescriptor));
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

    internal override uint GetUniformBufferMinOffsetAlignmentCore() {
        return this.MetalFeatures.IsMacOS ? 16u : 256u;
    }

    internal override uint GetStructuredBufferMinOffsetAlignmentCore() {
        return 16u;
    }

    protected override MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource) {
        if (resource is MtlBuffer buffer) {
            return this.MapBuffer(buffer, mode);
        }

        MtlTexture texture = Util.AssertSubtype<IMappableResource, MtlTexture>(resource);
        return this.MapTexture(texture, mode, subresource);
    }

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

    protected override void UnmapCore(IMappableResource resource, uint subresource) { }

    // Xamarin AOT requires native callbacks be static.
    [MonoPInvokeCallback(typeof(MTLCommandBufferHandler))]
    private static void OnCommandBufferCompleted_Static(IntPtr block, MTLCommandBuffer cb) {
        lock (_s_aot_registered_blocks) {
            if (_s_aot_registered_blocks.TryGetValue(block, out MtlGraphicsDevice gd)) {
                gd.OnCommandBufferCompleted(block, cb);
            }
        }
    }

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

    private void OnDisplayLinkCallback() {
        this._nextFrameReadyEvent.Set();
        this._frameEndedEvent.WaitOne();
    }

    private MappedResource MapBuffer(MtlBuffer buffer, MapMode mode) {
        return new MappedResource(
            buffer,
            mode,
            (IntPtr)buffer.Pointer,
            buffer.SizeInBytes,
            0,
            buffer.SizeInBytes,
            buffer.SizeInBytes);
    }

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

    private void ReturnResetEventArray(ManualResetEvent[] array) {
        lock (this._resetEventsLock) {
            this._resetEvents.Add(array);
        }
    }

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

    private protected override void WaitForNextFrameReadyCore() {
        this._frameEndedEvent.Reset();
        this._nextFrameReadyEvent?.WaitOne(TimeSpan.FromSeconds(1)); // Should never time out.

        // in iOS, if one frame takes longer than the next V-Sync request, the next frame will be processed immediately rather than being delayed to a subsequent V-Sync request,
        // therefore we will request the next drawable here as a method of waiting until we're ready to draw the next frame.
        if (!this.MetalFeatures.IsMacOS) {
            MtlSwapchainFramebuffer mtlSwapchainFramebuffer =
                Util.AssertSubtype<Framebuffer, MtlSwapchainFramebuffer>(this._mainSwapchain.Framebuffer);
            mtlSwapchainFramebuffer.EnsureDrawableAvailable();
        }
    }

    private protected override bool GetPixelFormatSupportCore(
        PixelFormat format,
        TextureType type,
        TextureUsage usage,
        out PixelFormatProperties properties) {
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

        properties = new PixelFormatProperties(
            maxWidth,
            maxHeight,
            maxDepth,
            uint.MaxValue,
            maxArrayLayer,
            sampleCounts);
        return true;
    }

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

    private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source,
        uint sizeInBytes) {
        MtlBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MtlBuffer>(buffer);
        void* destPtr = mtlBuffer.Pointer;
        byte* destOffsetPtr = (byte*)destPtr + bufferOffsetInBytes;

        if (destPtr == null) {
            throw new VeldridException("Attempting to write to a MTLBuffer that is inaccessible from a CPU.");
        }

        Unsafe.CopyBlock(destOffsetPtr, source.ToPointer(), sizeInBytes);
    }

    private protected override void UpdateTextureCore(
        Texture texture,
        IntPtr source,
        uint sizeInBytes,
        uint x,
        uint y,
        uint z,
        uint width,
        uint height,
        uint depth,
        uint mipLevel,
        uint arrayLayer) {
        MtlTexture mtlTex = Util.AssertSubtype<Texture, MtlTexture>(texture);

        if (mtlTex.StagingBuffer.IsNull) {
            Texture stagingTex = this.ResourceFactory.CreateTexture(new TextureDescription(
                width, height, depth, 1, 1, texture.Format, TextureUsage.Staging, texture.Type));
            this.UpdateTexture(stagingTex, source, sizeInBytes, 0, 0, 0, width, height, depth, 0, 0);
            CommandList cl = this.ResourceFactory.CreateCommandList();
            cl.Begin();
            cl.CopyTexture(
                stagingTex, 0, 0, 0, 0, 0,
                texture, x, y, z, mipLevel, arrayLayer,
                width, height, depth, 1);
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
            Util.CopyTextureRegion(
                source.ToPointer(),
                0, 0, 0,
                srcRowPitch, srcDepthPitch,
                (byte*)mtlTex.StagingBufferPointer + dstOffset,
                x, y, z,
                dstRowPitch, dstDepthPitch,
                width, height, depth,
                texture.Format);
        }
    }

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

internal sealed class MonoPInvokeCallbackAttribute : Attribute {
    public MonoPInvokeCallbackAttribute(Type t) { }
}