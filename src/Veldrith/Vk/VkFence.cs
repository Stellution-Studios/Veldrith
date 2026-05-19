using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

internal unsafe class VkFence : Fence {
    private readonly Vulkan.VkFence _fence;

    private readonly VkGraphicsDevice gd;
    private bool _destroyed;
    private string _name;

    public VkFence(VkGraphicsDevice gd, bool signaled) {
        this.gd = gd;
        VkFenceCreateInfo fenceCi = VkFenceCreateInfo.New();
        fenceCi.flags = signaled ? VkFenceCreateFlags.Signaled : VkFenceCreateFlags.None;
        VkResult result = vkCreateFence(this.gd.Device, ref fenceCi, null, out this._fence);
        VulkanUtil.CheckResult(result);
    }

    public Vulkan.VkFence DeviceFence => this._fence;

    public override bool Signaled => vkGetFenceStatus(this.gd.Device, this._fence) == VkResult.Success;
    public override bool IsDisposed => this._destroyed;

    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    public override void Dispose() {
        if (!this._destroyed) {
            vkDestroyFence(this.gd.Device, this._fence, null);
            this._destroyed = true;
        }
    }

    #endregion

    public override void Reset() {
        this.gd.ResetFence(this);
    }
}