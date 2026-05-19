using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="Framebuffer" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct FramebufferDescription : IEquatable<FramebufferDescription> {

    /// <summary>
    /// The depth texture, which must have been created with <see cref="TextureUsage.DepthStencil" /> usage flags.
    /// May be null.
    /// </summary>
    public FramebufferAttachmentDescription? DepthTarget;

    /// <summary>
    /// An array of color textures, all of which must have been created with <see cref="TextureUsage.RenderTarget" />
    /// usage flags. May be null or empty.
    /// </summary>
    public FramebufferAttachmentDescription[] ColorTargets;

    /// <summary>
    /// Constructs a new <see cref="FramebufferDescription" />.
    /// </summary>
    /// <param name="depthTarget">
    /// The depth texture, which must have been created with
    /// <see cref="TextureUsage.DepthStencil" /> usage flags. May be null.
    /// </param>
    /// <param name="colorTargets">
    /// An array of color textures, all of which must have been created with
    /// <see cref="TextureUsage.RenderTarget" /> usage flags. May be null or empty.
    /// </param>
    public FramebufferDescription(Texture depthTarget, params Texture[] colorTargets) {
        if (depthTarget != null) {
            this.DepthTarget = new FramebufferAttachmentDescription(depthTarget, 0);
        }
        else {
            this.DepthTarget = null;
        }

        this.ColorTargets = new FramebufferAttachmentDescription[colorTargets.Length];
        for (int i = 0; i < colorTargets.Length; i++) {
            this.ColorTargets[i] = new FramebufferAttachmentDescription(colorTargets[i], 0);
        }
    }

    /// <summary>
    /// Constructs a new <see cref="FramebufferDescription" />.
    /// </summary>
    /// <param name="depthTarget">A description of the depth attachment. May be null if no depth attachment will be used.</param>
    /// <param name="colorTargets">
    /// An array of descriptions of color attachments. May be empty if no color attachments will
    /// be used.
    /// </param>
    public FramebufferDescription(FramebufferAttachmentDescription? depthTarget, FramebufferAttachmentDescription[] colorTargets) {
        this.DepthTarget = depthTarget;
        this.ColorTargets = colorTargets;
    }

    /// <summary>
    /// Element-wise equality.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>True if all elements and all array elements are equal; false otherswise.</returns>
    public bool Equals(FramebufferDescription other) {
        return Util.NullableEquals(this.DepthTarget, other.DepthTarget) && Util.ArrayEqualsEquatable(this.ColorTargets, other.ColorTargets);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.DepthTarget.GetHashCode(), HashHelper.Array(this.ColorTargets));
    }
}