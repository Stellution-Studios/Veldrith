using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrith.Vk;

/// <summary>
/// Defines the behavior and responsibilities of the VkFence class.
/// </summary>
internal unsafe class VkFence : Fence {

    /// <summary>
    /// Stores the value associated with <c>_fence</c>.
    /// </summary>
    private readonly Vulkan.VkFence _fence;

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly VkGraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_destroyed</c>.
    /// </summary>
    private bool _destroyed;

    /// <summary>
    /// Stores the value associated with <c>_name</c>.
    /// </summary>
    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="VkFence" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="signaled">Specifies the value of <paramref name="signaled" />.</param>
    public VkFence(VkGraphicsDevice gd, bool signaled) {
        this.gd = gd;
        VkFenceCreateInfo fenceCi = VkFenceCreateInfo.New();
        fenceCi.flags = signaled ? VkFenceCreateFlags.Signaled : VkFenceCreateFlags.None;
        VkResult result = vkCreateFence(this.gd.Device, ref fenceCi, null, out this._fence);
        VulkanUtil.CheckResult(result);
    }

    /// <summary>
    /// Stores the value associated with <c>DeviceFence</c>.
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        if (!this._destroyed) {
            vkDestroyFence(this.gd.Device, this._fence, null);
            this._destroyed = true;
        }
    }

    #endregion

    /// <summary>
    /// Executes the Reset operation.
    /// </summary>
    public override void Reset() {
        this.gd.ResetFence(this);
    }
}