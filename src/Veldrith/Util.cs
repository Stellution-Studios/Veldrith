using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Veldrith;

/// <summary>
/// Represents the Util class.
/// </summary>
internal static class Util {

    /// <summary>
    /// Executes Clamp.
    /// </summary>
    public static uint Clamp(uint value, uint min, uint max) {
        if (value <= min) {
            return min;
        }

        if (value >= max) {
            return max;
        }

        return value;
    }

    /// <summary>
    /// Executes CopyTextureRegion.
    /// </summary>
    public static unsafe void CopyTextureRegion(void* src, uint srcX, uint srcY, uint srcZ, uint srcRowPitch, uint srcDepthPitch, void* dst, uint dstX, uint dstY, uint dstZ, uint dstRowPitch, uint dstDepthPitch, uint width, uint height, uint depth, PixelFormat format) {
        uint blockSize = FormatHelpers.IsCompressedFormat(format) ? 4u : 1u;
        uint blockSizeInBytes = blockSize > 1
            ? FormatHelpers.GetBlockSizeInBytes(format)
            : FormatSizeHelpers.GetSizeInBytes(format);
        uint compressedSrcX = srcX / blockSize;
        uint compressedSrcY = srcY / blockSize;
        uint compressedDstX = dstX / blockSize;
        uint compressedDstY = dstY / blockSize;
        uint numRows = FormatHelpers.GetNumRows(height, format);
        uint rowSize = width / blockSize * blockSizeInBytes;

        if (srcRowPitch == dstRowPitch && srcDepthPitch == dstDepthPitch) {
            uint totalCopySize = depth * srcDepthPitch;
            Buffer.MemoryCopy(src, dst, totalCopySize, totalCopySize);
        }
        else {
            for (uint zz = 0; zz < depth; zz++) {
                for (uint yy = 0; yy < numRows; yy++) {
                    byte* rowCopyDst = (byte*)dst
                                       + dstDepthPitch * (zz + dstZ)
                                       + dstRowPitch * (yy + compressedDstY)
                                       + blockSizeInBytes * compressedDstX;

                    byte* rowCopySrc = (byte*)src
                                       + srcDepthPitch * (zz + srcZ)
                                       + srcRowPitch * (yy + compressedSrcY)
                                       + blockSizeInBytes * compressedSrcX;

                    Unsafe.CopyBlock(rowCopyDst, rowCopySrc, rowSize);
                }
            }
        }
    }

    /// <summary>
    /// Executes GetBufferRange.
    /// </summary>
    public static DeviceBufferRange GetBufferRange(IBindableResource resource, uint additionalOffset) {
        if (resource is DeviceBufferRange range) {
            return new DeviceBufferRange(range.Buffer, range.Offset + additionalOffset, range.SizeInBytes);
        }

        DeviceBuffer buffer = (DeviceBuffer)resource;
        return new DeviceBufferRange(buffer, additionalOffset, buffer.SizeInBytes);
    }

    /// <summary>
    /// Executes GetDeviceBuffer.
    /// </summary>
    public static bool GetDeviceBuffer(IBindableResource resource, out DeviceBuffer buffer) {
        if (resource is DeviceBuffer db) {
            buffer = db;
            return true;
        }

        if (resource is DeviceBufferRange range) {
            buffer = range.Buffer;
            return true;
        }

        buffer = null;
        return false;
    }

    [DebuggerNonUserCode]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TDerived AssertSubtype<TBase, TDerived>(TBase value)
        where TDerived : class, TBase where TBase : class {
#if DEBUG
        if (value == null) throw new VeldridException($"Expected object of type {typeof(TDerived).FullName} but received null instead.");

        if (!(value is TDerived derived)) throw new VeldridException($"object {value} must be derived type {typeof(TDerived).FullName} to be used in this context.");

        return derived;

#else
        return (TDerived)value;
#endif
    }

    internal static void EnsureArrayMinimumSize<T>(ref T[] array, uint size) {
        if (array == null) {
            array = new T[size];
        }
        else if (array.Length < size) {
            Array.Resize(ref array, (int)size);
        }
    }

    internal static uint USizeOf<T>() where T : struct {
        return (uint)Unsafe.SizeOf<T>();
    }

    /// <summary>
    /// Executes GetString.
    /// </summary>
    internal static unsafe string GetString(byte* stringStart) {
        int characters = 0;
        while (stringStart[characters] != 0) {
            characters++;
        }

        return Encoding.UTF8.GetString(stringStart, characters);
    }

    internal static bool NullableEquals<T>(T? left, T? right) where T : struct, IEquatable<T> {
        if (left.HasValue && right.HasValue) {
            return left.Value.Equals(right.Value);
        }

        return left.HasValue == right.HasValue;
    }

    internal static bool ArrayEquals<T>(T[] left, T[] right) where T : class {
        if (left == null || right == null) {
            return left == right;
        }

        if (left.Length != right.Length) {
            return false;
        }

        for (int i = 0; i < left.Length; i++) {
            if (!ReferenceEquals(left[i], right[i])) {
                return false;
            }
        }

        return true;
    }

