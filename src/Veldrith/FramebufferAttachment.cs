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
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    public FramebufferAttachment(Texture target, uint arrayLayer) {
        this.Target = target;
        this.ArrayLayer = arrayLayer;
        this.MipLevel = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FramebufferAttachment" /> type.
    /// </summary>
    /// <param name="target">The target value used by this operation.</param>
    /// <param name="arrayLayer">The array layer index.</param>
    /// <param name="mipLevel">The mip level index.</param>
    public FramebufferAttachment(Texture target, uint arrayLayer, uint mipLevel) {
        this.Target = target;
        this.ArrayLayer = arrayLayer;
        this.MipLevel = mipLevel;
    }
}