using System;

namespace Veldrith;

/// <summary>
/// Describes a set of output attachments and their formats.
/// </summary>
public struct OutputDescription : IEquatable<OutputDescription> {

    /// <summary>
    /// A description of the depth attachment, or null if none exists.
    /// </summary>
    public OutputAttachmentDescription? DepthAttachment;

    /// <summary>
    /// An array of attachment descriptions, one for each color attachment. May be empty.
    /// </summary>
    public OutputAttachmentDescription[] ColorAttachments;

    /// <summary>
    /// The number of samples in each target attachment.
    /// </summary>
    public TextureSampleCount SampleCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputDescription" /> type.
    /// </summary>
    /// <param name="depthAttachment">The depth attachment value used by this operation.</param>
    /// <param name="colorAttachments">The color attachments value used by this operation.</param>
    public OutputDescription(OutputAttachmentDescription? depthAttachment, params OutputAttachmentDescription[] colorAttachments) {
        this.DepthAttachment = depthAttachment;
        this.ColorAttachments = colorAttachments ?? Array.Empty<OutputAttachmentDescription>();
        this.SampleCount = TextureSampleCount.Count1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputDescription" /> type.
    /// </summary>
    /// <param name="depthAttachment">The depth attachment value used by this operation.</param>
    /// <param name="colorAttachments">The color attachments value used by this operation.</param>
    /// <param name="sampleCount">The sample count value used by this operation.</param>
    public OutputDescription(OutputAttachmentDescription? depthAttachment, OutputAttachmentDescription[] colorAttachments, TextureSampleCount sampleCount) {
        this.DepthAttachment = depthAttachment;
        this.ColorAttachments = colorAttachments ?? Array.Empty<OutputAttachmentDescription>();
        this.SampleCount = sampleCount;
    }

    /// <summary>
    /// Creates the from framebuffer instance used by this backend.
    /// </summary>
    /// <param name="fb">The fb value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static OutputDescription CreateFromFramebuffer(Framebuffer fb) {
        TextureSampleCount sampleCount = 0;
        OutputAttachmentDescription? depthAttachment = null;

        if (fb.DepthTarget != null) {
            depthAttachment = new OutputAttachmentDescription(fb.DepthTarget.Value.Target.Format);
            sampleCount = fb.DepthTarget.Value.Target.SampleCount;
        }

        OutputAttachmentDescription[] colorAttachments = new OutputAttachmentDescription[fb.ColorTargets.Count];

        for (int i = 0; i < colorAttachments.Length; i++) {
            colorAttachments[i] = new OutputAttachmentDescription(fb.ColorTargets[i].Target.Format);
            sampleCount = fb.ColorTargets[i].Target.SampleCount;
        }

        return new OutputDescription(depthAttachment, colorAttachments, sampleCount);
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(OutputDescription other) {
        return this.DepthAttachment.GetValueOrDefault().Equals(other.DepthAttachment.GetValueOrDefault())
               && Util.ArrayEqualsEquatable(this.ColorAttachments, other.ColorAttachments)
               && this.SampleCount == other.SampleCount;
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.DepthAttachment.GetHashCode(), HashHelper.Array(this.ColorAttachments), (int)this.SampleCount);
    }
}