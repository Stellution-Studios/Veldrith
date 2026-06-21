using System;

namespace Veldrith;

/// <summary>
/// Describes a single attachment (color or depth) for a <see cref="Framebuffer" />.
/// </summary>
public struct FramebufferAttachmentDescription : IEquatable<FramebufferAttachmentDescription> {

    /// <summary>
    /// The target texture to render into. For color attachments, this resource must have been created with the
    /// </summary>
    public Texture Target;

    /// <summary>
    /// The array layer to render to. This value must be less than <see cref="Texture.ArrayLayers" /> in the target
    /// </summary>
    public uint ArrayLayer;

    /// <summary>
    /// The mip level to render to. This value must be less than <see cref="Texture.MipLevels" /> in the target
    /// </summary>
    public uint MipLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="FramebufferAttachmentDescription" /> type.
    /// </summary>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    public FramebufferAttachmentDescription(Texture target, uint arrayLayer)
        : this(target, arrayLayer, 0) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FramebufferAttachmentDescription" /> type.
    /// </summary>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    /// <param name="mipLevel">The mip level index.</param>
    public FramebufferAttachmentDescription(Texture target, uint arrayLayer, uint mipLevel) {
#if VALIDATE_USAGE
        uint effectiveArrayLayers = target.ArrayLayers;
        if ((target.Usage & TextureUsage.Cubemap) != 0) {
            effectiveArrayLayers *= 6;
        }

        if (arrayLayer >= effectiveArrayLayers) {
            throw new VeldridException($"{nameof(arrayLayer)} must be less than {nameof(target)}.{nameof(Texture.ArrayLayers)}.");
        }

        if (mipLevel >= target.MipLevels) {
            throw new VeldridException($"{nameof(mipLevel)} must be less than {nameof(target)}.{nameof(Texture.MipLevels)}.");
        }
#endif
        this.Target = target;
        this.ArrayLayer = arrayLayer;
        this.MipLevel = mipLevel;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(FramebufferAttachmentDescription other) {
        return this.Target.Equals(other.Target) && this.ArrayLayer.Equals(other.ArrayLayer) && this.MipLevel.Equals(other.MipLevel);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Target.GetHashCode(), this.ArrayLayer.GetHashCode(), this.MipLevel.GetHashCode());
    }
}