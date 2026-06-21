using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlTexture.
/// </summary>
internal class MtlTexture : Texture {

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlTexture" /> type.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="gd">The graphics device that owns this operation.</param>
    public MtlTexture(ref TextureDescription description, MtlGraphicsDevice gd) {
        this.Width = description.Width;
        this.Height = description.Height;
        this.Depth = description.Depth;
        this.ArrayLayers = description.ArrayLayers;
        this.MipLevels = description.MipLevels;
        this.Format = description.Format;
        this.Usage = description.Usage;
        this.Type = description.Type;
        this.SampleCount = description.SampleCount;
        bool isDepth = (this.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil;

        this.MtlPixelFormat = MtlFormats.VdToMtlPixelFormat(this.Format, isDepth);
        this.MtlTextureType = MtlFormats.VdToMtlTextureType(this.Type, this.ArrayLayers, this.SampleCount != TextureSampleCount.Count1, (this.Usage & TextureUsage.Cubemap) != 0);

        if (this.Usage != TextureUsage.Staging) {
            this.MtlStorageMode = isDepth && gd.PreferMemorylessDepthTargets
                ? MTLStorageMode.Memoryless
                : MTLStorageMode.Private;

            MTLTextureDescriptor texDescriptor = MTLTextureDescriptor.New();
            texDescriptor.Width = this.Width;
            texDescriptor.Height = this.Height;
            texDescriptor.Depth = this.Depth;
            texDescriptor.MipmapLevelCount = this.MipLevels;
            texDescriptor.ArrayLength = this.ArrayLayers;
            texDescriptor.SampleCount = FormatHelpers.GetSampleCountUInt32(this.SampleCount);
            texDescriptor.TextureType = this.MtlTextureType;
            texDescriptor.PixelFormat = this.MtlPixelFormat;
            texDescriptor.TextureUsage = MtlFormats.VdToMtlTextureUsage(this.Usage);
            texDescriptor.StorageMode = this.MtlStorageMode;

            this.DeviceTexture = gd.Device.NewTextureWithDescriptor(texDescriptor);
            ObjectiveCRuntime.Release(texDescriptor.NativePtr);
        }
        else {
            uint totalStorageSize = 0;

            for (uint level = 0; level < this.MipLevels; level++) {
                Util.GetMipDimensions(this, level, out uint levelWidth, out uint levelHeight, out uint levelDepth);
                totalStorageSize += levelDepth * FormatHelpers.GetDepthPitch(FormatHelpers.GetRowPitch(levelWidth, this.Format), levelHeight, this.Format);
            }

            totalStorageSize *= this.ArrayLayers;

            this.StagingBuffer = gd.Device.NewBufferWithLengthOptions(totalStorageSize, MTLResourceOptions.StorageModeShared);

            unsafe {
                this.StagingBufferPointer = this.StagingBuffer.Contents();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlTexture" /> type.
    /// </summary>
    /// <param name="nativeTexture">The native texture value used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public MtlTexture(ulong nativeTexture, ref TextureDescription description) {
        this.DeviceTexture = new MTLTexture((IntPtr)nativeTexture);
        this.Width = description.Width;
        this.Height = description.Height;
        this.Depth = description.Depth;
        this.ArrayLayers = description.ArrayLayers;
        this.MipLevels = description.MipLevels;
        this.Format = description.Format;
        this.Usage = description.Usage;
        this.Type = description.Type;
        this.SampleCount = description.SampleCount;
        bool isDepth = (this.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil;

        this.MtlPixelFormat = MtlFormats.VdToMtlPixelFormat(this.Format, isDepth);
        this.MtlTextureType = MtlFormats.VdToMtlTextureType(this.Type, this.ArrayLayers, this.SampleCount != TextureSampleCount.Count1, (this.Usage & TextureUsage.Cubemap) != 0);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlTexture" /> type.
    /// </summary>
    protected MtlTexture() { }

    /// <summary>
    /// The native MTLTexture object. This property is only valid for non-staging Textures.
    /// </summary>
    public virtual MTLTexture DeviceTexture { get; }

    /// <summary>
    /// The staging MTLBuffer object. This property is only valid for staging Textures.
    /// </summary>
    public MTLBuffer StagingBuffer { get; }

    /// <summary>
    /// Gets or sets Format.
    /// </summary>
    public override PixelFormat Format { get; }

    /// <summary>
    /// Gets or sets Width.
    /// </summary>
    public override uint Width { get; }

    /// <summary>
    /// Gets or sets Height.
    /// </summary>
    public override uint Height { get; }

    /// <summary>
    /// Gets or sets Depth.
    /// </summary>
    public override uint Depth { get; }

    /// <summary>
    /// Gets or sets MipLevels.
    /// </summary>
    public override uint MipLevels { get; }

    /// <summary>
    /// Gets or sets ArrayLayers.
    /// </summary>
    public override uint ArrayLayers { get; }

    /// <summary>
    /// Gets or sets Usage.
    /// </summary>
    public override TextureUsage Usage { get; }

    /// <summary>
    /// Gets or sets Type.
    /// </summary>
    public override TextureType Type { get; }

    /// <summary>
    /// Gets or sets SampleCount.
    /// </summary>
    public override TextureSampleCount SampleCount { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets MtlPixelFormat.
    /// </summary>
    public virtual MTLPixelFormat MtlPixelFormat { get; }

    /// <summary>
    /// Gets or sets MtlTextureType.
    /// </summary>
    public virtual MTLTextureType MtlTextureType { get; }

    /// <summary>
    /// Gets or sets MtlStorageMode.
    /// </summary>
    public MTLStorageMode MtlStorageMode { get; }

    /// <summary>
    /// Gets or sets StagingBufferPointer.
    /// </summary>
    public unsafe void* StagingBufferPointer { get; private set; }

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Gets the subresource size value.
    /// </summary>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    /// <returns>The value produced by this operation.</returns>
    internal uint GetSubresourceSize(uint mipLevel, uint arrayLayer) {
        uint blockSize = FormatHelpers.IsCompressedFormat(this.Format) ? 4u : 1u;
        Util.GetMipDimensions(this, mipLevel, out uint width, out uint height, out uint depth);
        uint storageWidth = Math.Max(blockSize, width);
        uint storageHeight = Math.Max(blockSize, height);
        return depth * FormatHelpers.GetDepthPitch(FormatHelpers.GetRowPitch(storageWidth, this.Format), storageHeight, this.Format);
    }

    /// <summary>
    /// Gets the subresource layout value.
    /// </summary>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    /// <param name="rowPitch">The row pitch value used by this operation.</param>
    /// <param name="depthPitch">The depth pitch value used by this operation.</param>
    internal void GetSubresourceLayout(uint mipLevel, uint arrayLayer, out uint rowPitch, out uint depthPitch) {
        uint blockSize = FormatHelpers.IsCompressedFormat(this.Format) ? 4u : 1u;
        Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint _);
        uint storageWidth = Math.Max(blockSize, mipWidth);
        uint storageHeight = Math.Max(blockSize, mipHeight);
        rowPitch = FormatHelpers.GetRowPitch(storageWidth, this.Format);
        depthPitch = FormatHelpers.GetDepthPitch(rowPitch, storageHeight, this.Format);
    }

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private protected override void DisposeCore() {
        if (!this._disposed) {
            this._disposed = true;

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (this.StagingBuffer.IsNull) {
                ObjectiveCRuntime.Release(this.DeviceTexture.NativePtr);
            }
            else {
                ObjectiveCRuntime.Release(this.StagingBuffer.NativePtr);
            }
        }
    }
}