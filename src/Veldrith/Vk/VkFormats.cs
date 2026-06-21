using Vortice.Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkFormats.
/// </summary>
internal static partial class VkFormats {

    /// <summary>
    /// Executes the vd to vk sampler address mode logic for this backend.
    /// </summary>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Gets the filter params value.
    /// </summary>
    /// <param name="filter">The filter value used by this operation.</param>
    /// <param name="minFilter">The min filter value used by this operation.</param>
    /// <param name="magFilter">The mag filter value used by this operation.</param>
    /// <param name="mipmapMode">The mipmap mode value used by this operation.</param>
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
    /// Executes the vd to vk texture usage logic for this backend.
    /// </summary>
    /// <param name="vdUsage">The vd usage value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the vd to vk texture type logic for this backend.
    /// </summary>
    /// <param name="type">The type value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static VkImageType VdToVkTextureType(TextureType type) {
        switch (type) {
            case TextureType.Texture1D: return VkImageType.Image1D;

            case TextureType.Texture2D: return VkImageType.Image2D;

            case TextureType.Texture3D: return VkImageType.Image3D;

            default: throw Illegal.Value<TextureType>();
        }
    }

    /// <summary>
    /// Executes the vd to vk descriptor type logic for this backend.
    /// </summary>
    /// <param name="kind">The kind value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the vd to vk sample count logic for this backend.
    /// </summary>
    /// <param name="sampleCount">The sample count value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the vd to vk stencil op logic for this backend.
    /// </summary>
    /// <param name="op">The op value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the vd to vk polygon mode logic for this backend.
    /// </summary>
    /// <param name="fillMode">The fill mode value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static VkPolygonMode VdToVkPolygonMode(PolygonFillMode fillMode) {
        switch (fillMode) {
            case PolygonFillMode.Solid: return VkPolygonMode.Fill;

            case PolygonFillMode.Wireframe: return VkPolygonMode.Line;

            default: throw Illegal.Value<PolygonFillMode>();
        }
    }

    /// <summary>
    /// Executes the vd to vk cull mode logic for this backend.
    /// </summary>
    /// <param name="cullMode">The cull mode value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static VkCullModeFlags VdToVkCullMode(FaceCullMode cullMode) {
        switch (cullMode) {
            case FaceCullMode.Back: return VkCullModeFlags.Back;

            case FaceCullMode.Front: return VkCullModeFlags.Front;

            case FaceCullMode.None: return VkCullModeFlags.None;

            default: throw Illegal.Value<FaceCullMode>();
        }
    }

    /// <summary>
    /// Executes the vd to vk blend op logic for this backend.
    /// </summary>
    /// <param name="func">The func value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the vd to vk color write mask logic for this backend.
    /// </summary>
    /// <param name="mask">The mask value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the vd to vk primitive topology logic for this backend.
    /// </summary>
    /// <param name="topology">The topology value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Gets the specialization constant size value.
    /// </summary>
    /// <param name="type">The type value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the vd to vk blend factor logic for this backend.
    /// </summary>
    /// <param name="factor">The factor value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the vd to vk vertex element format logic for this backend.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static VkFormat VdToVkVertexElementFormat(VertexElementFormat format) {
        switch (format) {
            case VertexElementFormat.Float1: return VkFormat.R32Sfloat;

            case VertexElementFormat.Float2: return VkFormat.R32G32Sfloat;

            case VertexElementFormat.Float3: return VkFormat.R32G32B32Sfloat;

            case VertexElementFormat.Float4: return VkFormat.R32G32B32A32Sfloat;

            case VertexElementFormat.Byte2Norm: return VkFormat.R8G8Unorm;

            case VertexElementFormat.Byte2: return VkFormat.R8G8Uint;

            case VertexElementFormat.Byte4Norm: return VkFormat.R8G8B8A8Unorm;

            case VertexElementFormat.Byte4: return VkFormat.R8G8B8A8Uint;

            case VertexElementFormat.SByte2Norm: return VkFormat.R8G8Snorm;

            case VertexElementFormat.SByte2: return VkFormat.R8G8Sint;

            case VertexElementFormat.SByte4Norm: return VkFormat.R8G8B8A8Snorm;

            case VertexElementFormat.SByte4: return VkFormat.R8G8B8A8Sint;

            case VertexElementFormat.UShort2Norm: return VkFormat.R16G16Unorm;

            case VertexElementFormat.UShort2: return VkFormat.R16G16Uint;

            case VertexElementFormat.UShort4Norm: return VkFormat.R16G16B16A16Unorm;

            case VertexElementFormat.UShort4: return VkFormat.R16G16B16A16Uint;

            case VertexElementFormat.Short2Norm: return VkFormat.R16G16Snorm;

            case VertexElementFormat.Short2: return VkFormat.R16G16Sint;

            case VertexElementFormat.Short4Norm: return VkFormat.R16G16B16A16Snorm;

            case VertexElementFormat.Short4: return VkFormat.R16G16B16A16Sint;

            case VertexElementFormat.UInt1: return VkFormat.R32Uint;

            case VertexElementFormat.UInt2: return VkFormat.R32G32Uint;

            case VertexElementFormat.UInt3: return VkFormat.R32G32B32Uint;

            case VertexElementFormat.UInt4: return VkFormat.R32G32B32A32Uint;

            case VertexElementFormat.Int1: return VkFormat.R32Sint;

            case VertexElementFormat.Int2: return VkFormat.R32G32Sint;

            case VertexElementFormat.Int3: return VkFormat.R32G32B32Sint;

            case VertexElementFormat.Int4: return VkFormat.R32G32B32A32Sint;

            case VertexElementFormat.Half1: return VkFormat.R16Sfloat;

            case VertexElementFormat.Half2: return VkFormat.R16G16Sfloat;

            case VertexElementFormat.Half4: return VkFormat.R16G16B16A16Sfloat;

            default: throw Illegal.Value<VertexElementFormat>();
        }
    }

    /// <summary>
    /// Executes the vd to vk shader stages logic for this backend.
    /// </summary>
    /// <param name="stage">The stage value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the vd to vk sampler border color logic for this backend.
    /// </summary>
    /// <param name="borderColor">The border color value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static VkBorderColor VdToVkSamplerBorderColor(SamplerBorderColor borderColor) {
        switch (borderColor) {
            case SamplerBorderColor.TransparentBlack: return VkBorderColor.FloatTransparentBlack;

            case SamplerBorderColor.OpaqueBlack: return VkBorderColor.FloatOpaqueBlack;

            case SamplerBorderColor.OpaqueWhite: return VkBorderColor.FloatOpaqueWhite;

            default: throw Illegal.Value<SamplerBorderColor>();
        }
    }

    /// <summary>
    /// Executes the vd to vk index format logic for this backend.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static VkIndexType VdToVkIndexFormat(IndexFormat format) {
        switch (format) {
            case IndexFormat.UInt16: return VkIndexType.Uint16;

            case IndexFormat.UInt32: return VkIndexType.Uint32;

            default: throw Illegal.Value<IndexFormat>();
        }
    }

    /// <summary>
    /// Executes the vd to vk compare op logic for this backend.
    /// </summary>
    /// <param name="comparisonKind">The comparison kind value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the vk to vd pixel format logic for this backend.
    /// </summary>
    /// <param name="vkFormat">The vk format value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
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

            case VkFormat.R8G8Unorm: return PixelFormat.R8G8UNorm;

            case VkFormat.R8G8Snorm: return PixelFormat.R8G8SNorm;

            case VkFormat.R8G8Uint: return PixelFormat.R8G8UInt;

            case VkFormat.R8G8Sint: return PixelFormat.R8G8SInt;

            case VkFormat.R16G16Unorm: return PixelFormat.R16G16UNorm;

            case VkFormat.R16G16Snorm: return PixelFormat.R16G16SNorm;

            case VkFormat.R16G16Uint: return PixelFormat.R16G16UInt;

            case VkFormat.R16G16Sint: return PixelFormat.R16G16SInt;

            case VkFormat.R16G16Sfloat: return PixelFormat.R16G16Float;

            case VkFormat.R32G32Uint: return PixelFormat.R32G32UInt;

            case VkFormat.R32G32Sint: return PixelFormat.R32G32SInt;

            case VkFormat.R32G32Sfloat: return PixelFormat.R32G32Float;

            case VkFormat.R8G8B8A8Unorm: return PixelFormat.R8G8B8A8UNorm;

            case VkFormat.R8G8B8A8Srgb: return PixelFormat.R8G8B8A8UNormSRgb;

            case VkFormat.B8G8R8A8Unorm: return PixelFormat.B8G8R8A8UNorm;

            case VkFormat.B8G8R8A8Srgb: return PixelFormat.B8G8R8A8UNormSRgb;

            case VkFormat.R8G8B8A8Snorm: return PixelFormat.R8G8B8A8SNorm;

            case VkFormat.R8G8B8A8Uint: return PixelFormat.R8G8B8A8UInt;

            case VkFormat.R8G8B8A8Sint: return PixelFormat.R8G8B8A8SInt;

            case VkFormat.R16G16B16A16Unorm: return PixelFormat.R16G16B16A16UNorm;

            case VkFormat.R16G16B16A16Snorm: return PixelFormat.R16G16B16A16SNorm;

            case VkFormat.R16G16B16A16Uint: return PixelFormat.R16G16B16A16UInt;

            case VkFormat.R16G16B16A16Sint: return PixelFormat.R16G16B16A16SInt;

            case VkFormat.R16G16B16A16Sfloat: return PixelFormat.R16G16B16A16Float;

            case VkFormat.R32G32B32A32Uint: return PixelFormat.R32G32B32A32UInt;

            case VkFormat.R32G32B32A32Sint: return PixelFormat.R32G32B32A32SInt;

            case VkFormat.R32G32B32A32Sfloat: return PixelFormat.R32G32B32A32Float;

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

            case VkFormat.A2B10G10R10UnormPack32: return PixelFormat.R10G10B10A2UNorm;

            case VkFormat.A2B10G10R10UintPack32: return PixelFormat.R10G10B10A2UInt;

            case VkFormat.B10G11R11UfloatPack32: return PixelFormat.R11G11B10Float;

            default: throw Illegal.Value<VkFormat>();
        }
    }
}