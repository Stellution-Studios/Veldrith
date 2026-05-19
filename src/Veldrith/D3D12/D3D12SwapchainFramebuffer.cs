namespace Veldrith.D3D12
{
    internal sealed class D3D12SwapchainFramebuffer : D3D12Framebuffer
    {
        private readonly D3D12Swapchain swapchain;

        public D3D12SwapchainFramebuffer(D3D12GraphicsDevice gd, D3D12Swapchain swapchain, ref FramebufferDescription description)
            : base(gd, ref description)
        {
            this.swapchain = swapchain;
        }

        public D3D12Swapchain Swapchain => swapchain;
    }
}
