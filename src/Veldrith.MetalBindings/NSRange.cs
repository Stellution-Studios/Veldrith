using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSRange data structure used by the graphics runtime.
/// </summary>
public struct NSRange {

    /// <summary>
    /// Stores the location state used by this instance.
    /// </summary>
    public UIntPtr location;

    /// <summary>
    /// Stores the length state used by this instance.
    /// </summary>
    public UIntPtr length;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSRange" /> type.
    /// </summary>
    /// <param name="location">The location value used by this operation.</param>
    /// <param name="length">The number of items involved in this operation.</param>
    public NSRange(UIntPtr location, UIntPtr length) {
        this.location = location;
        this.length = length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NSRange" /> type.
    /// </summary>
    /// <param name="location">The location value used by this operation.</param>
    /// <param name="length">The number of items involved in this operation.</param>
    public NSRange(uint location, uint length) {
        this.location = location;
        this.length = length;
    }
}