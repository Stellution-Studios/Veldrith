using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Veldrith.D3D12;

/// <summary>
/// Defines the behavior and responsibilities of the D3D12Swapchain class.
/// </summary>
internal sealed class D3D12Swapchain : Swapchain {

    /// <summary>
    /// Stores the value associated with <c>_bufferCount</c>.
    /// </summary>
    private readonly int _bufferCount = 3;

    /// <summary>
    /// Stores the value associated with <c>_canTear</c>.
    /// </summary>
    private readonly bool _canTear;

    /// <summary>
    /// Stores the value associated with <c>_hasNativeSwapchain</c>.
    /// </summary>
    private readonly bool _hasNativeSwapchain;

    /// <summary>
    /// Stores the value associated with <c>_nativeColorFormat</c>.
    /// </summary>
    private readonly Format _nativeColorFormat;

    /// <summary>
    /// Stores the value associated with <c>gd</c>.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Stores the value associated with <c>_allowTearing</c>.
    /// </summary>
    private bool _allowTearing;

    /// <summary>
    /// Stores the value associated with <c>_backBufferResources</c>.
    /// </summary>
    private ID3D12Resource[] _backBufferResources;

    /// <summary>
    /// Stores the value associated with <c>_backBufferRtvs</c>.
    /// </summary>
    private CpuDescriptorHandle[] _backBufferRtvs;

    /// <summary>
    /// Stores the value associated with <c>_backBufferStates</c>.
    /// </summary>
    private ResourceStates[] _backBufferStates;

    /// <summary>
    /// Stores the value associated with <c>_colorTexture</c>.
    /// </summary>
    private Texture _colorTexture;

    /// <summary>
    /// Stores the value associated with <c>_depthTexture</c>.
    /// </summary>
    private Texture _depthTexture;

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the value associated with <c>_dxgiSwapChain</c>.
    /// </summary>
    private IDXGISwapChain3 _dxgiSwapChain;

    /// <summary>
    /// Stores the value associated with <c>_framebuffer</c>.
    /// </summary>
    private Framebuffer _framebuffer;

    /// <summary>
    /// Stores the value associated with <c>_rtvDescriptorSize</c>.
    /// </summary>
    private int _rtvDescriptorSize;

    /// <summary>
    /// Stores the value associated with <c>_rtvHeap</c>.
    /// </summary>
    private ID3D12DescriptorHeap _rtvHeap;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Swapchain" /> type.
    /// </summary>
    /// <param name="gd">Specifies the value of <paramref name="gd" />.</param>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    public D3D12Swapchain(D3D12GraphicsDevice gd, ref SwapchainDescription description) {
        this.gd = gd;
        this.SyncToVerticalBlank = description.SyncToVerticalBlank;
        this._nativeColorFormat = description.ColorSrgb ? Format.B8G8R8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm;
        using (IDXGIFactory5 factory5 = gd.DxgiFactory.QueryInterfaceOrNull<IDXGIFactory5>()) {
            this._canTear = factory5?.PresentAllowTearing == true;
        }

