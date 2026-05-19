using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Veldrith.D3D12
{
    internal sealed class D3D12Texture : Texture
    {
        private readonly D3D12GraphicsDevice gd;
        private readonly byte[] _data;
        private readonly bool _ownsNativeTexture;
        private readonly uint _effectiveArrayLayers;
        private readonly ResourceStates[] _subresourceStates;
        private bool _hasCachedCommonState;
        private ResourceStates _cachedCommonState;
        private GCHandle _pinnedData;
        private MapMode? _activeMapMode;
        private uint _activeMapSubresource;
        private bool _mapped;
        private bool _disposed;
        private string _name;

        public D3D12Texture(D3D12GraphicsDevice gd, ref TextureDescription description, ulong? nativeHandle)
        {
            this.gd = gd;
            Width = description.Width;
            Height = description.Height;
            Depth = description.Depth;
            MipLevels = description.MipLevels;
            ArrayLayers = description.ArrayLayers;
            Format = description.Format;
            Usage = description.Usage;
            Type = description.Type;
            SampleCount = description.SampleCount;
            this._effectiveArrayLayers = GetEffectiveArrayLayers(Usage, ArrayLayers);
            this._data = new byte[ComputeTotalSize(ref description)];
            this._subresourceStates = new ResourceStates[MipLevels * this._effectiveArrayLayers];

            if (nativeHandle == null)
            {
                NativeTexture = CreateNativeTexture(gd, ref description);
                this._ownsNativeTexture = true;
                InitializeSubresourceStates(GetCreatedTextureInitialState(description.Usage));
            }
            else
            {
                NativeTexture = CreateWrappedNativeTexture(nativeHandle.Value);
                this._ownsNativeTexture = false;
                ValidateWrappedTextureDescription(NativeTexture.Description, ref description);
                InitializeSubresourceStates(ResourceStates.Common);
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
        internal ResourceStates CurrentState
        {
            get
            {
                if (this._subresourceStates == null || this._subresourceStates.Length == 0)
                {
                    return ResourceStates.Common;
                }

                return this._subresourceStates[0];
            }
            set => SetAllSubresourceStates(value);
        }
        internal uint SubresourceCount => (uint)(this._subresourceStates?.Length ?? 0);
        internal uint EffectiveArrayLayers => this._effectiveArrayLayers;

        public override string Name
        {
            get => this._name;
            set => this._name = value;
        }

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
            uint arrayLayer)
        {
            uint subresource = CalculateSubresource(mipLevel, arrayLayer);
            Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);
            if (x + width > mipWidth || y + height > mipHeight || z + depth > mipDepth)
            {
                throw new VeldridException("Texture update region exceeds texture bounds.");
            }

            uint requiredSize = FormatHelpers.GetRegionSize(width, height, depth, Format);
            if (sizeInBytes < requiredSize)
            {
                throw new VeldridException("Texture update source size is smaller than required for the destination region.");
            }

            GetSubresourceLayout(subresource, out uint dstOffset, out uint dstSize, out uint dstRowPitch, out uint dstDepthPitch);
            if (dstOffset + dstSize > (uint)this._data.Length)
            {
                throw new VeldridException("Texture update destination region exceeds texture storage.");
            }

            uint srcRowPitch = FormatHelpers.GetRowPitch(width, Format);
            uint srcDepthPitch = FormatHelpers.GetDepthPitch(srcRowPitch, height, Format);
            unsafe
            {
                fixed (byte* dstBase = this._data)
                {
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
                        depth,
                        Format);
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
            uint arrayLayer)
        {
            Update(source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);

            if (NativeTexture == null)
            {
                return;
            }

            uint subresource = CalculateSubresource(mipLevel, arrayLayer);
            SyncSubresourceToNative(subresource);
        }

        internal MappedResource Map(MapMode mode, uint subresource)
        {
            if (subresource >= SubresourceCount)
            {
                throw new VeldridException("Subresource index is out of bounds.");
            }

            if (IsStagingTexture() && NativeTexture != null && (mode == MapMode.Read || mode == MapMode.ReadWrite))
            {
                SyncSubresourceFromNative(subresource);
            }

            if (!this._mapped)
            {
                this._pinnedData = GCHandle.Alloc(this._data, GCHandleType.Pinned);
                this._mapped = true;
            }

            this._activeMapMode = mode;
            this._activeMapSubresource = subresource;
            GetSubresourceLayout(subresource, out uint offset, out uint size, out uint rowPitch, out uint depthPitch);
            IntPtr dataPtr = IntPtr.Add(this._pinnedData.AddrOfPinnedObject(), (int)offset);
            return new MappedResource(this, mode, dataPtr, size, subresource, rowPitch, depthPitch);
        }

        internal void Unmap()
        {
            if (this._mapped
                && IsStagingTexture()
                && NativeTexture != null
                && this._activeMapMode.HasValue
                && (this._activeMapMode.Value == MapMode.Write || this._activeMapMode.Value == MapMode.ReadWrite))
            {
                SyncSubresourceToNative(this._activeMapSubresource);
            }

            this._activeMapMode = null;
            if (this._mapped)
            {
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
            uint layerCount)
        {
            if (Format != destination.Format)
            {
                throw new VeldridException("Source and destination texture formats must match.");
            }

            for (uint layer = 0; layer < layerCount; layer++)
            {
                uint srcSubresource = (srcBaseArrayLayer + layer) * MipLevels + srcMipLevel;
                uint dstSubresource = (dstBaseArrayLayer + layer) * destination.MipLevels + dstMipLevel;
                GetSubresourceLayout(srcSubresource, out uint srcBaseOffset, out _, out uint srcRowPitch, out uint srcDepthPitch);
                destination.GetSubresourceLayout(dstSubresource, out uint dstBaseOffset, out _, out uint dstRowPitch, out uint dstDepthPitch);

                unsafe
                {
                    fixed (byte* srcBase = this._data)
                    {
                        fixed (byte* dstBase = destination._data)
                        {
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
                                depth,
                                Format);
                        }
                    }
                }
            }
        }

        internal bool GenerateMipmapsCpu()
        {
            if (MipLevels <= 1 || FormatHelpers.IsCompressedFormat(Format) || (Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil)
            {
                return false;
            }

            uint bytesPerPixel = FormatSizeHelpers.GetSizeInBytes(Format);
            if (bytesPerPixel == 0 || bytesPerPixel > 16)
            {
                return false;
            }

            for (uint layer = 0; layer < this._effectiveArrayLayers; layer++)
            {
                for (uint mipLevel = 1; mipLevel < MipLevels; mipLevel++)
                {
                    uint srcSubresource = CalculateSubresource(mipLevel - 1, layer);
                    uint dstSubresource = CalculateSubresource(mipLevel, layer);

                    GetSubresourceLayout(srcSubresource, out uint srcBaseOffset, out _, out uint srcRowPitch, out uint srcDepthPitch);
                    GetSubresourceLayout(dstSubresource, out uint dstBaseOffset, out _, out uint dstRowPitch, out uint dstDepthPitch);

                    Util.GetMipDimensions(this, mipLevel - 1, out uint srcWidth, out uint srcHeight, out uint srcDepth);
                    Util.GetMipDimensions(this, mipLevel, out uint dstWidth, out uint dstHeight, out uint dstDepth);

                    for (uint z = 0; z < dstDepth; z++)
                    {
                        uint srcZ0 = Math.Min(srcDepth - 1, z * 2);
                        uint srcZ1 = Math.Min(srcDepth - 1, srcZ0 + 1);

                        for (uint y = 0; y < dstHeight; y++)
                        {
                            uint srcY0 = Math.Min(srcHeight - 1, y * 2);
                            uint srcY1 = Math.Min(srcHeight - 1, srcY0 + 1);

                            for (uint x = 0; x < dstWidth; x++)
                            {
                                uint srcX0 = Math.Min(srcWidth - 1, x * 2);
                                uint srcX1 = Math.Min(srcWidth - 1, srcX0 + 1);

                                int dstPixelOffset = (int)(
                                    dstBaseOffset
                                    + z * dstDepthPitch
                                    + y * dstRowPitch
                                    + x * bytesPerPixel);

                                for (uint component = 0; component < bytesPerPixel; component++)
                                {
                                    uint sum = 0;
                                    uint sampleCount = 0;

                                    sum += GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY0, srcZ0, bytesPerPixel, component); sampleCount++;
                                    sum += GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY0, srcZ0, bytesPerPixel, component); sampleCount++;
                                    sum += GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY1, srcZ0, bytesPerPixel, component); sampleCount++;
                                    sum += GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY1, srcZ0, bytesPerPixel, component); sampleCount++;

                                    if (srcDepth > 1)
                                    {
                                        sum += GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY0, srcZ1, bytesPerPixel, component); sampleCount++;
                                        sum += GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY0, srcZ1, bytesPerPixel, component); sampleCount++;
                                        sum += GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY1, srcZ1, bytesPerPixel, component); sampleCount++;
                                        sum += GetSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY1, srcZ1, bytesPerPixel, component); sampleCount++;
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

        internal unsafe void UploadGeneratedMipmaps()
        {
            if (NativeTexture == null || MipLevels <= 1)
            {
                return;
            }

            fixed (byte* dataPtr = this._data)
            {
                for (uint layer = 0; layer < this._effectiveArrayLayers; layer++)
                {
                    for (uint mipLevel = 1; mipLevel < MipLevels; mipLevel++)
                    {
                        uint subresource = CalculateSubresource(mipLevel, layer);
                        GetSubresourceLayout(subresource, out uint offset, out uint size, out _, out _);
                        Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);
                        gd.UpdateTexture(
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
            uint component)
        {
            int srcPixelOffset = (int)(
                srcBaseOffset
                + srcZ * srcDepthPitch
                + srcY * srcRowPitch
                + srcX * bytesPerPixel
                + component);
            return this._data[srcPixelOffset];
        }

        private ID3D12Resource CreateNativeTexture(D3D12GraphicsDevice gd, ref TextureDescription description)
        {
            bool isDepth = (description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil;
            uint effectiveDescriptionArrayLayers = GetEffectiveArrayLayers(description.Usage, description.ArrayLayers);
            Format dxgiFormat = D3D12Formats.ToDxgiFormat(description.Format, isDepth);
            ResourceFlags resourceFlags = D3D12Formats.ToResourceFlags(description.Usage);
            ResourceDescription resourceDescription;

            switch (description.Type)
            {
                case TextureType.Texture1D:
                    resourceDescription = ResourceDescription.Texture1D(
                        dxgiFormat,
                        description.Width,
                        (ushort)effectiveDescriptionArrayLayers,
                        (ushort)description.MipLevels,
                        resourceFlags,
                        TextureLayout.Unknown,
                        0);
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
                        resourceFlags,
                        TextureLayout.Unknown,
                        0);
                    break;
                case TextureType.Texture3D:
                    resourceDescription = ResourceDescription.Texture3D(
                        dxgiFormat,
                        description.Width,
                        description.Height,
                        (ushort)description.Depth,
                        (ushort)description.MipLevels,
                        resourceFlags,
                        TextureLayout.Unknown,
                        0);
                    break;
                default:
                    throw Illegal.Value<TextureType>();
            }

            ResourceStates initialState = ResourceStates.Common;
            if (isDepth)
            {
                initialState = ResourceStates.DepthWrite;
            }
            else if ((description.Usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget)
            {
                initialState = ResourceStates.RenderTarget;
            }

            return gd.Device.CreateCommittedResource(
                HeapType.Default,
                HeapFlags.None,
                resourceDescription,
                initialState,
                null);
        }

        private static ID3D12Resource CreateWrappedNativeTexture(ulong nativeHandle)
        {
            if (nativeHandle == 0)
            {
                throw new VeldridException("Native D3D12 texture handle cannot be 0.");
            }

            return new ID3D12Resource((IntPtr)nativeHandle);
        }

        private static void ValidateWrappedTextureDescription(ResourceDescription nativeDescription, ref TextureDescription description)
        {
            bool validDimension =
                (description.Type == TextureType.Texture1D && nativeDescription.Dimension == ResourceDimension.Texture1D)
                || (description.Type == TextureType.Texture2D && nativeDescription.Dimension == ResourceDimension.Texture2D)
                || (description.Type == TextureType.Texture3D && nativeDescription.Dimension == ResourceDimension.Texture3D);
            if (!validDimension)
            {
                throw new VeldridException("Wrapped native D3D12 texture dimension does not match TextureDescription.Type.");
            }

            if (nativeDescription.Width != description.Width || nativeDescription.Height != description.Height)
            {
                throw new VeldridException("Wrapped native D3D12 texture dimensions do not match TextureDescription.");
            }

            if (description.Type == TextureType.Texture3D)
            {
                if (nativeDescription.DepthOrArraySize != description.Depth)
                {
                    throw new VeldridException("Wrapped native D3D12 texture depth does not match TextureDescription.");
                }
            }
            else
            {
                uint expectedArrayLayers = GetEffectiveArrayLayers(description.Usage, description.ArrayLayers);
                if (nativeDescription.DepthOrArraySize != expectedArrayLayers)
                {
                    throw new VeldridException("Wrapped native D3D12 texture array layers do not match TextureDescription.");
                }
            }

            if (nativeDescription.MipLevels != description.MipLevels)
            {
                throw new VeldridException("Wrapped native D3D12 texture mip levels do not match TextureDescription.");
            }

            if ((uint)nativeDescription.SampleDescription.Count != FormatHelpers.GetSampleCountUInt32(description.SampleCount))
            {
                throw new VeldridException("Wrapped native D3D12 texture sample count does not match TextureDescription.");
            }

            if (!IsNativeFormatCompatible(nativeDescription.Format, ref description))
            {
                throw new VeldridException("Wrapped native D3D12 texture format does not match TextureDescription.");
            }

            ResourceFlags requiredFlags = D3D12Formats.ToResourceFlags(description.Usage);
            if ((requiredFlags & ResourceFlags.AllowRenderTarget) != 0
                && (nativeDescription.Flags & ResourceFlags.AllowRenderTarget) == 0)
            {
                throw new VeldridException("Wrapped native D3D12 texture is missing render-target capability.");
            }

            if ((requiredFlags & ResourceFlags.AllowDepthStencil) != 0
                && (nativeDescription.Flags & ResourceFlags.AllowDepthStencil) == 0)
            {
                throw new VeldridException("Wrapped native D3D12 texture is missing depth-stencil capability.");
            }

            if ((requiredFlags & ResourceFlags.AllowUnorderedAccess) != 0
                && (nativeDescription.Flags & ResourceFlags.AllowUnorderedAccess) == 0)
            {
                throw new VeldridException("Wrapped native D3D12 texture is missing unordered-access capability.");
            }
        }

        private static bool IsNativeFormatCompatible(Format nativeFormat, ref TextureDescription description)
        {
            bool depthUsage = (description.Usage & TextureUsage.DepthStencil) != 0;
            Format expectedResourceFormat = D3D12Formats.ToDxgiFormat(description.Format, depthUsage);
            if (nativeFormat == expectedResourceFormat)
            {
                return true;
            }

            if (D3D12Formats.GetViewFormat(nativeFormat) == D3D12Formats.GetViewFormat(expectedResourceFormat))
            {
                return true;
            }

            if (depthUsage)
            {
                Format expectedDepthFormat = D3D12Formats.ToDepthFormat(description.Format);
                if (nativeFormat == expectedDepthFormat)
                {
                    return true;
                }

                if (D3D12Formats.GetViewFormat(nativeFormat) == D3D12Formats.GetViewFormat(expectedDepthFormat))
                {
                    return true;
                }
            }

            return false;
        }

        private static ResourceStates GetCreatedTextureInitialState(TextureUsage usage)
        {
            if ((usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil)
            {
                return ResourceStates.DepthWrite;
            }

            if ((usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget)
            {
                return ResourceStates.RenderTarget;
            }

            return ResourceStates.Common;
        }

        private void InitializeSubresourceStates(ResourceStates initialState)
        {
            for (int i = 0; i < this._subresourceStates.Length; i++)
            {
                this._subresourceStates[i] = initialState;
            }

            this._hasCachedCommonState = true;
            this._cachedCommonState = initialState;
        }

        private void GetSubresourceLayout(uint subresource, out uint offset, out uint size, out uint rowPitch, out uint depthPitch)
        {
            uint totalOffset = 0;
            for (uint arrayLayer = 0; arrayLayer < this._effectiveArrayLayers; arrayLayer++)
            {
                uint mipWidth = Width;
                uint mipHeight = Height;
                uint mipDepth = Depth;
                for (uint mip = 0; mip < MipLevels; mip++)
                {
                    uint currentSubresource = arrayLayer * MipLevels + mip;
                    uint currentRowPitch = FormatHelpers.GetRowPitch(mipWidth, Format);
                    uint currentDepthPitch = FormatHelpers.GetDepthPitch(currentRowPitch, mipHeight, Format);
                    uint currentSize = currentDepthPitch * mipDepth;
                    if (currentSubresource == subresource)
                    {
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

        private static int ComputeTotalSize(ref TextureDescription description)
        {
            uint total = 0;
            uint effectiveDescriptionArrayLayers = GetEffectiveArrayLayers(description.Usage, description.ArrayLayers);
            uint width = description.Width;
            uint height = description.Height;
            uint depth = description.Depth;

            for (uint mip = 0; mip < description.MipLevels; mip++)
            {
                total += FormatHelpers.GetRegionSize(width, height, depth, description.Format) * effectiveDescriptionArrayLayers;
                width = Math.Max(1, width / 2);
                height = Math.Max(1, height / 2);
                depth = Math.Max(1, depth / 2);
            }

            return (int)total;
        }

        private static uint GetEffectiveArrayLayers(TextureUsage usage, uint arrayLayers)
        {
            if ((usage & TextureUsage.Cubemap) != 0)
            {
                return arrayLayers * 6;
            }

            return arrayLayers;
        }

        private bool IsStagingTexture() => (Usage & TextureUsage.Staging) == TextureUsage.Staging;

        private void SyncSubresourceToNative(uint subresource)
        {
            ResourceDescription textureDescription = NativeTexture.Description;
            var layouts = new PlacedSubresourceFootPrint[1];
            var rowCounts = new uint[1];
            var rowSizesInBytes = new ulong[1];
            gd.Device.GetCopyableFootprints(textureDescription, subresource, 1, 0, layouts, rowCounts, rowSizesInBytes, out ulong totalBytes);

            ID3D12Resource uploadBuffer = gd.Device.CreateCommittedResource(
                HeapType.Upload,
                HeapFlags.None,
                ResourceDescription.Buffer(totalBytes),
                ResourceStates.GenericRead,
                null);

            try
            {
                GetSubresourceLayout(subresource, out uint srcOffset, out _, out uint srcRowPitch, out uint srcDepthPitch);
                Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out _);
                Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);

                unsafe
                {
                    void* mappedUpload = null;
                    uploadBuffer.Map(0, &mappedUpload).CheckError();
                    try
                    {
                        fixed (byte* srcBase = this._data)
                        {
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
                                mipDepth,
                                Format);
                        }
                    }
                    finally
                    {
                        uploadBuffer.Unmap(0, null);
                    }
                }

                ExecuteTextureBufferCopy(
                    subresource,
                    ResourceStates.CopyDest,
                    previousState =>
                    {
                        var destination = new TextureCopyLocation(NativeTexture, subresource);
                        var sourceLocation = new TextureCopyLocation(uploadBuffer, layouts[0]);
                        return (destination, sourceLocation, sourceBox: (Box?)null, previousState);
                    },
                    copyToTexture: true);
            }
            finally
            {
                uploadBuffer.Dispose();
            }
        }

        private void SyncSubresourceFromNative(uint subresource)
        {
            ResourceDescription textureDescription = NativeTexture.Description;
            var layouts = new PlacedSubresourceFootPrint[1];
            var rowCounts = new uint[1];
            var rowSizesInBytes = new ulong[1];
            gd.Device.GetCopyableFootprints(textureDescription, subresource, 1, 0, layouts, rowCounts, rowSizesInBytes, out ulong totalBytes);

            ID3D12Resource readbackBuffer = gd.Device.CreateCommittedResource(
                HeapType.Readback,
                HeapFlags.None,
                ResourceDescription.Buffer(totalBytes),
                ResourceStates.CopyDest,
                null);

            try
            {
                GetSubresourceLayout(subresource, out uint dstOffset, out _, out uint dstRowPitch, out uint dstDepthPitch);
                Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out _);
                Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);

                ExecuteTextureBufferCopy(
                    subresource,
                    ResourceStates.CopySource,
                    previousState =>
                    {
                        var destination = new TextureCopyLocation(readbackBuffer, layouts[0]);
                        var sourceLocation = new TextureCopyLocation(NativeTexture, subresource);
                        var sourceBox = new Box(0, 0, 0, (int)mipWidth, (int)mipHeight, (int)mipDepth);
                        return (destination, sourceLocation, sourceBox, previousState);
                    },
                    copyToTexture: false);

                unsafe
                {
                    void* mappedReadback = null;
                    readbackBuffer.Map(0, &mappedReadback).CheckError();
                    try
                    {
                        fixed (byte* dstBase = this._data)
                        {
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
                                mipDepth,
                                Format);
                        }
                    }
                    finally
                    {
                        readbackBuffer.Unmap(0, null);
                    }
                }
            }
            finally
            {
                readbackBuffer.Dispose();
            }
        }

        private unsafe void ExecuteTextureBufferCopy(
            uint subresource,
            ResourceStates copyState,
            Func<ResourceStates, (TextureCopyLocation destination, TextureCopyLocation source, Box? sourceBox, ResourceStates previousState)> buildCopy,
            bool copyToTexture)
        {
            ID3D12CommandAllocator allocator = gd.Device.CreateCommandAllocator(CommandListType.Direct);
            ID3D12GraphicsCommandList commandList = gd.Device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, allocator, null);
            try
            {
                ResourceStates previousState = GetSubresourceState(subresource);
                if (previousState != copyState)
                {
                    ResourceBarrier toCopy = ResourceBarrier.BarrierTransition(
                        NativeTexture,
                        previousState,
                        copyState,
                        subresource,
                        ResourceBarrierFlags.None);
                    commandList.ResourceBarrier(new[] { toCopy });
                    SetSubresourceState(subresource, copyState);
                }

                var copyInfo = buildCopy(previousState);
                if (copyToTexture)
                {
                    if (copyInfo.sourceBox.HasValue)
                    {
                        commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source, copyInfo.sourceBox.Value);
                    }
                    else
                    {
                        commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source, null);
                    }
                }
                else
                {
                    if (copyInfo.sourceBox.HasValue)
                    {
                        commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source, copyInfo.sourceBox.Value);
                    }
                    else
                    {
                        commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source, null);
                    }
                }

                if (copyInfo.previousState != copyState)
                {
                    ResourceBarrier fromCopy = ResourceBarrier.BarrierTransition(
                        NativeTexture,
                        copyState,
                        copyInfo.previousState,
                        subresource,
                        ResourceBarrierFlags.None);
                    commandList.ResourceBarrier(new[] { fromCopy });
                    SetSubresourceState(subresource, copyInfo.previousState);
                }

                commandList.Close();
                gd.CommandQueue.ExecuteCommandList(commandList);
                gd.WaitForIdle();
            }
            finally
            {
                commandList.Dispose();
                allocator.Dispose();
            }
        }

        private protected override void DisposeCore()
        {
            if (this._mapped)
            {
                this._pinnedData.Free();
                this._mapped = false;
            }

            if (this._ownsNativeTexture)
            {
                NativeTexture?.Dispose();
            }
            this._disposed = true;
        }

        internal ResourceStates GetSubresourceState(uint subresource)
        {
            if (this._subresourceStates == null || subresource >= this._subresourceStates.Length)
            {
                return ResourceStates.Common;
            }

            return this._subresourceStates[subresource];
        }

        internal void SetSubresourceState(uint subresource, ResourceStates state)
        {
            if (this._subresourceStates == null || subresource >= this._subresourceStates.Length)
            {
                return;
            }

            ResourceStates previous = this._subresourceStates[subresource];
            if (previous == state)
            {
                return;
            }

            this._subresourceStates[subresource] = state;
            if (this._hasCachedCommonState && state != this._cachedCommonState)
            {
                this._hasCachedCommonState = false;
            }
        }

        internal void SetAllSubresourceStates(ResourceStates state)
        {
            if (this._subresourceStates == null)
            {
                return;
            }

            for (int i = 0; i < this._subresourceStates.Length; i++)
            {
                this._subresourceStates[i] = state;
            }

            this._hasCachedCommonState = true;
            this._cachedCommonState = state;
        }

        internal bool TryGetCommonState(out ResourceStates state)
        {
            if (this._subresourceStates == null || this._subresourceStates.Length == 0)
            {
                state = ResourceStates.Common;
                return true;
            }

            if (this._hasCachedCommonState)
            {
                state = this._cachedCommonState;
                return true;
            }

            state = this._subresourceStates[0];
            for (int i = 1; i < this._subresourceStates.Length; i++)
            {
                if (this._subresourceStates[i] != state)
                {
                    return false;
                }
            }

            this._hasCachedCommonState = true;
            this._cachedCommonState = state;
            return true;
        }
    }
}
