using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Veldrith.D3D12;

/// <summary>
/// Tracks native D3D12 render-pass lifetime for draw commands.
/// </summary>
internal sealed class D3D12RenderPassTracker {

    /// <summary>
    /// Stores the maximum number of render targets supported by D3D12.
    /// </summary>
    private const int MaxRenderTargetCount = 8;

    /// <summary>
    /// Stores the command list that receives render-pass commands.
    /// </summary>
    private readonly D3D12CommandList _commandList;

    /// <summary>
    /// Tracks current swapchain back-buffer state for this command-list recording.
    /// </summary>
    private readonly D3D12SwapchainBackBufferTracker _swapchainBackBuffer;

    /// <summary>
    /// Reuses render-target descriptions for BeginRenderPass.
    /// </summary>
    private readonly RenderPassRenderTargetDescription[] _renderTargets = new RenderPassRenderTargetDescription[MaxRenderTargetCount];

    /// <summary>
    /// Stores queued color clears that can be folded into BeginRenderPass.
    /// </summary>
    private readonly ClearValue[] _colorClearValues = new ClearValue[MaxRenderTargetCount];

    /// <summary>
    /// Tracks which color attachments have queued clear values.
    /// </summary>
    private readonly bool[] _validColorClearValues = new bool[MaxRenderTargetCount];

    /// <summary>
    /// Stores the queued depth/stencil clear that can be folded into BeginRenderPass.
    /// </summary>
    private ClearValue _depthClearValue;

    /// <summary>
    /// Stores the framebuffer that owns queued clear values.
    /// </summary>
    private Framebuffer _queuedClearFramebuffer;

    /// <summary>
    /// Tracks whether a depth/stencil clear is queued.
    /// </summary>
    private bool _hasDepthClearValue;

    /// <summary>
    /// Tracks whether a native render pass is active.
    /// </summary>
    private bool _active;

    /// <summary>
    /// Gets whether a native D3D12 render pass is active.
    /// </summary>
    internal bool Active => this._active;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12RenderPassTracker" /> class.
    /// </summary>
    /// <param name="commandList">The command list that receives render-pass commands.</param>
    /// <param name="swapchainBackBuffer">The swapchain back-buffer tracker for this command list.</param>
    internal D3D12RenderPassTracker(D3D12CommandList commandList, D3D12SwapchainBackBufferTracker swapchainBackBuffer) {
        this._commandList = commandList;
        this._swapchainBackBuffer = swapchainBackBuffer;
    }

    /// <summary>
    /// Clears per-recording render-pass state.
    /// </summary>
    internal void Reset() {
        this._active = false;
        this.ClearQueuedClears();
        this._queuedClearFramebuffer = null;
    }

    /// <summary>
    /// Begins a native D3D12 render pass for the current framebuffer when supported.
    /// </summary>
    /// <param name="framebuffer">The current framebuffer.</param>
    internal void BeginDrawPass(Framebuffer framebuffer) {
        if (this._active || this._commandList.NativeCommandList4 == null || framebuffer == null) {
            return;
        }

        if (!this.TryBuildRenderPassDescriptions(framebuffer, out uint renderTargetCount, out RenderPassDepthStencilDescription? depthStencil)) {
            return;
        }

        this._commandList.NativeCommandList4.BeginRenderPass(renderTargetCount, this._renderTargets, depthStencil, RenderPassFlags.None);
        this._active = true;
        if (ReferenceEquals(this._queuedClearFramebuffer, framebuffer)) {
            this.ClearQueuedClears();
            this._queuedClearFramebuffer = null;
        }
    }

    /// <summary>
    /// Ends the current native D3D12 render pass when one is active.
    /// </summary>
    internal void EndPass() {
        if (!this._active) {
            return;
        }

        this._commandList.NativeCommandList4.EndRenderPass();
        this._active = false;
    }

