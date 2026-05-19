using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the Util class.
/// </summary>
internal static class Util {

    /// <summary>
    /// Executes the Clamp operation.
    /// </summary>
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
    /// <param name="min">Specifies the value of <paramref name="min" />.</param>
    /// <param name="max">Specifies the value of <paramref name="max" />.</param>
    /// <returns>Returns the result produced by the Clamp operation.</returns>
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
    /// Executes the CopyTextureRegion operation.
    /// </summary>
    /// <param name="src">Specifies the value of <paramref name="src" />.</param>
    /// <param name="srcX">Specifies the value of <paramref name="srcX" />.</param>
    /// <param name="srcY">Specifies the value of <paramref name="srcY" />.</param>
    /// <param name="srcZ">Specifies the value of <paramref name="srcZ" />.</param>
    /// <param name="srcRowPitch">Specifies the value of <paramref name="srcRowPitch" />.</param>
    /// <param name="srcDepthPitch">Specifies the value of <paramref name="srcDepthPitch" />.</param>
    /// <param name="dst">Specifies the value of <paramref name="dst" />.</param>
    /// <param name="dstX">Specifies the value of <paramref name="dstX" />.</param>
    /// <param name="dstY">Specifies the value of <paramref name="dstY" />.</param>
    /// <param name="dstZ">Specifies the value of <paramref name="dstZ" />.</param>
    /// <param name="dstRowPitch">Specifies the value of <paramref name="dstRowPitch" />.</param>
    /// <param name="dstDepthPitch">Specifies the value of <paramref name="dstDepthPitch" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
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
    /// Executes the GetBufferRange operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="additionalOffset">Specifies the value of <paramref name="additionalOffset" />.</param>
    /// <returns>Returns the result produced by the GetBufferRange operation.</returns>
    public static DeviceBufferRange GetBufferRange(IBindableResource resource, uint additionalOffset) {
        if (resource is DeviceBufferRange range) {
            return new DeviceBufferRange(range.Buffer, range.Offset + additionalOffset, range.SizeInBytes);
        }

        DeviceBuffer buffer = (DeviceBuffer)resource;
        return new DeviceBufferRange(buffer, additionalOffset, buffer.SizeInBytes);
    }

    /// <summary>
    /// Executes the GetDeviceBuffer operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="buffer">Specifies the value of <paramref name="buffer" />.</param>
    /// <returns>Returns the result produced by the GetDeviceBuffer operation.</returns>
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
    /// <param name="value">Specifies the value of <paramref name="value" />.</param>
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
    /// <param name="array">Specifies the value of <paramref name="array" />.</param>
    /// <param name="size">Specifies the value of <paramref name="size" />.</param>
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
    /// Executes the GetString operation.
    /// </summary>
    /// <param name="stringStart">Specifies the value of <paramref name="stringStart" />.</param>
    /// <returns>Returns the result produced by the GetString operation.</returns>
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
    /// <param name="left">Specifies the value of <paramref name="left" />.</param>
    /// <param name="right">Specifies the value of <paramref name="right" />.</param>
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
    /// <param name="left">Specifies the value of <paramref name="left" />.</param>
    /// <param name="right">Specifies the value of <paramref name="right" />.</param>
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
    /// <param name="left">Specifies the value of <paramref name="left" />.</param>
    /// <param name="right">Specifies the value of <paramref name="right" />.</param>
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
    /// <param name="array">Specifies the value of <paramref name="array" />.</param>
    /// <typeparam name="T">The array element type.</typeparam>
    internal static void ClearArray<T>(T[] array) {
        if (array != null) {
            Array.Clear(array, 0, array.Length);
        }
    }

