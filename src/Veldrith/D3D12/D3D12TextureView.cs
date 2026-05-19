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
    /// Stores the srv descriptor heap state used by this instance.
    /// </summary>
    private ID3D12DescriptorHeap _srvDescriptorHeap;

    /// <summary>
    /// Stores the uav descriptor heap state used by this instance.
    /// </summary>
    private ID3D12DescriptorHeap _uavDescriptorHeap;

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
        if (this._srvDescriptorHeap == null) {
            this._srvDescriptorHeap = this.gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, 1));
            ID3D12Resource nativeTexture = this.TargetTexture.NativeTexture
                                           ?? throw new PlatformNotSupportedException("Texture has no native D3D12 resource.");
            ShaderResourceViewDescription srvDescription = this.GetShaderResourceViewDescription();
            this.gd.Device.CreateShaderResourceView(nativeTexture, srvDescription, this._srvDescriptorHeap.GetCPUDescriptorHandleForHeapStart());
        }

        return this._srvDescriptorHeap.GetCPUDescriptorHandleForHeapStart();
    }

    /// <summary>
    /// Gets the or create unordered access view descriptor value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    internal CpuDescriptorHandle GetOrCreateUnorderedAccessViewDescriptor() {
        if (this._uavDescriptorHeap == null) {
            this._uavDescriptorHeap = this.gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, 1));
            ID3D12Resource nativeTexture = this.TargetTexture.NativeTexture
                                           ?? throw new PlatformNotSupportedException("Texture has no native D3D12 resource.");
            UnorderedAccessViewDescription uavDescription = this.GetUnorderedAccessViewDescription();
            this.gd.Device.CreateUnorderedAccessView(nativeTexture, null, uavDescription, this._uavDescriptorHeap.GetCPUDescriptorHandleForHeapStart());
        }

        return this._uavDescriptorHeap.GetCPUDescriptorHandleForHeapStart();
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
        this._srvDescriptorHeap?.Dispose();
        this._uavDescriptorHeap?.Dispose();
        this._disposed = true;
    }
}