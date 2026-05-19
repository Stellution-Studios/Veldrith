using System.Collections.Generic;
using Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkFramebufferBase.
/// </summary>
internal abstract class VkFramebufferBase : Framebuffer {

    /// <summary>
    /// Initializes a new instance of the <see cref="VkFramebufferBase" /> class.
    /// </summary>
    /// <param name="depthTexture">The depth texture value used by this operation.</param>
    /// <param name="colorTextures">The texture resource involved in this operation.</param>
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
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes the transition to intermediate layout logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    public abstract void TransitionToIntermediateLayout(VkCommandBuffer cb);

    /// <summary>
    /// Executes the transition to final layout logic for this backend.
    /// </summary>
    /// <param name="cb">The cb value used by this operation.</param>
    public abstract void TransitionToFinalLayout(VkCommandBuffer cb);

    /// <summary>
    /// Executes the dispose core logic for this backend.
    /// </summary>
    protected abstract void DisposeCore();
}