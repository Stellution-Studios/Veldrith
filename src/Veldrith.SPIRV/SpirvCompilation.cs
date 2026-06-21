using System.Text;
using Vortice.ShaderCompiler;

namespace Veldrith.SPIRV;

/// <summary>
/// Provides SPIR-V compilation support for SpirvCompilation.
/// </summary>
public static class SpirvCompilation {

    /// <summary>
    /// Executes the compile vertex fragment logic for this backend.
    /// </summary>
    /// <param name="vsBytes">The vs bytes value used by this operation.</param>
    /// <param name="fsBytes">The fs bytes value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static VertexFragmentCompilationResult CompileVertexFragment(byte[] vsBytes, byte[] fsBytes, CrossCompileTarget target) {
        return CompileVertexFragment(vsBytes, fsBytes, target, new CrossCompileOptions());
    }

    /// <summary>
    /// Executes the compile vertex fragment logic for this backend.
    /// </summary>
    /// <param name="vsBytes">The vs bytes value used by this operation.</param>
    /// <param name="fsBytes">The fs bytes value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static VertexFragmentCompilationResult CompileVertexFragment(byte[] vsBytes, byte[] fsBytes, CrossCompileTarget target, CrossCompileOptions options) {
        byte[] vsSpirvBytes = EnsureSpirvBytes(vsBytes, ShaderStages.Vertex, target);
        byte[] fsSpirvBytes = EnsureSpirvBytes(fsBytes, ShaderStages.Fragment, target);
        return SpirvCrossCompiler.CompileVertexFragment(vsSpirvBytes, fsSpirvBytes, target, options);
    }

    /// <summary>
    /// Executes the compile compute logic for this backend.
    /// </summary>
    /// <param name="csBytes">The cs bytes value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static ComputeCompilationResult CompileCompute(byte[] csBytes, CrossCompileTarget target) {
        return CompileCompute(csBytes, target, new CrossCompileOptions());
    }

    /// <summary>
    /// Executes the compile compute logic for this backend.
    /// </summary>
    /// <param name="csBytes">The cs bytes value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static ComputeCompilationResult CompileCompute(byte[] csBytes, CrossCompileTarget target, CrossCompileOptions options) {
        byte[] csSpirvBytes = EnsureSpirvBytes(csBytes, ShaderStages.Compute, target);
        return SpirvCrossCompiler.CompileCompute(csSpirvBytes, target, options);
    }

    /// <summary>
    /// Executes the compile glsl to spirv logic for this backend.
    /// </summary>
    /// <param name="sourceText">The source text value used by this operation.</param>
    /// <param name="fileName">The file name value used by this operation.</param>
    /// <param name="stage">The stage value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static SpirvCompilationResult CompileGlslToSpirv(string sourceText, string fileName, ShaderStages stage, GlslCompileOptions options) {
        if (string.IsNullOrEmpty(fileName)) {
            fileName = "<veldrid-spirv-input>";
        }

        CompilerOptions compilerOptions = new() {
            ShaderStage = GetShaderKind(stage),
            GeneratedDebug = options.Debug,
            OptimizationLevel = options.Debug ? OptimizationLevel.Zero : OptimizationLevel.Performance,
            EntryPoint = "main"
        };

        foreach (MacroDefinition macro in options.Macros) {
            compilerOptions.Defines.Add(new ShaderMacro(macro.Name, macro.Value));
        }

        using Compiler compiler = new();
        CompileResult result = compiler.Compile(sourceText, fileName, compilerOptions);
        if (result.Status != CompilationStatus.Success) {
            throw new SpirvCompilationException("GLSL compilation failed: " + result.ErrorMessage);
        }

        return new SpirvCompilationResult(result.Bytecode);
    }

    /// <summary>
    /// Executes the ensure spirv bytes logic for this backend.
    /// </summary>
    /// <param name="bytes">The bytes value used by this operation.</param>
    /// <param name="stage">The stage value used by this operation.</param>
    /// <param name="target">The target value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static byte[] EnsureSpirvBytes(byte[] bytes, ShaderStages stage, CrossCompileTarget target) {
        if (HasSpirvHeader(bytes)) {
            return bytes;
        }

        string sourceText = Encoding.ASCII.GetString(bytes);
        bool debug = target == CrossCompileTarget.GLSL;
        SpirvCompilationResult result = CompileGlslToSpirv(sourceText, string.Empty, stage, new GlslCompileOptions(debug));
        return result.SpirvBytes;
    }

    /// <summary>
    /// Executes the has spirv header logic for this backend.
    /// </summary>
    /// <param name="bytes">The bytes value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal static bool HasSpirvHeader(byte[] bytes) {
        return bytes.Length > 4
               && bytes[0] == 0x03
               && bytes[1] == 0x02
               && bytes[2] == 0x23
               && bytes[3] == 0x07;
    }

    /// <summary>
    /// Gets the shader kind value.
    /// </summary>
    /// <param name="stage">The stage value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static ShaderKind GetShaderKind(ShaderStages stage) {
        return stage switch {
            ShaderStages.Vertex => ShaderKind.VertexShader,
            ShaderStages.Fragment => ShaderKind.FragmentShader,
            ShaderStages.Compute => ShaderKind.ComputeShader,
            ShaderStages.Geometry => ShaderKind.GeometryShader,
            ShaderStages.TessellationControl => ShaderKind.TessControlShader,
            ShaderStages.TessellationEvaluation => ShaderKind.TessEvaluationShader,
            _ => throw new SpirvCompilationException($"Invalid shader stage: {stage}")
        };
    }
}
