namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CVSMPTETime struct.
/// </summary>
public struct CVSMPTETime {

    /// <summary>
    /// Represents the flags field.
    /// </summary>
    public ulong flags;

    /// <summary>
    /// Represents the hostTime field.
    /// </summary>
    public ulong hostTime;

    /// <summary>
    /// Represents the rateScalar field.
    /// </summary>
    public double rateScalar;

    /// <summary>
    /// Represents the reserved field.
    /// </summary>
    public ulong reserved;

    /// <summary>
    /// Represents the version field.
    /// </summary>
    public uint version;

    /// <summary>
    /// Represents the videoRefreshPeriod field.
    /// </summary>
    public long videoRefreshPeriod;

    /// <summary>
    /// Represents the videoTime field.
    /// </summary>
    public long videoTime;

    /// <summary>
    /// Represents the videoTimeScale field.
    /// </summary>
    public int videoTimeScale;
}