using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Shaderc;

namespace Veldrith.SPIRV;

/// <summary>
/// Represents the SpirvCompilation class.
/// </summary>
public static unsafe class SpirvCompilation {

    /// <summary>
    /// Performs the GetApi operation.
    /// </summary>
    /// <returns>The result of the GetApi operation.</returns>
    private static readonly Shaderc s_shaderc = Shaderc.GetApi();

    /// <summary>
    /// Performs the CompilerInitialize operation.
    /// </summary>
    /// <returns>The result of the CompilerInitialize operation.</returns>
    private static readonly Compiler* s_compiler = s_shaderc.CompilerInitialize();

    /// <summary>
    /// Performs the CompileVertexFragment operation.
    /// </summary>
    /// <param name="vsBytes">The value of vsBytes.</param>
    /// <param name="fsBytes">The value of fsBytes.</param>
    /// <param name="target">The value of target.</param>
    /// <returns>The result of the CompileVertexFragment operation.</returns>
    public static VertexFragmentCompilationResult CompileVertexFragment(byte[] vsBytes, byte[] fsBytes, CrossCompileTarget target) {
        return CompileVertexFragment(vsBytes, fsBytes, target, new CrossCompileOptions());
    }

    /// <summary>
    /// Performs the CompileVertexFragment operation.
    /// </summary>
    /// <param name="vsBytes">The value of vsBytes.</param>
    /// <param name="fsBytes">The value of fsBytes.</param>
    /// <param name="target">The value of target.</param>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the CompileVertexFragment operation.</returns>
    public static VertexFragmentCompilationResult CompileVertexFragment(byte[] vsBytes, byte[] fsBytes, CrossCompileTarget target, CrossCompileOptions options) {
        byte[] vsSpirvBytes = EnsureSpirvBytes(vsBytes, ShaderStages.Vertex, target);
        byte[] fsSpirvBytes = EnsureSpirvBytes(fsBytes, ShaderStages.Fragment, target);
        return SpirvCrossCompiler.CompileVertexFragment(vsSpirvBytes, fsSpirvBytes, target, options);
    }

    /// <summary>
    /// Performs the CompileCompute operation.
    /// </summary>
    /// <param name="csBytes">The value of csBytes.</param>
    /// <param name="target">The value of target.</param>
    /// <returns>The result of the CompileCompute operation.</returns>
    public static ComputeCompilationResult CompileCompute(byte[] csBytes, CrossCompileTarget target) {
        return CompileCompute(csBytes, target, new CrossCompileOptions());
    }

    /// <summary>
    /// Performs the CompileCompute operation.
    /// </summary>
    /// <param name="csBytes">The value of csBytes.</param>
    /// <param name="target">The value of target.</param>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the CompileCompute operation.</returns>
    public static ComputeCompilationResult CompileCompute(byte[] csBytes, CrossCompileTarget target, CrossCompileOptions options) {
        byte[] csSpirvBytes = EnsureSpirvBytes(csBytes, ShaderStages.Compute, target);
        return SpirvCrossCompiler.CompileCompute(csSpirvBytes, target, options);
    }

