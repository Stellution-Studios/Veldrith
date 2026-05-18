using Vortice.DXGI;
using Vortice.Direct3D12;

namespace Veldrid.D3D12
{
    internal sealed class D3D12Swapchain : Swapchain
    {
        private readonly D3D12GraphicsDevice gd;
        private readonly bool hasNativeSwapchain;
        private readonly Format nativeColorFormat;
        private readonly int bufferCount = 2;
        private Texture colorTexture;
        private Texture depthTexture;
        private Framebuffer framebuffer;
        private IDXGISwapChain3 dxgiSwapChain;
        private ID3D12DescriptorHeap rtvHeap;
        private ID3D12Resource[] backBufferResources;
        private CpuDescriptorHandle[] backBufferRtvs;
        private ResourceStates[] backBufferStates;
        private int rtvDescriptorSize;
        private bool disposed;
        private string name;

        public D3D12Swapchain(D3D12GraphicsDevice gd, ref SwapchainDescription description)
        {
            this.gd = gd;
            SyncToVerticalBlank = description.SyncToVerticalBlank;
            nativeColorFormat = description.ColorSrgb ? Format.B8G8R8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm;
            hasNativeSwapchain = tryCreateNativeSwapchain(ref description);
            createAttachments(description.Width, description.Height, description.DepthFormat, description.ColorSrgb);
        }

        public override Framebuffer Framebuffer => framebuffer;
        public override bool IsDisposed => disposed;
        public override bool SyncToVerticalBlank { get; set; }

        public override string Name
        {
            get => name;
            set => name = value;
        }

        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            disposeNativeResources();
            dxgiSwapChain?.Dispose();
            framebuffer.Dispose();
            colorTexture.Dispose();
            depthTexture?.Dispose();
        }

        public override void Resize(uint width, uint height)
        {
            if (width == 0 || height == 0)
            {
                return;
            }

            bool useDepth = depthTexture != null;
            PixelFormat? depthFormat = useDepth ? depthTexture.Format : null;
            bool srgb = colorTexture.Format == PixelFormat.B8G8R8A8UNormSRgb || colorTexture.Format == PixelFormat.R8G8B8A8UNormSRgb;

            framebuffer.Dispose();
            colorTexture.Dispose();
            depthTexture?.Dispose();
            if (hasNativeSwapchain)
            {
                disposeNativeResources();
                dxgiSwapChain.ResizeBuffers((uint)bufferCount, width, height, nativeColorFormat, SwapChainFlags.None);
                createNativeRenderTargets();
            }

            createAttachments(width, height, depthFormat, srgb);
        }

        internal void Present()
        {
            if (hasNativeSwapchain)
            {
                dxgiSwapChain.Present(SyncToVerticalBlank ? 1u : 0u, PresentFlags.None);
            }
        }

        private void createAttachments(uint width, uint height, PixelFormat? depthFormat, bool srgb)
        {
            var colorFormat = srgb ? PixelFormat.B8G8R8A8UNormSRgb : PixelFormat.B8G8R8A8UNorm;
            var colorDesc = TextureDescription.Texture2D(width, height, 1, 1, colorFormat, TextureUsage.RenderTarget | TextureUsage.Sampled);
            colorTexture = gd.ResourceFactory.CreateTexture(colorDesc);

            if (depthFormat != null)
            {
                var depthDesc = TextureDescription.Texture2D(width, height, 1, 1, depthFormat.Value, TextureUsage.DepthStencil);
                depthTexture = gd.ResourceFactory.CreateTexture(depthDesc);
            }
            else
            {
                depthTexture = null;
            }

            var fbDesc = new FramebufferDescription(depthTexture, colorTexture);
            if (hasNativeSwapchain)
            {
                framebuffer = new D3D12SwapchainFramebuffer(gd, this, ref fbDesc);
            }
            else
            {
                framebuffer = gd.ResourceFactory.CreateFramebuffer(fbDesc);
            }
        }

        private bool tryCreateNativeSwapchain(ref SwapchainDescription description)
        {
            if (description.Source is not Win32SwapchainSource win32Source)
            {
                return false;
            }

            var swapChainDesc = new SwapChainDescription1
            {
                Width = description.Width,
                Height = description.Height,
                Format = nativeColorFormat,
                BufferCount = (uint)bufferCount,
                BufferUsage = Usage.RenderTargetOutput,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.FlipDiscard,
                Scaling = Scaling.Stretch,
                Stereo = false,
                AlphaMode = AlphaMode.Ignore,
                Flags = SwapChainFlags.None
            };

            IDXGISwapChain1 swapChain1 = gd.DxgiFactory.CreateSwapChainForHwnd(gd.CommandQueue, win32Source.Hwnd, swapChainDesc, null, null);
            dxgiSwapChain = swapChain1.QueryInterface<IDXGISwapChain3>();
            swapChain1.Dispose();
            createNativeRenderTargets();
            return true;
        }

        internal bool TryGetCurrentBackBuffer(out ID3D12Resource resource, out CpuDescriptorHandle rtv, out int index, out ResourceStates state)
        {
            if (!hasNativeSwapchain || dxgiSwapChain == null)
            {
                resource = null;
                rtv = default;
                index = -1;
                state = ResourceStates.Common;
                return false;
            }

            index = (int)dxgiSwapChain.CurrentBackBufferIndex;
            resource = backBufferResources[index];
            rtv = backBufferRtvs[index];
            state = backBufferStates[index];
            return true;
        }

        internal void SetBackBufferState(int index, ResourceStates state)
        {
            backBufferStates[index] = state;
        }

        private void createNativeRenderTargets()
        {
            rtvDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
            rtvHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.RenderTargetView, (uint)bufferCount));
            backBufferResources = new ID3D12Resource[bufferCount];
            backBufferRtvs = new CpuDescriptorHandle[bufferCount];
            backBufferStates = new ResourceStates[bufferCount];

            CpuDescriptorHandle handle = rtvHeap.GetCPUDescriptorHandleForHeapStart();
            for (int i = 0; i < bufferCount; i++)
            {
                ID3D12Resource buffer = dxgiSwapChain.GetBuffer<ID3D12Resource>((uint)i);
                backBufferResources[i] = buffer;
                backBufferRtvs[i] = handle + (i * rtvDescriptorSize);
                backBufferStates[i] = ResourceStates.Present;
                gd.Device.CreateRenderTargetView(buffer, null, backBufferRtvs[i]);
            }
        }

        private void disposeNativeResources()
        {
            if (backBufferResources != null)
            {
                foreach (var resource in backBufferResources)
                {
                    resource?.Dispose();
                }
            }

            rtvHeap?.Dispose();
            backBufferResources = null;
            backBufferRtvs = null;
            backBufferStates = null;
        }
    }
}
