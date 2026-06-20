using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Plans D3D12 framebuffer clear operations and records the required attachment transitions.
/// </summary>
internal sealed class D3D12ClearPlanner {

    /// <summary>
    /// Stores the command list that receives clear and transition commands.
    /// </summary>
    private readonly D3D12CommandList _commandList;

    /// <summary>
    /// Tracks current swapchain back-buffer state for this command-list recording.
    /// </summary>
    private readonly D3D12SwapchainBackBufferTracker _swapchainBackBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ClearPlanner" /> class.
    /// </summary>
    /// <param name="commandList">The command list that receives D3D12 commands.</param>
    /// <param name="swapchainBackBuffer">The swapchain back-buffer tracker for this command list.</param>
    internal D3D12ClearPlanner(D3D12CommandList commandList, D3D12SwapchainBackBufferTracker swapchainBackBuffer) {
        this._commandList = commandList;
        this._swapchainBackBuffer = swapchainBackBuffer;
    }

    /// <summary>
    /// Clears a color attachment on the currently bound framebuffer.
    /// </summary>
    /// <param name="framebuffer">The framebuffer whose color attachment should be cleared.</param>
    /// <param name="index">The zero-based color attachment index.</param>
    /// <param name="clearColor">The clear color.</param>
    internal void ClearColorTarget(Framebuffer framebuffer, uint index, RgbaFloat clearColor) {
        this._commandList.FlushPendingUavBarrierForInternalUse();
        if (framebuffer is D3D12SwapchainFramebuffer swapchainFramebuffer
            && this._swapchainBackBuffer.TryGetBackBuffer(swapchainFramebuffer, out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int backBufferIndex, out ResourceStates currentState)) {
            this.ClearSwapchainColorTarget(swapchainFramebuffer, backBuffer, rtv, backBufferIndex, currentState, clearColor);
            return;
        }

        if (framebuffer is D3D12Framebuffer d3d12Framebuffer
            && d3d12Framebuffer.TryGetColorTargetView(index, out CpuDescriptorHandle offscreenRtv)) {
            this.ClearFramebufferColorTarget(d3d12Framebuffer, index, offscreenRtv, clearColor);
        }
    }

    /// <summary>
    /// Clears the depth/stencil attachment on the currently bound framebuffer.
    /// </summary>
    /// <param name="framebuffer">The framebuffer whose depth/stencil attachment should be cleared.</param>
    /// <param name="depth">The depth clear value.</param>
    /// <param name="stencil">The stencil clear value.</param>
    internal void ClearDepthStencil(Framebuffer framebuffer, float depth, byte stencil) {
        this._commandList.FlushPendingUavBarrierForInternalUse();
        if (framebuffer is D3D12SwapchainFramebuffer swapchainFramebuffer) {
            this.ClearSwapchainDepthStencil(swapchainFramebuffer, depth, stencil);
            return;
        }

        if (framebuffer is D3D12Framebuffer d3d12Framebuffer) {
            this.ClearFramebufferDepthStencil(d3d12Framebuffer, depth, stencil);
        }
    }

    /// <summary>
    /// Clears the current swapchain back buffer.
    /// </summary>
    /// <param name="framebuffer">The swapchain framebuffer.</param>
    /// <param name="backBuffer">The native back-buffer resource.</param>
    /// <param name="rtv">The current back-buffer render-target view.</param>
    /// <param name="backBufferIndex">The current back-buffer index.</param>
    /// <param name="currentState">The tracked current back-buffer state.</param>
    /// <param name="clearColor">The clear color.</param>
    private void ClearSwapchainColorTarget(D3D12SwapchainFramebuffer framebuffer, ID3D12Resource backBuffer, CpuDescriptorHandle rtv, int backBufferIndex, ResourceStates currentState, RgbaFloat clearColor) {
        this._commandList.TransitionForInternalUse(backBuffer, currentState, ResourceStates.RenderTarget);
        framebuffer.Swapchain.SetBackBufferState(backBufferIndex, ResourceStates.RenderTarget);
        this._swapchainBackBuffer.MarkBackBufferState(backBufferIndex, ResourceStates.RenderTarget);
        this._commandList.FlushPendingBarriersForInternalUse();
        this._commandList.ClearRenderTargetViewNoAllocForInternalUse(rtv, clearColor.R, clearColor.G, clearColor.B, clearColor.A);
    }

