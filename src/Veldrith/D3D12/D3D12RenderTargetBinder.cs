using Vortice.Direct3D12;

#if !VELDRID_D3D12_PERF
#pragma warning disable CS0162
#endif

namespace Veldrith.D3D12;

/// <summary>
/// Binds D3D12 render targets and depth-stencil targets for a command list.
/// </summary>
internal sealed class D3D12RenderTargetBinder {

    /// <summary>
    /// Stores the maximum number of render targets supported by D3D12.
    /// </summary>
    private const int MaxRenderTargetCount = 8;

    /// <summary>
    /// Stores the command list that receives transitions and output-merger commands.
    /// </summary>
    private readonly D3D12CommandList _commandList;

    /// <summary>
    /// Tracks current swapchain back-buffer state for this command-list recording.
    /// </summary>
    private readonly D3D12SwapchainBackBufferTracker _swapchainBackBuffer;

    /// <summary>
    /// Stores optional performance counters updated by render-target binding.
    /// </summary>
    private readonly D3D12CommandListPerfTracker _perf;

    /// <summary>
    /// Stores the last render target descriptors passed to OMSetRenderTargets.
    /// </summary>
    private readonly CpuDescriptorHandle[] _boundRtvs = new CpuDescriptorHandle[MaxRenderTargetCount];

    /// <summary>
    /// Stores the last depth-stencil descriptor passed to OMSetRenderTargets.
    /// </summary>
    private CpuDescriptorHandle _boundDsv;

    /// <summary>
    /// Stores the number of render-target descriptors currently cached.
    /// </summary>
    private uint _boundRtvCount;

    /// <summary>
    /// Stores whether the cached render-target descriptors were bound as one contiguous descriptor range.
    /// </summary>
    private bool _boundRtvsAreSingleDescriptorRange;

    /// <summary>
    /// Stores whether the cached output-merger binding includes a depth-stencil descriptor.
    /// </summary>
    private bool _boundHasDepthStencil;

    /// <summary>
    /// Gets whether the output-merger cache contains valid data.
    /// </summary>
    private bool _boundRenderTargetsValid;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12RenderTargetBinder" /> class.
    /// </summary>
    /// <param name="commandList">The command list that receives D3D12 commands.</param>
    /// <param name="swapchainBackBuffer">The swapchain back-buffer tracker for this command list.</param>
    /// <param name="perf">The optional performance tracker updated by the binder.</param>
    internal D3D12RenderTargetBinder(D3D12CommandList commandList, D3D12SwapchainBackBufferTracker swapchainBackBuffer, D3D12CommandListPerfTracker perf) {
        this._commandList = commandList;
        this._swapchainBackBuffer = swapchainBackBuffer;
        this._perf = perf;
    }

    /// <summary>
    /// Clears the cached output-merger state for a new command-list recording.
    /// </summary>
    internal void Reset() {
        this._boundRenderTargetsValid = false;
        this._boundRtvCount = 0;
        this._boundHasDepthStencil = false;
        this._boundRtvsAreSingleDescriptorRange = false;
        this._boundDsv = default;
    }

    /// <summary>
    /// Transitions framebuffer attachments and binds their render-target/depth-stencil descriptors.
    /// </summary>
    /// <param name="framebuffer">The framebuffer to bind.</param>
    internal void SetFramebuffer(Framebuffer framebuffer) {
        if (framebuffer is D3D12SwapchainFramebuffer swapchainFramebuffer
            && this._swapchainBackBuffer.TryGetBackBuffer(swapchainFramebuffer, out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int backBufferIndex, out ResourceStates currentState)) {
            this.BindSwapchainFramebuffer(swapchainFramebuffer, backBuffer, rtv, backBufferIndex, currentState);
            return;
        }

        this.BindFramebuffer(Util.AssertSubtype<Framebuffer, D3D12Framebuffer>(framebuffer));
    }

