using System;
using static Veldrith.MetalBindings.ObjectiveCRuntime;

namespace Veldrith.MetalBindings;

/// <summary>
/// Defines the data layout and behavior of the MTLFunction struct.
/// </summary>
public struct MTLFunction {

    /// <summary>
    /// Stores the value associated with <c>NativePtr</c>.
    /// </summary>
    public readonly IntPtr NativePtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="MTLFunction" /> type.
    /// </summary>
    /// <param name="ptr">Specifies the value of <paramref name="ptr" />.</param>
    public MTLFunction(IntPtr ptr) {
        this.NativePtr = ptr;
    }

    /// <summary>
    /// Gets or sets functionConstantsDictionary.
    /// </summary>
    public NSDictionary functionConstantsDictionary => objc_msgSend<NSDictionary>(this.NativePtr, sel_functionConstantsDictionary);

    /// <summary>
    /// Stores the value associated with <c>sel_functionConstantsDictionary</c>.
    /// </summary>
    private static readonly Selector sel_functionConstantsDictionary = "functionConstantsDictionary";
}