using Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Represents the VkFormats class.
/// </summary>
internal static partial class VkFormats {

    /// <summary>
    /// Performs the VdToVkSamplerAddressMode operation.
    /// </summary>
    /// <param name="mode">The value of mode.</param>
    /// <returns>The result of the VdToVkSamplerAddressMode operation.</returns>
    internal static VkSamplerAddressMode VdToVkSamplerAddressMode(SamplerAddressMode mode) {
        switch (mode) {
            case SamplerAddressMode.Wrap: return VkSamplerAddressMode.Repeat;

            case SamplerAddressMode.Mirror: return VkSamplerAddressMode.MirroredRepeat;

            case SamplerAddressMode.Clamp: return VkSamplerAddressMode.ClampToEdge;

            case SamplerAddressMode.Border: return VkSamplerAddressMode.ClampToBorder;

            default: throw Illegal.Value<SamplerAddressMode>();
        }
    }

    /// <summary>
    /// Performs the GetFilterParams operation.
    /// </summary>
    /// <param name="filter">The value of filter.</param>
    /// <param name="minFilter">The value of minFilter.</param>
    /// <param name="magFilter">The value of magFilter.</param>
    /// <param name="mipmapMode">The value of mipmapMode.</param>
    internal static void GetFilterParams(SamplerFilter filter, out VkFilter minFilter, out VkFilter magFilter, out VkSamplerMipmapMode mipmapMode) {
        switch (filter) {
            case SamplerFilter.Anisotropic:
                minFilter = VkFilter.Linear;
                magFilter = VkFilter.Linear;
                mipmapMode = VkSamplerMipmapMode.Linear;
                break;

            case SamplerFilter.MinPointMagPointMipPoint:
                minFilter = VkFilter.Nearest;
                magFilter = VkFilter.Nearest;
                mipmapMode = VkSamplerMipmapMode.Nearest;
                break;

            case SamplerFilter.MinPointMagPointMipLinear:
                minFilter = VkFilter.Nearest;
                magFilter = VkFilter.Nearest;
                mipmapMode = VkSamplerMipmapMode.Linear;
                break;

            case SamplerFilter.MinPointMagLinearMipPoint:
                minFilter = VkFilter.Nearest;
                magFilter = VkFilter.Linear;
                mipmapMode = VkSamplerMipmapMode.Nearest;
                break;

            case SamplerFilter.MinPointMagLinearMipLinear:
                minFilter = VkFilter.Nearest;
                magFilter = VkFilter.Linear;
                mipmapMode = VkSamplerMipmapMode.Linear;
                break;

            case SamplerFilter.MinLinearMagPointMipPoint:
                minFilter = VkFilter.Linear;
                magFilter = VkFilter.Nearest;
                mipmapMode = VkSamplerMipmapMode.Nearest;
                break;

            case SamplerFilter.MinLinearMagPointMipLinear:
                minFilter = VkFilter.Linear;
                magFilter = VkFilter.Nearest;
                mipmapMode = VkSamplerMipmapMode.Linear;
                break;

            case SamplerFilter.MinLinearMagLinearMipPoint:
                minFilter = VkFilter.Linear;
                magFilter = VkFilter.Linear;
                mipmapMode = VkSamplerMipmapMode.Nearest;
                break;

            case SamplerFilter.MinLinearMagLinearMipLinear:
                minFilter = VkFilter.Linear;
                magFilter = VkFilter.Linear;
                mipmapMode = VkSamplerMipmapMode.Linear;
                break;

            default: throw Illegal.Value<SamplerFilter>();
        }
    }

