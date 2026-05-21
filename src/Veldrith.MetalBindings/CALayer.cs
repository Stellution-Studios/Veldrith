using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CALayer data structure used by the graphics runtime.
/// </summary>
public struct CALayer {

    /// <summary>
    /// Stores the native ptr state used by this instance.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the int ptr logic for this backend.
    /// </summary>
    /// <param name="c">The c value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static implicit operator IntPtr(CALayer c) {
        return c.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CALayer" /> type.
    /// </summary>
    /// <param name="ptr">The ptr value used by this operation.</param>
    public CALayer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the add sublayer logic for this backend.
    /// </summary>
    /// <param name="layer">The layer value used by this operation.</param>
    public void AddSublayer(IntPtr layer) {
        ObjcMsgSend(this.NativePtr, sel_addSublayer, layer);
    }

    /// <summary>
    /// Stores the sel add sublayer state used by this instance.
    /// </summary>
    private static readonly Selector sel_addSublayer = "addSublayer:";
}

