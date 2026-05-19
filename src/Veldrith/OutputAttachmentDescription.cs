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
    /// <param name="format">The value of format.</param>
    public OutputAttachmentDescription(PixelFormat format) {
        this.Format = format;
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(OutputAttachmentDescription other) {
        return this.Format == other.Format;
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return (int)this.Format;
    }
}