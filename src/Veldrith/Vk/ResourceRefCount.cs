using System;
using System.Threading;

namespace Veldrith.Vk;

/// <summary>
/// Represents the ResourceRefCount class.
/// </summary>
internal class ResourceRefCount {

    /// <summary>
    /// Represents the disposeAction field.
    /// </summary>
    private readonly Action disposeAction;

    /// <summary>
    /// Represents the _refCount field.
    /// </summary>
    private int _refCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRefCount" /> type.
    /// </summary>
    /// <param name="disposeAction">The value of disposeAction.</param>
    public ResourceRefCount(Action disposeAction) {
        this.disposeAction = disposeAction;
        this._refCount = 1;
    }

    /// <summary>
    /// Performs the Increment operation.
    /// </summary>
    /// <returns>The result of the Increment operation.</returns>
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
    /// Performs the Decrement operation.
    /// </summary>
    /// <returns>The result of the Decrement operation.</returns>
    public int Decrement() {
        int ret = Interlocked.Decrement(ref this._refCount);
        if (ret == 0) {
            this.disposeAction();
        }

        return ret;
    }
}