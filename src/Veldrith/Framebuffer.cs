using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Veldrith;

/// <summary>
/// Represents the Framebuffer type used by the graphics runtime.
/// </summary>
public abstract class Framebuffer : IDeviceResource, IDisposable {

    /// <summary>
    /// Initializes a new instance of the <see cref="Framebuffer" /> type.
    /// </summary>
    internal Framebuffer() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Framebuffer" /> type.
    /// </summary>
    /// <param name="depthTargetDesc">The depth target desc value used by this operation.</param>
    /// <param name="colorTargetDescs">The color target descs value used by this operation.</param>
    internal Framebuffer(FramebufferAttachmentDescription? depthTargetDesc, IReadOnlyList<FramebufferAttachmentDescription> colorTargetDescs) {

        if (depthTargetDesc != null) {
            FramebufferAttachmentDescription depthAttachment = depthTargetDesc.Value;
            this.DepthTarget = new FramebufferAttachment(depthAttachment.Target, depthAttachment.ArrayLayer, depthAttachment.MipLevel);
        }

        FramebufferAttachment[] colorTargets = new FramebufferAttachment[colorTargetDescs.Count];

        for (int i = 0; i < colorTargets.Length; i++) {
            colorTargets[i] = new FramebufferAttachment(colorTargetDescs[i].Target, colorTargetDescs[i].ArrayLayer, colorTargetDescs[i].MipLevel);
        }

        this.ColorTargets = colorTargets;

        Texture dimTex;
        uint mipLevel;

        if (this.ColorTargets.Count > 0) {
            dimTex = this.ColorTargets[0].Target;
            mipLevel = this.ColorTargets[0].MipLevel;
        }
        else {
            Debug.Assert(this.DepthTarget != null);
            dimTex = this.DepthTarget.Value.Target;
            mipLevel = this.DepthTarget.Value.MipLevel;
        }

        Util.GetMipDimensions(dimTex, mipLevel, out uint mipWidth, out uint mipHeight, out _);
        this.Width = mipWidth;
        this.Height = mipHeight;

        this.OutputDescription = OutputDescription.CreateFromFramebuffer(this);
    }

    /// <summary>
    /// Gets the depth attachment associated with this instance. May be null if no depth texture is used.
    /// </summary>
    public virtual FramebufferAttachment? DepthTarget { get; }

    /// <summary>
    /// Gets the collection of color attachments associated with this instance. May be empty.
    /// </summary>
    public virtual IReadOnlyList<FramebufferAttachment> ColorTargets { get; }

    /// <summary>
    /// Gets an <see cref="Veldrith.OutputDescription" /> which describes the number and formats of the depth and color
    /// </summary>
    public virtual OutputDescription OutputDescription { get; }

    /// <summary>
    /// Gets the width of the <see cref="Framebuffer" />.
    /// </summary>
    public virtual uint Width { get; }

    /// <summary>
    /// Gets the height of the <see cref="Framebuffer" />.
    /// </summary>
    public virtual uint Height { get; }

    /// <summary>
    /// A bool indicating whether this instance has been disposed.
    /// </summary>
    public abstract bool IsDisposed { get; }

    /// <summary>
    /// A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
    /// </summary>
    public abstract string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public abstract void Dispose();

    #endregion
}