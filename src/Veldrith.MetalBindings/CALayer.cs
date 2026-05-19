using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the CALayer struct.
/// </summary>
public struct CALayer {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Executes the operator IntPtr operation.
    /// </summary>
    /// <param name="c">Specifies the value of <paramref name="c" />.</param>
    /// <returns>Returns the result produced by the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(CALayer c) {
        return c.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CALayer" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public CALayer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes the addSublayer operation.
    /// </summary>
    /// <param name="layer">Specifies the value of <paramref name="layer" />.</param>
    public void addSublayer(IntPtr layer) {
        objc_msgSend(this.NativePtr, sel_addSublayer, layer);
    }

    /// <summary>
    /// Stores the value associated with <c>sel_addSublayer</c>.
    /// </summary>
    private static readonly Selector sel_addSublayer = "addSublayer:";
}