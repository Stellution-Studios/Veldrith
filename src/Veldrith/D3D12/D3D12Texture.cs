using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Veldrith.D3D12;

/// <summary>
/// Defines the behavior and responsibilities of the D3D12Texture class.
/// </summary>
internal sealed class D3D12Texture : Texture {

    /// <summary>
    /// Stores the value associated with <c>_data</c>.
    /// </summary>
    private readonly byte[] _data;

    /// <summary>
    /// Stores the value associated with <c>_ownsNativeTexture</c>.
    /// </summary>
    private readonly bool _ownsNativeTexture;

    /// <summary>
    /// Stores the value associated with <c>_subresourceStates</c>.
    /// </summary>
    private readonly ResourceStates[] _subresourceStates;

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_activeMapMode</c>.
    /// </summary>
    private MapMode? _activeMapMode;

    /// <summary>
    /// Stores the value associated with <c>_activeMapSubresource</c>.
    /// </summary>
    private uint _activeMapSubresource;

    /// <summary>
    /// Stores the value associated with <c>_cachedCommonState</c>.
    /// </summary>
    private ResourceStates _cachedCommonState;

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the value associated with <c>_hasCachedCommonState</c>.
    /// </summary>
    private bool _hasCachedCommonState;

    /// <summary>
    /// Stores the value associated with <c>_mapped</c>.
    /// </summary>
    private bool _mapped;

    /// <summary>
    /// Stores the value associated with <c>_pinnedData</c>.
    /// </summary>
    private GCHandle _pinnedData;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Texture" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <param name="nativeHandle">Specifies the value of <paramref name="nativeHandle" />.</param>
    public D3D12Texture(D3D12GraphicsDevice gd, ref TextureDescription description, ulong? nativeHandle) {
        this.gd = gd;
        this.Width = description.Width;
        this.Height = description.Height;
        this.Depth = description.Depth;
        this.MipLevels = description.MipLevels;
        this.ArrayLayers = description.ArrayLayers;
        this.Format = description.Format;
        this.Usage = description.Usage;
        this.Type = description.Type;
        this.SampleCount = description.SampleCount;
        this.EffectiveArrayLayers = GetEffectiveArrayLayers(this.Usage, this.ArrayLayers);
        this._data = new byte[ComputeTotalSize(ref description)];
        this._subresourceStates = new ResourceStates[this.MipLevels * this.EffectiveArrayLayers];

        if (nativeHandle == null) {
            this.NativeTexture = this.CreateNativeTexture(gd, ref description);
            this._ownsNativeTexture = true;
            this.InitializeSubresourceStates(GetCreatedTextureInitialState(description.Usage));
        }
        else {
            this.NativeTexture = CreateWrappedNativeTexture(nativeHandle.Value);
            this._ownsNativeTexture = false;
            ValidateWrappedTextureDescription(this.NativeTexture.Description, ref description);
            this.InitializeSubresourceStates(ResourceStates.Common);
        }
    }

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
    /// Gets or sets NativeTexture.
    /// </summary>
    internal ID3D12Resource NativeTexture { get; }

    /// <summary>
    /// Gets or sets CurrentState.
    /// </summary>
    internal ResourceStates CurrentState {
        get {
            if (this._subresourceStates == null || this._subresourceStates.Length == 0) {
                return ResourceStates.Common;
            }

            return this._subresourceStates[0];
        }
        set => this.SetAllSubresourceStates(value);
    }

    /// <summary>
    /// Gets or sets SubresourceCount.
    /// </summary>
    internal uint SubresourceCount => (uint)(this._subresourceStates?.Length ?? 0);

    /// <summary>
    /// Gets or sets EffectiveArrayLayers.
    /// </summary>
    internal uint EffectiveArrayLayers { get; }

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Executes the Update operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="z">Specifies the value of <paramref name="z" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    internal void Update(IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
        uint subresource = this.CalculateSubresource(mipLevel, arrayLayer);
        Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);
        if (x + width > mipWidth || y + height > mipHeight || z + depth > mipDepth) {
            throw new VeldridException("Texture update region exceeds texture bounds.");
        }

        uint requiredSize = FormatHelpers.GetRegionSize(width, height, depth, this.Format);
        if (sizeInBytes < requiredSize) {
            throw new VeldridException("Texture update source size is smaller than required for the destination region.");
        }

        this.GetSubresourceLayout(subresource, out uint dstOffset, out uint dstSize, out uint dstRowPitch, out uint dstDepthPitch);
        if (dstOffset + dstSize > (uint)this._data.Length) {
            throw new VeldridException("Texture update destination region exceeds texture storage.");
        }

