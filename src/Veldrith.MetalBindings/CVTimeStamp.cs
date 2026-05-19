namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CVTimeStamp data structure used by the graphics runtime.
/// </summary>
public struct CVTimeStamp {

    /// <summary>
    /// Stores the flags state used by this instance.
    /// </summary>
    public ulong flags;

    /// <summary>
    /// Stores the host time state used by this instance.
    /// </summary>
    public ulong hostTime;

    /// <summary>
    /// Stores the rate scalar state used by this instance.
    /// </summary>
    public double rateScalar;

    /// <summary>
    /// Stores the reserved state used by this instance.
    /// </summary>
    public ulong reserved;

    /// <summary>
    /// Stores the smpte time state used by this instance.
    /// </summary>
    public CVSMPTETime smpteTime;

    /// <summary>
    /// Stores the version state used by this instance.
    /// </summary>
    public uint version;

    /// <summary>
    /// Stores the video refresh period state used by this instance.
    /// </summary>
    public long videoRefreshPeriod;

    /// <summary>
    /// Stores the video time state used by this instance.
    /// </summary>
    public long videoTime;

    /// <summary>
    /// Stores the video time scale state used by this instance.
    /// </summary>
    public int videoTimeScale;
}