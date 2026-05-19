using System.Text.Json;
using System.Text.Json.Serialization;

namespace Veldrith.SPIRV;

/// <summary>
/// Defines the behavior and responsibilities of the SpirvReflection class.
/// </summary>
public class SpirvReflection {

    /// <summary>
    /// Stores the value associated with <c>s_jsonOptions</c>.
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
    /// <param name="vertexElements">Specifies the value of <paramref name="vertexElements" />.</param>
    /// <param name="resourceLayouts">Specifies the value of <paramref name="resourceLayouts" />.</param>
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
    /// Executes the LoadFromJson operation.
    /// </summary>
    /// <param name="jsonPath">Specifies the value of <paramref name="jsonPath" />.</param>
    /// <returns>Returns the result produced by the LoadFromJson operation.</returns>
    public static SpirvReflection LoadFromJson(string jsonPath) {
        using FileStream jsonStream = File.OpenRead(jsonPath);
        return LoadFromJson(jsonStream);
    }

    /// <summary>
    /// Executes the LoadFromJson operation.
    /// </summary>
    /// <param name="jsonStream">Specifies the value of <paramref name="jsonStream" />.</param>
    /// <returns>Returns the result produced by the LoadFromJson operation.</returns>
    public static SpirvReflection LoadFromJson(Stream jsonStream) {
        return JsonSerializer.Deserialize<SpirvReflection>(jsonStream, s_jsonOptions);
    }
}
