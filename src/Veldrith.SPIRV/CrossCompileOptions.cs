namespace Veldrith.SPIRV;

/// <summary>
/// Defines the behavior and responsibilities of the CrossCompileOptions class.
/// </summary>
public class CrossCompileOptions {

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossCompileOptions" /> type.
    /// </summary>
    public CrossCompileOptions() {
        this.Specializations = Array.Empty<SpecializationConstant>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossCompileOptions" /> type.
    /// </summary>
    /// <param name="fixClipSpaceZ">Specifies the value of <paramref name="fixClipSpaceZ" />.</param>
    /// <param name="invertVertexOutputY">Specifies the value of <paramref name="invertVertexOutputY" />.</param>
    public CrossCompileOptions(bool fixClipSpaceZ, bool invertVertexOutputY) : this(fixClipSpaceZ, invertVertexOutputY, Array.Empty<SpecializationConstant>()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossCompileOptions" /> type.
    /// </summary>
    /// <param name="fixClipSpaceZ">Specifies the value of <paramref name="fixClipSpaceZ" />.</param>
    /// <param name="invertVertexOutputY">Specifies the value of <paramref name="invertVertexOutputY" />.</param>
    /// <param name="normalizeResourceNames">Specifies the value of <paramref name="normalizeResourceNames" />.</param>
    public CrossCompileOptions(bool fixClipSpaceZ, bool invertVertexOutputY, bool normalizeResourceNames) : this(fixClipSpaceZ, invertVertexOutputY, normalizeResourceNames, Array.Empty<SpecializationConstant>()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossCompileOptions" /> type.
    /// </summary>
    /// <param name="fixClipSpaceZ">Specifies the value of <paramref name="fixClipSpaceZ" />.</param>
    /// <param name="invertVertexOutputY">Specifies the value of <paramref name="invertVertexOutputY" />.</param>
    /// <param name="specializations">Specifies the value of <paramref name="specializations" />.</param>
    public CrossCompileOptions(bool fixClipSpaceZ, bool invertVertexOutputY, params SpecializationConstant[] specializations) {
        this.FixClipSpaceZ = fixClipSpaceZ;
        this.InvertVertexOutputY = invertVertexOutputY;
        this.Specializations = specializations;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossCompileOptions" /> type.
    /// </summary>
    /// <param name="fixClipSpaceZ">Specifies the value of <paramref name="fixClipSpaceZ" />.</param>
    /// <param name="invertVertexOutputY">Specifies the value of <paramref name="invertVertexOutputY" />.</param>
    /// <param name="normalizeResourceNames">Specifies the value of <paramref name="normalizeResourceNames" />.</param>
    /// <param name="specializations">Specifies the value of <paramref name="specializations" />.</param>
    public CrossCompileOptions(bool fixClipSpaceZ, bool invertVertexOutputY, bool normalizeResourceNames, params SpecializationConstant[] specializations) {
        this.FixClipSpaceZ = fixClipSpaceZ;
        this.InvertVertexOutputY = invertVertexOutputY;
        this.NormalizeResourceNames = normalizeResourceNames;
        this.Specializations = specializations;
    }

    /// <summary>
    /// Indicates whether or not the compiled shader output should include a clip-space Z-range fixup at the end of the
    /// vertex shader.
    /// </summary>
    public bool FixClipSpaceZ { get; set; }

    /// <summary>
    /// Indicates whether or not the compiled shader output should include a fixup at the end of the vertex shader which
    /// inverts the clip-space Y value.
    /// </summary>
    public bool InvertVertexOutputY { get; set; }

    /// <summary>
    /// Indicates whether all resource names should be forced into a normalized form. This has functional impact
    /// on compilation targets where resource names are meaningful, like GLSL.
    /// </summary>
    public bool NormalizeResourceNames { get; set; }

    /// <summary>
    /// An array of <see cref="SpecializationConstant" /> which will be substituted into the shader as new constants.
    /// </summary>
    public SpecializationConstant[] Specializations { get; set; }
}