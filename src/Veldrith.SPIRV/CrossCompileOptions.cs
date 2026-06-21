namespace Veldrith.SPIRV;

/// <summary>
/// Represents the CrossCompileOptions type used by the graphics runtime.
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
    /// <param name="fixClipSpaceZ">The fix clip space z value used by this operation.</param>
    /// <param name="invertVertexOutputY">The invert vertex output y value used by this operation.</param>
    public CrossCompileOptions(bool fixClipSpaceZ, bool invertVertexOutputY) : this(fixClipSpaceZ, invertVertexOutputY, Array.Empty<SpecializationConstant>()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossCompileOptions" /> type.
    /// </summary>
    /// <param name="fixClipSpaceZ">The fix clip space z value used by this operation.</param>
    /// <param name="invertVertexOutputY">The invert vertex output y value used by this operation.</param>
    /// <param name="normalizeResourceNames">The normalize resource names value used by this operation.</param>
    public CrossCompileOptions(bool fixClipSpaceZ, bool invertVertexOutputY, bool normalizeResourceNames) : this(fixClipSpaceZ, invertVertexOutputY, normalizeResourceNames, Array.Empty<SpecializationConstant>()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossCompileOptions" /> type.
    /// </summary>
    /// <param name="fixClipSpaceZ">The fix clip space z value used by this operation.</param>
    /// <param name="invertVertexOutputY">The invert vertex output y value used by this operation.</param>
    /// <param name="specializations">The specializations value used by this operation.</param>
    public CrossCompileOptions(bool fixClipSpaceZ, bool invertVertexOutputY, params SpecializationConstant[] specializations) {
        this.FixClipSpaceZ = fixClipSpaceZ;
        this.InvertVertexOutputY = invertVertexOutputY;
        this.Specializations = specializations;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossCompileOptions" /> type.
    /// </summary>
    /// <param name="fixClipSpaceZ">The fix clip space z value used by this operation.</param>
    /// <param name="invertVertexOutputY">The invert vertex output y value used by this operation.</param>
    /// <param name="normalizeResourceNames">The normalize resource names value used by this operation.</param>
    /// <param name="specializations">The specializations value used by this operation.</param>
    public CrossCompileOptions(bool fixClipSpaceZ, bool invertVertexOutputY, bool normalizeResourceNames, params SpecializationConstant[] specializations) {
        this.FixClipSpaceZ = fixClipSpaceZ;
        this.InvertVertexOutputY = invertVertexOutputY;
        this.NormalizeResourceNames = normalizeResourceNames;
        this.Specializations = specializations;
    }

    /// <summary>
    /// Indicates whether or not the compiled shader output should include a clip-space Z-range fixup at the end of the
    /// </summary>
    public bool FixClipSpaceZ { get; set; }

    /// <summary>
    /// Indicates whether or not the compiled shader output should include a fixup at the end of the vertex shader which
    /// </summary>
    public bool InvertVertexOutputY { get; set; }

    /// <summary>
    /// Indicates whether all resource names should be forced into a normalized form. This has functional impact
    /// </summary>
    public bool NormalizeResourceNames { get; set; }

    /// <summary>
    /// An array of <see cref="SpecializationConstant" /> which will be substituted into the shader as new constants.
    /// </summary>
    public SpecializationConstant[] Specializations { get; set; }
}