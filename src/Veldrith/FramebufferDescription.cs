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
    /// Initializes a new instance of the <see cref="FramebufferDescription" /> type.
    /// </summary>
    /// <param name="depthTarget">Specifies the value of <paramref name="depthTarget" />.</param>
    /// <param name="colorTargets">Specifies the value of <paramref name="colorTargets" />.</param>
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
    /// <param name="depthTarget">Specifies the value of <paramref name="depthTarget" />.</param>
    /// <param name="colorTargets">Specifies the value of <paramref name="colorTargets" />.</param>
    public FramebufferDescription(FramebufferAttachmentDescription? depthTarget, FramebufferAttachmentDescription[] colorTargets) {
        this.DepthTarget = depthTarget;
        this.ColorTargets = colorTargets;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(FramebufferDescription other) {
        return Util.NullableEquals(this.DepthTarget, other.DepthTarget) && Util.ArrayEqualsEquatable(this.ColorTargets, other.ColorTargets);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.DepthTarget.GetHashCode(), HashHelper.Array(this.ColorTargets));
    }
}