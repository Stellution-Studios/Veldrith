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
    /// <param name="depthAttachment">The value of depthAttachment.</param>
    /// <param name="colorAttachments">The value of colorAttachments.</param>
    public OutputDescription(OutputAttachmentDescription? depthAttachment, params OutputAttachmentDescription[] colorAttachments) {
        this.DepthAttachment = depthAttachment;
        this.ColorAttachments = colorAttachments ?? Array.Empty<OutputAttachmentDescription>();
        this.SampleCount = TextureSampleCount.Count1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputDescription" /> type.
    /// </summary>
    /// <param name="depthAttachment">The value of depthAttachment.</param>
    /// <param name="colorAttachments">The value of colorAttachments.</param>
    /// <param name="sampleCount">The value of sampleCount.</param>
    public OutputDescription(OutputAttachmentDescription? depthAttachment, OutputAttachmentDescription[] colorAttachments, TextureSampleCount sampleCount) {
        this.DepthAttachment = depthAttachment;
        this.ColorAttachments = colorAttachments ?? Array.Empty<OutputAttachmentDescription>();
        this.SampleCount = sampleCount;
    }

    /// <summary>
    /// Performs the CreateFromFramebuffer operation.
    /// </summary>
    /// <param name="fb">The value of fb.</param>
    /// <returns>The result of the CreateFromFramebuffer operation.</returns>
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
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
    public bool Equals(OutputDescription other) {
        return this.DepthAttachment.GetValueOrDefault().Equals(other.DepthAttachment.GetValueOrDefault())
               && Util.ArrayEqualsEquatable(this.ColorAttachments, other.ColorAttachments)
               && this.SampleCount == other.SampleCount;
    }

    /// <summary>
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.DepthAttachment.GetHashCode(), HashHelper.Array(this.ColorAttachments), (int)this.SampleCount);
    }
}