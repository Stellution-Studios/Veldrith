using System;
using System.Collections.Generic;
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
        this.ReferencedBuffers = CollectReferencedBuffers(this.BoundResources);
        this.PrepareDescriptors(gd);
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
    /// Gets buffers referenced by this resource set for fast dynamic snapshot dirty checks.
    /// </summary>
    internal D3D12DeviceBuffer[] ReferencedBuffers { get; }

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
    /// Creates persistent CPU descriptors when the resource set is built so first draw binding stays cheap.
    /// </summary>
    /// <param name="gd">The graphics device used to resolve full texture views.</param>
    private void PrepareDescriptors(D3D12GraphicsDevice gd) {
        ResourceLayoutElementDescription[] elements = this.ResourceLayoutInfo.Elements;
        int count = Math.Min(elements.Length, this.BoundResources.Length);
        for (int i = 0; i < count; i++) {
            IBindableResource resource = this.BoundResources[i];
            if (resource == null) {
                continue;
            }

            switch (elements[i].Kind) {
                case ResourceKind.TextureReadOnly: {
                        TextureView textureView = Util.GetTextureView(gd, resource);
                        D3D12TextureView d3D12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                        d3D12TextureView.EnsureBindingSupport(TextureUsage.Sampled, "sampled");
                        d3D12TextureView.GetOrCreateShaderResourceViewDescriptor();
                        break;
                    }
                case ResourceKind.TextureReadWrite: {
                        TextureView textureView = Util.GetTextureView(gd, resource);
                        D3D12TextureView d3D12TextureView = Util.AssertSubtype<TextureView, D3D12TextureView>(textureView);
                        d3D12TextureView.EnsureBindingSupport(TextureUsage.Storage, "storage");
                        d3D12TextureView.GetOrCreateUnorderedAccessViewDescriptor();
                        break;
                    }
                case ResourceKind.Sampler: {
                        D3D12Sampler d3D12Sampler = Util.AssertSubtype<IBindableResource, D3D12Sampler>(resource);
                        d3D12Sampler.GetOrCreateDescriptor();
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// Collects unique D3D12 buffers referenced by a resource set.
    /// </summary>
    /// <param name="resources">The resources to inspect.</param>
    /// <returns>The referenced D3D12 buffers.</returns>
    private static D3D12DeviceBuffer[] CollectReferencedBuffers(IBindableResource[] resources) {
        List<D3D12DeviceBuffer> buffers = null;
        for (int i = 0; i < resources.Length; i++) {
            if (!Util.GetDeviceBuffer(resources[i], out DeviceBuffer buffer)) {
                continue;
            }

            D3D12DeviceBuffer d3d12Buffer = Util.AssertSubtype<DeviceBuffer, D3D12DeviceBuffer>(buffer);
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
