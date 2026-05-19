using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSRange struct.
/// </summary>
public struct NSRange {

    /// <summary>
    /// Represents the location field.
    /// </summary>
    public UIntPtr location;

    /// <summary>
    /// Represents the length field.
    /// </summary>
    public UIntPtr length;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSRange" /> class.
    /// </summary>
    public NSRange(UIntPtr location, UIntPtr length) {
        this.location = location;
        this.length = length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NSRange" /> class.
    /// </summary>
    public NSRange(uint location, uint length) {
        this.location = location;
        this.length = length;
    }
}