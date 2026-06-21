using System;
using Vortice.Direct3D12;
using Vortice.DXGI;
using SharpGen.Runtime;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12Swapchain.
/// </summary>
internal sealed class D3D12Swapchain : Swapchain {

    /// <summary>
    /// Stores the buffer count value used during command execution.
    /// </summary>
    private const int SwapchainBufferCount = 2;

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
    /// Stores the native render-target view format state used by this instance.
    /// </summary>
    private readonly Format _nativeRtvFormat;

    /// <summary>
    /// Stores the gd state used by this instance.
    /// </summary>
    private readonly D3D12GraphicsDevice _gd;

    /// <summary>
    /// Stores the allow tearing state used by this instance.
    /// </summary>
    private bool _allowTearing;

    /// <summary>
    /// Stores the cached present sync interval for the current swapchain settings.
    /// </summary>
    private uint _presentSyncInterval;

    /// <summary>
    /// Stores the cached present flags for the current swapchain settings.
    /// </summary>
    private PresentFlags _presentFlags;

    /// <summary>
    /// Stores the sync-to-vblank state used by this instance.
    /// </summary>
    private bool _syncToVerticalBlank;

    /// <summary>
    /// Stores the back buffer resources collection used by this instance.
    /// </summary>
    private ID3D12Resource[] _backBufferResources;

    /// <summary>
    /// Cached current back buffer index — updated after every Present and resize so TryGetCurrentBackBuffer avoids a COM call.
    /// </summary>
    private int _currentBackBufferIndex;

    /// <summary>
    /// Increments whenever native swapchain back buffers are recreated.
    /// </summary>
    private uint _backBufferVersion;

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
    /// Stores the native DXGI swapchain pointer for no-alloc hotpath calls.
    /// </summary>
    private nint _dxgiSwapChainPointer;

    private unsafe delegate* unmanaged[Stdcall]<void*, uint, uint, int> _present;
    private unsafe delegate* unmanaged[Stdcall]<void*, uint> _getCurrentBackBufferIndex;

    /// <summary>
    /// Stores the framebuffer state used by this instance.
    /// </summary>
    private Framebuffer _framebuffer;

    /// <summary>
    /// Stores the rtv descriptor size value used during command execution.
    /// </summary>
    private int _rtvDescriptorSize;

    /// <summary>
    /// Stores the rtv descriptor allocation used by this instance.
    /// </summary>
    private D3D12CpuDescriptorAllocation _rtvDescriptors;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Swapchain" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="description">The description used to configure this operation.</param>
    public D3D12Swapchain(D3D12GraphicsDevice gd, ref SwapchainDescription description) {
        this._gd = gd;
        this._syncToVerticalBlank = description.SyncToVerticalBlank;
        this._nativeColorFormat = Format.B8G8R8A8_UNorm;
        this._nativeRtvFormat = description.ColorSrgb ? Format.B8G8R8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm;
        using (IDXGIFactory5 factory5 = gd.DxgiFactory.QueryInterfaceOrNull<IDXGIFactory5>()) {
            this._canTear = factory5?.PresentAllowTearing == true;
        }

        this._allowTearing = !description.SyncToVerticalBlank;
        this.RefreshPresentParameters();

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
    public override bool SyncToVerticalBlank {
        get => this._syncToVerticalBlank;
        set {
            if (this._syncToVerticalBlank == value) {
                return;
            }

            this._syncToVerticalBlank = value;
            if (!value) {
                this._allowTearing = true;
            }

            this.RefreshPresentParameters();
        }
    }

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
            this.RefreshPresentParameters();
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
        this._gd.WaitForIdle();
        this.DisposeNativeResources(disposeDescriptorHeap: true);
        this._dxgiSwapChain?.Dispose();
        this._framebuffer?.Dispose();
        this._colorTexture?.Dispose();
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

        if (this._framebuffer != null && this._framebuffer.Width == width && this._framebuffer.Height == height) {
            return;
        }

        lock (this._gd.CommandQueueLock) {
            if (this._framebuffer != null && this._framebuffer.Width == width && this._framebuffer.Height == height) {
                return;
            }

            this.ResizeImmediate(width, height);
        }
    }

    /// <summary>
    /// Executes the present logic for this backend.
    /// </summary>
    internal void Present() {
        if (this._hasNativeSwapchain) {
            uint syncInterval = this._presentSyncInterval;
            PresentFlags presentFlags = this._presentFlags;
            this.PresentNoAlloc(syncInterval, presentFlags);
            this._currentBackBufferIndex ^= 1;
        }
    }

    /// <summary>
    /// Refreshes cached DXGI present parameters after sync or tearing settings change.
    /// </summary>
    private void RefreshPresentParameters() {
        this._presentSyncInterval = this._syncToVerticalBlank ? 1u : 0u;
        this._presentFlags = !this._syncToVerticalBlank && this._allowTearing && this._canTear
            ? PresentFlags.AllowTearing
            : PresentFlags.None;
    }

