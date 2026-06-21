using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSAutoreleasePool data structure used by the graphics runtime.
/// </summary>
public struct NSAutoreleasePool : IDisposable {

    /// <summary>
    /// Stores the s class state used by this instance.
    /// </summary>
    private static readonly ObjCClass _sClass = new(nameof(NSAutoreleasePool));

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSAutoreleasePool" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public NSAutoreleasePool(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Begins the value operation.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    public static NSAutoreleasePool Begin() {
        return _sClass.AllocInit<NSAutoreleasePool>();
    }

    /// <summary>
    /// Releases resources held by this instance.
    /// </summary>
    public void Dispose() {
        ObjectiveCRuntime.Release(this.NativePtr);
    }
}