using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Veldrith
{
    /// <summary>
    ///     A device resource used to control which color and depth textures are rendered to.
    ///     See <see cref="FramebufferDescription" />.
    /// </summary>
    public abstract class Framebuffer : IDeviceResource, IDisposable
    {
        /// <summary>
        ///     Gets the depth attachment associated with this instance. May be null if no depth texture is used.
        /// </summary>
        public virtual FramebufferAttachment? DepthTarget { get; }

        /// <summary>
        ///     Gets the collection of color attachments associated with this instance. May be empty.
        /// </summary>
        public virtual IReadOnlyList<FramebufferAttachment> ColorTargets { get; }

        /// <summary>
        ///     Gets an <see cref="Veldrith.OutputDescription" /> which describes the number and formats of the depth and color
        ///     targets
        ///     in this instance.
        /// </summary>
        public virtual OutputDescription OutputDescription { get; }

        /// <summary>
        ///     Gets the width of the <see cref="Framebuffer" />.
        /// </summary>
        public virtual uint Width { get; }

        /// <summary>
        ///     Gets the height of the <see cref="Framebuffer" />.
        /// </summary>
        public virtual uint Height { get; }

        /// <summary>
        ///     A bool indicating whether this instance has been disposed.
        /// </summary>
        public abstract bool IsDisposed { get; }

        /// <summary>
        ///     A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
        ///     tools.
        /// </summary>
        public abstract string Name { get; set; }

        internal Framebuffer() { }

        internal Framebuffer(
            FramebufferAttachmentDescription? depthTargetDesc,
            IReadOnlyList<FramebufferAttachmentDescription> colorTargetDescs)
        {
            if (depthTargetDesc != null)
            {
                var depthAttachment = depthTargetDesc.Value;
                DepthTarget = new FramebufferAttachment(
                    depthAttachment.Target,
                    depthAttachment.ArrayLayer,
                    depthAttachment.MipLevel);
            }

            var colorTargets = new FramebufferAttachment[colorTargetDescs.Count];

            for (int i = 0; i < colorTargets.Length; i++)
            {
                colorTargets[i] = new FramebufferAttachment(
                    colorTargetDescs[i].Target,
                    colorTargetDescs[i].ArrayLayer,
                    colorTargetDescs[i].MipLevel);
            }

            ColorTargets = colorTargets;

            Texture dimTex;
            uint mipLevel;

            if (ColorTargets.Count > 0)
            {
                dimTex = ColorTargets[0].Target;
                mipLevel = ColorTargets[0].MipLevel;
            }
            else
            {
                Debug.Assert(DepthTarget != null);
                dimTex = DepthTarget.Value.Target;
                mipLevel = DepthTarget.Value.MipLevel;
            }

            Util.GetMipDimensions(dimTex, mipLevel, out uint mipWidth, out uint mipHeight, out _);
            Width = mipWidth;
            Height = mipHeight;

            OutputDescription = OutputDescription.CreateFromFramebuffer(this);
        }

        #region Disposal

        /// <summary>
        ///     Frees unmanaged device resources controlled by this instance.
        /// </summary>
        public abstract void Dispose();

        #endregion
    }
}
