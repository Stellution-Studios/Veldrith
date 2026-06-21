using System;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Veldrith.D3D12;

/// <summary>
/// Records D3D12 texture copy and resolve operations with temporary state capture and restore.
/// </summary>
internal sealed class D3D12TextureCopyPlanner {

    /// <summary>
    /// Stores the command list that records copy, resolve, and transition commands.
    /// </summary>
    private readonly D3D12CommandList _commandList;

    /// <summary>
    /// Reusable state capture buffer for source texture transitions.
    /// </summary>
    private ResourceStates[] _srcCaptureStates = new ResourceStates[128];

    /// <summary>
    /// Reusable state capture buffer for destination texture transitions.
    /// </summary>
    private ResourceStates[] _dstCaptureStates = new ResourceStates[128];

    /// <summary>
    /// Reusable subresource capture buffer for source texture copy/resolve transitions.
    /// </summary>
    private uint[] _srcCaptureSubresources = new uint[128];

    /// <summary>
    /// Reusable subresource capture buffer for destination texture copy/resolve transitions.
    /// </summary>
    private uint[] _dstCaptureSubresources = new uint[128];

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12TextureCopyPlanner" /> class.
    /// </summary>
    /// <param name="commandList">The command list that records D3D12 operations.</param>
    internal D3D12TextureCopyPlanner(D3D12CommandList commandList) {
        this._commandList = commandList;
    }

    /// <summary>
    /// Records a full texture resolve operation.
    /// </summary>
    /// <param name="source">The source texture.</param>
    /// <param name="destination">The destination texture.</param>
    internal void Resolve(Texture source, Texture destination) {
        this._commandList.FlushPendingUavBarrierForInternalUse();
        D3D12Texture src = Util.AssertSubtype<Texture, D3D12Texture>(source);
        D3D12Texture dst = Util.AssertSubtype<Texture, D3D12Texture>(destination);

        if (src.NativeTexture == null || dst.NativeTexture == null) {
            src.CopyRegionTo(dst, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, source.Width, source.Height, source.Depth, source.ArrayLayers);
            return;
        }

        uint mipLevels = Math.Min(source.MipLevels, destination.MipLevels);
        uint arrayLayers = Math.Min(source.ArrayLayers, destination.ArrayLayers);
        uint resolveSubresourceCount = mipLevels * arrayLayers;
        ResourceStates[] srcStates = this.CaptureResolveStates(src, mipLevels, arrayLayers, ref this._srcCaptureStates, ref this._srcCaptureSubresources);
        ResourceStates[] dstStates = this.CaptureResolveStates(dst, mipLevels, arrayLayers, ref this._dstCaptureStates, ref this._dstCaptureSubresources);
        this.TransitionCapturedSubresources(src, this._srcCaptureSubresources, resolveSubresourceCount, ResourceStates.ResolveSource);
        this.TransitionCapturedSubresources(dst, this._dstCaptureSubresources, resolveSubresourceCount, ResourceStates.ResolveDest);
        this._commandList.FlushPendingBarriersForInternalUse();

        Format resolveFormat = D3D12Formats.ToDxgiFormat(source.Format);
        for (uint arrayLayer = 0; arrayLayer < arrayLayers; arrayLayer++) {
            for (uint mipLevel = 0; mipLevel < mipLevels; mipLevel++) {
                uint srcSubresource = source.CalculateSubresource(mipLevel, arrayLayer);
                uint dstSubresource = destination.CalculateSubresource(mipLevel, arrayLayer);
                this._commandList.NativeCommandList.ResolveSubresource(dst.NativeTexture, dstSubresource, src.NativeTexture, srcSubresource, resolveFormat);
            }
        }

        this.RestoreCapturedSubresourceStates(src, this._srcCaptureSubresources, srcStates, resolveSubresourceCount);
        this.RestoreCapturedSubresourceStates(dst, this._dstCaptureSubresources, dstStates, resolveSubresourceCount);
    }

