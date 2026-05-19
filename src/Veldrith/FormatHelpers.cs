using System.Diagnostics;

namespace Veldrith;

/// <summary>
/// Represents the FormatHelpers class.
/// </summary>
internal static class FormatHelpers {

    /// <summary>
    /// Executes GetElementCount.
    /// </summary>
    public static int GetElementCount(VertexElementFormat format) {
        switch (format) {
            case VertexElementFormat.Float1: case VertexElementFormat.UInt1: case VertexElementFormat.Int1: case VertexElementFormat.Half1: return 1;

            case VertexElementFormat.Float2: case VertexElementFormat.Byte2Norm: case VertexElementFormat.Byte2: case VertexElementFormat.SByte2Norm: case VertexElementFormat.SByte2: case VertexElementFormat.UShort2Norm: case VertexElementFormat.UShort2: case VertexElementFormat.Short2Norm: case VertexElementFormat.Short2: case VertexElementFormat.UInt2: case VertexElementFormat.Int2: case VertexElementFormat.Half2: return 2;

            case VertexElementFormat.Float3: case VertexElementFormat.UInt3: case VertexElementFormat.Int3: return 3;

            case VertexElementFormat.Float4: case VertexElementFormat.Byte4Norm: case VertexElementFormat.Byte4: case VertexElementFormat.SByte4Norm: case VertexElementFormat.SByte4: case VertexElementFormat.UShort4Norm: case VertexElementFormat.UShort4: case VertexElementFormat.Short4Norm: case VertexElementFormat.Short4: case VertexElementFormat.UInt4: case VertexElementFormat.Int4: case VertexElementFormat.Half4: return 4;

            default: throw Illegal.Value<VertexElementFormat>();
        }
    }

    /// <summary>
    /// Executes GetBlockSizeInBytes.
    /// </summary>
    public static uint GetBlockSizeInBytes(PixelFormat format) {
        switch (format) {
            case PixelFormat.Bc1RgbUNorm: case PixelFormat.Bc1RgbUNormSRgb: case PixelFormat.Bc1RgbaUNorm: case PixelFormat.Bc1RgbaUNormSRgb: case PixelFormat.Bc4UNorm: case PixelFormat.Bc4SNorm: case PixelFormat.Etc2R8G8B8UNorm: case PixelFormat.Etc2R8G8B8A1UNorm: return 8;

            case PixelFormat.Bc2UNorm: case PixelFormat.Bc2UNormSRgb: case PixelFormat.Bc3UNorm: case PixelFormat.Bc3UNormSRgb: case PixelFormat.Bc5UNorm: case PixelFormat.Bc5SNorm: case PixelFormat.Bc7UNorm: case PixelFormat.Bc7UNormSRgb: case PixelFormat.Etc2R8G8B8A8UNorm: return 16;

            default: throw Illegal.Value<PixelFormat>();
        }
    }

    /// <summary>
    /// Executes GetSampleCountUInt32.
    /// </summary>
    internal static uint GetSampleCountUInt32(TextureSampleCount sampleCount) {
        switch (sampleCount) {
            case TextureSampleCount.Count1: return 1;

            case TextureSampleCount.Count2: return 2;

            case TextureSampleCount.Count4: return 4;

            case TextureSampleCount.Count8: return 8;

            case TextureSampleCount.Count16: return 16;

            case TextureSampleCount.Count32: return 32;

            default: throw Illegal.Value<TextureSampleCount>();
        }
    }

    /// <summary>
    /// Executes IsStencilFormat.
    /// </summary>
    internal static bool IsStencilFormat(PixelFormat format) {
        return format == PixelFormat.D24UNormS8UInt || format == PixelFormat.D32FloatS8UInt;
    }

    /// <summary>
    /// Executes IsDepthStencilFormat.
    /// </summary>
    internal static bool IsDepthStencilFormat(PixelFormat format) {
        return format == PixelFormat.D32FloatS8UInt
               || format == PixelFormat.D24UNormS8UInt
               || format == PixelFormat.R16UNorm
               || format == PixelFormat.R32Float;
    }

    /// <summary>
    /// Executes IsCompressedFormat.
    /// </summary>
    internal static bool IsCompressedFormat(PixelFormat format) {
        return format == PixelFormat.Bc1RgbUNorm
               || format == PixelFormat.Bc1RgbUNormSRgb
               || format == PixelFormat.Bc1RgbaUNorm
               || format == PixelFormat.Bc1RgbaUNormSRgb
               || format == PixelFormat.Bc2UNorm
               || format == PixelFormat.Bc2UNormSRgb
               || format == PixelFormat.Bc3UNorm
               || format == PixelFormat.Bc3UNormSRgb
               || format == PixelFormat.Bc4UNorm
               || format == PixelFormat.Bc4SNorm
               || format == PixelFormat.Bc5UNorm
               || format == PixelFormat.Bc5SNorm
               || format == PixelFormat.Bc7UNorm
               || format == PixelFormat.Bc7UNormSRgb
               || format == PixelFormat.Etc2R8G8B8UNorm
               || format == PixelFormat.Etc2R8G8B8A1UNorm
               || format == PixelFormat.Etc2R8G8B8A8UNorm;
    }

