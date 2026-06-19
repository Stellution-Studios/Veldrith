using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Owns the D3D12 compute path used to generate mipmaps on the GPU.
/// </summary>
internal sealed class D3D12GpuMipmapGenerator : IDisposable {

    /// <summary>
    /// Stores the HLSL compute shader used to downsample one mip level into the next mip level.
    /// </summary>
    private const string MipmapComputeShaderCode = @"Texture2D<float4> SourceTexture : register(t0);

                                                      RWTexture2D<float4> DestinationTexture : register(u0);

                                                      SamplerState LinearSampler : register(s0);

                                                      [numthreads(8, 8, 1)]
                                                      void cs_main(uint3 dispatchThreadID : SV_DispatchThreadID) {
                                                          uint width;
                                                          uint height;
                                                          DestinationTexture.GetDimensions(width, height);

                                                          if (dispatchThreadID.x >= width || dispatchThreadID.y >= height) {
                                                              return;
                                                          }

                                                          float2 uv = (float2(dispatchThreadID.xy) + 0.5f) / float2(width, height);
                                                          float4 value = SourceTexture.SampleLevel(LinearSampler, uv, 0.0f);
                                                          DestinationTexture[dispatchThreadID.xy] = value;
                                                      }";

    /// <summary>
    /// Stores the HLSL compute shader used to downsample all array layers for one mip level.
    /// </summary>
    private const string MipmapArrayComputeShaderCode = @"Texture2DArray<float4> SourceTexture : register(t0);

                                                           RWTexture2DArray<float4> DestinationTexture : register(u0);

                                                           SamplerState LinearSampler : register(s0);

                                                           [numthreads(8, 8, 1)]
                                                           void cs_main(uint3 dispatchThreadID : SV_DispatchThreadID) {
                                                               uint width;
                                                               uint height;
                                                               uint layers;
                                                               DestinationTexture.GetDimensions(width, height, layers);

                                                               if (dispatchThreadID.x >= width || dispatchThreadID.y >= height || dispatchThreadID.z >= layers) {
                                                                   return;
                                                               }

                                                               float2 uv = (float2(dispatchThreadID.xy) + 0.5f) / float2(width, height);
                                                               float4 value = SourceTexture.SampleLevel(LinearSampler, float3(uv, dispatchThreadID.z), 0.0f);
                                                               DestinationTexture[dispatchThreadID] = value;
                                                           }";

    /// <summary>
    /// Stores the graphics device used to create pipeline resources and transient bindings.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Stores the command list that records mipmap dispatches and restores D3D12 binding state.
    /// </summary>
    private readonly D3D12CommandList _commandList;

    /// <summary>
    /// Reusable buffer for captured subresource states before mipmap generation.
    /// </summary>
    private readonly ResourceStates[] _previousStatesScratch = new ResourceStates[128];

    /// <summary>
    /// Reusable buffer for tracked subresource states during mipmap generation.
    /// </summary>
    private readonly ResourceStates[] _subresourceStatesScratch = new ResourceStates[128];

    /// <summary>
    /// Stores the compute pipeline used to generate one destination mip from one source mip.
    /// </summary>
    private D3D12Pipeline _pipeline;

    /// <summary>
    /// Stores the compute pipeline used to generate one destination mip for all array layers.
    /// </summary>
    private D3D12Pipeline _arrayPipeline;

    /// <summary>
    /// Stores the resource layout expected by the mipmap compute shader.
    /// </summary>
    private ResourceLayout _resourceLayout;

    /// <summary>
    /// Stores the linear clamp sampler used by the mipmap compute shader.
    /// </summary>
    private Sampler _sampler;

    /// <summary>
    /// Tracks whether initialization has already been attempted.
    /// </summary>
    private bool _resourcesInitialized;

    /// <summary>
    /// Stores whether the GPU mipmap resources are available after initialization.
    /// </summary>
    private bool _resourcesAvailable;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12GpuMipmapGenerator" /> class.
    /// </summary>
    /// <param name="gd">The graphics device used to create D3D12 resources.</param>
    /// <param name="commandList">The command list that records mipmap commands.</param>
    internal D3D12GpuMipmapGenerator(D3D12GraphicsDevice gd, D3D12CommandList commandList) {
        this._gd = gd;
        this._commandList = commandList;
    }

