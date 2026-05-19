namespace Veldrith.D3D12;

/// <summary>
/// Defines the behavior and responsibilities of the D3D12SwapchainFramebuffer class.
/// </summary>
internal sealed class D3D12SwapchainFramebuffer : D3D12Framebuffer {

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12SwapchainFramebuffer" /> class.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="swapchain">Specifies the value of <paramref name="swapchain" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the base operation.</returns>
    public D3D12SwapchainFramebuffer(D3D12GraphicsDevice gd, D3D12Swapchain swapchain, ref FramebufferDescription description) : base(gd, ref description) {
        this.Swapchain = swapchain;
    }

    /// <summary>
    /// Gets or sets Swapchain.
    /// </summary>
    public D3D12Swapchain Swapchain { get; }
}