    /// <summary>
    /// Performs the VdToVkTextureUsage operation.
    /// </summary>
    /// <param name="vdUsage">The value of vdUsage.</param>
    /// <returns>The result of the VdToVkTextureUsage operation.</returns>
    internal static VkImageUsageFlags VdToVkTextureUsage(TextureUsage vdUsage) {
        VkImageUsageFlags vkUsage = VkImageUsageFlags.TransferDst | VkImageUsageFlags.TransferSrc;
        bool isDepthStencil = (vdUsage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil;

        if ((vdUsage & TextureUsage.Sampled) == TextureUsage.Sampled) {
            vkUsage |= VkImageUsageFlags.Sampled;
        }

        if (isDepthStencil) {
            vkUsage |= VkImageUsageFlags.DepthStencilAttachment;
        }

        if ((vdUsage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget) {
            vkUsage |= VkImageUsageFlags.ColorAttachment;
        }

        if ((vdUsage & TextureUsage.Storage) == TextureUsage.Storage) {
            vkUsage |= VkImageUsageFlags.Storage;
        }

        return vkUsage;
    }

    /// <summary>
    /// Performs the VdToVkTextureType operation.
    /// </summary>
    /// <param name="type">The value of type.</param>
    /// <returns>The result of the VdToVkTextureType operation.</returns>
    internal static VkImageType VdToVkTextureType(TextureType type) {
        switch (type) {
            case TextureType.Texture1D: return VkImageType.Image1D;

            case TextureType.Texture2D: return VkImageType.Image2D;

            case TextureType.Texture3D: return VkImageType.Image3D;

            default: throw Illegal.Value<TextureType>();
        }
    }

    /// <summary>
    /// Performs the VdToVkDescriptorType operation.
    /// </summary>
    /// <param name="kind">The value of kind.</param>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the VdToVkDescriptorType operation.</returns>
    internal static VkDescriptorType VdToVkDescriptorType(ResourceKind kind, ResourceLayoutElementOptions options) {
        bool dynamicBinding = (options & ResourceLayoutElementOptions.DynamicBinding) != 0;

        switch (kind) {
            case ResourceKind.UniformBuffer: return dynamicBinding ? VkDescriptorType.UniformBufferDynamic : VkDescriptorType.UniformBuffer;

            case ResourceKind.StructuredBufferReadWrite: case ResourceKind.StructuredBufferReadOnly: return dynamicBinding ? VkDescriptorType.StorageBufferDynamic : VkDescriptorType.StorageBuffer;

            case ResourceKind.TextureReadOnly: return VkDescriptorType.SampledImage;

            case ResourceKind.TextureReadWrite: return VkDescriptorType.StorageImage;

            case ResourceKind.Sampler: return VkDescriptorType.Sampler;

            default: throw Illegal.Value<ResourceKind>();
        }
    }

    /// <summary>
    /// Performs the VdToVkSampleCount operation.
    /// </summary>
    /// <param name="sampleCount">The value of sampleCount.</param>
    /// <returns>The result of the VdToVkSampleCount operation.</returns>
    internal static VkSampleCountFlags VdToVkSampleCount(TextureSampleCount sampleCount) {
        switch (sampleCount) {
            case TextureSampleCount.Count1: return VkSampleCountFlags.Count1;

            case TextureSampleCount.Count2: return VkSampleCountFlags.Count2;

            case TextureSampleCount.Count4: return VkSampleCountFlags.Count4;

            case TextureSampleCount.Count8: return VkSampleCountFlags.Count8;

            case TextureSampleCount.Count16: return VkSampleCountFlags.Count16;

            case TextureSampleCount.Count32: return VkSampleCountFlags.Count32;

            default: throw Illegal.Value<TextureSampleCount>();
        }
    }

    /// <summary>
    /// Performs the VdToVkStencilOp operation.
    /// </summary>
    /// <param name="op">The value of op.</param>
    /// <returns>The result of the VdToVkStencilOp operation.</returns>
    internal static VkStencilOp VdToVkStencilOp(StencilOperation op) {
        switch (op) {
            case StencilOperation.Keep: return VkStencilOp.Keep;

            case StencilOperation.Zero: return VkStencilOp.Zero;

            case StencilOperation.Replace: return VkStencilOp.Replace;

            case StencilOperation.IncrementAndClamp: return VkStencilOp.IncrementAndClamp;

            case StencilOperation.DecrementAndClamp: return VkStencilOp.DecrementAndClamp;

            case StencilOperation.Invert: return VkStencilOp.Invert;

            case StencilOperation.IncrementAndWrap: return VkStencilOp.IncrementAndWrap;

            case StencilOperation.DecrementAndWrap: return VkStencilOp.DecrementAndWrap;

            default: throw Illegal.Value<StencilOperation>();
        }
    }

    /// <summary>
    /// Performs the VdToVkPolygonMode operation.
    /// </summary>
    /// <param name="fillMode">The value of fillMode.</param>
    /// <returns>The result of the VdToVkPolygonMode operation.</returns>
    internal static VkPolygonMode VdToVkPolygonMode(PolygonFillMode fillMode) {
        switch (fillMode) {
            case PolygonFillMode.Solid: return VkPolygonMode.Fill;

            case PolygonFillMode.Wireframe: return VkPolygonMode.Line;

            default: throw Illegal.Value<PolygonFillMode>();
        }
    }

    /// <summary>
    /// Performs the VdToVkCullMode operation.
    /// </summary>
    /// <param name="cullMode">The value of cullMode.</param>
    /// <returns>The result of the VdToVkCullMode operation.</returns>
    internal static VkCullModeFlags VdToVkCullMode(FaceCullMode cullMode) {
        switch (cullMode) {
            case FaceCullMode.Back: return VkCullModeFlags.Back;

            case FaceCullMode.Front: return VkCullModeFlags.Front;

            case FaceCullMode.None: return VkCullModeFlags.None;

            default: throw Illegal.Value<FaceCullMode>();
        }
    }

    /// <summary>
    /// Performs the VdToVkBlendOp operation.
    /// </summary>
    /// <param name="func">The value of func.</param>
    /// <returns>The result of the VdToVkBlendOp operation.</returns>
    internal static VkBlendOp VdToVkBlendOp(BlendFunction func) {
        switch (func) {
            case BlendFunction.Add: return VkBlendOp.Add;

            case BlendFunction.Subtract: return VkBlendOp.Subtract;

            case BlendFunction.ReverseSubtract: return VkBlendOp.ReverseSubtract;

            case BlendFunction.Minimum: return VkBlendOp.Min;

            case BlendFunction.Maximum: return VkBlendOp.Max;

            default: throw Illegal.Value<BlendFunction>();
        }
    }

    /// <summary>
    /// Performs the VdToVkColorWriteMask operation.
    /// </summary>
    /// <param name="mask">The value of mask.</param>
    /// <returns>The result of the VdToVkColorWriteMask operation.</returns>
    internal static VkColorComponentFlags VdToVkColorWriteMask(ColorWriteMask mask) {
        VkColorComponentFlags flags = VkColorComponentFlags.None;

        if ((mask & ColorWriteMask.Red) == ColorWriteMask.Red) {
            flags |= VkColorComponentFlags.R;
        }

        if ((mask & ColorWriteMask.Green) == ColorWriteMask.Green) {
            flags |= VkColorComponentFlags.G;
        }

        if ((mask & ColorWriteMask.Blue) == ColorWriteMask.Blue) {
            flags |= VkColorComponentFlags.B;
        }

        if ((mask & ColorWriteMask.Alpha) == ColorWriteMask.Alpha) {
            flags |= VkColorComponentFlags.A;
        }

        return flags;
    }

    /// <summary>
    /// Performs the VdToVkPrimitiveTopology operation.
    /// </summary>
    /// <param name="topology">The value of topology.</param>
    /// <returns>The result of the VdToVkPrimitiveTopology operation.</returns>
    internal static VkPrimitiveTopology VdToVkPrimitiveTopology(PrimitiveTopology topology) {
        switch (topology) {
            case PrimitiveTopology.TriangleList: return VkPrimitiveTopology.TriangleList;

            case PrimitiveTopology.TriangleStrip: return VkPrimitiveTopology.TriangleStrip;

            case PrimitiveTopology.LineList: return VkPrimitiveTopology.LineList;

            case PrimitiveTopology.LineStrip: return VkPrimitiveTopology.LineStrip;

            case PrimitiveTopology.PointList: return VkPrimitiveTopology.PointList;

            default: throw Illegal.Value<PrimitiveTopology>();
        }
    }

    /// <summary>
    /// Performs the GetSpecializationConstantSize operation.
    /// </summary>
    /// <param name="type">The value of type.</param>
    /// <returns>The result of the GetSpecializationConstantSize operation.</returns>
    internal static uint GetSpecializationConstantSize(ShaderConstantType type) {
        switch (type) {
            case ShaderConstantType.Bool: return 4;

            case ShaderConstantType.UInt16: return 2;

            case ShaderConstantType.Int16: return 2;

            case ShaderConstantType.UInt32: return 4;

            case ShaderConstantType.Int32: return 4;

            case ShaderConstantType.UInt64: return 8;

            case ShaderConstantType.Int64: return 8;

            case ShaderConstantType.Float: return 4;

            case ShaderConstantType.Double: return 8;

            default: throw Illegal.Value<ShaderConstantType>();
        }
    }

    /// <summary>
    /// Performs the VdToVkBlendFactor operation.
    /// </summary>
    /// <param name="factor">The value of factor.</param>
    /// <returns>The result of the VdToVkBlendFactor operation.</returns>
    internal static VkBlendFactor VdToVkBlendFactor(BlendFactor factor) {
        switch (factor) {
            case BlendFactor.Zero: return VkBlendFactor.Zero;

            case BlendFactor.One: return VkBlendFactor.One;

            case BlendFactor.SourceAlpha: return VkBlendFactor.SrcAlpha;

            case BlendFactor.InverseSourceAlpha: return VkBlendFactor.OneMinusSrcAlpha;

            case BlendFactor.DestinationAlpha: return VkBlendFactor.DstAlpha;

            case BlendFactor.InverseDestinationAlpha: return VkBlendFactor.OneMinusDstAlpha;

            case BlendFactor.SourceColor: return VkBlendFactor.SrcColor;

            case BlendFactor.InverseSourceColor: return VkBlendFactor.OneMinusSrcColor;

            case BlendFactor.DestinationColor: return VkBlendFactor.DstColor;

            case BlendFactor.InverseDestinationColor: return VkBlendFactor.OneMinusDstColor;

            case BlendFactor.BlendFactor: return VkBlendFactor.ConstantColor;

            case BlendFactor.InverseBlendFactor: return VkBlendFactor.OneMinusConstantColor;

            default: throw Illegal.Value<BlendFactor>();
        }
    }

    /// <summary>
    /// Performs the VdToVkVertexElementFormat operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <returns>The result of the VdToVkVertexElementFormat operation.</returns>
    internal static VkFormat VdToVkVertexElementFormat(VertexElementFormat format) {
        switch (format) {
            case VertexElementFormat.Float1: return VkFormat.R32Sfloat;

            case VertexElementFormat.Float2: return VkFormat.R32g32Sfloat;

            case VertexElementFormat.Float3: return VkFormat.R32g32b32Sfloat;

            case VertexElementFormat.Float4: return VkFormat.R32g32b32a32Sfloat;

            case VertexElementFormat.Byte2Norm: return VkFormat.R8g8Unorm;

            case VertexElementFormat.Byte2: return VkFormat.R8g8Uint;

            case VertexElementFormat.Byte4Norm: return VkFormat.R8g8b8a8Unorm;

            case VertexElementFormat.Byte4: return VkFormat.R8g8b8a8Uint;

            case VertexElementFormat.SByte2Norm: return VkFormat.R8g8Snorm;

            case VertexElementFormat.SByte2: return VkFormat.R8g8Sint;

            case VertexElementFormat.SByte4Norm: return VkFormat.R8g8b8a8Snorm;

            case VertexElementFormat.SByte4: return VkFormat.R8g8b8a8Sint;

            case VertexElementFormat.UShort2Norm: return VkFormat.R16g16Unorm;

            case VertexElementFormat.UShort2: return VkFormat.R16g16Uint;

            case VertexElementFormat.UShort4Norm: return VkFormat.R16g16b16a16Unorm;

            case VertexElementFormat.UShort4: return VkFormat.R16g16b16a16Uint;

            case VertexElementFormat.Short2Norm: return VkFormat.R16g16Snorm;

            case VertexElementFormat.Short2: return VkFormat.R16g16Sint;

            case VertexElementFormat.Short4Norm: return VkFormat.R16g16b16a16Snorm;

            case VertexElementFormat.Short4: return VkFormat.R16g16b16a16Sint;

            case VertexElementFormat.UInt1: return VkFormat.R32Uint;

            case VertexElementFormat.UInt2: return VkFormat.R32g32Uint;

            case VertexElementFormat.UInt3: return VkFormat.R32g32b32Uint;

            case VertexElementFormat.UInt4: return VkFormat.R32g32b32a32Uint;

            case VertexElementFormat.Int1: return VkFormat.R32Sint;

            case VertexElementFormat.Int2: return VkFormat.R32g32Sint;

            case VertexElementFormat.Int3: return VkFormat.R32g32b32Sint;

            case VertexElementFormat.Int4: return VkFormat.R32g32b32a32Sint;

            case VertexElementFormat.Half1: return VkFormat.R16Sfloat;

            case VertexElementFormat.Half2: return VkFormat.R16g16Sfloat;

            case VertexElementFormat.Half4: return VkFormat.R16g16b16a16Sfloat;

            default: throw Illegal.Value<VertexElementFormat>();
        }
    }

    /// <summary>
    /// Performs the VdToVkShaderStages operation.
    /// </summary>
    /// <param name="stage">The value of stage.</param>
    /// <returns>The result of the VdToVkShaderStages operation.</returns>
    internal static VkShaderStageFlags VdToVkShaderStages(ShaderStages stage) {
        VkShaderStageFlags ret = VkShaderStageFlags.None;

        if ((stage & ShaderStages.Vertex) == ShaderStages.Vertex) {
            ret |= VkShaderStageFlags.Vertex;
        }

        if ((stage & ShaderStages.Geometry) == ShaderStages.Geometry) {
            ret |= VkShaderStageFlags.Geometry;
        }

        if ((stage & ShaderStages.TessellationControl) == ShaderStages.TessellationControl) {
            ret |= VkShaderStageFlags.TessellationControl;
        }

        if ((stage & ShaderStages.TessellationEvaluation) == ShaderStages.TessellationEvaluation) {
            ret |= VkShaderStageFlags.TessellationEvaluation;
        }

        if ((stage & ShaderStages.Fragment) == ShaderStages.Fragment) {
            ret |= VkShaderStageFlags.Fragment;
        }

        if ((stage & ShaderStages.Compute) == ShaderStages.Compute) {
            ret |= VkShaderStageFlags.Compute;
        }

        return ret;
    }

    /// <summary>
    /// Performs the VdToVkSamplerBorderColor operation.
    /// </summary>
    /// <param name="borderColor">The value of borderColor.</param>
    /// <returns>The result of the VdToVkSamplerBorderColor operation.</returns>
    internal static VkBorderColor VdToVkSamplerBorderColor(SamplerBorderColor borderColor) {
        switch (borderColor) {
            case SamplerBorderColor.TransparentBlack: return VkBorderColor.FloatTransparentBlack;

            case SamplerBorderColor.OpaqueBlack: return VkBorderColor.FloatOpaqueBlack;

            case SamplerBorderColor.OpaqueWhite: return VkBorderColor.FloatOpaqueWhite;

            default: throw Illegal.Value<SamplerBorderColor>();
        }
    }

    /// <summary>
    /// Performs the VdToVkIndexFormat operation.
    /// </summary>
    /// <param name="format">The value of format.</param>
    /// <returns>The result of the VdToVkIndexFormat operation.</returns>
    internal static VkIndexType VdToVkIndexFormat(IndexFormat format) {
        switch (format) {
            case IndexFormat.UInt16: return VkIndexType.Uint16;

            case IndexFormat.UInt32: return VkIndexType.Uint32;

            default: throw Illegal.Value<IndexFormat>();
        }
    }

    /// <summary>
    /// Performs the VdToVkCompareOp operation.
    /// </summary>
    /// <param name="comparisonKind">The value of comparisonKind.</param>
    /// <returns>The result of the VdToVkCompareOp operation.</returns>
    internal static VkCompareOp VdToVkCompareOp(ComparisonKind comparisonKind) {
        switch (comparisonKind) {
            case ComparisonKind.Never: return VkCompareOp.Never;

            case ComparisonKind.Less: return VkCompareOp.Less;

            case ComparisonKind.Equal: return VkCompareOp.Equal;

            case ComparisonKind.LessEqual: return VkCompareOp.LessOrEqual;

            case ComparisonKind.Greater: return VkCompareOp.Greater;

            case ComparisonKind.NotEqual: return VkCompareOp.NotEqual;

            case ComparisonKind.GreaterEqual: return VkCompareOp.GreaterOrEqual;

            case ComparisonKind.Always: return VkCompareOp.Always;

            default: throw Illegal.Value<ComparisonKind>();
        }
    }

    /// <summary>
    /// Performs the VkToVdPixelFormat operation.
    /// </summary>
    /// <param name="vkFormat">The value of vkFormat.</param>
    /// <returns>The result of the VkToVdPixelFormat operation.</returns>
    internal static PixelFormat VkToVdPixelFormat(VkFormat vkFormat) {
        switch (vkFormat) {
            case VkFormat.R8Unorm: return PixelFormat.R8UNorm;

            case VkFormat.R8Snorm: return PixelFormat.R8SNorm;

            case VkFormat.R8Uint: return PixelFormat.R8UInt;

            case VkFormat.R8Sint: return PixelFormat.R8SInt;

            case VkFormat.R16Unorm: return PixelFormat.R16UNorm;

            case VkFormat.R16Snorm: return PixelFormat.R16SNorm;

            case VkFormat.R16Uint: return PixelFormat.R16UInt;

            case VkFormat.R16Sint: return PixelFormat.R16SInt;

            case VkFormat.R16Sfloat: return PixelFormat.R16Float;

            case VkFormat.R32Uint: return PixelFormat.R32UInt;

            case VkFormat.R32Sint: return PixelFormat.R32SInt;

            case VkFormat.R32Sfloat: case VkFormat.D32Sfloat: return PixelFormat.R32Float;

            case VkFormat.R8g8Unorm: return PixelFormat.R8G8UNorm;

            case VkFormat.R8g8Snorm: return PixelFormat.R8G8SNorm;

            case VkFormat.R8g8Uint: return PixelFormat.R8G8UInt;

            case VkFormat.R8g8Sint: return PixelFormat.R8G8SInt;

            case VkFormat.R16g16Unorm: return PixelFormat.R16G16UNorm;

            case VkFormat.R16g16Snorm: return PixelFormat.R16G16SNorm;

            case VkFormat.R16g16Uint: return PixelFormat.R16G16UInt;

            case VkFormat.R16g16Sint: return PixelFormat.R16G16SInt;

            case VkFormat.R16g16Sfloat: return PixelFormat.R16G16Float;

            case VkFormat.R32g32Uint: return PixelFormat.R32G32UInt;

            case VkFormat.R32g32Sint: return PixelFormat.R32G32SInt;

            case VkFormat.R32g32Sfloat: return PixelFormat.R32G32Float;

            case VkFormat.R8g8b8a8Unorm: return PixelFormat.R8G8B8A8UNorm;

            case VkFormat.R8g8b8a8Srgb: return PixelFormat.R8G8B8A8UNormSRgb;

            case VkFormat.B8g8r8a8Unorm: return PixelFormat.B8G8R8A8UNorm;

            case VkFormat.B8g8r8a8Srgb: return PixelFormat.B8G8R8A8UNormSRgb;

            case VkFormat.R8g8b8a8Snorm: return PixelFormat.R8G8B8A8SNorm;

            case VkFormat.R8g8b8a8Uint: return PixelFormat.R8G8B8A8UInt;

            case VkFormat.R8g8b8a8Sint: return PixelFormat.R8G8B8A8SInt;

            case VkFormat.R16g16b16a16Unorm: return PixelFormat.R16G16B16A16UNorm;

            case VkFormat.R16g16b16a16Snorm: return PixelFormat.R16G16B16A16SNorm;

            case VkFormat.R16g16b16a16Uint: return PixelFormat.R16G16B16A16UInt;

            case VkFormat.R16g16b16a16Sint: return PixelFormat.R16G16B16A16SInt;

            case VkFormat.R16g16b16a16Sfloat: return PixelFormat.R16G16B16A16Float;

            case VkFormat.R32g32b32a32Uint: return PixelFormat.R32G32B32A32UInt;

            case VkFormat.R32g32b32a32Sint: return PixelFormat.R32G32B32A32SInt;

            case VkFormat.R32g32b32a32Sfloat: return PixelFormat.R32G32B32A32Float;

            case VkFormat.Bc1RgbUnormBlock: return PixelFormat.Bc1RgbUNorm;

            case VkFormat.Bc1RgbSrgbBlock: return PixelFormat.Bc1RgbUNormSRgb;

            case VkFormat.Bc1RgbaUnormBlock: return PixelFormat.Bc1RgbaUNorm;

            case VkFormat.Bc1RgbaSrgbBlock: return PixelFormat.Bc1RgbaUNormSRgb;

            case VkFormat.Bc2UnormBlock: return PixelFormat.Bc2UNorm;

            case VkFormat.Bc2SrgbBlock: return PixelFormat.Bc2UNormSRgb;

            case VkFormat.Bc3UnormBlock: return PixelFormat.Bc3UNorm;

            case VkFormat.Bc3SrgbBlock: return PixelFormat.Bc3UNormSRgb;

            case VkFormat.Bc4UnormBlock: return PixelFormat.Bc4UNorm;

            case VkFormat.Bc4SnormBlock: return PixelFormat.Bc4SNorm;

            case VkFormat.Bc5UnormBlock: return PixelFormat.Bc5UNorm;

            case VkFormat.Bc5SnormBlock: return PixelFormat.Bc5SNorm;

            case VkFormat.Bc7UnormBlock: return PixelFormat.Bc7UNorm;

            case VkFormat.Bc7SrgbBlock: return PixelFormat.Bc7UNormSRgb;

            case VkFormat.A2b10g10r10UnormPack32: return PixelFormat.R10G10B10A2UNorm;

            case VkFormat.A2b10g10r10UintPack32: return PixelFormat.R10G10B10A2UInt;

            case VkFormat.B10g11r11UfloatPack32: return PixelFormat.R11G11B10Float;

            default: throw Illegal.Value<VkFormat>();
        }
    }
}