    /// <summary>
    /// Releases lazily created pipeline resources.
    /// </summary>
    public void Dispose() {
        this._pipeline?.Dispose();
        this._arrayPipeline?.Dispose();
        this._resourceLayout?.Dispose();
        this._sampler?.Dispose();
    }

    /// <summary>
    /// Attempts to generate mipmaps on the GPU.
    /// </summary>
    /// <param name="texture">The D3D12 texture whose mip chain should be generated.</param>
    /// <returns><see langword="true" /> when GPU generation was recorded; otherwise, <see langword="false" />.</returns>
    [SupportedOSPlatform("windows")]
    internal bool TryGenerate(D3D12Texture texture) {
        if (!this.CanUseGpuPath(texture) || !this.EnsureResources()) {
            return false;
        }

        this.Generate(texture);
        return true;
    }

    /// <summary>
    /// Checks whether a texture is compatible with the compute mipmap path.
    /// </summary>
    /// <param name="texture">The texture to inspect.</param>
    /// <returns><see langword="true" /> when the compute path can be used.</returns>
    internal bool CanUseGpuPath(Texture texture) {
        return texture.Type == TextureType.Texture2D
               && texture.SampleCount == TextureSampleCount.Count1
               && (texture.Usage & TextureUsage.Cubemap) == 0
               && (texture.Usage & (TextureUsage.Sampled | TextureUsage.Storage)) == (TextureUsage.Sampled | TextureUsage.Storage)
               && this._gd.GetPixelFormatSupport(texture.Format, texture.Type, TextureUsage.Sampled | TextureUsage.Storage)
               && !FormatHelpers.IsCompressedFormat(texture.Format)
               && (texture.Usage & TextureUsage.DepthStencil) == 0;
    }

    /// <summary>
    /// Records compute dispatches for every mip level and restores command-list binding state afterwards.
    /// </summary>
    /// <param name="texture">The texture whose mipmaps should be generated.</param>
    private void Generate(D3D12Texture texture) {
        D3D12Pipeline previousGraphics = this._commandList.CurrentGraphicsPipeline;
        D3D12Pipeline previousCompute = this._commandList.CurrentComputePipeline;
        BoundResourceSetInfo previousComputeSet0 = this._commandList.ComputeResourceSets.CaptureSlot(0);
        uint layerCount = texture.ArrayLayers;
        bool useArrayPipeline = layerCount > 1;
        D3D12Pipeline mipPipeline = useArrayPipeline ? this._arrayPipeline : this._pipeline;
        uint subresourceCount = texture.MipLevels * layerCount;
        ResourceStates[] previousStates = this.GetPreviousStateBuffer(texture, subresourceCount);
        ResourceStates[] subresourceStates = this.GetWorkingStateBuffer(subresourceCount);
        Array.Copy(previousStates, subresourceStates, (int)subresourceCount);

        try {
            this._commandList.SetPipelineStateNoAllocForInternalUse(mipPipeline.PipelineState);
            this._commandList.SetComputeRootSignatureNoAllocForInternalUse(mipPipeline.RootSignature);
            this._commandList.CurrentGraphicsPipeline = null;
            this._commandList.CurrentComputePipeline = mipPipeline;

            if (useArrayPipeline) {
                for (uint mipLevel = 1; mipLevel < texture.MipLevels; mipLevel++) {
                    this.GenerateMipLevelArray(texture, mipLevel, layerCount, subresourceStates);
                }
            }
            else {
                for (uint mipLevel = 1; mipLevel < texture.MipLevels; mipLevel++) {
                    this.GenerateMipLevel(texture, 0, mipLevel, subresourceStates);
                }
            }
        }
        finally {
            this.RestoreTextureStates(texture, previousStates, subresourceStates, subresourceCount);
            this._commandList.ComputeResourceSets.RestoreSlot(0, previousComputeSet0);
            this.RestoreCommandListState(previousGraphics, previousCompute);
        }
    }

