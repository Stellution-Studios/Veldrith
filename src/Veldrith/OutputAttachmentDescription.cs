using System;

namespace Veldrith;

/// <summary>
/// Describes an individual output attachment and its format.
/// </summary>
public struct OutputAttachmentDescription : IEquatable<OutputAttachmentDescription> {

    /// <summary>
    /// The format of the <see cref="Texture" /> attachment.
    /// </summary>
    public PixelFormat Format;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputAttachmentDescription" /> type.
    /// </summary>
    /// <param name="format">The format used by this operation.</param>
    public OutputAttachmentDescription(PixelFormat format) {
        this.Format = format;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(OutputAttachmentDescription other) {
        return this.Format == other.Format;
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return (int)this.Format;
    }
}