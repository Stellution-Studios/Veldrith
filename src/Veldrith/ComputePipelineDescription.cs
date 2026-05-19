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
    /// <param name="computeShader">The value of computeShader.</param>
    /// <param name="resourceLayouts">The value of resourceLayouts.</param>
    /// <param name="threadGroupSizeX">The value of threadGroupSizeX.</param>
    /// <param name="threadGroupSizeY">The value of threadGroupSizeY.</param>
    /// <param name="threadGroupSizeZ">The value of threadGroupSizeZ.</param>
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
    /// <param name="shaderStage">The value of shaderStage.</param>
    /// <param name="resourceLayout">The value of resourceLayout.</param>
    /// <param name="threadGroupSizeX">The value of threadGroupSizeX.</param>
    /// <param name="threadGroupSizeY">The value of threadGroupSizeY.</param>
    /// <param name="threadGroupSizeZ">The value of threadGroupSizeZ.</param>
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
    /// <param name="shaderStage">The value of shaderStage.</param>
    /// <param name="resourceLayout">The value of resourceLayout.</param>
    /// <param name="threadGroupSizeX">The value of threadGroupSizeX.</param>
    /// <param name="threadGroupSizeY">The value of threadGroupSizeY.</param>
    /// <param name="threadGroupSizeZ">The value of threadGroupSizeZ.</param>
    /// <param name="specializations">The value of specializations.</param>
    public ComputePipelineDescription(Shader shaderStage, ResourceLayout resourceLayout, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ, SpecializationConstant[] specializations) {
        this.ComputeShader = shaderStage;
        this.ResourceLayouts = new[] { resourceLayout };
        this.ThreadGroupSizeX = threadGroupSizeX;
        this.ThreadGroupSizeY = threadGroupSizeY;
        this.ThreadGroupSizeZ = threadGroupSizeZ;
        this.Specializations = specializations;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(ComputePipelineDescription other) {
        return this.ComputeShader.Equals(other.ComputeShader)
               && Util.ArrayEquals(this.ResourceLayouts, other.ResourceLayouts)
               && this.ThreadGroupSizeX.Equals(other.ThreadGroupSizeX)
               && this.ThreadGroupSizeY.Equals(other.ThreadGroupSizeY)
               && this.ThreadGroupSizeZ.Equals(other.ThreadGroupSizeZ);
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.ComputeShader.GetHashCode(), HashHelper.Array(this.ResourceLayouts), this.ThreadGroupSizeX.GetHashCode(), this.ThreadGroupSizeY.GetHashCode(), this.ThreadGroupSizeZ.GetHashCode());
    }
}