namespace Veldrith.SPIRV;

/// <summary>
/// Represents the MacroDefinition class.
/// </summary>
public class MacroDefinition {

    /// <summary>
    /// Initializes a new instance of the <see cref="MacroDefinition" /> type.
    /// </summary>
    /// <param name="name">The value of name.</param>
    public MacroDefinition(string name) {
        this.Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MacroDefinition" /> type.
    /// </summary>
    /// <param name="name">The value of name.</param>
    /// <param name="value">The value of value.</param>
    public MacroDefinition(string name, string value) {
        this.Name = name;
        this.Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MacroDefinition" /> type.
    /// </summary>
    internal MacroDefinition() { }

    /// <summary>
    /// The name of the macro.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The macro's replacement value. May be null.
    /// </summary>
    public string Value { get; set; }
}