    /// <summary>
    /// Records a texture-region copy operation.
    /// </summary>
    /// <param name="source">The source texture.</param>
    /// <param name="srcX">The X origin in the source texture.</param>
    /// <param name="srcY">The Y origin in the source texture.</param>
    /// <param name="srcZ">The Z origin in the source texture.</param>
    /// <param name="srcMipLevel">The source mip level.</param>
    /// <param name="srcBaseArrayLayer">The first source array layer.</param>
    /// <param name="destination">The destination texture.</param>
    /// <param name="dstX">The X origin in the destination texture.</param>
    /// <param name="dstY">The Y origin in the destination texture.</param>
    /// <param name="dstZ">The Z origin in the destination texture.</param>
    /// <param name="dstMipLevel">The destination mip level.</param>
    /// <param name="dstBaseArrayLayer">The first destination array layer.</param>
    /// <param name="width">The copy width.</param>
    /// <param name="height">The copy height.</param>
    /// <param name="depth">The copy depth.</param>
    /// <param name="layerCount">The number of array layers to copy.</param>
    internal void Copy(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount) {
        this._commandList.FlushPendingUavBarrierForInternalUse();
        D3D12Texture src = Util.AssertSubtype<Texture, D3D12Texture>(source);
        D3D12Texture dst = Util.AssertSubtype<Texture, D3D12Texture>(destination);

        if (src.NativeTexture == null || dst.NativeTexture == null) {
            src.CopyRegionTo(dst, srcX, srcY, srcZ, srcMipLevel, srcBaseArrayLayer, dstX, dstY, dstZ, dstMipLevel, dstBaseArrayLayer, width, height, depth, layerCount);
            return;
        }

        ResourceStates[] srcStates = this.CaptureLayerStates(src, srcMipLevel, srcBaseArrayLayer, layerCount, ref this._srcCaptureStates, ref this._srcCaptureSubresources);
        ResourceStates[] dstStates = this.CaptureLayerStates(dst, dstMipLevel, dstBaseArrayLayer, layerCount, ref this._dstCaptureStates, ref this._dstCaptureSubresources);
        this.TransitionCapturedSubresources(src, this._srcCaptureSubresources, layerCount, ResourceStates.CopySource);
        this.TransitionCapturedSubresources(dst, this._dstCaptureSubresources, layerCount, ResourceStates.CopyDest);
        this._commandList.FlushPendingBarriersForInternalUse();

        Box srcBox = new((int)srcX, (int)srcY, (int)srcZ, (int)(srcX + width), (int)(srcY + height), (int)(srcZ + depth));
        for (uint layer = 0; layer < layerCount; layer++) {
            uint srcSubresource = source.CalculateSubresource(srcMipLevel, srcBaseArrayLayer + layer);
            uint dstSubresource = destination.CalculateSubresource(dstMipLevel, dstBaseArrayLayer + layer);
            TextureCopyLocation srcLocation = new(src.NativeTexture, srcSubresource);
            TextureCopyLocation dstLocation = new(dst.NativeTexture, dstSubresource);
            this._commandList.NativeCommandList.CopyTextureRegion(dstLocation, dstX, dstY, dstZ, srcLocation, srcBox);
        }

        this.RestoreCapturedSubresourceStates(src, this._srcCaptureSubresources, srcStates, layerCount);
        this.RestoreCapturedSubresourceStates(dst, this._dstCaptureSubresources, dstStates, layerCount);
    }

    /// <summary>
    /// Ensures a reusable capture buffer can hold all subresources for a texture.
    /// </summary>
    /// <param name="buffer">The capture buffer to grow.</param>
    /// <param name="subresourceCount">The required subresource count.</param>
    private void EnsureCaptureCapacity(ref ResourceStates[] buffer, uint subresourceCount) {
        if (subresourceCount <= (uint)buffer.Length) {
            return;
        }

        Util.EnsureArrayMinimumSize(ref buffer, subresourceCount);
    }

    /// <summary>
    /// Ensures a reusable subresource buffer can hold the requested number of entries.
    /// </summary>
    /// <param name="buffer">The subresource buffer to grow.</param>
    /// <param name="subresourceCount">The required subresource count.</param>
    private void EnsureSubresourceCaptureCapacity(ref uint[] buffer, uint subresourceCount) {
        if (subresourceCount <= (uint)buffer.Length) {
            return;
        }

        Util.EnsureArrayMinimumSize(ref buffer, subresourceCount);
    }

