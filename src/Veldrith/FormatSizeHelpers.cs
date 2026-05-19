using System.Diagnostics;

namespace Veldrith;

public static class FormatSizeHelpers {

    /// <summary>
    /// Given a pixel format, returns the number of bytes required to store
    /// a single pixel.
    /// Compressed formats may not be used with this method as the number of
    /// bytes per pixel is variable.
    /// </summary>
    /// <param name="format">An uncompressed pixel format</param>
    /// <returns>The number of bytes required to store a single pixel in the given format</returns>
    public static uint GetSizeInBytes(PixelFormat format) {
        switch (format) {
            case PixelFormat.R8UNorm: case PixelFormat.R8SNorm: case PixelFormat.R8UInt: case PixelFormat.R8SInt: return 1;

            case PixelFormat.R16UNorm: case PixelFormat.R16SNorm: case PixelFormat.R16UInt: case PixelFormat.R16SInt: case PixelFormat.R16Float: case PixelFormat.R8G8UNorm: case PixelFormat.R8G8SNorm: case PixelFormat.R8G8UInt: case PixelFormat.R8G8SInt: return 2;

            case PixelFormat.R32UInt: case PixelFormat.R32SInt: case PixelFormat.R32Float: case PixelFormat.R16G16UNorm: case PixelFormat.R16G16SNorm: case PixelFormat.R16G16UInt: case PixelFormat.R16G16SInt: case PixelFormat.R16G16Float: case PixelFormat.R8G8B8A8UNorm: case PixelFormat.R8G8B8A8UNormSRgb: case PixelFormat.R8G8B8A8SNorm: case PixelFormat.R8G8B8A8UInt: case PixelFormat.R8G8B8A8SInt: case PixelFormat.B8G8R8A8UNorm: case PixelFormat.B8G8R8A8UNormSRgb: case PixelFormat.R10G10B10A2UNorm: case PixelFormat.R10G10B10A2UInt: case PixelFormat.R11G11B10Float: case PixelFormat.D24UNormS8UInt: return 4;

            case PixelFormat.D32FloatS8UInt: return 5;

            case PixelFormat.R16G16B16A16UNorm: case PixelFormat.R16G16B16A16SNorm: case PixelFormat.R16G16B16A16UInt: case PixelFormat.R16G16B16A16SInt: case PixelFormat.R16G16B16A16Float: case PixelFormat.R32G32UInt: case PixelFormat.R32G32SInt: case PixelFormat.R32G32Float: return 8;

            case PixelFormat.R32G32B32A32Float: case PixelFormat.R32G32B32A32UInt: case PixelFormat.R32G32B32A32SInt: return 16;

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
                Debug.Fail("GetSizeInBytes should not be used on a compressed format.");
                throw Illegal.Value<PixelFormat>();

            default: throw Illegal.Value<PixelFormat>();
        }
    }

    /// <summary>
    /// Given a vertex element format, returns the number of bytes required
    /// to store an element in that format.
    /// </summary>
    /// <param name="format">A vertex element format</param>
    /// <returns>The number of bytes required to store an element in the given format</returns>
    public static uint GetSizeInBytes(VertexElementFormat format) {
        switch (format) {
            case VertexElementFormat.Byte2Norm: case VertexElementFormat.Byte2: case VertexElementFormat.SByte2Norm: case VertexElementFormat.SByte2: case VertexElementFormat.Half1: return 2;

            case VertexElementFormat.Float1: case VertexElementFormat.UInt1: case VertexElementFormat.Int1: case VertexElementFormat.Byte4Norm: case VertexElementFormat.Byte4: case VertexElementFormat.SByte4Norm: case VertexElementFormat.SByte4: case VertexElementFormat.UShort2Norm: case VertexElementFormat.UShort2: case VertexElementFormat.Short2Norm: case VertexElementFormat.Short2: case VertexElementFormat.Half2: return 4;

            case VertexElementFormat.Float2: case VertexElementFormat.UInt2: case VertexElementFormat.Int2: case VertexElementFormat.UShort4Norm: case VertexElementFormat.UShort4: case VertexElementFormat.Short4Norm: case VertexElementFormat.Short4: case VertexElementFormat.Half4: return 8;

            case VertexElementFormat.Float3: case VertexElementFormat.UInt3: case VertexElementFormat.Int3: return 12;

            case VertexElementFormat.Float4: case VertexElementFormat.UInt4: case VertexElementFormat.Int4: return 16;

            default: throw Illegal.Value<VertexElementFormat>();
        }
    }
}