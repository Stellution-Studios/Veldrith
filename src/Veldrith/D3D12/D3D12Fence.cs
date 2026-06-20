using System;
using System.Threading;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Provides the Direct3D 12 backend implementation for D3D12Fence.
/// </summary>
internal sealed class D3D12Fence : Fence {

    /// <summary>
    /// Stores the native fence state used by this instance.
    /// </summary>
    private readonly ID3D12Fence _nativeFence;

    /// <summary>
    /// Stores the wait event state used by this instance.
    /// </summary>
    private readonly AutoResetEvent _waitEvent;

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Stores the fence value state used by this instance.
    /// </summary>
    private ulong _fenceValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Fence" /> type.
    /// </summary>
    /// <param name="gd">The graphics device that owns this operation.</param>
    /// <param name="signaled">The signaled value used by this operation.</param>
    public D3D12Fence(D3D12GraphicsDevice gd, bool signaled) {
        ulong initialValue = signaled ? 1UL : 0UL;
        this._nativeFence = gd.Device.CreateFence(initialValue);
        this._waitEvent = new AutoResetEvent(false);
        this._fenceValue = initialValue;
    }

    /// <summary>
    /// Gets or sets Signaled.
    /// </summary>
    public override bool Signaled => this._nativeFence.CompletedValue >= this._fenceValue;

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (this._disposed) {
            return;
        }

        this._disposed = true;
        this._waitEvent.Dispose();
        this._nativeFence.Dispose();
    }

    /// <summary>
    /// Resets this instance to its initial state.
    /// </summary>
    public override void Reset() {
        this._fenceValue = this._nativeFence.CompletedValue + 1;
    }

    /// <summary>
    /// Executes the signal logic for this backend.
    /// </summary>
    /// <param name="commandQueue">The command queue value used by this operation.</param>
    internal void Signal(ID3D12CommandQueue commandQueue) {
        if (this._fenceValue <= this._nativeFence.CompletedValue) {
            this._fenceValue = this._nativeFence.CompletedValue + 1;
        }

        commandQueue.Signal(this._nativeFence, this._fenceValue).CheckError();
    }

    /// <summary>
    /// Executes the wait logic for this backend.
    /// </summary>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool Wait(ulong nanosecondTimeout) {
        if (this.Signaled) {
            return true;
        }

        this._nativeFence.SetEventOnCompletion(this._fenceValue, this._waitEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();

        if (nanosecondTimeout == ulong.MaxValue) {
            this._waitEvent.WaitOne();
            return true;
        }

        int milliseconds = nanosecondTimeout > 0 ? (int)Math.Min(int.MaxValue, nanosecondTimeout / 1_000_000) : 0;
        return this._waitEvent.WaitOne(milliseconds);
    }
}
