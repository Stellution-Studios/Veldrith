using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12Texture.
/// </summary>
internal sealed class D3D12Texture : Texture {

    /// <summary>
    /// Stores the data state used by this instance.
    /// </summary>
    private readonly byte[] _data;

    /// <summary>
    /// Reuses a single barrier array for immediate texture upload transitions.
    /// </summary>
    private readonly ResourceBarrier[] _singleBarrier = new ResourceBarrier[1];

    /// <summary>
    /// Stores the owns native texture state used by this instance.
    /// </summary>
    private readonly bool _ownsNativeTexture;

    /// <summary>
    /// Stores the subresource states collection used by this instance.
    /// </summary>
    private readonly ResourceStates[] _subresourceStates;

    /// <summary>
    /// Stores the placed-resource allocation block when this texture is allocated from the D3D12 memory manager.
    /// </summary>
    private D3D12ResourceAllocation _allocation;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Stores the active map mode state used by this instance.
    /// </summary>
    private MapMode? _activeMapMode;

    /// <summary>
    /// Stores the active map subresource state used by this instance.
    /// </summary>
    private uint _activeMapSubresource;

    /// <summary>
    /// Caches cached common state to reduce repeated allocations and lookups.
    /// </summary>
    private ResourceStates _cachedCommonState;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Caches has cached common state to reduce repeated allocations and lookups.
    /// </summary>
    private bool _hasCachedCommonState;

    /// <summary>
    /// Tracks subresource-state changes observed by TextureView transition caches.
    /// </summary>
    private ulong _stateVersion;

    /// <summary>
    /// Stores the mapped state used by this instance.
    /// </summary>
    private bool _mapped;

    /// <summary>
    /// Stores the pinned data state used by this instance.
    /// </summary>
    private GCHandle _pinnedData;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Texture" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <param name="nativeHandle">The native handle value used by this operation.</param>
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
        this._data = RequiresCpuStorage(description.Usage) ? new byte[ComputeTotalSize(ref description)] : null;
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
    /// Gets the version that changes whenever a tracked subresource state changes.
    /// </summary>
    internal ulong StateVersion => this._stateVersion;

    /// <summary>
    /// Gets or sets EffectiveArrayLayers.
    /// </summary>
    internal uint EffectiveArrayLayers { get; }

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Updates the value state for this command sequence.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    internal void Update(IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
        byte[] data = this.RequireCpuStorage();
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
        if (dstOffset + dstSize > (uint)data.Length) {
            throw new VeldridException("Texture update destination region exceeds texture storage.");
        }

        uint srcRowPitch = FormatHelpers.GetRowPitch(width, this.Format);
        uint srcDepthPitch = FormatHelpers.GetDepthPitch(srcRowPitch, height, this.Format);
        unsafe {
            fixed (byte* dstBase = data) {
                Util.CopyTextureRegion(source.ToPointer(), 0, 0, 0, srcRowPitch, srcDepthPitch, dstBase + dstOffset, x, y, z, dstRowPitch, dstDepthPitch, width, height, depth, this.Format);
            }
        }
    }

