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
    /// Initializes a new instance of the <see cref="VkVersion" /> class.
    /// </summary>
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
    /// Executes uint.
    /// </summary>
    public static implicit operator uint(VkVersion version) {
        return version._value;
    }
}