using System.Collections.Generic;
using Vulkan;

namespace Veldrith.Vk;

internal abstract class VkFramebufferBase : Framebuffer {

    /// <summary>
    /// Initializes a new instance of the <see cref="VkFramebufferBase" /> class.
    /// </summary>
    protected VkFramebufferBase(FramebufferAttachmentDescription? depthTexture, IReadOnlyList<FramebufferAttachmentDescription> colorTextures)
        : base(depthTexture, colorTextures) {
        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VkFramebufferBase" /> class.
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
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes TransitionToIntermediateLayout.
    /// </summary>
    public abstract void TransitionToIntermediateLayout(VkCommandBuffer cb);

    /// <summary>
    /// Executes TransitionToFinalLayout.
    /// </summary>
    public abstract void TransitionToFinalLayout(VkCommandBuffer cb);

    /// <summary>
    /// Executes DisposeCore.
    /// </summary>
    protected abstract void DisposeCore();
}