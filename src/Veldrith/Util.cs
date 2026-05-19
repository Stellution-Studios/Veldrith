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
    /// Performs the Clamp operation.
    /// </summary>
    /// <param name="value">The value of value.</param>
    /// <param name="min">The value of min.</param>
    /// <param name="max">The value of max.</param>
    /// <returns>The result of the Clamp operation.</returns>
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
    /// Performs the CopyTextureRegion operation.
    /// </summary>
    /// <param name="src">The value of src.</param>
    /// <param name="srcX">The value of srcX.</param>
    /// <param name="srcY">The value of srcY.</param>
    /// <param name="srcZ">The value of srcZ.</param>
    /// <param name="srcRowPitch">The value of srcRowPitch.</param>
    /// <param name="srcDepthPitch">The value of srcDepthPitch.</param>
    /// <param name="dst">The value of dst.</param>
    /// <param name="dstX">The value of dstX.</param>
    /// <param name="dstY">The value of dstY.</param>
    /// <param name="dstZ">The value of dstZ.</param>
    /// <param name="dstRowPitch">The value of dstRowPitch.</param>
    /// <param name="dstDepthPitch">The value of dstDepthPitch.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="format">The value of format.</param>
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
    /// Performs the GetBufferRange operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="additionalOffset">The value of additionalOffset.</param>
    /// <returns>The result of the GetBufferRange operation.</returns>
    public static DeviceBufferRange GetBufferRange(IBindableResource resource, uint additionalOffset) {
        if (resource is DeviceBufferRange range) {
            return new DeviceBufferRange(range.Buffer, range.Offset + additionalOffset, range.SizeInBytes);
        }

        DeviceBuffer buffer = (DeviceBuffer)resource;
        return new DeviceBufferRange(buffer, additionalOffset, buffer.SizeInBytes);
    }

    /// <summary>
    /// Performs the GetDeviceBuffer operation.
    /// </summary>
    /// <param name="resource">The value of resource.</param>
    /// <param name="buffer">The value of buffer.</param>
    /// <returns>The result of the GetDeviceBuffer operation.</returns>
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
    /// <param name="value">The value to cast.</param>
    /// <typeparam name="TBase">The expected base type.</typeparam>
    /// <typeparam name="TDerived">The required derived type.</typeparam>
    /// <returns>The cast value as <typeparamref name="TDerived" />.</returns>
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
    /// <param name="array">The array reference to validate or grow.</param>
    /// <param name="size">The minimum required length.</param>
    /// <typeparam name="T">The array element type.</typeparam>
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
    /// <typeparam name="T">The value type to measure.</typeparam>
    /// <returns>The size of <typeparamref name="T" /> in bytes.</returns>
    internal static uint USizeOf<T>() where T : struct {
        return (uint)Unsafe.SizeOf<T>();
    }

    /// <summary>
    /// Performs the GetString operation.
    /// </summary>
    /// <param name="stringStart">The value of stringStart.</param>
    /// <returns>The result of the GetString operation.</returns>
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
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <typeparam name="T">The nullable value type.</typeparam>
    /// <returns><see langword="true" /> if both values are equal or both are null; otherwise, <see langword="false" />.</returns>
    internal static bool NullableEquals<T>(T? left, T? right) where T : struct, IEquatable<T> {
        if (left.HasValue && right.HasValue) {
            return left.Value.Equals(right.Value);
        }

        return left.HasValue == right.HasValue;
    }

    /// <summary>
    /// Compares two arrays of reference types by length and reference identity of each element.
    /// </summary>
    /// <param name="left">The first array.</param>
    /// <param name="right">The second array.</param>
    /// <typeparam name="T">The reference element type.</typeparam>
    /// <returns><see langword="true" /> if both arrays are equal by reference semantics; otherwise, <see langword="false" />.</returns>
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
    /// <param name="left">The first array.</param>
    /// <param name="right">The second array.</param>
    /// <typeparam name="T">The equatable value type.</typeparam>
    /// <returns><see langword="true" /> if both arrays contain equal elements in the same order; otherwise, <see langword="false" />.</returns>
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
    /// <param name="array">The array to clear.</param>
    /// <typeparam name="T">The array element type.</typeparam>
    internal static void ClearArray<T>(T[] array) {
        if (array != null) {
            Array.Clear(array, 0, array.Length);
        }
    }

    /// <summary>
    /// Computes the mip level and array layer indices from a subresource index.
    /// </summary>
    /// <param name="tex">The texture whose subresource layout is used.</param>
    /// <param name="subresource">The combined subresource index.</param>
    /// <param name="mipLevel">Receives the extracted mip level index.</param>
    /// <param name="arrayLayer">Receives the extracted array layer index.</param>
    internal static void GetMipLevelAndArrayLayer(Texture tex, uint subresource, out uint mipLevel, out uint arrayLayer) {
        arrayLayer = subresource / tex.MipLevels;
        mipLevel = subresource - arrayLayer * tex.MipLevels;
    }

    /// <summary>
    /// Performs the GetMipDimensions operation.
    /// </summary>
    /// <param name="tex">The value of tex.</param>
    /// <param name="mipLevel">The value of mipLevel.</param>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    internal static void GetMipDimensions(Texture tex, uint mipLevel, out uint width, out uint height, out uint depth) {
        width = GetDimension(tex.Width, mipLevel);
        height = GetDimension(tex.Height, mipLevel);
        depth = GetDimension(tex.Depth, mipLevel);
    }

    /// <summary>
    /// Performs the GetDimension operation.
    /// </summary>
    /// <param name="largestLevelDimension">The value of largestLevelDimension.</param>
    /// <param name="mipLevel">The value of mipLevel.</param>
    /// <returns>The result of the GetDimension operation.</returns>
    internal static uint GetDimension(uint largestLevelDimension, uint mipLevel) {
        uint ret = largestLevelDimension;
        for (uint i = 0; i < mipLevel; i++) {
            ret /= 2;
        }

        return Math.Max(1, ret);
    }

    /// <summary>
    /// Performs the ComputeSubresourceOffset operation.
    /// </summary>
    /// <param name="tex">The value of tex.</param>
    /// <param name="mipLevel">The value of mipLevel.</param>
    /// <param name="arrayLayer">The value of arrayLayer.</param>
    /// <returns>The result of the ComputeSubresourceOffset operation.</returns>
    internal static ulong ComputeSubresourceOffset(Texture tex, uint mipLevel, uint arrayLayer) {
        Debug.Assert((tex.Usage & TextureUsage.Staging) == TextureUsage.Staging);
        return ComputeArrayLayerOffset(tex, arrayLayer) + ComputeMipOffset(tex, mipLevel);
    }

    /// <summary>
    /// Performs the ComputeMipOffset operation.
    /// </summary>
    /// <param name="tex">The value of tex.</param>
    /// <param name="mipLevel">The value of mipLevel.</param>
    /// <returns>The result of the ComputeMipOffset operation.</returns>
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
    /// Performs the ComputeArrayLayerOffset operation.
    /// </summary>
    /// <param name="tex">The value of tex.</param>
    /// <param name="arrayLayer">The value of arrayLayer.</param>
    /// <returns>The result of the ComputeArrayLayerOffset operation.</returns>
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
    /// <param name="array">The array to clone.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A new array instance containing the same element references or values.</returns>
    internal static T[] ShallowClone<T>(T[] array) {
        return (T[])array.Clone();
    }

    /// <summary>
    /// Performs the GetTextureView operation.
    /// </summary>
    /// <param name="gd">The value of gd.</param>
    /// <param name="resource">The value of resource.</param>
    /// <returns>The result of the GetTextureView operation.</returns>
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
    /// Performs the PackIntPtr operation.
    /// </summary>
    /// <param name="sourcePtr">The value of sourcePtr.</param>
    /// <param name="low">The value of low.</param>
    /// <param name="high">The value of high.</param>
    internal static void PackIntPtr(IntPtr sourcePtr, out uint low, out uint high) {
        ulong src64 = (ulong)sourcePtr;
        low = (uint)(src64 & 0x00000000FFFFFFFF);
        high = (uint)((src64 & 0xFFFFFFFF00000000u) >> 32);
    }

    /// <summary>
    /// Performs the UnpackIntPtr operation.
    /// </summary>
    /// <param name="low">The value of low.</param>
    /// <param name="high">The value of high.</param>
    /// <returns>The result of the UnpackIntPtr operation.</returns>
    internal static IntPtr UnpackIntPtr(uint low, uint high) {
        ulong src64 = low | ((ulong)high << 32);
        return (IntPtr)src64;
    }
}