    /// <summary>
    /// Records the barriers, descriptors, and dispatch for one destination mip level.
    /// </summary>
    /// <param name="texture">The texture being processed.</param>
    /// <param name="layer">The array layer being processed.</param>
    /// <param name="mipLevel">The destination mip level.</param>
    /// <param name="subresourceStates">The mutable state table for each subresource.</param>
    private void GenerateMipLevel(D3D12Texture texture, uint layer, uint mipLevel, ResourceStates[] subresourceStates) {
        uint srcSubresource = texture.CalculateSubresource(mipLevel - 1, layer);
        uint dstSubresource = texture.CalculateSubresource(mipLevel, layer);

        if (subresourceStates[srcSubresource] != ResourceStates.NonPixelShaderResource) {
            this._commandList.TransitionSubresourceForInternalUse(texture.NativeTexture, subresourceStates[srcSubresource], ResourceStates.NonPixelShaderResource, srcSubresource);
            subresourceStates[srcSubresource] = ResourceStates.NonPixelShaderResource;
            texture.SetSubresourceState(srcSubresource, ResourceStates.NonPixelShaderResource);
        }

        if (subresourceStates[dstSubresource] != ResourceStates.UnorderedAccess) {
            this._commandList.TransitionSubresourceForInternalUse(texture.NativeTexture, subresourceStates[dstSubresource], ResourceStates.UnorderedAccess, dstSubresource);
            subresourceStates[dstSubresource] = ResourceStates.UnorderedAccess;
            texture.SetSubresourceState(dstSubresource, ResourceStates.UnorderedAccess);
        }

        using TextureView srcView = this._gd.ResourceFactory.CreateTextureView(new TextureViewDescription(texture, mipLevel - 1, 1, layer, 1));
        using TextureView dstView = this._gd.ResourceFactory.CreateTextureView(new TextureViewDescription(texture, mipLevel, 1, layer, 1));
        using ResourceSet mipResourceSet = this._gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this._resourceLayout, srcView, dstView, this._sampler));

        this._commandList.SetComputeResourceSet(0, mipResourceSet);
        Util.GetMipDimensions(texture, mipLevel, out uint mipWidth, out uint mipHeight, out _);
        uint groupCountX = (mipWidth + 7) / 8;
        uint groupCountY = (mipHeight + 7) / 8;
        this._commandList.FlushComputeResourceSetsForInternalUse();
        this._commandList.FlushPendingUavBarrierForInternalUse();
        this._commandList.FlushPendingBarriersForInternalUse();
        this._commandList.DispatchNoAllocForInternalUse(groupCountX, groupCountY, 1);
        this._commandList.MarkUavBarrierPendingForInternalUse();
    }

    /// <summary>
    /// Records one dispatch that generates a destination mip level for all array layers.
    /// </summary>
    /// <param name="texture">The texture being processed.</param>
    /// <param name="mipLevel">The destination mip level.</param>
    /// <param name="layerCount">The number of array layers to process.</param>
    /// <param name="subresourceStates">The mutable state table for each subresource.</param>
    private void GenerateMipLevelArray(D3D12Texture texture, uint mipLevel, uint layerCount, ResourceStates[] subresourceStates) {
        for (uint layer = 0; layer < layerCount; layer++) {
            uint srcSubresource = texture.CalculateSubresource(mipLevel - 1, layer);
            uint dstSubresource = texture.CalculateSubresource(mipLevel, layer);

            if (subresourceStates[srcSubresource] != ResourceStates.NonPixelShaderResource) {
                this._commandList.TransitionSubresourceForInternalUse(texture.NativeTexture, subresourceStates[srcSubresource], ResourceStates.NonPixelShaderResource, srcSubresource);
                subresourceStates[srcSubresource] = ResourceStates.NonPixelShaderResource;
                texture.SetSubresourceState(srcSubresource, ResourceStates.NonPixelShaderResource);
            }

            if (subresourceStates[dstSubresource] != ResourceStates.UnorderedAccess) {
                this._commandList.TransitionSubresourceForInternalUse(texture.NativeTexture, subresourceStates[dstSubresource], ResourceStates.UnorderedAccess, dstSubresource);
                subresourceStates[dstSubresource] = ResourceStates.UnorderedAccess;
                texture.SetSubresourceState(dstSubresource, ResourceStates.UnorderedAccess);
            }
        }

        using TextureView srcView = this._gd.ResourceFactory.CreateTextureView(new TextureViewDescription(texture, mipLevel - 1, 1, 0, layerCount));
        using TextureView dstView = this._gd.ResourceFactory.CreateTextureView(new TextureViewDescription(texture, mipLevel, 1, 0, layerCount));
        using ResourceSet mipResourceSet = this._gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(this._resourceLayout, srcView, dstView, this._sampler));

        this._commandList.SetComputeResourceSet(0, mipResourceSet);
        Util.GetMipDimensions(texture, mipLevel, out uint mipWidth, out uint mipHeight, out _);
        uint groupCountX = (mipWidth + 7) / 8;
        uint groupCountY = (mipHeight + 7) / 8;
        this._commandList.FlushComputeResourceSetsForInternalUse();
        this._commandList.FlushPendingUavBarrierForInternalUse();
        this._commandList.FlushPendingBarriersForInternalUse();
        this._commandList.DispatchNoAllocForInternalUse(groupCountX, groupCountY, layerCount);
        this._commandList.MarkUavBarrierPendingForInternalUse();
    }

    /// <summary>
    /// Restores subresource states that were changed while generating mipmaps.
    /// </summary>
    /// <param name="texture">The texture being restored.</param>
    /// <param name="previousStates">The states captured before mipmap generation.</param>
    /// <param name="subresourceStates">The states currently tracked for the texture.</param>
    /// <param name="subresourceCount">The number of subresources to restore.</param>
    private void RestoreTextureStates(D3D12Texture texture, ResourceStates[] previousStates, ResourceStates[] subresourceStates, uint subresourceCount) {
        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            if (subresourceStates[subresource] == previousStates[subresource]) {
                continue;
            }

            this._commandList.TransitionSubresourceForInternalUse(texture.NativeTexture, subresourceStates[subresource], previousStates[subresource], subresource);
            subresourceStates[subresource] = previousStates[subresource];
        }

        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            texture.SetSubresourceState(subresource, subresourceStates[subresource]);
        }
    }

    /// <summary>
    /// Restores the command-list pipeline and dirty binding state that was active before mipmap generation.
    /// </summary>
    /// <param name="previousGraphics">The graphics pipeline that was active before mipmap generation.</param>
    /// <param name="previousCompute">The compute pipeline that was active before mipmap generation.</param>
    private void RestoreCommandListState(D3D12Pipeline previousGraphics, D3D12Pipeline previousCompute) {
        if (previousCompute != null) {
            this._commandList.SetPipelineStateNoAllocForInternalUse(previousCompute.PipelineState);
            this._commandList.SetComputeRootSignatureNoAllocForInternalUse(previousCompute.RootSignature);
            this._commandList.CurrentComputePipeline = previousCompute;
            this._commandList.CurrentGraphicsPipeline = null;
            this._commandList.RootBindingCache.InvalidateCompute();
            this._commandList.ComputeResourceSets.EnsureCapacity(previousCompute.ResourceSetCount);
            this._commandList.ComputeResourceSets.MarkBoundChanged(previousCompute.ResourceSetCount);
        }
        else if (previousGraphics != null) {
            this._commandList.SetPipelineStateNoAllocForInternalUse(previousGraphics.PipelineState);
            this._commandList.SetGraphicsRootSignatureNoAllocForInternalUse(previousGraphics.RootSignature);
            this._commandList.SetPrimitiveTopologyForInternalUse(previousGraphics.PrimitiveTopology);
            if (previousGraphics.UsesStencilReference) {
                this._commandList.SetStencilReferenceForInternalUse(previousGraphics.StencilReference);
            }

            this._commandList.CurrentGraphicsPipeline = previousGraphics;
            this._commandList.CurrentComputePipeline = null;
            this._commandList.RootBindingCache.InvalidateGraphics();
            this._commandList.GraphicsResourceSets.EnsureCapacity(previousGraphics.ResourceSetCount);
            this._commandList.GraphicsResourceSets.MarkBoundChanged(previousGraphics.ResourceSetCount);
        }
        else {
            this._commandList.CurrentComputePipeline = null;
            this._commandList.CurrentGraphicsPipeline = null;
        }
    }

    /// <summary>
    /// Gets a buffer containing the texture states captured before mipmap generation.
    /// </summary>
    /// <param name="texture">The texture to inspect.</param>
    /// <param name="subresourceCount">The number of subresources to capture.</param>
    /// <returns>The captured state buffer.</returns>
    private ResourceStates[] GetPreviousStateBuffer(D3D12Texture texture, uint subresourceCount) {
        ResourceStates[] states = subresourceCount <= (uint)this._previousStatesScratch.Length
            ? this._previousStatesScratch
            : new ResourceStates[subresourceCount];

        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            states[subresource] = texture.GetSubresourceState(subresource);
        }

        return states;
    }

    /// <summary>
    /// Gets a mutable state buffer for mipmap generation.
    /// </summary>
    /// <param name="subresourceCount">The number of tracked subresources.</param>
    /// <returns>The working state buffer.</returns>
    private ResourceStates[] GetWorkingStateBuffer(uint subresourceCount) {
        return subresourceCount <= (uint)this._subresourceStatesScratch.Length
            ? this._subresourceStatesScratch
            : new ResourceStates[subresourceCount];
    }

    /// <summary>
    /// Ensures that pipeline resources needed by the compute path have been created.
    /// </summary>
    /// <returns><see langword="true" /> when resources are available.</returns>
    [SupportedOSPlatform("windows")]
    private bool EnsureResources() {
        if (this._resourcesInitialized) {
            return this._resourcesAvailable;
        }

        this._resourcesInitialized = true;
        try {
            byte[] shaderBytes = CompileComputeShader(MipmapComputeShaderCode, "cs_main", "cs_5_0");
            byte[] arrayShaderBytes = CompileComputeShader(MipmapArrayComputeShaderCode, "cs_main", "cs_5_0");
            using Shader mipShader = this._gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Compute, shaderBytes, "cs_main"));
            using Shader mipArrayShader = this._gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Compute, arrayShaderBytes, "cs_main"));

            ResourceLayoutDescription resourceLayoutDescription = new(new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Compute), new ResourceLayoutElementDescription("DestinationTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute), new ResourceLayoutElementDescription("LinearSampler", ResourceKind.Sampler, ShaderStages.Compute));

            this._resourceLayout = this._gd.ResourceFactory.CreateResourceLayout(resourceLayoutDescription);
            SamplerDescription samplerDescription = SamplerDescription.LINEAR;
            samplerDescription.AddressModeU = SamplerAddressMode.Clamp;
            samplerDescription.AddressModeV = SamplerAddressMode.Clamp;
            samplerDescription.AddressModeW = SamplerAddressMode.Clamp;
            this._sampler = this._gd.ResourceFactory.CreateSampler(samplerDescription);

            ComputePipelineDescription computePipelineDescription = new(mipShader, [this._resourceLayout], 8, 8, 1);
            ComputePipelineDescription arrayComputePipelineDescription = new(mipArrayShader, [this._resourceLayout], 8, 8, 1);

            this._pipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(this._gd.ResourceFactory.CreateComputePipeline(computePipelineDescription));
            this._arrayPipeline = Util.AssertSubtype<Pipeline, D3D12Pipeline>(this._gd.ResourceFactory.CreateComputePipeline(arrayComputePipelineDescription));
            this._resourcesAvailable = true;
        }
        catch {
            this._resourcesAvailable = false;
        }

        return this._resourcesAvailable;
    }

    /// <summary>
    /// Compiles the HLSL compute shader used by the GPU mipmap path.
    /// </summary>
    /// <param name="sourceCode">The HLSL source code.</param>
    /// <param name="entryPoint">The shader entry point.</param>
    /// <param name="target">The shader model target.</param>
    /// <returns>The compiled shader bytecode.</returns>
    [SupportedOSPlatform("windows")]
    private static byte[] CompileComputeShader(string sourceCode, string entryPoint, string target) {
        byte[] sourceBytes = Encoding.UTF8.GetBytes(sourceCode);

        int result = D3DCompile(sourceBytes, (nuint)sourceBytes.Length, null, IntPtr.Zero, IntPtr.Zero, entryPoint, target, 0, 0, out IntPtr codeBlobPtr, out IntPtr errorBlobPtr);

        string errorMessage = null;

        if (errorBlobPtr != IntPtr.Zero) {
            try {
                ID3DBlob errorBlob = (ID3DBlob)Marshal.GetObjectForIUnknown(errorBlobPtr);
                IntPtr errorPtr = errorBlob.GetBufferPointer();
                int errorSize = checked((int)errorBlob.GetBufferSize());
                if (errorSize > 0) {
                    byte[] errorBytes = new byte[errorSize];
                    Marshal.Copy(errorPtr, errorBytes, 0, errorSize);
                    errorMessage = Encoding.UTF8.GetString(errorBytes).TrimEnd('\0', '\r', '\n');
                }
            }
            finally {
                Marshal.Release(errorBlobPtr);
            }
        }

        if (result < 0 || codeBlobPtr == IntPtr.Zero) {
            throw new VeldridException($"Failed to compile D3D12 mipmap compute shader. {errorMessage}");
        }

        try {
            ID3DBlob codeBlob = (ID3DBlob)Marshal.GetObjectForIUnknown(codeBlobPtr);
            IntPtr codePtr = codeBlob.GetBufferPointer();
            int codeSize = checked((int)codeBlob.GetBufferSize());
            byte[] shaderBytes = new byte[codeSize];
            Marshal.Copy(codePtr, shaderBytes, 0, codeSize);
            return shaderBytes;
        }
        finally {
            Marshal.Release(codeBlobPtr);
        }
    }

    /// <summary>
    /// Compiles shader source through D3DCompiler.
    /// </summary>
    /// <param name="srcData">The source byte buffer.</param>
    /// <param name="srcDataSize">The source byte count.</param>
    /// <param name="sourceName">The optional source name.</param>
    /// <param name="defines">The optional macro definitions.</param>
    /// <param name="include">The optional include handler.</param>
    /// <param name="entryPoint">The shader entry point.</param>
    /// <param name="target">The shader model target.</param>
    /// <param name="flags1">The first compiler flags value.</param>
    /// <param name="flags2">The second compiler flags value.</param>
    /// <param name="code">The compiled code blob.</param>
    /// <param name="errorMsgs">The compiler error blob.</param>
    /// <returns>The HRESULT produced by D3DCompiler.</returns>
    [DllImport("d3dcompiler_47.dll", CharSet = CharSet.Ansi)]
    private static extern int D3DCompile(byte[] srcData, nuint srcDataSize, string sourceName, IntPtr defines, IntPtr include, string entryPoint, string target, uint flags1, uint flags2, out IntPtr code, out IntPtr errorMsgs);

    /// <summary>
    /// Defines the ID3DBlob interface returned by D3DCompiler.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("8BA5FB08-5195-40E2-AC58-0D989C3A0102")]
    private interface ID3DBlob {

        /// <summary>
        /// Gets the native pointer to the blob contents.
        /// </summary>
        /// <returns>The native buffer pointer.</returns>
        [PreserveSig]
        IntPtr GetBufferPointer();

        /// <summary>
        /// Gets the size, in bytes, of the blob contents.
        /// </summary>
        /// <returns>The native buffer size.</returns>
        [PreserveSig]
        nuint GetBufferSize();
    }
}
