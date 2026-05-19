using System.Text.Json;
using System.Text.Json.Serialization;

namespace Veldrith.SPIRV;

/// <summary>
/// Represents the SpirvReflection class.
/// </summary>
public class SpirvReflection {

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    private static readonly JsonSerializerOptions s_jsonOptions = new() {
        WriteIndented = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [JsonConstructor]

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvReflection" /> type.
    /// </summary>
    /// <param name="vertexElements">The value of vertexElements.</param>
    /// <param name="resourceLayouts">The value of resourceLayouts.</param>
    public SpirvReflection(VertexElementDescription[] vertexElements, ResourceLayoutDescription[] resourceLayouts) {
        this.VertexElements = vertexElements;
        this.ResourceLayouts = resourceLayouts;
    }

    /// <summary>
    /// An array containing a description of each vertex element that is used by the compiled shader set.
    /// This array will be empty for compute shaders.
    /// </summary>
    public VertexElementDescription[] VertexElements { get; }

    /// <summary>
    /// An array containing a description of each set of resources used by the compiled shader set.
    /// </summary>
    public ResourceLayoutDescription[] ResourceLayouts { get; }

    /// <summary>
    /// Performs the LoadFromJson operation.
    /// </summary>
    /// <param name="jsonPath">The value of jsonPath.</param>
    /// <returns>The result of the LoadFromJson operation.</returns>
    public static SpirvReflection LoadFromJson(string jsonPath) {
        using FileStream jsonStream = File.OpenRead(jsonPath);
        return LoadFromJson(jsonStream);
    }

    /// <summary>
    /// Performs the LoadFromJson operation.
    /// </summary>
    /// <param name="jsonStream">The value of jsonStream.</param>
    /// <returns>The result of the LoadFromJson operation.</returns>
    public static SpirvReflection LoadFromJson(Stream jsonStream) {
        return JsonSerializer.Deserialize<SpirvReflection>(jsonStream, s_jsonOptions);
    }
}