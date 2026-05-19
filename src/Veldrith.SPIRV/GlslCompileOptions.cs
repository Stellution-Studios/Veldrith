namespace Veldrith.SPIRV;

/// <summary>
/// Represents the GlslCompileOptions class.
/// </summary>
public class GlslCompileOptions {

    /// <summary>
    /// Constructs a new <see cref="GlslCompileOptions" /> with default properties.
    /// </summary>
    public GlslCompileOptions() {
        this.Macros = Array.Empty<MacroDefinition>();
    }

    /// <summary>
    /// Constructs a new <see cref="GlslCompileOptions" />.
    /// </summary>
    public GlslCompileOptions(bool debug, params MacroDefinition[] macros) {
        this.Debug = debug;
        this.Macros = macros ?? Array.Empty<MacroDefinition>();
    }

    /// <summary>
    /// Indicates whether the compiled output should preserve debug information.
    /// </summary>
    public bool Debug { get; set; }

    /// <summary>
    /// An array of <see cref="MacroDefinition" /> which defines the set of preprocessor macros to define when compiling the
    /// GLSL source code.
    /// </summary>
    public MacroDefinition[] Macros { get; set; }

    /// <summary>
    /// Gets a default <see cref="GlslCompileOptions" />.
    /// </summary>
    public static GlslCompileOptions Default { get; } = new();
}

