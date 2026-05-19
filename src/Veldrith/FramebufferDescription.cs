using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="Framebuffer" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct FramebufferDescription : IEquatable<FramebufferDescription> {

    /// <summary>
    /// The depth texture, which must have been created with <see cref="TextureUsage.DepthStencil" /> usage flags.
    /// </summary>
    public FramebufferAttachmentDescription? DepthTarget;

    /// <summary>
    /// An array of color textures, all of which must have been created with <see cref="TextureUsage.RenderTarget" />
    /// </summary>
    public FramebufferAttachmentDescription[] ColorTargets;

    /// <summary>
    /// Initializes a new instance of the <see cref="FramebufferDescription" /> type.
    /// </summary>
    /// <param name="depthTarget">The depth target value used by this operation.</param>
    /// <param name="colorTargets">The color targets value used by this operation.</param>
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
    /// Initializes a new instance of the <see cref="FramebufferDescription" /> type.
    /// </summary>
    /// <param name="depthTarget">The depth target value used by this operation.</param>
    /// <param name="colorTargets">The color targets value used by this operation.</param>
    public FramebufferDescription(FramebufferAttachmentDescription? depthTarget, FramebufferAttachmentDescription[] colorTargets) {
        this.DepthTarget = depthTarget;
        this.ColorTargets = colorTargets;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(FramebufferDescription other) {
        return Util.NullableEquals(this.DepthTarget, other.DepthTarget) && Util.ArrayEqualsEquatable(this.ColorTargets, other.ColorTargets);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.DepthTarget.GetHashCode(), HashHelper.Array(this.ColorTargets));
    }
}