using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
/// Represents the D3D12GraphicsDevice class.
/// </summary>
internal sealed class D3D12GraphicsDevice : GraphicsDevice {

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="true">The value of true.</param>
    /// <param name="false">The value of false.</param>
    /// <returns>The result of the new operation.</returns>
    private static readonly GraphicsDeviceFeatures _d3d12Features = new(true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false);

    /// <summary>
    /// Represents the _d3d12Info field.
    /// </summary>
    private readonly BackendInfoD3D12 _d3d12Info;

    /// <summary>
    /// Represents the _device field.
    /// </summary>
    private readonly ID3D12Device _device;

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly Dictionary<Format, CachedFormatSupport> _formatSupportCache = new();

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly object _formatSupportCacheLock = new();

    /// <summary>
    /// Represents the _resourceFactory field.
    /// </summary>
    private readonly D3D12ResourceFactory _resourceFactory;

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="Ordinal">The value of Ordinal.</param>
    /// <returns>The result of the new operation.</returns>
    private readonly Dictionary<string, ID3D12RootSignature> _rootSignatureCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private readonly object _rootSignatureCacheLock = new();

    /// <summary>
    /// Represents the _submissionFence field.
    /// </summary>
    private readonly ID3D12Fence _submissionFence;

    /// <summary>
    /// Represents the _submissionFenceEvent field.
    /// </summary>
    private readonly AutoResetEvent _submissionFenceEvent;

    /// <summary>
    /// Represents the _immediateFenceValue field.
    /// </summary>
    private ulong _immediateFenceValue = 1;

    /// <summary>
    /// Represents the _nextSubmissionFenceValue field.
    /// </summary>
    private ulong _nextSubmissionFenceValue = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12GraphicsDevice" /> type.
    /// </summary>
    /// <param name="options">The value of options.</param>
    /// <param name="swapchainDescription">The value of swapchainDescription.</param>
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
    /// Represents the Device field.
    /// </summary>
    internal ID3D12Device Device => this._device;

    /// <summary>
    /// Gets or sets CommandQueue.
    /// </summary>
    internal ID3D12CommandQueue CommandQueue { get; }

    /// <summary>
    /// Gets or sets DxgiFactory.
    /// </summary>
    internal IDXGIFactory4 DxgiFactory { get; }