    /// <summary>
    /// Presents the swapchain without going through the managed COM wrapper.
    /// </summary>
    /// <param name="syncInterval">The vertical sync interval.</param>
    /// <param name="presentFlags">The DXGI present flags.</param>
    private unsafe void PresentNoAlloc(uint syncInterval, PresentFlags presentFlags) {
        int result = this._present((void*)this._dxgiSwapChainPointer, syncInterval, (uint)presentFlags);
        if (result < 0) {
            new Result(result).CheckError();
        }
    }

    /// <summary>
    /// Queries the current back buffer index without going through the managed COM wrapper.
    /// IDXGISwapChain3::GetCurrentBackBufferIndex is at vtable index 36.
    /// </summary>
    private unsafe uint GetCurrentBackBufferIndexNoAlloc() {
        return this._getCurrentBackBufferIndex((void*)this._dxgiSwapChainPointer);
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

        if (depthFormat != null) {
            TextureDescription depthDesc = TextureDescription.Texture2D(width, height, 1, 1, depthFormat.Value, TextureUsage.DepthStencil);
            this._depthTexture = this._gd.ResourceFactory.CreateTexture(depthDesc);
        }
        else {
            this._depthTexture = null;
        }

        if (this._hasNativeSwapchain) {
            this._colorTexture = null;
            this._framebuffer = new D3D12SwapchainFramebuffer(this._gd, this, width, height, colorFormat, (D3D12Texture)this._depthTexture);
        }
        else {
            TextureDescription colorDesc = TextureDescription.Texture2D(width, height, 1, 1, colorFormat, TextureUsage.RenderTarget | TextureUsage.Sampled);
            this._colorTexture = this._gd.ResourceFactory.CreateTexture(colorDesc);
            FramebufferDescription fbDesc = new(this._depthTexture, this._colorTexture);
            this._framebuffer = this._gd.ResourceFactory.CreateFramebuffer(fbDesc);
        }
    }

    /// <summary>
    /// Executes the resize immediately.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    private void ResizeImmediate(uint width, uint height) {
        if (this._framebuffer != null && this._framebuffer.Width == width && this._framebuffer.Height == height) {
            return;
        }

        this._gd.WaitForIdle();

        bool useDepth = this._depthTexture != null;
        PixelFormat? depthFormat = useDepth ? this._depthTexture.Format : null;
        bool srgb = this._framebuffer.OutputDescription.ColorAttachments[0].Format == PixelFormat.B8G8R8A8UNormSRgb
            || this._framebuffer.OutputDescription.ColorAttachments[0].Format == PixelFormat.R8G8B8A8UNormSRgb;

        this._colorTexture?.Dispose();
        this._colorTexture = null;
        this._depthTexture?.Dispose();
        if (this._hasNativeSwapchain) {
            this.DisposeNativeResources(disposeDescriptorHeap: false);
            this._dxgiSwapChain.ResizeBuffers((uint)SwapchainBufferCount, width, height, this._nativeColorFormat, this.GetSwapChainFlags());
            this.CreateNativeRenderTargets();
        }
        else {
            this._framebuffer.Dispose();
        }