    /// <summary>
    /// Updates the native subresource state for this command sequence.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="sizeInBytes">The size, in bytes, used by this operation.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    internal void UpdateNativeSubresource(IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
        if (this.IsStagingTexture()) {
            this.Update(source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
            if (this.NativeTexture != null) {
                uint subresource = this.CalculateSubresource(mipLevel, arrayLayer);
                this.SyncSubresourceToNative(subresource);
            }

            return;
        }

        this.UploadRegionToNative(source, sizeInBytes, x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    /// <summary>
    /// Maps the value resource for CPU access.
    /// </summary>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal MappedResource Map(MapMode mode, uint subresource) {
        byte[] data = this.RequireCpuStorage();
        if (subresource >= this.SubresourceCount) {
            throw new VeldridException("Subresource index is out of bounds.");
        }

        if (this.IsStagingTexture() && this.NativeTexture != null && (mode == MapMode.Read || mode == MapMode.ReadWrite)) {
            this.SyncSubresourceFromNative(subresource);
        }

        if (!this._mapped) {
            this._pinnedData = GCHandle.Alloc(data, GCHandleType.Pinned);
            this._mapped = true;
        }

        this._activeMapMode = mode;
        this._activeMapSubresource = subresource;
        this.GetSubresourceLayout(subresource, out uint offset, out uint size, out uint rowPitch, out uint depthPitch);
        IntPtr dataPtr = IntPtr.Add(this._pinnedData.AddrOfPinnedObject(), (int)offset);
        return new MappedResource(this, mode, dataPtr, size, subresource, rowPitch, depthPitch);
    }

    /// <summary>
    /// Unmaps the value resource from CPU access.
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
    /// Copies region to data between resources.
    /// </summary>
    /// <param name="destination">The destination value or resource.</param>
    /// <param name="srcX">The src x value used by this operation.</param>
    /// <param name="srcY">The src y value used by this operation.</param>
    /// <param name="srcZ">The src z value used by this operation.</param>
    /// <param name="srcMipLevel">The src mip level value used by this operation.</param>
    /// <param name="srcBaseArrayLayer">The src base array layer value used by this operation.</param>
    /// <param name="dstX">The dst x value used by this operation.</param>
    /// <param name="dstY">The dst y value used by this operation.</param>
    /// <param name="dstZ">The dst z value used by this operation.</param>
    /// <param name="dstMipLevel">The dst mip level value used by this operation.</param>
    /// <param name="dstBaseArrayLayer">The dst base array layer value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="layerCount">The layer count value used by this operation.</param>
    internal void CopyRegionTo(D3D12Texture destination, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount) {
        byte[] srcData = this.RequireCpuStorage();
        byte[] dstData = destination.RequireCpuStorage();
        if (this.Format != destination.Format) {
            throw new VeldridException("Source and destination texture formats must match.");
        }

        for (uint layer = 0; layer < layerCount; layer++) {
            uint srcSubresource = (srcBaseArrayLayer + layer) * this.MipLevels + srcMipLevel;
            uint dstSubresource = (dstBaseArrayLayer + layer) * destination.MipLevels + dstMipLevel;
            this.GetSubresourceLayout(srcSubresource, out uint srcBaseOffset, out _, out uint srcRowPitch, out uint srcDepthPitch);
            destination.GetSubresourceLayout(dstSubresource, out uint dstBaseOffset, out _, out uint dstRowPitch, out uint dstDepthPitch);

            unsafe {
                fixed (byte* srcBase = srcData) {
                    fixed (byte* dstBase = dstData) {
                        byte* srcSubresourcePtr = srcBase + srcBaseOffset;
                        byte* dstSubresourcePtr = dstBase + dstBaseOffset;
                        Util.CopyTextureRegion(srcSubresourcePtr, srcX, srcY, srcZ, srcRowPitch, srcDepthPitch, dstSubresourcePtr, dstX, dstY, dstZ, dstRowPitch, dstDepthPitch, width, height, depth, this.Format);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Executes the generate mipmaps cpu logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool GenerateMipmapsCpu() {
        byte[] data = this._data;
        if (data == null) {
            return false;
        }

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

                                sum += GetSourceByte(data, srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY0, srcZ0, bytesPerPixel, component);
                                sampleCount++;
                                sum += GetSourceByte(data, srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY0, srcZ0, bytesPerPixel, component);
                                sampleCount++;
                                sum += GetSourceByte(data, srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY1, srcZ0, bytesPerPixel, component);
                                sampleCount++;
                                sum += GetSourceByte(data, srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY1, srcZ0, bytesPerPixel, component);
                                sampleCount++;

                                if (srcDepth > 1) {
                                    sum += GetSourceByte(data, srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY0, srcZ1, bytesPerPixel, component);
                                    sampleCount++;
                                    sum += GetSourceByte(data, srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY0, srcZ1, bytesPerPixel, component);
                                    sampleCount++;
                                    sum += GetSourceByte(data, srcBaseOffset, srcRowPitch, srcDepthPitch, srcX0, srcY1, srcZ1, bytesPerPixel, component);
                                    sampleCount++;
                                    sum += GetSourceByte(data, srcBaseOffset, srcRowPitch, srcDepthPitch, srcX1, srcY1, srcZ1, bytesPerPixel, component);
                                    sampleCount++;
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

    /// <summary>
    /// Executes the upload generated mipmaps logic for this backend.
    /// </summary>
    internal unsafe void UploadGeneratedMipmaps() {
        byte[] data = this._data;
        if (data == null || this.NativeTexture == null || this.MipLevels <= 1) {
            return;
        }

        fixed (byte* dataPtr = data) {
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
    /// Gets the source byte value.
    /// </summary>
    /// <param name="srcBaseOffset">The src base offset value used by this operation.</param>
    /// <param name="srcRowPitch">The src row pitch value used by this operation.</param>
    /// <param name="srcDepthPitch">The src depth pitch value used by this operation.</param>
    /// <param name="srcX">The src x value used by this operation.</param>
    /// <param name="srcY">The src y value used by this operation.</param>
    /// <param name="srcZ">The src z value used by this operation.</param>
    /// <param name="bytesPerPixel">The bytes per pixel value used by this operation.</param>
    /// <param name="component">The component value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static uint GetSourceByte(byte[] data, uint srcBaseOffset, uint srcRowPitch, uint srcDepthPitch, uint srcX, uint srcY, uint srcZ, uint bytesPerPixel, uint component) {
        int srcPixelOffset = (int)(srcBaseOffset
            + srcZ * srcDepthPitch
            + srcY * srcRowPitch
            + srcX * bytesPerPixel
            + component);
        return data[srcPixelOffset];
    }

    /// <summary>
    /// Creates the native texture instance used by this backend.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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

        HeapFlags heapFlags = GetHeapFlags(resourceFlags);
        this._allocation = gd.MemoryManager.CreateResource(ref resourceDescription, initialState, HeapType.Default, heapFlags);
        return this._allocation.Resource;
    }

    /// <summary>
    /// Creates the wrapped native texture instance used by this backend.
    /// </summary>
    /// <param name="nativeHandle">The native handle value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static ID3D12Resource CreateWrappedNativeTexture(ulong nativeHandle) {
        if (nativeHandle == 0) {
            throw new VeldridException("Native D3D12 texture handle cannot be 0.");
        }

        return new ID3D12Resource((IntPtr)nativeHandle);
    }

    /// <summary>
    /// Executes the validate wrapped texture description logic for this backend.
    /// </summary>
    /// <param name="nativeDescription">The native description value used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
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
    /// Executes the is native format compatible logic for this backend.
    /// </summary>
    /// <param name="nativeFormat">The native format value used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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
    /// Gets the created texture initial state value.
    /// </summary>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the initialize subresource states logic for this backend.
    /// </summary>
    /// <param name="initialState">The initial state value used by this operation.</param>
    private void InitializeSubresourceStates(ResourceStates initialState) {
        for (int i = 0; i < this._subresourceStates.Length; i++) {
            this._subresourceStates[i] = initialState;
        }

        this._hasCachedCommonState = true;
        this._cachedCommonState = initialState;
    }

    /// <summary>
    /// Gets the subresource layout value.
    /// </summary>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <param name="offset">The byte offset used by this operation.</param>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    /// <param name="rowPitch">The row pitch value used by this operation.</param>
    /// <param name="depthPitch">The depth pitch value used by this operation.</param>
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
    /// Computes the total size value.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Aligns a value upward to the specified alignment.
    /// </summary>
    /// <param name="value">The value to align.</param>
    /// <param name="alignment">The alignment boundary.</param>
    /// <returns>The aligned value.</returns>
    private static uint AlignUp(uint value, uint alignment) {
        return (value + alignment - 1) / alignment * alignment;
    }

    /// <summary>
    /// Gets the effective array layers value.
    /// </summary>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="arrayLayers">The array layers value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static uint GetEffectiveArrayLayers(TextureUsage usage, uint arrayLayers) {
        if ((usage & TextureUsage.Cubemap) != 0) {
            return arrayLayers * 6;
        }

        return arrayLayers;
    }

    /// <summary>
    /// Executes the is staging texture logic for this backend.
    /// </summary>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool IsStagingTexture() {
        return (this.Usage & TextureUsage.Staging) == TextureUsage.Staging;
    }

    /// <summary>
    /// Determines whether a texture needs CPU-side storage for mapping and staging behavior.
    /// </summary>
    /// <param name="usage">The texture usage flags.</param>
    /// <returns><see langword="true" /> when CPU storage is required.</returns>
    private static bool RequiresCpuStorage(TextureUsage usage) {
        return (usage & TextureUsage.Staging) == TextureUsage.Staging;
    }

    /// <summary>
    /// Gets CPU-side texture storage or throws when the texture is GPU-only.
    /// </summary>
    /// <returns>The CPU-side storage buffer.</returns>
    private byte[] RequireCpuStorage() {
        if (this._data == null) {
            throw new VeldridException("This D3D12 texture does not have CPU-side storage. Use a staging texture for CPU mapping or CPU copies.");
        }

        return this._data;
    }

    /// <summary>
    /// Uploads a packed source region directly into the native D3D12 texture without keeping a CPU-side mirror.
    /// </summary>
    /// <param name="source">The packed source data.</param>
    /// <param name="sizeInBytes">The size of the packed source data in bytes.</param>
    /// <param name="x">The destination X coordinate.</param>
    /// <param name="y">The destination Y coordinate.</param>
    /// <param name="z">The destination Z coordinate.</param>
    /// <param name="width">The region width.</param>
    /// <param name="height">The region height.</param>
    /// <param name="depth">The region depth.</param>
    /// <param name="mipLevel">The target mip level.</param>
    /// <param name="arrayLayer">The target array layer.</param>
    private void UploadRegionToNative(IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer) {
        if (this.NativeTexture == null) {
            return;
        }

        Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);
        if (x + width > mipWidth || y + height > mipHeight || z + depth > mipDepth) {
            throw new VeldridException("Texture update region exceeds texture bounds.");
        }

        uint sourceRowPitch = FormatHelpers.GetRowPitch(width, this.Format);
        uint sourceRowCount = FormatHelpers.GetNumRows(height, this.Format);
        uint sourceDepthPitch = sourceRowPitch * sourceRowCount;
        ulong requiredBytes = (ulong)sourceDepthPitch * depth;
        if (sizeInBytes < requiredBytes) {
            throw new VeldridException("Texture update source size is smaller than required for the destination region.");
        }

        uint uploadRowPitch = AlignUp(sourceRowPitch, Vortice.Direct3D12.D3D12.TextureDataPitchAlignment);
        ulong uploadDepthPitch = (ulong)uploadRowPitch * sourceRowCount;
        ulong totalBytes = uploadDepthPitch * depth;
        D3D12ResourceAllocation uploadBuffer = this.gd.RentUploadBuffer(totalBytes, Vortice.Direct3D12.D3D12.TextureDataPlacementAlignment);
        bool uploadEnqueuedForDeferredDisposal = false;

        try {
            unsafe {
                byte* srcBase = (byte*)source.ToPointer();
                byte* dstBase = (byte*)uploadBuffer.MappedPointer.ToPointer();
                for (uint slice = 0; slice < depth; slice++) {
                    for (uint row = 0; row < sourceRowCount; row++) {
                        byte* srcRow = srcBase + slice * sourceDepthPitch + row * sourceRowPitch;
                        byte* dstRow = dstBase + slice * uploadDepthPitch + row * uploadRowPitch;
                        Buffer.MemoryCopy(srcRow, dstRow, uploadRowPitch, sourceRowPitch);
                    }
                }
            }

            bool depthUsage = (this.Usage & TextureUsage.DepthStencil) != 0;
            PlacedSubresourceFootPrint footprint = new() {
                Offset = uploadBuffer.Offset,
                Footprint = new SubresourceFootPrint(D3D12Formats.ToDxgiFormat(this.Format, depthUsage), width, height, depth, uploadRowPitch)
            };
            uint subresource = this.CalculateSubresource(mipLevel, arrayLayer);
            ulong signalValue = this.ExecuteTextureBufferCopy(subresource, ResourceStates.CopyDest, previousState => {
                TextureCopyLocation destination = new(this.NativeTexture, subresource);
                TextureCopyLocation sourceLocation = new(uploadBuffer.Resource, footprint);
                return (destination, sourceLocation, sourceBox: null, previousState);
            }, true, uploadBuffer, x, y, z);
            if (signalValue == 0) {
                uploadBuffer = null;
            }
            else {
                this.gd.EnqueueImmediateUploadBuffer(uploadBuffer, signalValue);
                uploadBuffer = null;
            }

            uploadEnqueuedForDeferredDisposal = true;
        }
        finally {
            if (!uploadEnqueuedForDeferredDisposal) {
                this.gd.ReturnUploadBuffer(uploadBuffer);
            }
        }
    }

    /// <summary>
    /// Executes the sync subresource to native logic for this backend.
    /// </summary>
    /// <param name="subresource">The subresource value used by this operation.</param>
    private void SyncSubresourceToNative(uint subresource) {
        byte[] data = this.RequireCpuStorage();
        ResourceDescription textureDescription = this.NativeTexture.Description;
        PlacedSubresourceFootPrint[] layouts = new PlacedSubresourceFootPrint[1];
        uint[] rowCounts = new uint[1];
        ulong[] rowSizesInBytes = new ulong[1];
        this.gd.Device.GetCopyableFootprints(textureDescription, subresource, 1, 0, layouts, rowCounts, rowSizesInBytes, out ulong totalBytes);

        D3D12ResourceAllocation uploadBuffer = this.gd.RentUploadBuffer(totalBytes, Vortice.Direct3D12.D3D12.TextureDataPlacementAlignment);
        bool uploadEnqueuedForDeferredDisposal = false;

        try {
            this.GetSubresourceLayout(subresource, out uint srcOffset, out _, out uint srcRowPitch, out uint srcDepthPitch);
            Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out _);
            Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);

            unsafe {
                fixed (byte* srcBase = data) {
                    byte* srcSubresource = srcBase + srcOffset;
                    byte* dstUpload = (byte*)uploadBuffer.MappedPointer.ToPointer() + layouts[0].Offset;
                    uint dstRowPitch = layouts[0].Footprint.RowPitch;
                    uint dstDepthPitch = dstRowPitch * rowCounts[0];
                    Util.CopyTextureRegion(srcSubresource, 0, 0, 0, srcRowPitch, srcDepthPitch, dstUpload, 0, 0, 0, dstRowPitch, dstDepthPitch, mipWidth, mipHeight, mipDepth, this.Format);
                }
            }

            ulong signalValue = this.ExecuteTextureBufferCopy(subresource, ResourceStates.CopyDest, previousState => {
                TextureCopyLocation destination = new(this.NativeTexture, subresource);
                PlacedSubresourceFootPrint sourceFootprint = layouts[0];
                sourceFootprint.Offset += uploadBuffer.Offset;
                TextureCopyLocation sourceLocation = new(uploadBuffer.Resource, sourceFootprint);
                return (destination, sourceLocation, sourceBox: null, previousState);
            }, true, uploadBuffer);
            if (signalValue == 0) {
                uploadBuffer = null;
            }
            else {
                this.gd.EnqueueImmediateUploadBuffer(uploadBuffer, signalValue);
                uploadBuffer = null;
            }

            uploadEnqueuedForDeferredDisposal = true;
        }
        finally {
            if (!uploadEnqueuedForDeferredDisposal) {
                this.gd.ReturnUploadBuffer(uploadBuffer);
            }
        }
    }

    /// <summary>
    /// Executes the sync subresource from native logic for this backend.
    /// </summary>
    /// <param name="subresource">The subresource value used by this operation.</param>
    private void SyncSubresourceFromNative(uint subresource) {
        byte[] data = this.RequireCpuStorage();
        ResourceDescription textureDescription = this.NativeTexture.Description;
        PlacedSubresourceFootPrint[] layouts = new PlacedSubresourceFootPrint[1];
        uint[] rowCounts = new uint[1];
        ulong[] rowSizesInBytes = new ulong[1];
        this.gd.Device.GetCopyableFootprints(textureDescription, subresource, 1, 0, layouts, rowCounts, rowSizesInBytes, out ulong totalBytes);

        ResourceDescription readbackDescription = ResourceDescription.Buffer(totalBytes);
        D3D12ResourceAllocation readbackAllocation = this.gd.MemoryManager.CreateResource(ref readbackDescription, ResourceStates.CopyDest, HeapType.Readback, HeapFlags.AllowOnlyBuffers);
        ID3D12Resource readbackBuffer = readbackAllocation.Resource;

        try {
            this.GetSubresourceLayout(subresource, out uint dstOffset, out _, out uint dstRowPitch, out uint dstDepthPitch);
            Util.GetMipLevelAndArrayLayer(this, subresource, out uint mipLevel, out _);
            Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint mipDepth);

            _ = this.ExecuteTextureBufferCopy(subresource, ResourceStates.CopySource, previousState => {
                TextureCopyLocation destination = new(readbackBuffer, layouts[0]);
                TextureCopyLocation sourceLocation = new(this.NativeTexture, subresource);
                Box sourceBox = new(0, 0, 0, (int)mipWidth, (int)mipHeight, (int)mipDepth);
                return (destination, sourceLocation, sourceBox, previousState);
            }, false);

            unsafe {
                fixed (byte* dstBase = data) {
                    byte* srcReadback = (byte*)readbackAllocation.MappedPointer.ToPointer() + layouts[0].Offset;
                    byte* dstSubresource = dstBase + dstOffset;
                    uint srcRowPitch = layouts[0].Footprint.RowPitch;
                    uint srcDepthPitch = srcRowPitch * rowCounts[0];
                    Util.CopyTextureRegion(srcReadback, 0, 0, 0, srcRowPitch, srcDepthPitch, dstSubresource, 0, 0, 0, dstRowPitch, dstDepthPitch, mipWidth, mipHeight, mipDepth, this.Format);
                }
            }
        }
        finally {
            readbackAllocation.Dispose();
        }
    }

    /// <summary>
    /// Executes the execute texture buffer copy logic for this backend.
    /// </summary>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <param name="copyState">The copy state value used by this operation.</param>
    /// <param name="buildCopy">The build copy value used by this operation.</param>
    /// <param name="copyToTexture">The copy to texture value used by this operation.</param>
    /// <returns>The fence value signaled for this copy submission.</returns>
    private ulong ExecuteTextureBufferCopy(uint subresource, ResourceStates copyState, Func<ResourceStates, (TextureCopyLocation destination, TextureCopyLocation source, Box? sourceBox, ResourceStates previousState)> buildCopy, bool copyToTexture, D3D12ResourceAllocation uploadBuffer = null, uint destinationX = 0, uint destinationY = 0, uint destinationZ = 0) {
        void RecordCopy(ID3D12GraphicsCommandList commandList) {
            ResourceStates previousState = this.GetSubresourceState(subresource);
            if (previousState != copyState) {
                ResourceBarrier toCopy = ResourceBarrier.BarrierTransition(this.NativeTexture, previousState, copyState, subresource);
                this._singleBarrier[0] = toCopy;
                commandList.ResourceBarrier(this._singleBarrier);
                this.SetSubresourceState(subresource, copyState);
            }

            (TextureCopyLocation destination, TextureCopyLocation source, Box? sourceBox, ResourceStates previousState)
                copyInfo = buildCopy(previousState);
            if (copyToTexture) {
                if (copyInfo.sourceBox.HasValue) {
                    commandList.CopyTextureRegion(copyInfo.destination, destinationX, destinationY, destinationZ, copyInfo.source, copyInfo.sourceBox.Value);
                }
                else {
                    commandList.CopyTextureRegion(copyInfo.destination, destinationX, destinationY, destinationZ, copyInfo.source);
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
                this._singleBarrier[0] = fromCopy;
                commandList.ResourceBarrier(this._singleBarrier);
                this.SetSubresourceState(subresource, copyInfo.previousState);
            }
        }

        if (copyToTexture) {
            ID3D12Resource retainedTexture = null;
            bool retainedTextureQueued = false;
            try {
                retainedTexture = this.NativeTexture?.QueryInterface<ID3D12Resource>();
                this.gd.RecordBatchedImmediateCommand(RecordCopy, uploadBuffer, retainedTexture);
                retainedTextureQueued = true;
            }
            finally {
                if (!retainedTextureQueued) {
                    retainedTexture?.Dispose();
                }
            }

            return 0;
        }

        return this.gd.ExecuteImmediateCommand(RecordCopy, waitForCompletion: true);
    }

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    private protected override void DisposeCore() {
        if (this._mapped) {
            this._pinnedData.Free();
            this._mapped = false;
        }

        if (this._ownsNativeTexture) {
            this.gd.ReleaseAfterLastSubmission(this._allocation);
        }

        this._disposed = true;
    }

    /// <summary>
    /// Gets the heap flags required by a placed texture resource.
    /// </summary>
    /// <param name="resourceFlags">The resource flags.</param>
    /// <returns>The value produced by this operation.</returns>
    private static HeapFlags GetHeapFlags(ResourceFlags resourceFlags) {
        if ((resourceFlags & (ResourceFlags.AllowRenderTarget | ResourceFlags.AllowDepthStencil)) != 0) {
            return HeapFlags.AllowOnlyRenderTargetDepthStencilTextures;
        }

        return HeapFlags.AllowOnlyNonRenderTargetDepthStencilTextures;
    }

    /// <summary>
    /// Gets the subresource state value.
    /// </summary>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal ResourceStates GetSubresourceState(uint subresource) {
        if (this._subresourceStates == null || subresource >= this._subresourceStates.Length) {
            return ResourceStates.Common;
        }

        return this._subresourceStates[subresource];
    }

    /// <summary>
    /// Sets the subresource state value.
    /// </summary>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <param name="state">The state value used by this operation.</param>
    internal void SetSubresourceState(uint subresource, ResourceStates state) {
        if (this._subresourceStates == null || subresource >= this._subresourceStates.Length) {
            return;
        }

        ResourceStates previous = this._subresourceStates[subresource];
        if (previous == state) {
            return;
        }

        this._subresourceStates[subresource] = state;
        this._stateVersion++;
        if (this._hasCachedCommonState && state != this._cachedCommonState) {
            this._hasCachedCommonState = false;
        }
    }

    /// <summary>
    /// Sets the all subresource states value.
    /// </summary>
    /// <param name="state">The state value used by this operation.</param>
    internal void SetAllSubresourceStates(ResourceStates state) {
        if (this._subresourceStates == null) {
            return;
        }

        if (this._hasCachedCommonState && this._cachedCommonState == state) {
            return;
        }

        for (int i = 0; i < this._subresourceStates.Length; i++) {
            this._subresourceStates[i] = state;
        }

        this._stateVersion++;
        this._hasCachedCommonState = true;
        this._cachedCommonState = state;
    }

    /// <summary>
    /// Attempts to get common state and reports whether it succeeded.
    /// </summary>
    /// <param name="state">The state value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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
