using System;

namespace Veldrith;

/// <summary>
/// A <see cref="Pipeline" /> component describing a full set of shader stages and vertex layouts.
/// </summary>
public struct ShaderSetDescription : IEquatable<ShaderSetDescription> {

    /// <summary>
    /// An array of <see cref="VertexLayoutDescription" /> describing the set of vertex layouts understood by the
    /// </summary>
    public VertexLayoutDescription[] VertexLayouts;

    /// <summary>
    /// An array of <see cref="Shader" /> objects, one for each shader stage which is to be active in the
    /// </summary>
    public Shader[] Shaders;

    /// <summary>
    /// An array of <see cref="SpecializationConstant" /> used to override specialization constants in the created
    /// </summary>
    public SpecializationConstant[] Specializations;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShaderSetDescription" /> type.
    /// </summary>
    /// <param name="vertexLayouts">The resource layout used by this operation.</param>
    /// <param name="shaders">The shaders value used by this operation.</param>
    public ShaderSetDescription(VertexLayoutDescription[] vertexLayouts, Shader[] shaders) {
        this.VertexLayouts = vertexLayouts;
        this.Shaders = shaders;
        this.Specializations = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShaderSetDescription" /> type.
    /// </summary>
    /// <param name="vertexLayouts">The resource layout used by this operation.</param>
    /// <param name="shaders">The shaders value used by this operation.</param>
    /// <param name="specializations">The specializations value used by this operation.</param>
    public ShaderSetDescription(VertexLayoutDescription[] vertexLayouts, Shader[] shaders, SpecializationConstant[] specializations) {
        this.VertexLayouts = vertexLayouts;
        this.Shaders = shaders;
        this.Specializations = specializations;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(ShaderSetDescription other) {
        return Util.ArrayEqualsEquatable(this.VertexLayouts, other.VertexLayouts)
               && Util.ArrayEquals(this.Shaders, other.Shaders)
               && Util.ArrayEqualsEquatable(this.Specializations, other.Specializations);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(HashHelper.Array(this.VertexLayouts), HashHelper.Array(this.Shaders), HashHelper.Array(this.Specializations));
    }
}