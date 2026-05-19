namespace Veldrith.SPIRV;

/// <summary>
/// Represents the GlslCompileOptions class.
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
    /// <param name="debug">The value of debug.</param>
    /// <param name="macros">The value of macros.</param>
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
    /// Performs the new operation.
    /// </summary>
    /// <returns>The result of the new operation.</returns>
    public static GlslCompileOptions Default { get; } = new();
}