using System.Text;
using System.Text.RegularExpressions;

namespace Veldrith.SPIRV;

/// <summary>
/// Represents the ResourceFactoryExtensions type used by the graphics runtime.
/// </summary>
public static class ResourceFactoryExtensions {

    /// <summary>
    /// Creates the from spirv instance used by this backend.
    /// </summary>
    /// <param name="factory">The factory value used by this operation.</param>
    /// <param name="vertexShaderDescription">The vertex shader description value used by this operation.</param>
    /// <param name="fragmentShaderDescription">The fragment shader description value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static Shader[] CreateFromSpirv(this ResourceFactory factory, ShaderDescription vertexShaderDescription, ShaderDescription fragmentShaderDescription) {
        return factory.CreateFromSpirv(vertexShaderDescription, fragmentShaderDescription, new CrossCompileOptions());
    }

    /// <summary>
    /// Creates the from spirv instance used by this backend.
    /// </summary>
    /// <param name="factory">The factory value used by this operation.</param>
    /// <param name="vertexShaderDescription">The vertex shader description value used by this operation.</param>
    /// <param name="fragmentShaderDescription">The fragment shader description value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static Shader[] CreateFromSpirv(this ResourceFactory factory, ShaderDescription vertexShaderDescription, ShaderDescription fragmentShaderDescription, CrossCompileOptions options) {
        GraphicsBackend backend = factory.BackendType;
        if (backend == GraphicsBackend.Vulkan) {
            vertexShaderDescription.ShaderBytes = EnsureSpirv(vertexShaderDescription);
            fragmentShaderDescription.ShaderBytes = EnsureSpirv(fragmentShaderDescription);

            return [
                factory.CreateShader(ref vertexShaderDescription), factory.CreateShader(ref fragmentShaderDescription)
            ];
        }

        CrossCompileTarget target = GetCompilationTarget(factory.BackendType);
        VertexFragmentCompilationResult compilationResult = SpirvCompilation.CompileVertexFragment(vertexShaderDescription.ShaderBytes, fragmentShaderDescription.ShaderBytes, target, options);

        byte[] vertexBytes = GetBytes(backend, compilationResult.VertexShader);
        string vertexEntryPoint = GetTranslatedEntryPoint(backend, compilationResult.VertexShader, vertexShaderDescription.Stage, vertexShaderDescription.EntryPoint);
        Shader vertexShader = factory.CreateShader(new ShaderDescription(vertexShaderDescription.Stage, vertexBytes, vertexEntryPoint));

        byte[] fragmentBytes = GetBytes(backend, compilationResult.FragmentShader);
        string fragmentEntryPoint = GetTranslatedEntryPoint(backend, compilationResult.FragmentShader, fragmentShaderDescription.Stage, fragmentShaderDescription.EntryPoint);
        Shader fragmentShader = factory.CreateShader(new ShaderDescription(fragmentShaderDescription.Stage, fragmentBytes, fragmentEntryPoint));

        return [vertexShader, fragmentShader];
    }

    /// <summary>
    /// Creates the from spirv instance used by this backend.
    /// </summary>
    /// <param name="factory">The factory value used by this operation.</param>
    /// <param name="computeShaderDescription">The compute shader description value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static Shader CreateFromSpirv(this ResourceFactory factory, ShaderDescription computeShaderDescription) {
        return factory.CreateFromSpirv(computeShaderDescription, new CrossCompileOptions());
    }

    /// <summary>
    /// Creates the from spirv instance used by this backend.
    /// </summary>
    /// <param name="factory">The factory value used by this operation.</param>
    /// <param name="computeShaderDescription">The compute shader description value used by this operation.</param>
    /// <param name="options">The options used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static Shader CreateFromSpirv(this ResourceFactory factory, ShaderDescription computeShaderDescription, CrossCompileOptions options) {
        GraphicsBackend backend = factory.BackendType;
        if (backend == GraphicsBackend.Vulkan) {
            computeShaderDescription.ShaderBytes = EnsureSpirv(computeShaderDescription);
            return factory.CreateShader(ref computeShaderDescription);
        }

        CrossCompileTarget target = GetCompilationTarget(factory.BackendType);
        ComputeCompilationResult compilationResult = SpirvCompilation.CompileCompute(computeShaderDescription.ShaderBytes, target, options);

        byte[] computeBytes = GetBytes(backend, compilationResult.ComputeShader);
        string computeEntryPoint = GetTranslatedEntryPoint(backend, compilationResult.ComputeShader, computeShaderDescription.Stage, computeShaderDescription.EntryPoint);
        return factory.CreateShader(new ShaderDescription(computeShaderDescription.Stage, computeBytes, computeEntryPoint));
    }

    /// <summary>
    /// Executes the ensure spirv logic for this backend.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static byte[] EnsureSpirv(ShaderDescription description) {
        if (SpirvCompilation.HasSpirvHeader(description.ShaderBytes)) {
            return description.ShaderBytes;
        }

        SpirvCompilationResult glslCompileResult = SpirvCompilation.CompileGlslToSpirv(Encoding.ASCII.GetString(description.ShaderBytes), null, description.Stage, new GlslCompileOptions(description.Debug));
        return glslCompileResult.SpirvBytes;
    }

    /// <summary>
    /// Gets the bytes value.
    /// </summary>
    /// <param name="backend">The backend value used by this operation.</param>
    /// <param name="code">The code value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static byte[] GetBytes(GraphicsBackend backend, string code) {
        switch (backend) {
            case GraphicsBackend.Direct3D12: case GraphicsBackend.Metal: return Encoding.ASCII.GetBytes(code);
            default: throw new SpirvCompilationException($"Invalid GraphicsBackend: {backend}");
        }
    }

    /// <summary>
    /// Resolves the backend entry point after source cross-compilation.
    /// </summary>
    /// <param name="backend">The backend that will consume the translated shader.</param>
    /// <param name="code">The translated shader source code.</param>
    /// <param name="stage">The shader stage.</param>
    /// <param name="fallback">The original entry point.</param>
    /// <returns>The entry point visible to the backend compiler.</returns>
    private static string GetTranslatedEntryPoint(GraphicsBackend backend, string code, ShaderStages stage, string fallback) {
        if (backend != GraphicsBackend.Metal) {
            return fallback;
        }

        string stageKeyword = stage switch {
            ShaderStages.Vertex => "vertex",
            ShaderStages.Fragment => "fragment",
            ShaderStages.Compute => "kernel",
            _ => null
        };

        if (stageKeyword == null || string.IsNullOrWhiteSpace(code)) {
            return fallback;
        }

        Match match = Regex.Match(code, $@"\b{stageKeyword}\b[\s\S]*?\b([A-Za-z_][A-Za-z0-9_]*)\s*\(", RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value : fallback;
    }

    /// <summary>
    /// Gets the compilation target value.
    /// </summary>
    /// <param name="backend">The backend value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static CrossCompileTarget GetCompilationTarget(GraphicsBackend backend) {
        switch (backend) {
            case GraphicsBackend.Direct3D12: return CrossCompileTarget.HLSL;
            case GraphicsBackend.Metal: return CrossCompileTarget.MSL;
            default: throw new SpirvCompilationException($"Invalid GraphicsBackend: {backend}");
        }
    }
}
