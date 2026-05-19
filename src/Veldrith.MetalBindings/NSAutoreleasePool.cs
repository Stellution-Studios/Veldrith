using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSAutoreleasePool struct.
/// </summary>
public struct NSAutoreleasePool : IDisposable {

    /// <summary>
    /// Performs the new operation.
    /// </summary>
    /// <param name="NSAutoreleasePool">The value of NSAutoreleasePool.</param>
    /// <returns>The result of the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(NSAutoreleasePool));

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSAutoreleasePool" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public NSAutoreleasePool(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Performs the Begin operation.
    /// </summary>
    /// <returns>The result of the Begin operation.</returns>
    public static NSAutoreleasePool Begin() {
        return s_class.AllocInit<NSAutoreleasePool>();
    }

    /// <summary>
    /// Performs the Dispose operation.
    /// </summary>
    public void Dispose() {
        ObjectiveCRuntime.release(this.NativePtr);
    }
}