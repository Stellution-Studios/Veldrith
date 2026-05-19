using System;

namespace Veldrith;

/// <summary>
/// A <see cref="Pipeline" /> component describing a full set of shader stages and vertex layouts.
/// </summary>
public struct ShaderSetDescription : IEquatable<ShaderSetDescription> {

    /// <summary>
    /// An array of <see cref="VertexLayoutDescription" /> describing the set of vertex layouts understood by the
    /// <see cref="Pipeline" />. Each element in this array describes the input layout of a single
    /// <see cref="DeviceBuffer" />
    /// to be bound when drawing.
    /// </summary>
    public VertexLayoutDescription[] VertexLayouts;

    /// <summary>
    /// An array of <see cref="Shader" /> objects, one for each shader stage which is to be active in the
    /// <see cref="Pipeline" />. At a minimum, every graphics Pipeline must include a Vertex and Fragment
    /// shader. All other stages are optional, but if either Tessellation stage is present, then the other must also be.
    /// </summary>
    public Shader[] Shaders;

    /// <summary>
    /// An array of <see cref="SpecializationConstant" /> used to override specialization constants in the created
    /// <see cref="Pipeline" />. Each element in this array describes a single ID-value pair, which will be matched with
    /// the
    /// constants specified in each <see cref="Shader" />.
    /// </summary>
    public SpecializationConstant[] Specializations;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShaderSetDescription" /> type.
    /// </summary>
    /// <param name="vertexLayouts">The value of vertexLayouts.</param>
    /// <param name="shaders">The value of shaders.</param>
    public ShaderSetDescription(VertexLayoutDescription[] vertexLayouts, Shader[] shaders) {
        this.VertexLayouts = vertexLayouts;
        this.Shaders = shaders;
        this.Specializations = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShaderSetDescription" /> type.
    /// </summary>
    /// <param name="vertexLayouts">The value of vertexLayouts.</param>
    /// <param name="shaders">The value of shaders.</param>
    /// <param name="specializations">The value of specializations.</param>
    public ShaderSetDescription(VertexLayoutDescription[] vertexLayouts, Shader[] shaders, SpecializationConstant[] specializations) {
        this.VertexLayouts = vertexLayouts;
        this.Shaders = shaders;
        this.Specializations = specializations;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(ShaderSetDescription other) {
        return Util.ArrayEqualsEquatable(this.VertexLayouts, other.VertexLayouts)
               && Util.ArrayEquals(this.Shaders, other.Shaders)
               && Util.ArrayEqualsEquatable(this.Specializations, other.Specializations);
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(HashHelper.Array(this.VertexLayouts), HashHelper.Array(this.Shaders), HashHelper.Array(this.Specializations));
    }
}