using System.Collections.Generic;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12SwapchainFramebuffer.
/// </summary>
internal sealed class D3D12SwapchainFramebuffer : Framebuffer {

    /// <summary>
    /// Stores the color target metadata used by this instance.
    /// </summary>
    private FramebufferAttachment[] _colorTargets;

    /// <summary>
    /// Stores the depth target metadata used by this instance.
    /// </summary>
    private FramebufferAttachment? _depthTarget;

    /// <summary>
    /// Stores the depth stencil view value used during command execution.
    /// </summary>
    private CpuDescriptorHandle? _depthStencilView;

    /// <summary>
    /// Stores the dsv heap state used by this instance.
    /// </summary>
    private ID3D12DescriptorHeap _dsvHeap;

    /// <summary>
    /// Stores the output description state used by this instance.
    /// </summary>
    private OutputDescription _outputDescription;

    /// <summary>
    /// Stores the width value used by this instance.
    /// </summary>
    private uint _width;

    /// <summary>
    /// Stores the height value used by this instance.
    /// </summary>
    private uint _height;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the graphics device that owns this framebuffer.
    /// </summary>
    private readonly D3D12GraphicsDevice gd;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12SwapchainFramebuffer" /> class.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="swapchain">The swapchain used by this operation.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="colorFormat">The color format value used by this operation.</param>
    /// <param name="depthTexture">The depth texture value used by this operation.</param>
    public D3D12SwapchainFramebuffer(D3D12GraphicsDevice gd, D3D12Swapchain swapchain, uint width, uint height, PixelFormat colorFormat, D3D12Texture depthTexture) {
        this.gd = gd;
        this.Swapchain = swapchain;
        this.SetAttachments(width, height, colorFormat, depthTexture);
    }

    /// <summary>
    /// Gets or sets ColorTargets.
    /// </summary>
    public override IReadOnlyList<FramebufferAttachment> ColorTargets => this._colorTargets;

    /// <summary>
    /// Gets or sets DepthTarget.
    /// </summary>
    public override FramebufferAttachment? DepthTarget => this._depthTarget;

    /// <summary>
    /// Gets or sets OutputDescription.
    /// </summary>
    public override OutputDescription OutputDescription => this._outputDescription;

    /// <summary>
    /// Gets or sets Width.
    /// </summary>
    public override uint Width => this._width;

    /// <summary>
    /// Gets or sets Height.
    /// </summary>
    public override uint Height => this._height;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Gets or sets Swapchain.
    /// </summary>
    public D3D12Swapchain Swapchain { get; }

    /// <summary>
    /// Gets or sets DepthTargetTexture.
    /// </summary>
    internal D3D12Texture DepthTargetTexture { get; private set; }

    /// <summary>
    /// Attempts to get depth stencil view and reports whether it succeeded.
    /// </summary>
    /// <param name="handle">The handle value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool TryGetDepthStencilView(out CpuDescriptorHandle handle) {
        if (!this._depthStencilView.HasValue || this.DepthTargetTexture?.NativeTexture == null) {
            handle = default;
            return false;
        }

        handle = this._depthStencilView.Value;
        return true;
    }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (this._disposed) {
            return;
        }

        this.ReleaseAttachmentMetadata();

        this._disposed = true;
    }

    /// <summary>
    /// Updates the framebuffer metadata after the native swapchain has resized.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="colorFormat">The color format value used by this operation.</param>
    /// <param name="depthTexture">The depth texture value used by this operation.</param>
    internal void Resize(uint width, uint height, PixelFormat colorFormat, D3D12Texture depthTexture) {
        this.ReleaseAttachmentMetadata();
        this.SetAttachments(width, height, colorFormat, depthTexture);
    }

    /// <summary>
    /// Sets the attachment metadata used by this framebuffer.
    /// </summary>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="colorFormat">The color format value used by this operation.</param>
    /// <param name="depthTexture">The depth texture value used by this operation.</param>
    private void SetAttachments(uint width, uint height, PixelFormat colorFormat, D3D12Texture depthTexture) {
        this._width = width;
        this._height = height;
        this._colorTargets = new[] { new FramebufferAttachment(new D3D12SwapchainTexture(width, height, colorFormat), 0) };
        this.DepthTargetTexture = depthTexture;

        if (depthTexture != null) {
            this._depthTarget = new FramebufferAttachment(depthTexture, 0);
            this._dsvHeap = this.gd.Device.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.DepthStencilView, 1));
            CpuDescriptorHandle dsv = this._dsvHeap.GetCPUDescriptorHandleForHeapStart();
            this._depthStencilView = dsv;
            DepthStencilViewDescription dsvDescription = D3D12Framebuffer.CreateDepthStencilViewDescription(depthTexture, this._depthTarget.Value);
            this.gd.Device.CreateDepthStencilView(depthTexture.NativeTexture, dsvDescription, dsv);
        }
        else {
            this._depthTarget = null;
            this._depthStencilView = null;
            this._dsvHeap = null;
        }

        this._outputDescription = OutputDescription.CreateFromFramebuffer(this);
    }

    /// <summary>
    /// Releases metadata-only attachment resources owned by this framebuffer.
    /// </summary>
    private void ReleaseAttachmentMetadata() {
        this.gd.ReleaseAfterLastSubmission(this._dsvHeap);
        this._dsvHeap = null;
        this._depthStencilView = null;

        if (this._colorTargets != null) {
            foreach (FramebufferAttachment colorTarget in this._colorTargets) {
                colorTarget.Target.Dispose();
            }
        }

        this._colorTargets = null;
    }
}
