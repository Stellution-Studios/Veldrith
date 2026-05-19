using System.Collections.Generic;
using Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Defines the behavior and responsibilities of the VkFramebufferBase class.
/// </summary>
internal abstract class VkFramebufferBase : Framebuffer {

    /// <summary>
    /// Initializes a new instance of the <see cref="VkFramebufferBase" /> class.
    /// </summary>
    /// <param name="depthTexture">Specifies the value of <paramref name="depthTexture" />.</param>
    /// <param name="colorTextures">Specifies the value of <paramref name="colorTextures" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    /// <summary>
    /// Executes the TransitionToIntermediateLayout operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    public abstract void TransitionToIntermediateLayout(VkCommandBuffer cb);

    /// <summary>
    /// Executes the TransitionToFinalLayout operation.
    /// </summary>
    /// <param name="cb">Specifies the value of <paramref name="cb" />.</param>
    public abstract void TransitionToFinalLayout(VkCommandBuffer cb);

    /// <summary>
    /// Executes the DisposeCore operation.
    /// </summary>
    protected abstract void DisposeCore();
}
