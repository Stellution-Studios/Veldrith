namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CVTimeStamp data structure used by the graphics runtime.
/// </summary>
public struct CVTimeStamp {

    /// <summary>
    /// Stores the flags state used by this instance.
    /// </summary>
    public ulong Flags;

    /// <summary>
    /// Stores the host time state used by this instance.
    /// </summary>
    public ulong HostTime;

    /// <summary>
    /// Stores the rate scalar state used by this instance.
    /// </summary>
    public double RateScalar;

    /// <summary>
    /// Stores the reserved state used by this instance.
    /// </summary>
    public ulong Reserved;

    /// <summary>
    /// Stores the smpte time state used by this instance.
    /// </summary>
    public CVSMPTETime SmpteTime;

    /// <summary>
    /// Stores the version state used by this instance.
    /// </summary>
    public uint Version;

    /// <summary>
    /// Stores the video refresh period state used by this instance.
    /// </summary>
    public long VideoRefreshPeriod;

    /// <summary>
    /// Stores the video time state used by this instance.
    /// </summary>
    public long VideoTime;

    /// <summary>
    /// Stores the video time scale state used by this instance.
    /// </summary>
    public int VideoTimeScale;
}