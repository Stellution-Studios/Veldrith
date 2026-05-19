using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk
{
    internal unsafe class VkFence : Fence
    {
        public Vulkan.VkFence DeviceFence => fence;

        public override bool Signaled => vkGetFenceStatus(gd.Device, fence) == VkResult.Success;
        public override bool IsDisposed => destroyed;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                gd.SetResourceName(this, value);
            }
        }

        private readonly VkGraphicsDevice gd;
        private readonly Vulkan.VkFence fence;
        private string name;
        private bool destroyed;

        public VkFence(VkGraphicsDevice gd, bool signaled)
        {
            this.gd = gd;
            var fenceCi = VkFenceCreateInfo.New();
            fenceCi.flags = signaled ? VkFenceCreateFlags.Signaled : VkFenceCreateFlags.None;
            var result = vkCreateFence(this.gd.Device, ref fenceCi, null, out fence);
            VulkanUtil.CheckResult(result);
        }

        #region Disposal

        public override void Dispose()
        {
            if (!destroyed)
            {
                vkDestroyFence(gd.Device, fence, null);
                destroyed = true;
            }
        }

        #endregion

        public override void Reset()
        {
            gd.ResetFence(this);
        }
    }
}
