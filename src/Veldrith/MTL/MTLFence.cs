using System;
using System.Threading;

namespace Veldrith.MTL;

/// <summary>
/// Defines the behavior and responsibilities of the MtlFence class.
/// </summary>
internal class MtlFence : Fence {

    /// <summary>
    /// Stores the value associated with <c>_disposed</c>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MtlFence" /> type.
    /// </summary>
    /// <param name="signaled">Specifies the value of <paramref name="signaled" />.</param>
    public MtlFence(bool signaled) {
        this.ResetEvent = new ManualResetEvent(signaled);
    }

    /// <summary>
    /// Gets or sets ResetEvent.
    /// </summary>
    public ManualResetEvent ResetEvent { get; }

    /// <summary>
    /// Executes the WaitOne operation.
    /// </summary>
    /// <returns>Returns the result produced by the WaitOne operation.</returns>
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
    /// Executes the Dispose operation.
    /// </summary>
    public override void Dispose() {
        if (!this._disposed) {
            this.ResetEvent.Dispose();
            this._disposed = true;
        }
    }

    #endregion

    /// <summary>
    /// Executes the Set operation.
    /// </summary>
    public void Set() {
        this.ResetEvent.Set();
    }

    /// <summary>
    /// Executes the Reset operation.
    /// </summary>
    public override void Reset() {
        this.ResetEvent.Reset();
    }

    /// <summary>
    /// Executes the Wait operation.
    /// </summary>
    /// <param name="nanosecondTimeout">Specifies the value of <paramref name="nanosecondTimeout" />.</param>
    /// <returns>Returns the result produced by the Wait operation.</returns>
    internal bool Wait(ulong nanosecondTimeout) {
        ulong timeout = Math.Min(int.MaxValue, nanosecondTimeout / 1_000_000);
        return this.ResetEvent.WaitOne((int)timeout);
    }
}