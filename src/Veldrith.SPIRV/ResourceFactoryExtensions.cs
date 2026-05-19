using System.Text;

namespace Veldrith.SPIRV;

/// <summary>
/// Defines the behavior and responsibilities of the ResourceFactoryExtensions class.
/// </summary>
public static class ResourceFactoryExtensions {

    /// <summary>
    /// Executes the CreateFromSpirv operation.
    /// </summary>
    /// <param name="factory">Specifies the value of <paramref name="factory" />.</param>
    /// <param name="vertexShaderDescription">Specifies the value of <paramref name="vertexShaderDescription" />.</param>
    /// <param name="fragmentShaderDescription">Specifies the value of <paramref name="fragmentShaderDescription" />.</param>
    /// <returns>Returns the result produced by the CreateFromSpirv operation.</returns>
    public static Shader[] CreateFromSpirv(this ResourceFactory factory, ShaderDescription vertexShaderDescription, ShaderDescription fragmentShaderDescription) {
        return factory.CreateFromSpirv(vertexShaderDescription, fragmentShaderDescription, new CrossCompileOptions());
    }

    /// <summary>
    /// Executes the CreateFromSpirv operation.
    /// </summary>
    /// <param name="factory">Specifies the value of <paramref name="factory" />.</param>
    /// <param name="vertexShaderDescription">Specifies the value of <paramref name="vertexShaderDescription" />.</param>
    /// <param name="fragmentShaderDescription">Specifies the value of <paramref name="fragmentShaderDescription" />.</param>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <returns>Returns the result produced by the CreateFromSpirv operation.</returns>
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
    /// Executes the CreateFromSpirv operation.
    /// </summary>
    /// <param name="factory">Specifies the value of <paramref name="factory" />.</param>
    /// <param name="computeShaderDescription">Specifies the value of <paramref name="computeShaderDescription" />.</param>
    /// <returns>Returns the result produced by the CreateFromSpirv operation.</returns>
    public static Shader CreateFromSpirv(this ResourceFactory factory, ShaderDescription computeShaderDescription) {
        return factory.CreateFromSpirv(computeShaderDescription, new CrossCompileOptions());
    }

    /// <summary>
    /// Executes the CreateFromSpirv operation.
    /// </summary>
    /// <param name="factory">Specifies the value of <paramref name="factory" />.</param>
    /// <param name="computeShaderDescription">Specifies the value of <paramref name="computeShaderDescription" />.</param>
    /// <param name="options">Specifies the value of <paramref name="options" />.</param>
    /// <returns>Returns the result produced by the CreateFromSpirv operation.</returns>
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
    /// Executes the EnsureSpirv operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the EnsureSpirv operation.</returns>
    private static byte[] EnsureSpirv(ShaderDescription description) {
        if (SpirvCompilation.HasSpirvHeader(description.ShaderBytes)) {
            return description.ShaderBytes;
        }

        SpirvCompilationResult glslCompileResult = SpirvCompilation.CompileGlslToSpirv(Encoding.ASCII.GetString(description.ShaderBytes), null, description.Stage, new GlslCompileOptions(description.Debug));
        return glslCompileResult.SpirvBytes;
    }

    /// <summary>
    /// Executes the GetBytes operation.
    /// </summary>
    /// <param name="backend">Specifies the value of <paramref name="backend" />.</param>
    /// <param name="code">Specifies the value of <paramref name="code" />.</param>
    /// <returns>Returns the result produced by the GetBytes operation.</returns>
    private static byte[] GetBytes(GraphicsBackend backend, string code) {
        switch (backend) {
            case GraphicsBackend.Direct3D12: case GraphicsBackend.Metal: return Encoding.ASCII.GetBytes(code);
            default: throw new SpirvCompilationException($"Invalid GraphicsBackend: {backend}");
        }
    }

    /// <summary>
    /// Executes the GetCompilationTarget operation.
    /// </summary>
    /// <param name="backend">Specifies the value of <paramref name="backend" />.</param>
    /// <returns>Returns the result produced by the GetCompilationTarget operation.</returns>
    private static CrossCompileTarget GetCompilationTarget(GraphicsBackend backend) {
        switch (backend) {
            case GraphicsBackend.Direct3D12: return CrossCompileTarget.HLSL;
            case GraphicsBackend.Metal: return CrossCompileTarget.MSL;
            default: throw new SpirvCompilationException($"Invalid GraphicsBackend: {backend}");
        }
    }
}