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
    /// Executes IntPtr.
    /// </summary>
    public static implicit operator IntPtr(CALayer c) {
        return c.NativePtr;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CALayer" /> class.
    /// </summary>
    public CALayer(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Executes addSublayer.
    /// </summary>
    public void addSublayer(IntPtr layer) {
        objc_msgSend(this.NativePtr, sel_addSublayer, layer);
    }

    /// <summary>
    /// Represents the sel_addSublayer field.
    /// </summary>
    private static readonly Selector sel_addSublayer = "addSublayer:";
}