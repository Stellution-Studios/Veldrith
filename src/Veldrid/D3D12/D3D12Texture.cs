using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Veldrid.D3D12
{
    internal sealed class D3D12Texture : Texture
    {
        private readonly D3D12GraphicsDevice gd;
        private readonly byte[] data;
        private readonly bool ownsNativeTexture;
        private readonly ResourceStates[] subresourceStates;
        private GCHandle pinnedData;
        private MapMode? activeMapMode;
        private uint activeMapSubresource;
        private bool mapped;
        private bool disposed;
        private string name;

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
            data = new byte[computeTotalSize(ref description)];
            subresourceStates = new ResourceStates[MipLevels * ArrayLayers];

            if (nativeHandle == null)
            {
                NativeTexture = createNativeTexture(gd, ref description);
                ownsNativeTexture = true;
                initializeSubresourceStates(getCreatedTextureInitialState(description.Usage));
            }
            else
            {
                NativeTexture = createWrappedNativeTexture(nativeHandle.Value);
                ownsNativeTexture = false;
                validateWrappedTextureDescription(NativeTexture.Description, ref description);
                initializeSubresourceStates(ResourceStates.Common);
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
        public override bool IsDisposed => disposed;
        internal ID3D12Resource NativeTexture { get; }
        internal ResourceStates CurrentState
        {
            get
            {
                if (subresourceStates == null || subresourceStates.Length == 0)
                {
                    return ResourceStates.Common;
                }

                return subresourceStates[0];
            }
            set => SetAllSubresourceStates(value);
        }
        internal uint SubresourceCount => (uint)(subresourceStates?.Length ?? 0);

        public override string Name
        {
            get => name;
            set => name = value;
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

            getSubresourceLayout(subresource, out uint dstOffset, out uint dstSize, out uint dstRowPitch, out uint dstDepthPitch);
            if (dstOffset + dstSize > (uint)data.Length)
            {
                throw new VeldridException("Texture update destination region exceeds texture storage.");
            }

            uint srcRowPitch = FormatHelpers.GetRowPitch(width, Format);
            uint srcDepthPitch = FormatHelpers.GetDepthPitch(srcRowPitch, height, Format);
            unsafe
            {
                fixed (byte* dstBase = data)
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

        internal MappedResource Map(MapMode mode, uint subresource)
        {
            if (subresource >= MipLevels * ArrayLayers)
            {
                throw new VeldridException("Subresource index is out of bounds.");
            }

            if (isStagingTexture() && NativeTexture != null && (mode == MapMode.Read || mode == MapMode.ReadWrite))
            {
                syncSubresourceFromNative(subresource);
            }

            if (!mapped)
            {
                pinnedData = GCHandle.Alloc(data, GCHandleType.Pinned);
                mapped = true;
            }

            activeMapMode = mode;
            activeMapSubresource = subresource;
            getSubresourceLayout(subresource, out uint offset, out uint size, out uint rowPitch, out uint depthPitch);
            IntPtr dataPtr = IntPtr.Add(pinnedData.AddrOfPinnedObject(), (int)offset);
            return new MappedResource(this, mode, dataPtr, size, subresource, rowPitch, depthPitch);
        }

        internal void Unmap()
        {
            if (mapped
                && isStagingTexture()
                && NativeTexture != null
                && activeMapMode.HasValue
                && (activeMapMode.Value == MapMode.Write || activeMapMode.Value == MapMode.ReadWrite))
            {
                syncSubresourceToNative(activeMapSubresource);
            }

            activeMapMode = null;
            if (mapped)
            {
                pinnedData.Free();
                mapped = false;
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
                getSubresourceLayout(srcSubresource, out uint srcBaseOffset, out _, out uint srcRowPitch, out uint srcDepthPitch);
                destination.getSubresourceLayout(dstSubresource, out uint dstBaseOffset, out _, out uint dstRowPitch, out uint dstDepthPitch);

                unsafe
                {
                    fixed (byte* srcBase = data)
                    {
                        fixed (byte* dstBase = destination.data)
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

            for (uint layer = 0; layer < ArrayLayers; layer++)
            {
                for (uint mipLevel = 1; mipLevel < MipLevels; mipLevel++)
                {
                    uint srcSubresource = CalculateSubresource(mipLevel - 1, layer);
                    uint dstSubresource = CalculateSubresource(mipLevel, layer);

                    getSubresourceLayout(srcSubresource, out uint srcBaseOffset, out _, out uint srcRowPitch, out uint srcDepthPitch);
                    getSubresourceLayout(dstSubresource, out uint dstBaseOffset, out _, out uint dstRowPitch, out uint dstDepthPitch);

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

                                    sum += getSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY0, srcZ0, bytesPerPixel, component); sampleCount++;
                                    sum += getSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY0, srcZ0, bytesPerPixel, component); sampleCount++;
                                    sum += getSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY1, srcZ0, bytesPerPixel, component); sampleCount++;
                                    sum += getSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY1, srcZ0, bytesPerPixel, component); sampleCount++;

                                    if (srcDepth > 1)
                                    {
                                        sum += getSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY0, srcZ1, bytesPerPixel, component); sampleCount++;
                                        sum += getSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY0, srcZ1, bytesPerPixel, component); sampleCount++;
                                        sum += getSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY1, srcZ1, bytesPerPixel, component); sampleCount++;
                                        sum += getSourceByte(srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY1, srcZ1, bytesPerPixel, component); sampleCount++;
                                    }

                                    data[dstPixelOffset + component] = (byte)(sum / sampleCount);
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

            fixed (byte* dataPtr = data)
            {
                for (uint layer = 0; layer < ArrayLayers; layer++)
                {
                    for (uint mipLevel = 1; mipLevel < MipLevels; mipLevel++)
                    {
                        uint subresource = CalculateSubresource(mipLevel, layer);
                        getSubresourceLayout(subresource, out uint offset, out uint size, out _, out _);
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

        private uint getSourceByte(
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
            return data[srcPixelOffset];
        }

        private ID3D12Resource createNativeTexture(D3D12GraphicsDevice gd, ref TextureDescription description)
        {
            bool isDepth = (description.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil;
            Format dxgiFormat = D3D12Formats.ToDxgiFormat(description.Format, isDepth);
            ResourceFlags resourceFlags = D3D12Formats.ToResourceFlags(description.Usage);
            ResourceDescription resourceDescription;

            switch (description.Type)
            {
                case TextureType.Texture1D:
                    resourceDescription = ResourceDescription.Texture1D(
                        dxgiFormat,
                        description.Width,
                        (ushort)description.ArrayLayers,
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
                        (ushort)description.ArrayLayers,
                        (ushort)description.MipLevels,
                        (uint)description.SampleCount,
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

        private static ID3D12Resource createWrappedNativeTexture(ulong nativeHandle)
        {
            if (nativeHandle == 0)
            {
                throw new VeldridException("Native D3D12 texture handle cannot be 0.");
            }

            return new ID3D12Resource((IntPtr)nativeHandle);
        }

        private static void validateWrappedTextureDescription(ResourceDescription nativeDescription, ref TextureDescription description)
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
            else if (nativeDescription.DepthOrArraySize != description.ArrayLayers)
            {
                throw new VeldridException("Wrapped native D3D12 texture array layers do not match TextureDescription.");
            }

            if (nativeDescription.MipLevels != description.MipLevels)
            {
                throw new VeldridException("Wrapped native D3D12 texture mip levels do not match TextureDescription.");
            }

            if ((uint)nativeDescription.SampleDescription.Count != FormatHelpers.GetSampleCountUInt32(description.SampleCount))
            {
                throw new VeldridException("Wrapped native D3D12 texture sample count does not match TextureDescription.");
            }

            if (!isNativeFormatCompatible(nativeDescription.Format, ref description))
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

        private static bool isNativeFormatCompatible(Format nativeFormat, ref TextureDescription description)
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

        private static ResourceStates getCreatedTextureInitialState(TextureUsage usage)
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

        private void initializeSubresourceStates(ResourceStates initialState)
        {
            for (int i = 0; i < subresourceStates.Length; i++)
            {
                subresourceStates[i] = initialState;
            }
        }

        private void getSubresourceLayout(uint subresource, out uint offset, out uint size, out uint rowPitch, out uint depthPitch)
        {
            uint totalOffset = 0;
            for (uint arrayLayer = 0; arrayLayer < ArrayLayers; arrayLayer++)
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

        private static int computeTotalSize(ref TextureDescription description)
        {
            uint total = 0;
            uint width = description.Width;
            uint height = description.Height;
            uint depth = description.Depth;

            for (uint mip = 0; mip < description.MipLevels; mip++)
            {
                total += FormatHelpers.GetRegionSize(width, height, depth, description.Format) * description.ArrayLayers;
                width = Math.Max(1, width / 2);
                height = Math.Max(1, height / 2);
                depth = Math.Max(1, depth / 2);
            }

            return (int)total;
        }

        private bool isStagingTexture() => (Usage & TextureUsage.Staging) == TextureUsage.Staging;

        private void syncSubresourceToNative(uint subresource)
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
                getSubresourceLayout(subresource, out uint srcOffset, out _, out uint srcRowPitch, out uint srcDepthPitch);
                Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out _);
                Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);

                unsafe
                {
                    void* mappedUpload = null;
                    uploadBuffer.Map(0, &mappedUpload).CheckError();
                    try
                    {
                        fixed (byte* srcBase = data)
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

                executeTextureBufferCopy(
                    subresource,
                    ResourceStates.CopyDest,
                    previousState =>
                    {
                        var destination = new TextureCopyLocation(NativeTexture, subresource);
                        var sourceLocation = new TextureCopyLocation(uploadBuffer, layouts[0]);
                        var sourceBox = new Box(0, 0, 0, (int)mipWidth, (int)mipHeight, (int)mipDepth);
                        return (destination, sourceLocation, sourceBox, previousState);
                    },
                    copyToTexture: true);
            }
            finally
            {
                uploadBuffer.Dispose();
            }
        }

        private void syncSubresourceFromNative(uint subresource)
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
                getSubresourceLayout(subresource, out uint dstOffset, out _, out uint dstRowPitch, out uint dstDepthPitch);
                Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out _);
                Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);

                executeTextureBufferCopy(
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
                        fixed (byte* dstBase = data)
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

        private unsafe void executeTextureBufferCopy(
            uint subresource,
            ResourceStates copyState,
            Func<ResourceStates, (TextureCopyLocation destination, TextureCopyLocation source, Box sourceBox, ResourceStates previousState)> buildCopy,
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
                    commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source, copyInfo.sourceBox);
                }
                else
                {
                    commandList.CopyTextureRegion(copyInfo.destination, 0, 0, 0, copyInfo.source, copyInfo.sourceBox);
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
            if (mapped)
            {
                pinnedData.Free();
                mapped = false;
            }

            if (ownsNativeTexture)
            {
                NativeTexture?.Dispose();
            }
            disposed = true;
        }

        internal ResourceStates GetSubresourceState(uint subresource)
        {
            if (subresourceStates == null || subresource >= subresourceStates.Length)
            {
                return ResourceStates.Common;
            }

            return subresourceStates[subresource];
        }

        internal void SetSubresourceState(uint subresource, ResourceStates state)
        {
            if (subresourceStates == null || subresource >= subresourceStates.Length)
            {
                return;
            }

            subresourceStates[subresource] = state;
        }

        internal void SetAllSubresourceStates(ResourceStates state)
        {
            if (subresourceStates == null)
            {
                return;
            }

            for (int i = 0; i < subresourceStates.Length; i++)
            {
                subresourceStates[i] = state;
            }
        }

        internal bool TryGetCommonState(out ResourceStates state)
        {
            if (subresourceStates == null || subresourceStates.Length == 0)
            {
                state = ResourceStates.Common;
                return true;
            }

            state = subresourceStates[0];
            for (int i = 1; i < subresourceStates.Length; i++)
            {
                if (subresourceStates[i] != state)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
