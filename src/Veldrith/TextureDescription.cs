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
    /// <see cref="TextureUsage.Sampled" /> must be included. If the Texture will be used as a depth target in a
    /// <see cref="Framebuffer" />, then <see cref="TextureUsage.DepthStencil" /> must be included. If the Texture will be
    /// used
    /// as a color target in a <see cref="Framebuffer" />, then <see cref="TextureUsage.RenderTarget" /> must be included.
    /// If the Texture will be used as a 2D cubemap, then <see cref="TextureUsage.Cubemap" /> must be included.
    /// </summary>
    public TextureUsage Usage;

    /// <summary>
    /// The type of Texture to create.
    /// </summary>
    public TextureType Type;

    /// <summary>
    /// The number of samples. If equal to <see cref="TextureSampleCount.Count1" />, this instance does not describe a
    /// multisample <see cref="Texture" />.
    /// </summary>
    public TextureSampleCount SampleCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureDescription" /> type.
    /// </summary>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="mipLevels">The value of mipLevels.</param>
    /// <param name="arrayLayers">The value of arrayLayers.</param>
    /// <param name="format">The value of format.</param>
    /// <param name="usage">The value of usage.</param>
    /// <param name="type">The value of type.</param>
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
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="mipLevels">The value of mipLevels.</param>
    /// <param name="arrayLayers">The value of arrayLayers.</param>
    /// <param name="format">The value of format.</param>
    /// <param name="usage">The value of usage.</param>
    /// <param name="type">The value of type.</param>
    /// <param name="sampleCount">The value of sampleCount.</param>
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
    /// Performs the Texture1D operation.
    /// </summary>
    /// <param name="width">The value of width.</param>
    /// <param name="mipLevels">The value of mipLevels.</param>
    /// <param name="arrayLayers">The value of arrayLayers.</param>
    /// <param name="format">The value of format.</param>
    /// <param name="usage">The value of usage.</param>
    /// <returns>The result of the Texture1D operation.</returns>
    public static TextureDescription Texture1D(uint width, uint mipLevels, uint arrayLayers, PixelFormat format, TextureUsage usage) {
        return new TextureDescription(width, 1, 1, mipLevels, arrayLayers, format, usage, TextureType.Texture1D, TextureSampleCount.Count1);
    }

    /// <summary>
    /// Performs the Texture2D operation.
    /// </summary>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="mipLevels">The value of mipLevels.</param>
    /// <param name="arrayLayers">The value of arrayLayers.</param>
    /// <param name="format">The value of format.</param>
    /// <param name="usage">The value of usage.</param>
    /// <returns>The result of the Texture2D operation.</returns>
    public static TextureDescription Texture2D(uint width, uint height, uint mipLevels, uint arrayLayers, PixelFormat format, TextureUsage usage) {
        return new TextureDescription(width, height, 1, mipLevels, arrayLayers, format, usage, TextureType.Texture2D, TextureSampleCount.Count1);
    }

    /// <summary>
    /// Performs the Texture2D operation.
    /// </summary>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="mipLevels">The value of mipLevels.</param>
    /// <param name="arrayLayers">The value of arrayLayers.</param>
    /// <param name="format">The value of format.</param>
    /// <param name="usage">The value of usage.</param>
    /// <param name="sampleCount">The value of sampleCount.</param>
    /// <returns>The result of the Texture2D operation.</returns>
    public static TextureDescription Texture2D(uint width, uint height, uint mipLevels, uint arrayLayers, PixelFormat format, TextureUsage usage, TextureSampleCount sampleCount) {
        return new TextureDescription(width, height, 1, mipLevels, arrayLayers, format, usage, TextureType.Texture2D, sampleCount);
    }

    /// <summary>
    /// Performs the Texture3D operation.
    /// </summary>
    /// <param name="width">The value of width.</param>
    /// <param name="height">The value of height.</param>
    /// <param name="depth">The value of depth.</param>
    /// <param name="mipLevels">The value of mipLevels.</param>
    /// <param name="format">The value of format.</param>
    /// <param name="usage">The value of usage.</param>
    /// <returns>The result of the Texture3D operation.</returns>
    public static TextureDescription Texture3D(uint width, uint height, uint depth, uint mipLevels, PixelFormat format, TextureUsage usage) {
        return new TextureDescription(width, height, depth, mipLevels, 1, format, usage, TextureType.Texture3D, TextureSampleCount.Count1);
    }

    /// <summary>
    /// Performs the Equals operation.
    /// </summary>
    /// <param name="other">The value of other.</param>
    /// <returns>The result of the Equals operation.</returns>
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
    /// Performs the GetHashCode operation.
    /// </summary>
    /// <returns>The result of the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Width.GetHashCode(), this.Height.GetHashCode(), this.Depth.GetHashCode(), this.MipLevels.GetHashCode(), this.ArrayLayers.GetHashCode(), (int)this.Format, (int)this.Usage, (int)this.Type, (int)this.SampleCount);
    }
}