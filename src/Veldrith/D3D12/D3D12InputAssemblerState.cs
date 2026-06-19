namespace Veldrith.D3D12;

/// <summary>
/// Tracks D3D12 input-assembler and small graphics pipeline state cached by a command list.
/// </summary>
internal sealed class D3D12InputAssemblerState {

    /// <summary>
    /// Stores the maximum number of vertex buffer slots supported by the cached state.
    /// </summary>
    internal const uint VertexBufferSlotCount = 16;

    /// <summary>
    /// Stores bound vertex buffers by input slot.
    /// </summary>
    private readonly D3D12DeviceBuffer[] _vertexBuffers = new D3D12DeviceBuffer[VertexBufferSlotCount];

    /// <summary>
    /// Stores bound vertex buffer byte offsets by input slot.
    /// </summary>
    private readonly uint[] _vertexBufferOffsets = new uint[VertexBufferSlotCount];

    /// <summary>
    /// Stores recorded vertex buffer strides by input slot.
    /// </summary>
    private readonly uint[] _vertexBufferStrides = new uint[VertexBufferSlotCount];

    /// <summary>
    /// Stores bound vertex buffer versions by input slot for dynamic buffer rebinding.
    /// </summary>
    private readonly ulong[] _vertexBufferVersions = new ulong[VertexBufferSlotCount];

    /// <summary>
    /// Stores the currently bound index buffer.
    /// </summary>
    private D3D12DeviceBuffer _indexBuffer;

    /// <summary>
    /// Stores the byte offset of the currently bound index buffer.
    /// </summary>
    private uint _indexBufferOffset;

    /// <summary>
    /// Stores the dynamic bind version of the currently bound index buffer.
    /// </summary>
    private ulong _indexBufferVersion;

    /// <summary>
    /// Stores the format of the currently bound index buffer.
    /// </summary>
    private IndexFormat _indexFormat = IndexFormat.UInt16;

    /// <summary>
    /// Stores the primitive topology recorded in command-list state.
    /// </summary>
    private Vortice.Direct3D.PrimitiveTopology _primitiveTopology;

    /// <summary>
    /// Stores the stencil reference recorded in command-list state.
    /// </summary>
    private uint _stencilReference;

    /// <summary>
    /// Gets whether an index buffer has been recorded.
    /// </summary>
    internal bool HasIndexBuffer { get; private set; }

    /// <summary>
    /// Gets whether the primitive topology cache matches command-list state.
    /// </summary>
    internal bool PrimitiveTopologyValid { get; private set; }

    /// <summary>
    /// Gets whether the stencil reference cache matches command-list state.
    /// </summary>
    internal bool StencilReferenceValid { get; private set; }

    /// <summary>
    /// Gets the highest bound vertex buffer slot plus one.
    /// </summary>
    internal uint MaxBoundVertexBufferSlot { get; private set; }

    /// <summary>
    /// Gets the currently bound index buffer.
    /// </summary>
    internal D3D12DeviceBuffer IndexBuffer => this._indexBuffer;

    /// <summary>
    /// Gets the byte offset of the currently bound index buffer.
    /// </summary>
    internal uint IndexBufferOffset => this._indexBufferOffset;

    /// <summary>
    /// Gets the format of the currently bound index buffer.
    /// </summary>
    internal IndexFormat IndexFormat => this._indexFormat;

    /// <summary>
    /// Clears all cached state for a new command-list recording.
    /// </summary>
    internal void Reset() {
        int count = System.Math.Min((int)this.MaxBoundVertexBufferSlot, this._vertexBuffers.Length);
        if (count > 0) {
            System.Array.Clear(this._vertexBuffers, 0, count);
            System.Array.Clear(this._vertexBufferOffsets, 0, count);
            System.Array.Clear(this._vertexBufferStrides, 0, count);
            System.Array.Clear(this._vertexBufferVersions, 0, count);
        }

        this._indexBuffer = null;
        this._indexBufferOffset = 0;
        this._indexBufferVersion = 0;
        this._indexFormat = IndexFormat.UInt16;
        this.HasIndexBuffer = false;
        this.MaxBoundVertexBufferSlot = 0;
        this.PrimitiveTopologyValid = false;
        this.StencilReferenceValid = false;
    }

    /// <summary>
    /// Gets the vertex buffer bound at a slot.
    /// </summary>
    /// <param name="index">The vertex buffer slot.</param>
    /// <returns>The bound buffer, or <see langword="null" />.</returns>
    internal D3D12DeviceBuffer GetVertexBuffer(uint index) {
        return this._vertexBuffers[index];
    }

    /// <summary>
    /// Gets the vertex buffer byte offset bound at a slot.
    /// </summary>
    /// <param name="index">The vertex buffer slot.</param>
    /// <returns>The byte offset.</returns>
    internal uint GetVertexBufferOffset(uint index) {
        return this._vertexBufferOffsets[index];
    }

    /// <summary>
    /// Gets the recorded vertex buffer stride at a slot.
    /// </summary>
    /// <param name="index">The vertex buffer slot.</param>
    /// <returns>The recorded stride.</returns>
    internal uint GetVertexBufferStride(uint index) {
        return this._vertexBufferStrides[index];
    }

