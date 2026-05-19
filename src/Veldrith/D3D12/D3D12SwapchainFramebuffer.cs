namespace Veldrith.D3D12;

/// <summary>
/// Represents the D3D12SwapchainFramebuffer class.
/// </summary>
internal sealed class D3D12SwapchainFramebuffer : D3D12Framebuffer {

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12SwapchainFramebuffer" /> class.
    /// </summary>
    /// <param name="gd">The value of gd.</param>
    /// <param name="swapchain">The value of swapchain.</param>
    /// <param name="description">The value of description.</param>
    /// <returns>The result of the base operation.</returns>
    public D3D12SwapchainFramebuffer(D3D12GraphicsDevice gd, D3D12Swapchain swapchain, ref FramebufferDescription description) : base(gd, ref description) {
        this.Swapchain = swapchain;
    }

    /// <summary>
    /// Gets or sets Swapchain.
    /// </summary>
    public D3D12Swapchain Swapchain { get; }
}
