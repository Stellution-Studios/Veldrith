using System;

namespace Veldrith.D3D12;

/// <summary>
/// Tracks D3D12 root-buffer and root-descriptor-table bindings to avoid redundant root updates.
/// </summary>
internal sealed class D3D12RootBindingCache {

    /// <summary>
    /// Stores cached compute root-buffer GPU addresses by root parameter index.
    /// </summary>
    private ulong[] _computeRootBufferAddresses = new ulong[32];

    /// <summary>
    /// Tracks which compute root-buffer cache slots contain valid addresses.
    /// </summary>
    private bool[] _computeRootBufferAddressValid = new bool[32];

    /// <summary>
    /// Stores cached compute root descriptor-table GPU pointers by root parameter index.
    /// </summary>
    private ulong[] _computeRootTablePointers = new ulong[32];

    /// <summary>
    /// Tracks which compute root descriptor-table cache slots contain valid pointers.
    /// </summary>
    private bool[] _computeRootTablePointerValid = new bool[32];

    /// <summary>
    /// Stores cached graphics root-buffer GPU addresses by root parameter index.
    /// </summary>
    private ulong[] _graphicsRootBufferAddresses = new ulong[32];

    /// <summary>
    /// Tracks which graphics root-buffer cache slots contain valid addresses.
    /// </summary>
    private bool[] _graphicsRootBufferAddressValid = new bool[32];

    /// <summary>
    /// Stores cached graphics root descriptor-table GPU pointers by root parameter index.
    /// </summary>
    private ulong[] _graphicsRootTablePointers = new ulong[32];

    /// <summary>
    /// Tracks which graphics root descriptor-table cache slots contain valid pointers.
    /// </summary>
    private bool[] _graphicsRootTablePointerValid = new bool[32];

