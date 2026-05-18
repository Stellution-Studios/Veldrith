using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Reflection;
using System.Text;
using Vortice;
using Vortice.Mathematics;
using Vortice.Direct3D12;
using Vortice.DXGI;
using VorticeD3D12 = Vortice.Direct3D12.D3D12;

namespace Veldrid.D3D12
{
    internal sealed class D3D12CommandList : CommandList
    {
        private readonly D3D12GraphicsDevice gd;
        private readonly ID3D12CommandAllocator commandAllocator;
        private readonly ID3D12GraphicsCommandList nativeCommandList;
        private readonly ID3D12DescriptorHeap shaderVisibleSrvUavHeap;
        private readonly ID3D12DescriptorHeap shaderVisibleSamplerHeap;
        private readonly int srvUavDescriptorSize;
        private readonly int samplerDescriptorSize;
        private readonly uint maxSrvUavDescriptors = 4096;
        private readonly uint maxSamplerDescriptors = 1024;
        private bool begun;
        private bool ended;
        private bool disposed;
        private string name;
        private int transitionedBackBufferIndex = -1;
        private D3D12Pipeline currentGraphicsPipeline;
        private D3D12Pipeline currentComputePipeline;
        private uint nextSrvUavDescriptor;
        private uint nextSamplerDescriptor;
        private bool descriptorHeapsBound;
        private bool gpuMipResourcesInitialized;
        private bool gpuMipResourcesAvailable;
        private D3D12Pipeline gpuMipPipeline;
        private ResourceLayout gpuMipResourceLayout;
        private Sampler gpuMipSampler;
        private bool indirectSignaturesInitialized;
        private bool indirectSignaturesAvailable;
        private ID3D12CommandSignature drawIndirectSignature;
        private ID3D12CommandSignature drawIndexedIndirectSignature;
        private ID3D12CommandSignature dispatchIndirectSignature;
        private readonly Vortice.Mathematics.Viewport[] activeViewports = new Vortice.Mathematics.Viewport[16];
        private readonly RawRect[] activeScissorRects = new RawRect[16];
        private uint activeViewportCount;
        private uint activeScissorRectCount;
        private readonly MethodInfo beginEventMethod;
        private readonly MethodInfo setMarkerMethod;
        private readonly MethodInfo endEventMethod;
        private bool uavBarrierPending;

        public ID3D12GraphicsCommandList NativeCommandList => nativeCommandList;

