using System;
using System.Runtime.CompilerServices;

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
    /// Stores validity generations for cached compute root-buffer addresses.
    /// </summary>
    private uint[] _computeRootBufferAddressGenerations = new uint[32];

    /// <summary>
    /// Stores cached compute root descriptor-table GPU pointers by root parameter index.
    /// </summary>
    private ulong[] _computeRootTablePointers = new ulong[32];

    /// <summary>
    /// Stores validity generations for cached compute root descriptor-table pointers.
    /// </summary>
    private uint[] _computeRootTablePointerGenerations = new uint[32];

    /// <summary>
    /// Stores cached graphics root-buffer GPU addresses by root parameter index.
    /// </summary>
    private ulong[] _graphicsRootBufferAddresses = new ulong[32];

    /// <summary>
    /// Stores validity generations for cached graphics root-buffer addresses.
    /// </summary>
    private uint[] _graphicsRootBufferAddressGenerations = new uint[32];

    /// <summary>
    /// Stores cached graphics root descriptor-table GPU pointers by root parameter index.
    /// </summary>
    private ulong[] _graphicsRootTablePointers = new ulong[32];

    /// <summary>
    /// Stores validity generations for cached graphics root descriptor-table pointers.
    /// </summary>
    private uint[] _graphicsRootTablePointerGenerations = new uint[32];

    /// <summary>
    /// Stores the active graphics cache generation.
    /// </summary>
    private uint _graphicsGeneration = 1;

    /// <summary>
    /// Stores the active compute cache generation.
    /// </summary>
    private uint _computeGeneration = 1;

    /// <summary>
    /// Updates a graphics root-buffer cache entry and reports whether D3D12 state must be changed.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="gpuAddress">The GPU virtual address to bind.</param>
    /// <returns><see langword="true" /> when the command list must receive a root-buffer update.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryUpdateGraphicsRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        if (rootParameterIndex >= (uint)this._graphicsRootBufferAddresses.Length) {
            return this.TryUpdateGraphicsRootBufferSlow(rootParameterIndex, gpuAddress);
        }

        int index = (int)rootParameterIndex;
        if (this._graphicsRootBufferAddressGenerations[index] == this._graphicsGeneration
            && this._graphicsRootBufferAddresses[index] == gpuAddress) {
            return false;
        }

        this._graphicsRootBufferAddresses[index] = gpuAddress;
        this._graphicsRootBufferAddressGenerations[index] = this._graphicsGeneration;
        return true;
    }

    /// <summary>
    /// Updates a compute root-buffer cache entry and reports whether D3D12 state must be changed.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="gpuAddress">The GPU virtual address to bind.</param>
    /// <returns><see langword="true" /> when the command list must receive a root-buffer update.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryUpdateComputeRootBuffer(uint rootParameterIndex, ulong gpuAddress) {
        if (rootParameterIndex >= (uint)this._computeRootBufferAddresses.Length) {
            return this.TryUpdateComputeRootBufferSlow(rootParameterIndex, gpuAddress);
        }

        int index = (int)rootParameterIndex;
        if (this._computeRootBufferAddressGenerations[index] == this._computeGeneration
            && this._computeRootBufferAddresses[index] == gpuAddress) {
            return false;
        }

        this._computeRootBufferAddresses[index] = gpuAddress;
        this._computeRootBufferAddressGenerations[index] = this._computeGeneration;
        return true;
    }

    /// <summary>
    /// Updates a graphics root-table cache entry and reports whether D3D12 state must be changed.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="tablePtr">The descriptor table GPU pointer to bind.</param>
    /// <returns><see langword="true" /> when the command list must receive a root-table update.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryUpdateGraphicsRootTable(uint rootParameterIndex, ulong tablePtr) {
        if (rootParameterIndex >= (uint)this._graphicsRootTablePointers.Length) {
            return this.TryUpdateGraphicsRootTableSlow(rootParameterIndex, tablePtr);
        }

        int index = (int)rootParameterIndex;
        if (this._graphicsRootTablePointerGenerations[index] == this._graphicsGeneration
            && this._graphicsRootTablePointers[index] == tablePtr) {
            return false;
        }

        this._graphicsRootTablePointers[index] = tablePtr;
        this._graphicsRootTablePointerGenerations[index] = this._graphicsGeneration;
        return true;
    }

    /// <summary>
    /// Updates a compute root-table cache entry and reports whether D3D12 state must be changed.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="tablePtr">The descriptor table GPU pointer to bind.</param>
    /// <returns><see langword="true" /> when the command list must receive a root-table update.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryUpdateComputeRootTable(uint rootParameterIndex, ulong tablePtr) {
        if (rootParameterIndex >= (uint)this._computeRootTablePointers.Length) {
            return this.TryUpdateComputeRootTableSlow(rootParameterIndex, tablePtr);
        }

        int index = (int)rootParameterIndex;
        if (this._computeRootTablePointerGenerations[index] == this._computeGeneration
            && this._computeRootTablePointers[index] == tablePtr) {
            return false;
        }

        this._computeRootTablePointers[index] = tablePtr;
        this._computeRootTablePointerGenerations[index] = this._computeGeneration;
        return true;
    }

    /// <summary>
    /// Invalidates graphics root-buffer and root descriptor-table cache entries.
    /// </summary>
    internal void InvalidateGraphics() {
        this._graphicsGeneration++;
        if (this._graphicsGeneration != 0) {
            return;
        }

        Array.Clear(this._graphicsRootBufferAddressGenerations, 0, this._graphicsRootBufferAddressGenerations.Length);
        Array.Clear(this._graphicsRootTablePointerGenerations, 0, this._graphicsRootTablePointerGenerations.Length);
        this._graphicsGeneration = 1;
    }

    /// <summary>
    /// Invalidates compute root-buffer and root descriptor-table cache entries.
    /// </summary>
    internal void InvalidateCompute() {
        this._computeGeneration++;
        if (this._computeGeneration != 0) {
            return;
        }

        Array.Clear(this._computeRootBufferAddressGenerations, 0, this._computeRootBufferAddressGenerations.Length);
        Array.Clear(this._computeRootTablePointerGenerations, 0, this._computeRootTablePointerGenerations.Length);
        this._computeGeneration = 1;
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
            Util.EnsureArrayMinimumSize(ref this._graphicsRootBufferAddressGenerations, rootParameterIndex + 1);
        }

        return index;
    }

    /// <summary>
    /// Slow path for graphics root-buffer cache updates that need a larger cache array.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="gpuAddress">The GPU virtual address to bind.</param>
    /// <returns><see langword="true" /> when the command list must receive a root-buffer update.</returns>
    private bool TryUpdateGraphicsRootBufferSlow(uint rootParameterIndex, ulong gpuAddress) {
        int index = this.EnsureGraphicsRootBufferCapacity(rootParameterIndex);
        if (this._graphicsRootBufferAddressGenerations[index] == this._graphicsGeneration
            && this._graphicsRootBufferAddresses[index] == gpuAddress) {
            return false;
        }

        this._graphicsRootBufferAddresses[index] = gpuAddress;
        this._graphicsRootBufferAddressGenerations[index] = this._graphicsGeneration;
        return true;
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
            Util.EnsureArrayMinimumSize(ref this._computeRootBufferAddressGenerations, rootParameterIndex + 1);
        }

        return index;
    }

    /// <summary>
    /// Slow path for compute root-buffer cache updates that need a larger cache array.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="gpuAddress">The GPU virtual address to bind.</param>
    /// <returns><see langword="true" /> when the command list must receive a root-buffer update.</returns>
    private bool TryUpdateComputeRootBufferSlow(uint rootParameterIndex, ulong gpuAddress) {
        int index = this.EnsureComputeRootBufferCapacity(rootParameterIndex);
        if (this._computeRootBufferAddressGenerations[index] == this._computeGeneration
            && this._computeRootBufferAddresses[index] == gpuAddress) {
            return false;
        }

        this._computeRootBufferAddresses[index] = gpuAddress;
        this._computeRootBufferAddressGenerations[index] = this._computeGeneration;
        return true;
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
            Util.EnsureArrayMinimumSize(ref this._graphicsRootTablePointerGenerations, rootParameterIndex + 1);
        }

        return index;
    }

    /// <summary>
    /// Slow path for graphics root-table cache updates that need a larger cache array.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="tablePtr">The descriptor table GPU pointer to bind.</param>
    /// <returns><see langword="true" /> when the command list must receive a root-table update.</returns>
    private bool TryUpdateGraphicsRootTableSlow(uint rootParameterIndex, ulong tablePtr) {
        int index = this.EnsureGraphicsRootTableCapacity(rootParameterIndex);
        if (this._graphicsRootTablePointerGenerations[index] == this._graphicsGeneration
            && this._graphicsRootTablePointers[index] == tablePtr) {
            return false;
        }

        this._graphicsRootTablePointers[index] = tablePtr;
        this._graphicsRootTablePointerGenerations[index] = this._graphicsGeneration;
        return true;
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
            Util.EnsureArrayMinimumSize(ref this._computeRootTablePointerGenerations, rootParameterIndex + 1);
        }

        return index;
    }

    /// <summary>
    /// Slow path for compute root-table cache updates that need a larger cache array.
    /// </summary>
    /// <param name="rootParameterIndex">The root parameter index.</param>
    /// <param name="tablePtr">The descriptor table GPU pointer to bind.</param>
    /// <returns><see langword="true" /> when the command list must receive a root-table update.</returns>
    private bool TryUpdateComputeRootTableSlow(uint rootParameterIndex, ulong tablePtr) {
        int index = this.EnsureComputeRootTableCapacity(rootParameterIndex);
        if (this._computeRootTablePointerGenerations[index] == this._computeGeneration
            && this._computeRootTablePointers[index] == tablePtr) {
            return false;
        }

        this._computeRootTablePointers[index] = tablePtr;
        this._computeRootTablePointerGenerations[index] = this._computeGeneration;
        return true;
    }
}
