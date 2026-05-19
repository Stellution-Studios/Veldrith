using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Represents the VkFence class.
/// </summary>
internal unsafe class VkFence : Fence {

    /// <summary>
    /// Represents the _fence field.
    /// </summary>
    private readonly Vulkan.VkFence _fence;

    /// <summary>
    /// Represents the gd field.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Represents the _destroyed field.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Represents the _name field.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkFence" /> class.
    /// </summary>
    public VkFence(VkGraphicsDevice gd, bool signaled) {
        this.gd = gd;
        VkFenceCreateInfo fenceCi = VkFenceCreateInfo.New();
        fenceCi.flags = signaled ? VkFenceCreateFlags.Signaled : VkFenceCreateFlags.None;
        VkResult result = vkCreateFence(this.gd.Device, ref fenceCi, null, out this._fence);
        VulkanUtil.CheckResult(result);
    }

    /// <summary>
    /// Represents the DeviceFence field.
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
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        if (!this._destroyed) {
            vkDestroyFence(this.gd.Device, this._fence, null);
            this._destroyed = true;
        }
    }

    #endregion

    /// <summary>
    /// Executes Reset.
    /// </summary>
    public override void Reset() {
        this.gd.ResetFence(this);
    }
}