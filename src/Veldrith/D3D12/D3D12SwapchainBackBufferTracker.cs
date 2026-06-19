using System;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Tracks the swapchain back buffer used by a D3D12 command-list recording.
/// </summary>
internal sealed class D3D12SwapchainBackBufferTracker {

    /// <summary>
    /// Stores the swapchain framebuffer whose current back buffer is cached.
    /// </summary>
    private D3D12SwapchainFramebuffer _cachedFramebuffer;

    /// <summary>
    /// Stores the cached current swapchain back-buffer resource.
    /// </summary>
    private ID3D12Resource _cachedBackBuffer;

    /// <summary>
    /// Stores the cached RTV descriptor for the current swapchain back buffer.
    /// </summary>
    private CpuDescriptorHandle _cachedRtv;

    /// <summary>
    /// Stores the cached current swapchain back-buffer index.
    /// </summary>
    private int _cachedBackBufferIndex = -1;

    /// <summary>
    /// Stores the cached current swapchain back-buffer state.
    /// </summary>
    private ResourceStates _cachedBackBufferState;

    /// <summary>
    /// Stores the back-buffer index that was transitioned during this recording.
    /// </summary>
    private int _transitionedBackBufferIndex = -1;

    /// <summary>
    /// Clears cached back-buffer and transition state for a new command-list recording.
    /// </summary>
    internal void Reset() {
        this._transitionedBackBufferIndex = -1;
        this._cachedFramebuffer = null;
        this._cachedBackBuffer = null;
        this._cachedRtv = default;
        this._cachedBackBufferIndex = -1;
        this._cachedBackBufferState = ResourceStates.Common;
    }

    /// <summary>
    /// Gets the current swapchain back buffer, using the cached value when possible.
    /// </summary>
    /// <param name="framebuffer">The swapchain framebuffer to query.</param>
    /// <param name="backBuffer">The current back-buffer resource.</param>
    /// <param name="rtv">The current back-buffer RTV descriptor.</param>
    /// <param name="index">The current back-buffer index.</param>
    /// <param name="state">The tracked current back-buffer state.</param>
    /// <returns><see langword="true" /> when a current back buffer was resolved.</returns>
    internal bool TryGetBackBuffer(D3D12SwapchainFramebuffer framebuffer, out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int index, out ResourceStates state) {
        if (ReferenceEquals(this._cachedFramebuffer, framebuffer)
            && this._cachedBackBuffer != null
            && this._cachedBackBufferIndex >= 0) {
            backBuffer = this._cachedBackBuffer;
            rtv = this._cachedRtv;
            index = this._cachedBackBufferIndex;
            state = this._cachedBackBufferState;
            return true;
        }

        if (!framebuffer.Swapchain.TryGetCurrentBackBuffer(out backBuffer, out rtv, out index, out state)) {
            return false;
        }

        this.Cache(framebuffer, backBuffer, rtv, index, state);
        return true;
    }

    /// <summary>
    /// Records the state assigned to a swapchain back buffer during command-list recording.
    /// </summary>
    /// <param name="index">The back-buffer index that changed state.</param>
    /// <param name="state">The new resource state.</param>
    internal void MarkBackBufferState(int index, ResourceStates state) {
        this._transitionedBackBufferIndex = index;
        this._cachedBackBufferState = state;
    }

    /// <summary>
    /// Transitions the touched swapchain back buffer back to present state at command-list end.
    /// </summary>
    /// <param name="framebuffer">The active swapchain framebuffer.</param>
    /// <param name="transition">The transition callback used by the command list.</param>
    internal void TransitionToPresent(D3D12SwapchainFramebuffer framebuffer, Action<ID3D12Resource, ResourceStates, ResourceStates> transition) {
        if (framebuffer == null || this._transitionedBackBufferIndex < 0) {
            return;
        }

        if (ReferenceEquals(this._cachedFramebuffer, framebuffer)
            && this._cachedBackBuffer != null
            && this._cachedBackBufferIndex == this._transitionedBackBufferIndex) {
            transition(this._cachedBackBuffer, this._cachedBackBufferState, ResourceStates.Present);
            framebuffer.Swapchain.SetBackBufferState(this._cachedBackBufferIndex, ResourceStates.Present);
            this._cachedBackBufferState = ResourceStates.Present;
            return;
        }

        if (framebuffer.Swapchain.TryGetCurrentBackBuffer(out ID3D12Resource backBuffer, out CpuDescriptorHandle rtv, out int currentIndex, out ResourceStates state)
            && currentIndex == this._transitionedBackBufferIndex) {
            this.Cache(framebuffer, backBuffer, rtv, currentIndex, state);
            transition(backBuffer, state, ResourceStates.Present);
            framebuffer.Swapchain.SetBackBufferState(currentIndex, ResourceStates.Present);
            this._cachedBackBufferState = ResourceStates.Present;
        }
    }

    /// <summary>
    /// Updates the cached current back-buffer values.
    /// </summary>
    /// <param name="framebuffer">The framebuffer that owns the back buffer.</param>
    /// <param name="backBuffer">The current back-buffer resource.</param>
    /// <param name="rtv">The RTV descriptor for the back buffer.</param>
    /// <param name="index">The current back-buffer index.</param>
    /// <param name="state">The current tracked state.</param>
    private void Cache(D3D12SwapchainFramebuffer framebuffer, ID3D12Resource backBuffer, CpuDescriptorHandle rtv, int index, ResourceStates state) {
        this._cachedFramebuffer = framebuffer;
        this._cachedBackBuffer = backBuffer;
        this._cachedRtv = rtv;
        this._cachedBackBufferIndex = index;
        this._cachedBackBufferState = state;
    }
}
