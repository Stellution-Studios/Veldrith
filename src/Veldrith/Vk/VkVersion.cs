namespace Veldrith.Vk;

/// <summary>
/// Defines the data layout and behavior of the VkVersion struct.
/// </summary>
internal struct VkVersion {

    /// <summary>
    /// Stores the value associated with <c>_value</c>.
    /// </summary>
    private readonly uint _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkVersion" /> type.
    /// </summary>
    /// <param name="major">Specifies the value of <paramref name="major" />.</param>
    /// <param name="minor">Specifies the value of <paramref name="minor" />.</param>
    /// <param name="patch">Specifies the value of <paramref name="patch" />.</param>
    public VkVersion(uint major, uint minor, uint patch) {
        this._value = (major << 22) | (minor << 12) | patch;
    }

    /// <summary>
    /// Gets or sets Major.
    /// </summary>
    public uint Major => this._value >> 22;

    /// <summary>
    /// Gets or sets Minor.
    /// </summary>
    public uint Minor => (this._value >> 12) & 0x3ff;

    /// <summary>
    /// Gets or sets Patch.
    /// </summary>
    public uint Patch => (this._value >> 22) & 0xfff;

    /// <summary>
    /// Executes the operator uint operation.
    /// </summary>
    /// <param name="version">Specifies the value of <paramref name="version" />.</param>
    /// <returns>Returns the result produced by the operator uint operation.</returns>
    public static implicit operator uint(VkVersion version) {
        return version._value;
    }
}