    /// <summary>
    /// Clears an offscreen framebuffer color attachment.
    /// </summary>
    /// <param name="framebuffer">The offscreen framebuffer.</param>
    /// <param name="index">The zero-based color attachment index.</param>
    /// <param name="rtv">The render-target view to clear.</param>
    /// <param name="clearColor">The clear color.</param>
    private void ClearFramebufferColorTarget(D3D12Framebuffer framebuffer, uint index, CpuDescriptorHandle rtv, RgbaFloat clearColor) {
        if (index < framebuffer.ColorTargetTextures.Length) {
            D3D12Texture colorTexture = framebuffer.ColorTargetTextures[(int)index];
            if (colorTexture != null) {
                this._commandList.TransitionTextureForInternalUse(colorTexture, ResourceStates.RenderTarget);
            }
        }

        this._commandList.FlushPendingBarriersForInternalUse();
        this._commandList.ClearRenderTargetViewNoAllocForInternalUse(rtv, clearColor.R, clearColor.G, clearColor.B, clearColor.A);
    }

    /// <summary>
    /// Clears a swapchain depth/stencil attachment.
    /// </summary>
    /// <param name="framebuffer">The swapchain framebuffer.</param>
    /// <param name="depth">The depth clear value.</param>
    /// <param name="stencil">The stencil clear value.</param>
    private void ClearSwapchainDepthStencil(D3D12SwapchainFramebuffer framebuffer, float depth, byte stencil) {
        if (framebuffer.DepthTargetTexture == null) {
            return;
        }

        this._commandList.TransitionTextureForInternalUse(framebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
        if (!framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv)) {
            return;
        }

        uint clearFlags = GetDepthStencilClearFlags(framebuffer.DepthTargetTexture);
        this._commandList.FlushPendingBarriersForInternalUse();
        this._commandList.ClearDepthStencilViewNoAllocForInternalUse(dsv, clearFlags, depth, stencil);
    }

    /// <summary>
    /// Clears an offscreen framebuffer depth/stencil attachment.
    /// </summary>
    /// <param name="framebuffer">The offscreen framebuffer.</param>
    /// <param name="depth">The depth clear value.</param>
    /// <param name="stencil">The stencil clear value.</param>
    private void ClearFramebufferDepthStencil(D3D12Framebuffer framebuffer, float depth, byte stencil) {
        if (framebuffer.DepthTargetTexture == null) {
            return;
        }

        this._commandList.TransitionTextureForInternalUse(framebuffer.DepthTargetTexture, ResourceStates.DepthWrite);
        if (!framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv)) {
            return;
        }

        uint clearFlags = GetDepthStencilClearFlags(framebuffer.DepthTargetTexture);
        this._commandList.FlushPendingBarriersForInternalUse();
        this._commandList.ClearDepthStencilViewNoAllocForInternalUse(dsv, clearFlags, depth, stencil);
    }

    /// <summary>
    /// Gets the native D3D12 clear flags required by a depth/stencil texture.
    /// </summary>
    /// <param name="depthTexture">The depth/stencil texture to inspect.</param>
    /// <returns>The native D3D12 clear flags.</returns>
    private static uint GetDepthStencilClearFlags(D3D12Texture depthTexture) {
        ClearFlags clearFlags = ClearFlags.Depth;
        if (FormatHelpers.IsStencilFormat(depthTexture.Format)) {
            clearFlags |= ClearFlags.Stencil;
        }

        return (uint)clearFlags;
    }
}
