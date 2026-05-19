using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the NSRange struct.
/// </summary>
public struct NSRange {

    /// <summary>
    /// Stores the value associated with <c>location</c>.
    /// </summary>
    public UIntPtr location;

    /// <summary>
    /// Stores the value associated with <c>length</c>.
    /// </summary>
    public UIntPtr length;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSRange" /> type.
    /// </summary>
    /// <param name="location">Specifies the value of <paramref name="location" />.</param>
    /// <param name="length">Specifies the value of <paramref name="length" />.</param>
    public NSRange(UIntPtr location, UIntPtr length) {
        this.location = location;
        this.length = length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NSRange" /> type.
    /// </summary>
    /// <param name="location">Specifies the value of <paramref name="location" />.</param>
    /// <param name="length">Specifies the value of <paramref name="length" />.</param>
    public NSRange(uint location, uint length) {
        this.location = location;
        this.length = length;
    }
}