    /// <summary>
    /// Performs the IsSupported operation.
    /// </summary>
    /// <returns>The result of the IsSupported operation.</returns>
    public static bool IsSupported() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            return false;
        }

        return VorticeD3D12.D3D12CreateDevice(null, FeatureLevel.Level_11_0, out ID3D12Device _).Success;
    }

    /// <summary>
    /// Performs the IsSubmissionFenceComplete operation.
    /// </summary>
    /// <param name="value">The value of value.</param>
    /// <returns>The result of the IsSubmissionFenceComplete operation.</returns>
    internal bool IsSubmissionFenceComplete(ulong value) {
        return this._submissionFence.CompletedValue >= value;
    }

    /// <summary>
    /// Performs the WaitForSubmissionFence operation.
    /// </summary>
    /// <param name="value">The value of value.</param>
    internal void WaitForSubmissionFence(ulong value) {
        if (this._submissionFence.CompletedValue >= value) {
            return;
        }

        this._submissionFence
            .SetEventOnCompletion(value, this._submissionFenceEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();
        this._submissionFenceEvent.WaitOne();
    }

    /// <summary>
    /// Performs the GetOrCreateRootSignature operation.
    /// </summary>
    /// <param name="cacheKey">The value of cacheKey.</param>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the GetOrCreateRootSignature operation.</returns>
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
    /// Performs the WaitForFence operation.
    /// </summary>
    /// <param name="fence">The value of fence.</param>
    /// <param name="nanosecondTimeout">The value of nanosecondTimeout.</param>
    /// <returns>The result of the WaitForFence operation.</returns>
    public override bool WaitForFence(Fence fence, ulong nanosecondTimeout) {
        D3D12Fence d3d12Fence = Util.AssertSubtype<Fence, D3D12Fence>(fence);
        return d3d12Fence.Wait(nanosecondTimeout);
    }

    /// <summary>
    /// Performs the WaitForFences operation.
    /// </summary>
    /// <param name="fences">The value of fences.</param>
    /// <param name="waitAll">The value of waitAll.</param>
    /// <param name="nanosecondTimeout">The value of nanosecondTimeout.</param>
    /// <returns>The result of the WaitForFences operation.</returns>
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
    /// Performs the ResetFence operation.
    /// </summary>
    /// <param name="fence">The value of fence.</param>
    public override void ResetFence(Fence fence) {
        fence.Reset();
    }

    /// <summary>
    /// Performs the GetSampleCountLimit operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="depthFormat">The value of depthFormat.</param>
    /// <returns>The result of the GetSampleCountLimit operation.</returns>
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
    /// Performs the GetUniformBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>The result of the GetUniformBufferMinOffsetAlignmentCore operation.</returns>
    internal override uint GetUniformBufferMinOffsetAlignmentCore() {
        return 256;
    }

    /// <summary>
    /// Performs the GetStructuredBufferMinOffsetAlignmentCore operation.
    /// </summary>
    /// <returns>The result of the GetStructuredBufferMinOffsetAlignmentCore operation.</returns>
    internal override uint GetStructuredBufferMinOffsetAlignmentCore() {
        return 16;
    }

    /// <summary>
    /// Performs the MapCore operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="mode">The value of mode.</param>
    /// <param name="subresource">The value of subresource.</param>
    /// <returns>The result of the MapCore operation.</returns>
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
    /// Performs the UnmapCore operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="subresource">The value of subresource.</param>
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
    /// Performs the PlatformDispose operation.
    /// </summary>
    protected override void PlatformDispose() {
        lock (this._rootSignatureCacheLock) {
            foreach (ID3D12RootSignature rootSignature in this._rootSignatureCache.Values) {
                rootSignature?.Dispose();
            }

            this._rootSignatureCache.Clear();
        }

        this._submissionFenceEvent?.Dispose();
        this._submissionFence?.Dispose();
        this.MainSwapchain?.Dispose();
        this.CommandQueue?.Dispose();
        this._device?.Dispose();
        this.DxgiFactory?.Dispose();
    }

    /// <summary>
    /// Performs the SubmitCommandsCore operation.
    /// </summary>
    /// <param name="commandList">The value of commandList.</param>
    /// <param name="fence">The value of fence.</param>
    private protected override void SubmitCommandsCore(CommandList commandList, Fence fence) {
        if (commandList is D3D12CommandList d3d12CommandList) {
            d3d12CommandList.ExecuteNoSignal();
            ulong signalValue = this._nextSubmissionFenceValue++;
            this.CommandQueue.Signal(this._submissionFence, signalValue).CheckError();
            d3d12CommandList.MarkSubmitted(signalValue);
            d3d12CommandList.ClearCachedState();
        }

        if (fence is D3D12Fence d3d12Fence) {
            d3d12Fence.Signal(this.CommandQueue);
        }
    }

    /// <summary>
    /// Performs the SwapBuffersCore operation.
    /// </summary>
    /// <param name="swapchain">The value of swapchain.</param>
    private protected override void SwapBuffersCore(Swapchain swapchain) {
        if (swapchain is D3D12Swapchain d3d12Swapchain) {
            d3d12Swapchain.Present();
            return;
        }

        throw new VeldridException("Swapchain belongs to a different backend.");
    }

    /// <summary>
    /// Performs the WaitForIdleCore operation.
    /// </summary>
    private protected override void WaitForIdleCore() {
        this.WaitForQueueIdle();
    }

    /// <summary>
    /// Performs the WaitForNextFrameReadyCore operation.
    /// </summary>
    private protected override void WaitForNextFrameReadyCore() {
        // Do not globally stall the GPU every frame on D3D12.
        // Frame pacing should be handled by swapchain present / fences.
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
    /// Performs the UpdateBufferCore operation.
    /// </summary>
    /// <param name="buffer">The value of buffer.</param>
    /// <param name="bufferOffsetInBytes">The value of bufferOffsetInBytes.</param>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        if (buffer is not D3D12DeviceBuffer d3d12Buffer) {
            throw new VeldridException("Buffer belongs to a different backend.");
        }

        ID3D12CommandAllocator allocator = this._device.CreateCommandAllocator(CommandListType.Direct);
        ID3D12GraphicsCommandList commandList = this._device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, allocator);
        ID3D12Resource temporaryUpload = null;
        try {
            temporaryUpload = d3d12Buffer.Update(commandList, source, bufferOffsetInBytes, sizeInBytes);
            commandList.Close();
            this.CommandQueue.ExecuteCommandList(commandList);
            this.WaitForQueueIdle();
        }
        finally {
            temporaryUpload?.Dispose();
            commandList.Dispose();
            allocator.Dispose();
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
    /// Performs the GetD3D12Info operation.
    /// </summary>
    /// <param name="info">The value of info.</param>
    /// <returns>The result of the GetD3D12Info operation.</returns>
    public override bool GetD3D12Info(out BackendInfoD3D12 info) {
        info = this._d3d12Info;
        return true;
    }

    /// <summary>
    /// Performs the SelectAdapter operation.
    /// </summary>
    /// <param name="factory">The value of factory.</param>
    /// <returns>The result of the SelectAdapter operation.</returns>
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
    /// Performs the GetSupportedSampleFlags operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <returns>The result of the GetSupportedSampleFlags operation.</returns>
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
    /// Performs the QuerySampleSupportFlag operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="sampleCount">The value of sampleCount.</param>
    /// <param name="textureSampleCount">The value of textureSampleCount.</param>
    /// <returns>The result of the QuerySampleSupportFlag operation.</returns>
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
    /// Performs the GetTextureTypeLimits operation.
    /// </summary>
    /// <param name="type">The value of type.</param>
    /// <param name="maxWidth">The value of maxWidth.</param>
    /// <param name="maxHeight">The value of maxHeight.</param>
    /// <param name="maxDepth">The value of maxDepth.</param>
    /// <param name="maxArrayLayers">The value of maxArrayLayers.</param>
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
    /// Performs the GetMaxMipLevels operation.
    /// </summary>
    /// <param name="maxWidth">The value of maxWidth.</param>
    /// <param name="maxHeight">The value of maxHeight.</param>
    /// <param name="maxDepth">The value of maxDepth.</param>
    /// <returns>The result of the GetMaxMipLevels operation.</returns>
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
    /// Performs the IsSrgbFormat operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <returns>The result of the IsSrgbFormat operation.</returns>
    private static bool IsSrgbFormat(PixelFormat format) {
        switch (format) {
            case PixelFormat.R8G8B8A8UNormSRgb: case PixelFormat.B8G8R8A8UNormSRgb: case PixelFormat.Bc1RgbUNormSRgb: case PixelFormat.Bc1RgbaUNormSRgb: case PixelFormat.Bc2UNormSRgb: case PixelFormat.Bc3UNormSRgb: case PixelFormat.Bc7UNormSRgb: return true;
            default: return false;
        }
    }

    /// <summary>
    /// Performs the IsRuntimeMipmapGenerationSupported operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="type">The value of type.</param>
    /// <param name="usage">The value of usage.</param>
    /// <param name="depthUsage">The value of depthUsage.</param>
    /// <returns>The result of the IsRuntimeMipmapGenerationSupported operation.</returns>
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
    /// Performs the TryGetFormatSupport operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <param name="formatSupport">The value of formatSupport.</param>
    /// <returns>The result of the TryGetFormatSupport operation.</returns>
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
    /// Performs the IsTypeSupported operation.
    /// </summary>
    /// <param name="type">The value of type.</param>
    /// <param name="support">The value of support.</param>
    /// <returns>The result of the IsTypeSupported operation.</returns>
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
    /// Performs the UpdateNativeTexture operation.
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
    private void UpdateNativeTexture(D3D12Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
        // Use the validated staging->native upload path in D3D12Texture to avoid
        // partial CopyTextureRegion edge-cases that can trigger device removal.
        texture.UpdateNativeSubresource(source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    /// <summary>
    /// Performs the CopyTextureDataToUploadBuffer operation.
    /// </summary>
    /// <param name="source">The value of source.</param>
    /// <param name="sizeInBytes">The value of sizeInBytes.</param>
    /// <param name="format">The value of format.</param>
    /// <param name="copyWidth">The value of copyWidth.</param>
    /// <param name="copyHeight">The value of copyHeight.</param>
    /// <param name="copyDepth">The value of copyDepth.</param>
    /// <param name="uploadMappedPtr">The value of uploadMappedPtr.</param>
    /// <param name="placedFootprint">The value of placedFootprint.</param>
    /// <param name="numRows">The value of numRows.</param>
    /// <param name="rowSizeInBytes">The value of rowSizeInBytes.</param>
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
    /// Performs the WaitForQueueIdle operation.
    /// </summary>
    private void WaitForQueueIdle() {
        ID3D12Fence fence = null;
        using AutoResetEvent waitEvent = new(false);
        try {
            fence = this._device.CreateFence();
            ulong signalValue = this._immediateFenceValue++;
            this.CommandQueue.Signal(fence, signalValue);
            if (fence.CompletedValue < signalValue) {
                fence.SetEventOnCompletion(signalValue, waitEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();
                waitEvent.WaitOne();
            }
        }
        catch (SharpGenException ex) when (ex.ResultCode.Code == unchecked((int)0x887A0005)) {
            // Device already lost. During shutdown this should not escalate into a second crash.
        }
        finally {
            fence?.Dispose();
        }
    }

    /// <summary>
    /// Represents the CachedFormatSupport struct.
    /// </summary>
    private readonly struct CachedFormatSupport {

        /// <summary>
        /// Represents the IsSupported field.
        /// </summary>
        public readonly bool IsSupported;

        /// <summary>
        /// Represents the Support field.
        /// </summary>
        public readonly FeatureDataFormatSupport Support;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedFormatSupport" /> type.
        /// </summary>
        /// <param name="isSupported">The value of isSupported.</param>
        /// <param name="support">The value of support.</param>
        public CachedFormatSupport(bool isSupported, FeatureDataFormatSupport support) {
            this.IsSupported = isSupported;
            this.Support = support;
        }
    }
}