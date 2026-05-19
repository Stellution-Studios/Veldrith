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
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    public OutputAttachmentDescription(PixelFormat format) {
        this.Format = format;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(OutputAttachmentDescription other) {
        return this.Format == other.Format;
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return (int)this.Format;
    }
}