namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlUnalignedBufferCopyInfo.
/// </summary>
internal struct MtlUnalignedBufferCopyInfo {

    /// <summary>
    /// Stores the source offset value used during command execution.
    /// </summary>
    public uint SourceOffset;

    /// <summary>
    /// Stores the destination offset value used during command execution.
    /// </summary>
    public uint DestinationOffset;

    /// <summary>
    /// Stores the copy size value used during command execution.
    /// </summary>
    public uint CopySize;
}