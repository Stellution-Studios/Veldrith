using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the NSObject struct.
/// </summary>
public struct NSObject {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSObject" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public NSObject(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the IsKindOfClass operation.
    /// </summary>
    /// <param name="class">Specifies the value of <paramref name="class" />.</param>
    /// <returns>Returns the result produced by the IsKindOfClass operation.</returns>
    public Bool8 IsKindOfClass(IntPtr @class) {
        return bool8_objc_msgSend(this.NativePtr, sel_isKindOfClass, @class);
    }

    /// <summary>
    /// Stores the value associated with <c>sel_isKindOfClass</c>.
    /// </summary>
    private static readonly Selector sel_isKindOfClass = "isKindOfClass:";
}