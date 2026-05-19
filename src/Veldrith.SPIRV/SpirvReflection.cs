using System.Text.Json;
using System.Text.Json.Serialization;

namespace Veldrith.SPIRV;

/// <summary>
/// Provides SPIR-V compilation support for SpirvReflection.
/// </summary>
public class SpirvReflection {

    /// <summary>
    /// Stores the s json options state used by this instance.
    /// </summary>
    private static readonly JsonSerializerOptions s_jsonOptions = new() {
        WriteIndented = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [JsonConstructor]

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvReflection" /> type.
    /// </summary>
    /// <param name="vertexElements">The vertex elements value used by this operation.</param>
    /// <param name="resourceLayouts">The resource layout used by this operation.</param>
    public SpirvReflection(VertexElementDescription[] vertexElements, ResourceLayoutDescription[] resourceLayouts) {
        this.VertexElements = vertexElements;
        this.ResourceLayouts = resourceLayouts;
    }

    /// <summary>
    /// An array containing a description of each vertex element that is used by the compiled shader set.
    /// </summary>
    public VertexElementDescription[] VertexElements { get; }

    /// <summary>
    /// An array containing a description of each set of resources used by the compiled shader set.
    /// </summary>
    public ResourceLayoutDescription[] ResourceLayouts { get; }

    /// <summary>
    /// Executes the load from json logic for this backend.
    /// </summary>
    /// <param name="jsonPath">The json path value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static SpirvReflection LoadFromJson(string jsonPath) {
        using FileStream jsonStream = File.OpenRead(jsonPath);
        return LoadFromJson(jsonStream);
    }

    /// <summary>
    /// Executes the load from json logic for this backend.
    /// </summary>
    /// <param name="jsonStream">The json stream value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static SpirvReflection LoadFromJson(Stream jsonStream) {
        return JsonSerializer.Deserialize<SpirvReflection>(jsonStream, s_jsonOptions);
    }
}