using System;
using System.Threading;

namespace Veldrith.MTL;

/// <summary>
/// Represents the MtlFence class.
/// </summary>
internal class MtlFence : Fence {

    /// <summary>
    /// Represents the _disposed field.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlFence" /> type.
    /// </summary>
    /// <param name="signaled">The value of signaled.</param>
    public MtlFence(bool signaled) {
        this.ResetEvent = new ManualResetEvent(signaled);
    }

    /// <summary>
    /// Gets or sets ResetEvent.
    /// </summary>
    public ManualResetEvent ResetEvent { get; }

    /// <summary>
    /// Performs the WaitOne operation.
    /// </summary>
    /// <returns>The result of the WaitOne operation.</returns>
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
    /// Performs the Dispose operation.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this.ResetEvent.Dispose();
            this._disposed = true;
        }
    }

    #endregion

    /// <summary>
    /// Performs the Set operation.
    /// </summary>
    public void Set() {
        this.ResetEvent.Set();
    }

    /// <summary>
    /// Performs the Reset operation.
    /// </summary>
    public override void Reset() {
        this.ResetEvent.Reset();
    }

    /// <summary>
    /// Performs the Wait operation.
    /// </summary>
    /// <param name="nanosecondTimeout">The value of nanosecondTimeout.</param>
    /// <returns>The result of the Wait operation.</returns>
    internal bool Wait(ulong nanosecondTimeout) {
        ulong timeout = Math.Min(int.MaxValue, nanosecondTimeout / 1_000_000);
        return this.ResetEvent.WaitOne((int)timeout);
    }
}