namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the available values of the MTLCommandBufferStatus enumeration.
/// </summary>
public enum MTLCommandBufferStatus {

    /// <summary>
    /// Stores the not enqueued state used by this instance.
    /// </summary>
    NotEnqueued = 0, Enqueued = 1, Committed = 2, Scheduled = 3, Completed = 4, Error = 5
}