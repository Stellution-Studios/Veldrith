using System;
using System.Collections.Generic;
using Veldrith.MetalBindings;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlBuffer.
/// </summary>
internal class MtlBuffer : DeviceBuffer {

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Synchronizes access to the last submitted command buffer using this buffer.
    /// </summary>
    private readonly object _submittedUseLock = new();

    /// <summary>
    /// Stores the last in-flight command buffer which may read this dynamic buffer.
    /// </summary>
    private MTLCommandBuffer _lastSubmittedUse;

    /// <summary>
    /// Stores the graphics device that owns this buffer.
    /// </summary>
    private readonly MtlGraphicsDevice _gd;

    /// <summary>
    /// Stores renamed buffer backings waiting for prior GPU work to complete.
    /// </summary>
    private readonly List<PendingBufferUse> _pendingRenamedBuffers = new();

    /// <summary>
    /// Stores renamed buffer backings available for reuse.
    /// </summary>
    private readonly List<MTLBuffer> _availableRenamedBuffers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlBuffer" /> type.
    /// </summary>
    /// <param name="bd">The bd value used by this operation.</param>
    /// <param name="gd">The graphics device that owns this operation.</param>
    public MtlBuffer(ref BufferDescription bd, MtlGraphicsDevice gd) {
        this._gd = gd;
        this.SizeInBytes = bd.SizeInBytes;
        uint roundFactor = (4 - this.SizeInBytes % 4) % 4;
        this.ActualCapacity = this.SizeInBytes + roundFactor;
        this.Usage = bd.Usage;

        bool sharedMemory = (this.Usage & (BufferUsage.Staging | BufferUsage.Dynamic)) != 0;
        MTLResourceOptions bufferOptions = sharedMemory ? MTLResourceOptions.StorageModeShared : MTLResourceOptions.StorageModePrivate;

        this.DeviceBuffer = this._gd.Device.NewBufferWithLengthOptions(this.ActualCapacity, bufferOptions);

        unsafe {
            if (sharedMemory) {
                this.Pointer = this.DeviceBuffer.Contents();
            }
        }
    }

    /// <summary>
    /// Gets or sets SizeInBytes.
    /// </summary>
    public override uint SizeInBytes { get; }

    /// <summary>
    /// Gets or sets Usage.
    /// </summary>
    public override BufferUsage Usage { get; }

    /// <summary>
    /// Gets or sets ActualCapacity.
    /// </summary>
    public uint ActualCapacity { get; }

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name {
        get;
        set {
            NSString nameNss = NSString.New(value);
            this.DeviceBuffer.AddDebugMarker(nameNss, new NSRange(0, this.SizeInBytes));
            ObjectiveCRuntime.Release(nameNss.NativePtr);
            field = value;
        }
    }

    /// <summary>
    /// Gets or sets DeviceBuffer.
    /// </summary>
    public MTLBuffer DeviceBuffer { get; private set; }

    /// <summary>
    /// Gets or sets Pointer.
    /// </summary>
    public unsafe void* Pointer { get; private set; }

    /// <summary>
    /// Prepares this dynamic buffer for a CPU write without stalling on in-flight GPU use.
    /// </summary>
    public void PrepareForCpuUpdate() {
        if ((this.Usage & BufferUsage.Dynamic) == 0) {
            return;
        }

        lock (this._submittedUseLock) {
            this.RecycleCompletedRenamedBuffers_NoLock();

            if (this._lastSubmittedUse.NativePtr == IntPtr.Zero) {
                return;
            }

            if (this._lastSubmittedUse.Status == MTLCommandBufferStatus.Completed) {
                ObjectiveCRuntime.Release(this._lastSubmittedUse.NativePtr);
                this._lastSubmittedUse = default;
                return;
            }

            this._pendingRenamedBuffers.Add(new PendingBufferUse(this.DeviceBuffer, this._lastSubmittedUse));
            this._lastSubmittedUse = default;

            int lastAvailableIndex = this._availableRenamedBuffers.Count - 1;
            if (lastAvailableIndex >= 0) {
                this.DeviceBuffer = this._availableRenamedBuffers[lastAvailableIndex];
                this._availableRenamedBuffers.RemoveAt(lastAvailableIndex);
            }
            else {
                this.DeviceBuffer = this.CreateSharedBuffer();
            }

            unsafe {
                this.Pointer = this.DeviceBuffer.Contents();
            }
        }
    }

