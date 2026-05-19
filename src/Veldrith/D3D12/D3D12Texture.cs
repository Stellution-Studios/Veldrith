using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Veldrith.D3D12;

internal sealed class D3D12Texture : Texture {
    private readonly byte[] _data;
    private readonly bool _ownsNativeTexture;
    private readonly ResourceStates[] _subresourceStates;
    private readonly D3D12GraphicsDevice gd;
    private MapMode? _activeMapMode;
    private uint _activeMapSubresource;
    private ResourceStates _cachedCommonState;
    private bool _disposed;
    private bool _hasCachedCommonState;
    private bool _mapped;
    private GCHandle _pinnedData;

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
    internal ID3D12Resource NativeTexture { get; }

    internal ResourceStates CurrentState {
        get {
            if (this._subresourceStates == null || this._subresourceStates.Length == 0) {
                return ResourceStates.Common;
            }

            return this._subresourceStates[0];
        }
        set => this.SetAllSubresourceStates(value);
    }

    internal uint SubresourceCount => (uint)(this._subresourceStates?.Length ?? 0);
    internal uint EffectiveArrayLayers { get; }

    public override string Name { get; set; }

    internal void Update(
        IntPtr source,
        uint sizeInBytes,
        uint x,
        uint y,
        uint z,
        uint width,
        uint height,
        uint depth,
        uint mipLevel,
        uint arrayLayer) {
        uint subresource = this.CalculateSubresource(mipLevel, arrayLayer);
        Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);
        if (x + width > mipWidth || y + height > mipHeight || z + depth > mipDepth) {
            throw new VeldridException("Texture update region exceeds texture bounds.");
        }

        uint requiredSize = FormatHelpers.GetRegionSize(width, height, depth, this.Format);
        if (sizeInBytes < requiredSize) {
            throw new VeldridException(
                "Texture update source size is smaller than required for the destination region.");
        }

        this.GetSubresourceLayout(subresource, out uint dstOffset, out uint dstSize, out uint dstRowPitch,
            out uint dstDepthPitch);
        if (dstOffset + dstSize > (uint)this._data.Length) {
            throw new VeldridException("Texture update destination region exceeds texture storage.");
        }

