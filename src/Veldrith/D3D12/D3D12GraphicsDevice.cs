using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using D3D12Feature = Vortice.Direct3D12.Feature;
using VorticeD3D12 = Vortice.Direct3D12.D3D12;
using VorticeDXGI = Vortice.DXGI.DXGI;

namespace Veldrith.D3D12
{
    internal sealed class D3D12GraphicsDevice : GraphicsDevice
    {
        private static readonly GraphicsDeviceFeatures _d3d12Features = new GraphicsDeviceFeatures(
            computeShader: true,
            geometryShader: true,
            tessellationShaders: true,
            multipleViewports: true,
            samplerLodBias: true,
            drawBaseVertex: true,
            drawBaseInstance: true,
            drawIndirect: true,
            drawIndirectBaseInstance: true,
            fillModeWireframe: true,
            samplerAnisotropy: true,
            depthClipDisable: true,
            texture1D: true,
            independentBlend: true,
            structuredBuffer: true,
            subsetTextureView: true,
            commandListDebugMarkers: true,
            bufferRangeBinding: true,
            shaderFloat64: false);

        private readonly D3D12ResourceFactory _resourceFactory;
        private readonly BackendInfoD3D12 _d3d12Info;
        private readonly Swapchain _mainSwapchain;
        private readonly IDXGIFactory4 _dxgiFactory;
        private readonly ID3D12Device _device;
        private readonly ID3D12CommandQueue _commandQueue;
        private readonly ID3D12Fence _submissionFence;
        private readonly AutoResetEvent _submissionFenceEvent;
        private ulong _nextSubmissionFenceValue = 1;
        private readonly Dictionary<string, ID3D12RootSignature> _rootSignatureCache = new Dictionary<string, ID3D12RootSignature>(StringComparer.Ordinal);
        private readonly object _rootSignatureCacheLock = new object();
        private readonly Dictionary<Format, CachedFormatSupport> _formatSupportCache = new Dictionary<Format, CachedFormatSupport>();
        private readonly object _formatSupportCacheLock = new object();
        private readonly string _deviceName;
        private readonly string _vendorName;
        private ulong _immediateFenceValue = 1;

        public static bool IsSupported()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            return VorticeD3D12.D3D12CreateDevice(null, FeatureLevel.Level_11_0, out ID3D12Device _).Success;
        }

