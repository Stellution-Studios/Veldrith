namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkVersion.
/// </summary>
internal struct VkVersion {

    /// <summary>
    /// Stores the value state used by this instance.
    /// </summary>
    private readonly uint _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkVersion" /> type.
    /// </summary>
    /// <param name="major">The major value used by this operation.</param>
    /// <param name="minor">The minor value used by this operation.</param>
    /// <param name="patch">The patch value used by this operation.</param>
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
    /// Executes the uint logic for this backend.
    /// </summary>
    /// <param name="version">The version value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator uint(VkVersion version) {
        return version._value;
    }
}