    internal static bool ArrayEqualsEquatable<T>(T[] left, T[] right) where T : struct, IEquatable<T> {
        if (left == null || right == null) {
            return left == right;
        }

        if (left.Length != right.Length) {
            return false;
        }

        for (int i = 0; i < left.Length; i++) {
            if (!left[i].Equals(right[i])) {
                return false;
            }
        }

        return true;
    }

    internal static void ClearArray<T>(T[] array) {
        if (array != null) {
            Array.Clear(array, 0, array.Length);
        }
    }

    internal static void

        /// <summary>
        /// Executes GetMipLevelAndArrayLayer.
        /// </summary>
        GetMipLevelAndArrayLayer(Texture tex, uint subresource, out uint mipLevel, out uint arrayLayer) {
        arrayLayer = subresource / tex.MipLevels;
        mipLevel = subresource - arrayLayer * tex.MipLevels;
    }

    /// <summary>
    /// Executes GetMipDimensions.
    /// </summary>
    internal static void GetMipDimensions(Texture tex, uint mipLevel, out uint width, out uint height, out uint depth) {
        width = GetDimension(tex.Width, mipLevel);
        height = GetDimension(tex.Height, mipLevel);
        depth = GetDimension(tex.Depth, mipLevel);
    }

    /// <summary>
    /// Executes GetDimension.
    /// </summary>
    internal static uint GetDimension(uint largestLevelDimension, uint mipLevel) {
        uint ret = largestLevelDimension;
        for (uint i = 0; i < mipLevel; i++) {
            ret /= 2;
        }

        return Math.Max(1, ret);
    }

    /// <summary>
    /// Executes ComputeSubresourceOffset.
    /// </summary>
    internal static ulong ComputeSubresourceOffset(Texture tex, uint mipLevel, uint arrayLayer) {
        Debug.Assert((tex.Usage & TextureUsage.Staging) == TextureUsage.Staging);
        return ComputeArrayLayerOffset(tex, arrayLayer) + ComputeMipOffset(tex, mipLevel);
    }

    /// <summary>
    /// Executes ComputeMipOffset.
    /// </summary>
    internal static uint ComputeMipOffset(Texture tex, uint mipLevel) {
        uint blockSize = FormatHelpers.IsCompressedFormat(tex.Format) ? 4u : 1u;
        uint offset = 0;

        for (uint level = 0; level < mipLevel; level++) {
            GetMipDimensions(tex, level, out uint mipWidth, out uint mipHeight, out uint mipDepth);
            uint storageWidth = Math.Max(mipWidth, blockSize);
            uint storageHeight = Math.Max(mipHeight, blockSize);
            offset += FormatHelpers.GetRegionSize(storageWidth, storageHeight, mipDepth, tex.Format);
        }

        return offset;
    }

    /// <summary>
    /// Executes ComputeArrayLayerOffset.
    /// </summary>
    internal static uint ComputeArrayLayerOffset(Texture tex, uint arrayLayer) {
        if (arrayLayer == 0) {
            return 0;
        }

        uint blockSize = FormatHelpers.IsCompressedFormat(tex.Format) ? 4u : 1u;
        uint layerPitch = 0;

        for (uint level = 0; level < tex.MipLevels; level++) {
            GetMipDimensions(tex, level, out uint mipWidth, out uint mipHeight, out uint mipDepth);
            uint storageWidth = Math.Max(mipWidth, blockSize);
            uint storageHeight = Math.Max(mipHeight, blockSize);
            layerPitch += FormatHelpers.GetRegionSize(storageWidth, storageHeight, mipDepth, tex.Format);
        }

        return layerPitch * arrayLayer;
    }

    internal static T[] ShallowClone<T>(T[] array) {
        return (T[])array.Clone();
    }

    /// <summary>
    /// Executes GetTextureView.
    /// </summary>
    internal static TextureView GetTextureView(GraphicsDevice gd, IBindableResource resource) {
        if (resource is TextureView view) {
            return view;
        }

        if (resource is Texture tex) {
            return tex.GetFullTextureView(gd);
        }

        throw new VeldridException($"Unexpected resource type. Expected Texture or TextureView but found {resource.GetType().Name}");
    }

    /// <summary>
    /// Executes PackIntPtr.
    /// </summary>
    internal static void PackIntPtr(IntPtr sourcePtr, out uint low, out uint high) {
        ulong src64 = (ulong)sourcePtr;
        low = (uint)(src64 & 0x00000000FFFFFFFF);
        high = (uint)((src64 & 0xFFFFFFFF00000000u) >> 32);
    }

    /// <summary>
    /// Executes UnpackIntPtr.
    /// </summary>
    internal static IntPtr UnpackIntPtr(uint low, uint high) {
        ulong src64 = low | ((ulong)high << 32);
        return (IntPtr)src64;
    }
}