    /// <summary>
    /// Tracks a submitted command buffer which may read this dynamic buffer.
    /// </summary>
    /// <param name="cb">The submitted command buffer.</param>
    public void TrackSubmittedUse(MTLCommandBuffer cb) {
        if ((this.Usage & BufferUsage.Dynamic) == 0 || cb.NativePtr == IntPtr.Zero) {
            return;
        }

        MTLCommandBuffer previous;
        lock (this._submittedUseLock) {
            if (this._lastSubmittedUse.NativePtr == cb.NativePtr) {
                return;
            }

            ObjectiveCRuntime.Retain(cb.NativePtr);
            previous = this._lastSubmittedUse;
            this._lastSubmittedUse = cb;
        }

        if (previous.NativePtr != IntPtr.Zero) {
            ObjectiveCRuntime.Release(previous.NativePtr);
        }
    }

    /// <summary>
    /// Waits until the last submitted command buffer using this dynamic buffer has completed.
    /// </summary>
    public void WaitForPendingUse() {
        MTLCommandBuffer cb;

        lock (this._submittedUseLock) {
            cb = this._lastSubmittedUse;

            if (cb.NativePtr == IntPtr.Zero) {
                return;
            }

            ObjectiveCRuntime.Retain(cb.NativePtr);
        }

        try {
            if (cb.Status != MTLCommandBufferStatus.Completed) {
                cb.WaitUntilCompleted();
            }
        }
        finally {
            bool releaseTrackedUse = false;

            lock (this._submittedUseLock) {
                if (this._lastSubmittedUse.NativePtr == cb.NativePtr && cb.Status == MTLCommandBufferStatus.Completed) {
                    this._lastSubmittedUse = default;
                    releaseTrackedUse = true;
                }
            }

            if (releaseTrackedUse) {
                ObjectiveCRuntime.Release(cb.NativePtr);
            }

            ObjectiveCRuntime.Release(cb.NativePtr);
        }
    }

    /// <summary>
    /// Creates a shared Metal buffer backing compatible with this buffer.
    /// </summary>
    /// <returns>The created Metal buffer.</returns>
    private MTLBuffer CreateSharedBuffer() {
        return this._gd.Device.NewBufferWithLengthOptions(this.ActualCapacity, MTLResourceOptions.StorageModeShared);
    }

    /// <summary>
    /// Moves completed renamed backings into the reuse list.
    /// </summary>
    private void RecycleCompletedRenamedBuffers_NoLock() {
        for (int i = this._pendingRenamedBuffers.Count - 1; i >= 0; i--) {
            PendingBufferUse pending = this._pendingRenamedBuffers[i];

            if (pending.CommandBuffer.Status != MTLCommandBufferStatus.Completed) {
                continue;
            }

            this._availableRenamedBuffers.Add(pending.Buffer);
            ObjectiveCRuntime.Release(pending.CommandBuffer.NativePtr);
            this._pendingRenamedBuffers.RemoveAt(i);
        }
    }

    #region Disposal

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this._disposed = true;
            if (this._lastSubmittedUse.NativePtr != IntPtr.Zero) {
                ObjectiveCRuntime.Release(this._lastSubmittedUse.NativePtr);
                this._lastSubmittedUse = default;
            }

            foreach (PendingBufferUse pending in this._pendingRenamedBuffers) {
                ObjectiveCRuntime.Release(pending.CommandBuffer.NativePtr);
                ObjectiveCRuntime.Release(pending.Buffer.NativePtr);
            }

            this._pendingRenamedBuffers.Clear();

            foreach (MTLBuffer buffer in this._availableRenamedBuffers) {
                ObjectiveCRuntime.Release(buffer.NativePtr);
            }

            this._availableRenamedBuffers.Clear();

            ObjectiveCRuntime.Release(this.DeviceBuffer.NativePtr);
        }
    }

    #endregion

    /// <summary>
    /// Stores a renamed backing buffer and the command buffer still using it.
    /// </summary>
    /// <param name="buffer">The renamed backing buffer.</param>
    /// <param name="commandBuffer">The command buffer using the backing buffer.</param>
    private readonly struct PendingBufferUse(MTLBuffer buffer, MTLCommandBuffer commandBuffer) {
        public readonly MTLBuffer Buffer = buffer;
        public readonly MTLCommandBuffer CommandBuffer = commandBuffer;
    }
}
