using System;

namespace Veldrith;

/// <summary>
/// Describes a single attachment (color or depth) for a <see cref="Framebuffer" />.
/// </summary>
public struct FramebufferAttachmentDescription : IEquatable<FramebufferAttachmentDescription> {

    /// <summary>
    /// The target texture to render into. For color attachments, this resource must have been created with the
    /// <see cref="TextureUsage.RenderTarget" /> flag. For depth attachments, this resource must have been created with the
    /// <see cref="TextureUsage.DepthStencil" /> flag.
    /// </summary>
    public Texture Target;

    /// <summary>
    /// The array layer to render to. This value must be less than <see cref="Texture.ArrayLayers" /> in the target
    /// <see cref="Texture" />.
    /// </summary>
    public uint ArrayLayer;

    /// <summary>
    /// The mip level to render to. This value must be less than <see cref="Texture.MipLevels" /> in the target
    /// <see cref="Texture" />.
    /// </summary>
    public uint MipLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="FramebufferAttachmentDescription" /> type.
    /// </summary>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    public FramebufferAttachmentDescription(Texture target, uint arrayLayer)
        : this(target, arrayLayer, 0) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FramebufferAttachmentDescription" /> type.
    /// </summary>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <param name="arrayLayer">Specifies the value of <paramref name="arrayLayer" />.</param>
    /// <param name="mipLevel">Specifies the value of <paramref name="mipLevel" />.</param>
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
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(FramebufferAttachmentDescription other) {
        return this.Target.Equals(other.Target) && this.ArrayLayer.Equals(other.ArrayLayer) && this.MipLevel.Equals(other.MipLevel);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Target.GetHashCode(), this.ArrayLayer.GetHashCode(), this.MipLevel.GetHashCode());
    }
}