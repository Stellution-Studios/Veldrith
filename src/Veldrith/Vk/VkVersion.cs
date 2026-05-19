namespace Veldrith.Vk;

/// <summary>
/// Represents the VkVersion struct.
/// </summary>
internal struct VkVersion {

    /// <summary>
    /// Represents the _value field.
    /// </summary>
    private readonly uint _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkVersion" /> type.
    /// </summary>
    /// <param name="major">The value of major.</param>
    /// <param name="minor">The value of minor.</param>
    /// <param name="patch">The value of patch.</param>
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
    /// Performs the operator uint operation.
    /// </summary>
    /// <param name="version">The value of version.</param>
    /// <returns>The result of the operator uint operation.</returns>
    public static implicit operator uint(VkVersion version) {
        return version._value;
    }
}