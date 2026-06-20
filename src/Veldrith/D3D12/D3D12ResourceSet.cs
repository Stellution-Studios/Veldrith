using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12ResourceSet.
/// </summary>
internal sealed class D3D12ResourceSet : ResourceSet {

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ResourceSet" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this resource set.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12ResourceSet(D3D12GraphicsDevice gd, ref ResourceSetDescription description) : base(ref description) {
        this.ResourceLayoutInfo = Util.AssertSubtype<ResourceLayout, D3D12ResourceLayout>(description.Layout);
        this.BoundResources = Util.ShallowClone(description.BoundResources);
        this.ElementCaches = CreateElementCaches(gd, this.ResourceLayoutInfo.Elements, this.BoundResources);
        this.ReferencedBuffers = CollectReferencedBuffers(this.ElementCaches);
        this.SingleReferencedBuffer = this.ReferencedBuffers.Length == 1 ? this.ReferencedBuffers[0] : null;
        gd.DescriptorHeapState.PrepopulateDescriptorTables(this);
    }

    /// <summary>
    /// Gets or sets ResourceLayoutInfo.
    /// </summary>
    internal D3D12ResourceLayout ResourceLayoutInfo { get; }

    /// <summary>
    /// Gets or sets BoundResources.
    /// </summary>
    internal IBindableResource[] BoundResources { get; }

    /// <summary>
    /// Gets pre-resolved D3D12 resources and descriptors for each ResourceSet element.
    /// </summary>
    internal D3D12ResourceSetElementCache[] ElementCaches { get; }

    /// <summary>
    /// Gets buffers referenced by this resource set for fast dynamic snapshot dirty checks.
    /// </summary>
    internal D3D12DeviceBuffer[] ReferencedBuffers { get; }

    /// <summary>
    /// Gets the only buffer referenced by this resource set, or null when the set references zero or multiple buffers.
    /// </summary>
    internal D3D12DeviceBuffer SingleReferencedBuffer { get; }

    /// <summary>
    /// Cached GPU descriptor table handle for the SRV/UAV descriptor table. Valid when the heap cache id and <see cref="CachedSrvUavSignature"/> match.
    /// </summary>
    internal GpuDescriptorHandle CachedSrvUavHandle;

    /// <summary>
    /// Shader-visible descriptor heap cache id that owns <see cref="CachedSrvUavHandle"/>.
    /// </summary>
    internal uint CachedSrvUavHeapId;

    /// <summary>
    /// Signature value that was active when <see cref="CachedSrvUavHandle"/> was populated.
    /// </summary>
    internal uint CachedSrvUavSignature;

    /// <summary>
    /// Whether <see cref="CachedSrvUavHandle"/> holds a valid cached value.
    /// </summary>
    internal bool HasCachedSrvUavHandle;

    /// <summary>
    /// Cached GPU descriptor table handle for the sampler descriptor table. Valid when the heap cache id and <see cref="CachedSamplerSignature"/> match.
    /// </summary>
    internal GpuDescriptorHandle CachedSamplerHandle;

    /// <summary>
    /// Shader-visible descriptor heap cache id that owns <see cref="CachedSamplerHandle"/>.
    /// </summary>
    internal uint CachedSamplerHeapId;

    /// <summary>
    /// Signature value that was active when <see cref="CachedSamplerHandle"/> was populated.
    /// </summary>
    internal uint CachedSamplerSignature;

    /// <summary>
    /// Whether <see cref="CachedSamplerHandle"/> holds a valid cached value.
    /// </summary>
    internal bool HasCachedSamplerHandle;

    /// <summary>
    /// Caches graphics SRV/UAV descriptor-table texture transition state.
    /// </summary>
    internal D3D12DescriptorTableTransitionCache GraphicsSrvUavTransitionCache;

    /// <summary>
    /// Caches compute SRV/UAV descriptor-table texture transition state.
    /// </summary>
    internal D3D12DescriptorTableTransitionCache ComputeSrvUavTransitionCache;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this._disposed = true;
    }

    /// <summary>
    /// Attempts to get a cached shader-visible descriptor table handle for this resource set.
    /// </summary>
    /// <param name="tableInfo">The descriptor table metadata required by the active root signature.</param>
    /// <param name="heapCacheId">The descriptor heap cache id that must own the handle.</param>
    /// <param name="handle">The cached GPU descriptor handle, when available.</param>
    /// <returns><see langword="true" /> when a cached handle was found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetCachedDescriptorTableHandle(D3D12DescriptorTableBindingInfo tableInfo, uint heapCacheId, out GpuDescriptorHandle handle) {
        if (tableInfo.TableKind == D3D12Pipeline.DescriptorTableKind.Sampler) {
            if (this.HasCachedSamplerHandle
                && this.CachedSamplerHeapId == heapCacheId
                && this.CachedSamplerSignature == tableInfo.Signature) {
                handle = this.CachedSamplerHandle;
                return true;
            }
        }
        else {
            if (this.HasCachedSrvUavHandle
                && this.CachedSrvUavHeapId == heapCacheId
                && this.CachedSrvUavSignature == tableInfo.Signature) {
                handle = this.CachedSrvUavHandle;
                return true;
            }
        }

        handle = default;
        return false;
    }

    /// <summary>
    /// Pre-resolves D3D12 resources and persistent CPU descriptors for ResourceSet hot-path binding.
    /// </summary>
    /// <param name="gd">The graphics device used to resolve full texture views.</param>
    /// <param name="elements">The resource layout elements.</param>
    /// <param name="resources">The bound resources.</param>
    /// <returns>The per-element D3D12 cache entries.</returns>
    private static D3D12ResourceSetElementCache[] CreateElementCaches(D3D12GraphicsDevice gd, ResourceLayoutElementDescription[] elements, IBindableResource[] resources) {
        D3D12ResourceSetElementCache[] caches = new D3D12ResourceSetElementCache[resources.Length];
        int count = Math.Min(elements.Length, resources.Length);
        for (int i = 0; i < count; i++) {
            IBindableResource resource = resources[i];
            if (resource == null) {
                continue;
            }

            switch (elements[i].Kind) {
                case ResourceKind.UniformBuffer:
                case ResourceKind.StructuredBufferReadOnly:
                case ResourceKind.StructuredBufferReadWrite: {
                        D3D12DeviceBuffer buffer;
                        uint bufferOffset;
                        if (resource is DeviceBufferRange range) {
                            buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(range.Buffer);
                            bufferOffset = range.Offset;
                        }
                        else {
                            buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>((DeviceBuffer)resource);
                            bufferOffset = 0;
                        }

                        caches[i] = new D3D12ResourceSetElementCache(buffer, bufferOffset, null, null, default, default, default);
                        break;
                    }
                case ResourceKind.TextureReadOnly: {
                        TextureView textureView = Util.GetTextureView(gd, resource);
                        D3D12TextureView d3D12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                        d3D12TextureView.EnsureBindingSupport(TextureUsage.Sampled, "sampled");
                        CpuDescriptorHandle descriptor = d3D12TextureView.GetOrCreateShaderResourceViewDescriptor();
                        caches[i] = new D3D12ResourceSetElementCache(null, 0, d3D12TextureView, null, descriptor, default, default);
                        break;
                    }
                case ResourceKind.TextureReadWrite: {
                        TextureView textureView = Util.GetTextureView(gd, resource);
                        D3D12TextureView d3D12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                        d3D12TextureView.EnsureBindingSupport(TextureUsage.Storage, "storage");
                        CpuDescriptorHandle descriptor = d3D12TextureView.GetOrCreateUnorderedAccessViewDescriptor();
                        caches[i] = new D3D12ResourceSetElementCache(null, 0, d3D12TextureView, null, default, descriptor, default);
                        break;
                    }
                case ResourceKind.Sampler: {
                        D3D12Sampler d3D12Sampler = Util.AssertSubtype<IBindableResource, D3D12Sampler>(resource);
                        CpuDescriptorHandle descriptor = d3D12Sampler.GetOrCreateDescriptor();
                        caches[i] = new D3D12ResourceSetElementCache(null, 0, null, d3D12Sampler, default, default, descriptor);
                        break;
                    }
            }
        }

        return caches;
    }

    /// <summary>
    /// Collects unique D3D12 buffers referenced by a resource set.
    /// </summary>
    /// <param name="elementCaches">The resource caches to inspect.</param>
    /// <returns>The referenced D3D12 buffers.</returns>
    private static D3D12DeviceBuffer[] CollectReferencedBuffers(D3D12ResourceSetElementCache[] elementCaches) {
        List<D3D12DeviceBuffer> buffers = null;
        for (int i = 0; i < elementCaches.Length; i++) {
            D3D12DeviceBuffer d3d12Buffer = elementCaches[i].Buffer;
            if (d3d12Buffer == null) {
                continue;
            }

            buffers ??= new List<D3D12DeviceBuffer>(1);
            bool alreadyAdded = false;
            for (int j = 0; j < buffers.Count; j++) {
                if (ReferenceEquals(buffers[j], d3d12Buffer)) {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded) {
                buffers.Add(d3d12Buffer);
            }
        }

        return buffers?.ToArray() ?? Array.Empty<D3D12DeviceBuffer>();
    }
}
