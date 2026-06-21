using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

#if !VELDRID_D3D12_PERF
#pragma warning disable CS0162
#endif

namespace Veldrith.D3D12;

/// <summary>
/// Records tiny aligned GPU-local buffer updates through D3D12 WriteBufferImmediate.
/// </summary>
internal sealed class D3D12ImmediateBufferWriter {

    /// <summary>
    /// Stores the default largest update that should use WriteBufferImmediate.
    /// </summary>
    private const uint DefaultMaxImmediateWriteBytes = 256;

    /// <summary>
    /// Stores the maximum number of pending 32-bit writes before the batch is flushed.
    /// </summary>
    private const uint MaxPendingDwords = 1024;

    /// <summary>
    /// Stores the configured largest update that should use WriteBufferImmediate.
    /// </summary>
    private static readonly uint MaxImmediateWriteBytes = ReadMaxImmediateWriteBytes();

    /// <summary>
    /// Stores the command list that receives barriers and immediate writes.
    /// </summary>
    private readonly D3D12CommandList _commandList;

    /// <summary>
    /// Stores optional performance counters updated while writes are recorded.
    /// </summary>
    private readonly D3D12CommandListPerfTracker _perf;

    /// <summary>
    /// Stores pending WriteBufferImmediate parameters.
    /// </summary>
    private WriteBufferImmediateParameter[] _parameters = new WriteBufferImmediateParameter[64];

    /// <summary>
    /// Stores unique destination buffers touched by the pending write batch.
    /// </summary>
    private readonly List<PendingBufferState> _pendingBuffers = new(8);

    /// <summary>
    /// Stores the most recently tracked pending buffer.
    /// </summary>
    private D3D12DeviceBuffer _lastPendingBuffer;

    /// <summary>
    /// Stores the number of pending 32-bit writes.
    /// </summary>
    private uint _parameterCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12ImmediateBufferWriter" /> class.
    /// </summary>
    /// <param name="commandList">The command list that receives writes.</param>
    /// <param name="perf">The optional performance tracker.</param>
    internal D3D12ImmediateBufferWriter(D3D12CommandList commandList, D3D12CommandListPerfTracker perf) {
        this._commandList = commandList;
        this._perf = perf;
    }

    /// <summary>
    /// Checks whether an update can use WriteBufferImmediate.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="bufferOffsetInBytes">The destination byte offset.</param>
    /// <param name="sizeInBytes">The update size.</param>
    /// <returns><see langword="true" /> when the fast path can be used.</returns>
    internal bool CanWrite(D3D12DeviceBuffer buffer, uint bufferOffsetInBytes, uint sizeInBytes) {
        return this._commandList.NativeCommandList2 != null
               && MaxImmediateWriteBytes != 0
               && sizeInBytes <= MaxImmediateWriteBytes
               && (bufferOffsetInBytes & 3) == 0
               && (sizeInBytes & 3) == 0
               && buffer.CanTransitionState;
    }

    /// <summary>
    /// Gets whether any immediate writes are waiting to be recorded.
    /// </summary>
    internal bool HasPendingWrites => this._parameterCount != 0;

    /// <summary>
    /// Queues one aligned update into the pending WriteBufferImmediate batch.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="bufferOffsetInBytes">The destination byte offset.</param>
    /// <param name="source">The source data pointer.</param>
    /// <param name="sizeInBytes">The number of bytes to write.</param>
    internal unsafe void Queue(D3D12DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes) {
        buffer.ValidateBufferUpdateRange(bufferOffsetInBytes, sizeInBytes);
        uint dwordCount = sizeInBytes >> 2;
        if (this._parameterCount + dwordCount > MaxPendingDwords) {
            this.Flush();
        }

        this.EnsureParameterCapacity(this._parameterCount + dwordCount);
        this.TrackBuffer(buffer);

        ulong destination = buffer.GetGpuVirtualAddress(bufferOffsetInBytes);
        byte* sourceBytes = (byte*)source.ToPointer();
        for (uint i = 0; i < dwordCount; i++) {
            uint value = Unsafe.ReadUnaligned<uint>(sourceBytes + i * 4);
            this._parameters[this._parameterCount++] = new WriteBufferImmediateParameter(destination + i * 4, value);
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.ImmediateBufferWriteBytes += sizeInBytes;
            this._perf.ImmediateBufferWriteDwords += dwordCount;
        }
    }

