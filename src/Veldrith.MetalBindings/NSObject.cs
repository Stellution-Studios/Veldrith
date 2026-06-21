using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSObject data structure used by the graphics runtime.
/// </summary>
public struct NSObject {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSObject" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public NSObject(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the is kind of class logic for this backend.
    /// </summary>
    /// <param name="class">The class value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public Bool8 IsKindOfClass(IntPtr @class) {
        return Bool8ObjcMsgSend(this.NativePtr, _selIsKindOfClass, @class);
    }

    /// <summary>
    /// Stores the sel is kind of class state used by this instance.
    /// </summary>
    private static readonly Selector _selIsKindOfClass = "isKindOfClass:";
}
