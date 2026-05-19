namespace Veldrith.MTL;

/// <summary>
/// Defines the data layout and behavior of the MtlUnalignedBufferCopyInfo struct.
/// </summary>
internal struct MtlUnalignedBufferCopyInfo {

    /// <summary>
    /// Stores the value associated with <c>SourceOffset</c>.
    /// </summary>
    public uint SourceOffset;

    /// <summary>
    /// Stores the value associated with <c>DestinationOffset</c>.
    /// </summary>
    public uint DestinationOffset;

    /// <summary>
    /// Stores the value associated with <c>CopySize</c>.
    /// </summary>
    public uint CopySize;
}