        public D3D12CommandList(
            D3D12GraphicsDevice gd,
            ref CommandListDescription description,
            GraphicsDeviceFeatures features,
            uint uniformAlignment,
            uint structuredAlignment)
            : base(ref description, features, uniformAlignment, structuredAlignment)
        {
            this.gd = gd;
            commandAllocator = gd.Device.CreateCommandAllocator(CommandListType.Direct);
            nativeCommandList = gd.Device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, commandAllocator, null);
            shaderVisibleSrvUavHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(
                DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
                maxSrvUavDescriptors,
                DescriptorHeapFlags.ShaderVisible));
            shaderVisibleSamplerHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(
                DescriptorHeapType.Sampler,
                maxSamplerDescriptors,
                DescriptorHeapFlags.ShaderVisible));
            srvUavDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            samplerDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler);
            beginEventMethod = getDebugMarkerMethod("BeginEvent");
            setMarkerMethod = getDebugMarkerMethod("SetMarker");
            endEventMethod = nativeCommandList.GetType().GetMethod("EndEvent", Type.EmptyTypes);
            nativeCommandList.Close();
        }

        public override bool IsDisposed => disposed;

        public override string Name
        {
            get => name;
            set => name = value;
        }

        public override void Dispose()
        {
            gpuMipPipeline?.Dispose();
            gpuMipResourceLayout?.Dispose();
            gpuMipSampler?.Dispose();
            drawIndirectSignature?.Dispose();
            drawIndexedIndirectSignature?.Dispose();
            dispatchIndirectSignature?.Dispose();
            nativeCommandList.Dispose();
            commandAllocator.Dispose();
            shaderVisibleSrvUavHeap.Dispose();
            shaderVisibleSamplerHeap.Dispose();
            disposed = true;
        }

        public override void Begin()
        {
            commandAllocator.Reset();
            nativeCommandList.Reset(commandAllocator);
            begun = true;
            ended = false;
            transitionedBackBufferIndex = -1;
            nextSrvUavDescriptor = 0;
            nextSamplerDescriptor = 0;
            descriptorHeapsBound = false;
            activeViewportCount = 0;
            activeScissorRectCount = 0;
            uavBarrierPending = false;
            ClearCachedState();
            currentGraphicsPipeline = null;
            currentComputePipeline = null;
        }

        public override void End()
        {
            if (!begun)
            {
                throw new VeldridException("CommandList.End cannot be called before Begin.");
            }

            flushPendingUavBarrier();
            transitionSwapchainBackBuffersToPresent();
            nativeCommandList.Close();
            ended = true;
        }

        public override void SetViewport(uint index, ref Viewport viewport)
        {
            if (index >= activeViewports.Length)
            {
                return;
            }

            activeViewports[index] = new Vortice.Mathematics.Viewport(
                viewport.X,
                viewport.Y,
                viewport.Width,
                viewport.Height,
                viewport.MinDepth,
                viewport.MaxDepth);

            if (index + 1 > activeViewportCount)
            {
                activeViewportCount = index + 1;
            }

            nativeCommandList.RSSetViewports(activeViewportCount, activeViewports);
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            if (index >= activeScissorRects.Length)
            {
                return;
            }

            activeScissorRects[index] = new RawRect((int)x, (int)y, (int)(x + width), (int)(y + height));

            if (index + 1 > activeScissorRectCount)
            {
                activeScissorRectCount = index + 1;
            }

            nativeCommandList.RSSetScissorRects(activeScissorRectCount, activeScissorRects);
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            flushPendingUavBarrier();
            nativeCommandList.Dispatch(groupCountX, groupCountY, groupCountZ);
            uavBarrierPending = true;
        }

        internal void Execute()
        {
            if (!ended)
            {
                throw new VeldridException("CommandList must be ended before submit.");
            }

            gd.CommandQueue.ExecuteCommandList(nativeCommandList);
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (currentGraphicsPipeline == null)
            {
                return;
            }

            var d3d12Set = Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(rs);
            ResourceLayoutElementDescription[] elements = d3d12Set.ResourceLayoutInfo.Elements;
            IBindableResource[] resources = d3d12Set.BoundResources;
            uint dynamicOffsetIndex = 0;

            for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++)
            {
                if (!currentGraphicsPipeline.TryGetGraphicsRootBinding(slot, elementIndex, out D3D12Pipeline.RootBindingInfo bindingInfo))
                {
                    continue;
                }

                ResourceLayoutElementDescription element = elements[elementIndex];
                IBindableResource resource = resources[elementIndex];

                uint dynamicOffset = 0;
                if ((element.Options & ResourceLayoutElementOptions.DynamicBinding) != 0)
                {
                    if (dynamicOffsetIndex >= dynamicOffsetsCount)
                    {
                        throw new VeldridException("Insufficient dynamic offsets provided for ResourceSet binding.");
                    }

                    dynamicOffset = Unsafe.Add(ref dynamicOffsets, (int)dynamicOffsetIndex);
                    dynamicOffsetIndex++;
                }

                bindGraphicsResource(bindingInfo, resource, dynamicOffset);
            }
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, uint dynamicOffsetsCount, ref uint dynamicOffsets)
        {
            if (currentComputePipeline == null)
            {
                return;
            }

            var d3d12Set = Util.AssertSubtype<ResourceSet, D3D12ResourceSet>(set);
            ResourceLayoutElementDescription[] elements = d3d12Set.ResourceLayoutInfo.Elements;
            IBindableResource[] resources = d3d12Set.BoundResources;
            uint dynamicOffsetIndex = 0;

            for (uint elementIndex = 0; elementIndex < elements.Length; elementIndex++)
            {
                if (!currentComputePipeline.TryGetComputeRootBinding(slot, elementIndex, out D3D12Pipeline.RootBindingInfo bindingInfo))
                {
                    continue;
                }

                ResourceLayoutElementDescription element = elements[elementIndex];
                IBindableResource resource = resources[elementIndex];

                uint dynamicOffset = 0;
                if ((element.Options & ResourceLayoutElementOptions.DynamicBinding) != 0)
                {
                    if (dynamicOffsetIndex >= dynamicOffsetsCount)
                    {
                        throw new VeldridException("Insufficient dynamic offsets provided for ResourceSet binding.");
                    }

                    dynamicOffset = Unsafe.Add(ref dynamicOffsets, (int)dynamicOffsetIndex);
                    dynamicOffsetIndex++;
                }

                bindComputeResource(bindingInfo, resource, dynamicOffset);
            }
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            if (fb is D3D12SwapchainFramebuffer swapchainFramebuffer
                && swapchainFramebuffer.Swapchain.TryGetCurrentBackBuffer(out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int backBufferIndex, out ResourceStates currentState))
            {
                transition(backBuffer, currentState, ResourceStates.RenderTarget);
                swapchainFramebuffer.Swapchain.SetBackBufferState(backBufferIndex, ResourceStates.RenderTarget);
                transitionedBackBufferIndex = backBufferIndex;
                if (swapchainFramebuffer.DepthTargetTexture != null)
                {
                    transitionTexture(swapchainFramebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
                }

                if (swapchainFramebuffer.TryGetDepthStencilView(out CpuDescriptorHandle swapchainDsv))
                {
                    nativeCommandList.OMSetRenderTargets(rtv, swapchainDsv);
                }
                else
                {
                    nativeCommandList.OMSetRenderTargets(rtv, null);
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
                    nativeCommandList.OMSetRenderTargets(Array.Empty<CpuDescriptorHandle>(), depthOnlyDsv);
                }
                return;
            }

            if (d3d12Framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv))
            {
                nativeCommandList.OMSetRenderTargets(rtvs, dsv);
            }
            else
            {
                nativeCommandList.OMSetRenderTargets(rtvs, null);
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
                executeIndirect(d3d12Buffer, offset, drawCount, stride, argumentSize, drawIndirectSignature);
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
                    nativeCommandList.DrawInstanced(arguments.VertexCount, arguments.InstanceCount, arguments.FirstVertex, arguments.FirstInstance);
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
                executeIndirect(d3d12Buffer, offset, drawCount, stride, argumentSize, drawIndexedIndirectSignature);
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
                    nativeCommandList.DrawIndexedInstanced(arguments.IndexCount, arguments.InstanceCount, arguments.FirstIndex, arguments.VertexOffset, arguments.FirstInstance);
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
                executeIndirect(d3d12Buffer, offset, 1, argumentSize, argumentSize, dispatchIndirectSignature);
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
                nativeCommandList.Dispatch(arguments.GroupCountX, arguments.GroupCountY, arguments.GroupCountZ);
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
                    nativeCommandList.ResolveSubresource(dst.NativeTexture, dstSubresource, src.NativeTexture, srcSubresource, resolveFormat);
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
            src.CopyTo(nativeCommandList, dst, sourceOffset, destinationOffset, sizeInBytes);
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
                    nativeCommandList.CopyTextureRegion(dstLocation, dstX, dstY, dstZ, srcLocation, srcBox);
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
                currentComputePipeline = d3d12ComputePipeline;
                currentGraphicsPipeline = null;
                nativeCommandList.SetPipelineState(d3d12ComputePipeline.PipelineState);
                nativeCommandList.SetComputeRootSignature(d3d12ComputePipeline.RootSignature);
                return;
            }

            var d3d12Pipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(pipeline);
            currentGraphicsPipeline = d3d12Pipeline;
            currentComputePipeline = null;
            nativeCommandList.SetPipelineState(d3d12Pipeline.PipelineState);
            nativeCommandList.SetGraphicsRootSignature(d3d12Pipeline.RootSignature);
            nativeCommandList.IASetPrimitiveTopology(d3d12Pipeline.PrimitiveTopology);
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            var d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
            transitionBuffer(d3d12Buffer, ResourceStates.VertexAndConstantBuffer);
            uint stride = 0;
            if (currentGraphicsPipeline != null && index < currentGraphicsPipeline.VertexStrides.Length)
            {
                stride = currentGraphicsPipeline.VertexStrides[index];
            }

            var view = new VertexBufferView(
                d3d12Buffer.GpuVirtualAddress + offset,
                d3d12Buffer.SizeInBytes - offset,
                stride);
            nativeCommandList.IASetVertexBuffers(index, view);
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            var d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
            transitionBuffer(d3d12Buffer, ResourceStates.IndexBuffer);
            var indexView = new IndexBufferView(
                d3d12Buffer.GpuVirtualAddress + offset,
                d3d12Buffer.SizeInBytes - offset,
                D3D12Formats.ToDxgiFormat(format));
            nativeCommandList.IASetIndexBuffer(indexView);
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            flushPendingUavBarrier();
            if (Framebuffer is D3D12SwapchainFramebuffer swapchainFramebuffer
                && swapchainFramebuffer.Swapchain.TryGetCurrentBackBuffer(out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int backBufferIndex, out ResourceStates currentState))
            {
                transition(backBuffer, currentState, ResourceStates.RenderTarget);
                swapchainFramebuffer.Swapchain.SetBackBufferState(backBufferIndex, ResourceStates.RenderTarget);
                transitionedBackBufferIndex = backBufferIndex;
                if (swapchainFramebuffer.TryGetDepthStencilView(out CpuDescriptorHandle swapchainDsv))
                {
                    nativeCommandList.OMSetRenderTargets(rtv, swapchainDsv);
                }
                else
                {
                    nativeCommandList.OMSetRenderTargets(rtv, null);
                }
                nativeCommandList.ClearRenderTargetView(rtv, new Color4(clearColor.R, clearColor.G, clearColor.B, clearColor.A), 0, null);
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

                nativeCommandList.ClearRenderTargetView(offscreenRtv, new Color4(clearColor.R, clearColor.G, clearColor.B, clearColor.A), 0, null);
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
                nativeCommandList.ClearDepthStencilView(dsv, ClearFlags.Depth | ClearFlags.Stencil, depth, stencil, 0, null);
            }
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            flushPendingUavBarrier();
            nativeCommandList.DrawInstanced(vertexCount, instanceCount, vertexStart, instanceStart);
            uavBarrierPending = true;
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            flushPendingUavBarrier();
            nativeCommandList.DrawIndexedInstanced(indexCount, instanceCount, indexStart, vertexOffset, instanceStart);
            uavBarrierPending = true;
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            flushPendingUavBarrier();
            var d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
            ID3D12Resource temporaryUpload = d3d12Buffer.Update(nativeCommandList, source, bufferOffsetInBytes, sizeInBytes);
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
            endEventMethod?.Invoke(nativeCommandList, null);
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
            nativeCommandList.ResourceBarrier(new[] { barrier });
        }

        private void transitionSubresource(ID3D12Resource resource, ResourceStates from, ResourceStates to, uint subresource)
        {
            if (from == to)
            {
                return;
            }

            ResourceBarrier barrier = ResourceBarrier.BarrierTransition(resource, from, to, subresource, ResourceBarrierFlags.None);
            nativeCommandList.ResourceBarrier(new[] { barrier });
        }

        private void flushPendingUavBarrier()
        {
            if (!uavBarrierPending)
            {
                return;
            }

            ResourceBarrier barrier = ResourceBarrier.BarrierUnorderedAccessView(null);
            nativeCommandList.ResourceBarrier(new[] { barrier });
            uavBarrierPending = false;
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
                nativeCommandList.ExecuteIndirect(signature, drawCount, argumentBuffer.NativeBuffer, offset, null, 0);
                transitionBuffer(argumentBuffer, previousState);
                return;
            }

            for (uint i = 0; i < drawCount; i++)
            {
                ulong commandOffset = offset + ((ulong)i * stride);
                nativeCommandList.ExecuteIndirect(signature, 1, argumentBuffer.NativeBuffer, commandOffset, null, 0);
            }

            transitionBuffer(argumentBuffer, previousState);
            uavBarrierPending = true;
        }

        private bool ensureIndirectCommandSignatures()
        {
            if (indirectSignaturesInitialized)
            {
                return indirectSignaturesAvailable;
            }

            indirectSignaturesInitialized = true;
            try
            {
                IndirectArgumentDescription drawArgument = default;
                drawArgument.Type = IndirectArgumentType.Draw;
                CommandSignatureDescription drawDescription = default;
                drawDescription.ByteStride = Unsafe.SizeOf<IndirectDrawArguments>();
                drawDescription.IndirectArguments = new[] { drawArgument };
                drawIndirectSignature = createCommandSignature(drawDescription);

                IndirectArgumentDescription drawIndexedArgument = default;
                drawIndexedArgument.Type = IndirectArgumentType.DrawIndexed;
                CommandSignatureDescription drawIndexedDescription = default;
                drawIndexedDescription.ByteStride = Unsafe.SizeOf<IndirectDrawIndexedArguments>();
                drawIndexedDescription.IndirectArguments = new[] { drawIndexedArgument };
                drawIndexedIndirectSignature = createCommandSignature(drawIndexedDescription);

                IndirectArgumentDescription dispatchArgument = default;
                dispatchArgument.Type = IndirectArgumentType.Dispatch;
                CommandSignatureDescription dispatchDescription = default;
                dispatchDescription.ByteStride = Unsafe.SizeOf<IndirectDispatchArguments>();
                dispatchDescription.IndirectArguments = new[] { dispatchArgument };
                dispatchIndirectSignature = createCommandSignature(dispatchDescription);

                indirectSignaturesAvailable = drawIndirectSignature != null
                    && drawIndexedIndirectSignature != null
                    && dispatchIndirectSignature != null;
            }
            catch
            {
                indirectSignaturesAvailable = false;
            }

            return indirectSignaturesAvailable;
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
            D3D12Pipeline previousGraphics = currentGraphicsPipeline;
            D3D12Pipeline previousCompute = currentComputePipeline;

            nativeCommandList.SetPipelineState(gpuMipPipeline.PipelineState);
            nativeCommandList.SetComputeRootSignature(gpuMipPipeline.RootSignature);
            currentGraphicsPipeline = null;
            currentComputePipeline = gpuMipPipeline;

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
                        using ResourceSet mipResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(gpuMipResourceLayout, srcView, dstView, gpuMipSampler));

                        SetComputeResourceSet(0, mipResourceSet);
                        Util.GetMipDimensions(texture, mipLevel, out uint mipWidth, out uint mipHeight, out _);
                        uint groupCountX = (mipWidth + 7) / 8;
                        uint groupCountY = (mipHeight + 7) / 8;
                        nativeCommandList.Dispatch(groupCountX, groupCountY, 1);
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
                    nativeCommandList.SetPipelineState(previousCompute.PipelineState);
                    nativeCommandList.SetComputeRootSignature(previousCompute.RootSignature);
                    currentComputePipeline = previousCompute;
                    currentGraphicsPipeline = null;
                }
                else if (previousGraphics != null)
                {
                    nativeCommandList.SetPipelineState(previousGraphics.PipelineState);
                    nativeCommandList.SetGraphicsRootSignature(previousGraphics.RootSignature);
                    nativeCommandList.IASetPrimitiveTopology(previousGraphics.PrimitiveTopology);
                    currentGraphicsPipeline = previousGraphics;
                    currentComputePipeline = null;
                }
                else
                {
                    currentComputePipeline = null;
                    currentGraphicsPipeline = null;
                }
            }
        }

        [SupportedOSPlatform("windows")]
        private bool ensureGpuMipmapResources()
        {
            if (gpuMipResourcesInitialized)
            {
                return gpuMipResourcesAvailable;
            }

            gpuMipResourcesInitialized = true;
            try
            {
                byte[] shaderBytes = compileComputeShader(mipmapComputeShaderCode, "cs_main", "cs_5_0");
                using Shader mipShader = gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Compute, shaderBytes, "cs_main"));

                ResourceLayoutDescription resourceLayoutDescription = new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("DestinationTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                    new ResourceLayoutElementDescription("LinearSampler", ResourceKind.Sampler, ShaderStages.Compute));

                gpuMipResourceLayout = gd.ResourceFactory.CreateResourceLayout(resourceLayoutDescription);
                var samplerDescription = SamplerDescription.LINEAR;
                samplerDescription.AddressModeU = SamplerAddressMode.Clamp;
                samplerDescription.AddressModeV = SamplerAddressMode.Clamp;
                samplerDescription.AddressModeW = SamplerAddressMode.Clamp;
                gpuMipSampler = gd.ResourceFactory.CreateSampler(samplerDescription);

                var computePipelineDescription = new ComputePipelineDescription(
                    mipShader,
                    new[] { gpuMipResourceLayout },
                    8,
                    8,
                    1);
                gpuMipPipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(gd.ResourceFactory.CreateComputePipeline(computePipelineDescription));
                gpuMipResourcesAvailable = true;
            }
            catch
            {
                gpuMipResourcesAvailable = false;
            }

            return gpuMipResourcesAvailable;
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

            bool fullResourceView =
                mipStart == 0
                && mipEnd == texture.MipLevels
                && layerStart == 0
                && layerEnd == texture.ArrayLayers;

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
                MethodInfo markerMethod = beginEvent ? beginEventMethod : setMarkerMethod;
                if (markerMethod == null)
                {
                    return;
                }

                ParameterInfo[] parameters = markerMethod.GetParameters();
                object metadata = parameters[0].ParameterType == typeof(int) ? 0 : 0u;
                object sizeValue = parameters[2].ParameterType == typeof(int) ? size : (uint)size;
                if (beginEvent)
                {
                    markerMethod.Invoke(nativeCommandList, new[] { metadata, (object)dataPtr, sizeValue });
                }
                else if (setMarker)
                {
                    markerMethod.Invoke(nativeCommandList, new[] { metadata, (object)dataPtr, sizeValue });
                }
            }
        }

        private MethodInfo getDebugMarkerMethod(string methodName)
        {
            MethodInfo[] methods = nativeCommandList.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
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
            IntPtr GetBufferPointer();
            nuint GetBufferSize();
        }

        private const string mipmapComputeShaderCode = @"
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
            ulong gpuAddress = d3d12Buffer.GpuVirtualAddress + range.Offset;

            switch (bindingInfo.Kind)
            {
                case ResourceKind.UniformBuffer:
                    nativeCommandList.SetGraphicsRootConstantBufferView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.StructuredBufferReadOnly:
                    nativeCommandList.SetGraphicsRootShaderResourceView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.StructuredBufferReadWrite:
                    nativeCommandList.SetGraphicsRootUnorderedAccessView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.TextureReadOnly:
                case ResourceKind.TextureReadWrite:
                case ResourceKind.Sampler:
                    throw new VeldridException("Texture and sampler root bindings must use descriptor tables.");
                default:
                    throw Illegal.Value<ResourceKind>();
            }
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
            ulong gpuAddress = d3d12Buffer.GpuVirtualAddress + range.Offset;

            switch (bindingInfo.Kind)
            {
                case ResourceKind.UniformBuffer:
                    nativeCommandList.SetComputeRootConstantBufferView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.StructuredBufferReadOnly:
                    nativeCommandList.SetComputeRootShaderResourceView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.StructuredBufferReadWrite:
                    nativeCommandList.SetComputeRootUnorderedAccessView(bindingInfo.RootParameterIndex, gpuAddress);
                    break;
                case ResourceKind.TextureReadOnly:
                case ResourceKind.TextureReadWrite:
                case ResourceKind.Sampler:
                    throw new VeldridException("Texture and sampler root bindings must use descriptor tables.");
                default:
                    throw Illegal.Value<ResourceKind>();
            }
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
                    allocateSrvUavDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle);
                    ShaderResourceViewDescription srvDescription = d3d12TextureView.GetShaderResourceViewDescription();
                    gd.Device.CreateShaderResourceView(nativeTexture, srvDescription, cpuHandle);
                    if (compute)
                    {
                        nativeCommandList.SetComputeRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                    }
                    else
                    {
                        nativeCommandList.SetGraphicsRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
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
                    allocateSrvUavDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle);
                    UnorderedAccessViewDescription uavDescription = d3d12TextureView.GetUnorderedAccessViewDescription();
                    gd.Device.CreateUnorderedAccessView(nativeTexture, null, uavDescription, cpuHandle);
                    if (compute)
                    {
                        nativeCommandList.SetComputeRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                    }
                    else
                    {
                        nativeCommandList.SetGraphicsRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                    }
                    break;
                }
                case ResourceKind.Sampler:
                {
                    var d3d12Sampler = Util.AssertSubtype<IBindableResource, D3D12Sampler>(resource);
                    allocateSamplerDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle);
                    Vortice.Direct3D12.SamplerDescription samplerDescription = toD3D12SamplerDescription(d3d12Sampler.Description);
                    gd.Device.CreateSampler(ref samplerDescription, cpuHandle);
                    if (compute)
                    {
                        nativeCommandList.SetComputeRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
                    }
                    else
                    {
                        nativeCommandList.SetGraphicsRootDescriptorTable(bindingInfo.RootParameterIndex, gpuHandle);
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
            if (descriptorHeapsBound)
            {
                return;
            }

            nativeCommandList.SetDescriptorHeaps(new[] { shaderVisibleSrvUavHeap, shaderVisibleSamplerHeap });
            descriptorHeapsBound = true;
        }

        private void allocateSrvUavDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle)
        {
            if (nextSrvUavDescriptor >= maxSrvUavDescriptors)
            {
                throw new VeldridException("D3D12 SRV/UAV descriptor heap exhausted for this CommandList recording.");
            }

            CpuDescriptorHandle cpuStart = shaderVisibleSrvUavHeap.GetCPUDescriptorHandleForHeapStart();
            GpuDescriptorHandle gpuStart = shaderVisibleSrvUavHeap.GetGPUDescriptorHandleForHeapStart();
            cpuHandle = new CpuDescriptorHandle(cpuStart, (int)nextSrvUavDescriptor, (uint)srvUavDescriptorSize);
            gpuHandle = new GpuDescriptorHandle(gpuStart, (int)nextSrvUavDescriptor, (uint)srvUavDescriptorSize);
            nextSrvUavDescriptor++;
        }

        private void allocateSamplerDescriptor(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle)
        {
            if (nextSamplerDescriptor >= maxSamplerDescriptors)
            {
                throw new VeldridException("D3D12 sampler descriptor heap exhausted for this CommandList recording.");
            }

            CpuDescriptorHandle cpuStart = shaderVisibleSamplerHeap.GetCPUDescriptorHandleForHeapStart();
            GpuDescriptorHandle gpuStart = shaderVisibleSamplerHeap.GetGPUDescriptorHandleForHeapStart();
            cpuHandle = new CpuDescriptorHandle(cpuStart, (int)nextSamplerDescriptor, (uint)samplerDescriptorSize);
            gpuHandle = new GpuDescriptorHandle(gpuStart, (int)nextSamplerDescriptor, (uint)samplerDescriptorSize);
            nextSamplerDescriptor++;
        }

        private static Vortice.Direct3D12.SamplerDescription toD3D12SamplerDescription(Veldrid.SamplerDescription description)
        {
            bool comparison = description.ComparisonKind != null;
            ComparisonFunction comparisonFunction = description.ComparisonKind != null
                ? D3D12Formats.ToComparison(description.ComparisonKind.Value)
                : ComparisonFunction.Always;

            return new Vortice.Direct3D12.SamplerDescription
            {
                Filter = D3D12Formats.ToFilter(description.Filter, comparison),
                AddressU = D3D12Formats.ToTextureAddressMode(description.AddressModeU),
                AddressV = D3D12Formats.ToTextureAddressMode(description.AddressModeV),
                AddressW = D3D12Formats.ToTextureAddressMode(description.AddressModeW),
                MipLODBias = description.LodBias,
                MaxAnisotropy = description.MaximumAnisotropy == 0 ? 1u : description.MaximumAnisotropy,
                ComparisonFunction = comparisonFunction,
                BorderColor = D3D12Formats.ToBorderColor(description.BorderColor),
                MinLOD = description.MinimumLod,
                MaxLOD = description.MaximumLod
            };
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

        private void transitionSwapchainBackBuffersToPresent()
        {
            if (Framebuffer is not D3D12SwapchainFramebuffer swapchainFramebuffer)
            {
                return;
            }

            if (transitionedBackBufferIndex < 0)
            {
                return;
            }

            if (swapchainFramebuffer.Swapchain.TryGetCurrentBackBuffer(out ID3D12Resource backBuffer, out _, out int currentIndex, out ResourceStates state)
                && currentIndex == transitionedBackBufferIndex)
            {
                transition(backBuffer, state, ResourceStates.Present);
                swapchainFramebuffer.Swapchain.SetBackBufferState(currentIndex, ResourceStates.Present);
            }
        }
    }
}