    /// <summary>
    /// Sets the recorded vertex buffer stride at a slot.
    /// </summary>
    /// <param name="index">The vertex buffer slot.</param>
    /// <param name="stride">The stride recorded into D3D12 state.</param>
    internal void SetVertexBufferStride(uint index, uint stride) {
        this._vertexBufferStrides[index] = stride;
    }

    /// <summary>
    /// Updates a vertex buffer slot and reports whether D3D12 state must be rebound.
    /// </summary>
    /// <param name="index">The vertex buffer slot.</param>
    /// <param name="buffer">The buffer to bind.</param>
    /// <param name="offset">The byte offset to bind.</param>
    /// <param name="bindVersion">The dynamic bind version for the buffer.</param>
    /// <param name="isDynamicBuffer">Whether the buffer can rotate native snapshots.</param>
    /// <returns><see langword="true" /> when the buffer view must be recorded.</returns>
    internal bool TrySetVertexBuffer(uint index, D3D12DeviceBuffer buffer, uint offset, ulong bindVersion, bool isDynamicBuffer) {
        if (index >= VertexBufferSlotCount) {
            return false;
        }

        if (ReferenceEquals(this._vertexBuffers[index], buffer)
            && this._vertexBufferOffsets[index] == offset
            && (!isDynamicBuffer || this._vertexBufferVersions[index] == bindVersion)) {
            return false;
        }

        this._vertexBuffers[index] = buffer;
        this._vertexBufferOffsets[index] = offset;
        this._vertexBufferVersions[index] = bindVersion;
        if (index + 1 > this.MaxBoundVertexBufferSlot) {
            this.MaxBoundVertexBufferSlot = index + 1;
        }

        return true;
    }

    /// <summary>
    /// Updates the cached dynamic version for a rebound vertex buffer slot.
    /// </summary>
    /// <param name="index">The vertex buffer slot.</param>
    /// <param name="bindVersion">The current dynamic bind version.</param>
    internal void SetVertexBufferVersion(uint index, ulong bindVersion) {
        this._vertexBufferVersions[index] = bindVersion;
    }

    /// <summary>
    /// Checks whether an index buffer binding matches cached state.
    /// </summary>
    /// <param name="buffer">The buffer to bind.</param>
    /// <param name="format">The index format to bind.</param>
    /// <param name="offset">The byte offset to bind.</param>
    /// <param name="bindVersion">The dynamic bind version for the buffer.</param>
    /// <param name="isDynamicBuffer">Whether the buffer can rotate native snapshots.</param>
    /// <returns><see langword="true" /> when the index buffer view must be recorded.</returns>
    internal bool NeedsIndexBufferBind(D3D12DeviceBuffer buffer, IndexFormat format, uint offset, ulong bindVersion, bool isDynamicBuffer) {
        return !this.HasIndexBuffer
               || !ReferenceEquals(this._indexBuffer, buffer)
               || this._indexBufferOffset != offset
               || this._indexFormat != format
               || (isDynamicBuffer && this._indexBufferVersion != bindVersion);
    }

    /// <summary>
    /// Stores the currently recorded index buffer binding.
    /// </summary>
    /// <param name="buffer">The bound index buffer.</param>
    /// <param name="format">The index buffer format.</param>
    /// <param name="offset">The bound byte offset.</param>
    /// <param name="bindVersion">The dynamic bind version.</param>
    internal void SetIndexBuffer(D3D12DeviceBuffer buffer, IndexFormat format, uint offset, ulong bindVersion) {
        this._indexBuffer = buffer;
        this._indexBufferOffset = offset;
        this._indexBufferVersion = bindVersion;
        this._indexFormat = format;
        this.HasIndexBuffer = true;
    }

    /// <summary>
    /// Updates the cached dynamic version for a rebound index buffer.
    /// </summary>
    /// <param name="bindVersion">The current dynamic bind version.</param>
    internal void SetIndexBufferVersion(ulong bindVersion) {
        this._indexBufferVersion = bindVersion;
    }

    /// <summary>
    /// Stores a primitive topology if it differs from cached state.
    /// </summary>
    /// <param name="topology">The topology to record.</param>
    /// <returns><see langword="true" /> when D3D12 state must be updated.</returns>
    internal bool TrySetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology topology) {
        if (this.PrimitiveTopologyValid && this._primitiveTopology == topology) {
            return false;
        }

        this._primitiveTopology = topology;
        this.PrimitiveTopologyValid = true;
        return true;
    }

    /// <summary>
    /// Stores a stencil reference if it differs from cached state.
    /// </summary>
    /// <param name="stencilReference">The stencil reference to record.</param>
    /// <returns><see langword="true" /> when D3D12 state must be updated.</returns>
    internal bool TrySetStencilReference(uint stencilReference) {
        if (this.StencilReferenceValid && this._stencilReference == stencilReference) {
            return false;
        }

        this._stencilReference = stencilReference;
        this.StencilReferenceValid = true;
        return true;
    }
}
