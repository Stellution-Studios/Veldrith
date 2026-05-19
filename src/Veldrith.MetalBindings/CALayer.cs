using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the CALayer struct.
/// </summary>
public struct CALayer {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Performs the operator IntPtr operation.
    /// </summary>
    /// <param name="c">The value of c.</param>
    /// <returns>The result of the operator IntPtr operation.</returns>
    public static implicit operator IntPtr(CALayer c) {
        return c.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CALayer" /> type.
    /// </summary>
    /// <param name="ptr">The value of ptr.</param>
    public CALayer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Performs the addSublayer operation.
    /// </summary>
    /// <param name="layer">The value of layer.</param>
    public void addSublayer(IntPtr layer) {
        objc_msgSend(this.NativePtr, sel_addSublayer, layer);
    }

    /// <summary>
    /// Represents the sel_addSublayer field.
    /// </summary>
    private static readonly Selector sel_addSublayer = "addSublayer:";
}