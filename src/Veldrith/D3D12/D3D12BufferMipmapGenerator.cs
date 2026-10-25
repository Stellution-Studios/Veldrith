using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Records mipmap generation through a temporary buffer when the texture cannot be written through a typed UAV.
/// </summary>
internal sealed partial class D3D12GpuMipmapGenerator {

    /// <summary>
    /// Caches compute pipelines for each pixel format and texture dimension.
    /// </summary>
    private readonly Dictionary<(PixelFormat, TextureType), D3D12Pipeline> _bufferPipelines = new();

    /// <summary>
    /// Caches the temporary buffer and source mip bindings for each texture.
    /// </summary>
    private readonly Dictionary<D3D12Texture, BufferMipResources> _bufferMipResources = new();

    /// <summary>
    /// Stores the layout shared by the source texture, output buffer, and filtering sampler.
    /// </summary>
    private ResourceLayout _bufferLayout;

    /// <summary>
    /// Stores the linear clamp sampler used to filter the source mip level.
    /// </summary>
    private Sampler _bufferSampler;

    /// <summary>
    /// Checks whether a color format and texture dimension have a buffer-based mipmap shader.
    /// </summary>
    /// <param name="format">The pixel format to inspect.</param>
    /// <param name="type">The texture dimension to inspect.</param>
    /// <returns><see langword="true" /> when a shader can encode the destination format.</returns>
    internal static bool SupportsBufferMipmaps(PixelFormat format, TextureType type) {
        return (type == TextureType.Texture1D || type == TextureType.Texture2D || type == TextureType.Texture3D)
               && format != PixelFormat.D24UNormS8UInt
               && format != PixelFormat.D32FloatS8UInt
               && format != PixelFormat.Etc2R8G8B8UNorm
               && format != PixelFormat.Etc2R8G8B8A1UNorm
               && format != PixelFormat.Etc2R8G8B8A8UNorm;
    }

    /// <summary>
    /// Attempts to filter and encode each destination mip into a buffer, then copy it into the texture.
    /// </summary>
    /// <param name="texture">The texture whose mip chain should be generated.</param>
    /// <returns><see langword="true" /> when mipmap commands were recorded; otherwise, <see langword="false" />.</returns>
    [SupportedOSPlatform("windows")]
    private bool TryGenerateThroughBuffer(D3D12Texture texture) {
        if (!SupportsBufferMipmaps(texture.Format, texture.Type) || texture.SampleCount != TextureSampleCount.Count1 || (texture.Usage & (TextureUsage.DepthStencil | TextureUsage.Staging)) != 0) {
            return false;
        }

        D3D12Pipeline pipeline = this.GetBufferMipmapPipeline(texture.Format, texture.Type);
        BufferMipResources resources = this.GetBufferMipResources(texture);

        // Preserve the caller's bindings and resource states while recording the internal compute work.
        D3D12Pipeline previousGraphics = this._commandList.CurrentGraphicsPipeline;
        D3D12Pipeline previousCompute = this._commandList.CurrentComputePipeline;
        BoundResourceSetInfo previousSet = this._commandList.ComputeResourceSets.CaptureSlot(0);
        ResourceStates[] previousStates = this.GetPreviousStateBuffer(texture, texture.SubresourceCount);

        try {
            this._commandList.SetPipelineStateNoAllocForInternalUse(pipeline.PipelineState);
            this._commandList.SetComputeRootSignatureNoAllocForInternalUse(pipeline.RootSignature);
            this._commandList.CurrentGraphicsPipeline = null;
            this._commandList.CurrentComputePipeline = pipeline;
            this._commandList.RootBindingCache.InvalidateCompute();

            for (uint mip = 1; mip < texture.MipLevels; mip++) {
                Util.GetMipDimensions(texture, mip, out uint width, out uint height, out uint depth);
                bool compressed = FormatHelpers.IsCompressedFormat(texture.Format);

                // D3D12 buffer-to-texture copies require 256-byte rows and 512-byte layer offsets.
                // These pitches must match the addresses calculated by the generated shader.
                uint rows = compressed ? (height + 3) / 4 : height;
                uint rowPitch = (FormatHelpers.GetRowPitch(width, texture.Format) + 255) & ~255u;
                uint slicePitch = rowPitch * rows;
                uint layerPitch = (slicePitch * depth + 511) & ~511u;

                // Each shader thread writes a 4x4 pixel tile or one compressed block.
                // The dispatch depth addresses volume slices or individual array and cubemap faces.
                this.SetMipmapResourceSet(resources.Sets[mip - 1]);
                this._commandList.FlushComputeResourceSetsForInternalUse();
                this._commandList.FlushPendingUavBarrierForInternalUse();
                this._commandList.FlushPendingBarriersForInternalUse();
                this._commandList.DispatchNoAllocForInternalUse(
                    ((width + 3) / 4 + 7) / 8,
                    ((height + 3) / 4 + 7) / 8,
                    texture.Type == TextureType.Texture3D ? depth : texture.EffectiveArrayLayers
                );
                this._commandList.MarkUavBarrierPendingForInternalUse();

                // A raw UAV buffer supports both packed color pixels and BC blocks.
                // Copying its output avoids requiring Storage usage on the texture itself.
                this._commandList.FlushPendingUavBarrierForInternalUse();
                this._commandList.TransitionBufferForInternalUse(resources.Buffer, ResourceStates.CopySource);

                for (uint layer = 0; layer < texture.EffectiveArrayLayers; layer++) {
                    uint subresource = texture.CalculateSubresource(mip, layer);
                    ResourceStates state = texture.GetSubresourceState(subresource);
                    this._commandList.TransitionSubresourceForInternalUse(texture.NativeTexture, state, ResourceStates.CopyDest, subresource);
                    texture.SetSubresourceState(subresource, ResourceStates.CopyDest);
                    this._commandList.FlushPendingBarriersForInternalUse();

                    PlacedSubresourceFootPrint footprint = new() {
                        Offset = (ulong)layer * layerPitch,
                        Footprint = new SubresourceFootPrint(
                            D3D12Formats.ToDxgiFormat(texture.Format),
                            compressed ? (width + 3) & ~3u : width,
                            compressed ? (height + 3) & ~3u : height,
                            depth,
                            rowPitch
                        )
                    };

                    TextureCopyLocation source = new(resources.Buffer.NativeBuffer, footprint);
                    TextureCopyLocation destination = new(texture.NativeTexture, subresource);

                    // BC footprints are block-rounded even for the 2x2 and 1x1 tail mips.
                    this._commandList.NativeCommandList.CopyTextureRegion(destination, 0, 0, 0, source, null);
                }
            }
        }
        finally {
            // The caller must observe the same texture states and pipeline bindings after generation.
            for (uint subresource = 0; subresource < texture.SubresourceCount; subresource++) {
                ResourceStates state = texture.GetSubresourceState(subresource);
                if (state != previousStates[subresource]) {
                    this._commandList.TransitionSubresourceForInternalUse(texture.NativeTexture, state, previousStates[subresource], subresource);
                    texture.SetSubresourceState(subresource, previousStates[subresource]);
                }
            }

            this._commandList.ComputeResourceSets.RestoreSlot(0, previousSet);
            this.RestoreCommandListState(previousGraphics, previousCompute);
        }

        return true;
    }

