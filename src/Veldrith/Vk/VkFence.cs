using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkFence.
/// </summary>
internal unsafe class VkFence : Fence {

    /// <summary>
    /// Stores the fence state used by this instance.
    /// </summary>
    private readonly Vulkan.VkFence _fence;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the destroyed state used by this instance.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the human-readable name associated with this instance.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkFence" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="signaled">The signaled value used by this operation.</param>
    public VkFence(VkGraphicsDevice gd, bool signaled) {
        this.gd = gd;
        VkFenceCreateInfo fenceCi = VkFenceCreateInfo.New();
        fenceCi.flags = signaled ? VkFenceCreateFlags.Signaled : VkFenceCreateFlags.None;
        VkResult result = vkCreateFence(this.gd.Device, ref fenceCi, null, out this._fence);
        VulkanUtil.CheckResult(result);
    }

    /// <summary>
    /// Stores the device fence state used by this instance.
    /// </summary>
    public Vulkan.VkFence DeviceFence => this._fence;

    /// <summary>
    /// Gets or sets Signaled.
    /// </summary>
    public override bool Signaled => vkGetFenceStatus(this.gd.Device, this._fence) == VkResult.Success;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._destroyed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name {
        get => this._name;
        set {
            this._name = value;
            this.gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (!this._destroyed) {
            vkDestroyFence(this.gd.Device, this._fence, null);
            this._destroyed = true;
        }
    }

    #endregion

    /// <summary>
    /// Resets this instance to its initial state.
    /// </summary>
    public override void Reset() {
        this.gd.ResetFence(this);
    }
}