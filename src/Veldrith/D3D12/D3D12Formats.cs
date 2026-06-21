using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12Formats.
/// </summary>
internal static class D3D12Formats {

    /// <summary>
    /// Executes the to dxgi format logic for this backend.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="depthFormat">The depth format value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static Format ToDxgiFormat(PixelFormat format, bool depthFormat = false) {
        switch (format) {
            case PixelFormat.R8UNorm: return Format.R8_UNorm;
            case PixelFormat.R8SNorm: return Format.R8_SNorm;
            case PixelFormat.R8UInt: return Format.R8_UInt;
            case PixelFormat.R8SInt: return Format.R8_SInt;
            case PixelFormat.R16UNorm: return depthFormat ? Format.R16_Typeless : Format.R16_UNorm;
            case PixelFormat.R16SNorm: return Format.R16_SNorm;
            case PixelFormat.R16UInt: return Format.R16_UInt;
            case PixelFormat.R16SInt: return Format.R16_SInt;
            case PixelFormat.R16Float: return Format.R16_Float;
            case PixelFormat.R32UInt: return Format.R32_UInt;
            case PixelFormat.R32SInt: return Format.R32_SInt;
            case PixelFormat.R32Float: return depthFormat ? Format.R32_Typeless : Format.R32_Float;
            case PixelFormat.R8G8UNorm: return Format.R8G8_UNorm;
            case PixelFormat.R8G8SNorm: return Format.R8G8_SNorm;
            case PixelFormat.R8G8UInt: return Format.R8G8_UInt;
            case PixelFormat.R8G8SInt: return Format.R8G8_SInt;
            case PixelFormat.R16G16UNorm: return Format.R16G16_UNorm;
            case PixelFormat.R16G16SNorm: return Format.R16G16_SNorm;
            case PixelFormat.R16G16UInt: return Format.R16G16_UInt;
            case PixelFormat.R16G16SInt: return Format.R16G16_SInt;
            case PixelFormat.R16G16Float: return Format.R16G16_Float;
            case PixelFormat.R32G32UInt: return Format.R32G32_UInt;
            case PixelFormat.R32G32SInt: return Format.R32G32_SInt;
            case PixelFormat.R32G32Float: return Format.R32G32_Float;
            case PixelFormat.R8G8B8A8UNorm: return Format.R8G8B8A8_UNorm;
            case PixelFormat.R8G8B8A8UNormSRgb: return Format.R8G8B8A8_UNorm_SRgb;
            case PixelFormat.B8G8R8A8UNorm: return Format.B8G8R8A8_UNorm;
            case PixelFormat.B8G8R8A8UNormSRgb: return Format.B8G8R8A8_UNorm_SRgb;
            case PixelFormat.R8G8B8A8SNorm: return Format.R8G8B8A8_SNorm;
            case PixelFormat.R8G8B8A8UInt: return Format.R8G8B8A8_UInt;
            case PixelFormat.R8G8B8A8SInt: return Format.R8G8B8A8_SInt;
            case PixelFormat.R16G16B16A16UNorm: return Format.R16G16B16A16_UNorm;
            case PixelFormat.R16G16B16A16SNorm: return Format.R16G16B16A16_SNorm;
            case PixelFormat.R16G16B16A16UInt: return Format.R16G16B16A16_UInt;
            case PixelFormat.R16G16B16A16SInt: return Format.R16G16B16A16_SInt;
            case PixelFormat.R16G16B16A16Float: return Format.R16G16B16A16_Float;
            case PixelFormat.R32G32B32A32UInt: return Format.R32G32B32A32_UInt;
            case PixelFormat.R32G32B32A32SInt: return Format.R32G32B32A32_SInt;
            case PixelFormat.R32G32B32A32Float: return Format.R32G32B32A32_Float;
            case PixelFormat.Bc1RgbUNorm: case PixelFormat.Bc1RgbaUNorm: return Format.BC1_UNorm;
            case PixelFormat.Bc1RgbUNormSRgb: case PixelFormat.Bc1RgbaUNormSRgb: return Format.BC1_UNorm_SRgb;
            case PixelFormat.Bc2UNorm: return Format.BC2_UNorm;
            case PixelFormat.Bc2UNormSRgb: return Format.BC2_UNorm_SRgb;
            case PixelFormat.Bc3UNorm: return Format.BC3_UNorm;
            case PixelFormat.Bc3UNormSRgb: return Format.BC3_UNorm_SRgb;
            case PixelFormat.Bc4UNorm: return Format.BC4_UNorm;
            case PixelFormat.Bc4SNorm: return Format.BC4_SNorm;
            case PixelFormat.Bc5UNorm: return Format.BC5_UNorm;
            case PixelFormat.Bc5SNorm: return Format.BC5_SNorm;
            case PixelFormat.Bc7UNorm: return Format.BC7_UNorm;
            case PixelFormat.Bc7UNormSRgb: return Format.BC7_UNorm_SRgb;
            case PixelFormat.D24UNormS8UInt: return Format.R24G8_Typeless;
            case PixelFormat.D32FloatS8UInt: return Format.R32G8X24_Typeless;
            case PixelFormat.R10G10B10A2UNorm: return Format.R10G10B10A2_UNorm;
            case PixelFormat.R10G10B10A2UInt: return Format.R10G10B10A2_UInt;
            case PixelFormat.R11G11B10Float: return Format.R11G11B10_Float;
            default: throw Illegal.Value<PixelFormat>();
        }
    }

    /// <summary>
    /// Executes the to depth format logic for this backend.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static Format ToDepthFormat(PixelFormat format) {
        switch (format) {
            case PixelFormat.R16UNorm: return Format.D16_UNorm;
            case PixelFormat.R32Float: return Format.D32_Float;
            case PixelFormat.D24UNormS8UInt: return Format.D24_UNorm_S8_UInt;
            case PixelFormat.D32FloatS8UInt: return Format.D32_Float_S8X24_UInt;
            default: throw new VeldridException("Invalid depth format: " + format);
        }
    }

    /// <summary>
    /// Gets the view format value.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static Format GetViewFormat(Format format) {
        switch (format) {
            case Format.R16_Typeless: return Format.R16_UNorm;
            case Format.R32_Typeless: return Format.R32_Float;
            case Format.R32G8X24_Typeless: return Format.R32_Float_X8X24_Typeless;
            case Format.R24G8_Typeless: return Format.R24_UNorm_X8_Typeless;
            default: return format;
        }
    }

    /// <summary>
    /// Executes the to resource flags logic for this backend.
    /// </summary>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static ResourceFlags ToResourceFlags(TextureUsage usage) {
        ResourceFlags flags = ResourceFlags.None;
        if ((usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget) {
            flags |= ResourceFlags.AllowRenderTarget;
        }

        if ((usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil) {
            flags |= ResourceFlags.AllowDepthStencil;
        }

        if ((usage & TextureUsage.Storage) == TextureUsage.Storage) {
            flags |= ResourceFlags.AllowUnorderedAccess;
        }

        return flags;
    }

    /// <summary>
    /// Executes the to dxgi format logic for this backend.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static Format ToDxgiFormat(IndexFormat format) {
        switch (format) {
            case IndexFormat.UInt16: return Format.R16_UInt;
            case IndexFormat.UInt32: return Format.R32_UInt;
            default: throw Illegal.Value<IndexFormat>();
        }
    }

    /// <summary>
    /// Executes the to dxgi format logic for this backend.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static Format ToDxgiFormat(VertexElementFormat format) {
        switch (format) {
            case VertexElementFormat.Float1: return Format.R32_Float;
            case VertexElementFormat.Float2: return Format.R32G32_Float;
            case VertexElementFormat.Float3: return Format.R32G32B32_Float;
            case VertexElementFormat.Float4: return Format.R32G32B32A32_Float;
            case VertexElementFormat.Byte2Norm: return Format.R8G8_UNorm;
            case VertexElementFormat.Byte2: return Format.R8G8_UInt;
            case VertexElementFormat.Byte4Norm: return Format.R8G8B8A8_UNorm;
            case VertexElementFormat.Byte4: return Format.R8G8B8A8_UInt;
            case VertexElementFormat.SByte2Norm: return Format.R8G8_SNorm;
            case VertexElementFormat.SByte2: return Format.R8G8_SInt;
            case VertexElementFormat.SByte4Norm: return Format.R8G8B8A8_SNorm;
            case VertexElementFormat.SByte4: return Format.R8G8B8A8_SInt;
            case VertexElementFormat.UShort2Norm: return Format.R16G16_UNorm;
            case VertexElementFormat.UShort2: return Format.R16G16_UInt;
            case VertexElementFormat.UShort4Norm: return Format.R16G16B16A16_UNorm;
            case VertexElementFormat.UShort4: return Format.R16G16B16A16_UInt;
            case VertexElementFormat.Short2Norm: return Format.R16G16_SNorm;
            case VertexElementFormat.Short2: return Format.R16G16_SInt;
            case VertexElementFormat.Short4Norm: return Format.R16G16B16A16_SNorm;
            case VertexElementFormat.Short4: return Format.R16G16B16A16_SInt;
            case VertexElementFormat.UInt1: return Format.R32_UInt;
            case VertexElementFormat.UInt2: return Format.R32G32_UInt;
            case VertexElementFormat.UInt3: return Format.R32G32B32_UInt;
            case VertexElementFormat.UInt4: return Format.R32G32B32A32_UInt;
            case VertexElementFormat.Int1: return Format.R32_SInt;
            case VertexElementFormat.Int2: return Format.R32G32_SInt;
            case VertexElementFormat.Int3: return Format.R32G32B32_SInt;
            case VertexElementFormat.Int4: return Format.R32G32B32A32_SInt;
            case VertexElementFormat.Half1: return Format.R16_Float;
            case VertexElementFormat.Half2: return Format.R16G16_Float;
            case VertexElementFormat.Half4: return Format.R16G16B16A16_Float;
            default: throw Illegal.Value<VertexElementFormat>();
        }
    }

    /// <summary>
    /// Executes the to d3 dprimitive topology logic for this backend.
    /// </summary>
    /// <param name="topology">The topology value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static Vortice.Direct3D.PrimitiveTopology ToD3DPrimitiveTopology(PrimitiveTopology topology) {
        switch (topology) {
            case PrimitiveTopology.TriangleList: return Vortice.Direct3D.PrimitiveTopology.TriangleList;
            case PrimitiveTopology.TriangleStrip: return Vortice.Direct3D.PrimitiveTopology.TriangleStrip;
            case PrimitiveTopology.LineList: return Vortice.Direct3D.PrimitiveTopology.LineList;
            case PrimitiveTopology.LineStrip: return Vortice.Direct3D.PrimitiveTopology.LineStrip;
            case PrimitiveTopology.PointList: return Vortice.Direct3D.PrimitiveTopology.PointList;
            default: throw Illegal.Value<PrimitiveTopology>();
        }
    }

    /// <summary>
    /// Executes the to primitive topology type logic for this backend.
    /// </summary>
    /// <param name="topology">The topology value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static PrimitiveTopologyType ToPrimitiveTopologyType(PrimitiveTopology topology) {
        switch (topology) {
            case PrimitiveTopology.TriangleList: case PrimitiveTopology.TriangleStrip: return PrimitiveTopologyType.Triangle;
            case PrimitiveTopology.LineList: case PrimitiveTopology.LineStrip: return PrimitiveTopologyType.Line;
            case PrimitiveTopology.PointList: return PrimitiveTopologyType.Point;
            default: throw Illegal.Value<PrimitiveTopology>();
        }
    }

    /// <summary>
    /// Executes the to fill mode logic for this backend.
    /// </summary>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static FillMode ToFillMode(PolygonFillMode mode) {
        switch (mode) {
            case PolygonFillMode.Solid: return FillMode.Solid;
            case PolygonFillMode.Wireframe: return FillMode.Wireframe;
            default: throw Illegal.Value<PolygonFillMode>();
        }
    }

    /// <summary>
    /// Executes the to cull mode logic for this backend.
    /// </summary>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static CullMode ToCullMode(FaceCullMode mode) {
        switch (mode) {
            case FaceCullMode.None: return CullMode.None;
            case FaceCullMode.Front: return CullMode.Front;
            case FaceCullMode.Back: return CullMode.Back;
            default: throw Illegal.Value<FaceCullMode>();
        }
    }

    /// <summary>
    /// Executes the to comparison logic for this backend.
    /// </summary>
    /// <param name="kind">The kind value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static ComparisonFunction ToComparison(ComparisonKind kind) {
        switch (kind) {
            case ComparisonKind.Never: return ComparisonFunction.Never;
            case ComparisonKind.Less: return ComparisonFunction.Less;
            case ComparisonKind.Equal: return ComparisonFunction.Equal;
            case ComparisonKind.LessEqual: return ComparisonFunction.LessEqual;
            case ComparisonKind.Greater: return ComparisonFunction.Greater;
            case ComparisonKind.NotEqual: return ComparisonFunction.NotEqual;
            case ComparisonKind.GreaterEqual: return ComparisonFunction.GreaterEqual;
            case ComparisonKind.Always: return ComparisonFunction.Always;
            default: throw Illegal.Value<ComparisonKind>();
        }
    }

    /// <summary>
    /// Executes the to stencil op logic for this backend.
    /// </summary>
    /// <param name="op">The op value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static Vortice.Direct3D12.StencilOperation ToStencilOp(StencilOperation op) {
        switch (op) {
            case StencilOperation.Keep: return Vortice.Direct3D12.StencilOperation.Keep;
            case StencilOperation.Zero: return Vortice.Direct3D12.StencilOperation.Zero;
            case StencilOperation.Replace: return Vortice.Direct3D12.StencilOperation.Replace;
            case StencilOperation.IncrementAndClamp: return Vortice.Direct3D12.StencilOperation.IncrementSaturate;
            case StencilOperation.DecrementAndClamp: return Vortice.Direct3D12.StencilOperation.DecrementSaturate;
            case StencilOperation.Invert: return Vortice.Direct3D12.StencilOperation.Invert;
            case StencilOperation.IncrementAndWrap: return Vortice.Direct3D12.StencilOperation.Increment;
            case StencilOperation.DecrementAndWrap: return Vortice.Direct3D12.StencilOperation.Decrement;
            default: throw Illegal.Value<StencilOperation>();
        }
    }

    /// <summary>
    /// Executes the to blend logic for this backend.
    /// </summary>
    /// <param name="factor">The factor value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static Blend ToBlend(BlendFactor factor) {
        switch (factor) {
            case BlendFactor.Zero: return Blend.Zero;
            case BlendFactor.One: return Blend.One;
            case BlendFactor.SourceAlpha: return Blend.SourceAlpha;
            case BlendFactor.InverseSourceAlpha: return Blend.InverseSourceAlpha;
            case BlendFactor.DestinationAlpha: return Blend.DestinationAlpha;
            case BlendFactor.InverseDestinationAlpha: return Blend.InverseDestinationAlpha;
            case BlendFactor.SourceColor: return Blend.SourceColor;
            case BlendFactor.InverseSourceColor: return Blend.InverseSourceColor;
            case BlendFactor.DestinationColor: return Blend.DestinationColor;
            case BlendFactor.InverseDestinationColor: return Blend.InverseDestinationColor;
            case BlendFactor.BlendFactor: return Blend.BlendFactor;
            case BlendFactor.InverseBlendFactor: return Blend.InverseBlendFactor;
            default: throw Illegal.Value<BlendFactor>();
        }
    }

    /// <summary>
    /// Executes the to blend op logic for this backend.
    /// </summary>
    /// <param name="function">The function value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static BlendOperation ToBlendOp(BlendFunction function) {
        switch (function) {
            case BlendFunction.Add: return BlendOperation.Add;
            case BlendFunction.Subtract: return BlendOperation.Subtract;
            case BlendFunction.ReverseSubtract: return BlendOperation.RevSubtract;
            case BlendFunction.Minimum: return BlendOperation.Min;
            case BlendFunction.Maximum: return BlendOperation.Max;
            default: throw Illegal.Value<BlendFunction>();
        }
    }

    /// <summary>
    /// Executes the to color write mask logic for this backend.
    /// </summary>
    /// <param name="mask">The mask value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static ColorWriteEnable ToColorWriteMask(ColorWriteMask mask) {
        ColorWriteEnable enable = ColorWriteEnable.None;
        if ((mask & ColorWriteMask.Red) == ColorWriteMask.Red) {
            enable |= ColorWriteEnable.Red;
        }

        if ((mask & ColorWriteMask.Green) == ColorWriteMask.Green) {
            enable |= ColorWriteEnable.Green;
        }

        if ((mask & ColorWriteMask.Blue) == ColorWriteMask.Blue) {
            enable |= ColorWriteEnable.Blue;
        }

        if ((mask & ColorWriteMask.Alpha) == ColorWriteMask.Alpha) {
            enable |= ColorWriteEnable.Alpha;
        }

        return enable;
    }

    /// <summary>
    /// Executes the to texture address mode logic for this backend.
    /// </summary>
    /// <param name="mode">The mode value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static TextureAddressMode ToTextureAddressMode(SamplerAddressMode mode) {
        switch (mode) {
            case SamplerAddressMode.Wrap: return TextureAddressMode.Wrap;
            case SamplerAddressMode.Mirror: return TextureAddressMode.Mirror;
            case SamplerAddressMode.Clamp: return TextureAddressMode.Clamp;
            case SamplerAddressMode.Border: return TextureAddressMode.Border;
            default: throw Illegal.Value<SamplerAddressMode>();
        }
    }

    /// <summary>
    /// Executes the to filter logic for this backend.
    /// </summary>
    /// <param name="filter">The filter value used by this operation.</param>
    /// <param name="comparison">The comparison value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static Filter ToFilter(SamplerFilter filter, bool comparison) {
        switch (filter) {
            case SamplerFilter.MinPointMagPointMipPoint: return comparison ? Filter.ComparisonMinMagMipPoint : Filter.MinMagMipPoint;
            case SamplerFilter.MinPointMagPointMipLinear: return comparison ? Filter.ComparisonMinMagPointMipLinear : Filter.MinMagPointMipLinear;
            case SamplerFilter.MinPointMagLinearMipPoint: return comparison ? Filter.ComparisonMinPointMagLinearMipPoint : Filter.MinPointMagLinearMipPoint;
            case SamplerFilter.MinPointMagLinearMipLinear: return comparison ? Filter.ComparisonMinPointMagMipLinear : Filter.MinPointMagMipLinear;
            case SamplerFilter.MinLinearMagPointMipPoint: return comparison ? Filter.ComparisonMinLinearMagMipPoint : Filter.MinLinearMagMipPoint;
            case SamplerFilter.MinLinearMagPointMipLinear: return comparison ? Filter.ComparisonMinLinearMagPointMipLinear : Filter.MinLinearMagPointMipLinear;
            case SamplerFilter.MinLinearMagLinearMipPoint: return comparison ? Filter.ComparisonMinMagLinearMipPoint : Filter.MinMagLinearMipPoint;
            case SamplerFilter.MinLinearMagLinearMipLinear: return comparison ? Filter.ComparisonMinMagMipLinear : Filter.MinMagMipLinear;
            case SamplerFilter.Anisotropic: return comparison ? Filter.ComparisonAnisotropic : Filter.Anisotropic;
            default: throw Illegal.Value<SamplerFilter>();
        }
    }

    /// <summary>
    /// Executes the to border color logic for this backend.
    /// </summary>
    /// <param name="color">The color value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static Color4 ToBorderColor(SamplerBorderColor color) {
        switch (color) {
            case SamplerBorderColor.TransparentBlack: return new Color4(0f, 0f, 0f, 0f);
            case SamplerBorderColor.OpaqueBlack: return new Color4(0f, 0f, 0f);
            case SamplerBorderColor.OpaqueWhite: return new Color4(1f, 1f, 1f);
            default: throw Illegal.Value<SamplerBorderColor>();
        }
    }
}