using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="Swapchain" />, for creation via a <see cref="ResourceFactory" />.
/// </summary>
public struct SwapchainDescription : IEquatable<SwapchainDescription> {

    /// <summary>
    /// The <see cref="SwapchainSource" /> which will be used as the target of rendering operations.
    /// </summary>
    public SwapchainSource Source;

    /// <summary>
    /// The initial width of the Swapchain surface.
    /// </summary>
    public uint Width;

    /// <summary>
    /// The initial height of the Swapchain surface.
    /// </summary>
    public uint Height;

    /// <summary>
    /// The optional format of the depth target of the Swapchain's Framebuffer.
    /// </summary>
    public PixelFormat? DepthFormat;

    /// <summary>
    /// Indicates whether presentation of the Swapchain will be synchronized to the window system's vertical refresh rate.
    /// </summary>
    public bool SyncToVerticalBlank;

    /// <summary>
    /// Indicates whether the color target of the Swapchain will use an sRGB PixelFormat.
    /// </summary>
    public bool ColorSrgb;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwapchainDescription" /> type.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depthFormat">The depth format value used by this operation.</param>
    /// <param name="syncToVerticalBlank">The sync to vertical blank value used by this operation.</param>
    public SwapchainDescription(SwapchainSource source, uint width, uint height, PixelFormat? depthFormat, bool syncToVerticalBlank) {
        this.Source = source;
        this.Width = width;
        this.Height = height;
        this.DepthFormat = depthFormat;
        this.SyncToVerticalBlank = syncToVerticalBlank;
        this.ColorSrgb = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwapchainDescription" /> type.
    /// </summary>
    /// <param name="source">The source value or resource.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depthFormat">The depth format value used by this operation.</param>
    /// <param name="syncToVerticalBlank">The sync to vertical blank value used by this operation.</param>
    /// <param name="colorSrgb">The color srgb value used by this operation.</param>
    public SwapchainDescription(SwapchainSource source, uint width, uint height, PixelFormat? depthFormat, bool syncToVerticalBlank, bool colorSrgb) {
        this.Source = source;
        this.Width = width;
        this.Height = height;
        this.DepthFormat = depthFormat;
        this.SyncToVerticalBlank = syncToVerticalBlank;
        this.ColorSrgb = colorSrgb;
    }

    /// <summary>
    /// Determines whether this instance is equal to the specified value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    public bool Equals(SwapchainDescription other) {
        return this.Source.Equals(other.Source)
               && this.Width.Equals(other.Width)
               && this.Height.Equals(other.Height)
               && this.DepthFormat == other.DepthFormat
               && this.SyncToVerticalBlank.Equals(other.SyncToVerticalBlank)
               && this.ColorSrgb.Equals(other.ColorSrgb);
    }

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Source.GetHashCode(), this.Width.GetHashCode(), this.Height.GetHashCode(), this.DepthFormat.GetHashCode(), this.SyncToVerticalBlank.GetHashCode(), this.ColorSrgb.GetHashCode());
    }
}