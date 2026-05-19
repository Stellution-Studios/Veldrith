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
    /// If this value is null, then the created TextureView will use the same <see cref="PixelFormat" /> as the target
    /// <see cref="Texture" />. If not null, this format must be "compatible" with the target Texture's. For uncompressed
    /// formats, the overall size and number of components in this format must be equal to the underlying format. For
    /// compressed formats, it is only possible to use the same PixelFormat or its sRGB/non-sRGB counterpart.
    /// </summary>
    public PixelFormat? Format;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureViewDescription" /> type.
    /// </summary>
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
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
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
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
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <param name="baseMipLevel">Specifies the value of <paramref name="baseMipLevel" />.</param>
    /// <param name="mipLevels">Specifies the value of <paramref name="mipLevels" />.</param>
    /// <param name="baseArrayLayer">Specifies the value of <paramref name="baseArrayLayer" />.</param>
    /// <param name="arrayLayers">Specifies the value of <paramref name="arrayLayers" />.</param>
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
    /// <param name="target">Specifies the value of <paramref name="target" />.</param>
    /// <param name="format">Specifies the value of <paramref name="format" />.</param>
    /// <param name="baseMipLevel">Specifies the value of <paramref name="baseMipLevel" />.</param>
    /// <param name="mipLevels">Specifies the value of <paramref name="mipLevels" />.</param>
    /// <param name="baseArrayLayer">Specifies the value of <paramref name="baseArrayLayer" />.</param>
    /// <param name="arrayLayers">Specifies the value of <paramref name="arrayLayers" />.</param>
    public TextureViewDescription(Texture target, PixelFormat format, uint baseMipLevel, uint mipLevels, uint baseArrayLayer, uint arrayLayers) {
        this.Target = target;
        this.BaseMipLevel = baseMipLevel;
        this.MipLevels = mipLevels;
        this.BaseArrayLayer = baseArrayLayer;
        this.ArrayLayers = arrayLayers;
        this.Format = target.Format;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(TextureViewDescription other) {
        return this.Target.Equals(other.Target)
               && this.BaseMipLevel.Equals(other.BaseMipLevel)
               && this.MipLevels.Equals(other.MipLevels)
               && this.BaseArrayLayer.Equals(other.BaseArrayLayer)
               && this.ArrayLayers.Equals(other.ArrayLayers)
               && this.Format == other.Format;
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Target.GetHashCode(), this.BaseMipLevel.GetHashCode(), this.MipLevels.GetHashCode(), this.BaseArrayLayer.GetHashCode(), this.ArrayLayers.GetHashCode(), this.Format?.GetHashCode() ?? 0);
    }
}