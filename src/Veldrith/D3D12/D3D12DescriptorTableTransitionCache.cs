namespace Veldrith.D3D12;

/// <summary>
/// Caches descriptor-table texture transition state for a resource set.
/// </summary>
internal struct D3D12DescriptorTableTransitionCache {

    /// <summary>
    /// Stores the binding-plan table signature that populated this cache.
    /// </summary>
    private uint _signature;

    /// <summary>
    /// Stores the combined state-version hash of the textures covered by this table.
    /// </summary>
    private ulong _stateVersionHash;

    /// <summary>
    /// Stores the number of texture descriptors covered by this table.
    /// </summary>
    private uint _textureCount;

    /// <summary>
    /// Tracks whether this cache contains a valid table transition state.
    /// </summary>
    private bool _valid;

    /// <summary>
    /// Checks whether this cache still describes the requested descriptor table.
    /// </summary>
    /// <param name="signature">The binding-plan table signature.</param>
    /// <param name="stateVersionHash">The combined texture state-version hash.</param>
    /// <param name="textureCount">The number of texture descriptors in the table.</param>
    /// <returns><see langword="true" /> when descriptor-table texture transitions can be skipped.</returns>
    internal readonly bool Matches(uint signature, ulong stateVersionHash, uint textureCount) {
        return this._valid
               && this._signature == signature
               && this._stateVersionHash == stateVersionHash
               && this._textureCount == textureCount;
    }

    /// <summary>
    /// Stores descriptor-table transition state after the table's textures have been prepared.
    /// </summary>
    /// <param name="signature">The binding-plan table signature.</param>
    /// <param name="stateVersionHash">The combined texture state-version hash.</param>
    /// <param name="textureCount">The number of texture descriptors in the table.</param>
    internal void Store(uint signature, ulong stateVersionHash, uint textureCount) {
        this._signature = signature;
        this._stateVersionHash = stateVersionHash;
        this._textureCount = textureCount;
        this._valid = true;
    }
}
