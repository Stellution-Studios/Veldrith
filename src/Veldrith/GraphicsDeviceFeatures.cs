namespace Veldrith;

/// <summary>
/// Defines the behavior and responsibilities of the GraphicsDeviceFeatures class.
/// </summary>
public class GraphicsDeviceFeatures {

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsDeviceFeatures" /> type.
    /// </summary>
    /// <param name="computeShader">Specifies the value of <paramref name="computeShader" />.</param>
    /// <param name="geometryShader">Specifies the value of <paramref name="geometryShader" />.</param>
    /// <param name="tessellationShaders">Specifies the value of <paramref name="tessellationShaders" />.</param>
    /// <param name="multipleViewports">Specifies the value of <paramref name="multipleViewports" />.</param>
    /// <param name="samplerLodBias">Specifies the value of <paramref name="samplerLodBias" />.</param>
    /// <param name="drawBaseVertex">Specifies the value of <paramref name="drawBaseVertex" />.</param>
    /// <param name="drawBaseInstance">Specifies the value of <paramref name="drawBaseInstance" />.</param>
    /// <param name="drawIndirect">Specifies the value of <paramref name="drawIndirect" />.</param>
    /// <param name="drawIndirectBaseInstance">Specifies the value of <paramref name="drawIndirectBaseInstance" />.</param>
    /// <param name="fillModeWireframe">Specifies the value of <paramref name="fillModeWireframe" />.</param>
    /// <param name="samplerAnisotropy">Specifies the value of <paramref name="samplerAnisotropy" />.</param>
    /// <param name="depthClipDisable">Specifies the value of <paramref name="depthClipDisable" />.</param>
    /// <param name="texture1D">Specifies the value of <paramref name="texture1D" />.</param>
    /// <param name="independentBlend">Specifies the value of <paramref name="independentBlend" />.</param>
    /// <param name="structuredBuffer">Specifies the value of <paramref name="structuredBuffer" />.</param>
    /// <param name="subsetTextureView">Specifies the value of <paramref name="subsetTextureView" />.</param>
    /// <param name="commandListDebugMarkers">Specifies the value of <paramref name="commandListDebugMarkers" />.</param>
    /// <param name="bufferRangeBinding">Specifies the value of <paramref name="bufferRangeBinding" />.</param>
    /// <param name="shaderFloat64">Specifies the value of <paramref name="shaderFloat64" />.</param>
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
    /// If this is not supported, then only the first Viewport index will be used for all render outputs.
    /// </summary>
    public bool MultipleViewports { get; }

    /// <summary>
    /// Indicates whether <see cref="SamplerDescription.LodBias" /> can be non-zero.
    /// If false, it is an error to attempt to use a non-zero bias value.
    /// </summary>
    public bool SamplerLodBias { get; }

    /// <summary>
    /// Indicates whether a non-zero "vertexStart" value can be used in
    /// <see cref="CommandList.Draw(uint, uint, uint, uint)" /> and
    /// <see cref="CommandList.DrawIndexed(uint, uint, uint, int, uint)" />.
    /// </summary>
    public bool DrawBaseVertex { get; }

    /// <summary>
    /// Indicates whether a non-zero "instanceStart" value can be used in
    /// <see cref="CommandList.Draw(uint, uint, uint, uint)" /> and
    /// <see cref="CommandList.DrawIndexed(uint, uint, uint, int, uint)" />.
    /// </summary>
    public bool DrawBaseInstance { get; }

    /// <summary>
    /// Indicates whether indirect draw commands can be issued.
    /// </summary>
    public bool DrawIndirect { get; }

    /// <summary>
    /// Indicates whether indirect draw structures stored in an Indirect DeviceBuffer can contain
    /// a non-zero FirstInstance value.
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
    /// <see cref="BlendAttachmentDescription" /> values for each attachment. If false, all attachments must have the same
    /// blend state.
    /// </summary>
    public bool IndependentBlend { get; }

    /// <summary>
    /// Indicates whether <see cref="BufferUsage.StructuredBufferReadOnly" /> and
    /// <see cref="BufferUsage.StructuredBufferReadWrite" /> can be used. If false, structured buffers cannot be created.
    /// </summary>
    public bool StructuredBuffer { get; }

    /// <summary>
    /// Indicates whether a <see cref="TextureView" /> can be created which does not view the full set of mip levels and
    /// array
    /// layers contained in its target Texture, or uses a different <see cref="PixelFormat" /> from the underlying Texture.
    /// </summary>
    public bool SubsetTextureView { get; }

    /// <summary>
    /// Indicates whether <see cref="CommandList" /> instances created with this device support the
    /// <see cref="CommandList.PushDebugGroup(string)" />, <see cref="CommandList.PopDebugGroup" />, and
    /// <see cref="CommandList.InsertDebugMarker(string)" /> methods. If not, these methods will have no effect.
    /// </summary>
    public bool CommandListDebugMarkers { get; }

    /// <summary>
    /// Indicates whether uniform and structured buffers can be bound with an offset and a size. If false, buffer resources
    /// must be bound with their full range.
    /// </summary>
    public bool BufferRangeBinding { get; }

    /// <summary>
    /// Indicates whether 64-bit floating point integers can be used in shaders.
    /// </summary>
    public bool ShaderFloat64 { get; }
}