    /// <summary>
    /// Queues a color clear so it can be emitted as a render-pass beginning access.
    /// </summary>
    /// <param name="framebuffer">The framebuffer whose color target should be cleared.</param>
    /// <param name="index">The zero-based color attachment index.</param>
    /// <param name="clearColor">The color clear value.</param>
    /// <returns><see langword="true" /> when the clear was queued.</returns>
    internal bool TryQueueColorClear(Framebuffer framebuffer, uint index, RgbaFloat clearColor) {
        if (this._active
            || this._commandList.NativeCommandList4 == null
            || framebuffer == null
            || index >= MaxRenderTargetCount
            || !this.CanQueueClearForFramebuffer(framebuffer)) {
            return false;
        }

        if (this._queuedClearFramebuffer != null && !ReferenceEquals(this._queuedClearFramebuffer, framebuffer)) {
            return false;
        }

        if (!this.TryGetColorClearFormat(framebuffer, index, out Vortice.DXGI.Format format)) {
            return false;
        }

        Color4 color = new(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
        this._colorClearValues[index] = new ClearValue(format, in color);
        this._validColorClearValues[index] = true;
        this._queuedClearFramebuffer = framebuffer;
        return true;
    }

    /// <summary>
    /// Queues a depth/stencil clear so it can be emitted as a render-pass beginning access.
    /// </summary>
    /// <param name="framebuffer">The framebuffer whose depth/stencil target should be cleared.</param>
    /// <param name="depth">The depth clear value.</param>
    /// <param name="stencil">The stencil clear value.</param>
    /// <returns><see langword="true" /> when the clear was queued.</returns>
    internal bool TryQueueDepthStencilClear(Framebuffer framebuffer, float depth, byte stencil) {
        if (this._active
            || this._commandList.NativeCommandList4 == null
            || framebuffer == null
            || !this.CanQueueClearForFramebuffer(framebuffer)) {
            return false;
        }

        if (this._queuedClearFramebuffer != null && !ReferenceEquals(this._queuedClearFramebuffer, framebuffer)) {
            return false;
        }

        if (!this.TryGetDepthClearFormat(framebuffer, out Vortice.DXGI.Format format)) {
            return false;
        }

        this._depthClearValue = new ClearValue(format, depth, stencil);
        this._hasDepthClearValue = true;
        this._queuedClearFramebuffer = framebuffer;
        return true;
    }

    /// <summary>
    /// Emits queued clears through the immediate clear planner when no draw consumed them.
    /// </summary>
    /// <param name="clearPlanner">The immediate clear planner used as fallback.</param>
    internal void FlushQueuedClears(D3D12ClearPlanner clearPlanner) {
        if (this._queuedClearFramebuffer == null) {
            return;
        }

        Framebuffer framebuffer = this._queuedClearFramebuffer;
        for (uint i = 0; i < MaxRenderTargetCount; i++) {
            if (!this._validColorClearValues[i]) {
                continue;
            }

            ClearValue clearValue = this._colorClearValues[i];
            Color4 color = clearValue.Color;
            clearPlanner.ClearColorTarget(framebuffer, i, new RgbaFloat(color.R, color.G, color.B, color.A));
        }

        if (this._hasDepthClearValue) {
            DepthStencilValue depthStencil = this._depthClearValue.DepthStencil;
            clearPlanner.ClearDepthStencil(framebuffer, depthStencil.Depth, depthStencil.Stencil);
        }

        this.ClearQueuedClears();
        this._queuedClearFramebuffer = null;
    }

    /// <summary>
    /// Builds render-pass descriptions for a framebuffer.
    /// </summary>
    /// <param name="framebuffer">The framebuffer to describe.</param>
    /// <param name="renderTargetCount">The number of render targets.</param>
    /// <param name="depthStencil">The optional depth-stencil description.</param>
    /// <returns><see langword="true" /> when a render pass can be started.</returns>
    private bool TryBuildRenderPassDescriptions(Framebuffer framebuffer, out uint renderTargetCount, out RenderPassDepthStencilDescription? depthStencil) {
        RenderPassBeginningAccess beginPreserve = new(RenderPassBeginningAccessType.Preserve);
        RenderPassEndingAccess endPreserve = new(RenderPassEndingAccessType.Preserve);

        if (framebuffer is D3D12SwapchainFramebuffer swapchainFramebuffer
            && this._swapchainBackBuffer.TryGetBackBuffer(swapchainFramebuffer, out _, out CpuDescriptorHandle swapchainRtv, out _, out _)) {
            RenderPassBeginningAccess colorBegin = this.GetColorBeginningAccess(framebuffer, 0, beginPreserve);
            this._renderTargets[0] = new RenderPassRenderTargetDescription(swapchainRtv, colorBegin, endPreserve);
            renderTargetCount = 1;
            depthStencil = swapchainFramebuffer.TryGetDepthStencilView(out CpuDescriptorHandle swapchainDsv)
                ? new RenderPassDepthStencilDescription(swapchainDsv, this.GetDepthBeginningAccess(framebuffer, beginPreserve), endPreserve)
                : null;
            return true;
        }

        if (framebuffer is not D3D12Framebuffer d3d12Framebuffer) {
            renderTargetCount = 0;
            depthStencil = null;
            return false;
        }

        renderTargetCount = 0;
        if (d3d12Framebuffer.TryGetColorTargetViews(out CpuDescriptorHandle[] rtvs)) {
            renderTargetCount = (uint)rtvs.Length;
            for (uint i = 0; i < renderTargetCount; i++) {
                RenderPassBeginningAccess colorBegin = this.GetColorBeginningAccess(framebuffer, i, beginPreserve);
                this._renderTargets[i] = new RenderPassRenderTargetDescription(rtvs[i], colorBegin, endPreserve);
            }
        }

        depthStencil = d3d12Framebuffer.TryGetDepthStencilView(out CpuDescriptorHandle dsv)
            ? new RenderPassDepthStencilDescription(dsv, this.GetDepthBeginningAccess(framebuffer, beginPreserve), endPreserve)
            : null;
        return renderTargetCount != 0 || depthStencil.HasValue;
    }

    /// <summary>
    /// Gets the beginning access for a color attachment.
    /// </summary>
    /// <param name="framebuffer">The framebuffer being described.</param>
    /// <param name="index">The zero-based color attachment index.</param>
    /// <param name="fallback">The access used when no clear is queued.</param>
    /// <returns>The beginning access for this color attachment.</returns>
    private RenderPassBeginningAccess GetColorBeginningAccess(Framebuffer framebuffer, uint index, RenderPassBeginningAccess fallback) {
        if (ReferenceEquals(this._queuedClearFramebuffer, framebuffer)
            && index < MaxRenderTargetCount
            && this._validColorClearValues[index]) {
            ClearValue clearValue = this._colorClearValues[index];
            return new RenderPassBeginningAccess(in clearValue);
        }

        return fallback;
    }

    /// <summary>
    /// Gets the beginning access for the depth/stencil attachment.
    /// </summary>
    /// <param name="framebuffer">The framebuffer being described.</param>
    /// <param name="fallback">The access used when no clear is queued.</param>
    /// <returns>The beginning access for this depth/stencil attachment.</returns>
    private RenderPassBeginningAccess GetDepthBeginningAccess(Framebuffer framebuffer, RenderPassBeginningAccess fallback) {
        if (ReferenceEquals(this._queuedClearFramebuffer, framebuffer) && this._hasDepthClearValue) {
            ClearValue clearValue = this._depthClearValue;
            return new RenderPassBeginningAccess(in clearValue);
        }

        return fallback;
    }

    /// <summary>
    /// Checks whether the framebuffer has native attachment descriptors for render-pass clear queuing.
    /// </summary>
    /// <param name="framebuffer">The framebuffer to inspect.</param>
    /// <returns><see langword="true" /> when clears can be queued for this framebuffer.</returns>
    private bool CanQueueClearForFramebuffer(Framebuffer framebuffer) {
        return framebuffer is D3D12Framebuffer
               || framebuffer is D3D12SwapchainFramebuffer swapchainFramebuffer
               && this._swapchainBackBuffer.TryGetBackBuffer(swapchainFramebuffer, out _, out _, out _, out _);
    }

    /// <summary>
    /// Gets the D3D12 clear format for a color attachment.
    /// </summary>
    /// <param name="framebuffer">The framebuffer to inspect.</param>
    /// <param name="index">The zero-based color attachment index.</param>
    /// <param name="format">The D3D12 clear format.</param>
    /// <returns><see langword="true" /> when a color clear format was resolved.</returns>
    private bool TryGetColorClearFormat(Framebuffer framebuffer, uint index, out Vortice.DXGI.Format format) {
        if (index >= framebuffer.ColorTargets.Count) {
            format = default;
            return false;
        }

        Texture target = framebuffer.ColorTargets[(int)index].Target;
        format = D3D12Formats.GetViewFormat(D3D12Formats.ToDxgiFormat(target.Format));
        return true;
    }

    /// <summary>
    /// Gets the D3D12 clear format for a depth/stencil attachment.
    /// </summary>
    /// <param name="framebuffer">The framebuffer to inspect.</param>
    /// <param name="format">The D3D12 clear format.</param>
    /// <returns><see langword="true" /> when a depth/stencil clear format was resolved.</returns>
    private bool TryGetDepthClearFormat(Framebuffer framebuffer, out Vortice.DXGI.Format format) {
        if (framebuffer.DepthTarget is not FramebufferAttachment depthAttachment) {
            format = default;
            return false;
        }

        format = D3D12Formats.ToDepthFormat(depthAttachment.Target.Format);
        return true;
    }

    /// <summary>
    /// Clears all queued clear values without touching the GPU.
    /// </summary>
    private void ClearQueuedClears() {
        for (int i = 0; i < MaxRenderTargetCount; i++) {
            this._validColorClearValues[i] = false;
        }

        this._hasDepthClearValue = false;
        this._depthClearValue = default;
    }
}