        this.TryResolveAttachmentSize(ref width, ref height);
        this.CreateOrUpdateAttachments(width, height, depthFormat, srgb);
    }

    /// <summary>
    /// Creates or updates the attachments instance used by this backend.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="depthFormat">The depth format value used by this operation.</param>
    /// <param name="srgb">The srgb value used by this operation.</param>
    private void CreateOrUpdateAttachments(uint width, uint height, PixelFormat? depthFormat, bool srgb) {
        if (!this._hasNativeSwapchain) {
            this.CreateAttachments(width, height, depthFormat, srgb);
            return;
        }

        PixelFormat colorFormat = srgb ? PixelFormat.B8G8R8A8UNormSRgb : PixelFormat.B8G8R8A8UNorm;
        if (depthFormat != null) {
            TextureDescription depthDesc = TextureDescription.Texture2D(width, height, 1, 1, depthFormat.Value, TextureUsage.DepthStencil);
            this._depthTexture = this._gd.ResourceFactory.CreateTexture(depthDesc);
        }
        else {
            this._depthTexture = null;
        }

        if (this._framebuffer is D3D12SwapchainFramebuffer swapchainFramebuffer) {
            swapchainFramebuffer.Resize(width, height, colorFormat, (D3D12Texture)this._depthTexture);
        }
        else {
            this._framebuffer = new D3D12SwapchainFramebuffer(this._gd, this, width, height, colorFormat, (D3D12Texture)this._depthTexture);
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
            BufferCount = (uint)SwapchainBufferCount,
            BufferUsage = Usage.RenderTargetOutput,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.FlipDiscard,
            Scaling = Scaling.Stretch,
            Stereo = false,
            AlphaMode = AlphaMode.Ignore,
            Flags = flags
        };

        IDXGISwapChain1 swapChain1 = this._gd.DxgiFactory.CreateSwapChainForHwnd(this._gd.CommandQueue, win32Source.Hwnd, swapChainDesc);
        this._dxgiSwapChain = swapChain1.QueryInterface<IDXGISwapChain3>();
        this.CacheSwapchainHotpathCalls();
        swapChain1.Dispose();
        this.CreateNativeRenderTargets();
        return true;
    }

    /// <summary>
    /// Caches DXGI swapchain function pointers used every presented frame.
    /// </summary>
    private unsafe void CacheSwapchainHotpathCalls() {
        this._dxgiSwapChainPointer = this._dxgiSwapChain.NativePointer;
        void** vtbl = *(void***)this._dxgiSwapChainPointer;
        this._present = (delegate* unmanaged[Stdcall]<void*, uint, uint, int>)vtbl[8];
        this._getCurrentBackBufferIndex = (delegate* unmanaged[Stdcall]<void*, uint>)vtbl[36];
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

        index = this._currentBackBufferIndex;
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
        if (this._rtvDescriptors == null) {
            this._rtvDescriptorSize = (int)this._gd.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
            this._rtvDescriptors = this._gd.RtvDescriptorAllocator.Allocate((uint)SwapchainBufferCount);
        }

        this._backBufferResources = new ID3D12Resource[SwapchainBufferCount];
        this._backBufferRtvs = new CpuDescriptorHandle[SwapchainBufferCount];
        this._backBufferStates = new ResourceStates[SwapchainBufferCount];

        CpuDescriptorHandle handle = this._rtvDescriptors.Handle;
        for (int i = 0; i < SwapchainBufferCount; i++) {
            ID3D12Resource buffer = this._dxgiSwapChain.GetBuffer<ID3D12Resource>((uint)i);
            this._backBufferResources[i] = buffer;
            this._backBufferRtvs[i] = handle + i * this._rtvDescriptorSize;
            this._backBufferStates[i] = ResourceStates.Present;
            this._gd.Device.CreateRenderTargetView(buffer, this.CreateBackBufferRenderTargetViewDescription(), this._backBufferRtvs[i]);
        }

        this._currentBackBufferIndex = (int)this.GetCurrentBackBufferIndexNoAlloc();
        unchecked {
            this._backBufferVersion++;
        }
    }

    /// <summary>
    /// Gets the native back-buffer generation used to invalidate command-list-local swapchain caches.
    /// </summary>
    internal uint BackBufferVersion => this._backBufferVersion;

    /// <summary>
    /// Creates the render-target view description used by native back buffers.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private RenderTargetViewDescription CreateBackBufferRenderTargetViewDescription() {
        return new RenderTargetViewDescription {
            Format = this._nativeRtvFormat,
            ViewDimension = RenderTargetViewDimension.Texture2D,
            Texture2D = new Texture2DRenderTargetView {
                MipSlice = 0,
                PlaneSlice = 0
            }
        };
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

        lock (this._gd.CommandQueueLock) {
            this._gd.WaitForIdle();

            bool useDepth = this._depthTexture != null;
            PixelFormat? depthFormat = useDepth ? this._depthTexture.Format : null;
            bool srgb = this._framebuffer.OutputDescription.ColorAttachments[0].Format == PixelFormat.B8G8R8A8UNormSRgb
                || this._framebuffer.OutputDescription.ColorAttachments[0].Format == PixelFormat.R8G8B8A8UNormSRgb;

            this._depthTexture?.Dispose();
            this._depthTexture = null;

            this.DisposeNativeResources(disposeDescriptorHeap: false);
            this._dxgiSwapChain.ResizeBuffers((uint)SwapchainBufferCount, width, height, this._nativeColorFormat, this.GetSwapChainFlags());
            this.CreateNativeRenderTargets();

            this.TryResolveAttachmentSize(ref width, ref height);
            this.CreateOrUpdateAttachments(width, height, depthFormat, srgb);
        }
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
        SwapChainFlags flags = SwapChainFlags.None;
        if (this._canTear) {
            flags |= SwapChainFlags.AllowTearing;
        }

        return flags;
    }

    /// <summary>
    /// Executes the dispose native resources logic for this backend.
    /// </summary>
    private void DisposeNativeResources(bool disposeDescriptorHeap) {
        if (this._backBufferResources != null) {
            foreach (ID3D12Resource resource in this._backBufferResources) {
                resource?.Dispose();
            }
        }

        if (disposeDescriptorHeap) {
            this._rtvDescriptors?.Dispose();
            this._rtvDescriptors = null;
            this._rtvDescriptorSize = 0;
        }

        this._backBufferResources = null;
        this._backBufferRtvs = null;
        this._backBufferStates = null;
    }

}
