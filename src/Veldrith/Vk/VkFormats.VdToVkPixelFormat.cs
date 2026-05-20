using Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkFormats.
/// </summary>
internal static partial class VkFormats {

    /// <summary>
    /// Executes the vd to vk pixel format logic for this backend.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="toDepthFormat">The to depth format value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static VkFormat VdToVkPixelFormat(PixelFormat format, bool toDepthFormat = false) {
        switch (format) {
            case PixelFormat.R8UNorm: return VkFormat.R8Unorm;

            case PixelFormat.R8SNorm: return VkFormat.R8Snorm;

            case PixelFormat.R8UInt: return VkFormat.R8Uint;

            case PixelFormat.R8SInt: return VkFormat.R8Sint;

            case PixelFormat.R16UNorm: return toDepthFormat ? VkFormat.D16Unorm : VkFormat.R16Unorm;

            case PixelFormat.R16SNorm: return VkFormat.R16Snorm;

            case PixelFormat.R16UInt: return VkFormat.R16Uint;

            case PixelFormat.R16SInt: return VkFormat.R16Sint;

            case PixelFormat.R16Float: return VkFormat.R16Sfloat;

            case PixelFormat.R32UInt: return VkFormat.R32Uint;

            case PixelFormat.R32SInt: return VkFormat.R32Sint;

            case PixelFormat.R32Float: return toDepthFormat ? VkFormat.D32Sfloat : VkFormat.R32Sfloat;

            case PixelFormat.R8G8UNorm: return VkFormat.R8g8Unorm;

            case PixelFormat.R8G8SNorm: return VkFormat.R8g8Snorm;

            case PixelFormat.R8G8UInt: return VkFormat.R8g8Uint;

            case PixelFormat.R8G8SInt: return VkFormat.R8g8Sint;

            case PixelFormat.R16G16UNorm: return VkFormat.R16g16Unorm;

            case PixelFormat.R16G16SNorm: return VkFormat.R16g16Snorm;

            case PixelFormat.R16G16UInt: return VkFormat.R16g16Uint;

            case PixelFormat.R16G16SInt: return VkFormat.R16g16Sint;

            case PixelFormat.R16G16Float: return VkFormat.R16g16Sfloat;

            case PixelFormat.R32G32UInt: return VkFormat.R32g32Uint;

            case PixelFormat.R32G32SInt: return VkFormat.R32g32Sint;

            case PixelFormat.R32G32Float: return VkFormat.R32g32Sfloat;

            case PixelFormat.R8G8B8A8UNorm: return VkFormat.R8g8b8a8Unorm;

            case PixelFormat.R8G8B8A8UNormSRgb: return VkFormat.R8g8b8a8Srgb;

            case PixelFormat.B8G8R8A8UNorm: return VkFormat.B8g8r8a8Unorm;

            case PixelFormat.B8G8R8A8UNormSRgb: return VkFormat.B8g8r8a8Srgb;

            case PixelFormat.R8G8B8A8SNorm: return VkFormat.R8g8b8a8Snorm;

            case PixelFormat.R8G8B8A8UInt: return VkFormat.R8g8b8a8Uint;

            case PixelFormat.R8G8B8A8SInt: return VkFormat.R8g8b8a8Sint;

            case PixelFormat.R16G16B16A16UNorm: return VkFormat.R16g16b16a16Unorm;

            case PixelFormat.R16G16B16A16SNorm: return VkFormat.R16g16b16a16Snorm;

            case PixelFormat.R16G16B16A16UInt: return VkFormat.R16g16b16a16Uint;

            case PixelFormat.R16G16B16A16SInt: return VkFormat.R16g16b16a16Sint;

            case PixelFormat.R16G16B16A16Float: return VkFormat.R16g16b16a16Sfloat;

            case PixelFormat.R32G32B32A32UInt: return VkFormat.R32g32b32a32Uint;

            case PixelFormat.R32G32B32A32SInt: return VkFormat.R32g32b32a32Sint;

            case PixelFormat.R32G32B32A32Float: return VkFormat.R32g32b32a32Sfloat;

            case PixelFormat.Bc1RgbUNorm: return VkFormat.Bc1RgbUnormBlock;

            case PixelFormat.Bc1RgbUNormSRgb: return VkFormat.Bc1RgbSrgbBlock;

            case PixelFormat.Bc1RgbaUNorm: return VkFormat.Bc1RgbaUnormBlock;

            case PixelFormat.Bc1RgbaUNormSRgb: return VkFormat.Bc1RgbaSrgbBlock;

            case PixelFormat.Bc2UNorm: return VkFormat.Bc2UnormBlock;

            case PixelFormat.Bc2UNormSRgb: return VkFormat.Bc2SrgbBlock;

            case PixelFormat.Bc3UNorm: return VkFormat.Bc3UnormBlock;

            case PixelFormat.Bc3UNormSRgb: return VkFormat.Bc3SrgbBlock;

            case PixelFormat.Bc4UNorm: return VkFormat.Bc4UnormBlock;

            case PixelFormat.Bc4SNorm: return VkFormat.Bc4SnormBlock;

            case PixelFormat.Bc5UNorm: return VkFormat.Bc5UnormBlock;

            case PixelFormat.Bc5SNorm: return VkFormat.Bc5SnormBlock;

            case PixelFormat.Bc7UNorm: return VkFormat.Bc7UnormBlock;

            case PixelFormat.Bc7UNormSRgb: return VkFormat.Bc7SrgbBlock;

            case PixelFormat.Etc2R8G8B8UNorm: return VkFormat.Etc2R8g8b8UnormBlock;

            case PixelFormat.Etc2R8G8B8A1UNorm: return VkFormat.Etc2R8g8b8a1UnormBlock;

            case PixelFormat.Etc2R8G8B8A8UNorm: return VkFormat.Etc2R8g8b8a8UnormBlock;

            case PixelFormat.D32FloatS8UInt: return VkFormat.D32SfloatS8Uint;

            case PixelFormat.D24UNormS8UInt: return VkFormat.D24UnormS8Uint;

            case PixelFormat.R10G10B10A2UNorm: return VkFormat.A2b10g10r10UnormPack32;

            case PixelFormat.R10G10B10A2UInt: return VkFormat.A2b10g10r10UintPack32;

            case PixelFormat.R11G11B10Float: return VkFormat.B10g11r11UfloatPack32;

            default: throw new VeldridException($"Invalid {nameof(PixelFormat)}: {format}");
        }
    }
}