        uint srcRowPitch = FormatHelpers.GetRowPitch(width, this.Format);
        uint srcDepthPitch = FormatHelpers.GetDepthPitch(srcRowPitch, height, this.Format);
        unsafe {
            fixed (byte* dstBase = this._data) {
                Util.CopyTextureRegion(
                    source.ToPointer(),
                    0, 0, 0,
                    srcRowPitch,
                    srcDepthPitch,
                    dstBase + dstOffset,
                    x, y, z,
                    dstRowPitch,
                    dstDepthPitch,
                    width,
                    height,
                    depth, this.Format);
            }
        }
    }

    internal void UpdateNativeSubresource(
        IntPtr source,
        uint sizeInBytes,
        uint x,
        uint y,
        uint z,
        uint width,
        uint height,
        uint depth,
        uint mipLevel,
        uint arrayLayer) {
        this.Update(source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);

        if (this.NativeTexture == null) {
            return;
        }

        uint subresource = this.CalculateSubresource(mipLevel, arrayLayer);
        this.SyncSubresourceToNative(subresource);
    }

    internal MappedResource Map(MapMode mode, uint subresource) {
        if (subresource >= this.SubresourceCount) {
            throw new VeldridException("Subresource index is out of bounds.");
        }

        if (this.IsStagingTexture() && this.NativeTexture != null &&
            (mode == MapMode.Read || mode == MapMode.ReadWrite)) {
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

    internal void CopyRegionTo(
        D3D12Texture destination,
        uint srcX,
        uint srcY,
        uint srcZ,
        uint srcMipLevel,
        uint srcBaseArrayLayer,
        uint dstX,
        uint dstY,
        uint dstZ,
        uint dstMipLevel,
        uint dstBaseArrayLayer,
        uint width,
        uint height,
        uint depth,
        uint layerCount) {
        if (this.Format != destination.Format) {
            throw new VeldridException("Source and destination texture formats must match.");
        }

        for (uint layer = 0; layer < layerCount; layer++) {
            uint srcSubresource = (srcBaseArrayLayer + layer) * this.MipLevels + srcMipLevel;
            uint dstSubresource = (dstBaseArrayLayer + layer) * destination.MipLevels + dstMipLevel;
            this.GetSubresourceLayout(srcSubresource, out uint srcBaseOffset, out _, out uint srcRowPitch,
                out uint srcDepthPitch);
            destination.GetSubresourceLayout(dstSubresource, out uint dstBaseOffset, out _, out uint dstRowPitch,
                out uint dstDepthPitch);

            unsafe {
                fixed (byte* srcBase = this._data) {
                    fixed (byte* dstBase = destination._data) {
                        byte* srcSubresourcePtr = srcBase + srcBaseOffset;
                        byte* dstSubresourcePtr = dstBase + dstBaseOffset;
                        Util.CopyTextureRegion(
                            srcSubresourcePtr,
                            srcX, srcY, srcZ,
                            srcRowPitch,
                            srcDepthPitch,
                            dstSubresourcePtr,
                            dstX, dstY, dstZ,
                            dstRowPitch,
                            dstDepthPitch,
                            width,
                            height,
                            depth, this.Format);
                    }
                }
            }
        }
    }

    internal bool GenerateMipmapsCpu() {
        if (this.MipLevels <= 1 || FormatHelpers.IsCompressedFormat(this.Format) ||
            (this.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil) {
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

                this.GetSubresourceLayout(srcSubresource, out uint srcBaseOffset, out _, out uint srcRowPitch,
                    out uint srcDepthPitch);
                this.GetSubresourceLayout(dstSubresource, out uint dstBaseOffset, out _, out uint dstRowPitch,
                    out uint dstDepthPitch);

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

                            int dstPixelOffset = (int)(
                                dstBaseOffset
                                + z * dstDepthPitch
                                + y * dstRowPitch
                                + x * bytesPerPixel);

                            for (uint component = 0; component < bytesPerPixel; component++) {
                                uint sum = 0;
                                uint sampleCount = 0;

                                sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY0,
                                    srcZ0, bytesPerPixel, component);
                                sampleCount++;
                                sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY0,
                                    srcZ0, bytesPerPixel, component);
                                sampleCount++;
                                sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY1,
                                    srcZ0, bytesPerPixel, component);
                                sampleCount++;
                                sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY1,
                                    srcZ0, bytesPerPixel, component);
                                sampleCount++;

                                if (srcDepth > 1) {
                                    sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY0,
                                        srcZ1, bytesPerPixel, component);
                                    sampleCount++;
                                    sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY0,
                                        srcZ1, bytesPerPixel, component);
                                    sampleCount++;
                                    sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY1,
                                        srcZ1, bytesPerPixel, component);
                                    sampleCount++;
                                    sum += this.GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY1,
                                        srcZ1, bytesPerPixel, component);
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
                    this.gd.UpdateTexture(
                        this,
                        (IntPtr)(dataPtr + offset),
                        size,
                        0, 0, 0,
                        mipWidth, mipHeight, mipDepth,
                        mipLevel, layer);
                }
            }
        }
    }

    private uint GetSourceByte(
        uint srcBaseOffset,
        uint srcRowPitch,
        uint srcDepthPitch,
        uint srcX,
        uint srcY,
        uint srcZ,
        uint bytesPerPixel,
        uint component) {
        int srcPixelOffset = (int)(
            srcBaseOffset
            + srcZ * srcDepthPitch
            + srcY * srcRowPitch
            + srcX * bytesPerPixel
            + component);
        return this._data[srcPixelOffset];
    }

    private ID3D12Resource CreateNativeTexture(D3D12GraphicsDevice gd, ref TextureDescription description) {
        bool isDepth = (description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil;
        uint effectiveDescriptionArrayLayers = GetEffectiveArrayLayers(description.Usage, description.ArrayLayers);
        Format dxgiFormat = D3D12Formats.ToDxgiFormat(description.Format, isDepth);
        ResourceFlags resourceFlags = D3D12Formats.ToResourceFlags(description.Usage);
        ResourceDescription resourceDescription;

        switch (description.Type) {
            case TextureType.Texture1D:
                resourceDescription = ResourceDescription.Texture1D(
                    dxgiFormat,
                    description.Width,
                    (ushort)effectiveDescriptionArrayLayers,
                    (ushort)description.MipLevels,
                    resourceFlags);
                break;
            case TextureType.Texture2D:
                resourceDescription = ResourceDescription.Texture2D(
                    dxgiFormat,
                    description.Width,
                    description.Height,
                    (ushort)effectiveDescriptionArrayLayers,
                    (ushort)description.MipLevels,
                    FormatHelpers.GetSampleCountUInt32(description.SampleCount),
                    0,
                    resourceFlags);
                break;
            case TextureType.Texture3D:
                resourceDescription = ResourceDescription.Texture3D(
                    dxgiFormat,
                    description.Width,
                    description.Height,
                    (ushort)description.Depth,
                    (ushort)description.MipLevels,
                    resourceFlags);
                break;
            default:
                throw Illegal.Value<TextureType>();
        }

        ResourceStates initialState = ResourceStates.Common;
        if (isDepth) {
            initialState = ResourceStates.DepthWrite;
        }
        else if ((description.Usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget) {
            initialState = ResourceStates.RenderTarget;
        }

        return gd.Device.CreateCommittedResource(
            HeapType.Default,
            HeapFlags.None,
            resourceDescription,
            initialState);
    }

    private static ID3D12Resource CreateWrappedNativeTexture(ulong nativeHandle) {
        if (nativeHandle == 0) {
            throw new VeldridException("Native D3D12 texture handle cannot be 0.");
        }

        return new ID3D12Resource((IntPtr)nativeHandle);
    }

    private static void ValidateWrappedTextureDescription(ResourceDescription nativeDescription,
        ref TextureDescription description) {
        bool validDimension =
            (description.Type == TextureType.Texture1D && nativeDescription.Dimension == ResourceDimension.Texture1D)
            || (description.Type == TextureType.Texture2D && nativeDescription.Dimension == ResourceDimension.Texture2D)
            || (description.Type == TextureType.Texture3D &&
                nativeDescription.Dimension == ResourceDimension.Texture3D);
        if (!validDimension) {
            throw new VeldridException(
                "Wrapped native D3D12 texture dimension does not match TextureDescription.Type.");
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
                throw new VeldridException(
                    "Wrapped native D3D12 texture array layers do not match TextureDescription.");
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

    private static ResourceStates GetCreatedTextureInitialState(TextureUsage usage) {
        if ((usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil) {
            return ResourceStates.DepthWrite;
        }

        if ((usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget) {
            return ResourceStates.RenderTarget;
        }

        return ResourceStates.Common;
    }

    private void InitializeSubresourceStates(ResourceStates initialState) {
        for (int i = 0; i < this._subresourceStates.Length; i++) {
            this._subresourceStates[i] = initialState;
        }

        this._hasCachedCommonState = true;
        this._cachedCommonState = initialState;
    }

    private void GetSubresourceLayout(uint subresource, out uint offset, out uint size, out uint rowPitch,
        out uint depthPitch) {
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

    private static int ComputeTotalSize(ref TextureDescription description) {
        uint total = 0;
        uint effectiveDescriptionArrayLayers = GetEffectiveArrayLayers(description.Usage, description.ArrayLayers);
        uint width = description.Width;
        uint height = description.Height;
        uint depth = description.Depth;

        for (uint mip = 0; mip < description.MipLevels; mip++) {
            total += FormatHelpers.GetRegionSize(width, height, depth, description.Format) *
                     effectiveDescriptionArrayLayers;
            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
            depth = Math.Max(1, depth / 2);
        }

        return (int)total;
    }

    private static uint GetEffectiveArrayLayers(TextureUsage usage, uint arrayLayers) {
        if ((usage & TextureUsage.Cubemap) != 0) {
            return arrayLayers * 6;
        }

        return arrayLayers;
    }

    private bool IsStagingTexture() {
        return (this.Usage & TextureUsage.Staging) == TextureUsage.Staging;
    }

    private void SyncSubresourceToNative(uint subresource) {
        ResourceDescription textureDescription = this.NativeTexture.Description;
        PlacedSubresourceFootPrint[] layouts = new PlacedSubresourceFootPrint[1];
        uint[] rowCounts = new uint[1];
        ulong[] rowSizesInBytes = new ulong[1];
        this.gd.Device.GetCopyableFootprints(textureDescription, subresource, 1, 0, layouts, rowCounts, rowSizesInBytes,
            out ulong totalBytes);

        ID3D12Resource uploadBuffer = this.gd.Device.CreateCommittedResource(
            HeapType.Upload,
            HeapFlags.None,
            ResourceDescription.Buffer(totalBytes),
            ResourceStates.GenericRead);

        try {
            this.GetSubresourceLayout(subresource, out uint srcOffset, out _, out uint srcRowPitch,
                out uint srcDepthPitch);
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
                        Util.CopyTextureRegion(
                            srcSubresource,
                            0, 0, 0,
                            srcRowPitch,
                            srcDepthPitch,
                            dstUpload,
                            0, 0, 0,
                            dstRowPitch,
                            dstDepthPitch,
                            mipWidth,
                            mipHeight,
                            mipDepth, this.Format);
                    }
                }
                finally {
                    uploadBuffer.Unmap(0);
                }
            }

            this.ExecuteTextureBufferCopy(
                subresource,
                ResourceStates.CopyDest,
                previousState => {
                    TextureCopyLocation destination = new(this.NativeTexture, subresource);
                    TextureCopyLocation sourceLocation = new(uploadBuffer, layouts[0]);
                    return (destination, sourceLocation, sourceBox: null, previousState);
                },
                true);
        }
        finally {
            uploadBuffer.Dispose();
        }
    }

    private void SyncSubresourceFromNative(uint subresource) {
        ResourceDescription textureDescription = this.NativeTexture.Description;
        PlacedSubresourceFootPrint[] layouts = new PlacedSubresourceFootPrint[1];
        uint[] rowCounts = new uint[1];
        ulong[] rowSizesInBytes = new ulong[1];
        this.gd.Device.GetCopyableFootprints(textureDescription, subresource, 1, 0, layouts, rowCounts, rowSizesInBytes,
            out ulong totalBytes);

        ID3D12Resource readbackBuffer = this.gd.Device.CreateCommittedResource(
            HeapType.Readback,
            HeapFlags.None,
            ResourceDescription.Buffer(totalBytes),
            ResourceStates.CopyDest);

        try {
            this.GetSubresourceLayout(subresource, out uint dstOffset, out _, out uint dstRowPitch,
                out uint dstDepthPitch);
            Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out _);
            Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);

            this.ExecuteTextureBufferCopy(
                subresource,
                ResourceStates.CopySource,
                previousState => {
                    TextureCopyLocation destination = new(readbackBuffer, layouts[0]);
                    TextureCopyLocation sourceLocation = new(this.NativeTexture, subresource);
                    Box sourceBox = new(0, 0, 0, (int)mipWidth, (int)mipHeight, (int)mipDepth);
                    return (destination, sourceLocation, sourceBox, previousState);
                },
                false);

            unsafe {
                void* mappedReadback = null;
                readbackBuffer.Map(0, &mappedReadback).CheckError();
                try {
                    fixed (byte* dstBase = this._data) {
                        byte* srcReadback = (byte*)mappedReadback + layouts[0].Offset;
                        byte* dstSubresource = dstBase + dstOffset;
                        uint srcRowPitch = layouts[0].Footprint.RowPitch;
                        uint srcDepthPitch = srcRowPitch * rowCounts[0];
                        Util.CopyTextureRegion(
                            srcReadback,
                            0, 0, 0,
                            srcRowPitch,
                            srcDepthPitch,
                            dstSubresource,
                            0, 0, 0,
                            dstRowPitch,
                            dstDepthPitch,
                            mipWidth,
                            mipHeight,
                            mipDepth, this.Format);
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

    private void ExecuteTextureBufferCopy(
        uint subresource,
        ResourceStates copyState,
        Func<ResourceStates, (TextureCopyLocation destination, TextureCopyLocation source, Box? sourceBox,
            ResourceStates previousState)> buildCopy,
        bool copyToTexture) {
        ID3D12CommandAllocator allocator = this.gd.Device.CreateCommandAllocator(CommandListType.Direct);
        ID3D12GraphicsCommandList commandList =
            this.gd.Device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, allocator);
        try {
            ResourceStates previousState = this.GetSubresourceState(subresource);
            if (previousState != copyState) {
                ResourceBarrier toCopy = ResourceBarrier.BarrierTransition(this.NativeTexture,
                    previousState,
                    copyState,
                    subresource);
                commandList.ResourceBarrier(new[] { toCopy });
                this.SetSubresourceState(subresource, copyState);
            }

            (TextureCopyLocation destination, TextureCopyLocation source, Box? sourceBox, ResourceStates previousState)
                copyInfo = buildCopy(previousState);
            if (copyToTexture) {
                if (copyInfo.sourceBox.HasValue) {
                    commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source,
                        copyInfo.sourceBox.Value);
                }
                else {
                    commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source);
                }
            }
            else {
                if (copyInfo.sourceBox.HasValue) {
                    commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source,
                        copyInfo.sourceBox.Value);
                }
                else {
                    commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source);
                }
            }

            if (copyInfo.previousState != copyState) {
                ResourceBarrier fromCopy = ResourceBarrier.BarrierTransition(this.NativeTexture,
                    copyState,
                    copyInfo.previousState,
                    subresource);
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

    internal ResourceStates GetSubresourceState(uint subresource) {
        if (this._subresourceStates == null || subresource >= this._subresourceStates.Length) {
            return ResourceStates.Common;
        }

        return this._subresourceStates[subresource];
    }

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