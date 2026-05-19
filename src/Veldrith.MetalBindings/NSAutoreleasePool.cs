using System;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSAutoreleasePool struct.
/// </summary>
public struct NSAutoreleasePool : IDisposable {

    /// <summary>
    /// Represents the s_class field.
    /// </summary>
    private static readonly ObjCClass s_class = new(nameof(NSAutoreleasePool));

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSAutoreleasePool" /> class.
    /// </summary>
    public NSAutoreleasePool(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes Begin.
    /// </summary>
    public static NSAutoreleasePool Begin() {
        return s_class.AllocInit<NSAutoreleasePool>();
    }

    /// <summary>
    /// Executes Dispose.
    /// </summary>
    public void Dispose() {
        ObjectiveCRuntime.release(this.NativePtr);
    }
}