    /// <summary>
    /// Binds a swapchain framebuffer using the current back buffer.
    /// </summary>
    /// <param name="framebuffer">The swapchain framebuffer.</param>
    /// <param name="backBuffer">The current swapchain back-buffer resource.</param>
    /// <param name="rtv">The current back-buffer RTV descriptor.</param>
    /// <param name="backBufferIndex">The current back-buffer index.</param>
    /// <param name="currentState">The current tracked back-buffer state.</param>
    private void BindSwapchainFramebuffer(D3D12SwapchainFramebuffer framebuffer, ID3D12Resource backBuffer, CpuDescriptorHandle rtv, int backBufferIndex, ResourceStates currentState) {
        this._commandList.TransitionForInternalUse(backBuffer, currentState, ResourceStates.RenderTarget);
        framebuffer.Swapchain.SetBackBufferState(backBufferIndex, ResourceStates.RenderTarget);
        this._swapchainBackBuffer.MarkBackBufferState(backBufferIndex, ResourceStates.RenderTarget);

        if (framebuffer.DepthTargetTexture != null) {
            this._commandList.TransitionTextureForInternalUse(framebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
        }

        if (framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv)) {
            this.BindSingleRenderTarget(rtv, true, dsv);
        }
        else {
            this.BindSingleRenderTarget(rtv, false, default);
        }
    }

