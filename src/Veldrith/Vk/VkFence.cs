using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk
{
    internal unsafe class VkFence : Fence
    {
        public Vulkan.VkFence DeviceFence => this._fence;

        public override bool Signaled => vkGetFenceStatus(gd.Device, this._fence) == VkResult.Success;
        public override bool IsDisposed => this._destroyed;

        public override string Name
        {
            get => this._name;
            set
            {
                this._name = value;
                gd.SetResourceName(this, value);
            }
        }

        private readonly VkGraphicsDevice gd;
        private readonly Vulkan.VkFence _fence;
        private string _name;
        private bool _destroyed;

        public VkFence(VkGraphicsDevice gd, bool signaled)
        {
            this.gd = gd;
            var fenceCi = VkFenceCreateInfo.New();
            fenceCi.flags = signaled ? VkFenceCreateFlags.Signaled : VkFenceCreateFlags.None;
            var result = vkCreateFence(this.gd.Device, ref fenceCi, null, out this._fence);
            VulkanUtil.CheckResult(result);
        }

        #region Disposal

        public override void Dispose()
        {
            if (!this._destroyed)
            {
                vkDestroyFence(gd.Device, this._fence, null);
                this._destroyed = true;
            }
        }

        #endregion

        public override void Reset()
        {
            gd.ResetFence(this);
        }
    }
}
