using System.Collections.Generic;
using Vulkan;

namespace Veldrith.Vk;

internal abstract class VkFramebufferBase : Framebuffer {
    protected VkFramebufferBase(
        FramebufferAttachmentDescription? depthTexture,
        IReadOnlyList<FramebufferAttachmentDescription> colorTextures)
        : base(depthTexture, colorTextures) {
        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    protected VkFramebufferBase() {
        this.RefCount = new ResourceRefCount(this.DisposeCore);
    }

    public ResourceRefCount RefCount { get; }

    public abstract uint RenderableWidth { get; }
    public abstract uint RenderableHeight { get; }

    public abstract Vulkan.VkFramebuffer CurrentFramebuffer { get; }
    public abstract VkRenderPass RenderPassNoClearInit { get; }
    public abstract VkRenderPass RenderPassNoClearLoad { get; }
    public abstract VkRenderPass RenderPassClear { get; }
    public abstract uint AttachmentCount { get; }

    #region Disposal

    public override void Dispose() {
        this.RefCount.Decrement();
    }

    #endregion

    public abstract void TransitionToIntermediateLayout(VkCommandBuffer cb);
    public abstract void TransitionToFinalLayout(VkCommandBuffer cb);

    protected abstract void DisposeCore();
}