    /// <summary>
    /// Gets or creates the compute pipeline that filters and packs the requested texture format.
    /// </summary>
    /// <param name="format">The format to sample and encode.</param>
    /// <param name="type">The dimension of the source texture.</param>
    /// <returns>The cached compute pipeline for the format and dimension.</returns>
    [SupportedOSPlatform("windows")]
    private D3D12Pipeline GetBufferMipmapPipeline(PixelFormat format, TextureType type) {
        if (this._bufferPipelines.TryGetValue((format, type), out D3D12Pipeline pipeline)) {
            return pipeline;
        }

        if (this._bufferLayout == null) {
            this._bufferLayout = this._gd.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                new ResourceLayoutElementDescription("Output", ResourceKind.StructuredBufferReadWrite, ShaderStages.Compute),
                new ResourceLayoutElementDescription("LinearSampler", ResourceKind.Sampler, ShaderStages.Compute)
            ));

            SamplerDescription sampler = SamplerDescription.LINEAR;
            sampler.AddressModeU = SamplerAddressMode.Clamp;
            sampler.AddressModeV = SamplerAddressMode.Clamp;
            sampler.AddressModeW = SamplerAddressMode.Clamp;
            this._bufferSampler = this._gd.ResourceFactory.CreateSampler(sampler);
        }

        byte[] code = CompileComputeShader(CreateBufferMipmapShader(format, type), "cs_main", "cs_5_0");
        using Shader shader = this._gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Compute, code, "cs_main"));

        pipeline = (D3D12Pipeline)this._gd.ResourceFactory.CreateComputePipeline(
            new ComputePipelineDescription(shader, new[] { this._bufferLayout }, 8, 8, 1)
        );

        this._bufferPipelines.Add((format, type), pipeline);
        return pipeline;
    }

    /// <summary>
    /// Gets or creates the reusable output buffer and source views for a texture's mip chain.
    /// </summary>
    /// <param name="texture">The texture whose source mip levels should be bound.</param>
    /// <returns>The cached buffer, source views, and resource sets.</returns>
    private BufferMipResources GetBufferMipResources(D3D12Texture texture) {
        if (this._bufferMipResources.TryGetValue(texture, out BufferMipResources resources)) {
            return resources;
        }

        // Release dead texture references on long-lived command lists.
        List<D3D12Texture> removed = new();

        foreach (KeyValuePair<D3D12Texture, BufferMipResources> entry in this._bufferMipResources) {
            if (entry.Key.IsDisposed) {
                entry.Value.Dispose();
                removed.Add(entry.Key);
            }
        }

        foreach (D3D12Texture key in removed) {
            this._bufferMipResources.Remove(key);
        }

        // The first generated mip is the largest. Its buffer is reused for every smaller mip.
        Util.GetMipDimensions(texture, 1, out uint width, out uint height, out uint depth);
        uint rows = FormatHelpers.IsCompressedFormat(texture.Format) ? (height + 3) / 4 : height;
        uint pitch = (FormatHelpers.GetRowPitch(width, texture.Format) + 255) & ~255u;
        uint size = checked(((pitch * rows * depth + 511) & ~511u) * texture.EffectiveArrayLayers);

        BufferDescription bufferDescription = new(size, BufferUsage.StructuredBufferReadWrite, 4, true);
        D3D12DeviceBuffer buffer = (D3D12DeviceBuffer)this._gd.ResourceFactory.CreateBuffer(bufferDescription);
        resources = new BufferMipResources(buffer, texture.MipLevels - 1);

        try {
            for (uint mip = 1; mip < texture.MipLevels; mip++) {
                TextureViewDescription description = new(texture, mip - 1, 1, 0, texture.EffectiveArrayLayers);
                D3D12TextureView view = new(this._gd, ref description) {
                    UseArrayView = texture.Type != TextureType.Texture3D
                };

                resources.Views[mip - 1] = view;

                // Internal views may expose individual cube faces, or a texture created solely for mip generation.
                ResourceSetDescription setDescription = new(this._bufferLayout, view, buffer, this._bufferSampler);
                resources.Sets[mip - 1] = new D3D12ResourceSet(this._gd, ref setDescription);
            }

            this._bufferMipResources.Add(texture, resources);
            return resources;
        }
        catch {
            resources.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Releases cached buffers, source bindings, pipelines, and shared filtering resources.
    /// </summary>
    private void DisposeBufferMipmapResources() {
        foreach (BufferMipResources resources in this._bufferMipResources.Values) {
            resources.Dispose();
        }

        this._bufferMipResources.Clear();

        foreach (D3D12Pipeline pipeline in this._bufferPipelines.Values) {
            pipeline.Dispose();
        }

        this._bufferPipelines.Clear();
        this._bufferLayout?.Dispose();
        this._bufferSampler?.Dispose();
    }

    /// <summary>
    /// Binds the internal mipmap resource set without changing public command-list validation state.
    /// </summary>
    /// <param name="set">The resource set used by the next mipmap dispatch.</param>
    private void SetMipmapResourceSet(ResourceSet set) {
        uint unusedOffset = 0;
        this._commandList.ComputeResourceSets.TrySet(0, set, 0, ref unusedOffset);
    }

    /// <summary>
    /// Owns a texture's reusable mipmap output buffer and per-mip source bindings.
    /// </summary>
    private sealed class BufferMipResources : IDisposable {
        
        /// <summary>
        /// Stores the raw UAV buffer containing the encoded destination mip.
        /// </summary>
        internal readonly D3D12DeviceBuffer Buffer;

        /// <summary>
        /// Stores one source view for each generated mip, including every array layer or cube face.
        /// </summary>
        internal readonly D3D12TextureView[] Views;

        /// <summary>
        /// Stores the source texture, output buffer, and sampler bindings for each generated mip.
        /// </summary>
        internal readonly ResourceSet[] Sets;

        /// <summary>
        /// Initializes storage for a texture's buffer-based mipmap resources.
        /// </summary>
        /// <param name="buffer">The output buffer reused across generated mip levels.</param>
        /// <param name="mipCount">The number of destination mip levels.</param>
        internal BufferMipResources(D3D12DeviceBuffer buffer, uint mipCount) {
            this.Buffer = buffer;
            this.Views = new D3D12TextureView[mipCount];
            this.Sets = new ResourceSet[mipCount];
        }

        /// <summary>
        /// Releases the per-mip bindings, source views, and output buffer.
        /// </summary>
        public void Dispose() {
            foreach (ResourceSet set in this.Sets) {
                set?.Dispose();
            }

            foreach (D3D12TextureView view in this.Views) {
                view?.Dispose();
            }

            this.Buffer.Dispose();
        }
    }
}