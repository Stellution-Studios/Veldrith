namespace Veldrith.SPIRV;

/// <summary>
/// Identifies a particular shading language.
/// </summary>
public enum CrossCompileTarget : uint {

    /// <summary>
    /// HLSL Shader Model 5.
    /// </summary>
    HLSL,

    /// <summary>
    /// GLSL source output, version 330 or 430.
    /// </summary>
    GLSL,

    /// <summary>
    /// Metal Shading Language.
    /// </summary>
    MSL
}