        public D3D12GraphicsDevice(GraphicsDeviceOptions options, SwapchainDescription? swapchainDescription)
        {
            if (!IsSupported())
            {
                throw new PlatformNotSupportedException("Direct3D 12 is only supported on Windows.");
            }

            _dxgiFactory = VorticeDXGI.CreateDXGIFactory2<IDXGIFactory4>(false);
            IDXGIAdapter1 adapter = selectAdapter(_dxgiFactory);
            try
            {
                if (adapter != null)
                {
                    VorticeD3D12.D3D12CreateDevice(adapter, FeatureLevel.Level_11_0, out _device).CheckError();
                    AdapterDescription1 description = adapter.Description1;
                    _deviceName = description.Description?.TrimEnd('\0');
                    _vendorName = $"0x{description.VendorId:X4}";
                }
                else
                {
                    VorticeD3D12.D3D12CreateDevice(null, FeatureLevel.Level_11_0, out _device).CheckError();
                    _deviceName = "Direct3D 12 Device";
                    _vendorName = "Unknown";
                }
            }
            finally
            {
                adapter?.Dispose();
            }

            _commandQueue = _device.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));
            _submissionFence = _device.CreateFence(0, FenceFlags.None);
            _submissionFenceEvent = new AutoResetEvent(false);
            _resourceFactory = new D3D12ResourceFactory(this, Features);

            if (swapchainDescription != null)
            {
                SwapchainDescription scDesc = swapchainDescription.Value;
                _mainSwapchain = new D3D12Swapchain(this, ref scDesc);
            }

            _d3d12Info = new BackendInfoD3D12(_device.NativePointer);
            if (_mainSwapchain != null)
            {
                SyncToVerticalBlank = options.SyncToVerticalBlank;
            }
            PostDeviceCreated();
        }

        public override string DeviceName => _deviceName;

        public override string VendorName => _vendorName;

        public override GraphicsApiVersion ApiVersion => GraphicsApiVersion.Unknown;

        public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

        public override bool IsUvOriginTopLeft => true;

        public override bool IsDepthRangeZeroToOne => true;

        public override bool IsClipSpaceYInverted => true;

        public override ResourceFactory ResourceFactory => _resourceFactory;

        public override Swapchain MainSwapchain => _mainSwapchain;

        public override GraphicsDeviceFeatures Features => _d3d12Features;
        public override bool AllowTearing
        {
            get => _mainSwapchain is D3D12Swapchain d3d12Swapchain && d3d12Swapchain.AllowTearing;
            set
            {
                if (_mainSwapchain is D3D12Swapchain d3d12Swapchain)
                {
                    d3d12Swapchain.AllowTearing = value;
                }
            }
        }
        internal ID3D12Device Device => _device;
        internal ID3D12CommandQueue CommandQueue => _commandQueue;
        internal IDXGIFactory4 DxgiFactory => _dxgiFactory;
        internal bool IsSubmissionFenceComplete(ulong value) => _submissionFence.CompletedValue >= value;
        internal void WaitForSubmissionFence(ulong value)
        {
            if (_submissionFence.CompletedValue >= value)
            {
                return;
            }

            _submissionFence.SetEventOnCompletion(value, _submissionFenceEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();
            _submissionFenceEvent.WaitOne();
        }
        internal ID3D12RootSignature GetOrCreateRootSignature(string cacheKey, in RootSignatureDescription description)
        {
            lock (_rootSignatureCacheLock)
            {
                if (_rootSignatureCache.TryGetValue(cacheKey, out ID3D12RootSignature cached))
                {
                    return cached;
                }

                ID3D12RootSignature created = _device.CreateRootSignature(in description, RootSignatureVersion.Version1);
                _rootSignatureCache.Add(cacheKey, created);
                return created;
            }
        }

        public override bool WaitForFence(Fence fence, ulong nanosecondTimeout)
        {
            var d3d12Fence = Util.AssertSubtype<Fence, D3D12Fence>(fence);
            return d3d12Fence.Wait(nanosecondTimeout);
        }

        public override bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout)
        {
            if (fences == null || fences.Length == 0)
            {
                return true;
            }

            if (waitAll)
            {
                foreach (var fence in fences)
                {
                    if (!WaitForFence(fence, nanosecondTimeout))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (nanosecondTimeout == ulong.MaxValue)
            {
                while (true)
                {
                    foreach (var fence in fences)
                    {
                        if (fence.Signaled)
                        {
                            return true;
                        }
                    }

                    Thread.Sleep(0);
                }
            }

            var deadline = DateTime.UtcNow + TimeSpan.FromTicks((long)(nanosecondTimeout / 100));
            while (DateTime.UtcNow <= deadline)
            {
                foreach (var fence in fences)
                {
                    if (fence.Signaled)
                    {
                        return true;
                    }
                }

                Thread.Sleep(0);
            }

            return false;
        }

        public override void ResetFence(Fence fence)
        {
            fence.Reset();
        }

        public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat)
        {
            Format dxgiFormat;
            try
            {
                dxgiFormat = depthFormat
                    ? D3D12Formats.ToDepthFormat(format)
                    : D3D12Formats.ToDxgiFormat(format);
            }
            catch (VeldridException)
            {
                return TextureSampleCount.Count1;
            }

            if (!tryGetFormatSupport(dxgiFormat, out FeatureDataFormatSupport formatSupport))
            {
                return TextureSampleCount.Count1;
            }

            FormatSupport1 support1 = formatSupport.Support1;
            if (depthFormat)
            {
                if ((support1 & FormatSupport1.DepthStencil) == 0)
                {
                    return TextureSampleCount.Count1;
                }
            }
            else if ((support1 & (FormatSupport1.RenderTarget | FormatSupport1.MultisampleRendertarget)) == 0)
            {
                return TextureSampleCount.Count1;
            }

            uint sampleMask = getSupportedSampleFlags(dxgiFormat);
            if ((sampleMask & (1u << (int)TextureSampleCount.Count32)) != 0) return TextureSampleCount.Count32;
            if ((sampleMask & (1u << (int)TextureSampleCount.Count16)) != 0) return TextureSampleCount.Count16;
            if ((sampleMask & (1u << (int)TextureSampleCount.Count8)) != 0) return TextureSampleCount.Count8;
            if ((sampleMask & (1u << (int)TextureSampleCount.Count4)) != 0) return TextureSampleCount.Count4;
            if ((sampleMask & (1u << (int)TextureSampleCount.Count2)) != 0) return TextureSampleCount.Count2;
            return TextureSampleCount.Count1;
        }

        internal override uint GetUniformBufferMinOffsetAlignmentCore() => 256;

        internal override uint GetStructuredBufferMinOffsetAlignmentCore() => 16;

        protected override MappedResource MapCore(IMappableResource resource, MapMode mode, uint subresource)
        {
            if (resource is D3D12DeviceBuffer buffer)
            {
                return buffer.Map(mode);
            }

            if (resource is D3D12Texture texture)
            {
                return texture.Map(mode, subresource);
            }

            throw new VeldridException("Resource belongs to a different backend.");
        }

        protected override void UnmapCore(IMappableResource resource, uint subresource)
        {
            if (resource is D3D12DeviceBuffer buffer)
            {
                buffer.Unmap();
                return;
            }

            if (resource is D3D12Texture texture)
            {
                texture.Unmap();
                return;
            }

            throw new VeldridException("Resource belongs to a different backend.");
        }

        protected override void PlatformDispose()
        {
            lock (_rootSignatureCacheLock)
            {
                foreach (ID3D12RootSignature rootSignature in _rootSignatureCache.Values)
                {
                    rootSignature?.Dispose();
                }
                _rootSignatureCache.Clear();
            }

            _submissionFenceEvent?.Dispose();
            _submissionFence?.Dispose();
            _mainSwapchain?.Dispose();
            _commandQueue?.Dispose();
            _device?.Dispose();
            _dxgiFactory?.Dispose();
        }

        private protected override void SubmitCommandsCore(CommandList commandList, Fence fence)
        {
            if (commandList is D3D12CommandList d3d12CommandList)
            {
                d3d12CommandList.ExecuteNoSignal();
                ulong signalValue = _nextSubmissionFenceValue++;
                _commandQueue.Signal(_submissionFence, signalValue).CheckError();
                d3d12CommandList.MarkSubmitted(signalValue);
                d3d12CommandList.ClearCachedState();
            }

            if (fence is D3D12Fence d3d12Fence)
            {
                d3d12Fence.Signal(_commandQueue);
            }
        }

        private protected override void SwapBuffersCore(Swapchain swapchain)
        {
            if (swapchain is D3D12Swapchain d3d12Swapchain)
            {
                d3d12Swapchain.Present();
                return;
            }

            throw new VeldridException("Swapchain belongs to a different backend.");
        }

        private protected override void WaitForIdleCore()
        {
            waitForQueueIdle();
        }

        private protected override void WaitForNextFrameReadyCore()
        {
            // Do not globally stall the GPU every frame on D3D12.
            // Frame pacing should be handled by swapchain present / fences.
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
            uint arrayLayer)
        {
            if (texture is not D3D12Texture d3d12Texture)
            {
                throw new VeldridException("Texture belongs to a different backend.");
            }

            if (d3d12Texture.NativeTexture != null)
            {
                updateNativeTexture(
                    d3d12Texture,
                    source,
                    sizeInBytes,
                    x,
                    y,
                    z,
                    width,
                    height,
                    depth,
                    mipLevel,
                    arrayLayer);
                return;
            }

            d3d12Texture.Update(source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            if (buffer is not D3D12DeviceBuffer d3d12Buffer)
            {
                throw new VeldridException("Buffer belongs to a different backend.");
            }

            ID3D12CommandAllocator allocator = _device.CreateCommandAllocator(CommandListType.Direct);
            ID3D12GraphicsCommandList commandList = _device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, allocator, null);
            ID3D12Resource temporaryUpload = null;
            try
            {
                temporaryUpload = d3d12Buffer.Update(commandList, source, bufferOffsetInBytes, sizeInBytes);
                commandList.Close();
                _commandQueue.ExecuteCommandList(commandList);
                waitForQueueIdle();
            }
            finally
            {
                temporaryUpload?.Dispose();
                commandList.Dispose();
                allocator.Dispose();
            }
        }

        private protected override bool GetPixelFormatSupportCore(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties)
        {
            if ((usage & TextureUsage.Cubemap) != 0 && type != TextureType.Texture2D)
            {
                properties = default;
                return false;
            }

            if ((usage & TextureUsage.DepthStencil) != 0 && (usage & (TextureUsage.RenderTarget | TextureUsage.Storage)) != 0)
            {
                properties = default;
                return false;
            }

            if ((usage & TextureUsage.DepthStencil) != 0 && type == TextureType.Texture3D)
            {
                properties = default;
                return false;
            }

            bool depthUsage = (usage & TextureUsage.DepthStencil) != 0;
            Format resourceFormat;
            Format depthStencilFormat = Format.Unknown;
            Format sampledViewFormat = Format.Unknown;
            try
            {
                if (depthUsage)
                {
                    resourceFormat = D3D12Formats.ToDxgiFormat(format, depthFormat: true);
                    depthStencilFormat = D3D12Formats.ToDepthFormat(format);
                    sampledViewFormat = D3D12Formats.GetViewFormat(resourceFormat);
                }
                else
                {
                    resourceFormat = D3D12Formats.ToDxgiFormat(format);
                    sampledViewFormat = resourceFormat;
                }
            }
            catch (VeldridException)
            {
                properties = default;
                return false;
            }

            if (!tryGetFormatSupport(resourceFormat, out FeatureDataFormatSupport formatSupport))
            {
                properties = default;
                return false;
            }

            FormatSupport1 support1 = formatSupport.Support1;
            FormatSupport2 support2 = formatSupport.Support2;
            if (!isTypeSupported(type, support1))
            {
                properties = default;
                return false;
            }

            if ((usage & TextureUsage.Cubemap) != 0 && (support1 & FormatSupport1.TextureCube) == 0)
            {
                properties = default;
                return false;
            }

            if ((usage & TextureUsage.Sampled) != 0)
            {
                if (depthUsage)
                {
                    if (!tryGetFormatSupport(sampledViewFormat, out FeatureDataFormatSupport sampledSupport))
                    {
                        properties = default;
                        return false;
                    }

                    FormatSupport1 sampledSupport1 = sampledSupport.Support1;
                    if ((sampledSupport1 & (FormatSupport1.ShaderSample | FormatSupport1.ShaderSampleComparison)) == 0)
                    {
                        properties = default;
                        return false;
                    }
                }
                else if ((support1 & FormatSupport1.ShaderSample) == 0)
                {
                    properties = default;
                    return false;
                }
            }

            if ((usage & TextureUsage.RenderTarget) != 0 && (support1 & FormatSupport1.RenderTarget) == 0)
            {
                properties = default;
                return false;
            }

            if (depthUsage && (support1 & FormatSupport1.DepthStencil) == 0)
            {
                if (!tryGetFormatSupport(depthStencilFormat, out FeatureDataFormatSupport depthSupport)
                    || (depthSupport.Support1 & FormatSupport1.DepthStencil) == 0)
                {
                    properties = default;
                    return false;
                }
            }

            if ((usage & TextureUsage.Storage) != 0
                && (support1 & FormatSupport1.TypedUnorderedAccessView) == 0)
            {
                properties = default;
                return false;
            }

            if ((usage & TextureUsage.Storage) != 0 && (FormatHelpers.IsCompressedFormat(format) || isSrgbFormat(format)))
            {
                properties = default;
                return false;
            }

            if ((usage & TextureUsage.Storage) != 0
                && ((support2 & FormatSupport2.UnorderedAccessViewTypedLoad) == 0
                    || (support2 & FormatSupport2.UnorderedAccessViewTypedStore) == 0))
            {
                properties = default;
                return false;
            }

            if ((usage & TextureUsage.GenerateMipmaps) != 0 && (support1 & FormatSupport1.Mip) == 0)
            {
                properties = default;
                return false;
            }

            if ((usage & TextureUsage.GenerateMipmaps) != 0
                && !isRuntimeMipmapGenerationSupported(format, type, usage, depthUsage))
            {
                properties = default;
                return false;
            }

            Format sampleCountFormat = depthUsage ? depthStencilFormat : resourceFormat;
            uint sampleFlags = getSupportedSampleFlags(sampleCountFormat);
            if (sampleFlags == 0)
            {
                sampleFlags = 1u << (int)TextureSampleCount.Count1;
            }

            bool supportsMsaa = (support1 & FormatSupport1.MultisampleRendertarget) != 0;
            bool allowsMsaaUsage = (usage & (TextureUsage.RenderTarget | TextureUsage.DepthStencil)) != 0;
            if (!supportsMsaa || !allowsMsaaUsage)
            {
                sampleFlags = 1u << (int)TextureSampleCount.Count1;
            }

            getTextureTypeLimits(type, out uint maxWidth, out uint maxHeight, out uint maxDepth, out uint maxArrayLayers);
            uint maxMipLevels = getMaxMipLevels(maxWidth, maxHeight, maxDepth);
            if (type != TextureType.Texture2D
                || (usage & (TextureUsage.Storage | TextureUsage.Staging | TextureUsage.GenerateMipmaps | TextureUsage.Cubemap)) != 0)
            {
                sampleFlags = 1u << (int)TextureSampleCount.Count1;
            }

            properties = new PixelFormatProperties(
                maxWidth,
                maxHeight,
                maxDepth,
                maxMipLevels,
                maxArrayLayers,
                sampleFlags);
            return true;
        }

        public override bool GetD3D12Info(out BackendInfoD3D12 info)
        {
            info = _d3d12Info;
            return true;
        }

        private static IDXGIAdapter1 selectAdapter(IDXGIFactory4 factory)
        {
            // Prefer the high-performance adapter when DXGI 1.6 is available.
            using (var factory6 = factory.QueryInterfaceOrNull<IDXGIFactory6>())
            {
                if (factory6 != null)
                {
                    uint hpIndex = 0;
                    while (factory6.EnumAdapterByGpuPreference(hpIndex, GpuPreference.HighPerformance, out IDXGIAdapter1 hpAdapter).Success)
                    {
                        AdapterDescription1 hpDescription = hpAdapter.Description1;
                        bool softwareHp = (hpDescription.Flags & AdapterFlags.Software) != 0;
                        if (!softwareHp
                            && VorticeD3D12.D3D12CreateDevice(hpAdapter, FeatureLevel.Level_11_0, out ID3D12Device hpProbeDevice).Success)
                        {
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
            while (factory.EnumAdapters1(index, out IDXGIAdapter1 adapter).Success)
            {
                AdapterDescription1 description = adapter.Description1;
                bool software = (description.Flags & AdapterFlags.Software) != 0;
                if (!software
                    && VorticeD3D12.D3D12CreateDevice(adapter, FeatureLevel.Level_11_0, out ID3D12Device probeDevice).Success)
                {
                    probeDevice.Dispose();
                    long dedicatedMemory = (long)description.DedicatedVideoMemory;
                    if (dedicatedMemory > bestDedicatedMemory)
                    {
                        bestAdapter?.Dispose();
                        bestAdapter = adapter;
                        bestDedicatedMemory = dedicatedMemory;
                    }
                    else
                    {
                        adapter.Dispose();
                    }
                }
                else
                {
                    adapter.Dispose();
                }

                index++;
            }

            return bestAdapter;
        }

        private uint getSupportedSampleFlags(Format format)
        {
            uint sampleFlags = 1u << (int)TextureSampleCount.Count1;
            sampleFlags |= querySampleSupportFlag(format, 2, TextureSampleCount.Count2);
            sampleFlags |= querySampleSupportFlag(format, 4, TextureSampleCount.Count4);
            sampleFlags |= querySampleSupportFlag(format, 8, TextureSampleCount.Count8);
            sampleFlags |= querySampleSupportFlag(format, 16, TextureSampleCount.Count16);
            sampleFlags |= querySampleSupportFlag(format, 32, TextureSampleCount.Count32);
            return sampleFlags;
        }

        private uint querySampleSupportFlag(Format format, uint sampleCount, TextureSampleCount textureSampleCount)
        {
            var msaa = new FeatureDataMultisampleQualityLevels
            {
                Format = format,
                SampleCount = sampleCount,
                Flags = MultisampleQualityLevelFlags.None
            };

            if (!tryCheckFeatureSupport(D3D12Feature.MultisampleQualityLevels, ref msaa) || msaa.NumQualityLevels == 0)
            {
                return 0;
            }

            return 1u << (int)textureSampleCount;
        }

        private static void getTextureTypeLimits(TextureType type, out uint maxWidth, out uint maxHeight, out uint maxDepth, out uint maxArrayLayers)
        {
            switch (type)
            {
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
                default:
                    throw Illegal.Value<TextureType>();
            }
        }

        private static uint getMaxMipLevels(uint maxWidth, uint maxHeight, uint maxDepth)
        {
            uint maxDimension = Math.Max(maxWidth, Math.Max(maxHeight, maxDepth));
            uint mipLevels = 1;
            while (maxDimension > 1)
            {
                maxDimension >>= 1;
                mipLevels++;
            }

            return mipLevels;
        }

        private static bool isSrgbFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R8G8B8A8UNormSRgb:
                case PixelFormat.B8G8R8A8UNormSRgb:
                case PixelFormat.Bc1RgbUNormSRgb:
                case PixelFormat.Bc1RgbaUNormSRgb:
                case PixelFormat.Bc2UNormSRgb:
                case PixelFormat.Bc3UNormSRgb:
                case PixelFormat.Bc7UNormSRgb:
                    return true;
                default:
                    return false;
            }
        }

        private static bool isRuntimeMipmapGenerationSupported(PixelFormat format, TextureType type, TextureUsage usage, bool depthUsage)
        {
            if (depthUsage)
            {
                return false;
            }

            if (FormatHelpers.IsCompressedFormat(format))
            {
                return false;
            }

            if ((usage & TextureUsage.Cubemap) != 0)
            {
                return false;
            }

            if (type != TextureType.Texture1D
                && type != TextureType.Texture2D
                && type != TextureType.Texture3D)
            {
                return false;
            }

            return true;
        }

        private bool tryGetFormatSupport(Format format, out FeatureDataFormatSupport formatSupport)
        {
            lock (_formatSupportCacheLock)
            {
                if (_formatSupportCache.TryGetValue(format, out CachedFormatSupport cached))
                {
                    formatSupport = cached.Support;
                    return cached.IsSupported;
                }

                formatSupport = new FeatureDataFormatSupport
                {
                    Format = format
                };

                bool isSupported = tryCheckFeatureSupport(D3D12Feature.FormatSupport, ref formatSupport);
                _formatSupportCache[format] = new CachedFormatSupport(isSupported, formatSupport);
                return isSupported;
            }
        }

        private static bool isTypeSupported(TextureType type, FormatSupport1 support)
        {
            switch (type)
            {
                case TextureType.Texture1D:
                    return (support & FormatSupport1.Texture1D) != 0;
                case TextureType.Texture2D:
                    return (support & FormatSupport1.Texture2D) != 0;
                case TextureType.Texture3D:
                    return (support & FormatSupport1.Texture3D) != 0;
                default:
                    return false;
            }
        }

        private bool tryCheckFeatureSupport<T>(D3D12Feature feature, ref T data)
            where T : unmanaged
        {
            return _device.CheckFeatureSupport(feature, ref data);
        }

        private readonly struct CachedFormatSupport
        {
            public readonly bool IsSupported;
            public readonly FeatureDataFormatSupport Support;

            public CachedFormatSupport(bool isSupported, FeatureDataFormatSupport support)
            {
                IsSupported = isSupported;
                Support = support;
            }
        }


        private unsafe void updateNativeTexture(
            D3D12Texture texture,
            IntPtr source,
            uint sizeInBytes,
            uint x,
            uint y,
            uint z,
            uint width,
            uint height,
            uint depth,
            uint mipLevel,
            uint arrayLayer)
        {
            // Use the validated staging->native upload path in D3D12Texture to avoid
            // partial CopyTextureRegion edge-cases that can trigger device removal.
            texture.UpdateNativeSubresource(
                source,
                sizeInBytes,
                x,
                y,
                z,
                width,
                height,
                depth,
                mipLevel,
                arrayLayer);
        }

        private unsafe void copyTextureDataToUploadBuffer(
            IntPtr source,
            uint sizeInBytes,
            PixelFormat format,
            uint copyWidth,
            uint copyHeight,
            uint copyDepth,
            void* uploadMappedPtr,
            PlacedSubresourceFootPrint placedFootprint,
            uint numRows,
            ulong rowSizeInBytes)
        {
            uint srcRowPitch = FormatHelpers.GetRowPitch(copyWidth, format);
            uint srcNumRows = FormatHelpers.GetNumRows(copyHeight, format);
            uint srcDepthPitch = srcRowPitch * srcNumRows;
            ulong requiredBytes = (ulong)srcDepthPitch * copyDepth;
            if (sizeInBytes < requiredBytes)
            {
                throw new VeldridException("Texture update source size is smaller than required for the destination texture.");
            }

            if (numRows < srcNumRows)
            {
                throw new VeldridException("Unexpected row count when uploading native D3D12 texture data.");
            }

            if (rowSizeInBytes < srcRowPitch)
            {
                throw new VeldridException("Unexpected row size when uploading native D3D12 texture data.");
            }

            byte* srcBase = (byte*)source.ToPointer();
            byte* dstBase = (byte*)uploadMappedPtr + placedFootprint.Offset;
            uint dstRowPitch = placedFootprint.Footprint.RowPitch;
            uint dstSlicePitch = dstRowPitch * numRows;
            uint copyRowSize = srcRowPitch;

            for (uint slice = 0; slice < copyDepth; slice++)
            {
                for (uint row = 0; row < srcNumRows; row++)
                {
                    byte* srcRow = srcBase + (slice * srcDepthPitch) + (row * srcRowPitch);
                    byte* dstRow = dstBase + (slice * dstSlicePitch) + (row * dstRowPitch);
                    Unsafe.CopyBlock(dstRow, srcRow, copyRowSize);
                }
            }
        }

        private void waitForQueueIdle()
        {
            ID3D12Fence fence = null;
            using var waitEvent = new AutoResetEvent(false);
            try
            {
                fence = _device.CreateFence(0, FenceFlags.None);
                ulong signalValue = _immediateFenceValue++;
                _commandQueue.Signal(fence, signalValue);
                if (fence.CompletedValue < signalValue)
                {
                    fence.SetEventOnCompletion(signalValue, waitEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();
                    waitEvent.WaitOne();
                }
            }
            catch (SharpGenException ex) when (ex.ResultCode.Code == unchecked((int)0x887A0005))
            {
                // Device already lost. During shutdown this should not escalate into a second crash.
                return;
            }
            finally
            {
                fence?.Dispose();
            }
        }
    }
}
