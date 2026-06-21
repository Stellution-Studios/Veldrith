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
    /// </summary>
    public PrimitiveTopology PrimitiveTopology;

    /// <summary>
    /// A description of the shader set to be used.
    /// </summary>
    public ShaderSetDescription ShaderSet;

    /// <summary>
    /// An array of <see cref="ResourceLayout" />, which controls the layout of shader resources in the
    /// </summary>
    public ResourceLayout[] ResourceLayouts;

    /// <summary>
    /// A description of the output attachments used by the <see cref="Pipeline" />.
    /// </summary>
    public OutputDescription Outputs;

    /// <summary>
    /// Specifies which model the rendering backend should use for binding resources.
    /// </summary>
    public ResourceBindingModel? ResourceBindingModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsPipelineDescription" /> type.
    /// </summary>
    /// <param name="blendState">The blend state value used by this operation.</param>
    /// <param name="depthStencilStateDescription">The depth stencil state description value used by this operation.</param>
    /// <param name="rasterizerState">The rasterizer state value used by this operation.</param>
    /// <param name="primitiveTopology">The primitive topology value used by this operation.</param>
    /// <param name="shaderSet">The shader set value used by this operation.</param>
    /// <param name="resourceLayouts">The resource layout used by this operation.</param>
    /// <param name="outputs">The outputs value used by this operation.</param>
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
    /// <param name="blendState">The blend state value used by this operation.</param>
    /// <param name="depthStencilStateDescription">The depth stencil state description value used by this operation.</param>
    /// <param name="rasterizerState">The rasterizer state value used by this operation.</param>
    /// <param name="primitiveTopology">The primitive topology value used by this operation.</param>
    /// <param name="shaderSet">The shader set value used by this operation.</param>
    /// <param name="resourceLayout">The resource layout value used by this operation.</param>
    /// <param name="outputs">The outputs value used by this operation.</param>
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
    /// <param name="blendState">The blend state value used by this operation.</param>
    /// <param name="depthStencilStateDescription">The depth stencil state description value used by this operation.</param>
    /// <param name="rasterizerState">The rasterizer state value used by this operation.</param>
    /// <param name="primitiveTopology">The primitive topology value used by this operation.</param>
    /// <param name="shaderSet">The shader set value used by this operation.</param>
    /// <param name="resourceLayouts">The resource layout used by this operation.</param>
    /// <param name="outputs">The outputs value used by this operation.</param>
    /// <param name="resourceBindingModel">The resource binding model value used by this operation.</param>
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
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.BlendState.GetHashCode(), this.DepthStencilState.GetHashCode(), this.RasterizerState.GetHashCode(), (int)this.PrimitiveTopology, this.ShaderSet.GetHashCode(), HashHelper.Array(this.ResourceLayouts), this.ResourceBindingModel.GetHashCode(), this.Outputs.GetHashCode());
    }
}