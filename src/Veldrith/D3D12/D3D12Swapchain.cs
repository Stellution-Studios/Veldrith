using Vortice.DXGI;
using Vortice.Direct3D12;

namespace Veldrith.D3D12
{
    internal sealed class D3D12Swapchain : Swapchain
    {
        private readonly D3D12GraphicsDevice gd;
        private readonly bool _hasNativeSwapchain;
        private readonly Format _nativeColorFormat;
        private readonly bool _canTear;
        private readonly int _bufferCount = 3;
        private Texture _colorTexture;
        private Texture _depthTexture;
        private Framebuffer _framebuffer;
        private IDXGISwapChain3 _dxgiSwapChain;
        private ID3D12DescriptorHeap _rtvHeap;
        private ID3D12Resource[] _backBufferResources;
        private CpuDescriptorHandle[] _backBufferRtvs;
        private ResourceStates[] _backBufferStates;
        private int _rtvDescriptorSize;
        private bool _allowTearing;
        private bool _disposed;
        private string _name;

        public D3D12Swapchain(D3D12GraphicsDevice gd, ref SwapchainDescription description)
        {
            this.gd = gd;
            SyncToVerticalBlank = description.SyncToVerticalBlank;
            this._nativeColorFormat = description.ColorSrgb ? Format.B8G8R8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm;
            using (var factory5 = gd.DxgiFactory.QueryInterfaceOrNull<IDXGIFactory5>())
            {
                this._canTear = factory5?.PresentAllowTearing == true;
            }
            this._hasNativeSwapchain = TryCreateNativeSwapchain(ref description);
            CreateAttachments(description.Width, description.Height, description.DepthFormat, description.ColorSrgb);
        }

        public override Framebuffer Framebuffer => this._framebuffer;
        public override bool IsDisposed => this._disposed;
        public override bool SyncToVerticalBlank { get; set; }
        internal bool AllowTearing
        {
            get => this._allowTearing;
            set
            {
                if (this._allowTearing == value)
                {
                    return;
                }

                this._allowTearing = value;
                if (!this._hasNativeSwapchain || this._dxgiSwapChain == null)
                {
                    return;
                }

                uint width = this._colorTexture?.Width ?? 1u;
                uint height = this._colorTexture?.Height ?? 1u;
                RecreateNativeSwapchain(width, height);
            }
        }

        public override string Name
        {
            get => this._name;
            set => this._name = value;
        }

        public override void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;
            DisposeNativeResources();
            this._dxgiSwapChain?.Dispose();
            this._framebuffer.Dispose();
            this._colorTexture.Dispose();
            this._depthTexture?.Dispose();
        }

        public override void Resize(uint width, uint height)
        {
            if (width == 0 || height == 0)
            {
                return;
            }

            bool useDepth = this._depthTexture != null;
            PixelFormat? depthFormat = useDepth ? this._depthTexture.Format : null;
            bool srgb = this._colorTexture.Format == PixelFormat.B8G8R8A8UNormSRgb || this._colorTexture.Format == PixelFormat.R8G8B8A8UNormSRgb;

            this._framebuffer.Dispose();
            this._colorTexture.Dispose();
            this._depthTexture?.Dispose();
            if (this._hasNativeSwapchain)
            {
                DisposeNativeResources();
                this._dxgiSwapChain.ResizeBuffers((uint)this._bufferCount, width, height, this._nativeColorFormat, SwapChainFlags.None);
                CreateNativeRenderTargets();
            }

            CreateAttachments(width, height, depthFormat, srgb);
        }

        internal void Present()
        {
            if (this._hasNativeSwapchain)
            {
                PresentFlags presentFlags = PresentFlags.None;
                if (this._allowTearing && this._canTear && !SyncToVerticalBlank)
                {
                    presentFlags = PresentFlags.AllowTearing;
                }

                this._dxgiSwapChain.Present(SyncToVerticalBlank ? 1u : 0u, presentFlags);
            }
        }

