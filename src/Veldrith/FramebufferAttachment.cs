namespace Veldrith;

/// <summary>
/// Represents a single output of a <see cref="Framebuffer" />. May be a color or depth attachment.
/// </summary>
public struct FramebufferAttachment {

    /// <summary>
    /// The target <see cref="Texture" /> which will be rendered to.
    /// </summary>
    public Texture Target { get; }

    /// <summary>
    /// The target array layer.
    /// </summary>
    public uint ArrayLayer { get; }

    /// <summary>
    /// The target mip level.
    /// </summary>
    public uint MipLevel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FramebufferAttachment" /> type.
    /// </summary>
    /// <param name="target">The value of target.</param>
    /// <param name="arrayLayer">The value of arrayLayer.</param>
    public FramebufferAttachment(Texture target, uint arrayLayer) {
        this.Target = target;
        this.ArrayLayer = arrayLayer;
        this.MipLevel = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FramebufferAttachment" /> type.
    /// </summary>
    /// <param name="target">The value of target.</param>
    /// <param name="arrayLayer">The value of arrayLayer.</param>
    /// <param name="mipLevel">The value of mipLevel.</param>
    public FramebufferAttachment(Texture target, uint arrayLayer, uint mipLevel) {
        this.Target = target;
        this.ArrayLayer = arrayLayer;
        this.MipLevel = mipLevel;
    }
}