    /// <summary>
    /// Executes GetRowPitch.
    /// </summary>
    internal static uint GetRowPitch(uint width, PixelFormat format) {
        switch (format) {
            case PixelFormat.Bc1RgbUNorm:
            case PixelFormat.Bc1RgbUNormSRgb:
            case PixelFormat.Bc1RgbaUNorm:
            case PixelFormat.Bc1RgbaUNormSRgb:
            case PixelFormat.Bc2UNorm:
            case PixelFormat.Bc2UNormSRgb:
            case PixelFormat.Bc3UNorm:
            case PixelFormat.Bc3UNormSRgb:
            case PixelFormat.Bc4UNorm:
            case PixelFormat.Bc4SNorm:
            case PixelFormat.Bc5UNorm:
            case PixelFormat.Bc5SNorm:
            case PixelFormat.Bc7UNorm:
            case PixelFormat.Bc7UNormSRgb:
            case PixelFormat.Etc2R8G8B8UNorm:
            case PixelFormat.Etc2R8G8B8A1UNorm:
            case PixelFormat.Etc2R8G8B8A8UNorm:
                uint blocksPerRow = (width + 3) / 4;
                uint blockSizeInBytes = GetBlockSizeInBytes(format);
                return blocksPerRow * blockSizeInBytes;

            default: return width * FormatSizeHelpers.GetSizeInBytes(format);
        }
    }

    /// <summary>
    /// Executes IsFormatViewCompatible.
    /// </summary>
    internal static bool IsFormatViewCompatible(PixelFormat viewFormat, PixelFormat realFormat) {
        if (IsCompressedFormat(realFormat)) {
            return IsSrgbCounterpart(viewFormat, realFormat);
        }

        return GetViewFamilyFormat(viewFormat) == GetViewFamilyFormat(realFormat);
    }

    /// <summary>
    /// Executes GetNumRows.
    /// </summary>
    internal static uint GetNumRows(uint height, PixelFormat format) {
        switch (format) {
            case PixelFormat.Bc1RgbUNorm: case PixelFormat.Bc1RgbUNormSRgb: case PixelFormat.Bc1RgbaUNorm: case PixelFormat.Bc1RgbaUNormSRgb: case PixelFormat.Bc2UNorm: case PixelFormat.Bc2UNormSRgb: case PixelFormat.Bc3UNorm: case PixelFormat.Bc3UNormSRgb: case PixelFormat.Bc4UNorm: case PixelFormat.Bc4SNorm: case PixelFormat.Bc5UNorm: case PixelFormat.Bc5SNorm: case PixelFormat.Bc7UNorm: case PixelFormat.Bc7UNormSRgb: case PixelFormat.Etc2R8G8B8UNorm: case PixelFormat.Etc2R8G8B8A1UNorm: case PixelFormat.Etc2R8G8B8A8UNorm: return (height + 3) / 4;

            default: return height;
        }
    }

    /// <summary>
    /// Executes GetDepthPitch.
    /// </summary>
    internal static uint GetDepthPitch(uint rowPitch, uint height, PixelFormat format) {
        return rowPitch * GetNumRows(height, format);
    }

    /// <summary>
    /// Executes GetRegionSize.
    /// </summary>
    internal static uint GetRegionSize(uint width, uint height, uint depth, PixelFormat format) {
        uint blockSizeInBytes;

        if (IsCompressedFormat(format)) {
            Debug.Assert((width % 4 == 0 || width < 4) && (height % 4 == 0 || height < 4));
            blockSizeInBytes = GetBlockSizeInBytes(format);
            width /= 4;
            height /= 4;
        }
        else {
            blockSizeInBytes = FormatSizeHelpers.GetSizeInBytes(format);
        }

        return width * height * depth * blockSizeInBytes;
    }

    /// <summary>
    /// Executes GetSampleCount.
    /// </summary>
    internal static TextureSampleCount GetSampleCount(uint samples) {
        switch (samples) {
            case 1: return TextureSampleCount.Count1;

            case 2: return TextureSampleCount.Count2;

            case 4: return TextureSampleCount.Count4;

            case 8: return TextureSampleCount.Count8;

            case 16: return TextureSampleCount.Count16;

            case 32: return TextureSampleCount.Count32;

            default: throw new VeldridException("Unsupported multisample count: " + samples);
        }
    }

