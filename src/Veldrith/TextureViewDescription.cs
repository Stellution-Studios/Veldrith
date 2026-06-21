using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="TextureView" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct TextureViewDescription : IEquatable<TextureViewDescription> {

    /// <summary>
    /// The desired target <see cref="Texture" />.
    /// </summary>
    public Texture Target;

    /// <summary>
    /// The base mip level visible in the view. Must be less than <see cref="Texture.MipLevels" />.
    /// </summary>
    public uint BaseMipLevel;

    /// <summary>
    /// The number of mip levels visible in the view.
    /// </summary>
    public uint MipLevels;

    /// <summary>
    /// The base array layer visible in the view.
    /// </summary>
    public uint BaseArrayLayer;

    /// <summary>
    /// The number of array layers visible in the view.
    /// </summary>
    public uint ArrayLayers;

    /// <summary>
    /// An optional <see cref="PixelFormat" /> which specifies how the data within <see cref="Target" /> will be viewed.
    /// </summary>
    public PixelFormat? Format;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureViewDescription" /> type.
    /// </summary>
    /// <param name="target">The target value used by this operation.</param>
    public TextureViewDescription(Texture target) {
        this.Target = target;
        this.BaseMipLevel = 0;
        this.MipLevels = target.MipLevels;
        this.BaseArrayLayer = 0;
        this.ArrayLayers = target.ArrayLayers;
        this.Format = target.Format;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureViewDescription" /> type.
    /// </summary>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    public TextureViewDescription(Texture target, PixelFormat format) {
        this.Target = target;
        this.BaseMipLevel = 0;
        this.MipLevels = target.MipLevels;
        this.BaseArrayLayer = 0;
        this.ArrayLayers = target.ArrayLayers;
        this.Format = format;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureViewDescription" /> type.
    /// </summary>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="baseMipLevel">The base mip level value used by this operation.</param>
    /// <param name="mipLevels">The mip levels value used by this operation.</param>
    /// <param name="baseArrayLayer">The base array layer value used by this operation.</param>
    /// <param name="arrayLayers">The array layers value used by this operation.</param>
    public TextureViewDescription(Texture target, uint baseMipLevel, uint mipLevels, uint baseArrayLayer, uint arrayLayers) {
        this.Target = target;
        this.BaseMipLevel = baseMipLevel;
        this.MipLevels = mipLevels;
        this.BaseArrayLayer = baseArrayLayer;
        this.ArrayLayers = arrayLayers;
        this.Format = target.Format;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureViewDescription" /> type.
    /// </summary>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="baseMipLevel">The base mip level value used by this operation.</param>
    /// <param name="mipLevels">The mip levels value used by this operation.</param>
    /// <param name="baseArrayLayer">The base array layer value used by this operation.</param>
    /// <param name="arrayLayers">The array layers value used by this operation.</param>
    public TextureViewDescription(Texture target, PixelFormat format, uint baseMipLevel, uint mipLevels, uint baseArrayLayer, uint arrayLayers) {
        this.Target = target;
        this.BaseMipLevel = baseMipLevel;
        this.MipLevels = mipLevels;
        this.BaseArrayLayer = baseArrayLayer;
        this.ArrayLayers = arrayLayers;
        this.Format = format;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(TextureViewDescription other) {
        return this.Target.Equals(other.Target)
               && this.BaseMipLevel.Equals(other.BaseMipLevel)
               && this.MipLevels.Equals(other.MipLevels)
               && this.BaseArrayLayer.Equals(other.BaseArrayLayer)
               && this.ArrayLayers.Equals(other.ArrayLayers)
               && this.Format == other.Format;
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Target.GetHashCode(), this.BaseMipLevel.GetHashCode(), this.MipLevels.GetHashCode(), this.BaseArrayLayer.GetHashCode(), this.ArrayLayers.GetHashCode(), this.Format?.GetHashCode() ?? 0);
    }
}