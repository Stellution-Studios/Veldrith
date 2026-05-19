namespace Veldrith.D3D12;

internal sealed class D3D12SwapchainFramebuffer : D3D12Framebuffer {
    public D3D12SwapchainFramebuffer(D3D12GraphicsDevice gd, D3D12Swapchain swapchain,
        ref FramebufferDescription description)
        : base(gd, ref description) {
        this.Swapchain = swapchain;
    }

    public D3D12Swapchain Swapchain { get; }
}