    /// <summary>
    /// Captures states for one mip level across a range of array layers.
    /// </summary>
    /// <param name="texture">The texture whose states are captured.</param>
    /// <param name="mipLevel">The mip level to copy.</param>
    /// <param name="baseArrayLayer">The first array layer to copy.</param>
    /// <param name="layerCount">The number of array layers to copy.</param>
    /// <param name="states">The reusable state buffer.</param>
    /// <param name="subresources">The reusable subresource buffer.</param>
    /// <returns>The reusable state buffer containing captured states.</returns>
    private ResourceStates[] CaptureLayerStates(D3D12Texture texture, uint mipLevel, uint baseArrayLayer, uint layerCount, ref ResourceStates[] states, ref uint[] subresources) {
        this.EnsureCaptureCapacity(ref states, layerCount);
        this.EnsureSubresourceCaptureCapacity(ref subresources, layerCount);
        for (uint layer = 0; layer < layerCount; layer++) {
            uint subresource = texture.CalculateSubresource(mipLevel, baseArrayLayer + layer);
            subresources[layer] = subresource;
            states[layer] = texture.GetSubresourceState(subresource);
        }

        return states;
    }

    /// <summary>
    /// Captures states for the mip and array-layer range used by a resolve.
    /// </summary>
    /// <param name="texture">The texture whose states are captured.</param>
    /// <param name="mipLevels">The number of mip levels to resolve.</param>
    /// <param name="arrayLayers">The number of array layers to resolve.</param>
    /// <param name="states">The reusable state buffer.</param>
    /// <param name="subresources">The reusable subresource buffer.</param>
    /// <returns>The reusable state buffer containing captured states.</returns>
    private ResourceStates[] CaptureResolveStates(D3D12Texture texture, uint mipLevels, uint arrayLayers, ref ResourceStates[] states, ref uint[] subresources) {
        uint count = mipLevels * arrayLayers;
        this.EnsureCaptureCapacity(ref states, count);
        this.EnsureSubresourceCaptureCapacity(ref subresources, count);
        uint index = 0;
        for (uint arrayLayer = 0; arrayLayer < arrayLayers; arrayLayer++) {
            for (uint mipLevel = 0; mipLevel < mipLevels; mipLevel++) {
                uint subresource = texture.CalculateSubresource(mipLevel, arrayLayer);
                subresources[index] = subresource;
                states[index] = texture.GetSubresourceState(subresource);
                index++;
            }
        }

        return states;
    }

    /// <summary>
    /// Transitions captured subresources to a copy or resolve state.
    /// </summary>
    /// <param name="texture">The texture to transition.</param>
    /// <param name="subresources">The captured subresource indices.</param>
    /// <param name="count">The number of captured entries.</param>
    /// <param name="targetState">The required D3D12 state.</param>
    private void TransitionCapturedSubresources(D3D12Texture texture, uint[] subresources, uint count, ResourceStates targetState) {
        if (texture.NativeTexture == null || count == 0) {
            return;
        }

        if (count == texture.SubresourceCount) {
            this._commandList.TransitionTextureForInternalUse(texture, targetState);
            return;
        }

        for (uint i = 0; i < count; i++) {
            uint subresource = subresources[i];
            ResourceStates current = texture.GetSubresourceState(subresource);
            if (current == targetState) {
                continue;
            }

            this._commandList.TransitionSubresourceForInternalUse(texture.NativeTexture, current, targetState, subresource);
            texture.SetSubresourceState(subresource, targetState);
        }
    }

    /// <summary>
    /// Restores captured subresource states after a copy or resolve operation.
    /// </summary>
    /// <param name="texture">The texture to restore.</param>
    /// <param name="subresources">The captured subresource indices.</param>
    /// <param name="previousStates">The previously captured states.</param>
    /// <param name="count">The number of captured entries.</param>
    private void RestoreCapturedSubresourceStates(D3D12Texture texture, uint[] subresources, ResourceStates[] previousStates, uint count) {
        if (texture.NativeTexture == null || subresources == null || previousStates == null || count == 0) {
            return;
        }

        for (uint i = 0; i < count; i++) {
            uint subresource = subresources[i];
            ResourceStates current = texture.GetSubresourceState(subresource);
            ResourceStates previous = previousStates[i];
            if (current == previous) {
                continue;
            }

            this._commandList.TransitionSubresourceForInternalUse(texture.NativeTexture, current, previous, subresource);
            texture.SetSubresourceState(subresource, previous);
        }
    }
}
