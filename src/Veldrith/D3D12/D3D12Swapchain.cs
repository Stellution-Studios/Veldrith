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
            _nativeColorFormat = description.ColorSrgb ? Format.B8G8R8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm;
            using (var factory5 = gd.DxgiFactory.QueryInterfaceOrNull<IDXGIFactory5>())
            {
                _canTear = factory5?.PresentAllowTearing == true;
            }
            _hasNativeSwapchain = tryCreateNativeSwapchain(ref description);
            createAttachments(description.Width, description.Height, description.DepthFormat, description.ColorSrgb);
        }

        public override Framebuffer Framebuffer => _framebuffer;
        public override bool IsDisposed => _disposed;
        public override bool SyncToVerticalBlank { get; set; }
        internal bool AllowTearing
        {
            get => _allowTearing;
            set
            {
                if (_allowTearing == value)
                {
                    return;
                }

                _allowTearing = value;
                if (!_hasNativeSwapchain || _dxgiSwapChain == null)
                {
                    return;
                }

                uint width = _colorTexture?.Width ?? 1u;
                uint height = _colorTexture?.Height ?? 1u;
                recreateNativeSwapchain(width, height);
            }
        }

        public override string Name
        {
            get => _name;
            set => _name = value;
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            disposeNativeResources();
            _dxgiSwapChain?.Dispose();
            _framebuffer.Dispose();
            _colorTexture.Dispose();
            _depthTexture?.Dispose();
        }

        public override void Resize(uint width, uint height)
        {
            if (width == 0 || height == 0)
            {
                return;
            }

            bool useDepth = _depthTexture != null;
            PixelFormat? depthFormat = useDepth ? _depthTexture.Format : null;
            bool srgb = _colorTexture.Format == PixelFormat.B8G8R8A8UNormSRgb || _colorTexture.Format == PixelFormat.R8G8B8A8UNormSRgb;

            _framebuffer.Dispose();
            _colorTexture.Dispose();
            _depthTexture?.Dispose();
            if (_hasNativeSwapchain)
            {
                disposeNativeResources();
                _dxgiSwapChain.ResizeBuffers((uint)_bufferCount, width, height, _nativeColorFormat, SwapChainFlags.None);
                createNativeRenderTargets();
            }

            createAttachments(width, height, depthFormat, srgb);
        }

        internal void Present()
        {
            if (_hasNativeSwapchain)
            {
                PresentFlags presentFlags = PresentFlags.None;
                if (_allowTearing && _canTear && !SyncToVerticalBlank)
                {
                    presentFlags = PresentFlags.AllowTearing;
                }

                _dxgiSwapChain.Present(SyncToVerticalBlank ? 1u : 0u, presentFlags);
            }
        }

        private void createAttachments(uint width, uint height, PixelFormat? depthFormat, bool srgb)
        {
            var colorFormat = srgb ? PixelFormat.B8G8R8A8UNormSRgb : PixelFormat.B8G8R8A8UNorm;
            var colorDesc = TextureDescription.Texture2D(width, height, 1, 1, colorFormat, TextureUsage.RenderTarget | TextureUsage.Sampled);
            _colorTexture = gd.ResourceFactory.CreateTexture(colorDesc);

            if (depthFormat != null)
            {
                var depthDesc = TextureDescription.Texture2D(width, height, 1, 1, depthFormat.Value, TextureUsage.DepthStencil);
                _depthTexture = gd.ResourceFactory.CreateTexture(depthDesc);
            }
            else
            {
                _depthTexture = null;
            }

            var fbDesc = new FramebufferDescription(_depthTexture, _colorTexture);
            if (_hasNativeSwapchain)
            {
                _framebuffer = new D3D12SwapchainFramebuffer(gd, this, ref fbDesc);
            }
            else
            {
                _framebuffer = gd.ResourceFactory.CreateFramebuffer(fbDesc);
            }
        }

        private bool tryCreateNativeSwapchain(ref SwapchainDescription description)
        {
            if (description.Source is not Win32SwapchainSource win32Source)
            {
                return false;
            }

            SwapChainFlags flags = getSwapChainFlags();
            var swapChainDesc = new SwapChainDescription1
            {
                Width = description.Width,
                Height = description.Height,
                Format = _nativeColorFormat,
                BufferCount = (uint)_bufferCount,
                BufferUsage = Usage.RenderTargetOutput,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.FlipDiscard,
                Scaling = Scaling.Stretch,
                Stereo = false,
                AlphaMode = AlphaMode.Ignore,
                Flags = flags
            };

            IDXGISwapChain1 swapChain1 = gd.DxgiFactory.CreateSwapChainForHwnd(gd.CommandQueue, win32Source.Hwnd, swapChainDesc, null, null);
            _dxgiSwapChain = swapChain1.QueryInterface<IDXGISwapChain3>();
            swapChain1.Dispose();
            createNativeRenderTargets();
            return true;
        }

        internal bool TryGetCurrentBackBuffer(out ID3D12Resource resource, out CpuDescriptorHandle rtv, out int index, out ResourceStates state)
        {
            if (!_hasNativeSwapchain || _dxgiSwapChain == null)
            {
                resource = null;
                rtv = default;
                index = -1;
                state = ResourceStates.Common;
                return false;
            }

            index = (int)_dxgiSwapChain.CurrentBackBufferIndex;
            resource = _backBufferResources[index];
            rtv = _backBufferRtvs[index];
            state = _backBufferStates[index];
            return true;
        }

        internal void SetBackBufferState(int index, ResourceStates state)
        {
            _backBufferStates[index] = state;
        }

        private void createNativeRenderTargets()
        {
            _rtvDescriptorSize = (int)gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
            _rtvHeap = gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.RenderTargetView, (uint)_bufferCount));
            _backBufferResources = new ID3D12Resource[_bufferCount];
            _backBufferRtvs = new CpuDescriptorHandle[_bufferCount];
            _backBufferStates = new ResourceStates[_bufferCount];

            CpuDescriptorHandle handle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
            for (int i = 0; i < _bufferCount; i++)
            {
                ID3D12Resource buffer = _dxgiSwapChain.GetBuffer<ID3D12Resource>((uint)i);
                _backBufferResources[i] = buffer;
                _backBufferRtvs[i] = handle + (i * _rtvDescriptorSize);
                _backBufferStates[i] = ResourceStates.Present;
                gd.Device.CreateRenderTargetView(buffer, null, _backBufferRtvs[i]);
            }
        }

        private void recreateNativeSwapchain(uint width, uint height)
        {
            if (!_hasNativeSwapchain || _dxgiSwapChain == null)
            {
                return;
            }

            disposeNativeResources();
            _dxgiSwapChain.ResizeBuffers((uint)_bufferCount, width, height, _nativeColorFormat, getSwapChainFlags());
            createNativeRenderTargets();
        }

        private SwapChainFlags getSwapChainFlags()
        {
            if (_allowTearing && _canTear)
            {
                return SwapChainFlags.AllowTearing;
            }

            return SwapChainFlags.None;
        }

        private void disposeNativeResources()
        {
            if (_backBufferResources != null)
            {
                foreach (var resource in _backBufferResources)
                {
                    resource?.Dispose();
                }
            }

            _rtvHeap?.Dispose();
            _backBufferResources = null;
            _backBufferRtvs = null;
            _backBufferStates = null;
        }
    }
}
