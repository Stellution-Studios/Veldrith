using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using Vortice;
using Vortice.Mathematics;
using Vortice.Direct3D12;
using Vortice.DXGI;
using VorticeD3D12 = Vortice.Direct3D12.D3D12;

namespace Veldrith.D3D12
{
    internal sealed class D3D12CommandList : CommandList
    {
        private const int FramesInFlight = 8;
        private const int PerfReportIntervalFrames = 240;
        private static readonly bool _perfLogEnabled = string.Equals(Environment.GetEnvironmentVariable("VELDRID_D3D12_PERF"), "1", StringComparison.Ordinal);
        private readonly D3D12GraphicsDevice gd;
        private readonly ID3D12CommandAllocator[] _commandAllocators = new ID3D12CommandAllocator[FramesInFlight];
        private readonly ID3D12GraphicsCommandList _nativeCommandList;
        private readonly ID3D12DescriptorHeap[] _shaderVisibleSrvUavHeaps = new ID3D12DescriptorHeap[FramesInFlight];
        private readonly ID3D12DescriptorHeap[] _shaderVisibleSamplerHeaps = new ID3D12DescriptorHeap[FramesInFlight];
        private readonly ID3D12DescriptorHeap[] _boundDescriptorHeaps = new ID3D12DescriptorHeap[2];
        private readonly ResourceBarrier[] _singleBarrier = new ResourceBarrier[1];
        private readonly int _srvUavDescriptorSize;
        private readonly int _samplerDescriptorSize;
        private readonly uint _maxSrvUavDescriptors = 4096;
        private readonly uint _maxSamplerDescriptors = 1024;
        private bool _begun;
        private bool _ended;
        private bool _disposed;
        private string name;
        private int _transitionedBackBufferIndex = -1;
        private D3D12Pipeline _currentGraphicsPipeline;
        private D3D12Pipeline _currentComputePipeline;
        private uint _nextSrvUavDescriptor;
        private uint _nextSamplerDescriptor;
        private bool _descriptorHeapsBound;
        private bool _gpuMipResourcesInitialized;
        private bool _gpuMipResourcesAvailable;
        private D3D12Pipeline _gpuMipPipeline;
        private ResourceLayout _gpuMipResourceLayout;
        private Sampler _gpuMipSampler;
        private bool _indirectSignaturesInitialized;
        private bool _indirectSignaturesAvailable;
        private ID3D12CommandSignature _drawIndirectSignature;
        private ID3D12CommandSignature _drawIndexedIndirectSignature;
        private ID3D12CommandSignature _dispatchIndirectSignature;
        private readonly Vortice.Mathematics.Viewport[] _activeViewports = new Vortice.Mathematics.Viewport[16];
        private readonly RawRect[] _activeScissorRects = new RawRect[16];
        private uint _activeViewportCount;
        private uint _activeScissorRectCount;
        private readonly MethodInfo _beginEventMethod;
        private readonly MethodInfo _setMarkerMethod;
        private readonly MethodInfo _endEventMethod;
        private bool _uavBarrierPending;
        private readonly D3D12DeviceBuffer[] _boundVertexBuffers = new D3D12DeviceBuffer[16];
        private readonly uint[] _boundVertexBufferOffsets = new uint[16];
        private readonly ulong[] _boundVertexBufferVersions = new ulong[16];
        private D3D12DeviceBuffer _boundIndexBuffer;
        private uint _boundIndexBufferOffset;
        private ulong _boundIndexBufferVersion;
        private IndexFormat _boundIndexFormat;
        private bool _hasBoundIndexBuffer;
        private BoundResourceSetInfo[] _boundGraphicsResourceSets = Array.Empty<BoundResourceSetInfo>();
        private BoundResourceSetInfo[] _boundComputeResourceSets = Array.Empty<BoundResourceSetInfo>();
        private readonly Dictionary<DescriptorCacheKey, GpuDescriptorHandle>[] _descriptorTableCaches = new Dictionary<DescriptorCacheKey, GpuDescriptorHandle>[FramesInFlight];
        private readonly Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlanEntry[]> _graphicsResourceSetBindingPlans = new Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlanEntry[]>(ResourceSetBindingPlanKeyComparer.Instance);
        private readonly Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlanEntry[]> _computeResourceSetBindingPlans = new Dictionary<ResourceSetBindingPlanKey, ResourceSetBindingPlanEntry[]>(ResourceSetBindingPlanKeyComparer.Instance);
        private readonly uint[] _nextSrvUavDescriptorsPerFrameSlot = new uint[FramesInFlight];
        private readonly uint[] _nextSamplerDescriptorsPerFrameSlot = new uint[FramesInFlight];
        private uint _maxBoundVertexBufferSlot;
        private readonly ulong[] _frameSlotFenceValues = new ulong[FramesInFlight];
        private int _currentFrameSlot = -1;
        private ulong[] _graphicsRootBufferAddresses = Array.Empty<ulong>();
        private bool[] _graphicsRootBufferAddressValid = Array.Empty<bool>();
        private ulong[] _computeRootBufferAddresses = Array.Empty<ulong>();
        private bool[] _computeRootBufferAddressValid = Array.Empty<bool>();
        private ulong[] _graphicsRootTablePointers = Array.Empty<ulong>();
        private bool[] _graphicsRootTablePointerValid = Array.Empty<bool>();
        private ulong[] _computeRootTablePointers = Array.Empty<ulong>();
        private bool[] _computeRootTablePointerValid = Array.Empty<bool>();
        private readonly Stopwatch _perfStopwatch = Stopwatch.StartNew();
        private ulong _perfFrames;
        private ulong _perfBeginWaitCount;
        private double _perfBeginWaitMs;
        private ulong _perfTransitions;
        private ulong _perfSubresourceTransitions;
        private ulong _perfUavBarriers;
        private ulong _perfPipelineChanges;
        private ulong _perfResourceSetChanges;
        private ulong _perfDescriptorCopies;
        private ulong _perfRootTableSets;
        private ulong _perfVertexBufferBinds;
        private ulong _perfIndexBufferBinds;
        private ulong _perfDrawCalls;
        private ulong _perfDispatchCalls;
        private ulong _perfAccumBeginWaitCount;
        private double _perfAccumBeginWaitMs;
        private ulong _perfAccumTransitions;
        private ulong _perfAccumSubresourceTransitions;
        private ulong _perfAccumUavBarriers;
        private ulong _perfAccumPipelineChanges;
        private ulong _perfAccumResourceSetChanges;
        private ulong _perfAccumDescriptorCopies;
        private ulong _perfAccumRootTableSets;
        private ulong _perfAccumVertexBufferBinds;
        private ulong _perfAccumIndexBufferBinds;
        private ulong _perfAccumDrawCalls;
        private ulong _perfAccumDispatchCalls;
        private double _perfLastReportMs;

        public ID3D12GraphicsCommandList NativeCommandList => _nativeCommandList;

        public D3D12CommandList(D3D12GraphicsDevice gd, ref CommandListDescription description, GraphicsDeviceFeatures features, uint uniformAlignment, uint structuredAlignment) : base(ref description, features, uniformAlignment, structuredAlignment) {
            this.gd = gd;

            for (int i = 0; i < FramesInFlight; i++)
            {
                _commandAllocators[i] = gd.Device.CreateCommandAllocator(CommandListType.Direct);

                _shaderVisibleSrvUavHeaps[i] = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(
                    DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
                    _maxSrvUavDescriptors,
                    DescriptorHeapFlags.ShaderVisible));

                _shaderVisibleSamplerHeaps[i] = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(
                    DescriptorHeapType.Sampler,
                    _maxSamplerDescriptors,
                    DescriptorHeapFlags.ShaderVisible));

                _descriptorTableCaches[i] = new Dictionary<DescriptorCacheKey, GpuDescriptorHandle>(DescriptorCacheKeyComparer.Instance);
            }

