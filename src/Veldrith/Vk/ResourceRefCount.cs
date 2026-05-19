using System;
using System.Threading;

namespace Veldrith.Vk;

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
    /// Initializes a new instance of the <see cref="ResourceRefCount" /> class.
    /// </summary>
    public ResourceRefCount(Action disposeAction) {
        this.disposeAction = disposeAction;
        this._refCount = 1;
    }

    /// <summary>
    /// Executes Increment.
    /// </summary>
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
    /// Executes Decrement.
    /// </summary>
    public int Decrement() {
        int ret = Interlocked.Decrement(ref this._refCount);
        if (ret == 0) {
            this.disposeAction();
        }

        return ret;
    }
}