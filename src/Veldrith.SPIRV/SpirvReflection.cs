using System.Text.Json;
using System.Text.Json.Serialization;

namespace Veldrith.SPIRV;

/// <summary>
/// Represents the SpirvReflection class.
/// </summary>
public class SpirvReflection {

    /// <summary>
    /// Represents the s_jsonOptions field.
    /// </summary>
    private static readonly JsonSerializerOptions s_jsonOptions = new() {
        WriteIndented = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Constructs a new <see cref="SpirvReflection" /> instance.
    /// </summary>
    [JsonConstructor]

    /// <summary>
    /// Initializes a new instance of the <see cref="SpirvReflection" /> class.
    /// </summary>
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
    /// Loads a <see cref="SpirvReflection" /> object from a serialized JSON file at the given path.
    /// </summary>
    /// <param name="jsonPath">The path to the JSON file.</param>
    /// <returns>A new <see cref="SpirvReflection" /> object, deserialized from the file.</returns>
    public static SpirvReflection LoadFromJson(string jsonPath) {
        using FileStream jsonStream = File.OpenRead(jsonPath);
        return LoadFromJson(jsonStream);
    }

    /// <summary>
    /// Loads a <see cref="SpirvReflection" /> object from a serialized JSON stream.
    /// </summary>
    /// <param name="jsonStream">The stream of serialized JSON text.</param>
    /// <returns>A new <see cref="SpirvReflection" /> object, deserialized from the stream.</returns>
    public static SpirvReflection LoadFromJson(Stream jsonStream) {
        return JsonSerializer.Deserialize<SpirvReflection>(jsonStream, s_jsonOptions);
    }
}