namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12SwapchainFramebuffer.
/// </summary>
internal sealed class D3D12SwapchainFramebuffer : D3D12Framebuffer {

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12SwapchainFramebuffer" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="swapchain">The swapchain used by this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12SwapchainFramebuffer(D3D12GraphicsDevice gd, D3D12Swapchain swapchain, ref FramebufferDescription description) : base(gd, ref description) {
        this.Swapchain = swapchain;
    }

    /// <summary>
    /// Gets or sets Swapchain.
    /// </summary>
    public D3D12Swapchain Swapchain { get; }
}