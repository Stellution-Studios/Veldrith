using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="Texture" />, for creation using a <see cref="ResourceFactory" />.
/// </summary>
public struct TextureDescription : IEquatable<TextureDescription> {

    /// <summary>
    /// The total width, in texels.
    /// </summary>
    public uint Width;

    /// <summary>
    /// The total height, in texels.
    /// </summary>
    public uint Height;

    /// <summary>
    /// The total depth, in texels.
    /// </summary>
    public uint Depth;

    /// <summary>
    /// The number of mipmap levels.
    /// </summary>
    public uint MipLevels;

    /// <summary>
    /// The number of array layers.
    /// </summary>
    public uint ArrayLayers;

    /// <summary>
    /// The format of individual texture elements.
    /// </summary>
    public PixelFormat Format;

    /// <summary>
    /// Controls how the Texture is permitted to be used. If the Texture will be sampled from a shader, then
    /// </summary>
    public TextureUsage Usage;

    /// <summary>
    /// The type of Texture to create.
    /// </summary>
    public TextureType Type;

    /// <summary>
    /// The number of samples. If equal to <see cref="TextureSampleCount.Count1" />, this instance does not describe a
    /// </summary>
    public TextureSampleCount SampleCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureDescription" /> type.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevels">The mip levels value used by this operation.</param>
    /// <param name="arrayLayers">The array layers value used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="type">The type value used by this operation.</param>
    public TextureDescription(uint width, uint height, uint depth, uint mipLevels, uint arrayLayers, PixelFormat format, TextureUsage usage, TextureType type) {
        this.Width = width;
        this.Height = height;
        this.Depth = depth;
        this.MipLevels = mipLevels;
        this.ArrayLayers = arrayLayers;
        this.Format = format;
        this.Usage = usage;
        this.SampleCount = TextureSampleCount.Count1;
        this.Type = type;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureDescription" /> type.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevels">The mip levels value used by this operation.</param>
    /// <param name="arrayLayers">The array layers value used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="type">The type value used by this operation.</param>
    /// <param name="sampleCount">The sample count value used by this operation.</param>
    public TextureDescription(uint width, uint height, uint depth, uint mipLevels, uint arrayLayers, PixelFormat format, TextureUsage usage, TextureType type, TextureSampleCount sampleCount) {
        this.Width = width;
        this.Height = height;
        this.Depth = depth;
        this.MipLevels = mipLevels;
        this.ArrayLayers = arrayLayers;
        this.Format = format;
        this.Usage = usage;
        this.Type = type;
        this.SampleCount = sampleCount;
    }

    /// <summary>
    /// Executes the texture1 d logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="mipLevels">The mip levels value used by this operation.</param>
    /// <param name="arrayLayers">The array layers value used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static TextureDescription Texture1D(uint width, uint mipLevels, uint arrayLayers, PixelFormat format, TextureUsage usage) {
        return new TextureDescription(width, 1, 1, mipLevels, arrayLayers, format, usage, TextureType.Texture1D, TextureSampleCount.Count1);
    }

    /// <summary>
    /// Executes the texture2 d logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="mipLevels">The mip levels value used by this operation.</param>
    /// <param name="arrayLayers">The array layers value used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static TextureDescription Texture2D(uint width, uint height, uint mipLevels, uint arrayLayers, PixelFormat format, TextureUsage usage) {
        return new TextureDescription(width, height, 1, mipLevels, arrayLayers, format, usage, TextureType.Texture2D, TextureSampleCount.Count1);
    }

    /// <summary>
    /// Executes the texture2 d logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="mipLevels">The mip levels value used by this operation.</param>
    /// <param name="arrayLayers">The array layers value used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <param name="sampleCount">The sample count value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static TextureDescription Texture2D(uint width, uint height, uint mipLevels, uint arrayLayers, PixelFormat format, TextureUsage usage, TextureSampleCount sampleCount) {
        return new TextureDescription(width, height, 1, mipLevels, arrayLayers, format, usage, TextureType.Texture2D, sampleCount);
    }

    /// <summary>
    /// Executes the texture3 d logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depth">The depth value.</param>
    /// <param name="mipLevels">The mip levels value used by this operation.</param>
    /// <param name="format">The format used by this operation.</param>
    /// <param name="usage">The usage value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static TextureDescription Texture3D(uint width, uint height, uint depth, uint mipLevels, PixelFormat format, TextureUsage usage) {
        return new TextureDescription(width, height, depth, mipLevels, 1, format, usage, TextureType.Texture3D, TextureSampleCount.Count1);
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(TextureDescription other) {
        return this.Width.Equals(other.Width)
               && this.Height.Equals(other.Height)
               && this.Depth.Equals(other.Depth)
               && this.MipLevels.Equals(other.MipLevels)
               && this.ArrayLayers.Equals(other.ArrayLayers)
               && this.Format == other.Format
               && this.Usage == other.Usage
               && this.Type == other.Type
               && this.SampleCount == other.SampleCount;
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Width.GetHashCode(), this.Height.GetHashCode(), this.Depth.GetHashCode(), this.MipLevels.GetHashCode(), this.ArrayLayers.GetHashCode(), (int)this.Format, (int)this.Usage, (int)this.Type, (int)this.SampleCount);
    }
}