using System;
using System.Threading;
using Vortice.Direct3D12;

namespace Veldrith.D3D12;

internal sealed class D3D12Fence : Fence {
    private readonly ID3D12Fence _nativeFence;
    private readonly AutoResetEvent _waitEvent;
    private bool _disposed;
    private ulong _fenceValue;

    public D3D12Fence(D3D12GraphicsDevice gd, bool signaled) {
        ulong initialValue = signaled ? 1UL : 0UL;
        this._nativeFence = gd.Device.CreateFence(initialValue);
        this._waitEvent = new AutoResetEvent(false);
        this._fenceValue = initialValue;
    }

    public override bool Signaled => this._nativeFence.CompletedValue >= this._fenceValue;
    public override bool IsDisposed => this._disposed;

    public override string Name { get; set; }

    public override void Dispose() {
        if (this._disposed) {
            return;
        }

        this._disposed = true;
        this._waitEvent.Dispose();
        this._nativeFence.Dispose();
    }

    public override void Reset() {
        this._fenceValue = this._nativeFence.CompletedValue + 1;
    }

    internal void Signal(ID3D12CommandQueue commandQueue) {
        if (this._fenceValue <= this._nativeFence.CompletedValue) {
            this._fenceValue = this._nativeFence.CompletedValue + 1;
        }

        commandQueue.Signal(this._nativeFence, this._fenceValue).CheckError();
    }

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