    /// <summary>
    /// Computes the mip level and array layer indices from a subresource index.
    /// </summary>
    /// <param name="tex">Specifies the value of <paramref name="tex" />.</param>
    /// <param name="subresource">Specifies the value of <paramref name="subresource" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    internal static void GetMipLevelAndArrayLayer(Texture tex, uint subresource, out uint mipLevel, out uint arrayLayer) {
        arrayLayer = subresource / tex.MipLevels;
        mipLevel = subresource - arrayLayer * tex.MipLevels;
    }

    /// <summary>
    /// Executes the GetMipDimensions operation.
    /// </summary>
    /// <param name="tex">Specifies the value of <paramref name="tex" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depth">Specifies the value of <paramref name="depth" />.</param>
    internal static void GetMipDimensions(Texture tex, uint mipLevel, out uint width, out uint height, out uint depth) {
        width = GetDimension(tex.Width, mipLevel);
        height = GetDimension(tex.Height, mipLevel);
        depth = GetDimension(tex.Depth, mipLevel);
    }

    /// <summary>
    /// Executes the GetDimension operation.
    /// </summary>
    /// <param name="largestLevelDimension">Specifies the value of <paramref name="largestLevelDimension" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <returns>Returns the result produced by the GetDimension operation.</returns>
    internal static uint GetDimension(uint largestLevelDimension, uint mipLevel) {
        uint ret = largestLevelDimension;
        for (uint i = 0; i < mipLevel; i++) {
            ret /= 2;
        }

        return Math.Max(1, ret);
    }

    /// <summary>
    /// Executes the ComputeSubresourceOffset operation.
    /// </summary>
    /// <param name="tex">Specifies the value of <paramref name="tex" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    /// <returns>Returns the result produced by the ComputeSubresourceOffset operation.</returns>
    internal static ulong ComputeSubresourceOffset(Texture tex, uint mipLevel, uint arrayLayer) {
        Debug.Assert((tex.Usage & TextureUsage.Staging) == TextureUsage.Staging);
        return ComputeArrayLayerOffset(tex, arrayLayer) + ComputeMipOffset(tex, mipLevel);
    }

    /// <summary>
    /// Executes the ComputeMipOffset operation.
    /// </summary>
    /// <param name="tex">Specifies the value of <paramref name="tex" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
    /// <returns>Returns the result produced by the ComputeMipOffset operation.</returns>
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
    /// Executes the ComputeArrayLayerOffset operation.
    /// </summary>
    /// <param name="tex">Specifies the value of <paramref name="tex" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    /// <returns>Returns the result produced by the ComputeArrayLayerOffset operation.</returns>
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
    /// <param name="array">Specifies the value of <paramref name="array" />.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A new array instance containing the same element references or values.</returns>
    internal static T[] ShallowClone<T>(T[] array) {
        return (T[])array.Clone();
    }

    /// <summary>
    /// Executes the GetTextureView operation.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <returns>Returns the result produced by the GetTextureView operation.</returns>
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
    /// Executes the PackIntPtr operation.
    /// </summary>
    /// <param name="sourcePtr">Specifies the value of <paramref name="sourcePtr" />.</param>
    /// <param name="low">Specifies the value of <paramref name="low" />.</param>
    /// <param name="high">Specifies the value of <paramref name="high" />.</param>
    internal static void PackIntPtr(IntPtr sourcePtr, out uint low, out uint high) {
        ulong src64 = (ulong)sourcePtr;
        low = (uint)(src64 & 0x00000000FFFFFFFF);
        high = (uint)((src64 & 0xFFFFFFFF00000000u) >> 32);
    }

    /// <summary>
    /// Executes the UnpackIntPtr operation.
    /// </summary>
    /// <param name="low">Specifies the value of <paramref name="low" />.</param>
    /// <param name="high">Specifies the value of <paramref name="high" />.</param>
    /// <returns>Returns the result produced by the UnpackIntPtr operation.</returns>
    internal static IntPtr UnpackIntPtr(uint low, uint high) {
        ulong src64 = low | ((ulong)high << 32);
        return (IntPtr)src64;
    }
}