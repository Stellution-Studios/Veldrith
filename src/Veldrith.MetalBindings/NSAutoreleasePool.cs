using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the NSAutoreleasePool struct.
/// </summary>
public struct NSAutoreleasePool : IDisposable {

    /// <summary>
    /// Stores the value associated with <c>name</c>.
    /// </summary>
    /// <param name="NSAutoreleasePool">Specifies the value of <paramref name="NSAutoreleasePool" />.</param>
    /// <returns>Returns the result produced by the new operation.</returns>
    private static readonly ObjCClass s_class = new(nameof(NSAutoreleasePool));

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSAutoreleasePool" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public NSAutoreleasePool(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the Begin operation.
    /// </summary>
    /// <returns>Returns the result produced by the Begin operation.</returns>
    public static NSAutoreleasePool Begin() {
        return s_class.AllocInit<NSAutoreleasePool>();
    }

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose() {
        ObjectiveCRuntime.release(this.NativePtr);
    }
}