    /// <summary>
    /// Executes GetViewFamilyFormat.
    /// </summary>
    internal static PixelFormat GetViewFamilyFormat(PixelFormat format) {
        switch (format) {
            case PixelFormat.R32G32B32A32Float: case PixelFormat.R32G32B32A32UInt: case PixelFormat.R32G32B32A32SInt: return PixelFormat.R32G32B32A32Float;

            case PixelFormat.R16G16B16A16Float: case PixelFormat.R16G16B16A16UNorm: case PixelFormat.R16G16B16A16UInt: case PixelFormat.R16G16B16A16SNorm: case PixelFormat.R16G16B16A16SInt: return PixelFormat.R16G16B16A16Float;

            case PixelFormat.R32G32Float: case PixelFormat.R32G32UInt: case PixelFormat.R32G32SInt: return PixelFormat.R32G32Float;

            case PixelFormat.R10G10B10A2UNorm: case PixelFormat.R10G10B10A2UInt: return PixelFormat.R10G10B10A2UNorm;

            case PixelFormat.R8G8B8A8UNorm: case PixelFormat.R8G8B8A8UNormSRgb: case PixelFormat.R8G8B8A8UInt: case PixelFormat.R8G8B8A8SNorm: case PixelFormat.R8G8B8A8SInt: return PixelFormat.R8G8B8A8UNorm;

            case PixelFormat.R16G16Float: case PixelFormat.R16G16UNorm: case PixelFormat.R16G16UInt: case PixelFormat.R16G16SNorm: case PixelFormat.R16G16SInt: return PixelFormat.R16G16Float;

            case PixelFormat.R32Float: case PixelFormat.R32UInt: case PixelFormat.R32SInt: return PixelFormat.R32Float;

            case PixelFormat.R8G8UNorm: case PixelFormat.R8G8UInt: case PixelFormat.R8G8SNorm: case PixelFormat.R8G8SInt: return PixelFormat.R8G8UNorm;

            case PixelFormat.R16Float: case PixelFormat.R16UNorm: case PixelFormat.R16UInt: case PixelFormat.R16SNorm: case PixelFormat.R16SInt: return PixelFormat.R16Float;

            case PixelFormat.R8UNorm: case PixelFormat.R8UInt: case PixelFormat.R8SNorm: case PixelFormat.R8SInt: return PixelFormat.R8UNorm;

            case PixelFormat.Bc1RgbaUNorm: case PixelFormat.Bc1RgbaUNormSRgb: case PixelFormat.Bc1RgbUNorm: case PixelFormat.Bc1RgbUNormSRgb: return PixelFormat.Bc1RgbaUNorm;

            case PixelFormat.Bc2UNorm: case PixelFormat.Bc2UNormSRgb: return PixelFormat.Bc2UNorm;

            case PixelFormat.Bc3UNorm: case PixelFormat.Bc3UNormSRgb: return PixelFormat.Bc3UNorm;

            case PixelFormat.Bc4UNorm: case PixelFormat.Bc4SNorm: return PixelFormat.Bc4UNorm;

            case PixelFormat.Bc5UNorm: case PixelFormat.Bc5SNorm: return PixelFormat.Bc5UNorm;

            case PixelFormat.B8G8R8A8UNorm: case PixelFormat.B8G8R8A8UNormSRgb: return PixelFormat.B8G8R8A8UNorm;

            case PixelFormat.Bc7UNorm: case PixelFormat.Bc7UNormSRgb: return PixelFormat.Bc7UNorm;

            default: return format;
        }
    }

    /// <summary>
    /// Executes IsSrgbCounterpart.
    /// </summary>
    private static bool IsSrgbCounterpart(PixelFormat viewFormat, PixelFormat realFormat) {
        if (viewFormat == realFormat) {
            return true;
        }

        switch (realFormat) {
            case PixelFormat.Bc1RgbUNorm: return viewFormat == PixelFormat.Bc1RgbUNormSRgb;
            case PixelFormat.Bc1RgbUNormSRgb: return viewFormat == PixelFormat.Bc1RgbUNorm;
            case PixelFormat.Bc1RgbaUNorm: return viewFormat == PixelFormat.Bc1RgbaUNormSRgb;
            case PixelFormat.Bc1RgbaUNormSRgb: return viewFormat == PixelFormat.Bc1RgbaUNorm;
            case PixelFormat.Bc2UNorm: return viewFormat == PixelFormat.Bc2UNormSRgb;
            case PixelFormat.Bc2UNormSRgb: return viewFormat == PixelFormat.Bc2UNorm;
            case PixelFormat.Bc3UNorm: return viewFormat == PixelFormat.Bc3UNormSRgb;
            case PixelFormat.Bc3UNormSRgb: return viewFormat == PixelFormat.Bc3UNorm;
            case PixelFormat.Bc7UNorm: return viewFormat == PixelFormat.Bc7UNormSRgb;
            case PixelFormat.Bc7UNormSRgb: return viewFormat == PixelFormat.Bc7UNorm;
            default: return false;
        }
    }
}