    /// <summary>
    /// Flushes queued immediate writes into the native command list.
    /// </summary>
    /// <returns><see langword="true" /> when any writes were recorded.</returns>
    internal bool Flush() {
        if (this._parameterCount == 0) {
            return false;
        }

        this._commandList.FlushPendingUavBarrierForInternalUse();
        for (int i = 0; i < this._pendingBuffers.Count; i++) {
            this._commandList.TransitionBufferForInternalUse(this._pendingBuffers[i].Buffer, ResourceStates.CopyDest);
        }

        this._commandList.FlushPendingBarriersForInternalUse();
        unsafe {
            fixed (WriteBufferImmediateParameter* parameters = this._parameters) {
                WriteBufferImmediateNoAlloc(this._commandList.NativeCommandList2, this._parameterCount, parameters);
            }
        }

        for (int i = 0; i < this._pendingBuffers.Count; i++) {
            PendingBufferState state = this._pendingBuffers[i];
            this._commandList.TransitionBufferForInternalUse(state.Buffer, state.PreviousState);
        }

        if (D3D12CommandListPerfTracker.Enabled) {
            this._perf.ImmediateBufferWriteBatches++;
        }

        this.Discard();
        return true;
    }

    /// <summary>
    /// Records WriteBufferImmediate through the native vtable without allocating wrapper arrays.
    /// </summary>
    /// <param name="commandList">The command-list interface.</param>
    /// <param name="count">The number of 32-bit writes.</param>
    /// <param name="parameters">The write parameters.</param>
    private static unsafe void WriteBufferImmediateNoAlloc(ID3D12GraphicsCommandList2 commandList, uint count, WriteBufferImmediateParameter* parameters) {
        void** vtbl = *(void***)commandList.NativePointer;
        delegate* unmanaged[Stdcall]<void*, uint, void*, void*, void> writeBufferImmediate = (delegate* unmanaged[Stdcall]<void*, uint, void*, void*, void>)vtbl[66];
        writeBufferImmediate((void*)commandList.NativePointer, count, parameters, null);
    }

    /// <summary>
    /// Discards queued writes without recording them.
    /// </summary>
    internal void Discard() {
        this._parameterCount = 0;
        this._pendingBuffers.Clear();
        this._lastPendingBuffer = null;
    }

    /// <summary>
    /// Ensures that the reusable parameter array is large enough.
    /// </summary>
    /// <param name="requiredCount">The required parameter count.</param>
    private void EnsureParameterCapacity(uint requiredCount) {
        if (requiredCount <= (uint)this._parameters.Length) {
            return;
        }

        uint newSize = (uint)this._parameters.Length;
        while (newSize < requiredCount) {
            newSize *= 2;
        }

        Array.Resize(ref this._parameters, checked((int)newSize));
    }

    /// <summary>
    /// Tracks a buffer's restore state once per pending batch.
    /// </summary>
    /// <param name="buffer">The buffer to track.</param>
    private void TrackBuffer(D3D12DeviceBuffer buffer) {
        if (ReferenceEquals(this._lastPendingBuffer, buffer)) {
            return;
        }

        for (int i = 0; i < this._pendingBuffers.Count; i++) {
            if (ReferenceEquals(this._pendingBuffers[i].Buffer, buffer)) {
                this._lastPendingBuffer = buffer;
                return;
            }
        }

        this._pendingBuffers.Add(new PendingBufferState(buffer, buffer.CurrentState));
        this._lastPendingBuffer = buffer;
    }

    /// <summary>
    /// Reads the configured maximum WriteBufferImmediate update size.
    /// </summary>
    /// <returns>The maximum update size in bytes.</returns>
    private static uint ReadMaxImmediateWriteBytes() {
        string value = Environment.GetEnvironmentVariable("VELDRID_D3D12_WRITEBUFFERIMMEDIATE_MAX_BYTES");
        if (uint.TryParse(value, out uint parsed)) {
            return parsed;
        }

        return DefaultMaxImmediateWriteBytes;
    }

    /// <summary>
    /// Represents the previous state of a buffer touched by queued immediate writes.
    /// </summary>
    private readonly struct PendingBufferState {

        /// <summary>
        /// Stores the buffer.
        /// </summary>
        public readonly D3D12DeviceBuffer Buffer;

        /// <summary>
        /// Stores the state to restore after queued writes.
        /// </summary>
        public readonly ResourceStates PreviousState;

        /// <summary>
        /// Initializes a new instance of the <see cref="PendingBufferState" /> struct.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="previousState">The state to restore after queued writes.</param>
        public PendingBufferState(D3D12DeviceBuffer buffer, ResourceStates previousState) {
            this.Buffer = buffer;
            this.PreviousState = previousState;
        }
    }
}
