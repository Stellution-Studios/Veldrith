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

        ResourceStates[] srcStates = this.CaptureSourceStates(src);
        ResourceStates[] dstStates = this.CaptureDestinationStates(dst);
        this._commandList.TransitionTextureForInternalUse(src, ResourceStates.ResolveSource);
        this._commandList.TransitionTextureForInternalUse(dst, ResourceStates.ResolveDest);
        this._commandList.FlushPendingBarriersForInternalUse();

        Format resolveFormat = D3D12Formats.ToDxgiFormat(source.Format);
        uint mipLevels = Math.Min(source.MipLevels, destination.MipLevels);
        uint arrayLayers = Math.Min(source.ArrayLayers, destination.ArrayLayers);
        for (uint arrayLayer = 0; arrayLayer < arrayLayers; arrayLayer++) {
            for (uint mipLevel = 0; mipLevel < mipLevels; mipLevel++) {
                uint srcSubresource = source.CalculateSubresource(mipLevel, arrayLayer);
                uint dstSubresource = destination.CalculateSubresource(mipLevel, arrayLayer);
                this._commandList.NativeCommandList.ResolveSubresource(dst.NativeTexture, dstSubresource, src.NativeTexture, srcSubresource, resolveFormat);
            }
        }

        this.RestoreTextureStates(src, srcStates);
        this.RestoreTextureStates(dst, dstStates);
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

        ResourceStates[] srcStates = this.CaptureSourceStates(src);
        ResourceStates[] dstStates = this.CaptureDestinationStates(dst);
        this._commandList.TransitionTextureForInternalUse(src, ResourceStates.CopySource);
        this._commandList.TransitionTextureForInternalUse(dst, ResourceStates.CopyDest);
        this._commandList.FlushPendingBarriersForInternalUse();

        Box srcBox = new((int)srcX, (int)srcY, (int)srcZ, (int)(srcX + width), (int)(srcY + height), (int)(srcZ + depth));
        for (uint layer = 0; layer < layerCount; layer++) {
            uint srcSubresource = source.CalculateSubresource(srcMipLevel, srcBaseArrayLayer + layer);
            uint dstSubresource = destination.CalculateSubresource(dstMipLevel, dstBaseArrayLayer + layer);
            TextureCopyLocation srcLocation = new(src.NativeTexture, srcSubresource);
            TextureCopyLocation dstLocation = new(dst.NativeTexture, dstSubresource);
            this._commandList.NativeCommandList.CopyTextureRegion(dstLocation, dstX, dstY, dstZ, srcLocation, srcBox);
        }

        this.RestoreTextureStates(src, srcStates);
        this.RestoreTextureStates(dst, dstStates);
    }

    /// <summary>
    /// Captures source texture states into a reusable buffer.
    /// </summary>
    /// <param name="texture">The source texture to capture.</param>
    /// <returns>The reusable state buffer containing current subresource states.</returns>
    private ResourceStates[] CaptureSourceStates(D3D12Texture texture) {
        this.EnsureCaptureCapacity(ref this._srcCaptureStates, texture.SubresourceCount);
        CaptureTextureStatesInto(texture, this._srcCaptureStates);
        return this._srcCaptureStates;
    }

    /// <summary>
    /// Captures destination texture states into a reusable buffer.
    /// </summary>
    /// <param name="texture">The destination texture to capture.</param>
    /// <returns>The reusable state buffer containing current subresource states.</returns>
    private ResourceStates[] CaptureDestinationStates(D3D12Texture texture) {
        this.EnsureCaptureCapacity(ref this._dstCaptureStates, texture.SubresourceCount);
        CaptureTextureStatesInto(texture, this._dstCaptureStates);
        return this._dstCaptureStates;
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
    /// Captures current per-subresource states into a pre-allocated buffer.
    /// </summary>
    /// <param name="texture">The texture whose states are captured.</param>
    /// <param name="buffer">The destination state buffer.</param>
    private static void CaptureTextureStatesInto(D3D12Texture texture, ResourceStates[] buffer) {
        uint subresourceCount = texture.SubresourceCount;
        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            buffer[subresource] = texture.GetSubresourceState(subresource);
        }
    }

    /// <summary>
    /// Restores a texture's subresource states after a temporary copy or resolve transition.
    /// </summary>
    /// <param name="texture">The texture to restore.</param>
    /// <param name="previousStates">The previously captured subresource states.</param>
    private void RestoreTextureStates(D3D12Texture texture, ResourceStates[] previousStates) {
        if (texture.NativeTexture == null || previousStates == null || previousStates.Length == 0) {
            return;
        }

        uint subresourceCount = Math.Min(texture.SubresourceCount, (uint)previousStates.Length);
        for (uint subresource = 0; subresource < subresourceCount; subresource++) {
            ResourceStates current = texture.GetSubresourceState(subresource);
            ResourceStates previous = previousStates[subresource];
            if (current == previous) {
                continue;
            }

            this._commandList.TransitionSubresourceForInternalUse(texture.NativeTexture, current, previous, subresource);
            texture.SetSubresourceState(subresource, previous);
        }
    }
}
