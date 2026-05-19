namespace Veldrith.SPIRV;

/// <summary>
/// Defines the behavior and responsibilities of the GlslCompileOptions class.
/// </summary>
public class GlslCompileOptions {

    /// <summary>
    /// Initializes a new instance of the <see cref="GlslCompileOptions" /> type.
    /// </summary>
    public GlslCompileOptions() {
        this.Macros = Array.Empty<MacroDefinition>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlslCompileOptions" /> type.
    /// </summary>
    /// <param name="debug">Specifies the value of <paramref name="debug" />.</param>
    /// <param name="macros">Specifies the value of <paramref name="macros" />.</param>
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
    /// Stores the value associated with <c>get</c>.
    /// </summary>
    public static GlslCompileOptions Default { get; } = new();
}
