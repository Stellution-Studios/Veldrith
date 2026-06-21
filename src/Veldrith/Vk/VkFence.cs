using Vortice.Vulkan;

namespace Veldrith.Vk;

/// <summary>
/// Provides the Vulkan backend implementation for VkFence.
/// </summary>
internal unsafe class VkFence : Fence {

    /// <summary>
    /// Stores the fence state used by this instance.
    /// </summary>
    private readonly global::Vortice.Vulkan.VkFence _fence;

    /// <summary>
    /// Stores the graphics device used by this instance.
    /// </summary>
    private readonly VkGraphicsDevice _gd;

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
        this._gd = gd;
        VkFenceCreateInfo fenceCi = new VkFenceCreateInfo();
        fenceCi.flags = signaled ? VkFenceCreateFlags.Signaled : VkFenceCreateFlags.None;
        VkResult result = this._gd.DeviceApi.vkCreateFence(ref fenceCi, null, out this._fence);
        VulkanUtil.CheckResult(result);
    }

    /// <summary>
    /// Stores the device fence state used by this instance.
    /// </summary>
    public global::Vortice.Vulkan.VkFence DeviceFence => this._fence;

    /// <summary>
    /// Gets or sets Signaled.
    /// </summary>
    public override bool Signaled => this._gd.DeviceApi.vkGetFenceStatus(this._fence) == VkResult.Success;

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
            this._gd.SetResourceName(this, value);
        }
    }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (!this._destroyed) {
            this._gd.DeviceApi.vkDestroyFence(this._fence, null);
            this._destroyed = true;
        }
    }

    #endregion

    /// <summary>
    /// Resets this instance to its initial state.
    /// </summary>
    public override void Reset() {
        this._gd.ResetFence(this);
    }
}