using System;

namespace Veldrith;

/// <summary>
/// Describes a compute <see cref="Pipeline" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct ComputePipelineDescription : IEquatable<ComputePipelineDescription> {

    /// <summary>
    /// The compute <see cref="Shader" /> to be used in the Pipeline. This must be a Shader with
    /// <see cref="ShaderStages.Compute" />.
    /// </summary>
    public Shader ComputeShader;

    /// <summary>
    /// An array of <see cref="ResourceLayout" />, which controls the layout of shader resoruces in the
    /// <see cref="Pipeline" />.
    /// </summary>
    public ResourceLayout[] ResourceLayouts;

    /// <summary>
    /// The X dimension of the thread group size.
    /// </summary>
    public uint ThreadGroupSizeX;

    /// <summary>
    /// The Y dimension of the thread group size.
    /// </summary>
    public uint ThreadGroupSizeY;

    /// <summary>
    /// The Z dimension of the thread group size.
    /// </summary>
    public uint ThreadGroupSizeZ;

    /// <summary>
    /// An array of <see cref="SpecializationConstant" /> used to override specialization constants in the created
    /// <see cref="Pipeline" />. Each element in this array describes a single ID-value pair, which will be matched with
    /// the
    /// constants specified in the <see cref="Shader" />.
    /// </summary>
    public SpecializationConstant[] Specializations;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputePipelineDescription" /> type.
    /// </summary>
    /// <param name="computeShader">Specifies the value of <paramref name="computeShader" />.</param>
    /// <param name="resourceLayouts">Specifies the value of <paramref name="resourceLayouts" />.</param>
    /// <param name="threadGroupSizeX">Specifies the value of <paramref name="threadGroupSizeX" />.</param>
    /// <param name="threadGroupSizeY">Specifies the value of <paramref name="threadGroupSizeY" />.</param>
    /// <param name="threadGroupSizeZ">Specifies the value of <paramref name="threadGroupSizeZ" />.</param>
    public ComputePipelineDescription(Shader computeShader, ResourceLayout[] resourceLayouts, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ) {
        this.ComputeShader = computeShader;
        this.ResourceLayouts = resourceLayouts;
        this.ThreadGroupSizeX = threadGroupSizeX;
        this.ThreadGroupSizeY = threadGroupSizeY;
        this.ThreadGroupSizeZ = threadGroupSizeZ;
        this.Specializations = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputePipelineDescription" /> type.
    /// </summary>
    /// <param name="shaderStage">Specifies the value of <paramref name="shaderStage" />.</param>
    /// <param name="resourceLayout">Specifies the value of <paramref name="resourceLayout" />.</param>
    /// <param name="threadGroupSizeX">Specifies the value of <paramref name="threadGroupSizeX" />.</param>
    /// <param name="threadGroupSizeY">Specifies the value of <paramref name="threadGroupSizeY" />.</param>
    /// <param name="threadGroupSizeZ">Specifies the value of <paramref name="threadGroupSizeZ" />.</param>
    public ComputePipelineDescription(Shader shaderStage, ResourceLayout resourceLayout, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ) {
        this.ComputeShader = shaderStage;
        this.ResourceLayouts = new[] { resourceLayout };
        this.ThreadGroupSizeX = threadGroupSizeX;
        this.ThreadGroupSizeY = threadGroupSizeY;
        this.ThreadGroupSizeZ = threadGroupSizeZ;
        this.Specializations = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputePipelineDescription" /> type.
    /// </summary>
    /// <param name="shaderStage">Specifies the value of <paramref name="shaderStage" />.</param>
    /// <param name="resourceLayout">Specifies the value of <paramref name="resourceLayout" />.</param>
    /// <param name="threadGroupSizeX">Specifies the value of <paramref name="threadGroupSizeX" />.</param>
    /// <param name="threadGroupSizeY">Specifies the value of <paramref name="threadGroupSizeY" />.</param>
    /// <param name="threadGroupSizeZ">Specifies the value of <paramref name="threadGroupSizeZ" />.</param>
    /// <param name="specializations">Specifies the value of <paramref name="specializations" />.</param>
    public ComputePipelineDescription(Shader shaderStage, ResourceLayout resourceLayout, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ, SpecializationConstant[] specializations) {
        this.ComputeShader = shaderStage;
        this.ResourceLayouts = new[] { resourceLayout };
        this.ThreadGroupSizeX = threadGroupSizeX;
        this.ThreadGroupSizeY = threadGroupSizeY;
        this.ThreadGroupSizeZ = threadGroupSizeZ;
        this.Specializations = specializations;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(ComputePipelineDescription other) {
        return this.ComputeShader.Equals(other.ComputeShader)
               && Util.ArrayEquals(this.ResourceLayouts, other.ResourceLayouts)
               && this.ThreadGroupSizeX.Equals(other.ThreadGroupSizeX)
               && this.ThreadGroupSizeY.Equals(other.ThreadGroupSizeY)
               && this.ThreadGroupSizeZ.Equals(other.ThreadGroupSizeZ);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.ComputeShader.GetHashCode(), HashHelper.Array(this.ResourceLayouts), this.ThreadGroupSizeX.GetHashCode(), this.ThreadGroupSizeY.GetHashCode(), this.ThreadGroupSizeZ.GetHashCode());
    }
}