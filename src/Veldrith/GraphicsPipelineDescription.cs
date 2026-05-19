using System;

namespace Veldrith;

/// <summary>
/// Describes a graphics <see cref="Pipeline" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct GraphicsPipelineDescription : IEquatable<GraphicsPipelineDescription> {

    /// <summary>
    /// A description of the blend state, which controls how color values are blended into each color target.
    /// </summary>
    public BlendStateDescription BlendState;

    /// <summary>
    /// A description of the depth stencil state, which controls depth tests, writing, and comparisons.
    /// </summary>
    public DepthStencilStateDescription DepthStencilState;

    /// <summary>
    /// A description of the rasterizer state, which controls culling, clipping, scissor, and polygon-fill behavior.
    /// </summary>
    public RasterizerStateDescription RasterizerState;

    /// <summary>
    /// The <see cref="PrimitiveTopology" /> to use, which controls how a series of input vertices is interpreted by the
    /// <see cref="Pipeline" />.
    /// </summary>
    public PrimitiveTopology PrimitiveTopology;

    /// <summary>
    /// A description of the shader set to be used.
    /// </summary>
    public ShaderSetDescription ShaderSet;

    /// <summary>
    /// An array of <see cref="ResourceLayout" />, which controls the layout of shader resources in the
    /// <see cref="Pipeline" />.
    /// </summary>
    public ResourceLayout[] ResourceLayouts;

    /// <summary>
    /// A description of the output attachments used by the <see cref="Pipeline" />.
    /// </summary>
    public OutputDescription Outputs;

    /// <summary>
    /// Specifies which model the rendering backend should use for binding resources.
    /// If <code>null</code>, the pipeline will use the value specified in <see cref="GraphicsDeviceOptions" />.
    /// </summary>
    public ResourceBindingModel? ResourceBindingModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsPipelineDescription" /> type.
    /// </summary>
    /// <param name="blendState">Specifies the value of <paramref name="blendState" />.</param>
    /// <param name="depthStencilStateDescription">Specifies the value of <paramref name="depthStencilStateDescription" />.</param>
    /// <param name="rasterizerState">Specifies the value of <paramref name="rasterizerState" />.</param>
    /// <param name="primitiveTopology">Specifies the value of <paramref name="primitiveTopology" />.</param>
    /// <param name="shaderSet">Specifies the value of <paramref name="shaderSet" />.</param>
    /// <param name="resourceLayouts">Specifies the value of <paramref name="resourceLayouts" />.</param>
    /// <param name="outputs">Specifies the value of <paramref name="outputs" />.</param>
    public GraphicsPipelineDescription(BlendStateDescription blendState, DepthStencilStateDescription depthStencilStateDescription, RasterizerStateDescription rasterizerState, PrimitiveTopology primitiveTopology, ShaderSetDescription shaderSet, ResourceLayout[] resourceLayouts, OutputDescription outputs) {
        this.BlendState = blendState;
        this.DepthStencilState = depthStencilStateDescription;
        this.RasterizerState = rasterizerState;
        this.PrimitiveTopology = primitiveTopology;
        this.ShaderSet = shaderSet;
        this.ResourceLayouts = resourceLayouts;
        this.Outputs = outputs;
        this.ResourceBindingModel = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsPipelineDescription" /> type.
    /// </summary>
    /// <param name="blendState">Specifies the value of <paramref name="blendState" />.</param>
    /// <param name="depthStencilStateDescription">Specifies the value of <paramref name="depthStencilStateDescription" />.</param>
    /// <param name="rasterizerState">Specifies the value of <paramref name="rasterizerState" />.</param>
    /// <param name="primitiveTopology">Specifies the value of <paramref name="primitiveTopology" />.</param>
    /// <param name="shaderSet">Specifies the value of <paramref name="shaderSet" />.</param>
    /// <param name="resourceLayout">Specifies the value of <paramref name="resourceLayout" />.</param>
    /// <param name="outputs">Specifies the value of <paramref name="outputs" />.</param>
    public GraphicsPipelineDescription(BlendStateDescription blendState, DepthStencilStateDescription depthStencilStateDescription, RasterizerStateDescription rasterizerState, PrimitiveTopology primitiveTopology, ShaderSetDescription shaderSet, ResourceLayout resourceLayout, OutputDescription outputs) {
        this.BlendState = blendState;
        this.DepthStencilState = depthStencilStateDescription;
        this.RasterizerState = rasterizerState;
        this.PrimitiveTopology = primitiveTopology;
        this.ShaderSet = shaderSet;
        this.ResourceLayouts = new[] { resourceLayout };
        this.Outputs = outputs;
        this.ResourceBindingModel = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsPipelineDescription" /> type.
    /// </summary>
    /// <param name="blendState">Specifies the value of <paramref name="blendState" />.</param>
    /// <param name="depthStencilStateDescription">Specifies the value of <paramref name="depthStencilStateDescription" />.</param>
    /// <param name="rasterizerState">Specifies the value of <paramref name="rasterizerState" />.</param>
    /// <param name="primitiveTopology">Specifies the value of <paramref name="primitiveTopology" />.</param>
    /// <param name="shaderSet">Specifies the value of <paramref name="shaderSet" />.</param>
    /// <param name="resourceLayouts">Specifies the value of <paramref name="resourceLayouts" />.</param>
    /// <param name="outputs">Specifies the value of <paramref name="outputs" />.</param>
    /// <param name="resourceBindingModel">Specifies the value of <paramref name="resourceBindingModel" />.</param>
    public GraphicsPipelineDescription(BlendStateDescription blendState, DepthStencilStateDescription depthStencilStateDescription, RasterizerStateDescription rasterizerState, PrimitiveTopology primitiveTopology, ShaderSetDescription shaderSet, ResourceLayout[] resourceLayouts, OutputDescription outputs, ResourceBindingModel resourceBindingModel) {
        this.BlendState = blendState;
        this.DepthStencilState = depthStencilStateDescription;
        this.RasterizerState = rasterizerState;
        this.PrimitiveTopology = primitiveTopology;
        this.ShaderSet = shaderSet;
        this.ResourceLayouts = resourceLayouts;
        this.Outputs = outputs;
        this.ResourceBindingModel = resourceBindingModel;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(GraphicsPipelineDescription other) {
        return this.BlendState.Equals(other.BlendState)
               && this.DepthStencilState.Equals(other.DepthStencilState)
               && this.RasterizerState.Equals(other.RasterizerState)
               && this.PrimitiveTopology == other.PrimitiveTopology
               && this.ShaderSet.Equals(other.ShaderSet)
               && Util.ArrayEquals(this.ResourceLayouts, other.ResourceLayouts)
               && (this.ResourceBindingModel.HasValue && other.ResourceBindingModel.HasValue
                   ? this.ResourceBindingModel.Value == other.ResourceBindingModel.Value
                   : this.ResourceBindingModel.HasValue == other.ResourceBindingModel.HasValue)
               && this.Outputs.Equals(other.Outputs);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.BlendState.GetHashCode(), this.DepthStencilState.GetHashCode(), this.RasterizerState.GetHashCode(), (int)this.PrimitiveTopology, this.ShaderSet.GetHashCode(), HashHelper.Array(this.ResourceLayouts), this.ResourceBindingModel.GetHashCode(), this.Outputs.GetHashCode());
    }
}