namespace Veldrith.SPIRV;

/// <summary>
/// Represents the GlslCompileOptions type used by the graphics runtime.
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
    /// <param name="debug">The debug value used by this operation.</param>
    /// <param name="macros">The macros value used by this operation.</param>
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
    /// </summary>
    public MacroDefinition[] Macros { get; set; }

    /// <summary>
    /// Stores the default state used by this instance.
    /// </summary>
    public static GlslCompileOptions Default { get; } = new();
}