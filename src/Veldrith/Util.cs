using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Veldrith;

/// <summary>
/// Represents the Util type used by the graphics runtime.
/// </summary>
internal static class Util {

    /// <summary>
    /// Executes the clamp logic for this backend.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
    /// <param name="min">The min value used by this operation.</param>
    /// <param name="max">The max value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Copies texture region data between resources.
    /// </summary>
    /// <param name="src">The source value or resource.</param>
    /// <param name="srcX">The src x value used by this operation.</param>
    /// <param name="srcY">The src y value used by this operation.</param>
    /// <param name="srcZ">The src z value used by this operation.</param>
    /// <param name="srcRowPitch">The src row pitch value used by this operation.</param>
    /// <param name="srcDepthPitch">The src depth pitch value used by this operation.</param>
    /// <param name="dst">The destination value or resource.</param>
    /// <param name="dstX">The dst x value used by this operation.</param>
    /// <param name="dstY">The dst y value used by this operation.</param>
    /// <param name="dstZ">The dst z value used by this operation.</param>
    /// <param name="dstRowPitch">The dst row pitch value used by this operation.</param>
    /// <param name="dstDepthPitch">The dst depth pitch value used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="format">The format used by this operation.</param>
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
    /// Gets the buffer range value.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="additionalOffset">The additional offset value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static DeviceBufferRange GetBufferRange(IBindableResource resource, uint additionalOffset) {
        if (resource is DeviceBufferRange range) {
            return new DeviceBufferRange(range.Buffer, range.Offset + additionalOffset, range.SizeInBytes);
        }

        DeviceBuffer buffer = (DeviceBuffer)resource;
        return new DeviceBufferRange(buffer, additionalOffset, buffer.SizeInBytes);
    }

    /// <summary>
    /// Gets the device buffer value.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="buffer">The buffer resource involved in this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Casts a base-type reference to a required derived type and validates the cast in debug builds.
    /// </summary>
    /// <param name="value">The value used by this operation.</param>
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

    /// <summary>
    /// Ensures that an array reference exists and can hold at least the requested number of elements.
    /// </summary>
    /// <param name="array">The array value used by this operation.</param>
    /// <param name="size">The size, in bytes, used by this operation.</param>
    internal static void EnsureArrayMinimumSize<T>(ref T[] array, uint size) {
        if (array == null) {
            array = new T[size];
        }
        else if (array.Length < size) {
            Array.Resize(ref array, (int)size);
        }
    }

    /// <summary>
    /// Returns the unmanaged size of <typeparamref name="T" /> as an unsigned integer.
    /// </summary>
    internal static uint USizeOf<T>() where T : struct {
        return (uint)Unsafe.SizeOf<T>();
    }

    /// <summary>
    /// Gets the string value.
    /// </summary>
    /// <param name="stringStart">The string start value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static unsafe string GetString(byte* stringStart) {
        int characters = 0;
        while (stringStart[characters] != 0) {
            characters++;
        }

        return Encoding.UTF8.GetString(stringStart, characters);
    }

    /// <summary>
    /// Compares two nullable value types for equality.
    /// </summary>
    /// <param name="left">The left operand of the operation.</param>
    /// <param name="right">The right operand of the operation.</param>
    internal static bool NullableEquals<T>(T? left, T? right) where T : struct, IEquatable<T> {
        if (left.HasValue && right.HasValue) {
            return left.Value.Equals(right.Value);
        }

        return left.HasValue == right.HasValue;
    }

    /// <summary>
    /// Compares two arrays of reference types by length and reference identity of each element.
    /// </summary>
    /// <param name="left">The left operand of the operation.</param>
    /// <param name="right">The right operand of the operation.</param>
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

    /// <summary>
    /// Compares two arrays of value types using each element's <see cref="IEquatable{T}" /> implementation.
    /// </summary>
    /// <param name="left">The left operand of the operation.</param>
    /// <param name="right">The right operand of the operation.</param>
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

    /// <summary>
    /// Clears all elements in an array when the array is not null.
    /// </summary>
    /// <param name="array">The array value used by this operation.</param>
    internal static void ClearArray<T>(T[] array) {
        if (array != null) {
            Array.Clear(array, 0, array.Length);
        }
    }

    /// <summary>
    /// Computes the mip level and array layer indices from a subresource index.
    /// </summary>
    /// <param name="tex">The tex value used by this operation.</param>
    /// <param name="subresource">The subresource value used by this operation.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    internal static void GetMipLevelAndArrayLayer(Texture tex, uint subresource, out uint mipLevel, out uint arrayLayer) {
        arrayLayer = subresource / tex.MipLevels;
        mipLevel = subresource - arrayLayer * tex.MipLevels;
    }

    /// <summary>
    /// Gets the mip dimensions value.
    /// </summary>
    /// <param name="tex">The tex value used by this operation.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    internal static void GetMipDimensions(Texture tex, uint mipLevel, out uint width, out uint height, out uint depth) {
        width = GetDimension(tex.Width, mipLevel);
        height = GetDimension(tex.Height, mipLevel);
        depth = GetDimension(tex.Depth, mipLevel);
    }

    /// <summary>
    /// Gets the dimension value.
    /// </summary>
    /// <param name="largestLevelDimension">The largest level dimension value used by this operation.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static uint GetDimension(uint largestLevelDimension, uint mipLevel) {
        uint ret = largestLevelDimension;
        for (uint i = 0; i < mipLevel; i++) {
            ret /= 2;
        }

        return Math.Max(1, ret);
    }

    /// <summary>
    /// Computes the subresource offset value.
    /// </summary>
    /// <param name="tex">The tex value used by this operation.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static ulong ComputeSubresourceOffset(Texture tex, uint mipLevel, uint arrayLayer) {
        Debug.Assert((tex.Usage & TextureUsage.Staging) == TextureUsage.Staging);
        return ComputeArrayLayerOffset(tex, arrayLayer) + ComputeMipOffset(tex, mipLevel);
    }

    /// <summary>
    /// Computes the mip offset value.
    /// </summary>
    /// <param name="tex">The tex value used by this operation.</param>
    /// <param name="mipLevel">The mip level index.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Computes the array layer offset value.
    /// </summary>
    /// <param name="tex">The tex value used by this operation.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    /// <returns>The value produced by this operation.</returns>
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

    /// <summary>
    /// Creates a shallow clone of an array.
    /// </summary>
    /// <param name="array">The array value used by this operation.</param>
    internal static T[] ShallowClone<T>(T[] array) {
        return (T[])array.Clone();
    }

    /// <summary>
    /// Gets the texture view value.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the pack int ptr logic for this backend.
    /// </summary>
    /// <param name="sourcePtr">The source ptr value used by this operation.</param>
    /// <param name="low">The low value used by this operation.</param>
    /// <param name="high">The high value used by this operation.</param>
    internal static void PackIntPtr(IntPtr sourcePtr, out uint low, out uint high) {
        ulong src64 = (ulong)sourcePtr;
        low = (uint)(src64 & 0x00000000FFFFFFFF);
        high = (uint)((src64 & 0xFFFFFFFF00000000u) >> 32);
    }

    /// <summary>
    /// Executes the unpack int ptr logic for this backend.
    /// </summary>
    /// <param name="low">The low value used by this operation.</param>
    /// <param name="high">The high value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static IntPtr UnpackIntPtr(uint low, uint high) {
        ulong src64 = low | ((ulong)high << 32);
        return (IntPtr)src64;
    }
}