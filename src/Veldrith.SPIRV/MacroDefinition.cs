namespace Veldrith.SPIRV;

/// <summary>
///     Represents a single preprocessor macro used when compiling shader source code.
/// </summary>
public class MacroDefinition {
    /// <summary>
    ///     Constructs a new <see cref="MacroDefinition" /> with no value.
    /// </summary>
    public MacroDefinition(string name) {
        this.Name = name;
    }

    /// <summary>
    ///     Constructs a new <see cref="MacroDefinition" /> with a value.
    /// </summary>
    public MacroDefinition(string name, string value) {
        this.Name = name;
        this.Value = value;
    }

    internal MacroDefinition() { }

    /// <summary>
    ///     The name of the macro.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     The macro's replacement value. May be null.
    /// </summary>
    public string Value { get; set; }
}