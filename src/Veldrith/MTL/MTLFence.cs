using System;
using System.Threading;

namespace Veldrith.MTL;

internal class MtlFence : Fence {
    private bool _disposed;

    public MtlFence(bool signaled) {
        this.ResetEvent = new ManualResetEvent(signaled);
    }

    public ManualResetEvent ResetEvent { get; }

    public override bool Signaled => this.ResetEvent.WaitOne(0);
    public override bool IsDisposed => this._disposed;

    public override string Name { get; set; }

    #region Disposal

    public override void Dispose() {
        if (!this._disposed) {
            this.ResetEvent.Dispose();
            this._disposed = true;
        }
    }

    #endregion

    public void Set() {
        this.ResetEvent.Set();
    }

    public override void Reset() {
        this.ResetEvent.Reset();
    }

    internal bool Wait(ulong nanosecondTimeout) {
        ulong timeout = Math.Min(int.MaxValue, nanosecondTimeout / 1_000_000);
        return this.ResetEvent.WaitOne((int)timeout);
    }
}