    /// <summary>
    /// Binds an offscreen framebuffer.
    /// </summary>
    /// <param name="framebuffer">The framebuffer to bind.</param>
    private void BindFramebuffer(D3D12Framebuffer framebuffer) {
        foreach (D3D12Texture colorTexture in framebuffer.ColorTargetTextures) {
            if (colorTexture != null) {
                this._commandList.TransitionTextureForInternalUse(colorTexture, ResourceStates.RenderTarget);
            }
        }

        if (framebuffer.DepthTargetTexture != null) {
            this._commandList.TransitionTextureForInternalUse(framebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
        }

        if (!framebuffer.TryGetColorTargetViews(out CpuDescriptorHandle[] rtvs)) {
            if (framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle depthOnlyDsv)) {
                this.BindDepthOnly(depthOnlyDsv);
            }

            return;
        }

        if (framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv)) {
            this.BindRenderTargetArray(rtvs, true, dsv);
        }
        else {
            this.BindRenderTargetArray(rtvs, false, default);
        }
    }

    /// <summary>
    /// Binds a single render-target descriptor, skipping redundant output-merger commands.
    /// </summary>
    /// <param name="rtv">The render-target descriptor.</param>
    /// <param name="hasDepthStencil">Whether a depth-stencil descriptor is present.</param>
    /// <param name="dsv">The depth-stencil descriptor.</param>
    private void BindSingleRenderTarget(CpuDescriptorHandle rtv, bool hasDepthStencil, CpuDescriptorHandle dsv) {
        if (this.IsSameRenderTargetState(1, true, hasDepthStencil, dsv)
            && this._boundRtvs[0].Ptr == rtv.Ptr) {
            this.RecordRenderTargetBindSkipped();
            return;
        }

        this._commandList.OMSetRenderTargetsNoAlloc(1, rtv, hasDepthStencil, dsv);
        this.CacheSingleRenderTarget(rtv, hasDepthStencil, dsv);
        this.RecordRenderTargetBind();
    }

    /// <summary>
    /// Binds only a depth-stencil descriptor, skipping redundant output-merger commands.
    /// </summary>
    /// <param name="dsv">The depth-stencil descriptor.</param>
    private void BindDepthOnly(CpuDescriptorHandle dsv) {
        if (this.IsSameRenderTargetState(0, true, true, dsv)) {
            this.RecordRenderTargetBindSkipped();
            return;
        }

        this._commandList.OMSetRenderTargetsNoAlloc(0, default, true, dsv);
        this.CacheRenderTargets(null, 0, true, true, dsv);
        this.RecordRenderTargetBind();
    }

    /// <summary>
    /// Binds an array of render-target descriptors, skipping redundant output-merger commands.
    /// </summary>
    /// <param name="rtvs">The render-target descriptor array.</param>
    /// <param name="hasDepthStencil">Whether a depth-stencil descriptor is present.</param>
    /// <param name="dsv">The depth-stencil descriptor.</param>
    private void BindRenderTargetArray(CpuDescriptorHandle[] rtvs, bool hasDepthStencil, CpuDescriptorHandle dsv) {
        uint rtvCount = (uint)rtvs.Length;
        if (this.IsSameRenderTargetState(rtvCount, false, hasDepthStencil, dsv)
            && this.AreSameRenderTargets(rtvs, rtvCount)) {
            this.RecordRenderTargetBindSkipped();
            return;
        }

        this._commandList.OMSetRenderTargetsArrayNoAlloc(rtvs, hasDepthStencil, dsv);
        this.CacheRenderTargets(rtvs, rtvCount, false, hasDepthStencil, dsv);
        this.RecordRenderTargetBind();
    }

    /// <summary>
    /// Checks whether non-RTV output-merger cache fields match the requested state.
    /// </summary>
    /// <param name="rtvCount">The number of render-target descriptors.</param>
    /// <param name="rtvsAreSingleDescriptorRange">Whether descriptors are represented as one contiguous descriptor range.</param>
    /// <param name="hasDepthStencil">Whether a depth-stencil descriptor is present.</param>
    /// <param name="dsv">The depth-stencil descriptor.</param>
    /// <returns><see langword="true" /> when the cached state matches.</returns>
    private bool IsSameRenderTargetState(uint rtvCount, bool rtvsAreSingleDescriptorRange, bool hasDepthStencil, CpuDescriptorHandle dsv) {
        return this._boundRenderTargetsValid
               && this._boundRtvCount == rtvCount
               && this._boundRtvsAreSingleDescriptorRange == rtvsAreSingleDescriptorRange
               && this._boundHasDepthStencil == hasDepthStencil
               && (!hasDepthStencil || this._boundDsv.Ptr == dsv.Ptr);
    }

    /// <summary>
    /// Checks whether cached render-target descriptors match a requested descriptor array.
    /// </summary>
    /// <param name="rtvs">The render-target descriptor array.</param>
    /// <param name="rtvCount">The number of descriptors to compare.</param>
    /// <returns><see langword="true" /> when all descriptors match.</returns>
    private bool AreSameRenderTargets(CpuDescriptorHandle[] rtvs, uint rtvCount) {
        for (uint i = 0; i < rtvCount; i++) {
            if (this._boundRtvs[i].Ptr != rtvs[i].Ptr) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Stores one render-target descriptor in the output-merger cache.
    /// </summary>
    /// <param name="rtv">The render-target descriptor.</param>
    /// <param name="hasDepthStencil">Whether a depth-stencil descriptor is present.</param>
    /// <param name="dsv">The depth-stencil descriptor.</param>
    private void CacheSingleRenderTarget(CpuDescriptorHandle rtv, bool hasDepthStencil, CpuDescriptorHandle dsv) {
        this._boundRtvs[0] = rtv;
        this.CacheRenderTargets(null, 1, true, hasDepthStencil, dsv);
    }

    /// <summary>
    /// Stores output-merger render-target state in the local cache.
    /// </summary>
    /// <param name="rtvs">The render-target descriptors, or <see langword="null" /> when already stored.</param>
    /// <param name="rtvCount">The number of render-target descriptors.</param>
    /// <param name="rtvsAreSingleDescriptorRange">Whether descriptors were bound as one contiguous descriptor range.</param>
    /// <param name="hasDepthStencil">Whether a depth-stencil descriptor is present.</param>
    /// <param name="dsv">The depth-stencil descriptor.</param>
    private void CacheRenderTargets(CpuDescriptorHandle[] rtvs, uint rtvCount, bool rtvsAreSingleDescriptorRange, bool hasDepthStencil, CpuDescriptorHandle dsv) {
        if (rtvs != null) {
            for (uint i = 0; i < rtvCount; i++) {
                this._boundRtvs[i] = rtvs[i];
            }
        }

        this._boundRtvCount = rtvCount;
        this._boundRtvsAreSingleDescriptorRange = rtvsAreSingleDescriptorRange;
        this._boundHasDepthStencil = hasDepthStencil;
        this._boundDsv = hasDepthStencil ? dsv : default;
        this._boundRenderTargetsValid = true;
    }

    /// <summary>
    /// Records one native output-merger render-target bind.
    /// </summary>
    private void RecordRenderTargetBind() {
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.RenderTargetBinds++;
        }
    }

    /// <summary>
    /// Records one skipped redundant output-merger render-target bind.
    /// </summary>
    private void RecordRenderTargetBindSkipped() {
        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.RenderTargetBindSkips++;
        }
    }
}
