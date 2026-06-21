using System;

namespace Veldrith;

/// <summary>
/// Describes a compute <see cref="Pipeline" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct ComputePipelineDescription : IEquatable<ComputePipelineDescription> {

    /// <summary>
    /// The compute <see cref="Shader" /> to be used in the Pipeline. This must be a Shader with
    /// </summary>
    public Shader ComputeShader;

    /// <summary>
    /// An array of <see cref="ResourceLayout" />, which controls the layout of shader resoruces in the
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
    /// </summary>
    public SpecializationConstant[] Specializations;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputePipelineDescription" /> type.
    /// </summary>
    /// <param name="computeShader">The compute shader value used by this operation.</param>
    /// <param name="resourceLayouts">The resource layout used by this operation.</param>
    /// <param name="threadGroupSizeX">The thread group size x value used by this operation.</param>
    /// <param name="threadGroupSizeY">The thread group size y value used by this operation.</param>
    /// <param name="threadGroupSizeZ">The thread group size z value used by this operation.</param>
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
    /// <param name="shaderStage">The shader stage value used by this operation.</param>
    /// <param name="resourceLayout">The resource layout value used by this operation.</param>
    /// <param name="threadGroupSizeX">The thread group size x value used by this operation.</param>
    /// <param name="threadGroupSizeY">The thread group size y value used by this operation.</param>
    /// <param name="threadGroupSizeZ">The thread group size z value used by this operation.</param>
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
    /// <param name="shaderStage">The shader stage value used by this operation.</param>
    /// <param name="resourceLayout">The resource layout value used by this operation.</param>
    /// <param name="threadGroupSizeX">The thread group size x value used by this operation.</param>
    /// <param name="threadGroupSizeY">The thread group size y value used by this operation.</param>
    /// <param name="threadGroupSizeZ">The thread group size z value used by this operation.</param>
    /// <param name="specializations">The specializations value used by this operation.</param>
    public ComputePipelineDescription(Shader shaderStage, ResourceLayout resourceLayout, uint threadGroupSizeX, uint threadGroupSizeY, uint threadGroupSizeZ, SpecializationConstant[] specializations) {
        this.ComputeShader = shaderStage;
        this.ResourceLayouts = new[] { resourceLayout };
        this.ThreadGroupSizeX = threadGroupSizeX;
        this.ThreadGroupSizeY = threadGroupSizeY;
        this.ThreadGroupSizeZ = threadGroupSizeZ;
        this.Specializations = specializations;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(ComputePipelineDescription other) {
        return this.ComputeShader.Equals(other.ComputeShader)
               && Util.ArrayEquals(this.ResourceLayouts, other.ResourceLayouts)
               && this.ThreadGroupSizeX.Equals(other.ThreadGroupSizeX)
               && this.ThreadGroupSizeY.Equals(other.ThreadGroupSizeY)
               && this.ThreadGroupSizeZ.Equals(other.ThreadGroupSizeZ);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.ComputeShader.GetHashCode(), HashHelper.Array(this.ResourceLayouts), this.ThreadGroupSizeX.GetHashCode(), this.ThreadGroupSizeY.GetHashCode(), this.ThreadGroupSizeZ.GetHashCode());
    }
}