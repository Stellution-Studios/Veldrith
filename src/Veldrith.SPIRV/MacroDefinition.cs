namespace Veldrith.SPIRV;

public class MacroDefinition {

    /// <summary>
    /// Constructs a new <see cref="MacroDefinition" /> with no value.
    /// </summary>
    public MacroDefinition(string name) {
        this.Name = name;
    }

    /// <summary>
    /// Constructs a new <see cref="MacroDefinition" /> with a value.
    /// </summary>
    public MacroDefinition(string name, string value) {
        this.Name = name;
        this.Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MacroDefinition" /> class.
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