            _nativeCommandList = gd.Device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, _commandAllocators[0], null);
            _srvUavDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            _samplerDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler);
            _beginEventMethod = getDebugMarkerMethod("BeginEvent");
            _setMarkerMethod = getDebugMarkerMethod("SetMarker");
            _endEventMethod = _nativeCommandList.GetType().GetMethod("EndEvent", Type.EmptyTypes);
            _nativeCommandList.Close();
        }

        public override bool IsDisposed => _disposed;

        public override string Name
        {
            get => name;
            set => name = value;
        }

        public override void Dispose()
        {
            _gpuMipPipeline?.Dispose();
            _gpuMipResourceLayout?.Dispose();
            _gpuMipSampler?.Dispose();
            _drawIndirectSignature?.Dispose();
            _drawIndexedIndirectSignature?.Dispose();
            _dispatchIndirectSignature?.Dispose();
            clearBoundResourceSets(_boundGraphicsResourceSets);
            clearBoundResourceSets(_boundComputeResourceSets);
            _nativeCommandList.Dispose();
            for (int i = 0; i < FramesInFlight; i++)
            {
                _commandAllocators[i]?.Dispose();
                _shaderVisibleSrvUavHeaps[i]?.Dispose();
                _shaderVisibleSamplerHeaps[i]?.Dispose();
            }
            _disposed = true;
        }

        public override void Begin()
        {
            if (_perfLogEnabled)
            {
                _perfBeginWaitCount = 0;
                _perfBeginWaitMs = 0;
                _perfTransitions = 0;
                _perfSubresourceTransitions = 0;
                _perfUavBarriers = 0;
                _perfPipelineChanges = 0;
                _perfResourceSetChanges = 0;
                _perfDescriptorCopies = 0;
                _perfRootTableSets = 0;
                _perfVertexBufferBinds = 0;
                _perfIndexBufferBinds = 0;
                _perfDrawCalls = 0;
                _perfDispatchCalls = 0;
            }

            _currentFrameSlot = (_currentFrameSlot + 1) % FramesInFlight;
            waitForFrameSlot(_currentFrameSlot);
            _commandAllocators[_currentFrameSlot].Reset();
            _nativeCommandList.Reset(_commandAllocators[_currentFrameSlot]);
            _begun = true;
            _ended = false;
            _transitionedBackBufferIndex = -1;
            _nextSrvUavDescriptor = _nextSrvUavDescriptorsPerFrameSlot[_currentFrameSlot];
            _nextSamplerDescriptor = _nextSamplerDescriptorsPerFrameSlot[_currentFrameSlot];
            _descriptorHeapsBound = false;
            _activeViewportCount = 0;
            _activeScissorRectCount = 0;
            _uavBarrierPending = false;
            Array.Clear(_boundVertexBuffers, 0, _boundVertexBuffers.Length);
            Array.Clear(_boundVertexBufferOffsets, 0, _boundVertexBufferOffsets.Length);
            Array.Clear(_boundVertexBufferVersions, 0, _boundVertexBufferVersions.Length);
            _boundIndexBuffer = null;
            _boundIndexBufferOffset = 0;
            _boundIndexBufferVersion = 0;
            _boundIndexFormat = IndexFormat.UInt16;
            _hasBoundIndexBuffer = false;
            clearBoundResourceSets(_boundGraphicsResourceSets);
            clearBoundResourceSets(_boundComputeResourceSets);
            invalidateGraphicsRootCaches();
            invalidateComputeRootCaches();
            _maxBoundVertexBufferSlot = 0;
            ClearCachedState();
            _currentGraphicsPipeline = null;
            _currentComputePipeline = null;
        }

        public override void End()
        {
            if (!_begun)
            {
                throw new VeldridException("CommandList.End cannot be called before Begin.");
            }

            flushPendingUavBarrier();
            transitionSwapchainBackBuffersToPresent();
            _nativeCommandList.Close();
            _ended = true;

            if (_perfLogEnabled)
            {
                _perfFrames++;
                _perfAccumBeginWaitCount += _perfBeginWaitCount;
                _perfAccumBeginWaitMs += _perfBeginWaitMs;
                _perfAccumTransitions += _perfTransitions;
                _perfAccumSubresourceTransitions += _perfSubresourceTransitions;
                _perfAccumUavBarriers += _perfUavBarriers;
                _perfAccumPipelineChanges += _perfPipelineChanges;
                _perfAccumResourceSetChanges += _perfResourceSetChanges;
                _perfAccumDescriptorCopies += _perfDescriptorCopies;
                _perfAccumRootTableSets += _perfRootTableSets;
                _perfAccumVertexBufferBinds += _perfVertexBufferBinds;
                _perfAccumIndexBufferBinds += _perfIndexBufferBinds;
                _perfAccumDrawCalls += _perfDrawCalls;
                _perfAccumDispatchCalls += _perfDispatchCalls;

                if ((_perfFrames % PerfReportIntervalFrames) == 0)
                {
                    double elapsedMs = _perfStopwatch.Elapsed.TotalMilliseconds;
                    double reportWindowMs = elapsedMs - _perfLastReportMs;
                    _perfLastReportMs = elapsedMs;
                    double invFrames = 1.0 / PerfReportIntervalFrames;
                    Console.WriteLine(
                        $"[D3D12 PERF] {PerfReportIntervalFrames}f/{reportWindowMs:F0}ms avg: " +
                        $"wait={_perfAccumBeginWaitMs * invFrames:F3}ms ({_perfAccumBeginWaitCount * invFrames:F2}x), " +
                        $"trans={_perfAccumTransitions * invFrames:F1}, subTrans={_perfAccumSubresourceTransitions * invFrames:F1}, uavB={_perfAccumUavBarriers * invFrames:F1}, " +
                        $"pso={_perfAccumPipelineChanges * invFrames:F1}, rs={_perfAccumResourceSetChanges * invFrames:F1}, " +
                        $"descCopy={_perfAccumDescriptorCopies * invFrames:F1}, rootTbl={_perfAccumRootTableSets * invFrames:F1}, " +
                        $"vb={_perfAccumVertexBufferBinds * invFrames:F1}, ib={_perfAccumIndexBufferBinds * invFrames:F1}, " +
                        $"draw={_perfAccumDrawCalls * invFrames:F1}, dispatch={_perfAccumDispatchCalls * invFrames:F1}");

                    _perfAccumBeginWaitCount = 0;
                    _perfAccumBeginWaitMs = 0;
                    _perfAccumTransitions = 0;
                    _perfAccumSubresourceTransitions = 0;
                    _perfAccumUavBarriers = 0;
                    _perfAccumPipelineChanges = 0;
                    _perfAccumResourceSetChanges = 0;
                    _perfAccumDescriptorCopies = 0;
                    _perfAccumRootTableSets = 0;
                    _perfAccumVertexBufferBinds = 0;
                    _perfAccumIndexBufferBinds = 0;
                    _perfAccumDrawCalls = 0;
                    _perfAccumDispatchCalls = 0;
                }
            }
        }

        public override void SetViewport(uint index, ref Viewport viewport)
        {
            if (index >= _activeViewports.Length)
            {
                return;
            }

            _activeViewports[index] = new Vortice.Mathematics.Viewport(
                viewport.X,
                viewport.Y,
                viewport.Width,
                viewport.Height,
                viewport.MinDepth,
                viewport.MaxDepth);

            if (index + 1 > _activeViewportCount)
            {
                _activeViewportCount = index + 1;
            }

            _nativeCommandList.RSSetViewports(_activeViewportCount, _activeViewports);
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            if (index >= _activeScissorRects.Length)
            {
                return;
            }

            _activeScissorRects[index] = new RawRect((int)x, (int)y, (int)(x + width), (int)(y + height));

            if (index + 1 > _activeScissorRectCount)
            {
                _activeScissorRectCount = index + 1;
            }

            _nativeCommandList.RSSetScissorRects(_activeScissorRectCount, _activeScissorRects);
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            flushPendingUavBarrier();
            _nativeCommandList.Dispatch(groupCountX, groupCountY, groupCountZ);
            if (_perfLogEnabled)
            {
                _perfDispatchCalls++;
            }
            _uavBarrierPending = true;
        }

        internal void ExecuteNoSignal()
        {
            if (!_ended)
            {
                throw new VeldridException("CommandList must be ended before submit.");
            }

            gd.CommandQueue.ExecuteCommandList(_nativeCommandList);
        }

        internal void MarkSubmitted(ulong signalValue)
        {
            if (_currentFrameSlot >= 0)
            {
                _frameSlotFenceValues[_currentFrameSlot] = signalValue;
            }
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (_currentGraphicsPipeline == null)
            {
                return;
            }

            Util.EnsureArrayMinimumSize(ref _boundGraphicsResourceSets, slot + 1);
            BoundResourceSetInfo previousBinding = _boundGraphicsResourceSets[slot];
            if (previousBinding.Equals(rs, dynamicOffsetsCount, ref dynamicOffsets))
            {
                return;
            }
            _boundGraphicsResourceSets[slot].Offsets.Dispose();
            _boundGraphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsetsCount, ref dynamicOffsets);
            if (_perfLogEnabled)
            {
                _perfResourceSetChanges++;
            }

            var d3d12Set = Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(rs);
            IBindableResource[] resources = d3d12Set.BoundResources;
            ResourceSetBindingPlanEntry[] bindingPlan = getGraphicsResourceSetBindingPlan(_currentGraphicsPipeline, slot, d3d12Set.ResourceLayoutInfo);
            uint dynamicOffsetIndex = 0;
            bool bindOnlyDynamicResources = ReferenceEquals(previousBinding.Set, rs);

            for (int i = 0; i < bindingPlan.Length; i++)
            {
                ref readonly ResourceSetBindingPlanEntry bindingEntry = ref bindingPlan[i];
                if (bindOnlyDynamicResources && !bindingEntry.IsDynamicBinding)
                {
                    continue;
                }

                uint dynamicOffset = 0;
                if (bindingEntry.IsDynamicBinding)
                {
                    if (dynamicOffsetIndex >= dynamicOffsetsCount)
                    {
                        throw new VeldridException("Insufficient dynamic offsets provided for ResourceSet binding.");
                    }

                    dynamicOffset = Unsafe.Add(ref dynamicOffsets, (int)dynamicOffsetIndex);
                    dynamicOffsetIndex++;
                }

                IBindableResource resource = resources[bindingEntry.ElementIndex];
                bindGraphicsResource(bindingEntry.BindingInfo, resource, dynamicOffset);
            }

        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (_currentComputePipeline == null)
            {
                return;
            }

            Util.EnsureArrayMinimumSize(ref _boundComputeResourceSets, slot + 1);
            BoundResourceSetInfo previousBinding = _boundComputeResourceSets[slot];
            if (previousBinding.Equals(set, dynamicOffsetsCount, ref dynamicOffsets))
            {
                return;
            }
            _boundComputeResourceSets[slot].Offsets.Dispose();
            _boundComputeResourceSets[slot] = new BoundResourceSetInfo(set, dynamicOffsetsCount, ref dynamicOffsets);
            if (_perfLogEnabled)
            {
                _perfResourceSetChanges++;
            }

            var d3d12Set = Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(set);
            IBindableResource[] resources = d3d12Set.BoundResources;
            ResourceSetBindingPlanEntry[] bindingPlan = getComputeResourceSetBindingPlan(_currentComputePipeline, slot, d3d12Set.ResourceLayoutInfo);
            uint dynamicOffsetIndex = 0;
            bool bindOnlyDynamicResources = ReferenceEquals(previousBinding.Set, set);

            for (int i = 0; i < bindingPlan.Length; i++)
            {
                ref readonly ResourceSetBindingPlanEntry bindingEntry = ref bindingPlan[i];
                if (bindOnlyDynamicResources && !bindingEntry.IsDynamicBinding)
                {
                    continue;
                }

                uint dynamicOffset = 0;
                if (bindingEntry.IsDynamicBinding)
                {
                    if (dynamicOffsetIndex >= dynamicOffsetsCount)
                    {
                        throw new VeldridException("Insufficient dynamic offsets provided for ResourceSet binding.");
                    }

                    dynamicOffset = Unsafe.Add(ref dynamicOffsets, (int)dynamicOffsetIndex);
                    dynamicOffsetIndex++;
                }

                IBindableResource resource = resources[bindingEntry.ElementIndex];
                bindComputeResource(bindingEntry.BindingInfo, resource, dynamicOffset);
            }

        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            if (fb is D3D12SwapchainFramebuffer swapchainFramebuffer
                && swapchainFramebuffer.Swapchain.TryGetCurrentBackBuffer(out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int backBufferIndex, out ResourceStates currentState))
            {
                transition(backBuffer, currentState, ResourceStates.RenderTarget);
                swapchainFramebuffer.Swapchain.SetBackBufferState(backBufferIndex, ResourceStates.RenderTarget);
                _transitionedBackBufferIndex = backBufferIndex;
                if (swapchainFramebuffer.DepthTargetTexture != null)
                {
                    transitionTexture(swapchainFramebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
                }

                if (swapchainFramebuffer.TryGetDepthStencilView(out CpuDescriptorHandle swapchainDsv))
                {
                    _nativeCommandList.OMSetRenderTargets(rtv, swapchainDsv);
                }
                else
                {
                    _nativeCommandList.OMSetRenderTargets(rtv, null);
                }
                return;
            }

            var d3d12Framebuffer = Util.AssertSubtype<Framebuffer, D3D12Framebuffer>(fb);
            foreach (D3D12Texture colorTexture in d3d12Framebuffer.ColorTargetTextures)
            {
                if (colorTexture != null)
                {
                    transitionTexture(colorTexture, ResourceStates.RenderTarget);
                }
            }

            if (d3d12Framebuffer.DepthTargetTexture != null)
            {
                transitionTexture(d3d12Framebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
            }

            if (!d3d12Framebuffer.TryGetColorTargetViews(out CpuDescriptorHandle[] rtvs))
            {
                if (d3d12Framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle depthOnlyDsv))
                {
                    _nativeCommandList.OMSetRenderTargets(Array.Empty<CpuDescriptorHandle>(), depthOnlyDsv);
                }
                return;
            }

            if (d3d12Framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv))
            {
                _nativeCommandList.OMSetRenderTargets(rtvs, dsv);
            }
            else
            {
                _nativeCommandList.OMSetRenderTargets(rtvs, null);
            }
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            var d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(indirectBuffer);
            uint argumentSize = (uint)Unsafe.SizeOf<IndirectDrawArguments>();
            if (drawCount > 0)
            {
                ulong requiredSize = (ulong)offset + (((ulong)drawCount - 1UL) * stride) + argumentSize;
                if (requiredSize > d3d12Buffer.SizeInBytes)
                {
                    throw new VeldridException("Indirect draw argument range exceeds buffer bounds.");
                }
            }

            if (ensureIndirectCommandSignatures())
            {
                executeIndirect(d3d12Buffer, offset, drawCount, stride, argumentSize, _drawIndirectSignature);
                return;
            }

            // Fallback path if command signatures are unavailable.
            unsafe
            {
                if (!d3d12Buffer.TryGetCpuReadPointer(out IntPtr mappedPointer))
                {
                    throw new PlatformNotSupportedException("D3D12 indirect fallback requires a CPU-readable argument buffer.");
                }

                byte* basePtr = (byte*)mappedPointer + offset;
                for (uint i = 0; i < drawCount; i++)
                {
                    IndirectDrawArguments arguments = *(IndirectDrawArguments*)(basePtr + (i * stride));
                    _nativeCommandList.DrawInstanced(arguments.VertexCount, arguments.InstanceCount, arguments.FirstVertex, arguments.FirstInstance);
                }
            }
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            var d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(indirectBuffer);
            uint argumentSize = (uint)Unsafe.SizeOf<IndirectDrawIndexedArguments>();
            if (drawCount > 0)
            {
                ulong requiredSize = (ulong)offset + (((ulong)drawCount - 1UL) * stride) + argumentSize;
                if (requiredSize > d3d12Buffer.SizeInBytes)
                {
                    throw new VeldridException("Indirect indexed draw argument range exceeds buffer bounds.");
                }
            }

            if (ensureIndirectCommandSignatures())
            {
                executeIndirect(d3d12Buffer, offset, drawCount, stride, argumentSize, _drawIndexedIndirectSignature);
                return;
            }

            // Fallback path if command signatures are unavailable.
            unsafe
            {
                if (!d3d12Buffer.TryGetCpuReadPointer(out IntPtr mappedPointer))
                {
                    throw new PlatformNotSupportedException("D3D12 indirect fallback requires a CPU-readable argument buffer.");
                }

                byte* basePtr = (byte*)mappedPointer + offset;
                for (uint i = 0; i < drawCount; i++)
                {
                    IndirectDrawIndexedArguments arguments = *(IndirectDrawIndexedArguments*)(basePtr + (i * stride));
                    _nativeCommandList.DrawIndexedInstanced(arguments.IndexCount, arguments.InstanceCount, arguments.FirstIndex, arguments.VertexOffset, arguments.FirstInstance);
                }
            }
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
            var d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(indirectBuffer);
            uint argumentSize = (uint)Unsafe.SizeOf<IndirectDispatchArguments>();
            ulong requiredSize = (ulong)offset + argumentSize;
            if (requiredSize > d3d12Buffer.SizeInBytes)
            {
                throw new VeldridException("Indirect dispatch argument range exceeds buffer bounds.");
            }

            if (ensureIndirectCommandSignatures())
            {
                executeIndirect(d3d12Buffer, offset, 1, argumentSize, argumentSize, _dispatchIndirectSignature);
                return;
            }

            // Fallback path if command signatures are unavailable.
            unsafe
            {
                if (!d3d12Buffer.TryGetCpuReadPointer(out IntPtr mappedPointer))
                {
                    throw new PlatformNotSupportedException("D3D12 indirect fallback requires a CPU-readable argument buffer.");
                }

                IndirectDispatchArguments arguments = *(IndirectDispatchArguments*)((byte*)mappedPointer + offset);
                _nativeCommandList.Dispatch(arguments.GroupCountX, arguments.GroupCountY, arguments.GroupCountZ);
            }
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            flushPendingUavBarrier();
            var src = Util.AssertSubtype<Texture, D3D12Texture>(source);
            var dst = Util.AssertSubtype<Texture, D3D12Texture>(destination);

            if (src.NativeTexture == null || dst.NativeTexture == null)
            {
                src.CopyRegionTo(
                    dst,
                    0, 0, 0,
                    0, 0,
                    0, 0, 0,
                    0, 0,
                    source.Width,
                    source.Height,
                    source.Depth,
                    source.ArrayLayers);
                return;
            }

            ResourceStates[] srcPreviousStates = captureTextureStates(src);
            ResourceStates[] dstPreviousStates = captureTextureStates(dst);
            transitionTexture(src, ResourceStates.ResolveSource);
            transitionTexture(dst, ResourceStates.ResolveDest);

            Format resolveFormat = D3D12Formats.ToDxgiFormat(source.Format);
            uint mipLevels = Math.Min(source.MipLevels, destination.MipLevels);
            uint arrayLayers = Math.Min(source.ArrayLayers, destination.ArrayLayers);
            for (uint arrayLayer = 0; arrayLayer < arrayLayers; arrayLayer++)
            {
                for (uint mipLevel = 0; mipLevel < mipLevels; mipLevel++)
                {
                    uint srcSubresource = source.CalculateSubresource(mipLevel, arrayLayer);
                    uint dstSubresource = destination.CalculateSubresource(mipLevel, arrayLayer);
                    _nativeCommandList.ResolveSubresource(dst.NativeTexture, dstSubresource, src.NativeTexture, srcSubresource, resolveFormat);
                }
            }

            restoreTextureStates(src, srcPreviousStates);
            restoreTextureStates(dst, dstPreviousStates);
        }

        protected override void CopyBufferCore(DeviceBuffer source, uint sourceOffset, DeviceBuffer destination, uint destinationOffset, uint sizeInBytes)
        {
            flushPendingUavBarrier();
            var src = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(source);
            var dst = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(destination);
            src.CopyTo(_nativeCommandList, dst, sourceOffset, destinationOffset, sizeInBytes);
        }

        protected override void CopyTextureCore(
            Texture source,
            uint srcX,
            uint srcY,
            uint srcZ,
            uint srcMipLevel,
            uint srcBaseArrayLayer,
            Texture destination,
            uint dstX,
            uint dstY,
            uint dstZ,
            uint dstMipLevel,
            uint dstBaseArrayLayer,
            uint width,
            uint height,
            uint depth,
            uint layerCount)
        {
            flushPendingUavBarrier();
            var src = Util.AssertSubtype<Texture, D3D12Texture>(source);
            var dst = Util.AssertSubtype<Texture, D3D12Texture>(destination);

            if (src.NativeTexture != null && dst.NativeTexture != null)
            {
                ResourceStates[] srcPreviousStates = captureTextureStates(src);
                ResourceStates[] dstPreviousStates = captureTextureStates(dst);
                transitionTexture(src, ResourceStates.CopySource);
                transitionTexture(dst, ResourceStates.CopyDest);

                for (uint layer = 0; layer < layerCount; layer++)
                {
                    uint srcSubresource = source.CalculateSubresource(srcMipLevel, srcBaseArrayLayer + layer);
                    uint dstSubresource = destination.CalculateSubresource(dstMipLevel, dstBaseArrayLayer + layer);
                    var srcLocation = new TextureCopyLocation(src.NativeTexture, srcSubresource);
                    var dstLocation = new TextureCopyLocation(dst.NativeTexture, dstSubresource);
                    var srcBox = new Box(
                        (int)srcX,
                        (int)srcY,
                        (int)srcZ,
                        (int)(srcX + width),
                        (int)(srcY + height),
                        (int)(srcZ + depth));
                    _nativeCommandList.CopyTextureRegion(dstLocation, dstX, dstY, dstZ, srcLocation, srcBox);
                }

                restoreTextureStates(src, srcPreviousStates);
                restoreTextureStates(dst, dstPreviousStates);
                return;
            }

            src.CopyRegionTo(
                dst,
                srcX,
                srcY,
                srcZ,
                srcMipLevel,
                srcBaseArrayLayer,
                dstX,
                dstY,
                dstZ,
                dstMipLevel,
                dstBaseArrayLayer,
                width,
                height,
                depth,
                layerCount);
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            if (pipeline.IsComputePipeline)
            {
                var d3d12ComputePipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline);
                if (ReferenceEquals(_currentComputePipeline, d3d12ComputePipeline))
                {
                    return;
                }

                bool computeRootSignatureChanged = !ReferenceEquals(_currentComputePipeline?.RootSignature, d3d12ComputePipeline.RootSignature);
                _currentComputePipeline = d3d12ComputePipeline;
                _currentGraphicsPipeline = null;
                if (_perfLogEnabled)
                {
                    _perfPipelineChanges++;
                }
                _nativeCommandList.SetPipelineState(d3d12ComputePipeline.PipelineState);
                if (computeRootSignatureChanged)
                {
                    clearBoundResourceSets(_boundComputeResourceSets);
                    invalidateComputeRootCaches();
                    _nativeCommandList.SetComputeRootSignature(d3d12ComputePipeline.RootSignature);
                }
                return;
            }

            var d3d12Pipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline);
            if (ReferenceEquals(_currentGraphicsPipeline, d3d12Pipeline))
            {
                return;
            }

            bool rootSignatureChanged = !ReferenceEquals(_currentGraphicsPipeline?.RootSignature, d3d12Pipeline.RootSignature);
            _currentGraphicsPipeline = d3d12Pipeline;
            _currentComputePipeline = null;
            if (_perfLogEnabled)
            {
                _perfPipelineChanges++;
            }
            _nativeCommandList.SetPipelineState(d3d12Pipeline.PipelineState);
            if (rootSignatureChanged)
            {
                clearBoundResourceSets(_boundGraphicsResourceSets);
                invalidateGraphicsRootCaches();
                _nativeCommandList.SetGraphicsRootSignature(d3d12Pipeline.RootSignature);
            }
            _nativeCommandList.IASetPrimitiveTopology(d3d12Pipeline.PrimitiveTopology);
            rebindVertexBuffersForCurrentPipeline();
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            if (index >= _boundVertexBuffers.Length)
            {
                return;
            }

            var d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
            bool isDynamicBuffer = (d3d12Buffer.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
            ulong bindVersion = d3d12Buffer.BindVersion;
            if (ReferenceEquals(_boundVertexBuffers[index], d3d12Buffer)
                && _boundVertexBufferOffsets[index] == offset
                && (!isDynamicBuffer || _boundVertexBufferVersions[index] == bindVersion))
            {
                return;
            }

            _boundVertexBuffers[index] = d3d12Buffer;
            _boundVertexBufferOffsets[index] = offset;
            _boundVertexBufferVersions[index] = bindVersion;
            if (index + 1 > _maxBoundVertexBufferSlot)
            {
                _maxBoundVertexBufferSlot = index + 1;
            }

            bindVertexBuffer(index, d3d12Buffer, offset);
            if (_perfLogEnabled)
            {
                _perfVertexBufferBinds++;
            }
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            var d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
            bool isDynamicBuffer = (d3d12Buffer.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
            ulong bindVersion = d3d12Buffer.BindVersion;
            if (_hasBoundIndexBuffer
                && ReferenceEquals(_boundIndexBuffer, d3d12Buffer)
                && _boundIndexBufferOffset == offset
                && _boundIndexFormat == format
                && (!isDynamicBuffer || _boundIndexBufferVersion == bindVersion))
            {
                return;
            }

            transitionBuffer(d3d12Buffer, ResourceStates.IndexBuffer);
            uint viewSize = d3d12Buffer.GetBindableSize(offset);
            var indexView = new IndexBufferView(
                d3d12Buffer.GetGpuVirtualAddress(offset),
                viewSize,
                D3D12Formats.ToDxgiFormat(format));
            _nativeCommandList.IASetIndexBuffer(indexView);
            _boundIndexBuffer = d3d12Buffer;
            _boundIndexBufferOffset = offset;
            _boundIndexBufferVersion = bindVersion;
            _boundIndexFormat = format;
            _hasBoundIndexBuffer = true;
            if (_perfLogEnabled)
            {
                _perfIndexBufferBinds++;
            }
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            flushPendingUavBarrier();
            if (Framebuffer is D3D12SwapchainFramebuffer swapchainFramebuffer
                && swapchainFramebuffer.Swapchain.TryGetCurrentBackBuffer(out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int backBufferIndex, out ResourceStates currentState))
            {
                transition(backBuffer, currentState, ResourceStates.RenderTarget);
                swapchainFramebuffer.Swapchain.SetBackBufferState(backBufferIndex, ResourceStates.RenderTarget);
                _transitionedBackBufferIndex = backBufferIndex;
                if (swapchainFramebuffer.TryGetDepthStencilView(out CpuDescriptorHandle swapchainDsv))
                {
                    _nativeCommandList.OMSetRenderTargets(rtv, swapchainDsv);
                }
                else
                {
                    _nativeCommandList.OMSetRenderTargets(rtv, null);
                }
                _nativeCommandList.ClearRenderTargetView(rtv, new Color4(clearColor.R, clearColor.G, clearColor.B, clearColor.A), 0, null);
                return;
            }

            if (Framebuffer is D3D12Framebuffer d3d12Framebuffer
                && d3d12Framebuffer.TryGetColorTargetView(index, out CpuDescriptorHandle offscreenRtv))
            {
                if (index < d3d12Framebuffer.ColorTargetTextures.Length)
                {
                    D3D12Texture colorTexture = d3d12Framebuffer.ColorTargetTextures[(int)index];
                    if (colorTexture != null)
                    {
                        transitionTexture(colorTexture, ResourceStates.RenderTarget);
                    }
                }

                _nativeCommandList.ClearRenderTargetView(offscreenRtv, new Color4(clearColor.R, clearColor.G, clearColor.B, clearColor.A), 0, null);
            }
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            flushPendingUavBarrier();
            if (Framebuffer is not D3D12Framebuffer d3d12Framebuffer)
            {
                return;
            }

            if (d3d12Framebuffer.DepthTargetTexture == null)
            {
                return;
            }

            transitionTexture(d3d12Framebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
            if (d3d12Framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv))
            {
                _nativeCommandList.ClearDepthStencilView(dsv, ClearFlags.Depth | ClearFlags.Stencil, depth, stencil, 0, null);
            }
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            flushPendingUavBarrier();
            _nativeCommandList.DrawInstanced(vertexCount, instanceCount, vertexStart, instanceStart);
            if (_perfLogEnabled)
            {
                _perfDrawCalls++;
            }
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            flushPendingUavBarrier();
            _nativeCommandList.DrawIndexedInstanced(indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
            if (_perfLogEnabled)
            {
                _perfDrawCalls++;
            }
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            flushPendingUavBarrier();
            var d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
            ID3D12Resource temporaryUpload = d3d12Buffer.Update(_nativeCommandList, source, bufferOffsetInBytes, sizeInBytes);
            if (temporaryUpload != null)
            {
                gd.DisposeWhenIdle(temporaryUpload);
            }
        }

        [SupportedOSPlatform("windows")]
        private protected override void GenerateMipmapsCore(Texture texture)
        {
            var d3d12Texture = Util.AssertSubtype<Texture, D3D12Texture>(texture);
            if (texture.MipLevels <= 1 || d3d12Texture.NativeTexture == null)
            {
                return;
            }

            if (!gd.GetPixelFormatSupport(texture.Format, texture.Type, texture.Usage))
            {
                throw new PlatformNotSupportedException("GenerateMipmaps is not supported for this D3D12 texture format/type/usage combination.");
            }

            if (canUseGpuMipmapPath(texture) && ensureGpuMipmapResources())
            {
                generateMipmapsGpu(d3d12Texture);
                return;
            }

            if (!d3d12Texture.GenerateMipmapsCpu())
            {
                throw new PlatformNotSupportedException("D3D12 mip generation currently supports only uncompressed color textures.");
            }

            d3d12Texture.UploadGeneratedMipmaps();
        }

        private protected override void PushDebugGroupCore(string name)
        {
            writeDebugMarker(name, beginEvent: true, setMarker: false);
        }

        private protected override void PopDebugGroupCore()
        {
            _endEventMethod?.Invoke(_nativeCommandList, null);
        }

        private protected override void InsertDebugMarkerCore(string name)
        {
            writeDebugMarker(name, beginEvent: false, setMarker: true);
        }

        private void transition(ID3D12Resource resource, ResourceStates from, ResourceStates to)
        {
            if (from == to)
            {
                return;
            }

            ResourceBarrier barrier = ResourceBarrier.BarrierTransition(resource, from, to, VorticeD3D12.ResourceBarrierAllSubResources, ResourceBarrierFlags.None);
            _singleBarrier[0] = barrier;
            _nativeCommandList.ResourceBarrier(_singleBarrier);
            if (_perfLogEnabled)
            {
                _perfTransitions++;
            }
        }

        private void bindVertexBuffer(uint index, D3D12DeviceBuffer buffer, uint offset)
        {
            transitionBuffer(buffer, ResourceStates.VertexAndConstantBuffer);

            uint stride = 0;
            if (_currentGraphicsPipeline != null && index < _currentGraphicsPipeline.VertexStrides.Length)
            {
                stride = _currentGraphicsPipeline.VertexStrides[index];
            }

            uint viewSize = buffer.GetBindableSize(offset);
            var view = new VertexBufferView(
                buffer.GetGpuVirtualAddress(offset),
                viewSize,
                stride);
            _nativeCommandList.IASetVertexBuffers(index, view);
        }

        private void rebindVertexBuffersForCurrentPipeline()
        {
            if (_currentGraphicsPipeline == null)
            {
                return;
            }

            for (uint index = 0; index < _maxBoundVertexBufferSlot; index++)
            {
                D3D12DeviceBuffer buffer = _boundVertexBuffers[index];
                if (buffer == null)
                {
                    continue;
                }

                bindVertexBuffer(index, buffer, _boundVertexBufferOffsets[index]);
            }
        }

        private void transitionSubresource(ID3D12Resource resource, ResourceStates from, ResourceStates to, uint subresource)
        {
            if (from == to)
            {
                return;
            }

            ResourceBarrier barrier = ResourceBarrier.BarrierTransition(resource, from, to, subresource, ResourceBarrierFlags.None);
            _singleBarrier[0] = barrier;
            _nativeCommandList.ResourceBarrier(_singleBarrier);
            if (_perfLogEnabled)
            {
                _perfSubresourceTransitions++;
            }
        }

        private void flushPendingUavBarrier()
        {
            if (!_uavBarrierPending)
            {
                return;
            }

            ResourceBarrier barrier = ResourceBarrier.BarrierUnorderedAccessView(null);
            _singleBarrier[0] = barrier;
            _nativeCommandList.ResourceBarrier(_singleBarrier);
            if (_perfLogEnabled)
            {
                _perfUavBarriers++;
            }
            _uavBarrierPending = false;
        }

        private void waitForFrameSlot(int frameSlot)
        {
            ulong fenceValue = _frameSlotFenceValues[frameSlot];
            if (fenceValue == 0)
            {
                return;
            }

            if (gd.IsSubmissionFenceComplete(fenceValue))
            {
                return;
            }

            long startTicks = 0;
            if (_perfLogEnabled)
            {
                startTicks = Stopwatch.GetTimestamp();
            }
            gd.WaitForSubmissionFence(fenceValue);
            if (_perfLogEnabled)
            {
                long elapsedTicks = Stopwatch.GetTimestamp() - startTicks;
                _perfBeginWaitMs += elapsedTicks * 1000.0 / Stopwatch.Frequency;
                _perfBeginWaitCount++;
            }
        }

        private void executeIndirect(
            D3D12DeviceBuffer argumentBuffer,
            uint offset,
            uint drawCount,
            uint stride,
            uint argumentSize,
            ID3D12CommandSignature signature)
        {
            if (drawCount == 0)
            {
                return;
            }

            flushPendingUavBarrier();
            ResourceStates previousState = argumentBuffer.CurrentState;
            transitionBuffer(argumentBuffer, ResourceStates.IndirectArgument);

            if (stride == argumentSize)
            {
                _nativeCommandList.ExecuteIndirect(signature, drawCount, argumentBuffer.NativeBuffer, offset, null, 0);
                transitionBuffer(argumentBuffer, previousState);
                return;
            }

            for (uint i = 0; i < drawCount; i++)
            {
                ulong commandOffset = offset + ((ulong)i * stride);
                _nativeCommandList.ExecuteIndirect(signature, 1, argumentBuffer.NativeBuffer, commandOffset, null, 0);
            }

            transitionBuffer(argumentBuffer, previousState);
            _uavBarrierPending = true;
        }

        private bool ensureIndirectCommandSignatures()
        {
            if (_indirectSignaturesInitialized)
            {
                return _indirectSignaturesAvailable;
            }

            _indirectSignaturesInitialized = true;
            try
            {
                IndirectArgumentDescription drawArgument = default;
                drawArgument.Type = IndirectArgumentType.Draw;
                CommandSignatureDescription drawDescription = default;
                drawDescription.ByteStride = Unsafe.SizeOf<IndirectDrawArguments>();
                drawDescription.IndirectArguments = new[] { drawArgument };
                _drawIndirectSignature = createCommandSignature(drawDescription);

                IndirectArgumentDescription drawIndexedArgument = default;
                drawIndexedArgument.Type = IndirectArgumentType.DrawIndexed;
                CommandSignatureDescription drawIndexedDescription = default;
                drawIndexedDescription.ByteStride = Unsafe.SizeOf<IndirectDrawIndexedArguments>();
                drawIndexedDescription.IndirectArguments = new[] { drawIndexedArgument };
                _drawIndexedIndirectSignature = createCommandSignature(drawIndexedDescription);

                IndirectArgumentDescription dispatchArgument = default;
                dispatchArgument.Type = IndirectArgumentType.Dispatch;
                CommandSignatureDescription dispatchDescription = default;
                dispatchDescription.ByteStride = Unsafe.SizeOf<IndirectDispatchArguments>();
                dispatchDescription.IndirectArguments = new[] { dispatchArgument };
                _dispatchIndirectSignature = createCommandSignature(dispatchDescription);

                _indirectSignaturesAvailable = _drawIndirectSignature != null
                    && _drawIndexedIndirectSignature != null
                    && _dispatchIndirectSignature != null;
            }
            catch
            {
                _indirectSignaturesAvailable = false;
            }

            return _indirectSignaturesAvailable;
        }

        private ID3D12CommandSignature createCommandSignature(CommandSignatureDescription description)
        {
            ID3D12CommandSignature signature = gd.Device.CreateCommandSignature<ID3D12CommandSignature>(description, null);
            if (signature == null)
            {
                throw new VeldridException("Unable to create D3D12 command signature.");
            }

            return signature;
        }

        private bool canUseGpuMipmapPath(Texture texture)
        {
            return texture.Type == TextureType.Texture2D
                   && texture.SampleCount == TextureSampleCount.Count1
                   && (texture.Usage & TextureUsage.Cubemap) == 0
                   && (texture.Usage & (TextureUsage.Sampled | TextureUsage.Storage)) == (TextureUsage.Sampled | TextureUsage.Storage)
                   && gd.GetPixelFormatSupport(texture.Format, texture.Type, TextureUsage.Sampled | TextureUsage.Storage)
                   && !FormatHelpers.IsCompressedFormat(texture.Format)
                   && (texture.Usage & TextureUsage.DepthStencil) == 0;
        }

        private void generateMipmapsGpu(D3D12Texture texture)
        {
            D3D12Pipeline previousGraphics = _currentGraphicsPipeline;
            D3D12Pipeline previousCompute = _currentComputePipeline;

            _nativeCommandList.SetPipelineState(_gpuMipPipeline.PipelineState);
            _nativeCommandList.SetComputeRootSignature(_gpuMipPipeline.RootSignature);
            _currentGraphicsPipeline = null;
            _currentComputePipeline = _gpuMipPipeline;

            uint layerCount = texture.ArrayLayers;
            uint subresourceCount = texture.MipLevels * layerCount;
            ResourceStates[] previousStates = captureTextureStates(texture);
            ResourceStates[] subresourceStates = new ResourceStates[subresourceCount];
            for (uint subresource = 0; subresource < subresourceCount; subresource++)
            {
                subresourceStates[subresource] = previousStates[subresource];
            }

            try
            {
                for (uint layer = 0; layer < layerCount; layer++)
                {
                    for (uint mipLevel = 1; mipLevel < texture.MipLevels; mipLevel++)
                    {
                        uint srcSubresource = texture.CalculateSubresource(mipLevel - 1, layer);
                        uint dstSubresource = texture.CalculateSubresource(mipLevel, layer);

                        if (subresourceStates[srcSubresource] != ResourceStates.NonPixelShaderResource)
                        {
                            transitionSubresource(texture.NativeTexture, subresourceStates[srcSubresource], ResourceStates.NonPixelShaderResource, srcSubresource);
                            subresourceStates[srcSubresource] = ResourceStates.NonPixelShaderResource;
                        }

                        if (subresourceStates[dstSubresource] != ResourceStates.UnorderedAccess)
                        {
                            transitionSubresource(texture.NativeTexture, subresourceStates[dstSubresource], ResourceStates.UnorderedAccess, dstSubresource);
                            subresourceStates[dstSubresource] = ResourceStates.UnorderedAccess;
                        }

                        using TextureView srcView = gd.ResourceFactory.CreateTextureView(new TextureViewDescription(texture, mipLevel - 1, 1, layer, 1));
                        using TextureView dstView = gd.ResourceFactory.CreateTextureView(new TextureViewDescription(texture, mipLevel, 1, layer, 1));
                        using ResourceSet mipResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_gpuMipResourceLayout, srcView, dstView, _gpuMipSampler));

                        SetComputeResourceSet(0, mipResourceSet);
                        Util.GetMipDimensions(texture, mipLevel, out uint mipWidth, out uint mipHeight, out _);
                        uint groupCountX = (mipWidth + 7) / 8;
                        uint groupCountY = (mipHeight + 7) / 8;
                        _nativeCommandList.Dispatch(groupCountX, groupCountY, 1);
                    }
                }

                for (uint subresource = 0; subresource < subresourceCount; subresource++)
                {
                    if (subresourceStates[subresource] != previousStates[subresource])
                    {
                        transitionSubresource(texture.NativeTexture, subresourceStates[subresource], previousStates[subresource], subresource);
                        subresourceStates[subresource] = previousStates[subresource];
                    }
                }

                for (uint subresource = 0; subresource < subresourceCount; subresource++)
                {
                    texture.SetSubresourceState(subresource, subresourceStates[subresource]);
                }
            }
            finally
            {
                if (previousCompute != null)
                {
                    _nativeCommandList.SetPipelineState(previousCompute.PipelineState);
                    _nativeCommandList.SetComputeRootSignature(previousCompute.RootSignature);
                    _currentComputePipeline = previousCompute;
                    _currentGraphicsPipeline = null;
                }
                else if (previousGraphics != null)
                {
                    _nativeCommandList.SetPipelineState(previousGraphics.PipelineState);
                    _nativeCommandList.SetGraphicsRootSignature(previousGraphics.RootSignature);
                    _nativeCommandList.IASetPrimitiveTopology(previousGraphics.PrimitiveTopology);
                    _currentGraphicsPipeline = previousGraphics;
                    _currentComputePipeline = null;
                }
                else
                {
                    _currentComputePipeline = null;
                    _currentGraphicsPipeline = null;
                }
            }
        }

        [SupportedOSPlatform("windows")]
        private bool ensureGpuMipmapResources()
        {
            if (_gpuMipResourcesInitialized)
            {
                return _gpuMipResourcesAvailable;
            }

            _gpuMipResourcesInitialized = true;
            try
            {
                byte[] shaderBytes = compileComputeShader(_mipmapComputeShaderCode, "cs_main", "cs_5_0");
                using Shader mipShader = gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Compute, shaderBytes, "cs_main"));

                ResourceLayoutDescription resourceLayoutDescription = new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("DestinationTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("LinearSampler", ResourceKind.Sampler, ShaderStages.Compute));

                _gpuMipResourceLayout = gd.ResourceFactory.CreateResourceLayout(resourceLayoutDescription);
                var samplerDescription = SamplerDescription.LINEAR;
                samplerDescription.AddressModeU = SamplerAddressMode.Clamp;
                samplerDescription.AddressModeV = SamplerAddressMode.Clamp;
                samplerDescription.AddressModeW = SamplerAddressMode.Clamp;
                _gpuMipSampler = gd.ResourceFactory.CreateSampler(samplerDescription);

                var computePipelineDescription = new ComputePipelineDescription(
                    mipShader,
                    new[] { _gpuMipResourceLayout },
                    8,
                    8,
                    1);
                _gpuMipPipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(gd.ResourceFactory.CreateComputePipeline(computePipelineDescription));
                _gpuMipResourcesAvailable = true;
            }
            catch
            {
                _gpuMipResourcesAvailable = false;
            }

            return _gpuMipResourcesAvailable;
        }

        [SupportedOSPlatform("windows")]
        private static byte[] compileComputeShader(string sourceCode, string entryPoint, string target)
        {
            byte[] sourceBytes = Encoding.UTF8.GetBytes(sourceCode);
            int result = D3DCompile(
                sourceBytes,
                (nuint)sourceBytes.Length,
                null,
                IntPtr.Zero,
                IntPtr.Zero,
                entryPoint,
                target,
                0,
                0,
                out IntPtr codeBlobPtr,
                out IntPtr errorBlobPtr);

            string errorMessage = null;
            if (errorBlobPtr != IntPtr.Zero)
            {
                try
                {
                    var errorBlob = (ID3DBlob)Marshal.GetObjectForIUnknown(errorBlobPtr);
                    IntPtr errorPtr = errorBlob.GetBufferPointer();
                    int errorSize = checked((int)errorBlob.GetBufferSize());
                    if (errorSize > 0)
                    {
                        byte[] errorBytes = new byte[errorSize];
                        Marshal.Copy(errorPtr, errorBytes, 0, errorSize);
                        errorMessage = Encoding.UTF8.GetString(errorBytes).TrimEnd('\0', '\r', '\n');
                    }
                }
                finally
                {
                    Marshal.Release(errorBlobPtr);
                }
            }

            if (result < 0 || codeBlobPtr == IntPtr.Zero)
            {
                throw new VeldridException($"Failed to compile D3D12 mipmap compute shader. {errorMessage}");
            }

            try
            {
                var codeBlob = (ID3DBlob)Marshal.GetObjectForIUnknown(codeBlobPtr);
                IntPtr codePtr = codeBlob.GetBufferPointer();
                int codeSize = checked((int)codeBlob.GetBufferSize());
                byte[] shaderBytes = new byte[codeSize];
                Marshal.Copy(codePtr, shaderBytes, 0, codeSize);
                return shaderBytes;
            }
            finally
            {
                Marshal.Release(codeBlobPtr);
            }
        }

        private void transitionTexture(D3D12Texture texture, ResourceStates toState)
        {
            if (texture.NativeTexture == null)
            {
                return;
            }

            if (texture.TryGetCommonState(out ResourceStates commonState))
            {
                if (commonState == toState)
                {
                    return;
                }

                transition(texture.NativeTexture, commonState, toState);
                texture.SetAllSubresourceStates(toState);
                return;
            }

            uint subresourceCount = texture.SubresourceCount;
            for (uint subresource = 0; subresource < subresourceCount; subresource++)
            {
                ResourceStates fromState = texture.GetSubresourceState(subresource);
                if (fromState == toState)
                {
                    continue;
                }

                transitionSubresource(texture.NativeTexture, fromState, toState, subresource);
                texture.SetSubresourceState(subresource, toState);
            }
        }

        private void transitionTextureView(D3D12TextureView textureView, ResourceStates toState)
        {
            D3D12Texture texture = textureView.TargetTexture;
            if (texture.NativeTexture == null)
            {
                return;
            }

            uint mipStart = textureView.BaseMipLevel;
            uint mipEnd = mipStart + textureView.MipLevels;
            uint layerStart = textureView.BaseArrayLayer;
            uint layerEnd = layerStart + textureView.ArrayLayers;
            if ((texture.Usage & TextureUsage.Cubemap) != 0)
            {
                layerStart *= 6;
                layerEnd *= 6;
            }

            bool fullResourceView =
                mipStart == 0
                && mipEnd == texture.MipLevels
                && layerStart == 0
                && layerEnd == texture.EffectiveArrayLayers;

            if (fullResourceView)
            {
                transitionTexture(texture, toState);
                return;
            }

            for (uint layer = layerStart; layer < layerEnd; layer++)
            {
                for (uint mip = mipStart; mip < mipEnd; mip++)
                {
                    uint subresource = texture.CalculateSubresource(mip, layer);
                    ResourceStates fromState = texture.GetSubresourceState(subresource);
                    if (fromState == toState)
                    {
                        continue;
                    }

                    transitionSubresource(texture.NativeTexture, fromState, toState, subresource);
                    texture.SetSubresourceState(subresource, toState);
                }
            }
        }

        private void transitionBuffer(D3D12DeviceBuffer buffer, ResourceStates toState)
        {
            if (!buffer.CanTransitionState || buffer.CurrentState == toState)
            {
                return;
            }

            transition(buffer.NativeBuffer, buffer.CurrentState, toState);
            buffer.CurrentState = toState;
        }

        private static ResourceStates[] captureTextureStates(D3D12Texture texture)
        {
            uint subresourceCount = texture.SubresourceCount;
            var states = new ResourceStates[subresourceCount];
            for (uint subresource = 0; subresource < subresourceCount; subresource++)
            {
                states[subresource] = texture.GetSubresourceState(subresource);
            }

            return states;
        }

        private void restoreTextureStates(D3D12Texture texture, ResourceStates[] previousStates)
        {
            if (texture.NativeTexture == null || previousStates == null || previousStates.Length == 0)
            {
                return;
            }

            uint subresourceCount = Math.Min(texture.SubresourceCount, (uint)previousStates.Length);
            for (uint subresource = 0; subresource < subresourceCount; subresource++)
            {
                ResourceStates current = texture.GetSubresourceState(subresource);
                ResourceStates previous = previousStates[subresource];
                if (current == previous)
                {
                    continue;
                }

                transitionSubresource(texture.NativeTexture, current, previous, subresource);
                texture.SetSubresourceState(subresource, previous);
            }
        }

        private unsafe void writeDebugMarker(string name, bool beginEvent, bool setMarker)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            byte[] utf8Bytes = Encoding.UTF8.GetBytes(name);
            fixed (byte* bytesPtr = utf8Bytes)
            {
                IntPtr dataPtr = (IntPtr)bytesPtr;
                int size = utf8Bytes.Length;
                MethodInfo markerMethod = beginEvent ? _beginEventMethod : _setMarkerMethod;
                if (markerMethod == null)
                {
                    return;
                }

                ParameterInfo[] parameters = markerMethod.GetParameters();
                object metadata = parameters[0].ParameterType == typeof(int) ? 0 : 0u;
                object sizeValue = parameters[2].ParameterType == typeof(int) ? size : (uint)size;
                if (beginEvent)
                {
                    markerMethod.Invoke(_nativeCommandList, new[] { metadata, (object)dataPtr, sizeValue });
                }
                else if (setMarker)
                {
                    markerMethod.Invoke(_nativeCommandList, new[] { metadata, (object)dataPtr, sizeValue });
                }
            }
        }

        private MethodInfo getDebugMarkerMethod(string methodName)
        {
            MethodInfo[] methods = _nativeCommandList.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 3
                    && parameters[1].ParameterType == typeof(IntPtr)
                    && (parameters[0].ParameterType == typeof(uint) || parameters[0].ParameterType == typeof(int))
                    && (parameters[2].ParameterType == typeof(uint) || parameters[2].ParameterType == typeof(int)))
                {
                    return method;
                }
            }

            return null;
        }

        [DllImport("d3dcompiler_47.dll", CharSet = CharSet.Ansi)]
        private static extern int D3DCompile(
            byte[] srcData,
            nuint srcDataSize,
            string sourceName,
            IntPtr defines,
            IntPtr include,
            string entryPoint,
            string target,
            uint flags1,
            uint flags2,
            out IntPtr code,
            out IntPtr errorMsgs);

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("8BA5FB08-5195-40E2-AC58-0D989C3A0102")]
        private interface ID3DBlob
        {
            [PreserveSig]
            IntPtr GetBufferPointer();

            [PreserveSig]
            nuint GetBufferSize();
        }

        private const string _mipmapComputeShaderCode = @"