        this._hasNativeSwapchain = this.TryCreateNativeSwapchain(ref description);
        this.CreateAttachments(description.Width, description.Height, description.DepthFormat, description.ColorSrgb);
    }

    /// <summary>
    /// Gets or sets Framebuffer.
    /// </summary>
    public override Framebuffer Framebuffer => this._framebuffer;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets SyncToVerticalBlank.
    /// </summary>
    public override bool SyncToVerticalBlank { get; set; }

    /// <summary>
    /// Gets or sets AllowTearing.
    /// </summary>
    internal bool AllowTearing {
        get => this._allowTearing;
        set {
            if (this._allowTearing == value) {
                return;
            }

            this._allowTearing = value;
            if (!this._hasNativeSwapchain || this._dxgiSwapChain == null) {
                return;
            }

            uint width = this._colorTexture?.Width ?? 1u;
            uint height = this._colorTexture?.Height ?? 1u;
            this.RecreateNativeSwapchain(width, height);
        }
    }

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        if (this._disposed) {
            return;
        }

        this._disposed = true;
        this.DisposeNativeResources();
        this._dxgiSwapChain?.Dispose();
        this._framebuffer.Dispose();
        this._colorTexture.Dispose();
        this._depthTexture?.Dispose();
    }

    /// <summary>
    /// Executes the Resize operation.
    /// </summary>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    public override void Resize(uint width, uint height) {
        if (width == 0 || height == 0) {
            return;
        }

        bool useDepth = this._depthTexture != null;
        PixelFormat? depthFormat = useDepth ? this._depthTexture.Format : null;
        bool srgb = this._colorTexture.Format == PixelFormat.B8G8R8A8UNormSRgb || this._colorTexture.Format == PixelFormat.R8G8B8A8UNormSRgb;

        this._framebuffer.Dispose();
        this._colorTexture.Dispose();
        this._depthTexture?.Dispose();
        if (this._hasNativeSwapchain) {
            this.DisposeNativeResources();
            this._dxgiSwapChain.ResizeBuffers((uint)this._bufferCount, width, height, this._nativeColorFormat, SwapChainFlags.None);
            this.CreateNativeRenderTargets();
        }

        this.CreateAttachments(width, height, depthFormat, srgb);
    }

    /// <summary>
    /// Executes the Present operation.
    /// </summary>
    internal void Present() {
        if (this._hasNativeSwapchain) {
            PresentFlags presentFlags = PresentFlags.None;
            if (this._allowTearing && this._canTear && !this.SyncToVerticalBlank) {
                presentFlags = PresentFlags.AllowTearing;
            }

            this._dxgiSwapChain.Present(this.SyncToVerticalBlank ? 1u : 0u, presentFlags);
        }
    }

    /// <summary>
    /// Executes the CreateAttachments operation.
    /// </summary>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    /// <param name="depthFormat">Specifies the value of <paramref name="depthFormat" />.</param>
    /// <param name="srgb">Specifies the value of <paramref name="srgb" />.</param>
    private void CreateAttachments(uint width, uint height, PixelFormat? depthFormat, bool srgb) {
        PixelFormat colorFormat = srgb ? PixelFormat.B8G8R8A8UNormSRgb : PixelFormat.B8G8R8A8UNorm;
        TextureDescription colorDesc = TextureDescription.Texture2D(width, height, 1, 1, colorFormat, TextureUsage.RenderTarget | TextureUsage.Sampled);
        this._colorTexture = this.gd.ResourceFactory.CreateTexture(colorDesc);

        if (depthFormat != null) {
            TextureDescription depthDesc = TextureDescription.Texture2D(width, height, 1, 1, depthFormat.Value, TextureUsage.DepthStencil);
            this._depthTexture = this.gd.ResourceFactory.CreateTexture(depthDesc);
        }
        else {
            this._depthTexture = null;
        }

        FramebufferDescription fbDesc = new(this._depthTexture, this._colorTexture);
        if (this._hasNativeSwapchain) {
            this._framebuffer = new D3D12SwapchainFramebuffer(this.gd, this, ref fbDesc);
        }
        else {
            this._framebuffer = this.gd.ResourceFactory.CreateFramebuffer(fbDesc);
        }
    }

    /// <summary>
    /// Executes the TryCreateNativeSwapchain operation.
    /// </summary>
    /// <param name="description">Specifies the value of <paramref name="description" />.</param>
    /// <returns>Returns the result produced by the TryCreateNativeSwapchain operation.</returns>
    private bool TryCreateNativeSwapchain(ref SwapchainDescription description) {
        if (description.Source is not Win32SwapchainSource win32Source) {
            return false;
        }

        SwapChainFlags flags = this.GetSwapChainFlags();
        SwapChainDescription1 swapChainDesc = new() {
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

        IDXGISwapChain1 swapChain1 = this.gd.DxgiFactory.CreateSwapChainForHwnd(this.gd.CommandQueue, win32Source.Hwnd, swapChainDesc);
        this._dxgiSwapChain = swapChain1.QueryInterface<IDXGISwapChain3>();
        swapChain1.Dispose();
        this.CreateNativeRenderTargets();
        return true;
    }

    /// <summary>
    /// Executes the TryGetCurrentBackBuffer operation.
    /// </summary>
    /// <param name="resource">Specifies the value of <paramref name="resource" />.</param>
    /// <param name="rtv">Specifies the value of <paramref name="rtv" />.</param>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="state">Specifies the value of <paramref name="state" />.</param>
    /// <returns>Returns the result produced by the TryGetCurrentBackBuffer operation.</returns>
    internal bool TryGetCurrentBackBuffer(out ID3D12Resource resource, out CpuDescriptorHandle rtv, out int index, out ResourceStates state) {
        if (!this._hasNativeSwapchain || this._dxgiSwapChain == null) {
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

    /// <summary>
    /// Executes the SetBackBufferState operation.
    /// </summary>
    /// <param name="index">Specifies the value of <paramref name="index" />.</param>
    /// <param name="state">Specifies the value of <paramref name="state" />.</param>
    internal void SetBackBufferState(int index, ResourceStates state) {
        this._backBufferStates[index] = state;
    }

    /// <summary>
    /// Executes the CreateNativeRenderTargets operation.
    /// </summary>
    private void CreateNativeRenderTargets() {
        this._rtvDescriptorSize = (int)this.gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        this._rtvHeap = this.gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.RenderTargetView, (uint)this._bufferCount));
        this._backBufferResources = new ID3D12Resource[this._bufferCount];
        this._backBufferRtvs = new CpuDescriptorHandle[this._bufferCount];
        this._backBufferStates = new ResourceStates[this._bufferCount];

        CpuDescriptorHandle handle = this._rtvHeap.GetCPUDescriptorHandleForHeapStart();
        for (int i = 0; i < this._bufferCount; i++) {
            ID3D12Resource buffer = this._dxgiSwapChain.GetBuffer<ID3D12Resource>((uint)i);
            this._backBufferResources[i] = buffer;
            this._backBufferRtvs[i] = handle + i * this._rtvDescriptorSize;
            this._backBufferStates[i] = ResourceStates.Present;
            this.gd.Device.CreateRenderTargetView(buffer, null, this._backBufferRtvs[i]);
        }
    }

    /// <summary>
    /// Executes the RecreateNativeSwapchain operation.
    /// </summary>
    /// <param name="width">Specifies the value of <paramref name="width" />.</param>
    /// <param name="height">Specifies the value of <paramref name="height" />.</param>
    private void RecreateNativeSwapchain(uint width, uint height) {
        if (!this._hasNativeSwapchain || this._dxgiSwapChain == null) {
            return;
        }

        this.DisposeNativeResources();
        this._dxgiSwapChain.ResizeBuffers((uint)this._bufferCount, width, height, this._nativeColorFormat, this.GetSwapChainFlags());
        this.CreateNativeRenderTargets();
    }

    /// <summary>
    /// Executes the GetSwapChainFlags operation.
    /// </summary>
    /// <returns>Returns the result produced by the GetSwapChainFlags operation.</returns>
    private SwapChainFlags GetSwapChainFlags() {
        if (this._allowTearing && this._canTear) {
            return SwapChainFlags.AllowTearing;
        }

        return SwapChainFlags.None;
    }

    /// <summary>
    /// Executes the DisposeNativeResources operation.
    /// </summary>
    private void DisposeNativeResources() {
        if (this._backBufferResources != null) {
            foreach (ID3D12Resource resource in this._backBufferResources) {
                resource?.Dispose();
            }
        }

        this._rtvHeap?.Dispose();
        this._backBufferResources = null;
        this._backBufferRtvs = null;
        this._backBufferStates = null;
    }
}