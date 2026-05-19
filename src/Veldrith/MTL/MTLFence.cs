using System;
using System.Threading;

namespace Veldrith.MTL;

internal class MtlFence : Fence {

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlFence" /> class.
    /// </summary>
    public MtlFence(bool signaled) {
        this.ResetEvent = new ManualResetEvent(signaled);
    }

    /// <summary>
    /// Gets or sets ResetEvent.
    /// </summary>
    public ManualResetEvent ResetEvent { get; }

    /// <summary>
    /// Gets or sets Signaled.
    /// </summary>
    public override bool Signaled => this.ResetEvent.WaitOne(0);

    /// <summary>
    /// Gets or sets IsDisposed.
    /// </summary>
    public override bool IsDisposed => this._disposed;

    /// <summary>
    /// Gets or sets Name.
    /// </summary>
    public override string Name { get; set; }

    #region Disposal

    /// <summary>
    /// Executes Dispose.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this.ResetEvent.Dispose();
            this._disposed = true;
        }
    }

    #endregion

    /// <summary>
    /// Executes Set.
    /// </summary>
    public void Set() {
        this.ResetEvent.Set();
    }

    /// <summary>
    /// Executes Reset.
    /// </summary>
    public override void Reset() {
        this.ResetEvent.Reset();
    }

    /// <summary>
    /// Executes Wait.
    /// </summary>
    internal bool Wait(ulong nanosecondTimeout) {
        ulong timeout = Math.Min(int.MaxValue, nanosecondTimeout / 1_000_000);
        return this.ResetEvent.WaitOne((int)timeout);
    }
}