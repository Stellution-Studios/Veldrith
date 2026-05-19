namespace Veldrith;

/// <summary>
/// Represents the GraphicsDeviceFeatures type used by the graphics runtime.
/// </summary>
public class GraphicsDeviceFeatures {

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsDeviceFeatures" /> type.
    /// </summary>
    /// <param name="computeShader">The compute shader value used by this operation.</param>
    /// <param name="geometryShader">The geometry shader value used by this operation.</param>
    /// <param name="tessellationShaders">The tessellation shaders value used by this operation.</param>
    /// <param name="multipleViewports">The multiple viewports value used by this operation.</param>
    /// <param name="samplerLodBias">The sampler lod bias value used by this operation.</param>
    /// <param name="drawBaseVertex">The draw base vertex value used by this operation.</param>
    /// <param name="drawBaseInstance">The draw base instance value used by this operation.</param>
    /// <param name="drawIndirect">The draw indirect value used by this operation.</param>
    /// <param name="drawIndirectBaseInstance">The draw indirect base instance value used by this operation.</param>
    /// <param name="fillModeWireframe">The fill mode wireframe value used by this operation.</param>
    /// <param name="samplerAnisotropy">The sampler anisotropy value used by this operation.</param>
    /// <param name="depthClipDisable">The depth clip disable value used by this operation.</param>
    /// <param name="texture1D">The texture1 d value used by this operation.</param>
    /// <param name="independentBlend">The independent blend value used by this operation.</param>
    /// <param name="structuredBuffer">The structured buffer value used by this operation.</param>
    /// <param name="subsetTextureView">The subset texture view value used by this operation.</param>
    /// <param name="commandListDebugMarkers">The command list debug markers value used by this operation.</param>
    /// <param name="bufferRangeBinding">The buffer range binding value used by this operation.</param>
    /// <param name="shaderFloat64">The shader float64 value used by this operation.</param>
    internal GraphicsDeviceFeatures(bool computeShader, bool geometryShader, bool tessellationShaders, bool multipleViewports, bool samplerLodBias, bool drawBaseVertex, bool drawBaseInstance, bool drawIndirect, bool drawIndirectBaseInstance, bool fillModeWireframe, bool samplerAnisotropy, bool depthClipDisable, bool texture1D, bool independentBlend, bool structuredBuffer, bool subsetTextureView, bool commandListDebugMarkers, bool bufferRangeBinding, bool shaderFloat64) {
        this.ComputeShader = computeShader;
        this.GeometryShader = geometryShader;
        this.TessellationShaders = tessellationShaders;
        this.MultipleViewports = multipleViewports;
        this.SamplerLodBias = samplerLodBias;
        this.DrawBaseVertex = drawBaseVertex;
        this.DrawBaseInstance = drawBaseInstance;
        this.DrawIndirect = drawIndirect;
        this.DrawIndirectBaseInstance = drawIndirectBaseInstance;
        this.FillModeWireframe = fillModeWireframe;
        this.SamplerAnisotropy = samplerAnisotropy;
        this.DepthClipDisable = depthClipDisable;
        this.Texture1D = texture1D;
        this.IndependentBlend = independentBlend;
        this.StructuredBuffer = structuredBuffer;
        this.SubsetTextureView = subsetTextureView;
        this.CommandListDebugMarkers = commandListDebugMarkers;
        this.BufferRangeBinding = bufferRangeBinding;
        this.ShaderFloat64 = shaderFloat64;
    }

    /// <summary>
    /// Indicates whether Compute Shaders can be used.
    /// </summary>
    public bool ComputeShader { get; }

    /// <summary>
    /// Indicates whether Geometry Shaders can be used.
    /// </summary>
    public bool GeometryShader { get; }

    /// <summary>
    /// Indicates whether Tessellation Shaders can be used.
    /// </summary>
    public bool TessellationShaders { get; }

    /// <summary>
    /// Indicates whether multiple independent viewports can be set simultaneously.
    /// </summary>
    public bool MultipleViewports { get; }

    /// <summary>
    /// Indicates whether <see cref="SamplerDescription.LodBias" /> can be non-zero.
    /// </summary>
    public bool SamplerLodBias { get; }

    /// <summary>
    /// Indicates whether a non-zero "vertexStart" value can be used in
    /// </summary>
    public bool DrawBaseVertex { get; }

    /// <summary>
    /// Indicates whether a non-zero "instanceStart" value can be used in
    /// </summary>
    public bool DrawBaseInstance { get; }

    /// <summary>
    /// Indicates whether indirect draw commands can be issued.
    /// </summary>
    public bool DrawIndirect { get; }

    /// <summary>
    /// Indicates whether indirect draw structures stored in an Indirect DeviceBuffer can contain
    /// </summary>
    public bool DrawIndirectBaseInstance { get; }

    /// <summary>
    /// Indicates whether <see cref="PolygonFillMode.Wireframe" /> is supported.
    /// </summary>
    public bool FillModeWireframe { get; }

    /// <summary>
    /// Indicates whether <see cref="SamplerFilter.Anisotropic" /> is supported.
    /// </summary>
    public bool SamplerAnisotropy { get; }

    /// <summary>
    /// Indicates whether <see cref="RasterizerStateDescription.DepthClipEnabled" /> can be set to false.
    /// </summary>
    public bool DepthClipDisable { get; }

    /// <summary>
    /// Indicates whether a <see cref="Texture" /> can be created with <see cref="TextureType.Texture1D" />.
    /// </summary>
    public bool Texture1D { get; }

    /// <summary>
    /// Indicates whether a <see cref="BlendStateDescription" /> can be used which has multiple different
    /// </summary>
    public bool IndependentBlend { get; }

    /// <summary>
    /// Indicates whether <see cref="BufferUsage.StructuredBufferReadOnly" /> and
    /// </summary>
    public bool StructuredBuffer { get; }

    /// <summary>
    /// Indicates whether a <see cref="TextureView" /> can be created which does not view the full set of mip levels and
    /// </summary>
    public bool SubsetTextureView { get; }

    /// <summary>
    /// Indicates whether <see cref="CommandList" /> instances created with this device support the
    /// </summary>
    public bool CommandListDebugMarkers { get; }

    /// <summary>
    /// Indicates whether uniform and structured buffers can be bound with an offset and a size. If false, buffer resources
    /// </summary>
    public bool BufferRangeBinding { get; }

    /// <summary>
    /// Indicates whether 64-bit floating point integers can be used in shaders.
    /// </summary>
    public bool ShaderFloat64 { get; }
}