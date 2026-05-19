using System;
using System.Threading;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

/// <summary>
/// Represents the D3D12Fence class.
/// </summary>
internal sealed class D3D12Fence : Fence {

    /// <summary>
    /// Represents the _nativeFence field.
    /// </summary>
    private readonly ID3D12Fence _nativeFence;

    /// <summary>
    /// Represents the _waitEvent field.
    /// </summary>
    private readonly AutoResetEvent _waitEvent;

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Represents the _fenceValue field.
    /// </summary>
    private ulong _fenceValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="D3D12Fence" /> class.
    /// </summary>
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
    /// Executes Dispose.
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
    /// Executes Reset.
    /// </summary>
    public override void Reset() {
        this._fenceValue = this._nativeFence.CompletedValue + 1;
    }

    /// <summary>
    /// Executes Signal.
    /// </summary>
    internal void Signal(ID3D12CommandQueue commandQueue) {
        if (this._fenceValue <= this._nativeFence.CompletedValue) {
            this._fenceValue = this._nativeFence.CompletedValue + 1;
        }

        commandQueue.Signal(this._nativeFence, this._fenceValue).CheckError();
    }

    /// <summary>
    /// Executes Wait.
    /// </summary>
    internal bool Wait(ulong nanosecondTimeout) {
        if (this.Signaled) {
            return true;
        }

        this._nativeFence.SetEventOnCompletion(this._fenceValue, this._waitEvent.SafeWaitHandle.DangerousGetHandle())
            .CheckError();

        if (nanosecondTimeout == ulong.MaxValue) {
            this._waitEvent.WaitOne();
            return true;
        }

        int milliseconds = nanosecondTimeout > 0
            ? (int)Math.Min(int.MaxValue, nanosecondTimeout / 1_000_000)
            : 0;
        return this._waitEvent.WaitOne(milliseconds);
    }
}