    /// <summary>
    /// Performs the CompileGlslToSpirv operation.
    /// </summary>
    /// <param name="sourceText">The value of sourceText.</param>
    /// <param name="fileName">The value of fileName.</param>
    /// <param name="stage">The value of stage.</param>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the CompileGlslToSpirv operation.</returns>
    public static SpirvCompilationResult CompileGlslToSpirv(string sourceText, string fileName, ShaderStages stage, GlslCompileOptions options) {
        Shaderc shaderc = s_shaderc;
        Compiler* compiler = s_compiler;
        CompileOptions* compileOptions = null;
        CompilationResult* result = null;
        try {
            compileOptions = shaderc.CompileOptionsInitialize();
            if (compileOptions == null) {
                throw new SpirvCompilationException("Failed to initialize compile options.");
            }

            if (options.Debug) {
                shaderc.CompileOptionsSetGenerateDebugInfo(compileOptions);
            }
            else {
                shaderc.CompileOptionsSetOptimizationLevel(compileOptions, OptimizationLevel.Performance);
            }

            foreach (MacroDefinition macro in options.Macros) {
                byte[] nameBytes = Encoding.ASCII.GetBytes(macro.Name);
                if (string.IsNullOrEmpty(macro.Value)) {
                    fixed (byte* namePtr = nameBytes) {
                        shaderc.CompileOptionsAddMacroDefinition(compileOptions, namePtr, (nuint)nameBytes.Length, (byte*)null, 0);
                    }
                }
                else {
                    byte[] valueBytes = Encoding.ASCII.GetBytes(macro.Value);
                    fixed (byte* namePtr = nameBytes)
                    fixed (byte* valuePtr = valueBytes) {
                        shaderc.CompileOptionsAddMacroDefinition(compileOptions, namePtr, (nuint)nameBytes.Length, valuePtr, (nuint)valueBytes.Length);
                    }
                }
            }

            ShaderKind shaderKind = GetShadercKind(stage);
            byte[] sourceBytes = Encoding.ASCII.GetBytes(sourceText);
            if (string.IsNullOrEmpty(fileName)) {
                fileName = "<veldrid-spirv-input>";
            }

            byte[] fileNameBytes = Encoding.ASCII.GetBytes(fileName + '\0');
            byte[] entryPointBytes = "main\0"u8.ToArray();

            fixed (byte* sourcePtr = sourceBytes)
            fixed (byte* fileNamePtr = fileNameBytes)
            fixed (byte* entryPtr = entryPointBytes) {
                result = shaderc.CompileIntoSpv(compiler, sourcePtr, (nuint)sourceBytes.Length, shaderKind, fileNamePtr, entryPtr, compileOptions);
            }

            if (result == null) {
                throw new SpirvCompilationException("Shaderc returned null result.");
            }

            CompilationStatus status = shaderc.ResultGetCompilationStatus(result);
            if (status != CompilationStatus.Success) {
                byte* errorMsgPtr = shaderc.ResultGetErrorMessage(result);
                string errorMsg = errorMsgPtr != null
                    ? Marshal.PtrToStringUTF8((nint)errorMsgPtr)
                    : "Unknown error";
                throw new SpirvCompilationException("GLSL compilation failed: " + errorMsg);
            }

            byte* bytesPtr = shaderc.ResultGetBytes(result);
            nuint length = shaderc.ResultGetLength(result);
            byte[] spirvBytes = new byte[(int)length];
            new Span<byte>(bytesPtr, (int)length).CopyTo(spirvBytes);

            return new SpirvCompilationResult(spirvBytes);
        }
        finally {
            if (result != null) {
                shaderc.ResultRelease(result);
            }

            if (compileOptions != null) {
                shaderc.CompileOptionsRelease(compileOptions);
            }
        }
    }

    /// <summary>
    /// Performs the EnsureSpirvBytes operation.
    /// </summary>
    /// <param name="bytes">The value of bytes.</param>
    /// <param name="stage">The value of stage.</param>
    /// <param name="target">The value of target.</param>
    /// <returns>The result of the EnsureSpirvBytes operation.</returns>
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
    /// Performs the HasSpirvHeader operation.
    /// </summary>
    /// <param name="bytes">The value of bytes.</param>
    /// <returns>The result of the HasSpirvHeader operation.</returns>
    internal static bool HasSpirvHeader(byte[] bytes) {
        return bytes.Length > 4
               && bytes[0] == 0x03
               && bytes[1] == 0x02
               && bytes[2] == 0x23
               && bytes[3] == 0x07;
    }

    /// <summary>
    /// Performs the GetShadercKind operation.
    /// </summary>
    /// <param name="stage">The value of stage.</param>
    /// <returns>The result of the GetShadercKind operation.</returns>
    private static ShaderKind GetShadercKind(ShaderStages stage) {
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