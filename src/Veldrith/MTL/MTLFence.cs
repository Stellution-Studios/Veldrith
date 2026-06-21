using System;
using System.Threading;

namespace Veldrith.MTL;

/// <summary>
/// Provides the Metal backend implementation for MtlFence.
/// </summary>
internal class MtlFence : Fence {

    /// <summary>
    /// Stores the disposed state used by this instance.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlFence" /> type.
    /// </summary>
    /// <param name="signaled">The signaled value used by this operation.</param>
    public MtlFence(bool signaled) {
        this.ResetEvent = new ManualResetEvent(signaled);
    }

    /// <summary>
    /// Gets or sets ResetEvent.
    /// </summary>
    public ManualResetEvent ResetEvent { get; }

    /// <summary>
    /// Executes the wait one logic for this backend.
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
    /// Releases resources held by this instance.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this.ResetEvent.Dispose();
            this._disposed = true;
        }
    }

    #endregion

    /// <summary>
    /// Sets the value value.
    /// </summary>
    public void Set() {
        this.ResetEvent.Set();
    }

    /// <summary>
    /// Resets this instance to its initial state.
    /// </summary>
    public override void Reset() {
        this.ResetEvent.Reset();
    }

    /// <summary>
    /// Executes the wait logic for this backend.
    /// </summary>
    /// <param name="nanosecondTimeout">The nanosecond timeout value used by this operation.</param>
    /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
    internal bool Wait(ulong nanosecondTimeout) {
        ulong timeout = Math.Min(int.MaxValue, nanosecondTimeout / 1_000_000);
        return this.ResetEvent.WaitOne((int)timeout);
    }
}