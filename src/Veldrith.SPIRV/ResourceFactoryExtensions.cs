using System.Text;

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

            return new[] {
                factory.CreateShader(ref vertexShaderDescription), factory.CreateShader(ref fragmentShaderDescription)
            };
        }

        CrossCompileTarget target = GetCompilationTarget(factory.BackendType);
        VertexFragmentCompilationResult compilationResult = SpirvCompilation.CompileVertexFragment(vertexShaderDescription.ShaderBytes, fragmentShaderDescription.ShaderBytes, target, options);

        byte[] vertexBytes = GetBytes(backend, compilationResult.VertexShader);
        Shader vertexShader = factory.CreateShader(new ShaderDescription(vertexShaderDescription.Stage, vertexBytes, vertexShaderDescription.EntryPoint));

        byte[] fragmentBytes = GetBytes(backend, compilationResult.FragmentShader);
        Shader fragmentShader = factory.CreateShader(new ShaderDescription(fragmentShaderDescription.Stage, fragmentBytes, fragmentShaderDescription.EntryPoint));

        return new[] { vertexShader, fragmentShader };
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
        return factory.CreateShader(new ShaderDescription(computeShaderDescription.Stage, computeBytes, computeShaderDescription.EntryPoint));
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