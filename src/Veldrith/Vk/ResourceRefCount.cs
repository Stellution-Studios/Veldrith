using System;
using System.Threading;

namespace Veldrith.Vk;

/// <summary>
/// Represents the ResourceRefCount type used by the graphics runtime.
/// </summary>
internal class ResourceRefCount {

    /// <summary>
    /// Stores the dispose action state used by this instance.
    /// </summary>
    private readonly Action disposeAction;

    /// <summary>
    /// Stores the ref count value used during command execution.
    /// </summary>
    private int _refCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRefCount" /> type.
    /// </summary>
    /// <param name="disposeAction">The dispose action value used by this operation.</param>
    public ResourceRefCount(Action disposeAction) {
        this.disposeAction = disposeAction;
        this._refCount = 1;
    }

    /// <summary>
    /// Executes the increment logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
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
    /// Executes the decrement logic for this backend.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public int Decrement() {
        int ret = Interlocked.Decrement(ref this._refCount);
        if (ret == 0) {
            this.disposeAction();
        }

        return ret;
    }
}