using System.Text;

namespace Veldrith.SPIRV;

/// <summary>
/// Represents the ResourceFactoryExtensions class.
/// </summary>
public static class ResourceFactoryExtensions {

    /// <summary>
    /// Performs the CreateFromSpirv operation.
    /// </summary>
    /// <param name="factory">The value of factory.</param>
    /// <param name="vertexShaderDescription">The value of vertexShaderDescription.</param>
    /// <param name="fragmentShaderDescription">The value of fragmentShaderDescription.</param>
    /// <returns>The result of the CreateFromSpirv operation.</returns>
    public static Shader[] CreateFromSpirv(this ResourceFactory factory, ShaderDescription vertexShaderDescription, ShaderDescription fragmentShaderDescription) {
        return factory.CreateFromSpirv(vertexShaderDescription, fragmentShaderDescription, new CrossCompileOptions());
    }

    /// <summary>
    /// Performs the CreateFromSpirv operation.
    /// </summary>
    /// <param name="factory">The value of factory.</param>
    /// <param name="vertexShaderDescription">The value of vertexShaderDescription.</param>
    /// <param name="fragmentShaderDescription">The value of fragmentShaderDescription.</param>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the CreateFromSpirv operation.</returns>
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
    /// Performs the CreateFromSpirv operation.
    /// </summary>
    /// <param name="factory">The value of factory.</param>
    /// <param name="computeShaderDescription">The value of computeShaderDescription.</param>
    /// <returns>The result of the CreateFromSpirv operation.</returns>
    public static Shader CreateFromSpirv(this ResourceFactory factory, ShaderDescription computeShaderDescription) {
        return factory.CreateFromSpirv(computeShaderDescription, new CrossCompileOptions());
    }

    /// <summary>
    /// Performs the CreateFromSpirv operation.
    /// </summary>
    /// <param name="factory">The value of factory.</param>
    /// <param name="computeShaderDescription">The value of computeShaderDescription.</param>
    /// <param name="options">The value of options.</param>
    /// <returns>The result of the CreateFromSpirv operation.</returns>
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
    /// Performs the EnsureSpirv operation.
    /// </summary>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the EnsureSpirv operation.</returns>
    private static byte[] EnsureSpirv(ShaderDescription description) {
        if (SpirvCompilation.HasSpirvHeader(description.ShaderBytes)) {
            return description.ShaderBytes;
        }

        SpirvCompilationResult glslCompileResult = SpirvCompilation.CompileGlslToSpirv(Encoding.ASCII.GetString(description.ShaderBytes), null, description.Stage, new GlslCompileOptions(description.Debug));
        return glslCompileResult.SpirvBytes;
    }

    /// <summary>
    /// Performs the GetBytes operation.
    /// </summary>
    /// <param name="backend">The value of backend.</param>
    /// <param name="code">The value of code.</param>
    /// <returns>The result of the GetBytes operation.</returns>
    private static byte[] GetBytes(GraphicsBackend backend, string code) {
        switch (backend) {
            case GraphicsBackend.Direct3D12: case GraphicsBackend.Metal: return Encoding.ASCII.GetBytes(code);
            default: throw new SpirvCompilationException($"Invalid GraphicsBackend: {backend}");
        }
    }

    /// <summary>
    /// Performs the GetCompilationTarget operation.
    /// </summary>
    /// <param name="backend">The value of backend.</param>
    /// <returns>The result of the GetCompilationTarget operation.</returns>
    private static CrossCompileTarget GetCompilationTarget(GraphicsBackend backend) {
        switch (backend) {
            case GraphicsBackend.Direct3D12: return CrossCompileTarget.HLSL;
            case GraphicsBackend.Metal: return CrossCompileTarget.MSL;
            default: throw new SpirvCompilationException($"Invalid GraphicsBackend: {backend}");
        }
    }
}