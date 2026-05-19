using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the NSObject struct.
/// </summary>
public struct NSObject {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="NSObject" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public NSObject(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Performs the IsKindOfClass operation.
    /// </summary>
    /// <param name="class">The value of class.</param>
    /// <returns>The result of the IsKindOfClass operation.</returns>
    public Bool8 IsKindOfClass(IntPtr @class) {
        return bool8_objc_msgSend(this.NativePtr, sel_isKindOfClass, @class);
    }

    /// <summary>
    /// Represents the sel_isKindOfClass field.
    /// </summary>
    private static readonly Selector sel_isKindOfClass = "isKindOfClass:";
}