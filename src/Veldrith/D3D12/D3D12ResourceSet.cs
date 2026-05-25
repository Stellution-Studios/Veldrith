using System;

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
}
