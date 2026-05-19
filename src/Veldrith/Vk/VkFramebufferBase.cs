using System.Collections.Generic;
using Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Represents the VkFramebufferBase class.
/// </summary>
internal abstract class VkFramebufferBase : Framebuffer {

    /// <summary>
    /// Initializes a new instance of the <see cref="VkFramebufferBase" /> class.
    /// </summary>
    /// <param name="depthTexture">The value of depthTexture.</param>
    /// <param name="colorTextures">The value of colorTextures.</param>
    /// <returns>The result of the base operation.</returns>
    protected VkFramebufferBase(FramebufferAttachmentDescription? depthTexture, IReadOnlyList<FramebufferAttachmentDescription> colorTextures) : base(depthTexture, colorTextures) {
        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VkFramebufferBase" /> type.
    /// </summary>
    protected VkFramebufferBase() {
        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    /// <summary>
    /// Gets or sets RefCount.
    /// </summary>
    public ResourceRefCount RefCount { get; }

    /// <summary>
    /// Gets or sets RenderableWidth.
    /// </summary>
    public abstract uint RenderableWidth { get; }

    /// <summary>
    /// Gets or sets RenderableHeight.
    /// </summary>
    public abstract uint RenderableHeight { get; }

    /// <summary>
    /// Gets or sets CurrentFramebuffer.
    /// </summary>
    public abstract Vulkan.VkFramebuffer CurrentFramebuffer { get; }

    /// <summary>
    /// Gets or sets RenderPassNoClearInit.
    /// </summary>
    public abstract VkRenderPass RenderPassNoClearInit { get; }

    /// <summary>
    /// Gets or sets RenderPassNoClearLoad.
    /// </summary>
    public abstract VkRenderPass RenderPassNoClearLoad { get; }

    /// <summary>
    /// Gets or sets RenderPassClear.
    /// </summary>
    public abstract VkRenderPass RenderPassClear { get; }

    /// <summary>
    /// Gets or sets AttachmentCount.
    /// </summary>
    public abstract uint AttachmentCount { get; }

    #region Disposal

    /// <summary>
    /// Performs the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Performs the TransitionToIntermediateLayout operation.
    /// </summary>
    /// <param name="cb">The value of cb.</param>
    public abstract void TransitionToIntermediateLayout(VkCommandBuffer cb);

    /// <summary>
    /// Performs the TransitionToFinalLayout operation.
    /// </summary>
    /// <param name="cb">The value of cb.</param>
    public abstract void TransitionToFinalLayout(VkCommandBuffer cb);

    /// <summary>
    /// Performs the DisposeCore operation.
    /// </summary>
    protected abstract void DisposeCore();
}
