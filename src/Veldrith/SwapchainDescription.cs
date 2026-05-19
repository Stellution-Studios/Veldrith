using System;

namespace Veldrith;

/// <summary>
/// Describes a <see cref="Swapchain" />, for creation via a <see cref="ResourceFactory" />.
/// </summary>
public struct SwapchainDescription : IEquatable<SwapchainDescription> {

    /// <summary>
    /// The <see cref="SwapchainSource" /> which will be used as the target of rendering operations.
    /// This is a window-system-specific object which differs by platform.
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
    /// If non-null, this must be a valid depth Texture format.
    /// If null, then no depth target will be created.
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
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depthFormat">Specifies the value of <paramref name="depthFormat" />.</param>
    /// <param name="syncToVerticalBlank">Specifies the value of <paramref name="syncToVerticalBlank" />.</param>
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
    /// <param name="source">Specifies the value of <paramref name="source" />.</param>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depthFormat">Specifies the value of <paramref name="depthFormat" />.</param>
    /// <param name="syncToVerticalBlank">Specifies the value of <paramref name="syncToVerticalBlank" />.</param>
    /// <param name="colorSrgb">Specifies the value of <paramref name="colorSrgb" />.</param>
    public SwapchainDescription(SwapchainSource source, uint width, uint height, PixelFormat? depthFormat, bool syncToVerticalBlank, bool colorSrgb) {
        this.Source = source;
        this.Width = width;
        this.Height = height;
        this.DepthFormat = depthFormat;
        this.SyncToVerticalBlank = syncToVerticalBlank;
        this.ColorSrgb = colorSrgb;
    }

    /// <summary>
    /// Executes the Equals operation.
    /// </summary>
    /// <param name="other">Specifies the value of <paramref name="other" />.</param>
    /// <returns>Returns the result produced by the Equals operation.</returns>
    public bool Equals(SwapchainDescription other) {
        return this.Source.Equals(other.Source)
               && this.Width.Equals(other.Width)
               && this.Height.Equals(other.Height)
               && this.DepthFormat == other.DepthFormat
               && this.SyncToVerticalBlank.Equals(other.SyncToVerticalBlank)
               && this.ColorSrgb.Equals(other.ColorSrgb);
    }

    /// <summary>
    /// Executes the GetHashCode operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetHashCode operation.</returns>
    public override int GetHashCode() {
        return HashHelper.Combine(this.Source.GetHashCode(), this.Width.GetHashCode(), this.Height.GetHashCode(), this.DepthFormat.GetHashCode(), this.SyncToVerticalBlank.GetHashCode(), this.ColorSrgb.GetHashCode());
    }
}