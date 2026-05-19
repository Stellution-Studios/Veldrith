using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Represents the MTLFunction struct.
/// </summary>
public struct MTLFunction {

    /// <summary>
    /// Represents the NativePtr field.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLFunction" /> class.
    /// </summary>
    public MTLFunction(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets functionConstantsDictionary.
    /// </summary>
    public NSDictionary functionConstantsDictionary => objc_msgSend<NSDictionary>(this.NativePtr, sel_functionConstantsDictionary);

    /// <summary>
    /// Represents the sel_functionConstantsDictionary field.
    /// </summary>
    private static readonly Selector sel_functionConstantsDictionary = "functionConstantsDictionary";
}