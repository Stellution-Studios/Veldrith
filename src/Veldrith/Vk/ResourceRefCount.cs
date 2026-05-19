using System;
using System.Threading;

namespace Veldrith.Vk;

/// <summary>
/// Defines the behavior and responsibilities of the ResourceRefCount class.
/// </summary>
internal class ResourceRefCount {

    /// <summary>
    /// Stores the value associated with <c>disposeAction</c>.
    /// </summary>
    private readonly Action disposeAction;

    /// <summary>
    /// Stores the value associated with <c>_refCount</c>.
    /// </summary>
    private int _refCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRefCount" /> type.
    /// </summary>
    /// <param name="disposeAction">Specifies the value of <paramref name="disposeAction" />.</param>
    public ResourceRefCount(Action disposeAction) {
        this.disposeAction = disposeAction;
        this._refCount = 1;
    }

    /// <summary>
    /// Executes the Increment operation.
    /// </summary>
    /// <returns>Returns the result produced by the Increment operation.</returns>
    public int Increment() {
        int ret = Interlocked.Increment(ref this._refCount);
#if VALIDATE_USAGE
        if (ret == 0) {
            throw new VeldridException("An attempt was made to reference a disposed resource.");
        }
#endif
        return ret;
    }

    /// <summary>
    /// Executes the Decrement operation.
    /// </summary>
    /// <returns>Returns the result produced by the Decrement operation.</returns>
    public int Decrement() {
        int ret = Interlocked.Decrement(ref this._refCount);
        if (ret == 0) {
            this.disposeAction();
        }

        return ret;
    }
}