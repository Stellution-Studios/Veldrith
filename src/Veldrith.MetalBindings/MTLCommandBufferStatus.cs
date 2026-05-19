namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLCommandBufferStatus enum.
/// </summary>
public enum MTLCommandBufferStatus {

    /// <summary>
    /// Represents the NotEnqueued field.
    /// </summary>
    NotEnqueued = 0, Enqueued = 1, Committed = 2, Scheduled = 3, Completed = 4, Error = 5
}