        uint srcRowPitch = FormatHelpers.GetRowPitch(width, this.Format);
        uint srcDepthPitch = FormatHelpers.GetDepthPitch(srcRowPitch, height, this.Format);
        unsafe {
            fixed (byte* dstBase = this._data) {
                Util.CopyTextureRegion(source.ToPointer(), 0, 0, 0, srcRowPitch, srcDepthPitch, dstBase + dstOffset, x, y, z, dstRowPitch, dstDepthPitch, width, height, depth, this.Format);
            }
        }
    }

    /// <summary>
    /// Executes the UpdateNativeSubresource operation.
    /// </summary>
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="sizeInBytes">Specifies the value of <paramref name="sizeInBytes" />.</param>
    /// <param name="x">Specifies the value of <paramref name="x" />.</param>
    /// <param name="y">Specifies the value of <paramref name="y" />.</param>
    /// <param name="z">Specifies the value of <paramref name="z" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    internal void UpdateNativeSubresource(IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
        this.Update(source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);

        if (this.NativeTexture == null) {
            return;
        }

        uint subresource = this.CalculateSubresource(mipLevel, arrayLayer);
        this.SyncSubresourceToNative(subresource);
    }

    /// <summary>
    /// Executes the Map operation.
    /// </summary>
    /// <param name="mode">Specifies the value of <paramref name="mode" />.</param>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    /// <returns>Returns the result produced by the Map operation.</returns>
    internal MappedResource Map(MapMode mode, uint subresource) {
        if (subresource >= this.SubresourceCount) {
            throw new VeldridException("Subresource index is out of bounds.");
        }

        if (this.IsStagingTexture() && this.NativeTexture != null && (mode == MapMode.Read || mode == MapMode.ReadWrite)) {
            this.SyncSubresourceFromNative(subresource);
        }

        if (!this._mapped) {
            this._pinnedData = GCHandle.Alloc(this._data, GCHandleType.Pinned);
            this._mapped = true;
        }

        this._activeMapMode = mode;
        this._activeMapSubresource = subresource;
        this.GetSubresourceLayout(subresource, out uint offset, out uint size, out uint rowPitch, out uint depthPitch);
        IntPtr dataPtr = IntPtr.Add(this._pinnedData.AddrOfPinnedObject(), (int)offset);
        return new MappedResource(this, mode, dataPtr, size, subresource, rowPitch, depthPitch);
    }

    /// <summary>
    /// Executes the Unmap operation.
    /// </summary>
    internal void Unmap() {
        if (this._mapped
            && this.IsStagingTexture()
            && this.NativeTexture != null
            && this._activeMapMode.HasValue
            && (this._activeMapMode.Value == MapMode.Write || this._activeMapMode.Value == MapMode.ReadWrite)) {
            this.SyncSubresourceToNative(this._activeMapSubresource);
        }

        this._activeMapMode = null;
        if (this._mapped) {
            this._pinnedData.Free();
            this._mapped = false;
        }
    }

    /// <summary>
    /// Executes the CopyRegionTo operation.
    /// </summary>
    /// <param name="destination">Specifies the value of <paramref name="destination" />.</param>
    /// <param name="srcX">Specifies the value of <paramref name="srcX" />.</param>
    /// <param name="srcY">Specifies the value of <paramref name="srcY" />.</param>
    /// <param name="srcZ">Specifies the value of <paramref name="srcZ" />.</param>
    /// <param name="srcMipLevel">Specifies the value of <paramref name="srcMipLevel" />.</param>
    /// <param name="srcBaseArrayLayer">Specifies the value of <paramref name="srcBaseArrayLayer" />.</param>
    /// <param name="dstX">Specifies the value of <paramref name="dstX" />.</param>
    /// <param name="dstY">Specifies the value of <paramref name="dstY" />.</param>
    /// <param name="dstZ">Specifies the value of <paramref name="dstZ" />.</param>
    /// <param name="dstMipLevel">Specifies the value of <paramref name="dstMipLevel" />.</param>
    /// <param name="dstBaseArrayLayer">Specifies the value of <paramref name="dstBaseArrayLayer" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="layerCount">Specifies the value of <paramref name="layerCount" />.</param>
    internal void CopyRegionTo(D3D12Texture destination, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount) {
        if (this.Format != destination.Format) {
            throw new VeldridException("Source and destination texture formats must match.");
        }

        for (uint layer = 0; layer < layerCount; layer++) {
            uint srcSubresource = (srcBaseArrayLayer + layer) * this.MipLevels + srcMipLevel;
            uint dstSubresource = (dstBaseArrayLayer + layer) * destination.MipLevels + dstMipLevel;
            this.GetSubresourceLayout(srcSubresource, out uint srcBaseOffset, out _, out uint srcRowPitch, out uint srcDepthPitch);
            destination.GetSubresourceLayout(dstSubresource, out uint dstBaseOffset, out _, out uint dstRowPitch, out uint dstDepthPitch);

            unsafe {
                fixed (byte* srcBase = this._data) {
                    fixed (byte* dstBase = destination._data) {
                        byte* srcSubresourcePtr = srcBase + srcBaseOffset;
                        byte* dstSubresourcePtr = dstBase + dstBaseOffset;
                        Util.CopyTextureRegion(srcSubresourcePtr, srcX, srcY, srcZ, srcRowPitch, srcDepthPitch, dstSubresourcePtr, dstX, dstY, dstZ, dstRowPitch, dstDepthPitch, width, height, depth, this.Format);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Executes the GenerateMipmapsCpu operation.
    /// </summary>
    /// <returns>Returns the result produced by the GenerateMipmapsCpu operation.</returns>
    internal bool GenerateMipmapsCpu() {
        if (this.MipLevels <= 1 || FormatHelpers.IsCompressedFormat(this.Format) || (this.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil) {
            return false;
        }

        uint bytesPerPixel = FormatSizeHelpers.GetSizeInBytes(this.Format);
        if (bytesPerPixel == 0 || bytesPerPixel > 16) {
            return false;
        }

        for (uint layer = 0; layer < this.EffectiveArrayLayers; layer++) {
            for (uint mipLevel = 1; mipLevel < this.MipLevels; mipLevel++) {
                uint srcSubresource = this.CalculateSubresource(mipLevel - 1, layer);
                uint dstSubresource = this.CalculateSubresource(mipLevel, layer);

                this.GetSubresourceLayout(srcSubresource, out uint srcBaseOffset, out _, out uint srcRowPitch, out uint srcDepthPitch);
                this.GetSubresourceLayout(dstSubresource, out uint dstBaseOffset, out _, out uint dstRowPitch, out uint dstDepthPitch);

                Util.GetMipDimensions(this, mipLevel - 1, out uint srcWidth, out uint srcHeight, out uint srcDepth);
                Util.GetMipDimensions(this, mipLevel, out uint dstWidth, out uint dstHeight, out uint dstDepth);

                for (uint z = 0; z < dstDepth; z++) {
                    uint srcZ0 = Math.Min(srcDepth - 1, z * 2);
                    uint srcZ1 = Math.Min(srcDepth - 1, srcZ0 + 1);

                    for (uint y = 0; y < dstHeight; y++) {
                        uint srcY0 = Math.Min(srcHeight - 1, y * 2);
                        uint srcY1 = Math.Min(srcHeight - 1, srcY0 + 1);

                        for (uint x = 0; x < dstWidth; x++) {
                            uint srcX0 = Math.Min(srcWidth - 1, x * 2);
                            uint srcX1 = Math.Min(srcWidth - 1, srcX0 + 1);

                            int dstPixelOffset = (int)(dstBaseOffset
                                + z * dstDepthPitch
                                + y * dstRowPitch
                                + x * bytesPerPixel);

                            for (uint component = 0; component < bytesPerPixel; component++) {
                                uint sum = 0;
                                uint sampleCount = 0;

                                sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY0, srcZ0, bytesPerPixel, component);
                                sampleCount++;
                                sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY0, srcZ0, bytesPerPixel, component);
                                sampleCount++;
                                sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY1, srcZ0, bytesPerPixel, component);
                                sampleCount++;
                                sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY1, srcZ0, bytesPerPixel, component);
                                sampleCount++;

                                if (srcDepth > 1) {
                                    sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY0, srcZ1, bytesPerPixel, component);
                                    sampleCount++;
                                    sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY0, srcZ1, bytesPerPixel, component);
                                    sampleCount++;
                                    sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY1, srcZ1, bytesPerPixel, component);
                                    sampleCount++;
                                    sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY1, srcZ1, bytesPerPixel, component);
                                    sampleCount++;
                                }

                                this._data[dstPixelOffset + component] = (byte)(sum / sampleCount);
                            }
                        }
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Executes the UploadGeneratedMipmaps operation.
    /// </summary>
    internal unsafe void UploadGeneratedMipmaps() {
        if (this.NativeTexture == null || this.MipLevels <= 1) {
            return;
        }

        fixed (byte* dataPtr = this._data) {
            for (uint layer = 0; layer < this.EffectiveArrayLayers; layer++) {
                for (uint mipLevel = 1; mipLevel < this.MipLevels; mipLevel++) {
                    uint subresource = this.CalculateSubresource(mipLevel, layer);
                    this.GetSubresourceLayout(subresource, out uint offset, out uint size, out _, out _);
                    Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);
                    this.gd.UpdateTexture(this, (IntPtr)(dataPtr + offset), size, 0, 0, 0, mipWidth, mipHeight, mipDepth, mipLevel, layer);
                }
            }
        }
    }

    /// <summary>
    /// Executes the GetSourceByte operation.
    /// </summary>
    /// <param name="srcBaseOffset">Specifies the value of <paramref name="srcBaseOffset" />.</param>
    /// <param name="srcRowPitch">Specifies the value of <paramref name="srcRowPitch" />.</param>
    /// <param name="srcDepthPitch">Specifies the value of <paramref name="srcDepthPitch" />.</param>
    /// <param name="srcX">Specifies the value of <paramref name="srcX" />.</param>
    /// <param name="srcY">Specifies the value of <paramref name="srcY" />.</param>
    /// <param name="srcZ">Specifies the value of <paramref name="srcZ" />.</param>
    /// <param name="bytesPerPixel">Specifies the value of <paramref name="bytesPerPixel" />.</param>
    /// <param name="component">Specifies the value of <paramref name="component" />.</param>
    /// <returns>Returns the result produced by the GetSourceByte operation.</returns>
    private uint GetSourceByte(uint srcBaseOffset, uint srcRowPitch, uint srcDepthPitch, uint srcX, uint srcY, uint srcZ, uint bytesPerPixel, uint component) {
        int srcPixelOffset = (int)(srcBaseOffset
            + srcZ * srcDepthPitch
            + srcY * srcRowPitch
            + srcX * bytesPerPixel
            + component);
        return this._data[srcPixelOffset];
    }

    /// <summary>
    /// Executes the CreateNativeTexture operation.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the CreateNativeTexture operation.</returns>
    private ID3D12Resource CreateNativeTexture(D3D12GraphicsDevice gd, ref TextureDescription description) {
        bool isDepth = (description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil;
        uint effectiveDescriptionArrayLayers = GetEffectiveArrayLayers(description.Usage, description.ArrayLayers);
        Format dxgiFormat = D3D12Formats.ToDxgiFormat(description.Format, isDepth);
        ResourceFlags resourceFlags = D3D12Formats.ToResourceFlags(description.Usage);
        ResourceDescription resourceDescription;

        switch (description.Type) {
            case TextureType.Texture1D:
                resourceDescription = ResourceDescription.Texture1D(dxgiFormat, description.Width, (ushort)effectiveDescriptionArrayLayers, (ushort)description.MipLevels, resourceFlags);
                break;
            case TextureType.Texture2D:
                resourceDescription = ResourceDescription.Texture2D(dxgiFormat, description.Width, description.Height, (ushort)effectiveDescriptionArrayLayers, (ushort)description.MipLevels, FormatHelpers.GetSampleCountUInt32(description.SampleCount), 0, resourceFlags);
                break;
            case TextureType.Texture3D:
                resourceDescription = ResourceDescription.Texture3D(dxgiFormat, description.Width, description.Height, (ushort)description.Depth, (ushort)description.MipLevels, resourceFlags);
                break;
            default: throw Illegal.Value<TextureType>();
        }

        ResourceStates initialState = ResourceStates.Common;
        if (isDepth) {
            initialState = ResourceStates.DepthWrite;
        }
        else if ((description.Usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget) {
            initialState = ResourceStates.RenderTarget;
        }

        return gd.Device.CreateCommittedResource(HeapType.Default, HeapFlags.None, resourceDescription, initialState);
    }

    /// <summary>
    /// Executes the CreateWrappedNativeTexture operation.
    /// </summary>
    /// <param name="nativeHandle">Specifies the value of <paramref name="nativeHandle" />.</param>
    /// <returns>Returns the result produced by the CreateWrappedNativeTexture operation.</returns>
    private static ID3D12Resource CreateWrappedNativeTexture(ulong nativeHandle) {
        if (nativeHandle == 0) {
            throw new VeldridException("Native D3D12 texture handle cannot be 0.");
        }

        return new ID3D12Resource((IntPtr)nativeHandle);
    }

    /// <summary>
    /// Executes the ValidateWrappedTextureDescription operation.
    /// </summary>
    /// <param name="nativeDescription">Specifies the value of <paramref name="nativeDescription" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    private static void ValidateWrappedTextureDescription(ResourceDescription nativeDescription, ref TextureDescription description) {
        bool validDimension = (description.Type == TextureType.Texture1D && nativeDescription.Dimension == ResourceDimension.Texture1D)
            || (description.Type == TextureType.Texture2D && nativeDescription.Dimension == ResourceDimension.Texture2D)
            || (description.Type == TextureType.Texture3D && nativeDescription.Dimension == ResourceDimension.Texture3D);
        if (!validDimension) {
            throw new VeldridException("Wrapped native D3D12 texture dimension does not match TextureDescription.Type.");
        }

        if (nativeDescription.Width != description.Width || nativeDescription.Height != description.Height) {
            throw new VeldridException("Wrapped native D3D12 texture dimensions do not match TextureDescription.");
        }

        if (description.Type == TextureType.Texture3D) {
            if (nativeDescription.DepthOrArraySize != description.Depth) {
                throw new VeldridException("Wrapped native D3D12 texture depth does not match TextureDescription.");
            }
        }
        else {
            uint expectedArrayLayers = GetEffectiveArrayLayers(description.Usage, description.ArrayLayers);
            if (nativeDescription.DepthOrArraySize != expectedArrayLayers) {
                throw new VeldridException("Wrapped native D3D12 texture array layers do not match TextureDescription.");
            }
        }

        if (nativeDescription.MipLevels != description.MipLevels) {
            throw new VeldridException("Wrapped native D3D12 texture mip levels do not match TextureDescription.");
        }

        if (nativeDescription.SampleDescription.Count != FormatHelpers.GetSampleCountUInt32(description.SampleCount)) {
            throw new VeldridException("Wrapped native D3D12 texture sample count does not match TextureDescription.");
        }

        if (!IsNativeFormatCompatible(nativeDescription.Format, ref description)) {
            throw new VeldridException("Wrapped native D3D12 texture format does not match TextureDescription.");
        }

        ResourceFlags requiredFlags = D3D12Formats.ToResourceFlags(description.Usage);
        if ((requiredFlags & ResourceFlags.AllowRenderTarget) != 0
            && (nativeDescription.Flags & ResourceFlags.AllowRenderTarget) == 0) {
            throw new VeldridException("Wrapped native D3D12 texture is missing render-target capability.");
        }

        if ((requiredFlags & ResourceFlags.AllowDepthStencil) != 0
            && (nativeDescription.Flags & ResourceFlags.AllowDepthStencil) == 0) {
            throw new VeldridException("Wrapped native D3D12 texture is missing depth-stencil capability.");
        }

        if ((requiredFlags & ResourceFlags.AllowUnorderedAccess) != 0
            && (nativeDescription.Flags & ResourceFlags.AllowUnorderedAccess) == 0) {
            throw new VeldridException("Wrapped native D3D12 texture is missing unordered-access capability.");
        }
    }

    /// <summary>
    /// Executes the IsNativeFormatCompatible operation.
    /// </summary>
    /// <param name="nativeFormat">Specifies the value of <paramref name="nativeFormat" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the IsNativeFormatCompatible operation.</returns>
    private static bool IsNativeFormatCompatible(Format nativeFormat, ref TextureDescription description) {
        bool depthUsage = (description.Usage & TextureUsage.DepthStencil) != 0;
        Format expectedResourceFormat = D3D12Formats.ToDxgiFormat(description.Format, depthUsage);
        if (nativeFormat == expectedResourceFormat) {
            return true;
        }

        if (D3D12Formats.GetViewFormat(nativeFormat) == D3D12Formats.GetViewFormat(expectedResourceFormat)) {
            return true;
        }

        if (depthUsage) {
            Format expectedDepthFormat = D3D12Formats.ToDepthFormat(description.Format);
            if (nativeFormat == expectedDepthFormat) {
                return true;
            }

            if (D3D12Formats.GetViewFormat(nativeFormat) == D3D12Formats.GetViewFormat(expectedDepthFormat)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Executes the GetCreatedTextureInitialState operation.
    /// </summary>
    /// <param name="usage">Specifies the value of <paramref name="usage" />.</param>
    /// <returns>Returns the result produced by the GetCreatedTextureInitialState operation.</returns>
    private static ResourceStates GetCreatedTextureInitialState(TextureUsage usage) {
        if ((usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil) {
            return ResourceStates.DepthWrite;
        }

        if ((usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget) {
            return ResourceStates.RenderTarget;
        }

        return ResourceStates.Common;
    }

    /// <summary>
    /// Executes the InitializeSubresourceStates operation.
    /// </summary>
    /// <param name="initialState">Specifies the value of <paramref name="initialState" />.</param>
    private void InitializeSubresourceStates(ResourceStates initialState) {
        for (int i = 0; i < this._subresourceStates.Length; i++) {
            this._subresourceStates[i] = initialState;
        }

        this._hasCachedCommonState = true;
        this._cachedCommonState = initialState;
    }

    /// <summary>
    /// Executes the GetSubresourceLayout operation.
    /// </summary>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    /// <param name="offset">Specifies the value of <paramref name="offset" />.</param>
    /// <param name="size">Specifies the value of <paramref name="size" />.</param>
    /// <param name="rowPitch">Specifies the value of <paramref name="rowPitch" />.</param>
    /// <param name="depthPitch">Specifies the value of <paramref name="depthPitch" />.</param>
    private void GetSubresourceLayout(uint subresource, out uint offset, out uint size, out uint rowPitch, out uint depthPitch) {
        uint totalOffset = 0;
        for (uint arrayLayer = 0; arrayLayer < this.EffectiveArrayLayers; arrayLayer++) {
            uint mipWidth = this.Width;
            uint mipHeight = this.Height;
            uint mipDepth = this.Depth;
            for (uint mip = 0; mip < this.MipLevels; mip++) {
                uint currentSubresource = arrayLayer * this.MipLevels + mip;
                uint currentRowPitch = FormatHelpers.GetRowPitch(mipWidth, this.Format);
                uint currentDepthPitch = FormatHelpers.GetDepthPitch(currentRowPitch, mipHeight, this.Format);
                uint currentSize = currentDepthPitch * mipDepth;
                if (currentSubresource == subresource) {
                    offset = totalOffset;
                    size = currentSize;
                    rowPitch = currentRowPitch;
                    depthPitch = currentDepthPitch;
                    return;
                }

                totalOffset += currentSize;
                mipWidth = Math.Max(1, mipWidth / 2);
                mipHeight = Math.Max(1, mipHeight / 2);
                mipDepth = Math.Max(1, mipDepth / 2);
            }
        }

        throw new VeldridException("Subresource index is out of bounds.");
    }

    /// <summary>
    /// Executes the ComputeTotalSize operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the ComputeTotalSize operation.</returns>
    private static int ComputeTotalSize(ref TextureDescription description) {
        uint total = 0;
        uint effectiveDescriptionArrayLayers = GetEffectiveArrayLayers(description.Usage, description.ArrayLayers);
        uint width = description.Width;
        uint height = description.Height;
        uint depth = description.Depth;

        for (uint mip = 0; mip < description.MipLevels; mip++) {
            total += FormatHelpers.GetRegionSize(width, height, depth, description.Format) * effectiveDescriptionArrayLayers;
            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
            depth = Math.Max(1, depth / 2);
        }

        return (int)total;
    }

    /// <summary>
    /// Executes the GetEffectiveArrayLayers operation.
    /// </summary>
    /// <param name="usage">Specifies the value of <paramref name="usage" />.</param>
    /// <param name="arrayLayers">Specifies the value of <paramref name="arrayLayers" />.</param>
    /// <returns>Returns the result produced by the GetEffectiveArrayLayers operation.</returns>
    private static uint GetEffectiveArrayLayers(TextureUsage usage, uint arrayLayers) {
        if ((usage & TextureUsage.Cubemap) != 0) {
            return arrayLayers * 6;
        }

        return arrayLayers;
    }

    /// <summary>
    /// Executes the IsStagingTexture operation.
    /// </summary>
    /// <returns>Returns the result produced by the IsStagingTexture operation.</returns>
    private bool IsStagingTexture() {
        return (this.Usage & TextureUsage.Staging) == TextureUsage.Staging;
    }

    /// <summary>
    /// Executes the SyncSubresourceToNative operation.
    /// </summary>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    private void SyncSubresourceToNative(uint subresource) {
        ResourceDescription textureDescription = this.NativeTexture.Description;
        PlacedSubresourceFootPrint[] layouts = new PlacedSubresourceFootPrint[1];
        uint[] rowCounts = new uint[1];
        ulong[] rowSizesInBytes = new ulong[1];
        this.gd.Device.GetCopyableFootprints(textureDescription, subresource, 1, 0, layouts, rowCounts, rowSizesInBytes, out ulong totalBytes);

        ID3D12Resource uploadBuffer = this.gd.Device.CreateCommittedResource(HeapType.Upload, HeapFlags.None, ResourceDescription.Buffer(totalBytes), ResourceStates.GenericRead);

        try {
            this.GetSubresourceLayout(subresource, out uint srcOffset, out _, out uint srcRowPitch, out uint srcDepthPitch);
            Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out _);
            Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);

            unsafe {
                void* mappedUpload = null;
                uploadBuffer.Map(0, &mappedUpload).CheckError();
                try {
                    fixed (byte* srcBase = this._data) {
                        byte* srcSubresource = srcBase + srcOffset;
                        byte* dstUpload = (byte*)mappedUpload + layouts[0].Offset;
                        uint dstRowPitch = layouts[0].Footprint.RowPitch;
                        uint dstDepthPitch = dstRowPitch * rowCounts[0];
                        Util.CopyTextureRegion(srcSubresource, 0, 0, 0, srcRowPitch, srcDepthPitch, dstUpload, 0, 0, 0, dstRowPitch, dstDepthPitch, mipWidth, mipHeight, mipDepth, this.Format);
                    }
                }
                finally {
                    uploadBuffer.Unmap(0);
                }
            }

            this.ExecuteTextureBufferCopy(subresource, ResourceStates.CopyDest, previousState => {
                TextureCopyLocation destination = new(this.NativeTexture, subresource);
                TextureCopyLocation sourceLocation = new(uploadBuffer, layouts[0]);
                return (destination, sourceLocation, sourceBox: null, previousState);
            }, true);
        }
        finally {
            uploadBuffer.Dispose();
        }
    }

    /// <summary>
    /// Executes the SyncSubresourceFromNative operation.
    /// </summary>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    private void SyncSubresourceFromNative(uint subresource) {
        ResourceDescription textureDescription = this.NativeTexture.Description;
        PlacedSubresourceFootPrint[] layouts = new PlacedSubresourceFootPrint[1];
        uint[] rowCounts = new uint[1];
        ulong[] rowSizesInBytes = new ulong[1];
        this.gd.Device.GetCopyableFootprints(textureDescription, subresource, 1, 0, layouts, rowCounts, rowSizesInBytes, out ulong totalBytes);

        ID3D12Resource readbackBuffer = this.gd.Device.CreateCommittedResource(HeapType.Readback, HeapFlags.None, ResourceDescription.Buffer(totalBytes), ResourceStates.CopyDest);

        try {
            this.GetSubresourceLayout(subresource, out uint dstOffset, out _, out uint dstRowPitch, out uint dstDepthPitch);
            Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out _);
            Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);

            this.ExecuteTextureBufferCopy(subresource, ResourceStates.CopySource, previousState => {
                TextureCopyLocation destination = new(readbackBuffer, layouts[0]);
                TextureCopyLocation sourceLocation = new(this.NativeTexture, subresource);
                Box sourceBox = new(0, 0, 0, (int)mipWidth, (int)mipHeight, (int)mipDepth);
                return (destination, sourceLocation, sourceBox, previousState);
            }, false);

            unsafe {
                void* mappedReadback = null;
                readbackBuffer.Map(0, &mappedReadback).CheckError();
                try {
                    fixed (byte* dstBase = this._data) {
                        byte* srcReadback = (byte*)mappedReadback + layouts[0].Offset;
                        byte* dstSubresource = dstBase + dstOffset;
                        uint srcRowPitch = layouts[0].Footprint.RowPitch;
                        uint srcDepthPitch = srcRowPitch * rowCounts[0];
                        Util.CopyTextureRegion(srcReadback, 0, 0, 0, srcRowPitch, srcDepthPitch, dstSubresource, 0, 0, 0, dstRowPitch, dstDepthPitch, mipWidth, mipHeight, mipDepth, this.Format);
                    }
                }
                finally {
                    readbackBuffer.Unmap(0);
                }
            }
        }
        finally {
            readbackBuffer.Dispose();
        }
    }

    /// <summary>
    /// Executes the ExecuteTextureBufferCopy operation.
    /// </summary>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    /// <param name="copyState">Specifies the value of <paramref name="copyState" />.</param>
    /// <param name="previousState">Specifies the value of <paramref name="previousState" />.</param>
    /// <param name="copyToTexture">Specifies the value of <paramref name="copyToTexture" />.</param>
    private void ExecuteTextureBufferCopy(uint subresource, ResourceStates copyState, Func<ResourceStates, (TextureCopyLocation destination, TextureCopyLocation source, Box? sourceBox, ResourceStates previousState)> buildCopy, bool copyToTexture) {
        ID3D12CommandAllocator allocator = this.gd.Device.CreateCommandAllocator(CommandListType.Direct);
        ID3D12GraphicsCommandList commandList = this.gd.Device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, allocator);
        try {
            ResourceStates previousState = this.GetSubresourceState(subresource);
            if (previousState != copyState) {
                ResourceBarrier toCopy = ResourceBarrier.BarrierTransition(this.NativeTexture, previousState, copyState, subresource);
                commandList.ResourceBarrier(new[] { toCopy });
                this.SetSubresourceState(subresource, copyState);
            }

            (TextureCopyLocation destination, TextureCopyLocation source, Box? sourceBox, ResourceStates previousState)
                copyInfo = buildCopy(previousState);
            if (copyToTexture) {
                if (copyInfo.sourceBox.HasValue) {
                    commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source, copyInfo.sourceBox.Value);
                }
                else {
                    commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source);
                }
            }
            else {
                if (copyInfo.sourceBox.HasValue) {
                    commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source, copyInfo.sourceBox.Value);
                }
                else {
                    commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source);
                }
            }

            if (copyInfo.previousState != copyState) {
                ResourceBarrier fromCopy = ResourceBarrier.BarrierTransition(this.NativeTexture, copyState, copyInfo.previousState, subresource);
                commandList.ResourceBarrier(new[] { fromCopy });
                this.SetSubresourceState(subresource, copyInfo.previousState);
            }

            commandList.Close();
            this.gd.CommandQueue.ExecuteCommandList(commandList);
            this.gd.WaitForIdle();
        }
        finally {
            commandList.Dispose();
            allocator.Dispose();
        }
    }

    /// <summary>
    /// Executes the DisposeCore operation.
    /// </summary>
    private protected override void DisposeCore() {
        if (this._mapped) {
            this._pinnedData.Free();
            this._mapped = false;
        }

        if (this._ownsNativeTexture) {
            this.NativeTexture?.Dispose();
        }

        this._disposed = true;
    }

    /// <summary>
    /// Executes the GetSubresourceState operation.
    /// </summary>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    /// <returns>Returns the result produced by the GetSubresourceState operation.</returns>
    internal ResourceStates GetSubresourceState(uint subresource) {
        if (this._subresourceStates == null || subresource >= this._subresourceStates.Length) {
            return ResourceStates.Common;
        }

        return this._subresourceStates[subresource];
    }

    /// <summary>
    /// Executes the SetSubresourceState operation.
    /// </summary>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    /// <param name="state">Specifies the value of <paramref name="state" />.</param>
    internal void SetSubresourceState(uint subresource, ResourceStates state) {
        if (this._subresourceStates == null || subresource >= this._subresourceStates.Length) {
            return;
        }

        ResourceStates previous = this._subresourceStates[subresource];
        if (previous == state) {
            return;
        }

        this._subresourceStates[subresource] = state;
        if (this._hasCachedCommonState && state != this._cachedCommonState) {
            this._hasCachedCommonState = false;
        }
    }

    /// <summary>
    /// Executes the SetAllSubresourceStates operation.
    /// </summary>
    /// <param name="state">Specifies the value of <paramref name="state" />.</param>
    internal void SetAllSubresourceStates(ResourceStates state) {
        if (this._subresourceStates == null) {
            return;
        }

        for (int i = 0; i < this._subresourceStates.Length; i++) {
            this._subresourceStates[i] = state;
        }

        this._hasCachedCommonState = true;
        this._cachedCommonState = state;
    }

    /// <summary>
    /// Executes the TryGetCommonState operation.
    /// </summary>
    /// <param name="state">Specifies the value of <paramref name="state" />.</param>
    /// <returns>Returns the result produced by the TryGetCommonState operation.</returns>
    internal bool TryGetCommonState(out ResourceStates state) {
        if (this._subresourceStates == null || this._subresourceStates.Length == 0) {
            state = ResourceStates.Common;
            return true;
        }

        if (this._hasCachedCommonState) {
            state = this._cachedCommonState;
            return true;
        }

        state = this._subresourceStates[0];
        for (int i = 1; i < this._subresourceStates.Length; i++) {
            if (this._subresourceStates[i] != state) {
                return false;
            }
        }

        this._hasCachedCommonState = true;
        this._cachedCommonState = state;
        return true;
    }
}