        private void CreateAttachments(uint width, uint height, PixelFormat? depthFormat, bool srgb)
        {
            var colorFormat = srgb ? PixelFormat.B8G8R8A8UNormSRgb : PixelFormat.B8G8R8A8UNorm;
            var colorDesc = TextureDescription.Texture2D(width, height, 1, 1, colorFormat, TextureUsage.RenderTarget | TextureUsage.Sampled);
            this._colorTexture = gd.ResourceFactory.CreateTexture(colorDesc);

            if (depthFormat != null)
            {
                var depthDesc = TextureDescription.Texture2D(width, height, 1, 1, depthFormat.Value, TextureUsage.DepthStencil);
                this._depthTexture = gd.ResourceFactory.CreateTexture(depthDesc);
            }
            else
            {
                this._depthTexture = null;
            }

            var fbDesc = new FramebufferDescription(this._depthTexture, this._colorTexture);
            if (this._hasNativeSwapchain)
            {
                this._framebuffer = new D3D12SwapchainFramebuffer(gd, this, ref fbDesc);
            }
            else
            {
                this._framebuffer = gd.ResourceFactory.CreateFramebuffer(fbDesc);
            }
        }

        private bool TryCreateNativeSwapchain(ref SwapchainDescription description)
        {
            if (description.Source is not Win32SwapchainSource win32Source)
            {
                return false;
            }

            SwapChainFlags flags = GetSwapChainFlags();
            var swapChainDesc = new SwapChainDescription1
            {
                Width = description.Width,
                Height = description.Height,
                Format = this._nativeColorFormat,
                BufferCount = (uint)this._bufferCount,
                BufferUsage = Usage.RenderTargetOutput,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.FlipDiscard,
                Scaling = Scaling.Stretch,
                Stereo = false,
                AlphaMode = AlphaMode.Ignore,
                Flags = flags
            };

            IDXGISwapChain1 swapChain1 = gd.DxgiFactory.CreateSwapChainForHwnd(gd.CommandQueue, win32Source.Hwnd, swapChainDesc, null, null);
            this._dxgiSwapChain = swapChain1.QueryInterface<IDXGISwapChain3>();
            swapChain1.Dispose();
            CreateNativeRenderTargets();
            return true;
        }

        internal bool TryGetCurrentBackBuffer(out ID3D12Resource resource, out CpuDescriptorHandle rtv, out int index, out ResourceStates state)
        {
            if (!this._hasNativeSwapchain || this._dxgiSwapChain == null)
            {
                resource = null;
                rtv = default;
                index = -1;
                state = ResourceStates.Common;
                return false;
            }

            index = (int)this._dxgiSwapChain.CurrentBackBufferIndex;
            resource = this._backBufferResources[index];
            rtv = this._backBufferRtvs[index];
            state = this._backBufferStates[index];
            return true;
        }

        internal void SetBackBufferState(int index, ResourceStates state)
        {
            this._backBufferStates[index] = state;
        }

        private void CreateNativeRenderTargets()
        {
            this._rtvDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
            this._rtvHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.RenderTargetView, (uint)this._bufferCount));
            this._backBufferResources = new ID3D12Resource[this._bufferCount];
            this._backBufferRtvs = new CpuDescriptorHandle[this._bufferCount];
            this._backBufferStates = new ResourceStates[this._bufferCount];

            CpuDescriptorHandle handle = this._rtvHeap.GetCPUDescriptorHandleForHeapStart();
            for (int i = 0; i < this._bufferCount; i++)
            {
                ID3D12Resource buffer = this._dxgiSwapChain.GetBuffer<ID3D12Resource>((uint)i);
                this._backBufferResources[i] = buffer;
                this._backBufferRtvs[i] = handle + (i * this._rtvDescriptorSize);
                this._backBufferStates[i] = ResourceStates.Present;
                gd.Device.CreateRenderTargetView(buffer, null, this._backBufferRtvs[i]);
            }
        }

        private void RecreateNativeSwapchain(uint width, uint height)
        {
            if (!this._hasNativeSwapchain || this._dxgiSwapChain == null)
            {
                return;
            }

            DisposeNativeResources();
            this._dxgiSwapChain.ResizeBuffers((uint)this._bufferCount, width, height, this._nativeColorFormat, GetSwapChainFlags());
            CreateNativeRenderTargets();
        }

        private SwapChainFlags GetSwapChainFlags()
        {
            if (this._allowTearing && this._canTear)
            {
                return SwapChainFlags.AllowTearing;
            }

            return SwapChainFlags.None;
        }

        private void DisposeNativeResources()
        {
            if (this._backBufferResources != null)
            {
                foreach (var resource in this._backBufferResources)
                {
                    resource?.Dispose();
                }
            }

            this._rtvHeap?.Dispose();
            this._backBufferResources = null;
            this._backBufferRtvs = null;
            this._backBufferStates = null;
        }
    }
}