    /// <summary>
    /// Checks whether a graphics root-buffer binding already matches the requested GPU address.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="gpuAddress">The GPU virtual address to compare.</param>
    /// <returns><see langword="true" /> when the cached graphics binding matches.</returns>
    internal bool IsSameGraphicsRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        int index = this.EnsureGraphicsRootBufferCapacity(rootParameterIndex);
        return this._graphicsRootBufferAddressValid[index] && this._graphicsRootBufferAddresses[index] == gpuAddress;
    }

    /// <summary>
    /// Stores a graphics root-buffer GPU address in the cache.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="gpuAddress">The GPU virtual address that was bound.</param>
    internal void SetGraphicsRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        int index = this.EnsureGraphicsRootBufferCapacity(rootParameterIndex);
        this._graphicsRootBufferAddresses[index] = gpuAddress;
        this._graphicsRootBufferAddressValid[index] = true;
    }

    /// <summary>
    /// Checks whether a compute root-buffer binding already matches the requested GPU address.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="gpuAddress">The GPU virtual address to compare.</param>
    /// <returns><see langword="true" /> when the cached compute binding matches.</returns>
    internal bool IsSameComputeRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        int index = this.EnsureComputeRootBufferCapacity(rootParameterIndex);
        return this._computeRootBufferAddressValid[index] && this._computeRootBufferAddresses[index] == gpuAddress;
    }

    /// <summary>
    /// Stores a compute root-buffer GPU address in the cache.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="gpuAddress">The GPU virtual address that was bound.</param>
    internal void SetComputeRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        int index = this.EnsureComputeRootBufferCapacity(rootParameterIndex);
        this._computeRootBufferAddresses[index] = gpuAddress;
        this._computeRootBufferAddressValid[index] = true;
    }

    /// <summary>
    /// Checks whether a graphics root descriptor-table binding already matches the requested GPU handle.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="tablePtr">The descriptor table GPU pointer to compare.</param>
    /// <returns><see langword="true" /> when the cached graphics table matches.</returns>
    internal bool IsSameGraphicsRootTable(uint rootParameterIndex, ulong tablePtr) {
        int index = this.EnsureGraphicsRootTableCapacity(rootParameterIndex);
        return this._graphicsRootTablePointerValid[index] && this._graphicsRootTablePointers[index] == tablePtr;
    }

    /// <summary>
    /// Stores a graphics root descriptor-table GPU pointer in the cache.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="tablePtr">The descriptor table GPU pointer that was bound.</param>
    internal void SetGraphicsRootTable(uint rootParameterIndex, ulong tablePtr) {
        int index = this.EnsureGraphicsRootTableCapacity(rootParameterIndex);
        this._graphicsRootTablePointers[index] = tablePtr;
        this._graphicsRootTablePointerValid[index] = true;
    }

    /// <summary>
    /// Checks whether a compute root descriptor-table binding already matches the requested GPU handle.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="tablePtr">The descriptor table GPU pointer to compare.</param>
    /// <returns><see langword="true" /> when the cached compute table matches.</returns>
    internal bool IsSameComputeRootTable(uint rootParameterIndex, ulong tablePtr) {
        int index = this.EnsureComputeRootTableCapacity(rootParameterIndex);
        return this._computeRootTablePointerValid[index] && this._computeRootTablePointers[index] == tablePtr;
    }

    /// <summary>
    /// Stores a compute root descriptor-table GPU pointer in the cache.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="tablePtr">The descriptor table GPU pointer that was bound.</param>
    internal void SetComputeRootTable(uint rootParameterIndex, ulong tablePtr) {
        int index = this.EnsureComputeRootTableCapacity(rootParameterIndex);
        this._computeRootTablePointers[index] = tablePtr;
        this._computeRootTablePointerValid[index] = true;
    }

    /// <summary>
    /// Invalidates graphics root-buffer and root descriptor-table cache entries.
    /// </summary>
    internal void InvalidateGraphics() {
        Array.Clear(this._graphicsRootBufferAddressValid, 0, this._graphicsRootBufferAddressValid.Length);
        Array.Clear(this._graphicsRootTablePointerValid, 0, this._graphicsRootTablePointerValid.Length);
    }

    /// <summary>
    /// Invalidates compute root-buffer and root descriptor-table cache entries.
    /// </summary>
    internal void InvalidateCompute() {
        Array.Clear(this._computeRootBufferAddressValid, 0, this._computeRootBufferAddressValid.Length);
        Array.Clear(this._computeRootTablePointerValid, 0, this._computeRootTablePointerValid.Length);
    }

    /// <summary>
    /// Ensures graphics root-buffer cache arrays can hold a root parameter index.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <returns>The array index corresponding to the root parameter.</returns>
    private int EnsureGraphicsRootBufferCapacity(uint rootParameterIndex) {
        int index = (int)rootParameterIndex;
        if (index >= this._graphicsRootBufferAddresses.Length) {
            Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddresses, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddressValid, rootParameterIndex + 1);
        }

        return index;
    }

    /// <summary>
    /// Ensures compute root-buffer cache arrays can hold a root parameter index.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <returns>The array index corresponding to the root parameter.</returns>
    private int EnsureComputeRootBufferCapacity(uint rootParameterIndex) {
        int index = (int)rootParameterIndex;
        if (index >= this._computeRootBufferAddresses.Length) {
            Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddresses, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddressValid, rootParameterIndex + 1);
        }

        return index;
    }

    /// <summary>
    /// Ensures graphics root descriptor-table cache arrays can hold a root parameter index.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <returns>The array index corresponding to the root parameter.</returns>
    private int EnsureGraphicsRootTableCapacity(uint rootParameterIndex) {
        int index = (int)rootParameterIndex;
        if (index >= this._graphicsRootTablePointers.Length) {
            Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointers, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointerValid, rootParameterIndex + 1);
        }

        return index;
    }

    /// <summary>
    /// Ensures compute root descriptor-table cache arrays can hold a root parameter index.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <returns>The array index corresponding to the root parameter.</returns>
    private int EnsureComputeRootTableCapacity(uint rootParameterIndex) {
        int index = (int)rootParameterIndex;
        if (index >= this._computeRootTablePointers.Length) {
            Util.EnsureArrayMinimumSize(ref this._computeRootTablePointers, rootParameterIndex + 1);
            Util.EnsureArrayMinimumSize(ref this._computeRootTablePointerValid, rootParameterIndex + 1);
        }

        return index;
    }
}
