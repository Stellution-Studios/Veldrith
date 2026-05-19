using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12Swapchain.
/// </summary>
internal sealed class D3D12Swapchain : Swapchain {

    /// <summary>
    /// Stores the buffer count value used during command execution.
    /// </summary>
    private readonly int _bufferCount = 3;

    /// <summary>
    /// Tracks whether can tear is currently enabled.
    /// </summary>
    private readonly bool _canTear;

    /// <summary>
    /// Tracks whether has native swapchain is currently enabled.
    /// </summary>
    private readonly bool _hasNativeSwapchain;

    /// <summary>
    /// Stores the native color format state used by this instance.
    /// </summary>
    private readonly Format _nativeColorFormat;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Stores the allow tearing state used by this instance.
    /// </summary>
    private bool _allowTearing;

    /// <summary>
    /// Stores the back buffer resources collection used by this instance.
    /// </summary>
    private ID3D12Resource[] _backBufferResources;

    /// <summary>
    /// Stores the back buffer rtvs state used by this instance.
    /// </summary>
    private CpuDescriptorHandle[] _backBufferRtvs;

    /// <summary>
    /// Stores the back buffer states collection used by this instance.
    /// </summary>
    private ResourceStates[] _backBufferStates;

    /// <summary>
    /// Stores the color texture state used by this instance.
    /// </summary>
    private Texture _colorTexture;

    /// <summary>
    /// Stores the depth texture value used during command execution.
    /// </summary>
    private Texture _depthTexture;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the dxgi swap chain state used by this instance.
    /// </summary>
    private IDXGISwapChain3 _dxgiSwapChain;

    /// <summary>
    /// Stores the framebuffer state used by this instance.
    /// </summary>
    private Framebuffer _framebuffer;

    /// <summary>
    /// Stores the rtv descriptor size value used during command execution.
    /// </summary>
    private int _rtvDescriptorSize;

    /// <summary>
    /// Stores the rtv heap state used by this instance.
    /// </summary>
    private ID3D12DescriptorHeap _rtvHeap;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Swapchain" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12Swapchain(D3D12GraphicsDevice gd, ref SwapchainDescription description) {
        this.gd = gd;
        this.SyncToVerticalBlank = description.SyncToVerticalBlank;
        this._nativeColorFormat = description.ColorSrgb ? Format.B8G8R8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm;
        using (IDXGIFactory5 factory5 = gd.DxgiFactory.QueryInterfaceOrNull<IDXGIFactory5>()) {
            this._canTear = factory5?.PresentAllowTearing == true;
        }

        this._hasNativeSwapchain = this.TryCreateNativeSwapchain(ref description);
        uint attachmentWidth = description.Width;
        uint attachmentHeight = description.Height;
        this.TryResolveAttachmentSize(ref attachmentWidth, ref attachmentHeight);
        this.CreateAttachments(attachmentWidth, attachmentHeight, description.DepthFormat, description.ColorSrgb);
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
    /// Releases resources held by this instance.
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
    /// Executes the resize logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
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
            this._dxgiSwapChain.ResizeBuffers((uint)this._bufferCount, width, height, this._nativeColorFormat, this.GetSwapChainFlags());
            this.CreateNativeRenderTargets();
        }

        this.TryResolveAttachmentSize(ref width, ref height);
        this.CreateAttachments(width, height, depthFormat, srgb);
    }

    /// <summary>
    /// Executes the present logic for this backend.
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
    /// Creates the attachments instance used by this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depthFormat">The depth format value used by this operation.</param>
    /// <param name="srgb">The srgb value used by this operation.</param>
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
    /// Attempts to create native swapchain and reports whether it succeeded.
    /// </summary>
    /// <param name="description">The description used to configure this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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
    /// Attempts to get current back buffer and reports whether it succeeded.
    /// </summary>
    /// <param name="resource">The resource involved in this operation.</param>
    /// <param name="rtv">The rtv value used by this operation.</param>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="state">The state value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
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
    /// Sets the back buffer state value.
    /// </summary>
    /// <param name="index">The zero-based index of the target item.</param>
    /// <param name="state">The state value used by this operation.</param>
    internal void SetBackBufferState(int index, ResourceStates state) {
        this._backBufferStates[index] = state;
    }

    /// <summary>
    /// Creates the native render targets instance used by this backend.
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
    /// Executes the recreate native swapchain logic for this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    private void RecreateNativeSwapchain(uint width, uint height) {
        if (!this._hasNativeSwapchain || this._dxgiSwapChain == null) {
            return;
        }

        this.DisposeNativeResources();
        this._dxgiSwapChain.ResizeBuffers((uint)this._bufferCount, width, height, this._nativeColorFormat, this.GetSwapChainFlags());
        this.CreateNativeRenderTargets();
    }

    /// <summary>
    /// Resolves the attachment size to the native swapchain back-buffer size when available.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    private void TryResolveAttachmentSize(ref uint width, ref uint height) {
        if (!this._hasNativeSwapchain) {
            return;
        }

        if (!this.TryGetNativeBackBufferSize(out uint nativeWidth, out uint nativeHeight)) {
            return;
        }

        width = nativeWidth;
        height = nativeHeight;
    }

    /// <summary>
    /// Attempts to get native back-buffer size and reports whether it succeeded.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    private bool TryGetNativeBackBufferSize(out uint width, out uint height) {
        width = 0;
        height = 0;

        if (!this._hasNativeSwapchain || this._backBufferResources == null || this._backBufferResources.Length == 0) {
            return false;
        }

        ID3D12Resource backBuffer = this._backBufferResources[0];
        if (backBuffer == null) {
            return false;
        }

        ResourceDescription description = backBuffer.Description;
        if (description.Width == 0 || description.Height == 0) {
            return false;
        }

        width = (uint)description.Width;
        height = description.Height;
        return true;
    }

    /// <summary>
    /// Gets the swap chain flags value.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private SwapChainFlags GetSwapChainFlags() {
        if (this._allowTearing && this._canTear) {
            return SwapChainFlags.AllowTearing;
        }

        return SwapChainFlags.None;
    }

    /// <summary>
    /// Executes the dispose native resources logic for this backend.
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
