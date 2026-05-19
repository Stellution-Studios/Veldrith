using System;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

internal class MtlTexture : Texture {
    private bool _disposed;

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
        this.MtlTextureType = MtlFormats.VdToMtlTextureType(this.Type, this.ArrayLayers,
            this.SampleCount != TextureSampleCount.Count1,
            (this.Usage & TextureUsage.Cubemap) != 0);

        if (this.Usage != TextureUsage.Staging) {
            this.MtlStorageMode = isDepth && gd.PreferMemorylessDepthTargets
                ? MTLStorageMode.Memoryless
                : MTLStorageMode.Private;

            MTLTextureDescriptor texDescriptor = MTLTextureDescriptor.New();
            texDescriptor.width = this.Width;
            texDescriptor.height = this.Height;
            texDescriptor.depth = this.Depth;
            texDescriptor.mipmapLevelCount = this.MipLevels;
            texDescriptor.arrayLength = this.ArrayLayers;
            texDescriptor.sampleCount = FormatHelpers.GetSampleCountUInt32(this.SampleCount);
            texDescriptor.textureType = this.MtlTextureType;
            texDescriptor.pixelFormat = this.MtlPixelFormat;
            texDescriptor.textureUsage = MtlFormats.VdToMtlTextureUsage(this.Usage);
            texDescriptor.storageMode = this.MtlStorageMode;

            this.DeviceTexture = gd.Device.newTextureWithDescriptor(texDescriptor);
            ObjectiveCRuntime.release(texDescriptor.NativePtr);
        }
        else {
            uint totalStorageSize = 0;

            for (uint level = 0; level < this.MipLevels; level++) {
                Util.GetMipDimensions(this, level, out uint levelWidth, out uint levelHeight, out uint levelDepth);
                totalStorageSize += levelDepth * FormatHelpers.GetDepthPitch(
                    FormatHelpers.GetRowPitch(levelWidth, this.Format),
                    levelHeight, this.Format);
            }

            totalStorageSize *= this.ArrayLayers;

            this.StagingBuffer = gd.Device.newBufferWithLengthOptions(
                totalStorageSize,
                MTLResourceOptions.StorageModeShared);

            unsafe {
                this.StagingBufferPointer = this.StagingBuffer.contents();
            }
        }
    }

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
        this.MtlTextureType = MtlFormats.VdToMtlTextureType(this.Type, this.ArrayLayers,
            this.SampleCount != TextureSampleCount.Count1,
            (this.Usage & TextureUsage.Cubemap) != 0);
    }

    protected MtlTexture() { }

    /// <summary>
    ///     The native MTLTexture object. This property is only valid for non-staging Textures.
    /// </summary>
    public virtual MTLTexture DeviceTexture { get; }

    /// <summary>
    ///     The staging MTLBuffer object. This property is only valid for staging Textures.
    /// </summary>
    public MTLBuffer StagingBuffer { get; }

    public override PixelFormat Format { get; }

    public override uint Width { get; }

    public override uint Height { get; }

    public override uint Depth { get; }

    public override uint MipLevels { get; }

    public override uint ArrayLayers { get; }

    public override TextureUsage Usage { get; }

    public override TextureType Type { get; }

    public override TextureSampleCount SampleCount { get; }
    public override bool IsDisposed => this._disposed;
    public virtual MTLPixelFormat MtlPixelFormat { get; }
    public virtual MTLTextureType MtlTextureType { get; }
    public MTLStorageMode MtlStorageMode { get; }

    public unsafe void* StagingBufferPointer { get; private set; }
    public override string Name { get; set; }

    internal uint GetSubresourceSize(uint mipLevel, uint arrayLayer) {
        uint blockSize = FormatHelpers.IsCompressedFormat(this.Format) ? 4u : 1u;
        Util.GetMipDimensions(this, mipLevel, out uint width, out uint height, out uint depth);
        uint storageWidth = Math.Max(blockSize, width);
        uint storageHeight = Math.Max(blockSize, height);
        return depth * FormatHelpers.GetDepthPitch(
            FormatHelpers.GetRowPitch(storageWidth, this.Format),
            storageHeight, this.Format);
    }

    internal void GetSubresourceLayout(uint mipLevel, uint arrayLayer, out uint rowPitch, out uint depthPitch) {
        uint blockSize = FormatHelpers.IsCompressedFormat(this.Format) ? 4u : 1u;
        Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint _);
        uint storageWidth = Math.Max(blockSize, mipWidth);
        uint storageHeight = Math.Max(blockSize, mipHeight);
        rowPitch = FormatHelpers.GetRowPitch(storageWidth, this.Format);
        depthPitch = FormatHelpers.GetDepthPitch(rowPitch, storageHeight, this.Format);
    }

    private protected override void DisposeCore() {
        if (!this._disposed) {
            this._disposed = true;

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (this.StagingBuffer.IsNull) {
                ObjectiveCRuntime.release(this.DeviceTexture.NativePtr);
            }
            else {
                ObjectiveCRuntime.release(this.StagingBuffer.NativePtr);
            }
        }
    }
}