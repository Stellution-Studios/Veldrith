namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the CVTimeStamp struct.
/// </summary>
public struct CVTimeStamp {

    /// <summary>
    /// Stores the value associated with <c>flags</c>.
    /// </summary>
    public ulong flags;

    /// <summary>
    /// Stores the value associated with <c>hostTime</c>.
    /// </summary>
    public ulong hostTime;

    /// <summary>
    /// Stores the value associated with <c>rateScalar</c>.
    /// </summary>
    public double rateScalar;

    /// <summary>
    /// Stores the value associated with <c>reserved</c>.
    /// </summary>
    public ulong reserved;

    /// <summary>
    /// Stores the value associated with <c>smpteTime</c>.
    /// </summary>
    public CVSMPTETime smpteTime;

    /// <summary>
    /// Stores the value associated with <c>version</c>.
    /// </summary>
    public uint version;

    /// <summary>
    /// Stores the value associated with <c>videoRefreshPeriod</c>.
    /// </summary>
    public long videoRefreshPeriod;

    /// <summary>
    /// Stores the value associated with <c>videoTime</c>.
    /// </summary>
    public long videoTime;

    /// <summary>
    /// Stores the value associated with <c>videoTimeScale</c>.
    /// </summary>
    public int videoTimeScale;
}