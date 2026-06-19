using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Veldrith.D3D12;

/// <summary>
/// Tracks dynamic D3D12 viewport and scissor state for a command list.
/// </summary>
internal sealed class D3D12ViewportScissorState {

    /// <summary>
    /// Stores the maximum number of cached viewport and scissor slots.
    /// </summary>
    private const uint MaxViewportScissorSlots = 16;

    /// <summary>
    /// Stores the active scissor rects recorded into D3D12 state.
    /// </summary>
    private readonly RawRect[] _activeScissorRects = new RawRect[MaxViewportScissorSlots];

    /// <summary>
    /// Stores the active viewports recorded into D3D12 state.
    /// </summary>
    private readonly Vortice.Mathematics.Viewport[] _activeViewports = new Vortice.Mathematics.Viewport[MaxViewportScissorSlots];

    /// <summary>
    /// Stores the number of active scissor rect slots.
    /// </summary>
    private uint _activeScissorRectCount;

    /// <summary>
    /// Stores the number of active viewport slots.
    /// </summary>
    private uint _activeViewportCount;

    /// <summary>
    /// Clears cached state for a new command-list recording.
    /// </summary>
    internal void Reset() {
        this._activeViewportCount = 0;
        this._activeScissorRectCount = 0;
    }

    /// <summary>
    /// Records a viewport if it differs from cached D3D12 state.
    /// </summary>
    /// <param name="commandList">The native command list receiving the viewport command.</param>
    /// <param name="index">The viewport slot.</param>
    /// <param name="viewport">The viewport to record.</param>
    internal void SetViewport(ID3D12GraphicsCommandList commandList, uint index, ref Viewport viewport) {
        if (index >= this._activeViewports.Length) {
            return;
        }

        Vortice.Mathematics.Viewport d3D12Viewport = new(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
        if (index + 1 <= this._activeViewportCount && this._activeViewports[index] == d3D12Viewport) {
            return;
        }

        this._activeViewports[index] = d3D12Viewport;

        if (index + 1 > this._activeViewportCount) {
            this._activeViewportCount = index + 1;
        }

        RSSetViewportsNoAlloc(commandList, this._activeViewportCount, this._activeViewports);
    }

    /// <summary>
    /// Records a scissor rectangle if it differs from cached D3D12 state.
    /// </summary>
    /// <param name="commandList">The native command list receiving the scissor command.</param>
    /// <param name="index">The scissor slot.</param>
    /// <param name="x">The left coordinate.</param>
    /// <param name="y">The top coordinate.</param>
    /// <param name="width">The rectangle width.</param>
    /// <param name="height">The rectangle height.</param>
    internal void SetScissorRect(ID3D12GraphicsCommandList commandList, uint index, uint x, uint y, uint width, uint height) {
        if (index >= this._activeScissorRects.Length) {
            return;
        }

        RawRect scissorRect = new((int)x, (int)y, (int)(x + width), (int)(y + height));
        if (index + 1 <= this._activeScissorRectCount && this._activeScissorRects[index] == scissorRect) {
            return;
        }

        this._activeScissorRects[index] = scissorRect;

        if (index + 1 > this._activeScissorRectCount) {
            this._activeScissorRectCount = index + 1;
        }

        RSSetScissorRectsNoAlloc(commandList, this._activeScissorRectCount, this._activeScissorRects);
    }

    /// <summary>
    /// Sets viewports without going through the managed COM wrapper.
    /// </summary>
    /// <param name="commandList">The native command list to invoke.</param>
    /// <param name="count">The number of viewport entries to record.</param>
    /// <param name="viewports">The cached viewport entries.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void RSSetViewportsNoAlloc(ID3D12GraphicsCommandList commandList, uint count, Vortice.Mathematics.Viewport[] viewports) {
        void** vtbl = *(void***)commandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, void> rsSetViewports = (delegate* unmanaged[Stdcall]<void*, uint, void*, void>)vtbl[21];
        fixed (Vortice.Mathematics.Viewport* pViewports = viewports) {
            rsSetViewports((void*)commandList.NativePointer, count, pViewports);
        }
    }

    /// <summary>
    /// Sets scissor rects without going through the managed COM wrapper.
    /// </summary>
    /// <param name="commandList">The native command list to invoke.</param>
    /// <param name="count">The number of scissor entries to record.</param>
    /// <param name="rects">The cached scissor entries.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void RSSetScissorRectsNoAlloc(ID3D12GraphicsCommandList commandList, uint count, RawRect[] rects) {
        void** vtbl = *(void***)commandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, void> rsSetScissorRects = (delegate* unmanaged[Stdcall]<void*, uint, void*, void>)vtbl[22];
        fixed (RawRect* pRects = rects) {
            rsSetScissorRects((void*)commandList.NativePointer, count, pRects);
        }
    }
}
