using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12TextureView.
/// </summary>
internal sealed class D3D12TextureView : TextureView {

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Tracks whether sampled binding support has been queried for this view.
    /// </summary>
    private bool _sampledBindingSupportChecked;

    /// <summary>
    /// Stores the cached sampled binding support result.
    /// </summary>
    private bool _sampledBindingSupported;

    /// <summary>
    /// Tracks whether storage binding support has been queried for this view.
    /// </summary>
    private bool _storageBindingSupportChecked;

    /// <summary>
    /// Stores the cached storage binding support result.
    /// </summary>
    private bool _storageBindingSupported;

    /// <summary>
    /// Stores the texture state version associated with the cached transition state.
    /// </summary>
    private ulong _cachedTransitionStateVersion;

    /// <summary>
    /// Stores the resource state known to cover this full texture view at the cached version.
    /// </summary>
    private ResourceStates _cachedTransitionState;

    /// <summary>
    /// Tracks whether the cached transition state is valid.
    /// </summary>
    private bool _hasCachedTransitionState;

    /// <summary>
    /// Stores the srv descriptor allocation used by this instance.
    /// </summary>
    private D3D12CpuDescriptorAllocation _srvDescriptor;

    /// <summary>
    /// Stores the uav descriptor allocation used by this instance.
    /// </summary>
    private D3D12CpuDescriptorAllocation _uavDescriptor;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12TextureView" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12TextureView(D3D12GraphicsDevice gd, ref TextureViewDescription description) : base(ref description) {
        this.gd = gd;
        this.TargetTexture = Util.AssertSubtype<Texture, D3D12Texture>(description.Target);
    }

    /// <summary>
    /// Gets or sets TargetTexture.
    /// </summary>
    internal D3D12Texture TargetTexture { get; }

    /// <summary>
    /// Checks whether this view is already known to be fully in the requested state.
    /// </summary>
    /// <param name="state">The required D3D12 resource state.</param>
    /// <returns><see langword="true" /> when no subresource scan is required.</returns>
    internal bool IsKnownInState(ResourceStates state) {
        return this._hasCachedTransitionState
               && this._cachedTransitionState == state
               && this._cachedTransitionStateVersion == this.TargetTexture.StateVersion;
    }

    /// <summary>
    /// Marks this view as fully transitioned to the requested state.
    /// </summary>
    /// <param name="state">The D3D12 resource state covering this view.</param>
    internal void MarkKnownState(ResourceStates state) {
        this._cachedTransitionState = state;
        this._cachedTransitionStateVersion = this.TargetTexture.StateVersion;
        this._hasCachedTransitionState = true;
    }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Gets the or create shader resource view descriptor value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal CpuDescriptorHandle GetOrCreateShaderResourceViewDescriptor() {
        if (this._srvDescriptor == null) {
            this._srvDescriptor = this.gd.SrvUavDescriptorAllocator.Allocate(1);
            ID3D12Resource nativeTexture = this.TargetTexture.NativeTexture
                                           ?? throw new PlatformNotSupportedException("Texture has no native D3D12 resource.");
            ShaderResourceViewDescription srvDescription = this.GetShaderResourceViewDescription();
            this.gd.Device.CreateShaderResourceView(nativeTexture, srvDescription, this._srvDescriptor.Handle);
        }

        return this._srvDescriptor.Handle;
    }

    /// <summary>
    /// Gets the or create unordered access view descriptor value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal CpuDescriptorHandle GetOrCreateUnorderedAccessViewDescriptor() {
        if (this._uavDescriptor == null) {
            this._uavDescriptor = this.gd.SrvUavDescriptorAllocator.Allocate(1);
            ID3D12Resource nativeTexture = this.TargetTexture.NativeTexture
                                           ?? throw new PlatformNotSupportedException("Texture has no native D3D12 resource.");
            UnorderedAccessViewDescription uavDescription = this.GetUnorderedAccessViewDescription();
            this.gd.Device.CreateUnorderedAccessView(nativeTexture, null, uavDescription, this._uavDescriptor.Handle);
        }

        return this._uavDescriptor.Handle;
    }

    /// <summary>
    /// Validates and caches whether this texture view can be bound with the requested usage.
    /// </summary>
    /// <param name="requestedUsage">The binding usage requested by the pipeline.</param>
    /// <param name="bindingKind">A diagnostic name used in error messages.</param>
    internal void EnsureBindingSupport(TextureUsage requestedUsage, string bindingKind) {
        ref bool checkedFlag = ref this._sampledBindingSupportChecked;
        ref bool supportedFlag = ref this._sampledBindingSupported;
        if ((requestedUsage & TextureUsage.Storage) != 0) {
            checkedFlag = ref this._storageBindingSupportChecked;
            supportedFlag = ref this._storageBindingSupported;
        }

        if (!checkedFlag) {
            TextureUsage usage = requestedUsage;
            if ((requestedUsage & TextureUsage.Sampled) != 0) {
                if ((this.TargetTexture.Usage & TextureUsage.Cubemap) != 0) {
                    usage |= TextureUsage.Cubemap;
                }

                if ((this.TargetTexture.Usage & TextureUsage.DepthStencil) != 0) {
                    usage |= TextureUsage.DepthStencil;
                }
            }

            supportedFlag = this.gd.GetPixelFormatSupport(this.Format, this.TargetTexture.Type, usage);
            checkedFlag = true;
        }

        if (!supportedFlag) {
            throw new PlatformNotSupportedException($"D3D12 {bindingKind} texture view binding is not supported for format {this.Format}, type {this.TargetTexture.Type}, usage {requestedUsage}.");
        }
    }

    /// <summary>
    /// Gets the shader resource view description value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal ShaderResourceViewDescription GetShaderResourceViewDescription() {
        ShaderResourceViewDescription description = new() {
            Format = D3D12Formats.GetViewFormat(D3D12Formats.ToDxgiFormat(this.Format)),
            Shader4ComponentMapping = ShaderComponentMapping.Default
        };

        if (this.TargetTexture.Type == TextureType.Texture3D) {
            description.ViewDimension = ShaderResourceViewDimension.Texture3D;
            description.Texture3D = new Texture3DShaderResourceView {
                MostDetailedMip = this.BaseMipLevel,
                MipLevels = this.MipLevels,
                ResourceMinLODClamp = 0f
            };
            return description;
        }

        bool isMultisampled = this.TargetTexture.SampleCount != TextureSampleCount.Count1;
        bool isCube = (this.TargetTexture.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap;

        if (isCube) {
            if (this.ArrayLayers > 1) {
                description.ViewDimension = ShaderResourceViewDimension.TextureCubeArray;
                description.TextureCubeArray = new TextureCubeArrayShaderResourceView {
                    MostDetailedMip = this.BaseMipLevel,
                    MipLevels = this.MipLevels,
                    First2DArrayFace = this.BaseArrayLayer * 6,
                    NumCubes = this.ArrayLayers,
                    ResourceMinLODClamp = 0f
                };
            }
            else {
                description.ViewDimension = ShaderResourceViewDimension.TextureCube;
                description.TextureCube = new TextureCubeShaderResourceView {
                    MostDetailedMip = this.BaseMipLevel,
                    MipLevels = this.MipLevels,
                    ResourceMinLODClamp = 0f
                };
            }

            return description;
        }

        if (this.ArrayLayers > 1) {
            description.ViewDimension = isMultisampled
                ? ShaderResourceViewDimension.Texture2DMultisampledArray
                : ShaderResourceViewDimension.Texture2DArray;
            if (isMultisampled) {
                description.Texture2DMSArray = new Texture2DMultisampledArrayShaderResourceView {
                    FirstArraySlice = this.BaseArrayLayer,
                    ArraySize = this.ArrayLayers
                };
            }
            else {
                description.Texture2DArray = new Texture2DArrayShaderResourceView {
                    MostDetailedMip = this.BaseMipLevel,
                    MipLevels = this.MipLevels,
                    FirstArraySlice = this.BaseArrayLayer,
                    ArraySize = this.ArrayLayers,
                    PlaneSlice = 0,
                    ResourceMinLODClamp = 0f
                };
            }
        }
        else {
            description.ViewDimension = isMultisampled
                ? ShaderResourceViewDimension.Texture2DMultisampled
                : ShaderResourceViewDimension.Texture2D;
            if (isMultisampled) {
                description.Texture2DMS = new Texture2DMultisampledShaderResourceView();
            }
            else {
                description.Texture2D = new Texture2DShaderResourceView {
                    MostDetailedMip = this.BaseMipLevel,
                    MipLevels = this.MipLevels,
                    PlaneSlice = 0,
                    ResourceMinLODClamp = 0f
                };
            }
        }

        return description;
    }

    /// <summary>
    /// Gets the unordered access view description value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal UnorderedAccessViewDescription GetUnorderedAccessViewDescription() {
        if (this.TargetTexture.SampleCount != TextureSampleCount.Count1) {
            throw new PlatformNotSupportedException("Multisampled UAV textures are not supported.");
        }

        UnorderedAccessViewDescription description = new() {
            Format = D3D12Formats.GetViewFormat(D3D12Formats.ToDxgiFormat(this.Format))
        };

        if (this.TargetTexture.Type == TextureType.Texture3D) {
            description.ViewDimension = UnorderedAccessViewDimension.Texture3D;
            description.Texture3D = new Texture3DUnorderedAccessView {
                MipSlice = this.BaseMipLevel,
                FirstWSlice = 0,
                WSize = uint.MaxValue
            };
            return description;
        }

        if (this.ArrayLayers > 1) {
            description.ViewDimension = UnorderedAccessViewDimension.Texture2DArray;
            description.Texture2DArray = new Texture2DArrayUnorderedAccessView {
                MipSlice = this.BaseMipLevel,
                FirstArraySlice = this.BaseArrayLayer,
                ArraySize = this.ArrayLayers,
                PlaneSlice = 0
            };
        }
        else {
            description.ViewDimension = UnorderedAccessViewDimension.Texture2D;
            description.Texture2D = new Texture2DUnorderedAccessView {
                MipSlice = this.BaseMipLevel,
                PlaneSlice = 0
            };
        }

        return description;
    }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this.gd.ReleaseAfterLastSubmission(this._srvDescriptor);
        this.gd.ReleaseAfterLastSubmission(this._uavDescriptor);
        this._disposed = true;
    }
}