Texture2D<float4> SourceTexture : register(t0);
RWTexture2D<float4> DestinationTexture : register(u0);
SamplerState LinearSampler : register(s0);

[numthreads(8, 8, 1)]
void cs_main(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    uint width;
    uint height;
    DestinationTexture.GetDimensions(width, height);
    if (dispatchThreadID.x >= width || dispatchThreadID.y >= height)
    {
        return;
    }

    float2 uv = (float2(dispatchThreadID.xy) + 0.5f) / float2(width, height);
    float4 value = SourceTexture.SampleLevel(LinearSampler, uv, 0.0f);
    DestinationTexture[dispatchThreadID.xy] = value;
}";


        private void bindGraphicsResource(D3D12Pipeline.RootBindingInfo bindingInfo, IBindableResource resource, uint dynamicOffset)
        {
            if (bindingInfo.DescriptorTable)
            {
                bindDescriptorTableResource(bindingInfo, resource, compute: false);
                return;
            }

            if (!Util.GetDeviceBuffer(resource, out DeviceBuffer _))
            {
                throw new PlatformNotSupportedException("D3D12 ResourceSet currently supports buffer resources only for non-table root bindings.");
            }

            DeviceBufferRange range = Util.GetBufferRange(resource, dynamicOffset);
            var d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(range.Buffer);
            transitionBuffer(d3d12Buffer, getGraphicsBufferState(bindingInfo.Kind));
            ulong gpuAddress = d3d12Buffer.GetGpuVirtualAddress(range.Offset);
            if (isSameGraphicsRootBuffer(bindingInfo.RootParameterIndex, gpuAddress))
            {
                return;
            }

            switch (bindingInfo.Kind)
            {
                case ResourceKind.UniformBuffer:
                    _nativeCommandList.SetGraphicsRootConstantBufferView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.StructuredBufferReadOnly:
                    _nativeCommandList.SetGraphicsRootShaderResourceView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.StructuredBufferReadWrite:
                    _nativeCommandList.SetGraphicsRootUnorderedAccessView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.TextureReadOnly:
                case ResourceKind.TextureReadWrite:
                case ResourceKind.Sampler:
                    throw new VeldridException("Texture and sampler root bindings must use descriptor tables.");
                default:
                    throw Illegal.Value<ResourceKind>();
            }

            setGraphicsRootBufferCache(bindingInfo.RootParameterIndex, gpuAddress);
        }

        private void bindComputeResource(D3D12Pipeline.RootBindingInfo bindingInfo, IBindableResource resource, uint dynamicOffset)
        {
            if (bindingInfo.DescriptorTable)
            {
                bindDescriptorTableResource(bindingInfo, resource, compute: true);
                return;
            }

            if (!Util.GetDeviceBuffer(resource, out DeviceBuffer _))
            {
                throw new PlatformNotSupportedException("D3D12 ResourceSet currently supports buffer resources only for non-table root bindings.");
            }

            DeviceBufferRange range = Util.GetBufferRange(resource, dynamicOffset);
            var d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(range.Buffer);
            transitionBuffer(d3d12Buffer, getComputeBufferState(bindingInfo.Kind));
            ulong gpuAddress = d3d12Buffer.GetGpuVirtualAddress(range.Offset);
            if (isSameComputeRootBuffer(bindingInfo.RootParameterIndex, gpuAddress))
            {
                return;
            }

            switch (bindingInfo.Kind)
            {
                case ResourceKind.UniformBuffer:
                    _nativeCommandList.SetComputeRootConstantBufferView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.StructuredBufferReadOnly:
                    _nativeCommandList.SetComputeRootShaderResourceView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.StructuredBufferReadWrite:
                    _nativeCommandList.SetComputeRootUnorderedAccessView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.TextureReadOnly:
                case ResourceKind.TextureReadWrite:
                case ResourceKind.Sampler:
                    throw new VeldridException("Texture and sampler root bindings must use descriptor tables.");
                default:
                    throw Illegal.Value<ResourceKind>();
            }

            setComputeRootBufferCache(bindingInfo.RootParameterIndex, gpuAddress);
        }

        private void bindDescriptorTableResource(D3D12Pipeline.RootBindingInfo bindingInfo, IBindableResource resource, bool compute)
        {
            bindDescriptorHeaps();

            switch (bindingInfo.Kind)
            {
                case ResourceKind.TextureReadOnly:
                {
                    TextureView textureView = Util.GetTextureView(gd, resource);
                    var d3d12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                    validateTextureViewBindingSupport(d3d12TextureView, TextureUsage.Sampled, "sampled");
                    ResourceStates readState = compute
                        ? ResourceStates.NonPixelShaderResource
                        : ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource;
                    transitionTextureView(d3d12TextureView, readState);
                    ID3D12Resource nativeTexture = d3d12TextureView.TargetTexture.NativeTexture
                        ?? throw new PlatformNotSupportedException("Texture has no native D3D12 resource.");
                    GpuDescriptorHandle gpuHandle = getOrCreateDescriptorTableHandle(resource, bindingInfo.Kind, () =>
                    {
                        allocateSrvUavDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle allocatedGpuHandle);
                        CpuDescriptorHandle sourceSrv = d3d12TextureView.GetOrCreateShaderResourceViewDescriptor();
                        gd.Device.CopyDescriptorsSimple(1u, cpuHandle, sourceSrv, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
                        return allocatedGpuHandle;
                    });
                    if ((compute && isSameComputeRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr))
                        || (!compute && isSameGraphicsRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr)))
                    {
                        break;
                    }
                    if (compute)
                    {
                        _nativeCommandList.SetComputeRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        setComputeRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }
                    else
                    {
                        _nativeCommandList.SetGraphicsRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        setGraphicsRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }
                    if (_perfLogEnabled)
                    {
                        _perfRootTableSets++;
                    }
                    break;
                }
                case ResourceKind.TextureReadWrite:
                {
                    TextureView textureView = Util.GetTextureView(gd, resource);
                    var d3d12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                    validateTextureViewBindingSupport(d3d12TextureView, TextureUsage.Storage, "storage");
                    transitionTextureView(d3d12TextureView, ResourceStates.UnorderedAccess);
                    ID3D12Resource nativeTexture = d3d12TextureView.TargetTexture.NativeTexture
                        ?? throw new PlatformNotSupportedException("Texture has no native D3D12 resource.");
                    GpuDescriptorHandle gpuHandle = getOrCreateDescriptorTableHandle(resource, bindingInfo.Kind, () =>
                    {
                        allocateSrvUavDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle allocatedGpuHandle);
                        CpuDescriptorHandle sourceUav = d3d12TextureView.GetOrCreateUnorderedAccessViewDescriptor();
                        gd.Device.CopyDescriptorsSimple(1u, cpuHandle, sourceUav, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
                        return allocatedGpuHandle;
                    });
                    if ((compute && isSameComputeRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr))
                        || (!compute && isSameGraphicsRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr)))
                    {
                        break;
                    }
                    if (compute)
                    {
                        _nativeCommandList.SetComputeRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        setComputeRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }
                    else
                    {
                        _nativeCommandList.SetGraphicsRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        setGraphicsRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }
                    if (_perfLogEnabled)
                    {
                        _perfRootTableSets++;
                    }
                    break;
                }
                case ResourceKind.Sampler:
                {
                    var d3d12Sampler = Util.AssertSubtype<IBindableResource, D3D12Sampler>(resource);
                    GpuDescriptorHandle gpuHandle = getOrCreateDescriptorTableHandle(resource, bindingInfo.Kind, () =>
                    {
                        allocateSamplerDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle allocatedGpuHandle);
                        CpuDescriptorHandle sourceSampler = d3d12Sampler.GetOrCreateDescriptor();
                        gd.Device.CopyDescriptorsSimple(1u, cpuHandle, sourceSampler, DescriptorHeapType.Sampler);
                        return allocatedGpuHandle;
                    });
                    if ((compute && isSameComputeRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr))
                        || (!compute && isSameGraphicsRootTable(bindingInfo.RootParameterIndex, gpuHandle.Ptr)))
                    {
                        break;
                    }
                    if (compute)
                    {
                        _nativeCommandList.SetComputeRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        setComputeRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }
                    else
                    {
                        _nativeCommandList.SetGraphicsRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                        setGraphicsRootTableCache(bindingInfo.RootParameterIndex, gpuHandle.Ptr);
                    }
                    if (_perfLogEnabled)
                    {
                        _perfRootTableSets++;
                    }
                    break;
                }
                default:
                    throw new VeldridException("Invalid descriptor-table binding kind.");
            }
        }

        private void validateTextureViewBindingSupport(D3D12TextureView textureView, TextureUsage requestedUsage, string bindingKind)
        {
            D3D12Texture texture = textureView.TargetTexture;
            TextureUsage usage = requestedUsage;

            if ((requestedUsage & TextureUsage.Sampled) != 0)
            {
                if ((texture.Usage & TextureUsage.Cubemap) != 0)
                {
                    usage |= TextureUsage.Cubemap;
                }

                if ((texture.Usage & TextureUsage.DepthStencil) != 0)
                {
                    usage |= TextureUsage.DepthStencil;
                }
            }

            if (!gd.GetPixelFormatSupport(textureView.Format, texture.Type, usage))
            {
                throw new PlatformNotSupportedException(
                    $"D3D12 {bindingKind} texture view binding is not supported for format {textureView.Format}, type {texture.Type}, usage {usage}.");
            }
        }

        private void bindDescriptorHeaps()
        {
            if (_descriptorHeapsBound)
            {
                return;
            }

            _boundDescriptorHeaps[0] = _shaderVisibleSrvUavHeaps[_currentFrameSlot];
            _boundDescriptorHeaps[1] = _shaderVisibleSamplerHeaps[_currentFrameSlot];
            _nativeCommandList.SetDescriptorHeaps(_boundDescriptorHeaps);
            _descriptorHeapsBound = true;
        }

        private void allocateSrvUavDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle)
        {
            if (_nextSrvUavDescriptor >= _maxSrvUavDescriptors)
            {
                throw new VeldridException("D3D12 SRV/UAV descriptor heap exhausted for this CommandList recording.");
            }

            CpuDescriptorHandle cpuStart = _shaderVisibleSrvUavHeaps[_currentFrameSlot].GetCPUDescriptorHandleForHeapStart();
            GpuDescriptorHandle gpuStart = _shaderVisibleSrvUavHeaps[_currentFrameSlot].GetGPUDescriptorHandleForHeapStart();
            cpuHandle = new CpuDescriptorHandle(cpuStart, (int)_nextSrvUavDescriptor, (uint)_srvUavDescriptorSize);
            gpuHandle = new GpuDescriptorHandle(gpuStart, (int)_nextSrvUavDescriptor, (uint)_srvUavDescriptorSize);
            _nextSrvUavDescriptor++;
            _nextSrvUavDescriptorsPerFrameSlot[_currentFrameSlot] = _nextSrvUavDescriptor;
        }

        private void allocateSamplerDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle)
        {
            if (_nextSamplerDescriptor >= _maxSamplerDescriptors)
            {
                throw new VeldridException("D3D12 sampler descriptor heap exhausted for this CommandList recording.");
            }

            CpuDescriptorHandle cpuStart = _shaderVisibleSamplerHeaps[_currentFrameSlot].GetCPUDescriptorHandleForHeapStart();
            GpuDescriptorHandle gpuStart = _shaderVisibleSamplerHeaps[_currentFrameSlot].GetGPUDescriptorHandleForHeapStart();
            cpuHandle = new CpuDescriptorHandle(cpuStart, (int)_nextSamplerDescriptor, (uint)_samplerDescriptorSize);
            gpuHandle = new GpuDescriptorHandle(gpuStart, (int)_nextSamplerDescriptor, (uint)_samplerDescriptorSize);
            _nextSamplerDescriptor++;
            _nextSamplerDescriptorsPerFrameSlot[_currentFrameSlot] = _nextSamplerDescriptor;
        }

        private GpuDescriptorHandle getOrCreateDescriptorTableHandle(IBindableResource resource, ResourceKind kind, Func<GpuDescriptorHandle> createHandle)
        {
            Dictionary<DescriptorCacheKey, GpuDescriptorHandle> descriptorTableCache = _descriptorTableCaches[_currentFrameSlot];
            var key = new DescriptorCacheKey(resource, kind);
            if (descriptorTableCache.TryGetValue(key, out GpuDescriptorHandle cachedHandle))
            {
                return cachedHandle;
            }

            GpuDescriptorHandle newHandle = createHandle();
            if (_perfLogEnabled)
            {
                _perfDescriptorCopies++;
            }
            descriptorTableCache.Add(key, newHandle);
            return newHandle;
        }

        private ResourceSetBindingPlanEntry[] getGraphicsResourceSetBindingPlan(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout)
        {
            var key = new ResourceSetBindingPlanKey(pipeline, layout, slot);
            if (_graphicsResourceSetBindingPlans.TryGetValue(key, out ResourceSetBindingPlanEntry[] existingPlan))
            {
                return existingPlan;
            }

            ResourceSetBindingPlanEntry[] createdPlan = createGraphicsResourceSetBindingPlan(pipeline, slot, layout.Elements);
            _graphicsResourceSetBindingPlans.Add(key, createdPlan);
            return createdPlan;
        }

        private ResourceSetBindingPlanEntry[] getComputeResourceSetBindingPlan(D3D12Pipeline pipeline, uint slot, D3D12ResourceLayout layout)
        {
            var key = new ResourceSetBindingPlanKey(pipeline, layout, slot);
            if (_computeResourceSetBindingPlans.TryGetValue(key, out ResourceSetBindingPlanEntry[] existingPlan))
            {
                return existingPlan;
            }

            ResourceSetBindingPlanEntry[] createdPlan = createComputeResourceSetBindingPlan(pipeline, slot, layout.Elements);
            _computeResourceSetBindingPlans.Add(key, createdPlan);
            return createdPlan;
        }

        private static ResourceSetBindingPlanEntry[] createGraphicsResourceSetBindingPlan(
            D3D12Pipeline pipeline,
            uint slot,
            ResourceLayoutElementDescription[] elements)
        {
            var plan = new List<ResourceSetBindingPlanEntry>(elements.Length);
            for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++)
            {
                if (!pipeline.TryGetGraphicsRootBinding(slot, elementIndex, out D3D12Pipeline.RootBindingInfo bindingInfo))
                {
                    continue;
                }

                bool isDynamicBinding = (elements[elementIndex].Options & ResourceLayoutElementOptions.DynamicBinding) != 0;
                plan.Add(new ResourceSetBindingPlanEntry(elementIndex, bindingInfo, isDynamicBinding));
            }

            return plan.ToArray();
        }

        private static ResourceSetBindingPlanEntry[] createComputeResourceSetBindingPlan(
            D3D12Pipeline pipeline,
            uint slot,
            ResourceLayoutElementDescription[] elements)
        {
            var plan = new List<ResourceSetBindingPlanEntry>(elements.Length);
            for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++)
            {
                if (!pipeline.TryGetComputeRootBinding(slot, elementIndex, out D3D12Pipeline.RootBindingInfo bindingInfo))
                {
                    continue;
                }

                bool isDynamicBinding = (elements[elementIndex].Options & ResourceLayoutElementOptions.DynamicBinding) != 0;
                plan.Add(new ResourceSetBindingPlanEntry(elementIndex, bindingInfo, isDynamicBinding));
            }

            return plan.ToArray();
        }

        private readonly struct DescriptorCacheKey
        {
            public DescriptorCacheKey(IBindableResource resource, ResourceKind kind)
            {
                Resource = resource;
                Kind = kind;
            }

            public IBindableResource Resource { get; }
            public ResourceKind Kind { get; }
        }

        private readonly struct ResourceSetBindingPlanKey
        {
            public ResourceSetBindingPlanKey(D3D12Pipeline pipeline, D3D12ResourceLayout layout, uint slot)
            {
                Pipeline = pipeline;
                Layout = layout;
                Slot = slot;
            }

            public D3D12Pipeline Pipeline { get; }
            public D3D12ResourceLayout Layout { get; }
            public uint Slot { get; }
        }

        private readonly struct ResourceSetBindingPlanEntry
        {
            public ResourceSetBindingPlanEntry(uint elementIndex, D3D12Pipeline.RootBindingInfo bindingInfo, bool isDynamicBinding)
            {
                ElementIndex = elementIndex;
                BindingInfo = bindingInfo;
                IsDynamicBinding = isDynamicBinding;
            }

            public uint ElementIndex { get; }
            public D3D12Pipeline.RootBindingInfo BindingInfo { get; }
            public bool IsDynamicBinding { get; }
        }

        private sealed class DescriptorCacheKeyComparer : IEqualityComparer<DescriptorCacheKey>
        {
            public static readonly DescriptorCacheKeyComparer Instance = new DescriptorCacheKeyComparer();

            public bool Equals(DescriptorCacheKey x, DescriptorCacheKey y)
                => x.Kind == y.Kind && ReferenceEquals(x.Resource, y.Resource);

            public int GetHashCode(DescriptorCacheKey obj)
                => HashCode.Combine((int)obj.Kind, RuntimeHelpers.GetHashCode(obj.Resource));
        }

        private sealed class ResourceSetBindingPlanKeyComparer : IEqualityComparer<ResourceSetBindingPlanKey>
        {
            public static readonly ResourceSetBindingPlanKeyComparer Instance = new ResourceSetBindingPlanKeyComparer();

            public bool Equals(ResourceSetBindingPlanKey x, ResourceSetBindingPlanKey y)
                => x.Slot == y.Slot
                   && ReferenceEquals(x.Pipeline, y.Pipeline)
                   && ReferenceEquals(x.Layout, y.Layout);

            public int GetHashCode(ResourceSetBindingPlanKey obj)
                => HashCode.Combine(
                    (int)obj.Slot,
                    RuntimeHelpers.GetHashCode(obj.Pipeline),
                    RuntimeHelpers.GetHashCode(obj.Layout));
        }

        private static ResourceStates getGraphicsBufferState(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.UniformBuffer:
                    return ResourceStates.VertexAndConstantBuffer;
                case ResourceKind.StructuredBufferReadOnly:
                    return ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource;
                case ResourceKind.StructuredBufferReadWrite:
                    return ResourceStates.UnorderedAccess;
                default:
                    return ResourceStates.Common;
            }
        }

        private static ResourceStates getComputeBufferState(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.UniformBuffer:
                    return ResourceStates.VertexAndConstantBuffer;
                case ResourceKind.StructuredBufferReadOnly:
                    return ResourceStates.NonPixelShaderResource;
                case ResourceKind.StructuredBufferReadWrite:
                    return ResourceStates.UnorderedAccess;
                default:
                    return ResourceStates.Common;
            }
        }

        private bool isSameGraphicsRootBuffer(uint rootParameterIndex, ulong gpuAddress)
        {
            int index = (int)rootParameterIndex;
            Util.EnsureArrayMinimumSize(ref _graphicsRootBufferAddresses, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref _graphicsRootBufferAddressValid, rootParameterIndex + 1);
            return _graphicsRootBufferAddressValid[index] && _graphicsRootBufferAddresses[index] == gpuAddress;
        }

        private void setGraphicsRootBufferCache(uint rootParameterIndex, ulong gpuAddress)
        {
            int index = (int)rootParameterIndex;
            Util.EnsureArrayMinimumSize(ref _graphicsRootBufferAddresses, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref _graphicsRootBufferAddressValid, rootParameterIndex + 1);
            _graphicsRootBufferAddresses[index] = gpuAddress;
            _graphicsRootBufferAddressValid[index] = true;
        }

        private bool isSameComputeRootBuffer(uint rootParameterIndex, ulong gpuAddress)
        {
            int index = (int)rootParameterIndex;
            Util.EnsureArrayMinimumSize(ref _computeRootBufferAddresses, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref _computeRootBufferAddressValid, rootParameterIndex + 1);
            return _computeRootBufferAddressValid[index] && _computeRootBufferAddresses[index] == gpuAddress;
        }

        private void setComputeRootBufferCache(uint rootParameterIndex, ulong gpuAddress)
        {
            int index = (int)rootParameterIndex;
            Util.EnsureArrayMinimumSize(ref _computeRootBufferAddresses, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref _computeRootBufferAddressValid, rootParameterIndex + 1);
            _computeRootBufferAddresses[index] = gpuAddress;
            _computeRootBufferAddressValid[index] = true;
        }

        private bool isSameGraphicsRootTable(uint rootParameterIndex, ulong tablePtr)
        {
            int index = (int)rootParameterIndex;
            Util.EnsureArrayMinimumSize(ref _graphicsRootTablePointers, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref _graphicsRootTablePointerValid, rootParameterIndex + 1);
            return _graphicsRootTablePointerValid[index] && _graphicsRootTablePointers[index] == tablePtr;
        }

        private void setGraphicsRootTableCache(uint rootParameterIndex, ulong tablePtr)
        {
            int index = (int)rootParameterIndex;
            Util.EnsureArrayMinimumSize(ref _graphicsRootTablePointers, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref _graphicsRootTablePointerValid, rootParameterIndex + 1);
            _graphicsRootTablePointers[index] = tablePtr;
            _graphicsRootTablePointerValid[index] = true;
        }

        private bool isSameComputeRootTable(uint rootParameterIndex, ulong tablePtr)
        {
            int index = (int)rootParameterIndex;
            Util.EnsureArrayMinimumSize(ref _computeRootTablePointers, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref _computeRootTablePointerValid, rootParameterIndex + 1);
            return _computeRootTablePointerValid[index] && _computeRootTablePointers[index] == tablePtr;
        }

        private void setComputeRootTableCache(uint rootParameterIndex, ulong tablePtr)
        {
            int index = (int)rootParameterIndex;
            Util.EnsureArrayMinimumSize(ref _computeRootTablePointers, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref _computeRootTablePointerValid, rootParameterIndex + 1);
            _computeRootTablePointers[index] = tablePtr;
            _computeRootTablePointerValid[index] = true;
        }

        private void invalidateGraphicsRootCaches()
        {
            Array.Clear(_graphicsRootBufferAddressValid, 0, _graphicsRootBufferAddressValid.Length);
            Array.Clear(_graphicsRootTablePointerValid, 0, _graphicsRootTablePointerValid.Length);
        }

        private void invalidateComputeRootCaches()
        {
            Array.Clear(_computeRootBufferAddressValid, 0, _computeRootBufferAddressValid.Length);
            Array.Clear(_computeRootTablePointerValid, 0, _computeRootTablePointerValid.Length);
        }

        private static void clearBoundResourceSets(BoundResourceSetInfo[] infos)
        {
            if (infos == null)
            {
                return;
            }

            for (int i = 0; i < infos.Length; i++)
            {
                infos[i].Offsets.Dispose();
            }

            Util.ClearArray(infos);
        }

        private void transitionSwapchainBackBuffersToPresent()
        {
            if (Framebuffer is not D3D12SwapchainFramebuffer swapchainFramebuffer)
            {
                return;
            }

            if (_transitionedBackBufferIndex < 0)
            {
                return;
            }

            if (swapchainFramebuffer.Swapchain.TryGetCurrentBackBuffer(out ID3D12Resource backBuffer, out _, out int currentIndex, out ResourceStates state)
                && currentIndex == _transitionedBackBufferIndex)
            {
                transition(backBuffer, state, ResourceStates.Present);
                swapchainFramebuffer.Swapchain.SetBackBufferState(currentIndex, ResourceStates.Present);
            }
        }
    }
}
